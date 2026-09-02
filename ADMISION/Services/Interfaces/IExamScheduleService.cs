using ADMISION.ENTITIES.Models.Infrastructure;
using ADMISION.Models.Shared;

namespace ADMISION.Services.Interfaces
{
    public class ExamScheduleRoomDto
    {
        public Guid ClassroomId { get; set; }
        public Guid? TeacherId { get; set; }
        public Guid TematicAreaId { get; set; }
        public int AssignedCapacity { get; set; }
    }

    public class ExamScheduleDetailDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public Guid ModalityId { get; set; }
        public Guid TermId { get; set; }
        public string? ModalityName { get; set; }
        public string? TermName { get; set; }
        public List<ExamScheduleRoomDetailDto> Rooms { get; set; } = new();
    }

    public class ExamScheduleRoomDetailDto
    {
        public Guid Id { get; set; }
        public Guid ClassroomId { get; set; }
        public string? ClassroomName { get; set; }
        public Guid? TeacherId { get; set; }
        public string? TeacherName { get; set; }
        public Guid TematicAreaId { get; set; }
        public string? TematicAreaCode { get; set; }
        public int AssignedCapacity { get; set; }
        public int ClassroomCapacity { get; set; }
        public string? PavilionName { get; set; }
        public string? PavilionCode { get; set; }
        public int Floor { get; set; }
    }

    public interface IExamScheduleService
    {
        Task<ExamScheduleDetailDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<ExamScheduleDetailDto?> GetByModalityAsync(Guid modalityId, CancellationToken ct = default);
        Task<SaveResult> CreateAsync(string name, Guid modalityId, Guid termId, List<ExamScheduleRoomDto> rooms, string actor, CancellationToken ct = default);
        Task<SaveResult> UpdateAsync(Guid id, List<ExamScheduleRoomDto> rooms, string actor, CancellationToken ct = default);
        Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
    }
}
