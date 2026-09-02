using ADMISION.Models.ViewModels.Reports;

namespace ADMISION.Services.Interfaces
{
    public interface IGeneralReportService
    {
        Task<GeneralReportViewModel> BuildAsync(GeneralReportFilter filter, CancellationToken ct = default);
        Task<List<GeneralReportItem>> BuildAllAsync(GeneralReportFilter filter, CancellationToken ct = default);
    }
}
