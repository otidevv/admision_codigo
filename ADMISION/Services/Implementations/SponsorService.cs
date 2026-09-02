using ADMISION.ENTITIES.Data;
using ADMISION.ENTITIES.Models.Info;
using ADMISION.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace ADMISION.Services.Implementations
{
    public class SponsorService : ISponsorService
    {
        private readonly AppDbContext _context;
        private readonly IFileService _files;

        public SponsorService(AppDbContext context, IFileService files)
        {
            _context = context;
            _files = files;
        }

        public async Task<IReadOnlyList<Sponsor>> GetAllAsync(CancellationToken ct = default)
        {
            return await _context.Sponsors
                .AsNoTracking()
                .OrderBy(s => s.DisplayOrder)
                .ThenBy(s => s.Name)
                .ToListAsync(ct);
        }

        public async Task<List<Sponsor>> GetActiveSponsorsAsync(CancellationToken ct = default)
        {
            return await _context.Sponsors
                .AsNoTracking()
                .Where(s => s.IsActive)
                .OrderBy(s => s.DisplayOrder)
                .ThenBy(s => s.Name)
                .ToListAsync(ct);
        }

        public Task<Sponsor?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            return _context.Sponsors.FirstOrDefaultAsync(s => s.Id == id, ct);
        }

        public async Task<Sponsor> CreateAsync(Sponsor sponsor, IFormFile? logo, string actor, CancellationToken ct = default)
        {
            if (logo is not null && logo.Length > 0)
            {
                var result = await _files.ValidateFileAsync(logo);
                if (!result.IsValid)
                    throw new InvalidFileException(logo.FileName ?? "archivo", result.Reason);
                sponsor.LogoUrl = await _files.SaveFileAsync(logo, "Sponsors");
            }

            sponsor.Id = Guid.NewGuid();
            sponsor.CreatedAt = DateTimeOffset.UtcNow;
            sponsor.CreatedBy = actor;

            _context.Sponsors.Add(sponsor);
            await _context.SaveChangesAsync(ct);
            return sponsor;
        }

        public async Task<bool> UpdateAsync(Sponsor sponsor, IFormFile? logo, string actor, CancellationToken ct = default)
        {
            var existing = await _context.Sponsors.AsNoTracking().FirstOrDefaultAsync(s => s.Id == sponsor.Id, ct);
            if (existing == null) return false;

            string? oldLogoToPurge = null;

            if (logo is not null && logo.Length > 0)
            {
                var result = await _files.ValidateFileAsync(logo);
                if (!result.IsValid)
                    throw new InvalidFileException(logo.FileName ?? "archivo", result.Reason);
                sponsor.LogoUrl = await _files.SaveFileAsync(logo, "Sponsors");
                if (!string.IsNullOrEmpty(existing.LogoUrl)) oldLogoToPurge = existing.LogoUrl;
            }
            else
            {
                sponsor.LogoUrl = existing.LogoUrl;
            }

            sponsor.CreatedAt = existing.CreatedAt;
            sponsor.CreatedBy = existing.CreatedBy;
            sponsor.UpdatedAt = DateTimeOffset.UtcNow;
            sponsor.UpdatedBy = actor;

            _context.Sponsors.Update(sponsor);
            await _context.SaveChangesAsync(ct);

            if (oldLogoToPurge != null) { try { _files.DeleteFile(oldLogoToPurge); } catch { } }
            return true;
        }

        public async Task<DeleteOutcome> DeleteAsync(Guid id, CancellationToken ct = default)
        {
            var sponsor = await _context.Sponsors.FindAsync(new object[] { id }, ct);
            if (sponsor == null) return DeleteOutcome.NotFound;

            try
            {
                if (!string.IsNullOrEmpty(sponsor.LogoUrl))
                    _files.DeleteFile(sponsor.LogoUrl);
                _context.Sponsors.Remove(sponsor);
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
