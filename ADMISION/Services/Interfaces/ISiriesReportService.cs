using ADMISION.Models.ViewModels.Reports;

namespace ADMISION.Services.Interfaces
{
    public interface ISiriesReportService
    {
        Task<SiriesReportViewModel> BuildAsync(SiriesReportFilter filter, CancellationToken ct = default);
    }

    public class SiriesReportFilter
    {
        public Guid? TermId { get; set; }
    }
}
