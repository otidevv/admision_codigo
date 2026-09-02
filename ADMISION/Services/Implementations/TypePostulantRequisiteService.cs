using ADMISION.ENTITIES.Data;
using ADMISION.ENTITIES.Models.Requirement;
using ADMISION.Models.Shared;
using ADMISION.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ADMISION.Services.Implementations
{
    public class TypePostulantRequisiteService : ITypePostulantRequisiteService
    {
        private readonly AppDbContext _context;

        public TypePostulantRequisiteService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<PagedResult<TypePostulantRequisiteListItem>> ListAsync(TypePostulantRequisiteListQuery query, CancellationToken ct = default)
        {
            var q = _context.TypePostulantRequisites
                .AsNoTracking()
                .Include(m => m.TypePostulantInscription)
                .Include(m => m.FileRequirementManagement)
                .AsQueryable();

            if (query.TypePostulantInscriptionId.HasValue && query.TypePostulantInscriptionId.Value != Guid.Empty)
            {
                q = q.Where(m => m.TypePostulantInscriptionId == query.TypePostulantInscriptionId.Value);
            }

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var search = query.Search.Trim();
                q = q.Where(t =>
                    EF.Functions.ILike(t.FileRequirementManagement!.Name, $"%{search}%") ||
                    EF.Functions.ILike(t.TypePostulantInscription!.Name, $"%{search}%"));
            }

            // Ordenar sobre la entidad ANTES de proyectar — EF Core 10 no traduce
            // OrderBy sobre records posicionales (positional record constructor).
            q = (query.SortBy?.ToLowerInvariant()) switch
            {
                "type" => query.IsDescending ? q.OrderByDescending(t => t.TypePostulantInscription!.Name) : q.OrderBy(t => t.TypePostulantInscription!.Name),
                "requirement" => query.IsDescending ? q.OrderByDescending(t => t.FileRequirementManagement!.Name) : q.OrderBy(t => t.FileRequirementManagement!.Name),
                _ => q.OrderBy(t => t.TypePostulantInscription!.Name)
            };

            var projected = q.Select(t => new TypePostulantRequisiteListItem(
                t.Id,
                t.TypePostulantInscription != null ? t.TypePostulantInscription.Name : null,
                t.FileRequirementManagement != null ? t.FileRequirementManagement.Name : null));

            return await PagedResult<TypePostulantRequisiteListItem>.CreateAsync(projected, query.Page, query.PageSize, ct);
        }

        public async Task<TypePostulantRequisiteCreateOutcome> CreateAsync(TypePostulantRequisite entity, string actor, CancellationToken ct = default)
        {
            var exists = await _context.TypePostulantRequisites.AnyAsync(mr =>
                mr.TypePostulantInscriptionId == entity.TypePostulantInscriptionId &&
                mr.FileRequirementManagementId == entity.FileRequirementManagementId, ct);

            if (exists) return TypePostulantRequisiteCreateOutcome.Duplicate();

            entity.Id = Guid.NewGuid();
            entity.CreatedAt = DateTimeOffset.UtcNow;
            entity.CreatedBy = actor;
            _context.TypePostulantRequisites.Add(entity);
            await _context.SaveChangesAsync(ct);
            return TypePostulantRequisiteCreateOutcome.Ok(entity.TypePostulantInscriptionId);
        }

        public async Task<DeleteAssignmentOutcome> DeleteAsync(Guid id, CancellationToken ct = default)
        {
            var item = await _context.TypePostulantRequisites.FindAsync(new object[] { id }, ct);
            if (item == null) return new DeleteAssignmentOutcome { Outcome = DeleteOutcome.NotFound };

            var typePostulantId = item.TypePostulantInscriptionId;
            try
            {
                _context.TypePostulantRequisites.Remove(item);
                await _context.SaveChangesAsync(ct);
                return new DeleteAssignmentOutcome { Outcome = DeleteOutcome.Deleted, TypePostulantInscriptionId = typePostulantId };
            }
            catch (DbUpdateException)
            {
                return new DeleteAssignmentOutcome { Outcome = DeleteOutcome.HasDependencies, TypePostulantInscriptionId = typePostulantId };
            }
        }
    }
}
