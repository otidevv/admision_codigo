using ADMISION.ENTITIES.Models.Exam;
using ADMISION.Models.ViewModels.Reports;

namespace ADMISION.Services.Interfaces
{
    public interface ICepreReportService
    {
        Task<CepreReportViewModel> BuildAsync(CepreReportFilter filter, CancellationToken ct = default);
        Task<CepreReportViewModel> BuildAllAsync(CepreReportFilter filter, CancellationToken ct = default);
        Task<List<CepreImportVersion>> GetVersionsAsync(Guid termId, CancellationToken ct = default);
    }

    public class CepreReportFilter
    {
        public Guid? TermId { get; set; }
        public Guid? VersionId { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }
}
