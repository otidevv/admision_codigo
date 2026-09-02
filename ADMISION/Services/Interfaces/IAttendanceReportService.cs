using ADMISION.Models.ViewModels.Reports;

namespace ADMISION.Services.Interfaces
{
    public interface IAttendanceReportService
    {
        Task<AttendanceReportViewModel> BuildAsync(AttendanceReportFilter filter, CancellationToken ct = default);
    }
}
