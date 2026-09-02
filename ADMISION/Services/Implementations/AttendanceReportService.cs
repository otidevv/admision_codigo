using ADMISION.ENTITIES.Data;
using ADMISION.Models.ViewModels.Reports;
using ADMISION.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ADMISION.Services.Implementations
{
    public class AttendanceReportService : IAttendanceReportService
    {
        private readonly AppDbContext _context;

        public AttendanceReportService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<AttendanceReportViewModel> BuildAsync(AttendanceReportFilter filter, CancellationToken ct = default)
        {
            var vm = new AttendanceReportViewModel { Filter = filter };

            var term = await _context.Terms.AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == filter.TermId, ct);
            if (term == null) return vm;
            vm.TermName = term.Name;

            if (filter.ModalityId.HasValue)
            {
                var mod = await _context.Modalities.AsNoTracking()
                    .FirstOrDefaultAsync(m => m.Id == filter.ModalityId.Value, ct);
                vm.ModalityName = mod?.Name;
            }

            // Query ExamAssignments for this term (+ optional modality filter)
            var query = _context.ExamAssignments
                .AsNoTracking()
                .Include(a => a.Inscription).ThenInclude(i => i!.Postulant).ThenInclude(p => p!.User)
                .Include(a => a.Inscription).ThenInclude(i => i!.Modality)
                .Include(a => a.Inscription).ThenInclude(i => i!.TypeModality)
                .Include(a => a.Inscription).ThenInclude(i => i!.Career)
                .Include(a => a.Inscription).ThenInclude(i => i!.Postulant).ThenInclude(p => p!.Disabilities).ThenInclude(d => d!.DisabilityType)
                .Include(a => a.Classroom)
                .Include(a => a.TematicArea)
                .Include(a => a.Teacher).ThenInclude(t => t!.User)
                .Where(a => a.TermId == filter.TermId);

            if (filter.ModalityId.HasValue)
                query = query.Where(a => a.ModalityId == filter.ModalityId.Value);

            var assignments = await query
                .OrderBy(a => a.Classroom!.Name)
                .ThenBy(a => a.SeatNumber)
                .ToListAsync(ct);

            if (assignments.Count == 0) return vm;

            // Load attendance records for these inscriptions
            var inscriptionIds = assignments.Select(a => a.InscriptionId).Distinct().ToList();
            var attendanceSet = await _context.PostulantAttendances
                .AsNoTracking()
                .Where(pa => inscriptionIds.Contains(pa.InscriptionId))
                .GroupBy(pa => pa.InscriptionId)
                .Select(g => g.First().InscriptionId)
                .ToHashSetAsync(ct);

            // Build items
            var allItems = assignments.Select(a =>
            {
                var ins = a.Inscription;
                var user = ins?.Postulant?.User;
                var hasDisability = ins?.Postulant?.Disabilities?.Any() == true;
                var disabilityNames = ins?.Postulant?.Disabilities?
                    .Where(d => d.DisabilityType != null)
                    .Select(d => d.DisabilityType!.Name)
                    .Distinct().ToList();
                var disabilityStr = hasDisability
                    ? string.Join(", ", disabilityNames ?? new List<string>())
                    : "—";

                var attended = attendanceSet.Contains(a.InscriptionId);

                return new AttendanceReportItem
                {
                    SeatNumber = a.SeatNumber,
                    CodePostulant = ins?.CodePostulant ?? "",
                    Apellidos = BuildApellidos(user),
                    Nombres = user?.Name ?? "",
                    Modality = ins?.Modality?.Name ?? "",
                    TypeModality = ins?.TypeModality?.Name ?? "",
                    Disability = disabilityStr,
                    Classroom = a.Classroom?.Name ?? "",
                    Docente = a.Teacher?.User?.FullName ?? "—",
                    AttendanceStatus = attended ? "Asistió" : "No asistió",
                    TematicArea = a.TematicArea?.Code ?? "Sin área",
                    Career = ins?.Career?.Name ?? ""
                };
            }).ToList();

            // Apply attendance filter
            allItems = filter.AttendanceStatus switch
            {
                "attended" => allItems.Where(i => i.AttendanceStatus == "Asistió").ToList(),
                "not_attended" => allItems.Where(i => i.AttendanceStatus == "No asistió").ToList(),
                _ => allItems // "all" — attended first, then not attended
            };

            // Sort: attended first, then by classroom, then seat
            allItems = allItems
                .OrderBy(i => i.AttendanceStatus == "No asistió" ? 1 : 0)
                .ThenBy(i => i.Classroom)
                .ThenBy(i => i.SeatNumber)
                .ToList();

            vm.Items = allItems;
            vm.TotalAssigned = assignments.Count;
            vm.TotalAttended = attendanceSet.Count;
            vm.HasData = true;

            // Summary by classroom
            vm.SummaryByClassroom = allItems
                .GroupBy(i => i.Classroom)
                .Select(g => new AttendanceClassroomSummary
                {
                    Classroom = g.Key,
                    Attended = g.Count(i => i.AttendanceStatus == "Asistió"),
                    Missing = g.Count(i => i.AttendanceStatus == "No asistió")
                })
                .OrderBy(s => s.Classroom)
                .ToList();

            // Summary by tematic area
            vm.SummaryByArea = allItems
                .GroupBy(i => i.TematicArea)
                .Select(g => new AttendanceAreaSummary
                {
                    Area = g.Key,
                    Attended = g.Count(i => i.AttendanceStatus == "Asistió"),
                    Missing = g.Count(i => i.AttendanceStatus == "No asistió")
                })
                .OrderBy(s => s.Area)
                .ToList();

            // Summary by career
            vm.SummaryByCareer = allItems
                .GroupBy(i => i.Career)
                .Select(g => new AttendanceCareerSummary
                {
                    Career = g.Key,
                    Attended = g.Count(i => i.AttendanceStatus == "Asistió"),
                    Missing = g.Count(i => i.AttendanceStatus == "No asistió")
                })
                .OrderByDescending(s => s.Total)
                .ToList();

            return vm;
        }

        private static string BuildApellidos(ENTITIES.Models.Users.Users? user)
        {
            if (user == null) return "";
            var parts = new[] { user.FirstNameFather, user.FirstNameMother }
                .Where(p => !string.IsNullOrWhiteSpace(p));
            return string.Join(" ", parts);
        }
    }
}
