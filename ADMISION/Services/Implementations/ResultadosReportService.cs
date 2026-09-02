using ADMISION.ENTITIES.Models.Exam;
using ADMISION.Models.ViewModels.Reports;
using ADMISION.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ADMISION.Services.Implementations
{
    public class ResultadosReportService : IResultadosReportService
    {
        private readonly ADMISION.ENTITIES.Data.AppDbContext _context;

        public ResultadosReportService(ADMISION.ENTITIES.Data.AppDbContext context)
        {
            _context = context;
        }

        public async Task<ResultadosFilterOptions> GetFilterOptionsAsync(Guid termId, CancellationToken ct = default)
        {
            var baseQ = _context.AdmissionResultImportRecords.AsNoTracking()
                .Where(r => r.TermId == termId);

            return new ResultadosFilterOptions
            {
                Condiciones = await baseQ.Where(r => r.Condicion != null).Select(r => r.Condicion!).Distinct().OrderBy(x => x).ToListAsync(ct)
            };
        }

        public async Task<ResultadosReportViewModel> BuildAsync(ResultadosReportFilter filter, CancellationToken ct = default)
        {
            var (vm, query) = await BuildContextAsync(filter, ct);
            if (query == null) return vm;

            vm.TotalRecords = await query.CountAsync(ct);
            vm.PageSize = Math.Max(1, filter.PageSize);
            vm.Page = Math.Clamp(filter.Page, 1, Math.Max(1, (int)Math.Ceiling((double)vm.TotalRecords / vm.PageSize)));

            var records = await query
                .OrderBy(r => r.ApellidosNombres)
                .ThenBy(r => r.Nro)
                .Skip((vm.Page - 1) * vm.PageSize)
                .Take(vm.PageSize)
                .ToListAsync(ct);

            vm.Items = MapRecords(records);
            return vm;
        }

        public async Task<ResultadosReportViewModel> BuildAllAsync(ResultadosReportFilter filter, CancellationToken ct = default)
        {
            var (vm, query) = await BuildContextAsync(filter, ct);
            if (query == null) return vm;

            var records = await query
                .OrderBy(r => r.ApellidosNombres)
                .ThenBy(r => r.Nro)
                .ToListAsync(ct);

            vm.Items = MapRecords(records);
            vm.TotalRecords = records.Count;
            return vm;
        }

        private async Task<(ResultadosReportViewModel vm, IQueryable<AdmissionResultImportRecord>? query)> BuildContextAsync(
            ResultadosReportFilter filter,
            CancellationToken ct)
        {
            var vm = new ResultadosReportViewModel
            {
                TermId = filter.TermId,
                ModalityId = filter.ModalityId,
                TypeModalityId = filter.TypeModalityId,
                TypePostulantId = filter.TypePostulantId,
                CareerId = filter.CareerId,
                Condicion = filter.Condicion
            };

            if (filter.TermId.HasValue)
                vm.TermName = await _context.Terms.AsNoTracking()
                    .Where(t => t.Id == filter.TermId.Value)
                    .Select(t => t.Name)
                    .FirstOrDefaultAsync(ct);

            if (filter.ModalityId.HasValue)
                vm.ModalityName = await _context.Modalities.AsNoTracking()
                    .Where(m => m.Id == filter.ModalityId.Value)
                    .Select(m => m.Name)
                    .FirstOrDefaultAsync(ct);

            if (filter.TypeModalityId.HasValue)
                vm.TypeModalityName = await _context.TypeModalities.AsNoTracking()
                    .Where(t => t.Id == filter.TypeModalityId.Value)
                    .Select(t => t.Name)
                    .FirstOrDefaultAsync(ct);

            if (filter.TypePostulantId.HasValue)
                vm.TypePostulantName = await _context.TypePostulantInscriptions.AsNoTracking()
                    .Where(t => t.Id == filter.TypePostulantId.Value)
                    .Select(t => t.Name)
                    .FirstOrDefaultAsync(ct);

            if (filter.CareerId.HasValue)
                vm.CareerName = await _context.Careers.AsNoTracking()
                    .Where(c => c.Id == filter.CareerId.Value)
                    .Select(c => c.Name)
                    .FirstOrDefaultAsync(ct);

            if (!filter.TermId.HasValue) return (vm, null);

            var query = _context.AdmissionResultImportRecords
                .AsNoTracking()
                .Include(r => r.Inscription).ThenInclude(i => i!.Modality)
                .Include(r => r.Inscription).ThenInclude(i => i!.TypeModality)
                .Include(r => r.Inscription).ThenInclude(i => i!.TypePostulantInscription)
                .Include(r => r.Inscription).ThenInclude(i => i!.Career)
                .Where(r => r.TermId == filter.TermId.Value);

            if (filter.ModalityId.HasValue) query = query.Where(r => r.Inscription != null && r.Inscription.ModalityId == filter.ModalityId.Value);
            if (filter.TypeModalityId.HasValue) query = query.Where(r => r.Inscription != null && r.Inscription.TypeModalityId == filter.TypeModalityId.Value);
            if (filter.TypePostulantId.HasValue) query = query.Where(r => r.Inscription != null && r.Inscription.TypePostulantInscriptionId == filter.TypePostulantId.Value);
            if (filter.CareerId.HasValue) query = query.Where(r => r.Inscription != null && r.Inscription.CareerId == filter.CareerId.Value);
            if (!string.IsNullOrWhiteSpace(filter.Condicion)) query = query.Where(r => r.Condicion == filter.Condicion);

            return (vm, query);
        }

        private static List<ResultadosReportItem> MapRecords(List<AdmissionResultImportRecord> records)
        {
            return records.Select(r => new ResultadosReportItem
            {
                Nro = r.Nro,
                Codigo = r.Codigo ?? string.Empty,
                ApellidosNombres = r.ApellidosNombres ?? string.Empty,
                Examen = r.Inscription?.Modality?.Name ?? string.Empty,
                TipoModalidad = r.Inscription?.TypeModality?.Name ?? string.Empty,
                TipoPostulante = r.Inscription?.TypePostulantInscription?.Name ?? string.Empty,
                Carrera = r.Inscription?.Career?.Name ?? r.CarreraProfesional ?? string.Empty,
                Grupo = r.Grupo ?? string.Empty,
                Correctas = r.Correctas ?? string.Empty,
                Blancas = r.Blancas ?? string.Empty,
                Puntaje = r.Puntaje ?? string.Empty,
                Nota = r.Nota ?? string.Empty,
                Condicion = r.Condicion ?? string.Empty
            }).ToList();
        }
    }
}
