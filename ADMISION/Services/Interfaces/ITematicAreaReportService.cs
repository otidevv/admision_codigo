using ADMISION.Models.ViewModels.Reports;

namespace ADMISION.Services.Interfaces
{
    public interface ITematicAreaReportService
    {
        Task<TematicAreaReportViewModel> BuildAsync(TematicAreaReportFilter filter, CancellationToken ct = default);
    }

    public class TematicAreaReportFilter
    {
        public Guid? TermId { get; set; }
        public Guid? ModalityId { get; set; }
        public Guid? TypeModalityId { get; set; }
        public Guid? TypePostulantId { get; set; }
    }
}
