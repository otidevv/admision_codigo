using System.Globalization;
using System.Text;
using ADMISION.ENTITIES.Models.Exam;
using ADMISION.Models.ViewModels.Reports;
using ADMISION.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ADMISION.Services.Implementations
{
    public class IngresantesReportService : IIngresantesReportService
    {
        private readonly ADMISION.ENTITIES.Data.AppDbContext _context;

        public IngresantesReportService(ADMISION.ENTITIES.Data.AppDbContext context)
        {
            _context = context;
        }

        public async Task<IngresantesReportViewModel> BuildAsync(IngresantesReportFilter filter, CancellationToken ct = default)
        {
            var vm = await BuildVmBaseAsync(filter, ct);
            if (!filter.TermId.HasValue) return vm;

            if (filter.TipoReporte == "preliminar")
            {
                var all = await BuildPreliminarItemsAsync(filter, ct);
                vm.TotalIngresantes = all.Count;
                vm.PageSize = Math.Max(1, filter.PageSize);
                vm.Page = Math.Clamp(filter.Page, 1, Math.Max(1, (int)Math.Ceiling((double)vm.TotalIngresantes / vm.PageSize)));
                vm.Items = all
                    .Skip((vm.Page - 1) * vm.PageSize)
                    .Take(vm.PageSize)
                    .ToList();
                return vm;
            }

            var (consolidadoVm, query) = await BuildConsolidadoContextAsync(filter, vm, ct);
            if (query == null) return consolidadoVm;

            consolidadoVm.TotalIngresantes = await query.CountAsync(ct);
            consolidadoVm.PageSize = Math.Max(1, filter.PageSize);
            consolidadoVm.Page = Math.Clamp(filter.Page, 1, Math.Max(1, (int)Math.Ceiling((double)consolidadoVm.TotalIngresantes / consolidadoVm.PageSize)));

            var records = await query
                .OrderBy(r => r.Inscription!.Career!.Name)
                .ThenBy(r => r.CodigoEstudiante)
                .Skip((consolidadoVm.Page - 1) * consolidadoVm.PageSize)
                .Take(consolidadoVm.PageSize)
                .ToListAsync(ct);

            consolidadoVm.Items = await MapRecordsAsync(records, filter.TermId!.Value, ct);
            return consolidadoVm;
        }

        public async Task<IngresantesReportViewModel> BuildAllAsync(IngresantesReportFilter filter, CancellationToken ct = default)
        {
            var vm = await BuildVmBaseAsync(filter, ct);
            if (!filter.TermId.HasValue) return vm;

            if (filter.TipoReporte == "preliminar")
            {
                var all = await BuildPreliminarItemsAsync(filter, ct);
                vm.Items = all;
                vm.TotalIngresantes = all.Count;
                return vm;
            }

            var (consolidadoVm, query) = await BuildConsolidadoContextAsync(filter, vm, ct);
            if (query == null) return consolidadoVm;

            var records = await query
                .OrderBy(r => r.Inscription!.Career!.Name)
                .ThenBy(r => r.CodigoEstudiante)
                .ToListAsync(ct);

            consolidadoVm.Items = await MapRecordsAsync(records, filter.TermId!.Value, ct);
            consolidadoVm.TotalIngresantes = records.Count;
            return consolidadoVm;
        }

        private async Task<IngresantesReportViewModel> BuildVmBaseAsync(IngresantesReportFilter filter, CancellationToken ct)
        {
            var vm = new IngresantesReportViewModel
            {
                TermId = filter.TermId,
                ModalityId = filter.ModalityId,
                TypeModalityId = filter.TypeModalityId,
                TypePostulantId = filter.TypePostulantId,
                CareerId = filter.CareerId,
                TematicAreaId = filter.TematicAreaId,
                SegundaCarrera = filter.SegundaCarrera,
                TipoReporte = string.IsNullOrWhiteSpace(filter.TipoReporte) ? "consolidado" : filter.TipoReporte!
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

            if (filter.TematicAreaId.HasValue)
                vm.TematicAreaName = await _context.TematicAreas.AsNoTracking()
                    .Where(a => a.Id == filter.TematicAreaId.Value)
                    .Select(a => a.Code)
                    .FirstOrDefaultAsync(ct);

            return vm;
        }

        private async Task<(IngresantesReportViewModel vm, IQueryable<ConsolidadoIngresantesRecord>? query)> BuildConsolidadoContextAsync(
            IngresantesReportFilter filter,
            IngresantesReportViewModel vm,
            CancellationToken ct)
        {
            var termId = filter.TermId!.Value;

            var latestVersion = await _context.ConsolidadoIngresantesVersions
                .AsNoTracking()
                .Where(v => v.TermId == termId && v.IsLatest)
                .OrderByDescending(v => v.VersionNumber)
                .FirstOrDefaultAsync(ct);

            if (latestVersion == null)
            {
                vm.Items = new List<IngresantesReportItem>();
                vm.TotalIngresantes = 0;
                return (vm, null);
            }

            var query = _context.ConsolidadoIngresantesRecords
                .AsNoTracking()
                .Include(r => r.Inscription).ThenInclude(i => i!.Modality)
                .Include(r => r.Inscription).ThenInclude(i => i!.Career)
                .Include(r => r.Inscription).ThenInclude(i => i!.TypeModality)
                .Include(r => r.Inscription).ThenInclude(i => i!.TypePostulantInscription)
                .Include(r => r.Inscription).ThenInclude(i => i!.Postulant).ThenInclude(p => p!.User)
                .Where(r => r.VersionId == latestVersion.Id);

            if (filter.ModalityId.HasValue) query = query.Where(r => r.Inscription != null && r.Inscription.ModalityId == filter.ModalityId.Value);
            if (filter.TypeModalityId.HasValue) query = query.Where(r => r.Inscription != null && r.Inscription.TypeModalityId == filter.TypeModalityId.Value);
            if (filter.TypePostulantId.HasValue) query = query.Where(r => r.Inscription != null && r.Inscription.TypePostulantInscriptionId == filter.TypePostulantId.Value);
            if (filter.CareerId.HasValue) query = query.Where(r => r.Inscription != null && r.Inscription.CareerId == filter.CareerId.Value);
            if (filter.SegundaCarrera == "1") query = query.Where(r => r.SegundaCarrera == "1");
            else if (filter.SegundaCarrera == "0") query = query.Where(r => r.SegundaCarrera != "1");

            if (filter.TematicAreaId.HasValue)
            {
                var careerIdsInArea = await _context.TematicAreaCareers.AsNoTracking()
                    .Where(tac => tac.TermId == termId && tac.TematicAreaId == filter.TematicAreaId.Value)
                    .Select(tac => tac.CareerId)
                    .ToListAsync(ct);

                query = query.Where(r => r.Inscription != null && careerIdsInArea.Contains(r.Inscription.CareerId));
            }

            return (vm, query);
        }

        private async Task<List<IngresantesReportItem>> BuildPreliminarItemsAsync(IngresantesReportFilter filter, CancellationToken ct)
        {
            var termId = filter.TermId!.Value;

            var areaByCareer = await _context.TematicAreaCareers.AsNoTracking()
                .Where(tac => tac.TermId == termId)
                .ToDictionaryAsync(tac => tac.CareerId, tac => tac.TematicAreaId, ct);

            var areaNames = await _context.TematicAreas.AsNoTracking()
                .ToDictionaryAsync(a => a.Id, a => a.Code, ct);

            var cepreModalityIds = await _context.Modalities.AsNoTracking()
                .Where(m => m.TermId == termId && m.IsCepreExam)
                .Select(m => m.Id)
                .ToListAsync(ct);

            HashSet<Guid>? careerIdsInArea = null;
            if (filter.TematicAreaId.HasValue)
            {
                careerIdsInArea = (await _context.TematicAreaCareers.AsNoTracking()
                    .Where(tac => tac.TermId == termId && tac.TematicAreaId == filter.TematicAreaId.Value)
                    .Select(tac => tac.CareerId)
                    .ToListAsync(ct)).ToHashSet();
            }

            var items = new List<IngresantesReportItem>();

            // ───────────────────────────────────────────────────────────
            // 1) Modalidades distintas a CEPRE con postulantes ingresantes
            // ───────────────────────────────────────────────────────────
            var onlyCepre = filter.ModalityId.HasValue && cepreModalityIds.Contains(filter.ModalityId.Value);

            if (!onlyCepre)
            {
                var resignedIds = await _context.Resignations.AsNoTracking()
                    .Where(r => r.Inscription != null && r.Inscription.Modality != null && r.Inscription.Modality.TermId == termId)
                    .Select(r => r.InscriptionId)
                    .ToListAsync(ct);

                var inscriptionQuery = _context.Inscriptions
                    .AsNoTracking()
                    .Include(i => i.Modality)
                    .Include(i => i.Career)
                    .Include(i => i.TypeModality)
                    .Include(i => i.TypePostulantInscription)
                    .Include(i => i.Postulant).ThenInclude(p => p!.User)
                    .Where(i => i.IsAdmission
                        && i.Modality != null
                        && i.Modality.TermId == termId
                        && !cepreModalityIds.Contains(i.Modality!.Id)
                        && !resignedIds.Contains(i.Id));

                if (filter.ModalityId.HasValue)
                    inscriptionQuery = inscriptionQuery.Where(i => i.ModalityId == filter.ModalityId.Value);
                if (filter.TypeModalityId.HasValue)
                    inscriptionQuery = inscriptionQuery.Where(i => i.TypeModalityId == filter.TypeModalityId.Value);
                if (filter.TypePostulantId.HasValue)
                    inscriptionQuery = inscriptionQuery.Where(i => i.TypePostulantInscriptionId == filter.TypePostulantId.Value);
                if (filter.CareerId.HasValue)
                    inscriptionQuery = inscriptionQuery.Where(i => i.CareerId == filter.CareerId.Value);
                if (filter.TematicAreaId.HasValue && careerIdsInArea != null)
                    inscriptionQuery = inscriptionQuery.Where(i => careerIdsInArea.Contains(i.CareerId));

                if (filter.SegundaCarrera == "1")
                    inscriptionQuery = inscriptionQuery.Where(_ => false);

                var inscriptions = await inscriptionQuery
                    .OrderBy(i => i.Career!.Name)
                    .ThenBy(i => i.CodePostulant)
                    .ToListAsync(ct);

                foreach (var i in inscriptions)
                {
                    var areaId = areaByCareer.TryGetValue(i.CareerId, out var aid) ? (Guid?)aid : null;

                    items.Add(new IngresantesReportItem
                    {
                        CodigoEstudiante = i.CodePostulant,
                        Examen = i.Modality?.Name ?? "",
                        TipoModalidad = i.TypeModality?.Name ?? "",
                        TipoPostulante = i.TypePostulantInscription?.Name ?? "",
                        Apellidos = $"{i.Postulant?.User?.FirstNameFather} {i.Postulant?.User?.FirstNameMother}".Trim(),
                        Nombres = i.Postulant?.User?.Name ?? "",
                        CarreraProfesional = i.Career?.Name ?? "",
                        Tema = areaId.HasValue && areaNames.TryGetValue(areaId.Value, out var code) ? code : "",
                        Nota = i.GradeAdmission,
                        IsAdmission = true,
                        SegundaCarrera = "0"
                    });
                }
            }

            // ───────────────────────────────────────────────────────────
            // 2) Última versión importada de CEPRE con condición INGRESO
            // ───────────────────────────────────────────────────────────
            var includeCepre = !filter.TypeModalityId.HasValue && !filter.TypePostulantId.HasValue
                && (filter.ModalityId == null || cepreModalityIds.Contains(filter.ModalityId.Value))
                && filter.SegundaCarrera != "1";

            if (includeCepre)
            {
                var latestCepreVersion = await _context.CepreImportVersions
                    .AsNoTracking()
                    .Where(v => v.TermId == termId && v.IsLatest)
                    .OrderByDescending(v => v.VersionNumber)
                    .FirstOrDefaultAsync(ct);

                if (latestCepreVersion != null)
                {
                    var cepreRecords = await _context.CepreImportRecords
                        .AsNoTracking()
                        .Where(r => r.VersionId == latestCepreVersion.Id)
                        .ToListAsync(ct);

                    var ingresoRecords = cepreRecords
                        .Where(r => string.Equals(
                            RemoveDiacritics(r.Estado ?? "").Trim().ToUpperInvariant(),
                            "INGRESO",
                            StringComparison.Ordinal))
                        .Where(r => !string.IsNullOrWhiteSpace(r.Dni))
                        .GroupBy(r => r.Dni!.Trim())
                        .Select(g => g.OrderByDescending(r => ParseCiclo(r.Ciclo)).First())
                        .ToList();

                    if (ingresoRecords.Count > 0)
                    {
                        var careersByCode = await _context.Careers.AsNoTracking()
                            .ToDictionaryAsync(c => (c.Code ?? "").Trim(), c => c.Id, ct);

                        foreach (var rec in ingresoRecords)
                        {
                            Guid? careerId = null;
                            if (!string.IsNullOrWhiteSpace(rec.CodigoCarrera) && careersByCode.TryGetValue(rec.CodigoCarrera!.Trim(), out var cId))
                                careerId = cId;

                            if (filter.CareerId.HasValue && careerId != filter.CareerId.Value) continue;
                            if (filter.TematicAreaId.HasValue && careerIdsInArea != null && (!careerId.HasValue || !careerIdsInArea.Contains(careerId.Value))) continue;

                            var areaId = careerId.HasValue && areaByCareer.TryGetValue(careerId.Value, out var aid) ? (Guid?)aid : null;

                            items.Add(new IngresantesReportItem
                            {
                                CodigoEstudiante = rec.Codigo ?? "",
                                Examen = "CEPRE",
                                TipoModalidad = "CEPRE",
                                TipoPostulante = "",
                                Apellidos = $"{rec.Apaterno} {rec.Amaterno}".Trim(),
                                Nombres = rec.Nombres ?? "",
                                CarreraProfesional = rec.CarreraProfesional ?? "",
                                Tema = areaId.HasValue && areaNames.TryGetValue(areaId.Value, out var code) ? code : "",
                                Nota = rec.NotaFinal,
                                IsAdmission = true,
                                SegundaCarrera = "0"
                            });
                        }
                    }
                }
            }

            return items
                .OrderBy(x => x.CarreraProfesional, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.CodigoEstudiante, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private async Task<List<IngresantesReportItem>> MapRecordsAsync(
            List<ConsolidadoIngresantesRecord> records,
            Guid termId,
            CancellationToken ct)
        {
            var areaByCareer = await _context.TematicAreaCareers.AsNoTracking()
                .Where(tac => tac.TermId == termId)
                .ToDictionaryAsync(tac => tac.CareerId, tac => tac.TematicAreaId, ct);

            var areaNames = await _context.TematicAreas.AsNoTracking()
                .ToDictionaryAsync(a => a.Id, a => a.Code, ct);

            return records.Select(r =>
            {
                var i = r.Inscription;
                var areaId = i != null && areaByCareer.TryGetValue(i.CareerId, out var aid) ? aid : (Guid?)null;

                return new IngresantesReportItem
                {
                    CodigoEstudiante = r.CodigoEstudiante,
                    Examen = i?.Modality?.Name ?? "",
                    TipoModalidad = i?.TypeModality?.Name ?? "",
                    TipoPostulante = i?.TypePostulantInscription?.Name ?? "",
                    Apellidos = $"{i?.Postulant?.User?.FirstNameFather} {i?.Postulant?.User?.FirstNameMother}".Trim(),
                    Nombres = i?.Postulant?.User?.Name ?? "",
                    CarreraProfesional = i?.Career?.Name ?? "",
                    Tema = areaId.HasValue && areaNames.TryGetValue(areaId.Value, out var code) ? code : "",
                    Nota = i?.GradeAdmission,
                    IsAdmission = true,
                    SegundaCarrera = r.SegundaCarrera == "1" ? "1" : "0"
                };
            }).ToList();
        }

        private static string RemoveDiacritics(string text)
        {
            var normalized = text.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();
            foreach (var c in normalized)
            {
                var cat = CharUnicodeInfo.GetUnicodeCategory(c);
                if (cat != UnicodeCategory.NonSpacingMark)
                    sb.Append(c);
            }
            return sb.ToString().Normalize(NormalizationForm.FormC);
        }

        private static (int Year, int Period) ParseCiclo(string? ciclo)
        {
            if (string.IsNullOrWhiteSpace(ciclo))
                return (0, 0);

            var parts = ciclo.Trim().Split('-', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2)
                return (int.TryParse(parts.FirstOrDefault(), out var y) ? y : 0, 0);

            var year = int.TryParse(parts[0], out var yr) ? yr : 0;
            var periodStr = parts[1].Trim().ToUpperInvariant();
            var period = periodStr switch
            {
                "0" => 0,
                "I" => 1,
                "II" => 2,
                "III" => 3,
                "IV" => 4,
                "V" => 5,
                _ => int.TryParse(periodStr, out var p) ? p : 0
            };

            return (year, period);
        }
    }
}