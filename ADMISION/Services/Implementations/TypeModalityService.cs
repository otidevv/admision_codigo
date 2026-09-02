using ADMISION.ENTITIES.Data;
using ADMISION.ENTITIES.Models.Modality;
using ADMISION.Models.Shared;
using ADMISION.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ADMISION.Services.Implementations
{
    public class TypeModalityService : ITypeModalityService
    {
        private readonly AppDbContext _context;

        public TypeModalityService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<PagedResult<TypeModalityListItem>> ListAsync(TypeModalityListQuery query, CancellationToken ct = default)
        {
            var q = _context.TypeModalities
                .AsNoTracking()
                .Include(t => t.Modality)
                .AsQueryable();

            if (query.TermId.HasValue && query.TermId.Value != Guid.Empty)
            {
                q = q.Where(t => t.Modality != null && t.Modality.TermId == query.TermId.Value);
            }

            if (query.ModalityId.HasValue && query.ModalityId.Value != Guid.Empty)
            {
                q = q.Where(t => t.ModalityId == query.ModalityId.Value);
            }

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var search = query.Search.Trim();
                q = q.Where(t => EF.Functions.ILike(t.Name, $"%{search}%"));
            }

            // Ordenar sobre la entidad ANTES de proyectar — EF Core 10 no traduce
            // OrderBy sobre records posicionales (positional record constructor).
            q = (query.SortBy?.ToLowerInvariant()) switch
            {
                "name" => query.IsDescending ? q.OrderByDescending(t => t.Name) : q.OrderBy(t => t.Name),
                "modality" => query.IsDescending ? q.OrderByDescending(t => t.Modality!.Name) : q.OrderBy(t => t.Modality!.Name),
                "isactive" => query.IsDescending ? q.OrderByDescending(t => t.IsActive) : q.OrderBy(t => t.IsActive),
                _ => q.OrderByDescending(t => t.Name)
            };

            var projected = q.Select(t => new TypeModalityListItem(
                t.Id,
                t.Name,
                t.Description,
                t.DiscountPercentage,
                t.IsActive,
                t.Modality != null ? t.Modality.Name : null));

            return await PagedResult<TypeModalityListItem>.CreateAsync(projected, query.Page, query.PageSize, ct);
        }

        public Task<TypeModality?> GetByIdAsync(Guid id, bool includeModality = false, CancellationToken ct = default)
        {
            var q = _context.TypeModalities.AsQueryable();
            if (includeModality) q = q.Include(t => t.Modality);
            return q.FirstOrDefaultAsync(t => t.Id == id, ct);
        }

        public async Task<TypeModality> CreateAsync(TypeModality entity, string actor, CancellationToken ct = default)
        {
            entity.Id = Guid.NewGuid();
            entity.CreatedAt = DateTimeOffset.UtcNow;
            entity.CreatedBy = actor;
            _context.TypeModalities.Add(entity);
            await _context.SaveChangesAsync(ct);
            return entity;
        }

        public async Task<bool> UpdateAsync(TypeModality entity, string actor, CancellationToken ct = default)
        {
            var existing = await _context.TypeModalities.AsNoTracking().FirstOrDefaultAsync(t => t.Id == entity.Id, ct);
            if (existing == null) return false;

            entity.CreatedAt = existing.CreatedAt;
            entity.CreatedBy = existing.CreatedBy;
            entity.UpdatedAt = DateTimeOffset.UtcNow;
            entity.UpdatedBy = actor;

            _context.TypeModalities.Update(entity);
            await _context.SaveChangesAsync(ct);
            return true;
        }

        public async Task<DeleteOutcome> DeleteAsync(Guid id, CancellationToken ct = default)
        {
            var entity = await _context.TypeModalities.FindAsync(new object[] { id }, ct);
            if (entity == null) return DeleteOutcome.NotFound;

            try
            {
                _context.TypeModalities.Remove(entity);
                await _context.SaveChangesAsync(ct);
                return DeleteOutcome.Deleted;
            }
            catch (DbUpdateException)
            {
                return DeleteOutcome.HasDependencies;
            }
        }

        public async Task<List<Guid>> GetCareerIdsAsync(Guid typeModalityId, CancellationToken ct = default)
        {
            return await _context.TypeModalityCareers
                .AsNoTracking()
                .Where(tmc => tmc.TypeModalityId == typeModalityId)
                .Select(tmc => tmc.CareerId)
                .ToListAsync(ct);
        }

        public async Task SaveCareerAssociationsAsync(Guid typeModalityId, List<Guid> careerIds, CancellationToken ct = default)
        {
            var existing = await _context.TypeModalityCareers
                .Where(tmc => tmc.TypeModalityId == typeModalityId)
                .ToListAsync(ct);

            _context.TypeModalityCareers.RemoveRange(existing);

            foreach (var careerId in careerIds)
            {
                _context.TypeModalityCareers.Add(new TypeModalityCareer
                {
                    Id = Guid.NewGuid(),
                    TypeModalityId = typeModalityId,
                    CareerId = careerId
                });
            }

            await _context.SaveChangesAsync(ct);
        }
    }
}
