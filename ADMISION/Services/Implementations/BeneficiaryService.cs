using ADMISION.ENTITIES.Data;
using ADMISION.ENTITIES.Models.Modality;
using ADMISION.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ADMISION.Services.Implementations
{
    public class BeneficiaryService : IBeneficiaryService
    {
        private readonly AppDbContext _context;

        public BeneficiaryService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IReadOnlyList<Beneficiarie>> GetAllAsync(CancellationToken ct = default)
        {
            return await _context.Beneficiaries
                .AsNoTracking()
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync(ct);
        }

        public Task<Beneficiarie?> GetByIdAsync(Guid id, CancellationToken ct = default)
            => _context.Beneficiaries.FirstOrDefaultAsync(b => b.Id == id, ct);

        public async Task<Beneficiarie> CreateAsync(Beneficiarie entity, string actor, CancellationToken ct = default)
        {
            entity.Id = Guid.NewGuid();
            entity.CreatedAt = DateTimeOffset.UtcNow;
            entity.CreatedBy = actor;
            _context.Beneficiaries.Add(entity);
            await _context.SaveChangesAsync(ct);
            return entity;
        }

        public async Task<bool> UpdateAsync(Beneficiarie entity, string actor, CancellationToken ct = default)
        {
            var existing = await _context.Beneficiaries.AsNoTracking().FirstOrDefaultAsync(b => b.Id == entity.Id, ct);
            if (existing == null) return false;

            entity.CreatedAt = existing.CreatedAt;
            entity.CreatedBy = existing.CreatedBy;
            entity.UpdatedAt = DateTimeOffset.UtcNow;
            entity.UpdatedBy = actor;
            _context.Beneficiaries.Update(entity);
            await _context.SaveChangesAsync(ct);
            return true;
        }

        public async Task<DeleteOutcome> DeleteAsync(Guid id, CancellationToken ct = default)
        {
            var entity = await _context.Beneficiaries.FindAsync(new object[] { id }, ct);
            if (entity == null) return DeleteOutcome.NotFound;

            try
            {
                _context.Beneficiaries.Remove(entity);
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
