using ADMISION.ENTITIES.Data;
using ADMISION.ENTITIES.Models.Info;
using ADMISION.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace ADMISION.Services.Implementations
{
    public class BannerService : IBannerService
    {
        private readonly AppDbContext _context;
        private readonly IFileService _files;

        public BannerService(AppDbContext context, IFileService files)
        {
            _context = context;
            _files = files;
        }

        public async Task<List<Banner>> GetActiveBannersAsync()
        {
            return await _context.Banners
                .AsNoTracking()
                .Where(b => b.IsActive)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();
        }

        public async Task<IReadOnlyList<Banner>> GetAllAsync(CancellationToken ct = default)
        {
            return await _context.Banners
                .AsNoTracking()
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync(ct);
        }

        public Task<Banner?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            return _context.Banners.FirstOrDefaultAsync(b => b.Id == id, ct);
        }

        public async Task<Banner> CreateAsync(Banner banner, IFormFile? imageHorizontal, IFormFile? imageVertical, string actor, CancellationToken ct = default)
        {
            await EnsureFileValidAsync(imageHorizontal);
            await EnsureFileValidAsync(imageVertical);

            if (imageHorizontal != null && imageHorizontal.Length > 0)
                banner.ImageUrl = await _files.SaveFileAsync(imageHorizontal, "Banners");

            if (imageVertical != null && imageVertical.Length > 0)
                banner.ImageUrlVertical = await _files.SaveFileAsync(imageVertical, "Banners");

            banner.Id = Guid.NewGuid();
            banner.CreatedAt = DateTimeOffset.UtcNow;
            banner.CreatedBy = actor;

            if (banner.StartDate == default) banner.StartDate = DateTimeOffset.UtcNow;
            else banner.StartDate = banner.StartDate.ToOffset(TimeSpan.Zero);

            if (banner.EndDate == default) banner.EndDate = DateTimeOffset.UtcNow.AddYears(1);
            else banner.EndDate = banner.EndDate.ToOffset(TimeSpan.Zero);

            _context.Banners.Add(banner);
            await _context.SaveChangesAsync(ct);
            return banner;
        }

        public async Task<bool> UpdateAsync(Banner banner, IFormFile? imageHorizontal, IFormFile? imageVertical, string actor, CancellationToken ct = default)
        {
            var existing = await _context.Banners.AsNoTracking().FirstOrDefaultAsync(b => b.Id == banner.Id, ct);
            if (existing == null) return false;

            await EnsureFileValidAsync(imageHorizontal);
            await EnsureFileValidAsync(imageVertical);

            string? oldHorizontalToPurge = null;
            string? oldVerticalToPurge = null;

            if (imageHorizontal != null && imageHorizontal.Length > 0)
            {
                banner.ImageUrl = await _files.SaveFileAsync(imageHorizontal, "Banners");
                if (!string.IsNullOrEmpty(existing.ImageUrl)) oldHorizontalToPurge = existing.ImageUrl;
            }
            else
            {
                banner.ImageUrl = existing.ImageUrl;
            }

            if (imageVertical != null && imageVertical.Length > 0)
            {
                banner.ImageUrlVertical = await _files.SaveFileAsync(imageVertical, "Banners");
                if (!string.IsNullOrEmpty(existing.ImageUrlVertical)) oldVerticalToPurge = existing.ImageUrlVertical;
            }
            else
            {
                banner.ImageUrlVertical = existing.ImageUrlVertical;
            }

            banner.CreatedAt = existing.CreatedAt;
            banner.CreatedBy = existing.CreatedBy;
            banner.UpdatedAt = DateTimeOffset.UtcNow;
            banner.UpdatedBy = actor;

            banner.StartDate = banner.StartDate.ToOffset(TimeSpan.Zero);
            banner.EndDate = banner.EndDate.ToOffset(TimeSpan.Zero);

            _context.Banners.Update(banner);
            await _context.SaveChangesAsync(ct);

            if (oldHorizontalToPurge != null) { try { _files.DeleteFile(oldHorizontalToPurge); } catch { } }
            if (oldVerticalToPurge != null) { try { _files.DeleteFile(oldVerticalToPurge); } catch { } }
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
            var banner = await _context.Banners.FindAsync(new object[] { id }, ct);
            if (banner == null) return DeleteOutcome.NotFound;

            try
            {
                if (!string.IsNullOrEmpty(banner.ImageUrl))
                    _files.DeleteFile(banner.ImageUrl);
                if (!string.IsNullOrEmpty(banner.ImageUrlVertical))
                    _files.DeleteFile(banner.ImageUrlVertical);
                _context.Banners.Remove(banner);
                await _context.SaveChangesAsync(ct);
                return DeleteOutcome.Deleted;
            }
            catch (DbUpdateException)
            {
                return DeleteOutcome.HasDependencies;
            }
        }
    }
}
