using ADMISION.ENTITIES.Data;
using ADMISION.ENTITIES.Models.Info;
using ADMISION.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace ADMISION.Services.Implementations
{
    public class BrochureService : IBrochureService
    {
        private const string StorageModule = "Brochures";
        private readonly AppDbContext _context;
        private readonly IFileService _files;

        public BrochureService(AppDbContext context, IFileService files)
        {
            _context = context;
            _files = files;
        }

        public async Task<IReadOnlyList<Brochure>> GetAllAsync(CancellationToken ct = default)
        {
            return await _context.Brochures
                .AsNoTracking()
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync(ct);
        }

        public async Task<Brochure?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            return await _context.Brochures.FirstOrDefaultAsync(b => b.Id == id, ct);
        }

        public async Task<Brochure?> GetActiveAsync(CancellationToken ct = default)
        {
            return await _context.Brochures
                .AsNoTracking()
                .Where(b => b.IsActive)
                .OrderByDescending(b => b.CreatedAt)
                .FirstOrDefaultAsync(ct);
        }

        public async Task<Brochure> CreateAsync(Brochure brochure, IFormFile? uploadFile, string actor, CancellationToken ct = default)
        {
            await EnsureFileValidAsync(uploadFile);

            if (uploadFile != null && uploadFile.Length > 0)
            {
                ApplyFileMetadata(brochure, await _files.SaveFileAsync(uploadFile, StorageModule), uploadFile);
            }

            brochure.Id = Guid.NewGuid();
            brochure.CreatedAt = DateTimeOffset.UtcNow;
            brochure.CreatedBy = actor;

            _context.Brochures.Add(brochure);
            await _context.SaveChangesAsync(ct);
            return brochure;
        }

        public async Task<bool> UpdateAsync(Brochure brochure, IFormFile? uploadFile, string actor, CancellationToken ct = default)
        {
            var existing = await _context.Brochures.AsNoTracking().FirstOrDefaultAsync(b => b.Id == brochure.Id, ct);
            if (existing == null) return false;

            await EnsureFileValidAsync(uploadFile);

            string? oldFileToPurge = null;

            if (uploadFile != null && uploadFile.Length > 0)
            {
                ApplyFileMetadata(brochure, await _files.SaveFileAsync(uploadFile, StorageModule), uploadFile);
                if (!string.IsNullOrEmpty(existing.FileUrl)) oldFileToPurge = existing.FileUrl;
            }
            else
            {
                brochure.FileUrl = existing.FileUrl;
                brochure.FileName = existing.FileName;
                brochure.FileType = existing.FileType;
                brochure.FileSize = existing.FileSize;
            }

            brochure.CreatedAt = existing.CreatedAt;
            brochure.CreatedBy = existing.CreatedBy;
            brochure.UpdatedAt = DateTimeOffset.UtcNow;
            brochure.UpdatedBy = actor;

            _context.Brochures.Update(brochure);
            await _context.SaveChangesAsync(ct);

            if (oldFileToPurge != null)
            {
                try { _files.DeleteFile(oldFileToPurge); } catch { }
            }
            return true;
        }

        public async Task<DeleteOutcome> DeleteAsync(Guid id, CancellationToken ct = default)
        {
            var brochure = await _context.Brochures.FindAsync(new object[] { id }, ct);
            if (brochure == null) return DeleteOutcome.NotFound;

            try
            {
                if (!string.IsNullOrEmpty(brochure.FileUrl)) _files.DeleteFile(brochure.FileUrl);
                _context.Brochures.Remove(brochure);
                await _context.SaveChangesAsync(ct);
                return DeleteOutcome.Deleted;
            }
            catch (DbUpdateException)
            {
                return DeleteOutcome.HasDependencies;
            }
        }

        private async Task EnsureFileValidAsync(IFormFile? file)
        {
            if (file is null || file.Length == 0) return;
            var result = await _files.ValidateFileAsync(file);
            if (!result.IsValid)
                throw new InvalidFileException(file.FileName ?? "archivo", result.Reason);
        }

        private static void ApplyFileMetadata(Brochure entity, string url, IFormFile file)
        {
            entity.FileUrl = url;
            entity.FileName = file.FileName;
            entity.FileType = file.ContentType;
            entity.FileSize = (file.Length / 1024.0 / 1024.0).ToString("F2") + " MB";
        }
    }
}
