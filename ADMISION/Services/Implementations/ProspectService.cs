using ADMISION.ENTITIES.Data;
using ADMISION.ENTITIES.Models.Info;
using ADMISION.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace ADMISION.Services.Implementations
{
    public class ProspectService : IProspectService
    {
        private readonly AppDbContext _context;
        private readonly IFileService _files;

        public ProspectService(AppDbContext context, IFileService files)
        {
            _context = context;
            _files = files;
        }

        public async Task<IReadOnlyList<Prospect>> GetAllAsync(CancellationToken ct = default)
        {
            return await _context.Prospects
                .AsNoTracking()
                .Include(p => p.Term)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync(ct);
        }

        public Task<Prospect?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            return _context.Prospects.FirstOrDefaultAsync(p => p.Id == id, ct);
        }

        public async Task<Prospect> CreateAsync(Prospect prospect, IFormFile? pdfFile, string actor, CancellationToken ct = default)
        {
            await EnsureFileValidAsync(pdfFile);

            if (pdfFile != null && pdfFile.Length > 0)
            {
                ApplyFileMetadata(prospect, await _files.SaveFileAsync(pdfFile, "Prospects"), pdfFile);
            }

            prospect.Id = Guid.NewGuid();
            prospect.CreatedAt = DateTimeOffset.UtcNow;
            prospect.CreatedBy = actor;

            _context.Prospects.Add(prospect);
            await _context.SaveChangesAsync(ct);
            return prospect;
        }

        public async Task<bool> UpdateAsync(Prospect prospect, IFormFile? pdfFile, string actor, CancellationToken ct = default)
        {
            var existing = await _context.Prospects.AsNoTracking().FirstOrDefaultAsync(p => p.Id == prospect.Id, ct);
            if (existing == null) return false;

            // Pre-validar antes de borrar el PDF previo.
            await EnsureFileValidAsync(pdfFile);

            string? oldFileToPurge = null;

            if (pdfFile != null && pdfFile.Length > 0)
            {
                ApplyFileMetadata(prospect, await _files.SaveFileAsync(pdfFile, "Prospects"), pdfFile);
                if (!string.IsNullOrEmpty(existing.FileUrl)) oldFileToPurge = existing.FileUrl;
            }
            else
            {
                prospect.FileUrl = existing.FileUrl;
                prospect.FileName = existing.FileName;
                prospect.FileType = existing.FileType;
                prospect.FileSize = existing.FileSize;
            }

            prospect.CreatedAt = existing.CreatedAt;
            prospect.CreatedBy = existing.CreatedBy;
            prospect.UpdatedAt = DateTimeOffset.UtcNow;
            prospect.UpdatedBy = actor;

            _context.Prospects.Update(prospect);
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
            var prospect = await _context.Prospects.FindAsync(new object[] { id }, ct);
            if (prospect == null) return DeleteOutcome.NotFound;

            try
            {
                if (!string.IsNullOrEmpty(prospect.FileUrl)) _files.DeleteFile(prospect.FileUrl);
                _context.Prospects.Remove(prospect);
                await _context.SaveChangesAsync(ct);
                return DeleteOutcome.Deleted;
            }
            catch (DbUpdateException)
            {
                return DeleteOutcome.HasDependencies;
            }
        }

        private static void ApplyFileMetadata(Prospect prospect, string url, IFormFile file)
        {
            prospect.FileUrl = url;
            prospect.FileName = file.FileName;
            prospect.FileType = file.ContentType;
            prospect.FileSize = (file.Length / 1024.0 / 1024.0).ToString("F2") + " MB";
        }
    }
}
