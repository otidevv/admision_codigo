using ADMISION.ENTITIES.Data;
using ADMISION.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ADMISION.Services.Implementations
{
    public class CatalogService : ICatalogService
    {
        private readonly AppDbContext _context;

        public CatalogService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IReadOnlyList<CatalogOption>> GetTermsAsync(bool onlyActive = false, CancellationToken ct = default)
        {
            var q = _context.Terms.AsNoTracking().AsQueryable();
            if (onlyActive) q = q.Where(t => t.IsActive);
            return await q
                .OrderByDescending(t => t.Year).ThenByDescending(t => t.Number)
                .Select(t => new CatalogOption(t.Id, t.Name))
                .ToListAsync(ct);
        }

        public async Task<IReadOnlyList<CatalogOption>> GetFacultiesAsync(CancellationToken ct = default)
        {
            return await _context.Faculties.AsNoTracking()
                .OrderBy(f => f.Name)
                .Select(f => new CatalogOption(f.Id, f.Name))
                .ToListAsync(ct);
        }

        public async Task<IReadOnlyList<CatalogOption>> GetCareersAsync(Guid? facultyId = null, bool onlyActive = false, CancellationToken ct = default)
        {
            var q = _context.Careers.AsNoTracking().AsQueryable();
            if (facultyId.HasValue && facultyId.Value != Guid.Empty)
            {
                q = q.Where(c => c.FacultyId == facultyId.Value);
            }
            if (onlyActive) q = q.Where(c => c.IsActive);

            return await q
                .OrderBy(c => c.Name)
                .Select(c => new CatalogOption(c.Id, c.Name))
                .ToListAsync(ct);
        }

        public async Task<IReadOnlyList<CatalogOption>> GetModalitiesAsync(Guid? termId = null, bool onlyActive = false, CancellationToken ct = default)
        {
            var q = _context.Modalities.AsNoTracking().AsQueryable();
            if (termId.HasValue && termId.Value != Guid.Empty)
            {
                q = q.Where(m => m.TermId == termId.Value);
            }
            if (onlyActive) q = q.Where(m => m.IsActive);

            return await q
                .OrderBy(m => m.DisplayOrder).ThenBy(m => m.Name)
                .Select(m => new CatalogOption(m.Id, m.Name))
                .ToListAsync(ct);
        }

        public async Task<IReadOnlyList<TypeModalityOption>> GetTypeModalitiesAsync(Guid modalityId, bool onlyActive = true, CancellationToken ct = default)
        {
            var q = _context.TypeModalities.AsNoTracking()
                .Where(t => t.ModalityId == modalityId);
            if (onlyActive) q = q.Where(t => t.IsActive);

            return await q
                .OrderBy(t => t.Name)
                .Select(t => new TypeModalityOption(t.Id, t.Name, t.DiscountPercentage, t.ModalityId))
                .ToListAsync(ct);
        }

        public async Task<IReadOnlyList<TypeModalityOption>> GetAllTypeModalitiesAsync(bool onlyActive = true, CancellationToken ct = default)
        {
            var q = _context.TypeModalities.AsNoTracking().AsQueryable();
            if (onlyActive) q = q.Where(t => t.IsActive);

            return await q
                .OrderBy(t => t.Name)
                .Select(t => new TypeModalityOption(t.Id, t.Name, t.DiscountPercentage, t.ModalityId))
                .ToListAsync(ct);
        }

        public async Task<IReadOnlyList<CatalogOption>> GetTypePostulantsAsync(CancellationToken ct = default)
        {
            return await _context.TypePostulantInscriptions.AsNoTracking()
                .OrderBy(t => t.Name)
                .Select(t => new CatalogOption(t.Id, t.Name))
                .ToListAsync(ct);
        }

        public async Task<IReadOnlyList<CatalogOption>> GetTematicAreasByTermAsync(Guid termId, CancellationToken ct = default)
        {
            // Áreas temáticas que tienen al menos una carrera asignada en el término dado.
            return await _context.TematicAreaCareers
                .AsNoTracking()
                .Where(tac => tac.TermId == termId && tac.TematicArea != null)
                .Select(tac => tac.TematicArea!)
                .Distinct()
                .OrderBy(a => a.Code)
                .Select(a => new CatalogOption(a.Id, a.Code))
                .ToListAsync(ct);
        }
    }
}
