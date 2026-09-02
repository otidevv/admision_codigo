using ADMISION.Models.ViewModels.Reports;

namespace ADMISION.Services.Interfaces
{
    public interface IReportExportService
    {
        byte[] BuildGeneralExcel(List<GeneralReportItem> items);
        byte[] BuildGeneralPdf(List<GeneralReportItem> items);
        byte[] BuildEconomicoExcel(List<EconomicReportItem> items);
        byte[] BuildEconomicoPdf(List<EconomicReportItem> items);
        byte[] BuildVacantesExcel(VacantesReportViewModel vm);
        byte[] BuildVacantesPdf(VacantesReportViewModel vm);
        byte[] BuildSorteoAulasResumenExcel(SorteoAulasReportViewModel vm);
        byte[] BuildSorteoAulasResumenPdf(SorteoAulasReportViewModel vm);
        byte[] BuildSorteoAulasListadoExcel(SorteoAulasReportViewModel vm);
        byte[] BuildSorteoAulasListadoPdf(SorteoAulasReportViewModel vm);
        byte[] BuildAsistenciasExcel(AttendanceReportViewModel vm);
        byte[] BuildAsistenciasPdf(AttendanceReportViewModel vm);
    }
}
