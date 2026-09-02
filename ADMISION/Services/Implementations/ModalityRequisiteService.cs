using ADMISION.ENTITIES.Data;
using ADMISION.ENTITIES.Models.Requirement;
using ADMISION.Models.Shared;
using ADMISION.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ADMISION.Services.Implementations
{
    public class ModalityRequisiteService : IModalityRequisiteService
    {
        private readonly AppDbContext _context;

        public ModalityRequisiteService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<PagedResult<ModalityRequisiteListItem>> ListAsync(ModalityRequisiteListQuery query, CancellationToken ct = default)
        {
            var q = _context.ModalityRequisites
                .AsNoTracking()
                .Include(m => m.Modality)
                .Include(m => m.TypeModality)
                .Include(m => m.FileRequirementManagement)
                .AsQueryable();

            if (query.TermId.HasValue && query.TermId.Value != Guid.Empty)
                q = q.Where(m => m.Modality!.TermId == query.TermId.Value);
            if (query.ModalityId.HasValue && query.ModalityId.Value != Guid.Empty)
                q = q.Where(m => m.ModalityId == query.ModalityId.Value);
            if (query.TypeModalityId.HasValue && query.TypeModalityId.Value != Guid.Empty)
                q = q.Where(m => m.TypeModalityId == query.TypeModalityId.Value);

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var search = query.Search.Trim();
                q = q.Where(t => EF.Functions.ILike(t.FileRequirementManagement!.Name, $"%{search}%"));
            }

            // Ordenar sobre la entidad ANTES de proyectar — EF Core 10 no traduce
            // OrderBy sobre records posicionales (positional record constructor).
            q = (query.SortBy?.ToLowerInvariant()) switch
            {
                "modality" => query.IsDescending ? q.OrderByDescending(t => t.Modality!.Name) : q.OrderBy(t => t.Modality!.Name),
                "type" => query.IsDescending ? q.OrderByDescending(t => t.TypeModality!.Name) : q.OrderBy(t => t.TypeModality!.Name),
                "requirement" => query.IsDescending ? q.OrderByDescending(t => t.FileRequirementManagement!.Name) : q.OrderBy(t => t.FileRequirementManagement!.Name),
                _ => q.OrderByDescending(t => t.Modality!.Name)
            };

            var projected = q.Select(t => new ModalityRequisiteListItem(
                t.Id,
                t.Modality != null ? t.Modality.Name : null,
                t.TypeModality != null ? t.TypeModality.Name : null,
                t.FileRequirementManagement != null ? t.FileRequirementManagement.Name : null));

            return await PagedResult<ModalityRequisiteListItem>.CreateAsync(projected, query.Page, query.PageSize, ct);
        }

        public async Task<ModalityRequisiteCreateOutcome> CreateAsync(ModalityRequisite entity, string actor, CancellationToken ct = default)
        {
            var exists = await _context.ModalityRequisites.AnyAsync(mr =>
                mr.ModalityId == entity.ModalityId &&
                mr.TypeModalityId == entity.TypeModalityId &&
                mr.FileRequirementManagementId == entity.FileRequirementManagementId, ct);

            if (exists) return ModalityRequisiteCreateOutcome.Duplicate();

            entity.Id = Guid.NewGuid();
            entity.CreatedAt = DateTimeOffset.UtcNow;
            entity.CreatedBy = actor;
            _context.ModalityRequisites.Add(entity);
            await _context.SaveChangesAsync(ct);
            return ModalityRequisiteCreateOutcome.Ok(entity.ModalityId, entity.TypeModalityId);
        }

        public async Task<ModalityRequisiteDeleteResult> DeleteAsync(Guid id, CancellationToken ct = default)
        {
            var item = await _context.ModalityRequisites.FindAsync(new object[] { id }, ct);
            if (item == null) return new ModalityRequisiteDeleteResult { Outcome = DeleteOutcome.NotFound };

            var modalityId = item.ModalityId;
            var typeModalityId = item.TypeModalityId;

            try
            {
                _context.ModalityRequisites.Remove(item);
                await _context.SaveChangesAsync(ct);
                return new ModalityRequisiteDeleteResult
                {
                    Outcome = DeleteOutcome.Deleted,
                    ModalityId = modalityId,
                    TypeModalityId = typeModalityId
                };
            }
            catch (DbUpdateException)
            {
                return new ModalityRequisiteDeleteResult
                {
                    Outcome = DeleteOutcome.HasDependencies,
                    ModalityId = modalityId,
                    TypeModalityId = typeModalityId
                };
            }
        }

        public async Task<IReadOnlyList<AssignmentGridItem>> BuildAssignmentGridAsync(Guid termId, Guid requirementId, CancellationToken ct = default)
        {
            if (termId == Guid.Empty || requirementId == Guid.Empty)
                return Array.Empty<AssignmentGridItem>();

            // Modalidades del periodo
            var modalities = await _context.Modalities
                .AsNoTracking()
                .Where(m => m.TermId == termId)
                .OrderBy(m => m.Name)
                .Select(m => new { m.Id, m.Name })
                .ToListAsync(ct);

            if (modalities.Count == 0) return Array.Empty<AssignmentGridItem>();

            var modalityIds = modalities.Select(m => m.Id).ToList();

            // Tipos asociados a estas modalidades
            var types = await _context.TypeModalities
                .AsNoTracking()
                .Where(t => t.IsActive && modalityIds.Contains(t.ModalityId))
                .OrderBy(t => t.Name)
                .Select(t => new { t.Id, t.Name, t.ModalityId })
                .ToListAsync(ct);

            // Asignaciones existentes del requisito sobre estas modalidades
            var existing = await _context.ModalityRequisites
                .AsNoTracking()
                .Where(mr => mr.FileRequirementManagementId == requirementId
                          && modalityIds.Contains(mr.ModalityId))
                .Select(mr => new { mr.ModalityId, mr.TypeModalityId })
                .ToListAsync(ct);

            var assignedNoType = new HashSet<Guid>(existing.Where(e => e.TypeModalityId == null).Select(e => e.ModalityId));
            var assignedWithType = new HashSet<Guid>(existing.Where(e => e.TypeModalityId.HasValue).Select(e => e.TypeModalityId!.Value));

            var typesByModality = types.GroupBy(t => t.ModalityId).ToDictionary(g => g.Key, g => g.ToList());

            return modalities.Select(m =>
            {
                var modalityTypes = typesByModality.TryGetValue(m.Id, out var list) ? list : new();
                var typeItems = modalityTypes
                    .Select(t => new AssignmentGridTypeItem(t.Id, t.Name, assignedWithType.Contains(t.Id)))
                    .ToList();

                return new AssignmentGridItem(
                    m.Id,
                    m.Name,
                    assignedNoType.Contains(m.Id),
                    typeItems);
            }).ToList();
        }

        public async Task<BulkAssignmentResult> CreateBulkAsync(Guid requirementId, IReadOnlyList<BulkAssignmentSelection> selections, string actor, CancellationToken ct = default)
        {
            if (requirementId == Guid.Empty || selections == null || selections.Count == 0)
                return new BulkAssignmentResult(0, 0);

            var distinct = selections
                .Where(s => s.ModalityId != Guid.Empty)
                .Select(s => new { s.ModalityId, s.TypeModalityId })
                .Distinct()
                .ToList();
            if (distinct.Count == 0) return new BulkAssignmentResult(0, 0);

            var modalityIds = distinct.Select(s => s.ModalityId).Distinct().ToList();

            // Asignaciones ya existentes para evitar duplicados
            var existing = await _context.ModalityRequisites
                .AsNoTracking()
                .Where(mr => mr.FileRequirementManagementId == requirementId
                          && modalityIds.Contains(mr.ModalityId))
                .Select(mr => new { mr.ModalityId, mr.TypeModalityId })
                .ToListAsync(ct);
            var existingSet = new HashSet<(Guid, Guid?)>(existing.Select(e => (e.ModalityId, e.TypeModalityId)));

            int created = 0, skipped = 0;
            var now = DateTimeOffset.UtcNow;

            foreach (var sel in distinct)
            {
                if (existingSet.Contains((sel.ModalityId, sel.TypeModalityId)))
                {
                    skipped++;
                    continue;
                }
                _context.ModalityRequisites.Add(new ModalityRequisite
                {
                    Id = Guid.NewGuid(),
                    ModalityId = sel.ModalityId,
                    TypeModalityId = sel.TypeModalityId,
                    FileRequirementManagementId = requirementId,
                    CreatedAt = now,
                    CreatedBy = actor
                });
                created++;
            }

            if (created > 0) await _context.SaveChangesAsync(ct);
            return new BulkAssignmentResult(created, skipped);
        }
    }
}
