using ADMISION.ENTITIES.Constants;
using ADMISION.Models.ViewModels.Reports;
using ADMISION.Services.Interfaces;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ADMISION.Controllers.Admin.ReportsController
{
    [Authorize(Roles = AppConstants.Roles.SuperAdmin + "," + AppConstants.Roles.Admin + "," + AppConstants.Roles.Consultor)]
    [Route("admin/reportes")]
    public class TematicAreaReportController : Controller
    {
        private readonly ITematicAreaReportService _reports;
        private readonly ITermService _terms;
        private readonly IModalityService _modalities;
        private readonly ICatalogService _catalog;

        public TematicAreaReportController(
            ITematicAreaReportService reports,
            ITermService terms,
            IModalityService modalities,
            ICatalogService catalog)
        {
            _reports = reports;
            _terms = terms;
            _modalities = modalities;
            _catalog = catalog;
        }
        [HttpGet("postulantes-por-area")]
        public async Task<IActionResult> Index(
            Guid? termId, Guid? modalityId, Guid? typeModalityId, Guid? typePostulantId,
            CancellationToken ct = default)
        {
            var terms = await _terms.GetAllAsync(ct);
            var selectedTerm = termId.HasValue
                ? terms.FirstOrDefault(t => t.Id == termId.Value)
                : terms.FirstOrDefault(t => t.IsActive) ?? terms.FirstOrDefault();

            ViewBag.Terms = terms;
            ViewBag.Modalities = selectedTerm == null
                ? Array.Empty<ADMISION.ENTITIES.Models.Modality.Modality>()
                : await _modalities.GetEntitiesByTermAsync(selectedTerm.Id, ct);
            ViewBag.TypeModalities = modalityId.HasValue
                ? await _catalog.GetTypeModalitiesAsync(modalityId.Value, onlyActive: false, ct)
                : Array.Empty<TypeModalityOption>();
            ViewBag.TypePostulants = await _catalog.GetTypePostulantsAsync(ct);

            var report = await _reports.BuildAsync(new TematicAreaReportFilter
            {
                TermId = selectedTerm?.Id,
                ModalityId = modalityId,
                TypeModalityId = typeModalityId,
                TypePostulantId = typePostulantId
            }, ct);

            return View("~/Pages/Admin/Reports/TematicAreaReport/Index.cshtml", report);
        }

        [HttpGet("postulantes-por-area/export/excel")]
        public async Task<IActionResult> ExportExcel(
            Guid? termId, Guid? modalityId, Guid? typeModalityId, Guid? typePostulantId,
            CancellationToken ct = default)
        {
            var report = await _reports.BuildAsync(new TematicAreaReportFilter
            {
                TermId = termId,
                ModalityId = modalityId,
                TypeModalityId = typeModalityId,
                TypePostulantId = typePostulantId
            }, ct);

            var bytes = BuildExcel(report);
            var fileName = $"Reporte_Postulantes_Area_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        [HttpGet("postulantes-por-area/export/pdf")]
        public async Task<IActionResult> ExportPdf(
            Guid? termId, Guid? modalityId, Guid? typeModalityId, Guid? typePostulantId,
            CancellationToken ct = default)
        {
            var report = await _reports.BuildAsync(new TematicAreaReportFilter
            {
                TermId = termId,
                ModalityId = modalityId,
                TypeModalityId = typeModalityId,
                TypePostulantId = typePostulantId
            }, ct);

            var bytes = BuildPdf(report);
            var fileName = $"Reporte_Postulantes_Area_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
            return File(bytes, "application/pdf", fileName);
        }

        // ============ Builders de presentación (Excel + PDF) ============
        private static byte[] BuildExcel(TematicAreaReportViewModel report)
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Postulantes por Área");

            ws.Cell(1, 1).Value = "REPORTE DE POSTULANTES POR ÁREA TEMÁTICA";
            ws.Range(1, 1, 1, 4).Merge().Style
                .Font.SetBold(true).Font.SetFontSize(14)
                .Fill.SetBackgroundColor(XLColor.FromHtml("#1e3a8a"))
                .Font.SetFontColor(XLColor.White)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            int row = 3;
            ws.Cell(row, 1).Value = "Periodo:";
            ws.Cell(row, 2).Value = report.TermName ?? "(todos)";
            ws.Cell(row + 1, 1).Value = "Modalidad:";
            ws.Cell(row + 1, 2).Value = report.ModalityName ?? "(todas)";
            ws.Cell(row + 2, 1).Value = "Tipo de Modalidad:";
            ws.Cell(row + 2, 2).Value = report.TypeModalityName ?? "(todos)";
            ws.Cell(row + 3, 1).Value = "Tipo de Postulante:";
            ws.Cell(row + 3, 2).Value = report.TypePostulantName ?? "(todos)";
            ws.Range(row, 1, row + 3, 1).Style.Font.SetBold(true);

            row = 8;
            var headers = new[] { "Área / Carrera", "Código", "Nombre", "Inscritos" };
            for (int i = 0; i < headers.Length; i++)
            {
                var cell = ws.Cell(row, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.SetBold(true).Font.SetFontColor(XLColor.White)
                    .Fill.SetBackgroundColor(XLColor.FromHtml("#2563eb"))
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                    .Border.SetOutsideBorder(XLBorderStyleValues.Thin);
            }
            row++;

            foreach (var area in report.Areas)
            {
                ws.Cell(row, 1).Value = $"ÁREA: {area.AreaCode}";
                ws.Range(row, 1, row, 3).Merge().Style
                    .Font.SetBold(true)
                    .Fill.SetBackgroundColor(XLColor.FromHtml("#dbeafe"));
                ws.Cell(row, 4).Value = area.Subtotal;
                ws.Cell(row, 4).Style.Font.SetBold(true)
                    .Fill.SetBackgroundColor(XLColor.FromHtml("#dbeafe"))
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                ws.Range(row, 1, row, 4).Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin);
                row++;

                foreach (var c in area.Careers)
                {
                    ws.Cell(row, 1).Value = "";
                    ws.Cell(row, 2).Value = c.CareerCode;
                    ws.Cell(row, 3).Value = c.CareerName;
                    ws.Cell(row, 4).Value = c.Inscritos;
                    ws.Cell(row, 4).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                    ws.Range(row, 1, row, 4).Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin);
                    row++;
                }
            }

            ws.Cell(row, 1).Value = "TOTAL GENERAL";
            ws.Range(row, 1, row, 3).Merge().Style
                .Font.SetBold(true).Font.SetFontSize(12)
                .Fill.SetBackgroundColor(XLColor.FromHtml("#1e3a8a"))
                .Font.SetFontColor(XLColor.White)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);
            ws.Cell(row, 4).Value = report.TotalInscripciones;
            ws.Cell(row, 4).Style.Font.SetBold(true).Font.SetFontSize(12)
                .Fill.SetBackgroundColor(XLColor.FromHtml("#1e3a8a"))
                .Font.SetFontColor(XLColor.White)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            ws.Columns().AdjustToContents();
            ws.Column(3).Width = Math.Max(ws.Column(3).Width, 45);

            using var ms = new MemoryStream();
            workbook.SaveAs(ms);
            return ms.ToArray();
        }

        private static byte[] BuildPdf(TematicAreaReportViewModel report)
        {
            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(28);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily(Fonts.Calibri));

                    page.Header().Column(col =>
                    {
                        col.Item().Text("REPORTE DE POSTULANTES POR ÁREA TEMÁTICA")
                            .Bold().FontSize(14).FontColor("#1e3a8a");
                        col.Item().PaddingTop(2).Text($"Generado: {DateTime.Now:dd/MM/yyyy HH:mm}")
                            .FontSize(8).FontColor("#64748b");
                        col.Item().LineHorizontal(1).LineColor("#1e3a8a");
                    });

                    page.Content().PaddingVertical(8).Column(col =>
                    {
                        col.Spacing(8);

                        col.Item().Background("#eff6ff").Padding(8).Column(f =>
                        {
                            f.Spacing(2);
                            f.Item().Row(r =>
                            {
                                r.RelativeItem().Text(t => { t.Span("Periodo: ").Bold(); t.Span(report.TermName ?? "(todos)"); });
                                r.RelativeItem().Text(t => { t.Span("Modalidad: ").Bold(); t.Span(report.ModalityName ?? "(todas)"); });
                            });
                            f.Item().Row(r =>
                            {
                                r.RelativeItem().Text(t => { t.Span("Tipo Modalidad: ").Bold(); t.Span(report.TypeModalityName ?? "(todos)"); });
                                r.RelativeItem().Text(t => { t.Span("Tipo Postulante: ").Bold(); t.Span(report.TypePostulantName ?? "(todos)"); });
                            });
                        });

                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.ConstantColumn(70);
                                c.RelativeColumn(5);
                                c.ConstantColumn(70);
                            });

                            table.Header(h =>
                            {
                                h.Cell().Background("#2563eb").Padding(6).Text("Código").FontColor(Colors.White).Bold();
                                h.Cell().Background("#2563eb").Padding(6).Text("Descripción").FontColor(Colors.White).Bold();
                                h.Cell().Background("#2563eb").Padding(6).AlignCenter().Text("Inscritos").FontColor(Colors.White).Bold();
                            });

                            foreach (var area in report.Areas)
                            {
                                table.Cell().Background("#dbeafe").Padding(5).Text(area.AreaCode).Bold();
                                table.Cell().Background("#dbeafe").Padding(5).Text($"ÁREA TEMÁTICA — {area.AreaCode}").Bold();
                                table.Cell().Background("#dbeafe").Padding(5).AlignCenter().Text(area.Subtotal.ToString()).Bold();

                                foreach (var c in area.Careers)
                                {
                                    table.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(4).Text(c.CareerCode);
                                    table.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(4).Text(c.CareerName);
                                    table.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(4).AlignCenter().Text(c.Inscritos.ToString());
                                }
                            }

                            table.Cell().ColumnSpan(2).Background("#1e3a8a").Padding(8).AlignRight().Text("TOTAL GENERAL").FontColor(Colors.White).Bold().FontSize(11);
                            table.Cell().Background("#1e3a8a").Padding(8).AlignCenter().Text(report.TotalInscripciones.ToString()).FontColor(Colors.White).Bold().FontSize(11);
                        });
                    });

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Página ").FontSize(8).FontColor("#64748b");
                        x.CurrentPageNumber().FontSize(8).FontColor("#64748b");
                        x.Span(" de ").FontSize(8).FontColor("#64748b");
                        x.TotalPages().FontSize(8).FontColor("#64748b");
                    });
                });
            }).GeneratePdf();
        }
    }
}
