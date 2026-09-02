using ADMISION.ENTITIES.Constants;
using ADMISION.ENTITIES.Data;
using ADMISION.Models.ViewModels.Reports;
using ADMISION.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ADMISION.Services.Implementations
{
    public class TematicAreaReportService : ITematicAreaReportService
    {
        private readonly AppDbContext _context;

        public TematicAreaReportService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<TematicAreaReportViewModel> BuildAsync(TematicAreaReportFilter filter, CancellationToken ct = default)
        {
            var vm = new TematicAreaReportViewModel
            {
                TermId = filter.TermId,
                ModalityId = filter.ModalityId,
                TypeModalityId = filter.TypeModalityId,
                TypePostulantId = filter.TypePostulantId
            };

            // Resolver nombres legibles para el header del reporte.
            if (filter.TermId.HasValue)
                vm.TermName = (await _context.Terms.AsNoTracking().FirstOrDefaultAsync(t => t.Id == filter.TermId.Value, ct))?.Name;
            if (filter.ModalityId.HasValue)
                vm.ModalityName = (await _context.Modalities.AsNoTracking().FirstOrDefaultAsync(m => m.Id == filter.ModalityId.Value, ct))?.Name;
            if (filter.TypeModalityId.HasValue)
                vm.TypeModalityName = (await _context.TypeModalities.AsNoTracking().FirstOrDefaultAsync(t => t.Id == filter.TypeModalityId.Value, ct))?.Name;
            if (filter.TypePostulantId.HasValue)
                vm.TypePostulantName = (await _context.TypePostulantInscriptions.AsNoTracking().FirstOrDefaultAsync(t => t.Id == filter.TypePostulantId.Value, ct))?.Name;

            // Sin término no hay datos que agregar.
            if (!filter.TermId.HasValue) return vm;

            var query = _context.Inscriptions.AsNoTracking()
                .Include(i => i.Career)
                .Include(i => i.Modality)
                .Where(i => i.State == AppConstants.InscripcionState.Aprobado);

            query = query.Where(i => i.Modality != null && i.Modality.TermId == filter.TermId.Value);
            if (filter.ModalityId.HasValue) query = query.Where(i => i.ModalityId == filter.ModalityId.Value);
            if (filter.TypeModalityId.HasValue) query = query.Where(i => i.TypeModalityId == filter.TypeModalityId.Value);
            if (filter.TypePostulantId.HasValue) query = query.Where(i => i.TypePostulantInscriptionId == filter.TypePostulantId.Value);

            var inscriptions = await query.ToListAsync(ct);

            // Mapping carrera→área válido para el término seleccionado.
            var areaByCareer = await _context.TematicAreaCareers.AsNoTracking()
                .Where(tac => tac.TermId == filter.TermId.Value)
                .ToDictionaryAsync(tac => tac.CareerId, tac => tac.TematicAreaId, ct);

            var areas = await _context.TematicAreas.AsNoTracking()
                .ToDictionaryAsync(a => a.Id, a => a.Code, ct);

            var careerCounts = inscriptions
                .GroupBy(i => i.CareerId)
                .Select(g => new
                {
                    CareerId = g.Key,
                    CareerCode = g.First().Career?.Code ?? "",
                    CareerName = g.First().Career?.Name ?? "",
                    Count = g.Count(),
                    AreaId = areaByCareer.TryGetValue(g.Key, out var aid) ? (Guid?)aid : null
                })
                .ToList();

            vm.Areas = careerCounts
                .GroupBy(x => x.AreaId)
                .Select(g => new TematicAreaReportItem
                {
                    TematicAreaId = g.Key,
                    AreaCode = g.Key.HasValue && areas.TryGetValue(g.Key.Value, out var code) ? code : "SIN ÁREA",
                    Subtotal = g.Sum(x => x.Count),
                    Careers = g.OrderBy(x => x.CareerName)
                        .Select(x => new CareerReportItem
                        {
                            CareerId = x.CareerId,
                            CareerCode = x.CareerCode,
                            CareerName = x.CareerName,
                            Inscritos = x.Count
                        }).ToList()
                })
                .OrderBy(a => a.AreaCode)
                .ToList();

            vm.TotalInscripciones = inscriptions.Count;
            return vm;
        }
    }
}
