using ADMISION.ENTITIES.Constants;
using ADMISION.ENTITIES.Data;
using ADMISION.Models.ViewModels.Reports;
using ADMISION.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ADMISION.Services.Implementations
{
    public class EconomicReportService : IEconomicReportService
    {
        private readonly AppDbContext _context;

        public EconomicReportService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<EconomicReportViewModel> BuildAsync(EconomicReportFilter filter, CancellationToken ct = default)
        {
            var vm = new EconomicReportViewModel
            {
                TermId = filter.TermId,
                ModalityId = filter.ModalityId,
                TypeModalityId = filter.TypeModalityId,
                TypePostulantId = filter.TypePostulantId,
                Page = filter.Page,
                PageSize = filter.PageSize
            };

            if (filter.TermId.HasValue)
                vm.TermName = (await _context.Terms.AsNoTracking().FirstOrDefaultAsync(t => t.Id == filter.TermId.Value, ct))?.Name;
            if (filter.ModalityId.HasValue)
                vm.ModalityName = (await _context.Modalities.AsNoTracking().FirstOrDefaultAsync(m => m.Id == filter.ModalityId.Value, ct))?.Name;
            if (filter.TypeModalityId.HasValue)
                vm.TypeModalityName = (await _context.TypeModalities.AsNoTracking().FirstOrDefaultAsync(t => t.Id == filter.TypeModalityId.Value, ct))?.Name;
            if (filter.TypePostulantId.HasValue)
                vm.TypePostulantName = (await _context.TypePostulantInscriptions.AsNoTracking().FirstOrDefaultAsync(t => t.Id == filter.TypePostulantId.Value, ct))?.Name;

            if (!filter.TermId.HasValue) return vm;

            var baseQuery = BuildBaseQuery(filter);

            var inscriptionIds = await baseQuery.Select(i => i.Id).ToListAsync(ct);
            vm.TotalRecords = inscriptionIds.Count;

            var payments = await BuildPaymentsSummaryAsync(inscriptionIds, ct);

            var paidCount = payments.Count(kv => kv.Value > 0);
            vm.ConPago = paidCount;
            vm.SinPago = inscriptionIds.Count - paidCount;
            vm.TotalMonto = payments.Values.Where(v => v > 0).Sum();

            var skip = (filter.Page - 1) * filter.PageSize;
            var items = await baseQuery
                .OrderBy(i => i.CodePostulant)
                .Skip(skip)
                .Take(filter.PageSize)
                .ToListAsync(ct);

            vm.Items = await MapItemsAsync(items, payments, ct);

            return vm;
        }

        public async Task<List<EconomicReportItem>> BuildAllAsync(EconomicReportFilter filter, CancellationToken ct = default)
        {
            if (!filter.TermId.HasValue) return new List<EconomicReportItem>();

            var items = await BuildBaseQuery(filter)
                .OrderBy(i => i.CodePostulant)
                .ToListAsync(ct);

            var inscriptionIds = items.Select(i => i.Id).Distinct().ToList();
            var payments = await BuildPaymentsSummaryAsync(inscriptionIds, ct);

            return await MapItemsAsync(items, payments, ct);
        }

        private async Task<Dictionary<Guid, decimal>> BuildPaymentsSummaryAsync(List<Guid> inscriptionIds, CancellationToken ct)
        {
            if (inscriptionIds.Count == 0) return new Dictionary<Guid, decimal>();

            return await _context.Payments.AsNoTracking()
                .Where(p => p.IsApproved && inscriptionIds.Contains(p.InscriptionId))
                .GroupBy(p => p.InscriptionId)
                .Select(g => new { InscriptionId = g.Key, Total = g.Sum(p => p.Amount) })
                .ToDictionaryAsync(g => g.InscriptionId, g => g.Total, ct);
        }

        private IQueryable<ENTITIES.Models.Postulante.Inscription> BuildBaseQuery(EconomicReportFilter filter)
        {
            var query = _context.Inscriptions.AsNoTracking()
                .Include(i => i.Modality).ThenInclude(m => m!.Term)
                .Include(i => i.TypeModality)
                .Where(i => i.State == AppConstants.InscripcionState.Aprobado)
                .Where(i => i.Modality != null && i.Modality.TermId == filter.TermId!.Value);

            if (filter.ModalityId.HasValue)
                query = query.Where(i => i.ModalityId == filter.ModalityId.Value);
            if (filter.TypeModalityId.HasValue)
                query = query.Where(i => i.TypeModalityId == filter.TypeModalityId.Value);
            if (filter.TypePostulantId.HasValue)
                query = query.Where(i => i.TypePostulantInscriptionId == filter.TypePostulantId.Value);

            return query;
        }

        private async Task<List<EconomicReportItem>> MapItemsAsync(
            List<ENTITIES.Models.Postulante.Inscription> inscriptions,
            Dictionary<Guid, decimal> payments,
            CancellationToken ct)
        {
            var postulantIds = inscriptions.Select(i => i.PostulantId).Distinct().ToList();

            var postulants = await _context.Postulants.AsNoTracking()
                .Include(p => p.User)
                .Where(p => postulantIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, ct);

            var typePostulantIds = inscriptions
                .Where(i => i.TypePostulantInscriptionId.HasValue)
                .Select(i => i.TypePostulantInscriptionId!.Value)
                .Distinct().ToList();

            var typePostulants = await _context.TypePostulantInscriptions.AsNoTracking()
                .Where(t => typePostulantIds.Contains(t.Id))
                .ToDictionaryAsync(t => t.Id, ct);

            var typeModalityIds = inscriptions
                .Where(i => i.TypeModalityId.HasValue)
                .Select(i => i.TypeModalityId!.Value)
                .Distinct().ToList();

            var typeModalities = await _context.TypeModalities.AsNoTracking()
                .Where(t => typeModalityIds.Contains(t.Id))
                .ToDictionaryAsync(t => t.Id, t => new { t.Name, t.DiscountPercentage });

            var result = new List<EconomicReportItem>(inscriptions.Count);

            foreach (var i in inscriptions)
            {
                var postulant = postulants.TryGetValue(i.PostulantId, out var p) ? p : null;
                var user = postulant?.User;

                var ciclo = i.Modality?.Term?.Name ?? "—";

                var examen = "—";
                decimal examenDiscount = 0;
                if (i.TypeModalityId.HasValue && typeModalities.TryGetValue(i.TypeModalityId.Value, out var tm))
                {
                    examen = tm.Name;
                    examenDiscount = tm.DiscountPercentage;
                }

                var modalidad = i.Modality?.Name ?? "—";

                var tipoPostulante = "—";
                decimal postulanteDiscount = 0;
                if (i.TypePostulantInscriptionId.HasValue && typePostulants.TryGetValue(i.TypePostulantInscriptionId.Value, out var tp))
                {
                    tipoPostulante = tp.Name;
                    postulanteDiscount = tp.DiscountPercentage;
                }

                var effectiveDiscount = Math.Max(examenDiscount, postulanteDiscount);
                var descuento = effectiveDiscount > 0 ? $"{effectiveDiscount}%" : "0%";

                var monto = payments.TryGetValue(i.Id, out var amount) && amount > 0
                    ? $"S/ {amount:N2}"
                    : "—";

                result.Add(new EconomicReportItem
                {
                    Ciclo = ciclo,
                    Codigo = i.CodePostulant ?? "—",
                    Dni = user?.Document ?? "—",
                    ApellidoPaterno = user?.FirstNameFather ?? "—",
                    ApellidoMaterno = user?.FirstNameMother ?? "—",
                    Nombres = user?.Name ?? "—",
                    Examen = examen,
                    Modalidad = modalidad,
                    TipoPostulante = tipoPostulante,
                    Descuento = descuento,
                    Monto = monto
                });
            }

            return result;
        }
    }
}
