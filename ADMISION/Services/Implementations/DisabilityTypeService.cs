using ADMISION.ENTITIES.Data;
using ADMISION.ENTITIES.Models.Postulant;
using ADMISION.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ADMISION.Services.Implementations
{
    public class DisabilityTypeService : IDisabilityTypeService
    {
        private readonly AppDbContext _context;

        public DisabilityTypeService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IReadOnlyList<DisabilityTypeListItem>> ListAsync(string? search, bool? isActive, CancellationToken ct = default)
        {
            var q = _context.DisabilityTypes.AsNoTracking().AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                q = q.Where(x => x.Name.Contains(search) || x.Description.Contains(search));
            }
            if (isActive.HasValue)
            {
                q = q.Where(x => x.IsActive == isActive);
            }

            return await q
                .OrderBy(x => x.Name)
                .Select(x => new DisabilityTypeListItem(x.Id, x.Name, x.Description, x.IsActive))
                .ToListAsync(ct);
        }

        public Task<DisabilityType?> GetByIdAsync(Guid id, CancellationToken ct = default)
            => _context.DisabilityTypes.FirstOrDefaultAsync(d => d.Id == id, ct);

        public async Task<DisabilityType> CreateAsync(DisabilityType entity, string actor, CancellationToken ct = default)
        {
            entity.Id = Guid.NewGuid();
            entity.CreatedAt = DateTimeOffset.UtcNow;
            entity.CreatedBy = actor;
            _context.DisabilityTypes.Add(entity);
            await _context.SaveChangesAsync(ct);
            return entity;
        }

        public async Task<bool> UpdateAsync(DisabilityType entity, string actor, CancellationToken ct = default)
        {
            var existing = await _context.DisabilityTypes.FindAsync(new object[] { entity.Id }, ct);
            if (existing == null) return false;

            existing.Name = entity.Name;
            existing.Description = entity.Description;
            existing.IsActive = entity.IsActive;
            existing.UpdatedAt = DateTimeOffset.UtcNow;
            existing.UpdatedBy = actor;
            _context.DisabilityTypes.Update(existing);
            await _context.SaveChangesAsync(ct);
            return true;
        }

        public async Task<DeleteOutcome> DeleteAsync(Guid id, CancellationToken ct = default)
        {
            var entity = await _context.DisabilityTypes.FindAsync(new object[] { id }, ct);
            if (entity == null) return DeleteOutcome.NotFound;

            try
            {
                _context.DisabilityTypes.Remove(entity);
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
