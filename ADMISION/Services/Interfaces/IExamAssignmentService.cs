using ADMISION.ENTITIES.Models.Infrastructure;
using ADMISION.ENTITIES.Models.Modality;

namespace ADMISION.Services.Interfaces
{
    public class SorteoSummary
    {
        public int TotalInscripciones { get; set; }
        public int TotalAsignadas { get; set; }
        public int TotalNoAsignadas { get; set; }
        public int TotalSalones { get; set; }
        public int TotalAforo { get; set; }
        public List<SorteoAreaSummary> PorArea { get; set; } = new();
        public List<SorteoRoomSummary> PorSalon { get; set; } = new();
    }

    public class SorteoAreaSummary
    {
        public Guid? TematicAreaId { get; set; }
        public string AreaCode { get; set; } = string.Empty;
        public int Cantidad { get; set; }
        public int Asignadas { get; set; }
    }

    public class SorteoRoomSummary
    {
        public string PavilionCode { get; set; } = string.Empty;
        public string PavilionName { get; set; } = string.Empty;
        public int Floor { get; set; }
        public string ClassroomName { get; set; } = string.Empty;
        public string? TeacherName { get; set; }
        public string AreaCode { get; set; } = string.Empty;
        public int Capacity { get; set; }
        public int Assigned { get; set; }
    }

    public interface IExamAssignmentService
    {
        Task<SorteoSummary> PreviewAsync(Guid examScheduleId, CancellationToken ct = default);
        Task<SorteoSummary> ExecuteAsync(Guid examScheduleId, string createdBy, CancellationToken ct = default);
        Task ClearAsync(Guid examScheduleId, CancellationToken ct = default);

        Task<int> CountByScheduleAsync(Guid examScheduleId, CancellationToken ct = default);
        Task<IReadOnlyList<ExamAssignment>> GetByScheduleAsync(Guid examScheduleId, CancellationToken ct = default);
        Task<ExamAssignmentExportData?> GetExportDataAsync(Guid examScheduleId, CancellationToken ct = default);
    }

    public record ExamAssignmentExportData(Modality Modality, IReadOnlyList<ExamAssignment> Assignments);
}
