using ADMISION.ENTITIES.Data;
using ADMISION.ENTITIES.Models.Postulant;
using ADMISION.Models.Shared;
using ADMISION.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ADMISION.Services.Implementations
{
    public class TypePostulantInscriptionService : ITypePostulantInscriptionService
    {
        private readonly AppDbContext _context;

        public TypePostulantInscriptionService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<PagedResult<TypePostulantListItem>> ListAsync(ListQuery query, CancellationToken ct = default)
        {
            var q = _context.TypePostulantInscriptions.AsNoTracking().AsQueryable();

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
                "discountpercentage" => query.IsDescending ? q.OrderByDescending(t => t.DiscountPercentage) : q.OrderBy(t => t.DiscountPercentage),
                _ => q.OrderByDescending(t => t.Name)
            };

            var projected = q.Select(t => new TypePostulantListItem(
                t.Id, t.Name, t.Description, t.DiscountPercentage, t.IsActive));

            return await PagedResult<TypePostulantListItem>.CreateAsync(projected, query.Page, query.PageSize, ct);
        }

        public Task<TypePostulantInscription?> GetByIdAsync(Guid id, CancellationToken ct = default)
            => _context.TypePostulantInscriptions.FirstOrDefaultAsync(t => t.Id == id, ct);

        public async Task<TypePostulantInscription> CreateAsync(TypePostulantInscription entity, string actor, CancellationToken ct = default)
        {
            entity.Id = Guid.NewGuid();
            entity.CreatedAt = DateTimeOffset.UtcNow;
            entity.CreatedBy = actor;
            _context.TypePostulantInscriptions.Add(entity);
            await _context.SaveChangesAsync(ct);
            return entity;
        }

        public async Task<bool> UpdateAsync(TypePostulantInscription entity, string actor, CancellationToken ct = default)
        {
            var existing = await _context.TypePostulantInscriptions.AsNoTracking().FirstOrDefaultAsync(t => t.Id == entity.Id, ct);
            if (existing == null) return false;

            entity.CreatedAt = existing.CreatedAt;
            entity.CreatedBy = existing.CreatedBy;
            entity.UpdatedAt = DateTimeOffset.UtcNow;
            entity.UpdatedBy = actor;
            _context.TypePostulantInscriptions.Update(entity);
            await _context.SaveChangesAsync(ct);
            return true;
        }

        public async Task<DeleteOutcome> DeleteAsync(Guid id, CancellationToken ct = default)
        {
            var entity = await _context.TypePostulantInscriptions.FindAsync(new object[] { id }, ct);
            if (entity == null) return DeleteOutcome.NotFound;

            try
            {
                _context.TypePostulantInscriptions.Remove(entity);
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
