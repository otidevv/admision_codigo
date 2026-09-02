using ADMISION.ENTITIES.Constants;
using ADMISION.ENTITIES.Data;
using ADMISION.ENTITIES.Models.Infrastructure;
using ADMISION.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ADMISION.Services.Implementations
{
    public class ExamAssignmentService : IExamAssignmentService
    {
        private readonly AppDbContext _context;

        public ExamAssignmentService(AppDbContext context)
        {
            _context = context;
        }

        public Task<SorteoSummary> PreviewAsync(Guid examScheduleId, CancellationToken ct = default)
            => BuildAsync(examScheduleId, persist: false, createdBy: null, ct);

        public Task<SorteoSummary> ExecuteAsync(Guid examScheduleId, string createdBy, CancellationToken ct = default)
            => BuildAsync(examScheduleId, persist: true, createdBy: createdBy, ct);

        public async Task ClearAsync(Guid examScheduleId, CancellationToken ct = default)
        {
            var schedule = await _context.ExamSchedules
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == examScheduleId, ct);

            if (schedule == null) return;

            await _context.ExamAssignments
                .Where(e => e.ModalityId == schedule.ModalityId)
                .ExecuteDeleteAsync(ct);
        }

        public async Task<int> CountByScheduleAsync(Guid examScheduleId, CancellationToken ct = default)
            => await _context.ExamAssignments.AsNoTracking()
                .CountAsync(e => e.ExamScheduleId == examScheduleId, ct);

        public async Task<IReadOnlyList<ExamAssignment>> GetByScheduleAsync(Guid examScheduleId, CancellationToken ct = default)
        {
            return await _context.ExamAssignments
                .AsNoTracking()
                .Include(e => e.Classroom).ThenInclude(c => c!.Pavilion)
                .Include(e => e.Inscription).ThenInclude(i => i!.Postulant).ThenInclude(p => p!.User)
                .Include(e => e.Inscription).ThenInclude(i => i!.Career)
                .Include(e => e.TematicArea)
                .Include(e => e.Teacher).ThenInclude(t => t!.User)
                .Include(e => e.ExamSchedule)
                .Where(e => e.ExamScheduleId == examScheduleId)
                .OrderBy(e => e.Classroom!.Pavilion!.Code)
                .ThenBy(e => e.Classroom!.Floor)
                .ThenBy(e => e.Classroom!.Name)
                .ThenBy(e => e.SeatNumber)
                .ToListAsync(ct);
        }

        public async Task<ExamAssignmentExportData?> GetExportDataAsync(Guid examScheduleId, CancellationToken ct = default)
        {
            var schedule = await _context.ExamSchedules
                .AsNoTracking()
                .Include(s => s.Modality)
                .FirstOrDefaultAsync(s => s.Id == examScheduleId, ct);
            if (schedule?.Modality == null) return null;

            var assignments = await GetByScheduleAsync(examScheduleId, ct);
            return new ExamAssignmentExportData(schedule.Modality, assignments);
        }

        private async Task<SorteoSummary> BuildAsync(Guid examScheduleId, bool persist, string? createdBy, CancellationToken ct)
        {
            var schedule = await _context.ExamSchedules
                .AsNoTracking()
                .Include(s => s.Modality)
                .Include(s => s.Rooms!)
                    .ThenInclude(r => r.Classroom)
                        .ThenInclude(c => c!.Pavilion)
                .Include(s => s.Rooms!)
                    .ThenInclude(r => r.TematicArea)
                .Include(s => s.Rooms!)
                    .ThenInclude(r => r.Teacher)
                        .ThenInclude(t => t!.User)
                .FirstOrDefaultAsync(s => s.Id == examScheduleId, ct);

            if (schedule?.Modality == null || schedule.Rooms == null || schedule.Rooms.Count == 0)
                return new SorteoSummary();

            var modalityId = schedule.ModalityId;
            var termId = schedule.TermId;

            var inscriptions = await _context.Inscriptions
                .AsNoTracking()
                .Include(i => i.Career)
                .Where(i => i.ModalityId == modalityId
                            && i.State == AppConstants.InscripcionState.Aprobado)
                .ToListAsync(ct);

            var areaByCareer = await _context.TematicAreaCareers
                .AsNoTracking()
                .Where(tac => tac.TermId == termId)
                .ToDictionaryAsync(tac => tac.CareerId, tac => tac.TematicAreaId);

            var tematicAreas = await _context.TematicAreas
                .AsNoTracking()
                .ToDictionaryAsync(a => a.Id, a => a.Code);

            var inscriptionQueues = inscriptions
                .Select(i => new
                {
                    Inscription = i,
                    AreaId = areaByCareer.TryGetValue(i.CareerId, out var aid) ? (Guid?)aid : null
                })
                .GroupBy(x => x.AreaId)
                .ToDictionary(
                    g => g.Key,
                    g => new Queue<ENTITIES.Models.Postulante.Inscription>(
                        Shuffle(g.Select(x => x.Inscription).ToList()))
                );

            var inscriptionCounts = inscriptionQueues.ToDictionary(
                kv => kv.Key,
                kv => kv.Value.Count);

            var assignments = new List<ExamAssignment>();
            var roomSummaries = new List<SorteoRoomSummary>();
            var now = DateTimeOffset.UtcNow;

            var orderedRooms = schedule.Rooms
                .OrderBy(r => r.Classroom?.Pavilion?.Code)
                .ThenBy(r => r.Classroom?.Floor)
                .ThenBy(r => r.Classroom?.Name)
                .ToList();

            foreach (var room in orderedRooms)
            {
                var areaId = room.TematicAreaId;
                var areaCode = tematicAreas.TryGetValue(areaId, out var code) ? code : "SIN ÁREA";

                var queue = inscriptionQueues.TryGetValue(areaId, out var q)
                    ? q
                    : new Queue<ENTITIES.Models.Postulante.Inscription>();

                int seat = 1;
                int folder = 1;
                int assigned = 0;

                while (seat <= room.AssignedCapacity && queue.Count > 0)
                {
                    var inscription = queue.Dequeue();
                    assignments.Add(new ExamAssignment
                    {
                        Id = Guid.NewGuid(),
                        InscriptionId = inscription.Id,
                        ClassroomId = room.ClassroomId,
                        TermId = termId,
                        ModalityId = modalityId,
                        TematicAreaId = areaId,
                        ExamScheduleId = examScheduleId,
                        TeacherId = room.TeacherId,
                        SeatNumber = seat,
                        FolderNumber = folder,
                        CreatedAt = now,
                        CreatedBy = createdBy ?? "Preview"
                    });
                    assigned++;
                    seat++;
                    folder++;
                }

                roomSummaries.Add(new SorteoRoomSummary
                {
                    PavilionCode = room.Classroom?.Pavilion?.Code ?? "",
                    PavilionName = room.Classroom?.Pavilion?.Name ?? "",
                    Floor = room.Classroom?.Floor ?? 0,
                    ClassroomName = room.Classroom?.Name ?? "",
                    TeacherName = room.Teacher?.User?.FullName,
                    AreaCode = areaCode,
                    Capacity = room.AssignedCapacity,
                    Assigned = assigned
                });
            }

            if (persist)
            {
                await _context.ExamAssignments
                    .Where(e => e.ModalityId == modalityId)
                    .ExecuteDeleteAsync(ct);

                _context.ExamAssignments.AddRange(assignments);
                await _context.SaveChangesAsync(ct);
            }

            int totalAforo = orderedRooms.Sum(r => r.AssignedCapacity);

            return new SorteoSummary
            {
                TotalInscripciones = inscriptions.Count,
                TotalAsignadas = assignments.Count,
                TotalNoAsignadas = inscriptions.Count - assignments.Count,
                TotalSalones = orderedRooms.Count,
                TotalAforo = totalAforo,
                PorArea = inscriptionQueues.Select(g =>
                {
                    var areaCode = g.Key.HasValue && tematicAreas.TryGetValue(g.Key.Value, out var c) ? c : "SIN ÁREA";
                    var asignadasArea = assignments.Count(a => a.TematicAreaId == g.Key);
                    var totalArea = inscriptionCounts.TryGetValue(g.Key, out var cnt) ? cnt : 0;
                    return new SorteoAreaSummary
                    {
                        TematicAreaId = g.Key,
                        AreaCode = areaCode,
                        Cantidad = totalArea,
                        Asignadas = asignadasArea
                    };
                }).ToList(),
                PorSalon = roomSummaries
            };
        }

        private static List<T> Shuffle<T>(List<T> list)
        {
            var rng = new Random();
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
            return list;
        }
    }
}
