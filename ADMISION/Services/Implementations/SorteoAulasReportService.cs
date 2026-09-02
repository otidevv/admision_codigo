using ADMISION.ENTITIES.Data;
using ADMISION.ENTITIES.Models.Biometrics;
using ADMISION.Models.ViewModels.Reports;
using ADMISION.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace ADMISION.Services.Implementations
{
    public class SorteoAulasReportService : ISorteoAulasReportService
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _configuration;

        public SorteoAulasReportService(AppDbContext context, IWebHostEnvironment env, IConfiguration configuration)
        {
            _context = context;
            _env = env;
            _configuration = configuration;
        }

        public async Task<SorteoAulasReportViewModel> BuildAsync(SorteoAulasReportFilter filter, CancellationToken ct = default)
        {
            var vm = new SorteoAulasReportViewModel { Filter = filter };

            var schedule = await _context.ExamSchedules
                .AsNoTracking()
                .Include(s => s.Modality).Include(s => s.Term)
                .FirstOrDefaultAsync(s =>
                    s.TermId == filter.TermId &&
                    s.ModalityId == filter.ModalityId, ct);

            if (schedule == null) return vm;

            vm.TermName = schedule.Term?.Name;
            vm.ModalityName = schedule.Modality?.Name;

            var assignments = await _context.ExamAssignments
                .AsNoTracking()
                .Include(a => a.Classroom).ThenInclude(c => c!.Pavilion)
                .Include(a => a.Inscription).ThenInclude(i => i!.Postulant).ThenInclude(p => p!.User)
                .Include(a => a.Inscription).ThenInclude(i => i!.Career)
                .Include(a => a.Teacher).ThenInclude(t => t!.User)
                .Include(a => a.TematicArea)
                .Where(a => a.ExamScheduleId == schedule.Id)
                .OrderBy(a => a.Classroom!.Pavilion!.Code)
                .ThenBy(a => a.Classroom!.Group)
                .ThenBy(a => a.Classroom!.Name)
                .ThenBy(a => a.SeatNumber)
                .ToListAsync(ct);

            if (assignments.Count == 0) return vm;

            vm.HasData = true;

            var classroomMap = assignments
                .Where(a => a.Classroom != null)
                .GroupBy(a => a.ClassroomId)
                .ToDictionary(
                    g => g.Key,
                    g =>
                    {
                        var first = g.First();
                        return new
                        {
                            Name = first.Classroom!.Name,
                            Capacity = first.Classroom.Capacity,
                            Floor = first.Classroom.Floor,
                            Group = first.Classroom.Group ?? "Sin grupo",
                            PavilionCode = first.Classroom.Pavilion!.Code,
                            PavilionName = first.Classroom.Pavilion.Name,
                            PavilionId = first.Classroom.PavilionId,
                            TeacherName = g.FirstOrDefault(a => a.Teacher != null)?.Teacher?.User?.FullName,
                            AreaCode = g.FirstOrDefault(a => a.TematicArea != null)?.TematicArea?.Code,
                            Count = g.Count()
                        };
                    });

            var pavilionGroups = classroomMap.Values
                .GroupBy(c => new { c.PavilionId, c.PavilionCode, c.PavilionName })
                .OrderBy(g => g.Key.PavilionCode)
                .Select(pg => new SorteoAulasPavilionGroup
                {
                    PavilionCode = pg.Key.PavilionCode,
                    PavilionName = pg.Key.PavilionName,
                    Groups = pg.GroupBy(c => c.Group)
                        .OrderBy(g => g.Key)
                        .Select(g => new SorteoAulasClassroomGroup
                        {
                            GroupName = g.Key,
                            Classrooms = g.OrderBy(c => c.Floor).ThenBy(c => c.Name)
                                .Select(c => new SorteoAulasClassroomItem
                                {
                                    ClassroomName = c.Name,
                                    Capacidad = c.Capacity,
                                    Asignados = c.Count,
                                    Docente = c.TeacherName,
                                    AreaTematica = c.AreaCode,
                                    Piso = c.Floor
                                }).ToList()
                        }).ToList()
                }).ToList();

            vm.Summary = new SorteoAulasSummary
            {
                TotalAsignados = assignments.Count,
                TotalAulas = classroomMap.Count,
                TotalAforo = classroomMap.Values.Sum(c => c.Capacity),
                PorPabellon = pavilionGroups
            };

            var photoLookup = await _context.Set<PostulantPhoto>()
                .AsNoTracking()
                .Where(p => p.IsPrimary)
                .GroupBy(p => p.PostulantId)
                .Select(g => g.FirstOrDefault()!)
                .ToDictionaryAsync(p => p.PostulantId, p => p.PhotoUrl, ct);

            vm.Details = assignments.Select(a =>
            {
                var postulantId = a.Inscription?.PostulantId ?? Guid.Empty;
                var photoUrl = photoLookup.TryGetValue(postulantId, out var url) ? url : null;
                return new SorteoAulasDetailItem
                {
                    Silla = a.SeatNumber,
                    CodigoPostulante = a.Inscription?.CodePostulant ?? "",
                    Apellidos = BuildApellidos(a.Inscription?.Postulant?.User),
                    Nombres = a.Inscription?.Postulant?.User?.Name ?? "",
                    Carrera = a.Inscription?.Career?.Name ?? "",
                    Aula = a.Classroom?.Name ?? "",
                    Pabellon = a.Classroom?.Pavilion?.Code,
                    PhotoBytes = TryReadImage(photoUrl),
                    FotoBase64 = photoUrl
                };
            })
            .OrderBy(d => d.Aula)
            .ThenBy(d => d.Silla)
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

        private byte[]? TryReadImage(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return null;

            try
            {
                if (path.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                    path.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                    return null;

                var storageRoot = _configuration["FileUpload:BaseStoragePath"];

                if (path.StartsWith("uploads/", StringComparison.OrdinalIgnoreCase))
                    path = path["uploads/".Length..];

                var fullPath = Path.Combine(
                    storageRoot,
                    path.Replace('/', Path.DirectorySeparatorChar));

                if (!File.Exists(fullPath))
                    return null;

                using var image = Image.Load(fullPath);
                image.Mutate(ctx => ctx.Resize(new ResizeOptions
                {
                    Size = new Size(200, 200),
                    Mode = ResizeMode.Min
                }));

                using var ms = new MemoryStream();
                image.SaveAsJpeg(ms, new JpegEncoder { Quality = 75 });
                return ms.ToArray();
            }
            catch
            {
                return null;
            }
        }
    }
}
