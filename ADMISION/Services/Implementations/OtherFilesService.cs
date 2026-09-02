using ADMISION.ENTITIES.Data;
using ADMISION.ENTITIES.Models.Info;
using ADMISION.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace ADMISION.Services.Implementations
{
    public class OtherFilesService : IOtherFilesService
    {
        private readonly AppDbContext _context;
        private readonly IFileService _files;

        public OtherFilesService(AppDbContext context, IFileService files)
        {
            _context = context;
            _files = files;
        }

        public async Task<IReadOnlyList<OtherFiles>> GetByCategoryAsync(string category, CancellationToken ct = default)
        {
            return await _context.OtherFiles
                .AsNoTracking()
                .Where(f => f.Category == category)
                .OrderByDescending(f => f.CreatedAt)
                .ToListAsync(ct);
        }

        public async Task<OtherFiles?> GetByIdAsync(Guid id, string category, CancellationToken ct = default)
        {
            var file = await _context.OtherFiles.FirstOrDefaultAsync(f => f.Id == id, ct);
            // Bloquea acceso cruzado entre módulos: Syllabi no debe editar Reglamentos.
            if (file != null && file.Category != category) return null;
            return file;
        }

        public async Task<OtherFiles> CreateAsync(OtherFiles file, IFormFile? uploadFile, string category, string storageModule, string actor, CancellationToken ct = default)
        {
            await EnsureFileValidAsync(uploadFile);

            if (uploadFile != null && uploadFile.Length > 0)
            {
                ApplyFileMetadata(file, await _files.SaveFileAsync(uploadFile, storageModule), uploadFile);
            }

            file.Id = Guid.NewGuid();
            file.Category = category;
            file.CreatedAt = DateTimeOffset.UtcNow;
            file.CreatedBy = actor;

            _context.OtherFiles.Add(file);
            await _context.SaveChangesAsync(ct);
            return file;
        }

        public async Task<bool> UpdateAsync(OtherFiles file, IFormFile? uploadFile, string category, string storageModule, string actor, CancellationToken ct = default)
        {
            var existing = await _context.OtherFiles.AsNoTracking().FirstOrDefaultAsync(f => f.Id == file.Id, ct);
            if (existing == null || existing.Category != category) return false;

            // Pre-validar antes de borrar el archivo previo.
            await EnsureFileValidAsync(uploadFile);

            string? oldFileToPurge = null;

            if (uploadFile != null && uploadFile.Length > 0)
            {
                ApplyFileMetadata(file, await _files.SaveFileAsync(uploadFile, storageModule), uploadFile);
                if (!string.IsNullOrEmpty(existing.FileUrl)) oldFileToPurge = existing.FileUrl;
            }
            else
            {
                file.FileUrl = existing.FileUrl;
                file.FileName = existing.FileName;
                file.FileType = existing.FileType;
                file.FileSize = existing.FileSize;
            }

            file.Category = category;
            file.CreatedAt = existing.CreatedAt;
            file.CreatedBy = existing.CreatedBy;
            file.UpdatedAt = DateTimeOffset.UtcNow;
            file.UpdatedBy = actor;

            _context.OtherFiles.Update(file);
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

        public async Task<DeleteOutcome> DeleteAsync(Guid id, string category, CancellationToken ct = default)
        {
            var file = await _context.OtherFiles.FindAsync(new object[] { id }, ct);
            if (file == null || file.Category != category) return DeleteOutcome.NotFound;

            try
            {
                if (!string.IsNullOrEmpty(file.FileUrl)) _files.DeleteFile(file.FileUrl);
                _context.OtherFiles.Remove(file);
                await _context.SaveChangesAsync(ct);
                return DeleteOutcome.Deleted;
            }
            catch (DbUpdateException)
            {
                return DeleteOutcome.HasDependencies;
            }
        }

        private static void ApplyFileMetadata(OtherFiles entity, string url, IFormFile file)
        {
            entity.FileUrl = url;
            entity.FileName = file.FileName;
            entity.FileType = file.ContentType;
            entity.FileSize = (file.Length / 1024.0 / 1024.0).ToString("F2") + " MB";
        }
    }
}
