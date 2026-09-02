using ADMISION.Models.ViewModels.Reports;
using ADMISION.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ADMISION.Services.Interfaces
{
    public interface IIngresantesReportService
    {
        Task<IngresantesReportViewModel> BuildAsync(IngresantesReportFilter filter, CancellationToken ct = default);
        Task<IngresantesReportViewModel> BuildAllAsync(IngresantesReportFilter filter, CancellationToken ct = default);
    }

    public class IngresantesReportFilter
    {
        public Guid? TermId { get; set; }
        public Guid? ModalityId { get; set; }
        public Guid? TypeModalityId { get; set; }
        public Guid? TypePostulantId { get; set; }
        public Guid? CareerId { get; set; }
        public Guid? TematicAreaId { get; set; }
        public string? SegundaCarrera { get; set; }
        public string? TipoReporte { get; set; } = "consolidado";
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }
}
