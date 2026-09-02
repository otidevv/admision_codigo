using ADMISION.Models.ViewModels.Reports;

namespace ADMISION.Services.Interfaces
{
    public interface ISorteoAulasReportService
    {
        Task<SorteoAulasReportViewModel> BuildAsync(SorteoAulasReportFilter filter, CancellationToken ct = default);
    }
}
