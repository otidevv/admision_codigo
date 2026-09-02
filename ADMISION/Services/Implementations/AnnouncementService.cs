using ADMISION.ENTITIES.Data;
using ADMISION.ENTITIES.Models.Info;
using ADMISION.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace ADMISION.Services.Implementations
{
    public class AnnouncementService : IAnnouncementService
    {
        private readonly AppDbContext _context;
        private readonly IFileService _files;

        public AnnouncementService(AppDbContext context, IFileService files)
        {
            _context = context;
            _files = files;
        }

        public async Task<IReadOnlyList<Announcement>> GetAllAsync(CancellationToken ct = default)
        {
            return await _context.Announcements
                .AsNoTracking()
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync(ct);
        }

        public async Task<List<Announcement>> GetActiveAnnouncementsAsync(CancellationToken ct = default)
        {
            var now = DateTimeOffset.UtcNow;
            return await _context.Announcements
                .AsNoTracking()
                .Where(a => a.IsActive && a.StartDate <= now && a.EndDate >= now)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync(ct);
        }

        public Task<Announcement?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            return _context.Announcements.FirstOrDefaultAsync(a => a.Id == id, ct);
        }

        public async Task<Announcement> CreateAsync(Announcement announcement, IFormFile? image, string actor, CancellationToken ct = default)
        {
            if (image is not null && image.Length > 0)
            {
                var result = await _files.ValidateFileAsync(image);
                if (!result.IsValid)
                    throw new InvalidFileException(image.FileName ?? "archivo", result.Reason);
                announcement.ImageUrl = await _files.SaveFileAsync(image, "Announcements");
            }

            announcement.Id = Guid.NewGuid();
            announcement.CreatedAt = DateTimeOffset.UtcNow;
            announcement.CreatedBy = actor;

            if (announcement.StartDate == default) announcement.StartDate = DateTimeOffset.UtcNow;
            else announcement.StartDate = announcement.StartDate.ToOffset(TimeSpan.Zero);

            if (announcement.EndDate == default) announcement.EndDate = DateTimeOffset.UtcNow.AddYears(1);
            else announcement.EndDate = announcement.EndDate.ToOffset(TimeSpan.Zero);

            _context.Announcements.Add(announcement);
            await _context.SaveChangesAsync(ct);
            return announcement;
        }

        public async Task<bool> UpdateAsync(Announcement announcement, IFormFile? image, string actor, CancellationToken ct = default)
        {
            var existing = await _context.Announcements.AsNoTracking().FirstOrDefaultAsync(a => a.Id == announcement.Id, ct);
            if (existing == null) return false;

            string? oldImageToPurge = null;

            if (image is not null && image.Length > 0)
            {
                var result = await _files.ValidateFileAsync(image);
                if (!result.IsValid)
                    throw new InvalidFileException(image.FileName ?? "archivo", result.Reason);
                announcement.ImageUrl = await _files.SaveFileAsync(image, "Announcements");
                if (!string.IsNullOrEmpty(existing.ImageUrl)) oldImageToPurge = existing.ImageUrl;
            }
            else
            {
                announcement.ImageUrl = existing.ImageUrl;
            }

            announcement.CreatedAt = existing.CreatedAt;
            announcement.CreatedBy = existing.CreatedBy;
            announcement.UpdatedAt = DateTimeOffset.UtcNow;
            announcement.UpdatedBy = actor;

            announcement.StartDate = announcement.StartDate.ToOffset(TimeSpan.Zero);
            announcement.EndDate = announcement.EndDate.ToOffset(TimeSpan.Zero);

            _context.Announcements.Update(announcement);
            await _context.SaveChangesAsync(ct);

            if (oldImageToPurge != null) { try { _files.DeleteFile(oldImageToPurge); } catch { } }
            return true;
        }

        public async Task<DeleteOutcome> DeleteAsync(Guid id, CancellationToken ct = default)
        {
            var announcement = await _context.Announcements.FindAsync(new object[] { id }, ct);
            if (announcement == null) return DeleteOutcome.NotFound;

            try
            {
                if (!string.IsNullOrEmpty(announcement.ImageUrl))
                    _files.DeleteFile(announcement.ImageUrl);
                _context.Announcements.Remove(announcement);
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
