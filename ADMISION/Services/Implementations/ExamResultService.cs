using ADMISION.ENTITIES.Data;
using ADMISION.ENTITIES.Models.Modality;
using ADMISION.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace ADMISION.Services.Implementations
{
    public class ExamResultService : IExamResultService
    {
        private readonly AppDbContext _context;
        private readonly IFileService _files;

        public ExamResultService(AppDbContext context, IFileService files)
        {
            _context = context;
            _files = files;
        }

        public async Task<IReadOnlyList<ExamResult>> GetAllAsync(CancellationToken ct = default)
        {
            return await _context.ExamResults
                .AsNoTracking()
                .Include(r => r.Term)
                .Include(r => r.Modality)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync(ct);
        }

        public Task<ExamResult?> GetByIdAsync(Guid id, CancellationToken ct = default)
            => _context.ExamResults.FirstOrDefaultAsync(r => r.Id == id, ct);

        public async Task<ExamResult> CreateAsync(ExamResult result, IFormFile pdfFile, string actor, CancellationToken ct = default)
        {
            await EnsureFileValidAsync(pdfFile);

            ApplyFileMetadata(result, await _files.SaveFileAsync(pdfFile, "ExamResults"), pdfFile);

            result.Id = Guid.NewGuid();
            result.PublishedAt = result.IsActive ? DateTimeOffset.UtcNow : null;
            result.CreatedAt = DateTimeOffset.UtcNow;
            result.CreatedBy = actor;

            _context.ExamResults.Add(result);
            await _context.SaveChangesAsync(ct);
            return result;
        }

        public async Task<bool> UpdateAsync(ExamResult result, IFormFile? pdfFile, string actor, CancellationToken ct = default)
        {
            var existing = await _context.ExamResults.AsNoTracking().FirstOrDefaultAsync(r => r.Id == result.Id, ct);
            if (existing == null) return false;

            // Pre-validar antes de borrar el PDF previo.
            await EnsureFileValidAsync(pdfFile);

            string? oldFileToPurge = null;

            if (pdfFile != null && pdfFile.Length > 0)
            {
                ApplyFileMetadata(result, await _files.SaveFileAsync(pdfFile, "ExamResults"), pdfFile);
                if (!string.IsNullOrEmpty(existing.FileUrl)) oldFileToPurge = existing.FileUrl;
            }
            else
            {
                result.FileUrl = existing.FileUrl;
                result.FileName = existing.FileName;
                result.FileType = existing.FileType;
                result.FileSize = existing.FileSize;
            }

            // PublishedAt: fijar al activar por primera vez; si se desactiva, limpiar.
            result.PublishedAt = result.IsActive
                ? (existing.PublishedAt ?? DateTimeOffset.UtcNow)
                : null;

            result.CreatedAt = existing.CreatedAt;
            result.CreatedBy = existing.CreatedBy;
            result.UpdatedAt = DateTimeOffset.UtcNow;
            result.UpdatedBy = actor;

            _context.ExamResults.Update(result);
            await _context.SaveChangesAsync(ct);

            if (oldFileToPurge != null)
            {
                try { _files.DeleteFile(oldFileToPurge); } catch { /* best-effort */ }
            }
            return true;
        }

        private async Task EnsureFileValidAsync(IFormFile? file)
        {
            if (file is null || file.Length == 0) return;
            var result = await _files.ValidateFileAsync(file);
            if (!result.IsValid)
                throw new InvalidFileException(file.FileName ?? "archivo", result.Reason);
        }

        public async Task<DeleteOutcome> DeleteAsync(Guid id, CancellationToken ct = default)
        {
            var result = await _context.ExamResults.FindAsync(new object[] { id }, ct);
            if (result == null) return DeleteOutcome.NotFound;

            try
            {
                if (!string.IsNullOrEmpty(result.FileUrl)) _files.DeleteFile(result.FileUrl);
                _context.ExamResults.Remove(result);
                await _context.SaveChangesAsync(ct);
                return DeleteOutcome.Deleted;
            }
            catch (DbUpdateException)
            {
                return DeleteOutcome.HasDependencies;
            }
        }

        private static void ApplyFileMetadata(ExamResult result, string url, IFormFile file)
        {
            result.FileUrl = url;
            result.FileName = file.FileName;
            result.FileType = file.ContentType;
            result.FileSize = (file.Length / 1024.0 / 1024.0).ToString("F2") + " MB";
        }
    }
}
