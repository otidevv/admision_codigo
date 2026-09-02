using ADMISION.Models.ViewModels.Reports;

namespace ADMISION.Services.Interfaces
{
    public interface IResultadosReportService
    {
        Task<ResultadosReportViewModel> BuildAsync(ResultadosReportFilter filter, CancellationToken ct = default);
        Task<ResultadosReportViewModel> BuildAllAsync(ResultadosReportFilter filter, CancellationToken ct = default);
        Task<ResultadosFilterOptions> GetFilterOptionsAsync(Guid termId, CancellationToken ct = default);
    }

    public class ResultadosReportFilter
    {
        public Guid? TermId { get; set; }
        public Guid? ModalityId { get; set; }
        public Guid? TypeModalityId { get; set; }
        public Guid? TypePostulantId { get; set; }
        public Guid? CareerId { get; set; }
        public string? Condicion { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }

    public class ResultadosFilterOptions
    {
        public List<string> Condiciones { get; set; } = new();
    }
}
