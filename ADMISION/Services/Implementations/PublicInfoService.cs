using ADMISION.ENTITIES.Data;
using ADMISION.ENTITIES.Models.Info;
using ADMISION.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ADMISION.Services.Implementations
{
    public class PublicInfoService : IPublicInfoService
    {
        private readonly AppDbContext _context;

        public PublicInfoService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IReadOnlyList<PublicInfo>> GetAllAsync(CancellationToken ct = default)
        {
            return await _context.PublicInfos
                .AsNoTracking()
                .Include(p => p.Term)
                .Include(p => p.Modality)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync(ct);
        }

        public Task<PublicInfo?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            return _context.PublicInfos.FirstOrDefaultAsync(p => p.Id == id, ct);
        }

        public async Task<PublicInfo> CreateAsync(PublicInfo info, string actor, CancellationToken ct = default)
        {
            info.Id = Guid.NewGuid();
            info.CreatedAt = DateTimeOffset.UtcNow;
            info.CreatedBy = actor;
            _context.PublicInfos.Add(info);
            await _context.SaveChangesAsync(ct);
            return info;
        }

        public async Task<bool> UpdateAsync(PublicInfo info, string actor, CancellationToken ct = default)
        {
            var existing = await _context.PublicInfos.AsNoTracking().FirstOrDefaultAsync(p => p.Id == info.Id, ct);
            if (existing == null) return false;

            info.CreatedAt = existing.CreatedAt;
            info.CreatedBy = existing.CreatedBy;
            info.UpdatedAt = DateTimeOffset.UtcNow;
            info.UpdatedBy = actor;
            _context.PublicInfos.Update(info);
            await _context.SaveChangesAsync(ct);
            return true;
        }

        public async Task<DeleteOutcome> DeleteAsync(Guid id, CancellationToken ct = default)
        {
            var info = await _context.PublicInfos.FindAsync(new object[] { id }, ct);
            if (info == null) return DeleteOutcome.NotFound;

            try
            {
                _context.PublicInfos.Remove(info);
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
