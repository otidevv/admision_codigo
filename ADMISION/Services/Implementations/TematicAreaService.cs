using ADMISION.ENTITIES.Data;
using ADMISION.ENTITIES.Models.Modality;
using ADMISION.Models.Shared;
using ADMISION.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ADMISION.Services.Implementations
{
    public class TematicAreaService : ITematicAreaService
    {
        private readonly AppDbContext _context;

        public TematicAreaService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<PagedResult<TematicAreaListItem>> ListAsync(ListQuery query, CancellationToken ct = default)
        {
            var q = _context.TematicAreas.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var search = query.Search.Trim();
                q = q.Where(t => EF.Functions.ILike(t.Code, $"%{search}%"));
            }

            // Ordenar sobre la entidad ANTES de proyectar — EF Core 10 no traduce
            // OrderBy sobre records posicionales (positional record constructor).
            q = (query.SortBy?.ToLowerInvariant()) switch
            {
                "code" => query.IsDescending ? q.OrderByDescending(t => t.Code) : q.OrderBy(t => t.Code),
                "createdat" => query.IsDescending ? q.OrderByDescending(t => t.CreatedAt) : q.OrderBy(t => t.CreatedAt),
                _ => q.OrderBy(t => t.Code)
            };

            var projected = q.Select(t => new TematicAreaListItem(t.Id, t.Code, t.CreatedAt));

            return await PagedResult<TematicAreaListItem>.CreateAsync(projected, query.Page, query.PageSize, ct);
        }

        public Task<TematicArea?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            return _context.TematicAreas.FirstOrDefaultAsync(a => a.Id == id, ct);
        }

        public async Task<TematicArea> CreateAsync(TematicArea area, string actor, CancellationToken ct = default)
        {
            area.Id = Guid.NewGuid();
            area.CreatedAt = DateTimeOffset.UtcNow;
            area.CreatedBy = actor;
            _context.TematicAreas.Add(area);
            await _context.SaveChangesAsync(ct);
            return area;
        }

        public async Task<bool> UpdateAsync(TematicArea area, string actor, CancellationToken ct = default)
        {
            var existing = await _context.TematicAreas.AsNoTracking().FirstOrDefaultAsync(a => a.Id == area.Id, ct);
            if (existing == null) return false;

            area.CreatedAt = existing.CreatedAt;
            area.CreatedBy = existing.CreatedBy;
            area.UpdatedAt = DateTimeOffset.UtcNow;
            area.UpdatedBy = actor;
            _context.TematicAreas.Update(area);
            await _context.SaveChangesAsync(ct);
            return true;
        }

        public async Task<DeleteOutcome> DeleteAsync(Guid id, CancellationToken ct = default)
        {
            var area = await _context.TematicAreas.FindAsync(new object[] { id }, ct);
            if (area == null) return DeleteOutcome.NotFound;

            // Bloquea borrado si tiene asignaciones (carreras vinculadas).
            if (await _context.TematicAreaCareers.AnyAsync(tac => tac.TematicAreaId == id, ct))
            {
                return DeleteOutcome.HasDependencies;
            }

            try
            {
                _context.TematicAreas.Remove(area);
                await _context.SaveChangesAsync(ct);
                return DeleteOutcome.Deleted;
            }
            catch (DbUpdateException)
            {
                return DeleteOutcome.HasDependencies;
            }
        }

        public async Task<IReadOnlyList<TematicArea>> GetAllAsync(CancellationToken ct = default)
        {
            return await _context.TematicAreas
                .AsNoTracking()
                .OrderBy(a => a.Code)
                .ToListAsync(ct);
        }

        public async Task<IReadOnlyList<CareerWithTematicAreas>> GetMatrixAsync(Guid termId, CancellationToken ct = default)
        {
            if (termId == Guid.Empty) return Array.Empty<CareerWithTematicAreas>();

            var careers = await _context.Careers
                .AsNoTracking()
                .OrderBy(c => c.Name)
                .Select(c => new { c.Id, c.Name })
                .ToListAsync(ct);

            var assignments = await _context.TematicAreaCareers
                .AsNoTracking()
                .Where(t => t.TermId == termId)
                .Select(t => new { t.CareerId, t.TematicAreaId })
                .ToListAsync(ct);

            return careers.Select(c =>
            {
                var ids = assignments.Where(a => a.CareerId == c.Id).Select(a => a.TematicAreaId).ToList();
                return new CareerWithTematicAreas(c.Id, c.Name, ids);
            }).ToList();
        }

        public async Task SaveMatrixAsync(Guid termId, IList<CareerTematicAreaAssignment> assignments, string actor, CancellationToken ct = default)
        {
            if (termId == Guid.Empty || assignments == null || assignments.Count == 0) return;

            var existing = await _context.TematicAreaCareers
                .Where(t => t.TermId == termId)
                .ToListAsync(ct);

            foreach (var careerData in assignments)
            {
                if (careerData.CareerId == Guid.Empty) continue;
                var incomingAreaIds = careerData.TematicAreaIds ?? new List<Guid>();
                var currentAssignments = existing.Where(e => e.CareerId == careerData.CareerId).ToList();

                // Quitar las que ya no vienen.
                var toRemove = currentAssignments.Where(a => !incomingAreaIds.Contains(a.TematicAreaId)).ToList();
                _context.TematicAreaCareers.RemoveRange(toRemove);

                // Añadir las nuevas.
                var existingAreaIds = currentAssignments.Select(a => a.TematicAreaId).ToList();
                foreach (var areaId in incomingAreaIds)
                {
                    if (areaId == Guid.Empty || existingAreaIds.Contains(areaId)) continue;
                    _context.TematicAreaCareers.Add(new TematicAreaCareer
                    {
                        Id = Guid.NewGuid(),
                        TermId = termId,
                        CareerId = careerData.CareerId,
                        TematicAreaId = areaId,
                        CreatedAt = DateTimeOffset.UtcNow,
                        CreatedBy = actor
                    });
                }
            }

            await _context.SaveChangesAsync(ct);
        }

        public async Task SaveCareerAssignmentsAsync(Guid termId, Guid careerId, IList<Guid> selectedAreaIds, string actor, CancellationToken ct = default)
        {
            if (termId == Guid.Empty || careerId == Guid.Empty) return;
            selectedAreaIds ??= new List<Guid>();

            var current = await _context.TematicAreaCareers
                .Where(tac => tac.TermId == termId && tac.CareerId == careerId)
                .ToListAsync(ct);

            // Quitar deseleccionadas.
            var toRemove = current.Where(tac => !selectedAreaIds.Contains(tac.TematicAreaId)).ToList();
            _context.TematicAreaCareers.RemoveRange(toRemove);

            // Añadir las nuevas seleccionadas.
            var existingIds = current.Select(tac => tac.TematicAreaId).ToList();
            foreach (var selectedId in selectedAreaIds.Where(id => !existingIds.Contains(id)))
            {
                _context.TematicAreaCareers.Add(new TematicAreaCareer
                {
                    Id = Guid.NewGuid(),
                    TermId = termId,
                    CareerId = careerId,
                    TematicAreaId = selectedId,
                    CreatedAt = DateTimeOffset.UtcNow,
                    CreatedBy = actor
                });
            }

            await _context.SaveChangesAsync(ct);
        }
    }
}
