using ADMISION.ENTITIES.Data;
using ADMISION.ENTITIES.Models.Infrastructure;
using ADMISION.Models.Shared;
using ADMISION.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ADMISION.Services.Implementations
{
    public class ExamScheduleService : IExamScheduleService
    {
        private readonly AppDbContext _context;

        public ExamScheduleService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<ExamScheduleDetailDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            var schedule = await _context.ExamSchedules
                .AsNoTracking()
                .Include(s => s.Modality)
                .Include(s => s.Term)
                .Include(s => s.Rooms!)
                    .ThenInclude(r => r.Classroom)
                        .ThenInclude(c => c!.Pavilion)
                .Include(s => s.Rooms!)
                    .ThenInclude(r => r.Teacher)
                        .ThenInclude(t => t!.User)
                .Include(s => s.Rooms!)
                    .ThenInclude(r => r.TematicArea)
                .FirstOrDefaultAsync(s => s.Id == id, ct);

            return schedule == null ? null : MapToDetailDto(schedule);
        }

        public async Task<ExamScheduleDetailDto?> GetByModalityAsync(Guid modalityId, CancellationToken ct = default)
        {
            var schedule = await _context.ExamSchedules
                .AsNoTracking()
                .Include(s => s.Modality)
                .Include(s => s.Term)
                .Include(s => s.Rooms!)
                    .ThenInclude(r => r.Classroom)
                        .ThenInclude(c => c!.Pavilion)
                .Include(s => s.Rooms!)
                    .ThenInclude(r => r.Teacher)
                        .ThenInclude(t => t!.User)
                .Include(s => s.Rooms!)
                    .ThenInclude(r => r.TematicArea)
                .FirstOrDefaultAsync(s => s.ModalityId == modalityId, ct);

            return schedule == null ? null : MapToDetailDto(schedule);
        }

        public async Task<SaveResult> CreateAsync(string name, Guid modalityId, Guid termId, List<ExamScheduleRoomDto> rooms, string actor, CancellationToken ct = default)
        {
            var existing = await _context.ExamSchedules
                .AnyAsync(s => s.ModalityId == modalityId, ct);
            if (existing)
                return SaveResult.Invalid(new ValidationError(string.Empty, "Ya existe un horario de examen para esta modalidad."));

            var now = DateTimeOffset.UtcNow;
            var schedule = new ExamSchedule
            {
                Id = Guid.NewGuid(),
                Name = name,
                ModalityId = modalityId,
                TermId = termId,
                CreatedAt = now,
                CreatedBy = actor,
                Rooms = new List<ENTITIES.Models.Infrastructure.ExamScheduleRoom>()
            };

            foreach (var r in rooms)
            {
                schedule.Rooms!.Add(new ExamScheduleRoom
                {
                    Id = Guid.NewGuid(),
                    ExamScheduleId = schedule.Id,
                    ClassroomId = r.ClassroomId,
                    TeacherId = r.TeacherId,
                    TematicAreaId = r.TematicAreaId,
                    AssignedCapacity = r.AssignedCapacity,
                    CreatedAt = now,
                    CreatedBy = actor
                });
            }

            _context.ExamSchedules.Add(schedule);
            await _context.SaveChangesAsync(ct);
            return SaveResult.Ok();
        }

        public async Task<SaveResult> UpdateAsync(Guid id, List<ExamScheduleRoomDto> rooms, string actor, CancellationToken ct = default)
        {
            var now = DateTimeOffset.UtcNow;

            var affected = await _context.ExamSchedules
                .Where(s => s.Id == id)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(s => s.UpdatedAt, now)
                    .SetProperty(s => s.UpdatedBy, actor), ct);

            if (affected == 0) return SaveResult.NotFoundResult();

            await _context.ExamScheduleRooms
                .Where(r => r.ExamScheduleId == id)
                .ExecuteDeleteAsync(ct);

            foreach (var r in rooms)
            {
                _context.ExamScheduleRooms.Add(new ExamScheduleRoom
                {
                    Id = Guid.NewGuid(),
                    ExamScheduleId = id,
                    ClassroomId = r.ClassroomId,
                    TeacherId = r.TeacherId,
                    TematicAreaId = r.TematicAreaId,
                    AssignedCapacity = r.AssignedCapacity,
                    CreatedAt = now,
                    CreatedBy = actor
                });
            }

            await _context.SaveChangesAsync(ct);
            return SaveResult.Ok();
        }

        public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
        {
            var schedule = await _context.ExamSchedules
                .Include(s => s.Rooms)
                .FirstOrDefaultAsync(s => s.Id == id, ct);

            if (schedule == null) return false;

            _context.ExamScheduleRooms.RemoveRange(schedule.Rooms!);
            _context.ExamSchedules.Remove(schedule);
            await _context.SaveChangesAsync(ct);
            return true;
        }

        private static ExamScheduleDetailDto MapToDetailDto(ExamSchedule schedule)
        {
            return new ExamScheduleDetailDto
            {
                Id = schedule.Id,
                Name = schedule.Name,
                ModalityId = schedule.ModalityId,
                TermId = schedule.TermId,
                ModalityName = schedule.Modality?.Name,
                TermName = schedule.Term?.Name,
                Rooms = schedule.Rooms?.Select(r => new ExamScheduleRoomDetailDto
                {
                    Id = r.Id,
                    ClassroomId = r.ClassroomId,
                    ClassroomName = r.Classroom?.Name,
                    TeacherId = r.TeacherId,
                    TeacherName = r.Teacher?.User?.FullName,
                    TematicAreaId = r.TematicAreaId,
                    TematicAreaCode = r.TematicArea?.Code,
                    AssignedCapacity = r.AssignedCapacity,
                    ClassroomCapacity = r.Classroom?.Capacity ?? 0,
                    PavilionName = r.Classroom?.Pavilion?.Name,
                    PavilionCode = r.Classroom?.Pavilion?.Code,
                    Floor = r.Classroom?.Floor ?? 0
                }).OrderBy(r => r.PavilionCode).ThenBy(r => r.Floor).ThenBy(r => r.ClassroomName).ToList() ?? new()
            };
        }
    }
}
