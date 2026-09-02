using ADMISION.ENTITIES.Constants;
using ADMISION.ENTITIES.Data;
using ADMISION.Models.ViewModels.Reports;
using ADMISION.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ADMISION.Services.Implementations
{
    public class VacantesReportService : IVacantesReportService
    {
        private readonly AppDbContext _context;

        public VacantesReportService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<VacantesReportViewModel> BuildAsync(VacantesReportFilter filter, CancellationToken ct = default)
        {
            var vm = new VacantesReportViewModel
            {
                TermId = filter.TermId,
                ReportType = filter.ReportType
            };

            if (!filter.TermId.HasValue) return vm;

            var term = await _context.Terms.AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == filter.TermId.Value, ct);
            vm.TermName = term?.Name;

            var modalities = await _context.Modalities
                .AsNoTracking()
                .Where(m => m.TermId == filter.TermId.Value && !m.IsMockExam)
                .OrderBy(m => m.DisplayOrder)
                .ThenBy(m => m.Name)
                .ToListAsync(ct);

            var typeModalities = await _context.TypeModalities
                .AsNoTracking()
                .Where(tm => tm.Modality != null && tm.Modality.TermId == filter.TermId.Value)
                .OrderBy(tm => tm.Name)
                .ToListAsync(ct);

            var typesByModality = typeModalities
                .GroupBy(tm => tm.ModalityId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var modalityGroups = new List<VacantesModalityGroup>();
            var columns = new List<VacantesColumnInfo>();

            foreach (var modality in modalities)
            {
                if (typesByModality.TryGetValue(modality.Id, out var types) && types.Count > 0)
                {
                    modalityGroups.Add(new VacantesModalityGroup
                    {
                        ModalityId = modality.Id,
                        ModalityName = modality.Name,
                        ColumnCount = types.Count,
                        HasSubHeaders = true
                    });
                    foreach (var tm in types)
                    {
                        columns.Add(new VacantesColumnInfo
                        {
                            ModalityId = modality.Id,
                            TypeModalityId = tm.Id,
                            Header = tm.Name
                        });
                    }
                }
                else
                {
                    modalityGroups.Add(new VacantesModalityGroup
                    {
                        ModalityId = modality.Id,
                        ModalityName = modality.Name,
                        ColumnCount = 1,
                        HasSubHeaders = false
                    });
                    columns.Add(new VacantesColumnInfo
                    {
                        ModalityId = modality.Id,
                        TypeModalityId = null,
                        Header = modality.Name
                    });
                }
            }

            vm.ModalityGroups = modalityGroups;
            vm.Columns = columns;

            if (columns.Count == 0) return vm;

            var data = filter.ReportType switch
            {
                "postulantes" => await BuildPostulantesDataAsync(filter.TermId.Value, columns, ct),
                "ingresantes" => await BuildIngresantesDataAsync(filter.TermId.Value, columns, ct),
                "consolidado" => await BuildConsolidadoDataAsync(filter.TermId.Value, columns, ct),
                _ => await BuildVacantesDataAsync(filter.TermId.Value, columns, ct)
            };

            vm.Faculties = data;
            vm.ColumnTotals = BuildColumnTotals(data, columns.Count);
            vm.GrandTotal = vm.ColumnTotals.Sum();
            return vm;
        }

        private async Task<IReadOnlyList<VacantesFacultyGroup>> BuildVacantesDataAsync(
            Guid termId, List<VacantesColumnInfo> columns, CancellationToken ct)
        {
            var rawVacancies = await _context.Vacancies
                .AsNoTracking()
                .Include(v => v.Career).ThenInclude(c => c!.Faculty)
                .Where(v => v.Modality != null && v.Modality.TermId == termId)
                .ToListAsync(ct);

            var grouped = rawVacancies
                .Where(v => v.Career?.Faculty != null)
                .GroupBy(v => new { FacultyId = v.Career!.Faculty!.Id, v.CareerId })
                .Select(g => new RawRow
                {
                    FacultyId = g.Key.FacultyId,
                    FacultyName = g.First().Career!.Faculty!.Name,
                    FacultyIcon = "ti ti-book",
                    CareerName = g.First().Career!.Name,
                    Values = columns.Select(col =>
                        g.Where(v => v.ModalityId == col.ModalityId && v.TypeModalityId == col.TypeModalityId)
                         .Sum(v => v.Quantity)
                    ).ToList()
                })
                .ToList();

            return GroupRows(grouped);
        }

        private async Task<IReadOnlyList<VacantesFacultyGroup>> BuildPostulantesDataAsync(
            Guid termId, List<VacantesColumnInfo> columns, CancellationToken ct)
        {
            var inscriptions = await _context.Inscriptions
                .AsNoTracking()
                .Include(i => i.Career).ThenInclude(c => c!.Faculty)
                .Include(i => i.Modality)
                .Where(i => i.Modality != null
                         && i.Modality.TermId == termId
                         && i.State == AppConstants.InscripcionState.Aprobado)
                .ToListAsync(ct);

            var grouped = inscriptions
                .Where(i => i.Career?.Faculty != null)
                .GroupBy(i => new { FacultyId = i.Career!.Faculty!.Id, i.CareerId })
                .Select(g => new RawRow
                {
                    FacultyId = g.Key.FacultyId,
                    FacultyName = g.First().Career!.Faculty!.Name,
                    FacultyIcon = "ti ti-book",
                    CareerName = g.First().Career!.Name,
                    Values = columns.Select(col =>
                        g.Count(i => i.ModalityId == col.ModalityId && i.TypeModalityId == col.TypeModalityId)
                    ).ToList()
                })
                .ToList();

            return GroupRows(grouped);
        }

        private async Task<IReadOnlyList<VacantesFacultyGroup>> BuildIngresantesDataAsync(
            Guid termId, List<VacantesColumnInfo> columns, CancellationToken ct)
        {
            var inscriptions = await _context.Inscriptions
                .AsNoTracking()
                .Include(i => i.Career).ThenInclude(c => c!.Faculty)
                .Include(i => i.Modality)
                .Where(i => i.Modality != null
                         && i.Modality.TermId == termId
                         && i.IsAdmission
                         && i.GradeAdmission != null)
                .ToListAsync(ct);

            var grouped = inscriptions
                .Where(i => i.Career?.Faculty != null)
                .GroupBy(i => new { FacultyId = i.Career!.Faculty!.Id, i.CareerId })
                .Select(g => new RawRow
                {
                    FacultyId = g.Key.FacultyId,
                    FacultyName = g.First().Career!.Faculty!.Name,
                    FacultyIcon = "ti ti-book",
                    CareerName = g.First().Career!.Name,
                    Values = columns.Select(col =>
                        g.Count(i => i.ModalityId == col.ModalityId && i.TypeModalityId == col.TypeModalityId)
                    ).ToList()
                })
                .ToList();

            return GroupRows(grouped);
        }

        private async Task<IReadOnlyList<VacantesFacultyGroup>> BuildConsolidadoDataAsync(
            Guid termId, List<VacantesColumnInfo> columns, CancellationToken ct)
        {
            var version = await _context.ConsolidadoIngresantesVersions
                .AsNoTracking()
                .Where(v => v.TermId == termId && v.IsLatest)
                .FirstOrDefaultAsync(ct);

            if (version == null) return Array.Empty<VacantesFacultyGroup>();

            var records = await _context.ConsolidadoIngresantesRecords
                .AsNoTracking()
                .Where(r => r.VersionId == version.Id && r.InscriptionId.HasValue)
                .ToListAsync(ct);

            if (records.Count == 0) return Array.Empty<VacantesFacultyGroup>();

            var inscriptionIds = records.Select(r => r.InscriptionId!.Value).Distinct().ToList();

            var inscriptions = await _context.Inscriptions
                .AsNoTracking()
                .Include(i => i.Career).ThenInclude(c => c!.Faculty)
                .Where(i => inscriptionIds.Contains(i.Id))
                .ToDictionaryAsync(i => i.Id, ct);

            var grouped = records
                .Where(r => inscriptions.TryGetValue(r.InscriptionId!.Value, out var ins)
                         && ins.Career?.Faculty != null)
                .Select(r => new { Inscription = inscriptions[r.InscriptionId!.Value] })
                .GroupBy(x => new { FacultyId = x.Inscription.Career!.Faculty!.Id, x.Inscription.CareerId })
                .Select(g => new RawRow
                {
                    FacultyId = g.Key.FacultyId,
                    FacultyName = g.First().Inscription.Career!.Faculty!.Name,
                    FacultyIcon = "ti ti-book",
                    CareerName = g.First().Inscription.Career!.Name,
                    Values = columns.Select(col =>
                        g.Count(x => x.Inscription.ModalityId == col.ModalityId
                                  && x.Inscription.TypeModalityId == col.TypeModalityId)
                    ).ToList()
                })
                .ToList();

            return GroupRows(grouped);
        }

        private static IReadOnlyList<VacantesFacultyGroup> GroupRows(List<RawRow> rows)
        {
            return rows
                .GroupBy(r => r.FacultyId)
                .Select(fg =>
                {
                    var first = fg.First();
                    return new VacantesFacultyGroup
                    {
                        Id = first.FacultyId,
                        Name = first.FacultyName,
                        Icon = first.FacultyIcon,
                        Careers = fg
                            .Select(r => new VacantesCareerRow
                            {
                                CareerName = r.CareerName,
                                Values = r.Values,
                                Total = r.Values.Sum()
                            })
                            .OrderBy(r => r.CareerName)
                            .ToList(),
                        Subtotal = fg.Sum(r => r.Values.Sum())
                    };
                })
                .OrderBy(f => f.Name)
                .ToList();
        }

        private static IReadOnlyList<int> BuildColumnTotals(IReadOnlyList<VacantesFacultyGroup> faculties, int columnCount)
        {
            var totals = new int[columnCount];
            foreach (var faculty in faculties)
            {
                foreach (var career in faculty.Careers)
                {
                    for (int i = 0; i < Math.Min(career.Values.Count, columnCount); i++)
                    {
                        totals[i] += career.Values[i];
                    }
                }
            }
            return totals;
        }

        private class RawRow
        {
            public Guid FacultyId { get; set; }
            public string FacultyName { get; set; } = string.Empty;
            public string FacultyIcon { get; set; } = string.Empty;
            public string CareerName { get; set; } = string.Empty;
            public List<int> Values { get; set; } = new();
        }
    }
}
