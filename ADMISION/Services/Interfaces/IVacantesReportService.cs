using ADMISION.Models.ViewModels.Reports;

namespace ADMISION.Services.Interfaces
{
    public interface IVacantesReportService
    {
        Task<VacantesReportViewModel> BuildAsync(VacantesReportFilter filter, CancellationToken ct = default);
    }
}
