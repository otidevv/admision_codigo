using ADMISION.Models.ViewModels.Reports;

namespace ADMISION.Services.Interfaces
{
    public interface IEconomicReportService
    {
        Task<EconomicReportViewModel> BuildAsync(EconomicReportFilter filter, CancellationToken ct = default);
        Task<List<EconomicReportItem>> BuildAllAsync(EconomicReportFilter filter, CancellationToken ct = default);
    }
}
