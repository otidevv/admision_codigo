using ADMISION.ENTITIES.Constants;
using ADMISION.ENTITIES.Models.Exam;
using ADMISION.Models.ViewModels.Reports;
using ADMISION.Services.Interfaces;
using admision.Models.ViewModels.Api;
using ADMISION.Models.Shared;
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
    public class ReportsController : Controller
    {
        private readonly IGeneralReportService _generalReport;
        private readonly IEconomicReportService _economicReport;
        private readonly IVacantesReportService _vacantesReport;
        private readonly IIngresantesReportService _ingresantesReport;
        private readonly ISiriesReportService _siriesReport;
        private readonly ICepreReportService _cepreReport;
        private readonly IResultadosReportService _resultadosReport;
        private readonly ISorteoAulasReportService _sorteoAulasReport;
        private readonly IAttendanceReportService _attendanceReport;
        private readonly ITermService _terms;
        private readonly IModalityService _modalities;
        private readonly ICatalogService _catalog;
        private readonly IReportExportService _export;
        private readonly IConsolidadoConsultaService _consolidadoConsulta;

        public ReportsController(
            IGeneralReportService generalReport,
            IEconomicReportService economicReport,
            IVacantesReportService vacantesReport,
            IIngresantesReportService ingresantesReport,
            ISiriesReportService siriesReport,
            ICepreReportService cepreReport,
            IResultadosReportService resultadosReport,
            ISorteoAulasReportService sorteoAulasReport,
            IAttendanceReportService attendanceReport,
            ITermService terms,
            IModalityService modalities,
            ICatalogService catalog,
            IReportExportService export,
            IConsolidadoConsultaService consolidadoConsulta)
        {
            _generalReport = generalReport;
            _economicReport = economicReport;
            _vacantesReport = vacantesReport;
            _ingresantesReport = ingresantesReport;
            _siriesReport = siriesReport;
            _cepreReport = cepreReport;
            _resultadosReport = resultadosReport;
            _sorteoAulasReport = sorteoAulasReport;
            _attendanceReport = attendanceReport;
            _terms = terms;
            _modalities = modalities;
            _catalog = catalog;
            _export = export;
            _consolidadoConsulta = consolidadoConsulta;
        }

        [HttpGet("")]
        public IActionResult Index()
        {
            return View("~/Pages/Admin/Reports/Index.cshtml");
        }

        // ═══════════════════════════════════════════════════════
        // REPORTE GENERAL
        // ═══════════════════════════════════════════════════════
        [HttpGet("general")]
        public async Task<IActionResult> General(
            Guid? termId, Guid? modalityId, Guid? typeModalityId, Guid? typePostulantId,
            int page = 1, int pageSize = 50,
            CancellationToken ct = default)
        {
            var terms = await _terms.GetAllAsync(ct);
            var selectedTerm = termId.HasValue
                ? terms.FirstOrDefault(t => t.Id == termId.Value)
                : terms.FirstOrDefault(t => t.IsActive) ?? terms.FirstOrDefault();

            ViewBag.Terms = terms;
            ViewBag.Modalities = selectedTerm == null
                ? Array.Empty<ENTITIES.Models.Modality.Modality>()
                : await _modalities.GetEntitiesByTermAsync(selectedTerm.Id, ct);
            ViewBag.TypeModalities = modalityId.HasValue
                ? await _catalog.GetTypeModalitiesAsync(modalityId.Value, onlyActive: false, ct)
                : Array.Empty<TypeModalityOption>();
            ViewBag.TypePostulants = await _catalog.GetTypePostulantsAsync(ct);

            var report = await _generalReport.BuildAsync(new GeneralReportFilter
            {
                TermId = selectedTerm?.Id,
                ModalityId = modalityId,
                TypeModalityId = typeModalityId,
                TypePostulantId = typePostulantId,
                Page = page,
                PageSize = pageSize
            }, ct);

            return View("~/Pages/Admin/Reports/General/Index.cshtml", report);
        }

        [HttpGet("general/export/excel")]
        public async Task<IActionResult> ExportGeneralExcel(
            Guid? termId, Guid? modalityId, Guid? typeModalityId, Guid? typePostulantId,
            CancellationToken ct = default)
        {
            var items = await _generalReport.BuildAllAsync(new GeneralReportFilter
            {
                TermId = termId,
                ModalityId = modalityId,
                TypeModalityId = typeModalityId,
                TypePostulantId = typePostulantId
            }, ct);

            var bytes = _export.BuildGeneralExcel(items);
            var fileName = $"Reporte_General_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        [HttpGet("general/export/pdf")]
        public async Task<IActionResult> ExportGeneralPdf(
            Guid? termId, Guid? modalityId, Guid? typeModalityId, Guid? typePostulantId,
            CancellationToken ct = default)
        {
            var items = await _generalReport.BuildAllAsync(new GeneralReportFilter
            {
                TermId = termId,
                ModalityId = modalityId,
                TypeModalityId = typeModalityId,
                TypePostulantId = typePostulantId
            }, ct);

            var bytes = _export.BuildGeneralPdf(items);
            var fileName = $"Reporte_General_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
            return File(bytes, "application/pdf", fileName);
        }

        // ═══════════════════════════════════════════════════════
        // REPORTES PLACEHOLDER
        // ═══════════════════════════════════════════════════════
        // ═══════════════════════════════════════════════════════
        // REPORTE SORTEO DE AULAS
        // ═══════════════════════════════════════════════════════
        [HttpGet("sorteo-aulas")]
        public async Task<IActionResult> SorteoAulas(CancellationToken ct = default)
        {
            var terms = await _terms.GetAllAsync(ct);
            ViewBag.Terms = terms;
            ViewBag.Modalities = Array.Empty<ENTITIES.Models.Modality.Modality>();
            return View("~/Pages/Admin/Reports/SorteoAulas/Index.cshtml", new SorteoAulasReportViewModel());
        }

        [HttpGet("sorteo-aulas/modalities")]
        public async Task<IActionResult> SorteoAulasModalities(Guid termId, CancellationToken ct)
        {
            var modalities = await _modalities.GetEntitiesByTermAsync(termId, ct);
            return Json(modalities.Select(m => new { id = m.Id, name = m.Name }));
        }

        [HttpGet("sorteo-aulas/data")]
        public async Task<IActionResult> SorteoAulasData(Guid? termId, Guid? modalityId, CancellationToken ct = default)
        {
            if (!termId.HasValue || !modalityId.HasValue)
                return Json(new { hasData = false });

            var report = await _sorteoAulasReport.BuildAsync(new SorteoAulasReportFilter
            {
                TermId = termId.Value,
                ModalityId = modalityId.Value
            }, ct);

            return Json(new
            {
                hasData = report.HasData,
                termName = report.TermName,
                modalityName = report.ModalityName,
                summary = report.HasData ? new
                {
                    report.Summary.TotalAsignados,
                    report.Summary.TotalAulas,
                    report.Summary.TotalAforo,
                    porPabellon = report.Summary.PorPabellon.Select(p => new
                    {
                        p.PavilionCode,
                        p.PavilionName,
                        p.TotalAsignados,
                        TotalAforo = p.TotalAforo,
                        Groups = p.Groups.Select(g => new
                        {
                            g.GroupName,
                            g.TotalAsignados,
                            g.Capacidad,
                            Classrooms = g.Classrooms.Select(c => new
                            {
                                c.ClassroomName,
                                c.Capacidad,
                                c.Asignados,
                                c.Piso,
                                c.Docente,
                                c.AreaTematica
                            })
                        })
                    })
                } : null,
                details = report.HasData ? report.Details.Select(d => new
                {
                    d.Silla,
                    d.CodigoPostulante,
                    d.Apellidos,
                    d.Nombres,
                    d.Carrera,
                    d.Aula,
                    d.Pabellon,
                    FotoBase64 = d.PhotoBytes is { Length: > 0 }
                        ? "data:image/jpeg;base64," + Convert.ToBase64String(d.PhotoBytes)
                        : null
                }) : null
            });
        }

        [HttpGet("sorteo-aulas/export/resumen/excel")]
        public async Task<IActionResult> ExportSorteoAulasResumenExcel(Guid? termId, Guid? modalityId, CancellationToken ct = default)
        {
            if (!termId.HasValue || !modalityId.HasValue)
                return BadRequest("Seleccione periodo y modalidad.");

            var report = await _sorteoAulasReport.BuildAsync(new SorteoAulasReportFilter
            {
                TermId = termId.Value,
                ModalityId = modalityId.Value
            }, ct);

            var bytes = _export.BuildSorteoAulasResumenExcel(report);
            var fileName = $"SorteoAulas_Resumen_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        [HttpGet("sorteo-aulas/export/resumen/pdf")]
        public async Task<IActionResult> ExportSorteoAulasResumenPdf(Guid? termId, Guid? modalityId, CancellationToken ct = default)
        {
            if (!termId.HasValue || !modalityId.HasValue)
                return BadRequest("Seleccione periodo y modalidad.");

            var report = await _sorteoAulasReport.BuildAsync(new SorteoAulasReportFilter
            {
                TermId = termId.Value,
                ModalityId = modalityId.Value
            }, ct);

            var bytes = _export.BuildSorteoAulasResumenPdf(report);
            var fileName = $"SorteoAulas_Resumen_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
            return File(bytes, "application/pdf", fileName);
        }

        [HttpGet("sorteo-aulas/export/listado/pdf")]
        public async Task<IActionResult> ExportSorteoAulasListadoPdf(Guid? termId, Guid? modalityId, CancellationToken ct = default)
        {
            if (!termId.HasValue || !modalityId.HasValue)
                return BadRequest("Seleccione periodo y modalidad.");

            var report = await _sorteoAulasReport.BuildAsync(new SorteoAulasReportFilter
            {
                TermId = termId.Value,
                ModalityId = modalityId.Value
            }, ct);

            var bytes = _export.BuildSorteoAulasListadoPdf(report);
            var fileName = $"SorteoAulas_Listado_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
            return File(bytes, "application/pdf", fileName);
        }

        [HttpGet("sorteo-aulas/export/listado/excel")]
        public async Task<IActionResult> ExportSorteoAulasListadoExcel(Guid? termId, Guid? modalityId, CancellationToken ct = default)
        {
            if (!termId.HasValue || !modalityId.HasValue)
                return BadRequest("Seleccione periodo y modalidad.");

            var report = await _sorteoAulasReport.BuildAsync(new SorteoAulasReportFilter
            {
                TermId = termId.Value,
                ModalityId = modalityId.Value
            }, ct);

            var bytes = _export.BuildSorteoAulasListadoExcel(report);
            var fileName = $"SorteoAulas_Listado_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        [HttpGet("asistencias")]
        public async Task<IActionResult> Asistencias(
            Guid? termId, Guid? modalityId, string? attendanceStatus,
            CancellationToken ct = default)
        {
            var terms = await _terms.GetAllAsync(ct);
            var selectedTerm = termId.HasValue
                ? terms.FirstOrDefault(t => t.Id == termId.Value)
                : terms.FirstOrDefault(t => t.IsActive) ?? terms.FirstOrDefault();

            ViewBag.Terms = terms;
            ViewBag.Modalities = selectedTerm == null
                ? Array.Empty<ENTITIES.Models.Modality.Modality>()
                : await _modalities.GetEntitiesByTermAsync(selectedTerm.Id, ct);

            AttendanceReportViewModel report;
            if (selectedTerm != null)
            {
                var filter = new AttendanceReportFilter
                {
                    TermId = selectedTerm.Id,
                    ModalityId = modalityId,
                    AttendanceStatus = attendanceStatus ?? "all"
                };
                report = await _attendanceReport.BuildAsync(filter, ct);
            }
            else
            {
                report = new AttendanceReportViewModel();
            }

            return View("~/Pages/Admin/Reports/Asistencias/Index.cshtml", report);
        }

        [HttpGet("asistencias/data")]
        public async Task<IActionResult> AsistenciasData(
            Guid termId, Guid? modalityId, string? attendanceStatus,
            CancellationToken ct = default)
        {
            var filter = new AttendanceReportFilter
            {
                TermId = termId,
                ModalityId = modalityId,
                AttendanceStatus = attendanceStatus ?? "all"
            };
            var report = await _attendanceReport.BuildAsync(filter, ct);
            return Json(report);
        }

        [HttpGet("asistencias/export/excel")]
        public async Task<IActionResult> ExportAsistenciasExcel(
            Guid termId, Guid? modalityId, string? attendanceStatus,
            CancellationToken ct = default)
        {
            var filter = new AttendanceReportFilter
            {
                TermId = termId,
                ModalityId = modalityId,
                AttendanceStatus = attendanceStatus ?? "all"
            };
            var report = await _attendanceReport.BuildAsync(filter, ct);
            var bytes = _export.BuildAsistenciasExcel(report);
            var fileName = $"Reporte_Asistencias_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        [HttpGet("asistencias/export/pdf")]
        public async Task<IActionResult> ExportAsistenciasPdf(
            Guid termId, Guid? modalityId, string? attendanceStatus,
            CancellationToken ct = default)
        {
            var filter = new AttendanceReportFilter
            {
                TermId = termId,
                ModalityId = modalityId,
                AttendanceStatus = attendanceStatus ?? "all"
            };
            var report = await _attendanceReport.BuildAsync(filter, ct);
            var bytes = _export.BuildAsistenciasPdf(report);
            var fileName = $"Reporte_Asistencias_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
            return File(bytes, "application/pdf", fileName);
        }

        //[HttpGet("sunedu")]
        //public IActionResult Sunedu()
        //{
        //    return View("~/Pages/Admin/Reports/Sunedu/Index.cshtml");
        //}

        [HttpGet("cepre")]
        public async Task<IActionResult> Cepre(Guid? termId, Guid? versionId, int page = 1, int pageSize = 50, CancellationToken ct = default)
        {
            var terms = await _terms.GetAllAsync(ct);
            var selectedTerm = termId.HasValue
                ? terms.FirstOrDefault(t => t.Id == termId.Value)
                : terms.FirstOrDefault(t => t.IsActive) ?? terms.FirstOrDefault();

            ViewBag.Terms = terms;
            ViewBag.Versions = selectedTerm == null
                ? new List<ADMISION.ENTITIES.Models.Exam.CepreImportVersion>()
                : await _cepreReport.GetVersionsAsync(selectedTerm.Id, ct);

            var report = await _cepreReport.BuildAsync(new CepreReportFilter
            {
                TermId = selectedTerm?.Id,
                VersionId = versionId,
                Page = page,
                PageSize = pageSize
            }, ct);

            return View("~/Pages/Admin/Reports/Cepre/Index.cshtml", report);
        }

        [HttpGet("cepre/export/excel")]
        public async Task<IActionResult> ExportCepreExcel(Guid? termId, Guid? versionId, CancellationToken ct = default)
        {
            var report = await _cepreReport.BuildAllAsync(new CepreReportFilter { TermId = termId, VersionId = versionId }, ct);

            var bytes = BuildCepreExcel(report);
            var fileName = $"Reporte_CEPRE_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        // ============ Builders de presentación (Excel) — CEPRE ============
        private static byte[] BuildCepreExcel(Models.ViewModels.Reports.CepreReportViewModel report)
        {
            using var workbook = new ClosedXML.Excel.XLWorkbook();
            var ws = workbook.Worksheets.Add("CEPRE");

            ws.Cell(1, 1).Value = "REPORTE CEPRE — DATOS IMPORTADOS";
            ws.Range(1, 1, 1, 40).Merge().Style
                .Font.SetBold(true).Font.SetFontSize(14)
                .Fill.SetBackgroundColor(ClosedXML.Excel.XLColor.FromHtml("#1e3a8a"))
                .Font.SetFontColor(ClosedXML.Excel.XLColor.White)
                .Alignment.SetHorizontal(ClosedXML.Excel.XLAlignmentHorizontalValues.Center);

            int row = 3;
            ws.Cell(row, 1).Value = "Periodo:";
            ws.Cell(row, 2).Value = report.TermName ?? "(todos)";
            ws.Cell(row + 1, 1).Value = "Versión:";
            ws.Cell(row + 1, 2).Value = report.VersionLabel ?? "-";
            ws.Cell(row + 2, 1).Value = "Registros:";
            ws.Cell(row + 2, 2).Value = report.TotalRecords;
            ws.Range(row, 1, row + 2, 1).Style.Font.SetBold(true);

            row = 7;
            var headers = new[]
            {
                "NRO", "CICLO", "CODIGO", "DNI", "TIPO DE DOCUMENTO", "APELLIDO PATERNO", "APELLIDO MATERNO",
                "NOMBRES", "APELLIDOS Y NOMBRES", "SEXO", "FECHA DE NACIMIENTO", "DIRECCIÓN", "ESTADO CIVIL",
                "AÑO DE EGRESO", "CORREO", "CELULAR", "COLEGIO", "NOMBRE DEL COLEGIO", "UBIGEO COLEGIO",
                "DIRECCIÓN COLEGIO", "UBIGEO", "DEPARTAMENTO", "PROVINCIA", "DISTRITO", "LUGAR DE NACIMIENTO",
                "MODALIDAD", "CÓDIGO CARRERA", "CARRERA PROFESIONAL", "GRUPO", "MODALIDAD DE PAGO", "MONTO",
                "PUNTAJE 01", "NOTA 01", "PUNTAJE 02", "NOTA 02", "PUNTAJE 03", "NOTA 03", "NOTA FINAL",
                "PUNTAJE", "ESTADO"
            };
            for (int i = 0; i < headers.Length; i++)
            {
                var cell = ws.Cell(row, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.SetBold(true).Font.SetFontColor(ClosedXML.Excel.XLColor.White)
                    .Fill.SetBackgroundColor(ClosedXML.Excel.XLColor.FromHtml("#2563eb"))
                    .Alignment.SetHorizontal(ClosedXML.Excel.XLAlignmentHorizontalValues.Center)
                    .Border.SetOutsideBorder(ClosedXML.Excel.XLBorderStyleValues.Thin);
            }
            row++;

            foreach (var item in report.Items)
            {
                ws.Cell(row, 1).Value = item.Nro;
                ws.Cell(row, 2).Value = item.Ciclo;
                ws.Cell(row, 3).Value = item.Codigo;
                ws.Cell(row, 4).Value = item.Dni;
                ws.Cell(row, 5).Value = item.TDocumento;
                ws.Cell(row, 6).Value = item.Apaterno;
                ws.Cell(row, 7).Value = item.Amaterno;
                ws.Cell(row, 8).Value = item.Nombres;
                ws.Cell(row, 9).Value = item.ApellidosNombres;
                ws.Cell(row, 10).Value = item.Sexo;
                ws.Cell(row, 11).Value = item.FechaNacimiento;
                ws.Cell(row, 12).Value = item.Direccion;
                ws.Cell(row, 13).Value = item.EstadoCivil;
                ws.Cell(row, 14).Value = item.AnioEgreso;
                ws.Cell(row, 15).Value = item.Correo;
                ws.Cell(row, 16).Value = item.Celular;
                ws.Cell(row, 17).Value = item.Colegio;
                ws.Cell(row, 18).Value = item.NombreColegio;
                ws.Cell(row, 19).Value = item.UbigeoColegio;
                ws.Cell(row, 20).Value = item.DireccionColegio;
                ws.Cell(row, 21).Value = item.Ubigeo;
                ws.Cell(row, 22).Value = item.Departamento;
                ws.Cell(row, 23).Value = item.Provincia;
                ws.Cell(row, 24).Value = item.Distrito;
                ws.Cell(row, 25).Value = item.LugarNacimiento;
                ws.Cell(row, 26).Value = item.Modalidad;
                ws.Cell(row, 27).Value = item.CodigoCarrera;
                ws.Cell(row, 28).Value = item.CarreraProfesional;
                ws.Cell(row, 29).Value = item.Grupo;
                ws.Cell(row, 30).Value = item.ModalidadPago;
                ws.Cell(row, 31).Value = item.Monto;
                ws.Cell(row, 32).Value = item.Puntaje01;
                ws.Cell(row, 33).Value = item.Nota01;
                ws.Cell(row, 34).Value = item.Puntaje02;
                ws.Cell(row, 35).Value = item.Nota02;
                ws.Cell(row, 36).Value = item.Puntaje03;
                ws.Cell(row, 37).Value = item.Nota03;
                ws.Cell(row, 38).Value = item.NotaFinal;
                ws.Cell(row, 39).Value = item.Puntaje;
                ws.Cell(row, 40).Value = item.Estado;
                ws.Range(row, 1, row, 40).Style.Border.SetOutsideBorder(ClosedXML.Excel.XLBorderStyleValues.Thin);
                row++;
            }

            ws.Cell(row, 1).Value = "TOTAL REGISTROS";
            ws.Range(row, 1, row, 39).Merge().Style
                .Font.SetBold(true).Font.SetFontSize(12)
                .Fill.SetBackgroundColor(ClosedXML.Excel.XLColor.FromHtml("#1e3a8a"))
                .Font.SetFontColor(ClosedXML.Excel.XLColor.White)
                .Alignment.SetHorizontal(ClosedXML.Excel.XLAlignmentHorizontalValues.Right);
            ws.Cell(row, 40).Value = report.TotalRecords;
            ws.Cell(row, 40).Style.Font.SetBold(true).Font.SetFontSize(12)
                .Fill.SetBackgroundColor(ClosedXML.Excel.XLColor.FromHtml("#1e3a8a"))
                .Font.SetFontColor(ClosedXML.Excel.XLColor.White)
                .Alignment.SetHorizontal(ClosedXML.Excel.XLAlignmentHorizontalValues.Center);

            ws.Columns().AdjustToContents();
            ws.Column(4).Width = Math.Max(ws.Column(4).Width, 14);
            ws.Column(5).Width = Math.Max(ws.Column(5).Width, 18);
            ws.Column(6).Width = Math.Max(ws.Column(6).Width, 22);
            ws.Column(7).Width = Math.Max(ws.Column(7).Width, 22);
            ws.Column(8).Width = Math.Max(ws.Column(8).Width, 22);
            ws.Column(9).Width = Math.Max(ws.Column(9).Width, 30);
            ws.Column(12).Width = Math.Max(ws.Column(12).Width, 30);
            ws.Column(15).Width = Math.Max(ws.Column(15).Width, 30);
            ws.Column(18).Width = Math.Max(ws.Column(18).Width, 30);
            ws.Column(20).Width = Math.Max(ws.Column(20).Width, 30);
            ws.Column(28).Width = Math.Max(ws.Column(28).Width, 30);

            using var ms = new System.IO.MemoryStream();
            workbook.SaveAs(ms);
            return ms.ToArray();
        }

        // ═══════════════════════════════════════════════════════
        // REPORTE DE RESULTADOS
        // ═══════════════════════════════════════════════════════
        [HttpGet("resultados")]
        public async Task<IActionResult> Resultados(
            Guid? termId, Guid? modalityId, Guid? typeModalityId, Guid? typePostulantId, Guid? careerId,
            string? condicion,
            int page = 1, int pageSize = 50,
            CancellationToken ct = default)
        {
            var terms = await _terms.GetAllAsync(ct);
            var selectedTerm = termId.HasValue
                ? terms.FirstOrDefault(t => t.Id == termId.Value)
                : terms.FirstOrDefault(t => t.IsActive) ?? terms.FirstOrDefault();

            ViewBag.Terms = terms;
            ViewBag.Modalities = selectedTerm == null
                ? Array.Empty<ENTITIES.Models.Modality.Modality>()
                : await _modalities.GetEntitiesByTermAsync(selectedTerm.Id, ct);
            ViewBag.TypeModalities = modalityId.HasValue
                ? await _catalog.GetTypeModalitiesAsync(modalityId.Value, onlyActive: false, ct)
                : Array.Empty<TypeModalityOption>();
            ViewBag.TypePostulants = await _catalog.GetTypePostulantsAsync(ct);
            ViewBag.Careers = await _catalog.GetCareersAsync(ct: ct);
            ViewBag.Options = selectedTerm == null
                ? new ResultadosFilterOptions()
                : await _resultadosReport.GetFilterOptionsAsync(selectedTerm.Id, ct);

            var report = await _resultadosReport.BuildAsync(new ResultadosReportFilter
            {
                TermId = selectedTerm?.Id,
                ModalityId = modalityId,
                TypeModalityId = typeModalityId,
                TypePostulantId = typePostulantId,
                CareerId = careerId,
                Condicion = condicion,
                Page = page,
                PageSize = pageSize
            }, ct);

            return View("~/Pages/Admin/Reports/Resultados/Index.cshtml", report);
        }

        [HttpGet("resultados/export/excel")]
        public async Task<IActionResult> ExportResultadosExcel(
            Guid? termId, Guid? modalityId, Guid? typeModalityId, Guid? typePostulantId, Guid? careerId,
            string? condicion,
            CancellationToken ct = default)
        {
            var report = await _resultadosReport.BuildAllAsync(new ResultadosReportFilter
            {
                TermId = termId,
                ModalityId = modalityId,
                TypeModalityId = typeModalityId,
                TypePostulantId = typePostulantId,
                CareerId = careerId,
                Condicion = condicion
            }, ct);

            var bytes = BuildResultadosExcel(report);
            var fileName = $"Reporte_Resultados_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        // ============ Builders de presentación (Excel) — RESULTADOS ============
        private static byte[] BuildResultadosExcel(Models.ViewModels.Reports.ResultadosReportViewModel report)
        {
            using var workbook = new ClosedXML.Excel.XLWorkbook();
            var ws = workbook.Worksheets.Add("RESULTADOS");

            ws.Cell(1, 1).Value = "REPORTE DE RESULTADOS — POSTULANTES";
            ws.Range(1, 1, 1, 13).Merge().Style
                .Font.SetBold(true).Font.SetFontSize(14)
                .Fill.SetBackgroundColor(ClosedXML.Excel.XLColor.FromHtml("#1e3a8a"))
                .Font.SetFontColor(ClosedXML.Excel.XLColor.White)
                .Alignment.SetHorizontal(ClosedXML.Excel.XLAlignmentHorizontalValues.Center);

            int row = 3;
            ws.Cell(row, 1).Value = "Periodo:";
            ws.Cell(row, 2).Value = report.TermName ?? "(todos)";
            ws.Cell(row + 1, 1).Value = "Registros:";
            ws.Cell(row + 1, 2).Value = report.TotalRecords;
            ws.Range(row, 1, row + 1, 1).Style.Font.SetBold(true);

            row = 6;
            var headers = new[]
            {
                "NRO", "CÓDIGO", "APELLIDOS Y NOMBRES", "EXAMEN", "TIPO DE MODALIDAD", "TIPO DE POSTULANTE",
                "CARRERA PROFESIONAL", "GRUPO", "CORRECTAS", "BLANCAS", "PUNTAJE", "NOTA", "CONDICIÓN"
            };
            for (int i = 0; i < headers.Length; i++)
            {
                var cell = ws.Cell(row, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.SetBold(true).Font.SetFontColor(ClosedXML.Excel.XLColor.White)
                    .Fill.SetBackgroundColor(ClosedXML.Excel.XLColor.FromHtml("#2563eb"))
                    .Alignment.SetHorizontal(ClosedXML.Excel.XLAlignmentHorizontalValues.Center)
                    .Border.SetOutsideBorder(ClosedXML.Excel.XLBorderStyleValues.Thin);
            }
            row++;

            foreach (var item in report.Items)
            {
                ws.Cell(row, 1).Value = item.Nro;
                ws.Cell(row, 2).Value = item.Codigo;
                ws.Cell(row, 3).Value = item.ApellidosNombres;
                ws.Cell(row, 4).Value = item.Examen;
                ws.Cell(row, 5).Value = item.TipoModalidad;
                ws.Cell(row, 6).Value = item.TipoPostulante;
                ws.Cell(row, 7).Value = item.Carrera;
                ws.Cell(row, 8).Value = item.Grupo;
                ws.Cell(row, 9).Value = item.Correctas;
                ws.Cell(row, 10).Value = item.Blancas;
                ws.Cell(row, 11).Value = item.Puntaje;
                ws.Cell(row, 12).Value = item.Nota;
                ws.Cell(row, 13).Value = item.Condicion;
                ws.Range(row, 1, row, 13).Style.Border.SetOutsideBorder(ClosedXML.Excel.XLBorderStyleValues.Thin);
                row++;
            }

            ws.Cell(row, 1).Value = "TOTAL REGISTROS";
            ws.Range(row, 1, row, 12).Merge().Style
                .Font.SetBold(true).Font.SetFontSize(12)
                .Fill.SetBackgroundColor(ClosedXML.Excel.XLColor.FromHtml("#1e3a8a"))
                .Font.SetFontColor(ClosedXML.Excel.XLColor.White)
                .Alignment.SetHorizontal(ClosedXML.Excel.XLAlignmentHorizontalValues.Right);
            ws.Cell(row, 13).Value = report.TotalRecords;
            ws.Cell(row, 13).Style.Font.SetBold(true).Font.SetFontSize(12)
                .Fill.SetBackgroundColor(ClosedXML.Excel.XLColor.FromHtml("#1e3a8a"))
                .Font.SetFontColor(ClosedXML.Excel.XLColor.White)
                .Alignment.SetHorizontal(ClosedXML.Excel.XLAlignmentHorizontalValues.Center);

            ws.Columns().AdjustToContents();
            ws.Column(2).Width = Math.Max(ws.Column(2).Width, 16);
            ws.Column(3).Width = Math.Max(ws.Column(3).Width, 35);
            ws.Column(7).Width = Math.Max(ws.Column(7).Width, 30);

            using var ms = new System.IO.MemoryStream();
            workbook.SaveAs(ms);
            return ms.ToArray();
        }

        [HttpGet("ingresantes")]
        public async Task<IActionResult> Ingresantes(
            Guid? termId, Guid? modalityId, Guid? typeModalityId, Guid? typePostulantId,
            Guid? careerId, Guid? tematicAreaId, string? segundaCarrera, string? tipoReporte,
            int page = 1, int pageSize = 50,
            CancellationToken ct = default)
        {
            var terms = await _terms.GetAllAsync(ct);
            var selectedTerm = termId.HasValue
                ? terms.FirstOrDefault(t => t.Id == termId.Value)
                : terms.FirstOrDefault(t => t.IsActive) ?? terms.FirstOrDefault();

            ViewBag.Terms = terms;
            ViewBag.Modalities = selectedTerm == null
                ? Array.Empty<ENTITIES.Models.Modality.Modality>()
                : await _modalities.GetEntitiesByTermAsync(selectedTerm.Id, ct);
            ViewBag.TypeModalities = modalityId.HasValue
                ? await _catalog.GetTypeModalitiesAsync(modalityId.Value, onlyActive: false, ct)
                : Array.Empty<TypeModalityOption>();
            ViewBag.TypePostulants = await _catalog.GetTypePostulantsAsync(ct);
            ViewBag.Careers = await _catalog.GetCareersAsync(ct: ct);
            ViewBag.TematicAreas = selectedTerm != null
                ? await _catalog.GetTematicAreasByTermAsync(selectedTerm.Id, ct)
                : Array.Empty<CatalogOption>();

            var report = await _ingresantesReport.BuildAsync(new IngresantesReportFilter
            {
                TermId = selectedTerm?.Id,
                ModalityId = modalityId,
                TypeModalityId = typeModalityId,
                TypePostulantId = typePostulantId,
                CareerId = careerId,
                TematicAreaId = tematicAreaId,
                SegundaCarrera = segundaCarrera,
                TipoReporte = string.IsNullOrWhiteSpace(tipoReporte) ? "consolidado" : tipoReporte
            }, ct);

            return View("~/Pages/Admin/Reports/Ingresantes/Index.cshtml", report);
        }

        // Endpoint AJAX consumido por el DataTable_DataTable reutilizable (_DataTable.cshtml).
        // Orden por defecto: nombre de carrera (A–Z) y luego código de estudiante.
        [HttpGet("ingresantes/data")]
        public async Task<IActionResult> IngresantesData(
            Guid? termId, Guid? modalityId, Guid? typeModalityId, Guid? typePostulantId,
            Guid? careerId, Guid? tematicAreaId, string? segundaCarrera, string? tipoReporte,
            int page = 1, int pageSize = 50,
            CancellationToken ct = default)
        {
            var report = await _ingresantesReport.BuildAsync(new IngresantesReportFilter
            {
                TermId = termId,
                ModalityId = modalityId,
                TypeModalityId = typeModalityId,
                TypePostulantId = typePostulantId,
                CareerId = careerId,
                TematicAreaId = tematicAreaId,
                SegundaCarrera = segundaCarrera,
                TipoReporte = string.IsNullOrWhiteSpace(tipoReporte) ? "consolidado" : tipoReporte,
                Page = Math.Max(1, page),
                PageSize = Math.Clamp(pageSize, 1, 200)
            }, ct);

            return Json(new DataTableResponse<object>
            {
                Data = report.Items.Select(i => (object)new
                {
                    codigoEstudiante = i.CodigoEstudiante,
                    examen = i.Examen,
                    modalidad = i.TipoModalidad,
                    tipoPostulante = i.TipoPostulante,
                    nombreCompleto = $"{i.Apellidos} {i.Nombres}".Trim(),
                    carrera = i.CarreraProfesional,
                    tema = i.Tema,
                    nota = i.Nota?.ToString("N2") ?? "—",
                    estado = i.Estado,
                    segundaCarrera = i.SegundaCarreraText
                }).ToList(),
                TotalItems = report.TotalIngresantes,
                TotalPages = Math.Max(1, (int)Math.Ceiling((double)report.TotalIngresantes / Math.Max(1, pageSize))),
                PageSize = pageSize,
                CurrentPage = report.Page
            });
        }

        [HttpGet("ingresantes/export/excel")]
        public async Task<IActionResult> ExportIngresantesExcel(
            Guid? termId, Guid? modalityId, Guid? typeModalityId, Guid? typePostulantId,
            Guid? careerId, Guid? tematicAreaId, string? segundaCarrera, string? tipoReporte,
            CancellationToken ct = default)
        {
            var report = await _ingresantesReport.BuildAllAsync(new IngresantesReportFilter
            {
                TermId = termId,
                ModalityId = modalityId,
                TypeModalityId = typeModalityId,
                TypePostulantId = typePostulantId,
                CareerId = careerId,
                TematicAreaId = tematicAreaId,
                SegundaCarrera = segundaCarrera,
                TipoReporte = string.IsNullOrWhiteSpace(tipoReporte) ? "consolidado" : tipoReporte
            }, ct);

            var prefix = report.TipoReporte == "preliminar" ? "Preliminar" : "Consolidado";
            var bytes = BuildIngresantesExcel(report);
            var fileName = $"Reporte_Ingresantes_{prefix}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        [HttpGet("ingresantes/export/pdf")]
        public async Task<IActionResult> ExportIngresantesPdf(
            Guid? termId, Guid? modalityId, Guid? typeModalityId, Guid? typePostulantId,
            Guid? careerId, Guid? tematicAreaId, string? segundaCarrera, string? tipoReporte,
            CancellationToken ct = default)
        {
            var report = await _ingresantesReport.BuildAllAsync(new IngresantesReportFilter
            {
                TermId = termId,
                ModalityId = modalityId,
                TypeModalityId = typeModalityId,
                TypePostulantId = typePostulantId,
                CareerId = careerId,
                TematicAreaId = tematicAreaId,
                SegundaCarrera = segundaCarrera,
                TipoReporte = string.IsNullOrWhiteSpace(tipoReporte) ? "consolidado" : tipoReporte
            }, ct);

            var prefix = report.TipoReporte == "preliminar" ? "Preliminar" : "Consolidado";
            var bytes = BuildIngresantesPdf(report);
            var fileName = $"Reporte_Ingresantes_{prefix}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
            return File(bytes, "application/pdf", fileName);
        }

        [HttpGet("ingresantes/export/preliminar")]
        public async Task<IActionResult> ExportPreliminarExcel(
            Guid? termId, Guid? modalityId, Guid? typeModalityId, Guid? typePostulantId,
            Guid? careerId, Guid? tematicAreaId, string? segundaCarrera,
            CancellationToken ct = default)
        {
            var report = await _ingresantesReport.BuildAllAsync(new IngresantesReportFilter
            {
                TermId = termId,
                ModalityId = modalityId,
                TypeModalityId = typeModalityId,
                TypePostulantId = typePostulantId,
                CareerId = careerId,
                TematicAreaId = tematicAreaId,
                SegundaCarrera = segundaCarrera,
                TipoReporte = "preliminar"
            }, ct);

            var bytes = BuildIngresantesExcel(report);
            var fileName = $"Preliminar_Ingresantes_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        [HttpGet("ingresantes/export/consolidado")]
        public async Task<IActionResult> ExportConsolidadoExcel(Guid? termId, CancellationToken ct = default)
        {
            var version = termId.HasValue
                ? await _consolidadoConsulta.GetLatestVersionByTermAsync(termId.Value, ct)
                : await _consolidadoConsulta.GetLatestVersionAsync(ct);

            if (version == null)
            {
                TempData["Error"] = "No se encontró un consolidado de la última versión activa para el período seleccionado.";
                return RedirectToAction(nameof(Ingresantes), new { termId });
            }

            var records = await _consolidadoConsulta.GetRecordsByVersionAsync(version.Id, ct);
            var bytes = BuildConsolidadoExcel(records, version);
            var fileName = $"Consolidado_Ingresantes_V{version.VersionNumber}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        // ═══════════════════════════════════════════════════════
        // REPORTE SIRIES (SIRIES)
        // ═══════════════════════════════════════════════════════
        [HttpGet("siries")]
        public async Task<IActionResult> Siries(Guid? termId, CancellationToken ct = default)
        {
            var terms = await _terms.GetAllAsync(ct);
            var selectedTerm = termId.HasValue
                ? terms.FirstOrDefault(t => t.Id == termId.Value)
                : terms.FirstOrDefault(t => t.IsActive) ?? terms.FirstOrDefault();

            ViewBag.Terms = terms;

            var report = await _siriesReport.BuildAsync(new SiriesReportFilter
            {
                TermId = selectedTerm?.Id
            }, ct);

            return View("~/Pages/Admin/Reports/Siries/Index.cshtml", report);
        }

        [HttpGet("siries/export/excel")]
        public async Task<IActionResult> ExportSiriesExcel(Guid? termId, CancellationToken ct = default)
        {
            var report = await _siriesReport.BuildAsync(new SiriesReportFilter { TermId = termId }, ct);

            var bytes = BuildSiriesExcel(report);
            var fileName = $"Reporte_SIRIES_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        [HttpGet("siries/export/pdf")]
        public async Task<IActionResult> ExportSiriesPdf(Guid? termId, CancellationToken ct = default)
        {
            var report = await _siriesReport.BuildAsync(new SiriesReportFilter { TermId = termId }, ct);

            var bytes = BuildSiriesPdf(report);
            var fileName = $"Reporte_SIRIES_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
            return File(bytes, "application/pdf", fileName);
        }

        // ============ Builders de presentación (Excel + PDF) — SIRIES ============
        private static byte[] BuildSiriesExcel(Models.ViewModels.Reports.SiriesReportViewModel report)
        {
            using var workbook = new ClosedXML.Excel.XLWorkbook();
            var ws = workbook.Worksheets.Add("SIRIES");

            ws.Cell(1, 1).Value = "REPORTE SIRIES — POSTULANTES E INGRESANTES";
            ws.Range(1, 1, 1, 22).Merge().Style
                .Font.SetBold(true).Font.SetFontSize(14)
                .Fill.SetBackgroundColor(ClosedXML.Excel.XLColor.FromHtml("#1e3a8a"))
                .Font.SetFontColor(ClosedXML.Excel.XLColor.White)
                .Alignment.SetHorizontal(ClosedXML.Excel.XLAlignmentHorizontalValues.Center);

            int row = 3;
            ws.Cell(row, 1).Value = "Periodo:";
            ws.Cell(row, 2).Value = report.TermName ?? "(todos)";
            ws.Cell(row + 1, 1).Value = "Postulantes:";
            ws.Cell(row + 1, 2).Value = report.TotalPostulantes;
            ws.Cell(row + 2, 1).Value = "Ingresantes:";
            ws.Cell(row + 2, 2).Value = report.TotalIngresantes;
            ws.Range(row, 1, row + 2, 1).Style.Font.SetBold(true);

            row = 7;
            var headers = new[]
            {
                "TIPO DE DOCUMENTO", "NÚMERO DE DOCUMENTO", "APELLIDO PATERNO", "APELLIDO MATERNO", "NOMBRES",
                "GÉNERO", "FECHA DE NACIMIENTO", "DISCAPACIDAD", "PERIODO", "LOCAL",
                "CARRERA DE PRIMERA OPCIÓN", "CARRERA DE SEGUNDA OPCIÓN", "MODALIDAD DE ADMISIÓN", "MODALIDAD DE ESTUDIOS", "PUNTAJE",
                "¿ES INGRESANTE?", "CARRERA DE INGRESO", "IDENTIDAD ÉTNICA", "CORREO INSTITUCIONAL", "CORREO PERSONAL",
                "TELÉFONO", "CELULAR"
            };
            for (int i = 0; i < headers.Length; i++)
            {
                var cell = ws.Cell(row, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.SetBold(true).Font.SetFontColor(ClosedXML.Excel.XLColor.White)
                    .Fill.SetBackgroundColor(ClosedXML.Excel.XLColor.FromHtml("#2563eb"))
                    .Alignment.SetHorizontal(ClosedXML.Excel.XLAlignmentHorizontalValues.Center)
                    .Border.SetOutsideBorder(ClosedXML.Excel.XLBorderStyleValues.Thin);
            }
            row++;

            foreach (var item in report.Items)
            {
                ws.Cell(row, 1).Value = item.TipoDocumento;
                ws.Cell(row, 2).Value = item.NumeroDocumento;
                ws.Cell(row, 3).Value = item.ApellidoPaterno;
                ws.Cell(row, 4).Value = item.ApellidoMaterno;
                ws.Cell(row, 5).Value = item.Nombres;
                ws.Cell(row, 6).Value = item.Genero;
                ws.Cell(row, 7).Value = item.FechaNacimiento;
                ws.Cell(row, 8).Value = item.Discapacidad;
                ws.Cell(row, 9).Value = item.Periodo;
                ws.Cell(row, 10).Value = item.Local;
                ws.Cell(row, 11).Value = item.CarreraPrimeraOpcion;
                ws.Cell(row, 12).Value = item.CarreraSegundaOpcion;
                ws.Cell(row, 13).Value = item.ModalidadAdmision;
                ws.Cell(row, 14).Value = item.ModalidadEstudios;
                ws.Cell(row, 15).Value = item.Puntaje;
                ws.Cell(row, 16).Value = item.EsIngresante;
                ws.Cell(row, 17).Value = item.CarreraIngreso;
                ws.Cell(row, 18).Value = item.IdentidadEtnica;
                ws.Cell(row, 19).Value = item.CorreoInstitucional;
                ws.Cell(row, 20).Value = item.CorreoPersonal;
                ws.Cell(row, 21).Value = item.Telefono;
                ws.Cell(row, 22).Value = item.Celular;
                ws.Range(row, 1, row, 22).Style.Border.SetOutsideBorder(ClosedXML.Excel.XLBorderStyleValues.Thin);
                row++;
            }

            ws.Cell(row, 1).Value = "TOTAL POSTULANTES";
            ws.Range(row, 1, row, 15).Merge().Style
                .Font.SetBold(true).Font.SetFontSize(12)
                .Fill.SetBackgroundColor(ClosedXML.Excel.XLColor.FromHtml("#1e3a8a"))
                .Font.SetFontColor(ClosedXML.Excel.XLColor.White)
                .Alignment.SetHorizontal(ClosedXML.Excel.XLAlignmentHorizontalValues.Right);
            ws.Cell(row, 16).Value = report.TotalPostulantes;
            ws.Cell(row, 16).Style.Font.SetBold(true).Font.SetFontSize(12)
                .Fill.SetBackgroundColor(ClosedXML.Excel.XLColor.FromHtml("#1e3a8a"))
                .Font.SetFontColor(ClosedXML.Excel.XLColor.White)
                .Alignment.SetHorizontal(ClosedXML.Excel.XLAlignmentHorizontalValues.Center);

            ws.Columns().AdjustToContents();
            ws.Column(5).Width = Math.Max(ws.Column(5).Width, 30);
            ws.Column(8).Width = Math.Max(ws.Column(8).Width, 30);
            ws.Column(11).Width = Math.Max(ws.Column(11).Width, 35);
            ws.Column(17).Width = Math.Max(ws.Column(17).Width, 30);
            ws.Column(19).Width = Math.Max(ws.Column(19).Width, 35);

            using var ms = new System.IO.MemoryStream();
            workbook.SaveAs(ms);
            return ms.ToArray();
        }

        private static byte[] BuildSiriesPdf(Models.ViewModels.Reports.SiriesReportViewModel report)
        {
            return QuestPDF.Fluent.Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(QuestPDF.Helpers.PageSizes.A3.Landscape());
                    page.Margin(24);
                    page.DefaultTextStyle(x => x.FontSize(7).FontFamily(QuestPDF.Helpers.Fonts.Calibri));

                    page.Header().Column(col =>
                    {
                        col.Item().Text("REPORTE SIRIES — POSTULANTES E INGRESANTES")
                            .Bold().FontSize(13).FontColor("#1e3a8a");
                        col.Item().PaddingTop(2).Text($"Generado: {DateTime.Now:dd/MM/yyyy HH:mm}")
                            .FontSize(8).FontColor("#64748b");
                        col.Item().LineHorizontal(1).LineColor("#1e3a8a");
                    });

                    page.Content().PaddingVertical(6).Column(col =>
                    {
                        col.Spacing(6);

                        col.Item().Background("#eff6ff").Padding(6).Row(r =>
                        {
                            r.RelativeItem().Text(t => { t.Span("Periodo: ").Bold(); t.Span(report.TermName ?? "(todos)"); });
                            r.RelativeItem().Text(t => { t.Span("Postulantes: ").Bold(); t.Span(report.TotalPostulantes.ToString()); });
                            r.RelativeItem().Text(t => { t.Span("Ingresantes: ").Bold(); t.Span(report.TotalIngresantes.ToString()); });
                        });

                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.ConstantColumn(30);   // TIPO DOC
                                c.ConstantColumn(60);   // N° DOC
                                c.RelativeColumn(1);    // APELLIDO PATERNO
                                c.RelativeColumn(1);    // APELLIDO MATERNO
                                c.RelativeColumn(1);    // NOMBRES
                                c.ConstantColumn(28);   // GÉNERO
                                c.ConstantColumn(50);   // F. NACIMIENTO
                                c.RelativeColumn(1);    // DISCAPACIDAD
                                c.ConstantColumn(40);   // PERIODO
                                c.ConstantColumn(50);   // LOCAL
                                c.RelativeColumn(1);    // CARRERA 1ª
                                c.RelativeColumn(1);    // CARRERA 2ª
                                c.RelativeColumn(1);    // MOD. ADMISIÓN
                                c.ConstantColumn(40);   // MOD. ESTUDIOS
                                c.ConstantColumn(38);   // PUNTAJE
                                c.ConstantColumn(32);   // INGRESANTE
                                c.RelativeColumn(1);    // CARRERA INGRESO
                                c.ConstantColumn(45);   // ID. ÉTNICA
                                c.RelativeColumn(1);    // CORREO INST.
                                c.RelativeColumn(1);    // CORREO PERSONAL
                                c.ConstantColumn(35);   // TELÉFONO
                                c.ConstantColumn(60);   // CELULAR
                            });

                            table.Header(h =>
                            {
                                foreach (var hdr in new[]
                                {
                                    "TIPO DOC", "N° DOCUMENTO", "AP. PATERNO", "AP. MATERNO", "NOMBRES",
                                    "GÉNERO", "F. NACIMIENTO", "DISCAPACIDAD", "PERIODO", "LOCAL",
                                    "CARRERA 1ª", "CARRERA 2ª", "MOD. ADMISIÓN", "MOD. ESTUDIOS", "PUNTAJE",
                                    "INGRESA", "CARRERA INGRESO", "ID. ÉTNICA", "CORREO INST.", "CORREO PERSONAL",
                                    "TELÉFONO", "CELULAR"
                                })
                                {
                                    h.Cell().Background("#2563eb").Padding(3).Text(hdr).FontColor(QuestPDF.Helpers.Colors.White).Bold().FontSize(6);
                                }
                            });

                            foreach (var item in report.Items)
                            {
                                table.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(2).Text(item.TipoDocumento).FontSize(7);
                                table.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(2).Text(item.NumeroDocumento).FontSize(7);
                                table.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(2).Text(item.ApellidoPaterno).FontSize(7);
                                table.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(2).Text(item.ApellidoMaterno).FontSize(7);
                                table.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(2).Text(item.Nombres).FontSize(7);
                                table.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(2).Text(item.Genero).FontSize(7);
                                table.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(2).Text(item.FechaNacimiento).FontSize(7);
                                table.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(2).Text(item.Discapacidad).FontSize(7);
                                table.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(2).Text(item.Periodo).FontSize(7);
                                table.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(2).Text(item.Local).FontSize(7);
                                table.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(2).Text(item.CarreraPrimeraOpcion).FontSize(7);
                                table.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(2).Text(item.CarreraSegundaOpcion).FontSize(7);
                                table.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(2).Text(item.ModalidadAdmision).FontSize(7);
                                table.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(2).Text(item.ModalidadEstudios).FontSize(7);
                                table.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(2).Text(item.Puntaje).FontSize(7);
                                table.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(2).AlignCenter().Text(item.EsIngresante).FontSize(7);
                                table.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(2).Text(item.CarreraIngreso).FontSize(7);
                                table.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(2).Text(item.IdentidadEtnica).FontSize(7);
                                table.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(2).Text(item.CorreoInstitucional).FontSize(7);
                                table.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(2).Text(item.CorreoPersonal).FontSize(7);
                                table.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(2).Text(item.Telefono).FontSize(7);
                                table.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(2).Text(item.Celular).FontSize(7);
                            }

                            table.Cell().ColumnSpan(15).Background("#1e3a8a").Padding(4).AlignRight().Text("TOTAL POSTULANTES").FontColor(QuestPDF.Helpers.Colors.White).Bold().FontSize(7);
                            table.Cell().Background("#1e3a8a").Padding(4).AlignCenter().Text(report.TotalPostulantes.ToString()).FontColor(QuestPDF.Helpers.Colors.White).Bold().FontSize(7);
                        });
                    });

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Página ").FontSize(7).FontColor("#64748b");
                        x.CurrentPageNumber().FontSize(7).FontColor("#64748b");
                        x.Span(" de ").FontSize(7).FontColor("#64748b");
                        x.TotalPages().FontSize(7).FontColor("#64748b");
                    });
                });
            }).GeneratePdf();
        }

        // ============ Builders de presentación (Excel + PDF) — Ingresantes ============
        private static byte[] BuildIngresantesExcel(Models.ViewModels.Reports.IngresantesReportViewModel report)
        {
            using var workbook = new ClosedXML.Excel.XLWorkbook();
            var ws = workbook.Worksheets.Add("Ingresantes");

            ws.Cell(1, 1).Value = "REPORTE DE INGRESANTES";
            ws.Range(1, 1, 1, 10).Merge().Style
                .Font.SetBold(true).Font.SetFontSize(14)
                .Fill.SetBackgroundColor(ClosedXML.Excel.XLColor.FromHtml("#1e3a8a"))
                .Font.SetFontColor(ClosedXML.Excel.XLColor.White)
                .Alignment.SetHorizontal(ClosedXML.Excel.XLAlignmentHorizontalValues.Center);

            int row = 3;
            ws.Cell(row, 1).Value = "Periodo:";
            ws.Cell(row, 2).Value = report.TermName ?? "(todos)";
            ws.Cell(row + 1, 1).Value = "Modalidad:";
            ws.Cell(row + 1, 2).Value = report.ModalityName ?? "(todas)";
            ws.Cell(row + 2, 1).Value = "Tipo de Modalidad:";
            ws.Cell(row + 2, 2).Value = report.TypeModalityName ?? "(todos)";
            ws.Cell(row + 3, 1).Value = "Tipo de Postulante:";
            ws.Cell(row + 3, 2).Value = report.TypePostulantName ?? "(todos)";
            ws.Cell(row + 4, 1).Value = "Carrera:";
            ws.Cell(row + 4, 2).Value = report.CareerName ?? "(todas)";
            ws.Cell(row + 5, 1).Value = "Área Temática:";
            ws.Cell(row + 5, 2).Value = report.TematicAreaName ?? "(todas)";
            ws.Range(row, 1, row + 5, 1).Style.Font.SetBold(true);

            row = 10;
            var headers = new[] { "CÓDIGO", "EXAMEN", "MODALIDAD", "TIPO POSTULANTE", "APELLIDOS Y NOMBRES", "CARRERA PROFESIONAL", "TEMA", "NOTA", "ESTADO", "SEGUNDA CARRERA" };
            for (int i = 0; i < headers.Length; i++)
            {
                var cell = ws.Cell(row, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.SetBold(true).Font.SetFontColor(ClosedXML.Excel.XLColor.White)
                    .Fill.SetBackgroundColor(ClosedXML.Excel.XLColor.FromHtml("#2563eb"))
                    .Alignment.SetHorizontal(ClosedXML.Excel.XLAlignmentHorizontalValues.Center)
                    .Border.SetOutsideBorder(ClosedXML.Excel.XLBorderStyleValues.Thin);
            }
            row++;

            foreach (var item in report.Items)
            {
                ws.Cell(row, 1).Value = item.CodigoEstudiante;
                ws.Cell(row, 2).Value = item.Examen;
                ws.Cell(row, 3).Value = item.TipoModalidad;
                ws.Cell(row, 4).Value = item.TipoPostulante;
                ws.Cell(row, 5).Value = $"{item.Apellidos} {item.Nombres}";
                ws.Cell(row, 6).Value = item.CarreraProfesional;
                ws.Cell(row, 7).Value = item.Tema;
                ws.Cell(row, 8).Value = item.Nota?.ToString("N2") ?? "";
                ws.Cell(row, 9).Value = item.Estado;
                ws.Cell(row, 10).Value = item.SegundaCarreraText;
                ws.Range(row, 1, row, 10).Style.Border.SetOutsideBorder(ClosedXML.Excel.XLBorderStyleValues.Thin);
                row++;
            }

            ws.Cell(row, 1).Value = "TOTAL GENERAL";
            ws.Range(row, 1, row, 9).Merge().Style
                .Font.SetBold(true).Font.SetFontSize(12)
                .Fill.SetBackgroundColor(ClosedXML.Excel.XLColor.FromHtml("#1e3a8a"))
                .Font.SetFontColor(ClosedXML.Excel.XLColor.White)
                .Alignment.SetHorizontal(ClosedXML.Excel.XLAlignmentHorizontalValues.Right);
            ws.Cell(row, 10).Value = report.TotalIngresantes;
            ws.Cell(row, 10).Style.Font.SetBold(true).Font.SetFontSize(12)
                .Fill.SetBackgroundColor(ClosedXML.Excel.XLColor.FromHtml("#1e3a8a"))
                .Font.SetFontColor(ClosedXML.Excel.XLColor.White)
                .Alignment.SetHorizontal(ClosedXML.Excel.XLAlignmentHorizontalValues.Center);

            ws.Columns().AdjustToContents();
            ws.Column(5).Width = Math.Max(ws.Column(5).Width, 35);
            ws.Column(6).Width = Math.Max(ws.Column(6).Width, 35);

            using var ms = new System.IO.MemoryStream();
            workbook.SaveAs(ms);
            return ms.ToArray();
        }

        private static byte[] BuildIngresantesPdf(Models.ViewModels.Reports.IngresantesReportViewModel report)
        {
            return QuestPDF.Fluent.Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(QuestPDF.Helpers.PageSizes.A4.Landscape());
                    page.Margin(28);
                    page.DefaultTextStyle(x => x.FontSize(9).FontFamily(QuestPDF.Helpers.Fonts.Calibri));

                    page.Header().Column(col =>
                    {
                        col.Item().Text("REPORTE DE INGRESANTES")
                            .Bold().FontSize(14).FontColor("#1e3a8a");
                        col.Item().PaddingTop(2).Text($"Generado: {DateTime.Now:dd/MM/yyyy HH:mm}")
                            .FontSize(8).FontColor("#64748b");
                        col.Item().LineHorizontal(1).LineColor("#1e3a8a");
                    });

                    page.Content().PaddingVertical(8).Column(col =>
                    {
                        col.Spacing(6);

                        col.Item().Background("#eff6ff").Padding(8).Column(f =>
                        {
                            f.Spacing(2);
                            f.Item().Row(r =>
                            {
                                r.RelativeItem().Text(t => { t.Span("Periodo: ").Bold(); t.Span(report.TermName ?? "(todos)"); });
                                r.RelativeItem().Text(t => { t.Span("Modalidad: ").Bold(); t.Span(report.ModalityName ?? "(todas)"); });
                                r.RelativeItem().Text(t => { t.Span("Tipo Modalidad: ").Bold(); t.Span(report.TypeModalityName ?? "(todos)"); });
                            });
                            f.Item().Row(r =>
                            {
                                r.RelativeItem().Text(t => { t.Span("Tipo Postulante: ").Bold(); t.Span(report.TypePostulantName ?? "(todos)"); });
                                r.RelativeItem().Text(t => { t.Span("Carrera: ").Bold(); t.Span(report.CareerName ?? "(todas)"); });
                                r.RelativeItem().Text(t => { t.Span("Área Temática: ").Bold(); t.Span(report.TematicAreaName ?? "(todas)"); });
                            });
                        });

                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.ConstantColumn(75);   // CÓDIGO
                                c.RelativeColumn(1);    // EXAMEN
                                c.RelativeColumn(1);    // MODALIDAD
                                c.RelativeColumn(1);    // TIPO POSTULANTE
                                c.RelativeColumn(2);    // APELLIDOS Y NOMBRES
                                c.RelativeColumn(2);    // CARRERA PROFESIONAL
                                c.ConstantColumn(50);   // TEMA
                                c.ConstantColumn(50);   // NOTA
                                c.ConstantColumn(65);   // ESTADO
                                c.ConstantColumn(60);   // SEGUNDA CARRERA
                            });

                            table.Header(h =>
                            {
                                foreach (var hdr in new[] { "CÓDIGO", "EXAMEN", "MODALIDAD", "TIPO POST.", "APELLIDOS Y NOMBRES", "CARRERA PROF.", "TEMA", "NOTA", "ESTADO", "SEG. CARRERA" })
                                {
                                    h.Cell().Background("#2563eb").Padding(4).Text(hdr).FontColor(QuestPDF.Helpers.Colors.White).Bold().FontSize(8);
                                }
                            });

                            foreach (var item in report.Items)
                            {
                                table.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(3).Text(item.CodigoEstudiante).FontSize(8);
                                table.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(3).Text(item.Examen).FontSize(8);
                                table.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(3).Text(item.TipoModalidad).FontSize(8);
                                table.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(3).Text(item.TipoPostulante).FontSize(8);
                                table.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(3).Text($"{item.Apellidos} {item.Nombres}").FontSize(8);
                                table.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(3).Text(item.CarreraProfesional).FontSize(8);
                                table.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(3).Text(item.Tema).FontSize(8);
                                table.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(3).AlignCenter().Text(item.Nota?.ToString("N2") ?? "").FontSize(8);
                                table.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(3).Text(item.Estado).FontSize(8);
                                table.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(3).AlignCenter().Text(item.SegundaCarreraText).FontSize(8);
                            }

                            table.Cell().ColumnSpan(9).Background("#1e3a8a").Padding(6).AlignRight().Text("TOTAL").FontColor(QuestPDF.Helpers.Colors.White).Bold().FontSize(9);
                            table.Cell().Background("#1e3a8a").Padding(6).AlignCenter().Text(report.TotalIngresantes.ToString()).FontColor(QuestPDF.Helpers.Colors.White).Bold().FontSize(9);
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

        private static byte[] BuildConsolidadoExcel(List<ConsolidadoIngresantesRecordDto> records, ConsolidadoIngresantesVersion version)
        {
            using var workbook = new ClosedXML.Excel.XLWorkbook();
            var ws = workbook.Worksheets.Add("Consolidado");

            ws.Cell(1, 1).Value = $"CONSOLIDADO DE INGRESANTES — VERSIÓN {version.VersionNumber}";
            ws.Range(1, 1, 1, 20).Merge().Style
                .Font.SetBold(true).Font.SetFontSize(14)
                .Fill.SetBackgroundColor(ClosedXML.Excel.XLColor.FromHtml("#1e3a8a"))
                .Font.SetFontColor(ClosedXML.Excel.XLColor.White)
                .Alignment.SetHorizontal(ClosedXML.Excel.XLAlignmentHorizontalValues.Center);

            int row = 3;
            ws.Cell(row, 1).Value = "Fecha generación:";
            ws.Cell(row, 2).Value = version.CreatedAt.ToString("dd/MM/yyyy HH:mm");
            ws.Cell(row + 1, 1).Value = "Registros:";
            ws.Cell(row + 1, 2).Value = records.Count;
            ws.Range(row, 1, row + 1, 1).Style.Font.SetBold(true);

            row = 6;
            var headers = new[]
            {
                "NRO", "CÓDIGO ESTUDIANTE", "CÓDIGO CARRERA", "SEGUNDA CARRERA", "SEMESTRE",
                "NOMBRES", "PATERNO", "MATERNO", "TIPO DOC", "DNI",
                "EMAIL", "CELULAR", "DIRECCIÓN", "F. NACIMIENTO", "SEXO",
                "ESTADO CIVIL", "UBIGEO", "TIPO POSTULANTE", "TIPO OBS", "OBSERVACIONES"
            };
            for (int i = 0; i < headers.Length; i++)
            {
                var cell = ws.Cell(row, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.SetBold(true).Font.SetFontColor(ClosedXML.Excel.XLColor.White)
                    .Fill.SetBackgroundColor(ClosedXML.Excel.XLColor.FromHtml("#2563eb"))
                    .Alignment.SetHorizontal(ClosedXML.Excel.XLAlignmentHorizontalValues.Center)
                    .Border.SetOutsideBorder(ClosedXML.Excel.XLBorderStyleValues.Thin);
            }
            row++;

            foreach (var r in records)
            {
                ws.Cell(row, 1).Value = r.Nro;
                ws.Cell(row, 2).Value = r.CodigoEstudiante;
                ws.Cell(row, 3).Value = r.CodigoCarrera;
                ws.Cell(row, 4).Value = r.SegundaCarrera;
                ws.Cell(row, 5).Value = r.Semestre;
                ws.Cell(row, 6).Value = r.Nombres;
                ws.Cell(row, 7).Value = r.Paterno;
                ws.Cell(row, 8).Value = r.Materno;
                ws.Cell(row, 9).Value = r.DType;
                ws.Cell(row, 10).Value = r.Dni;
                ws.Cell(row, 11).Value = r.Email;
                ws.Cell(row, 12).Value = r.Celular;
                ws.Cell(row, 13).Value = r.Direccion;
                ws.Cell(row, 14).Value = r.FechaNacimiento;
                ws.Cell(row, 15).Value = r.Sexo;
                ws.Cell(row, 16).Value = r.EstadoCivil;
                ws.Cell(row, 17).Value = r.Ubigeo;
                ws.Cell(row, 18).Value = r.TipoPostulante;
                ws.Cell(row, 19).Value = r.TipoObs;
                ws.Cell(row, 20).Value = r.Observaciones;
                ws.Range(row, 1, row, 20).Style.Border.SetOutsideBorder(ClosedXML.Excel.XLBorderStyleValues.Thin);
                row++;
            }

            ws.Cell(row, 1).Value = "TOTAL GENERAL";
            ws.Range(row, 1, row, 19).Merge().Style
                .Font.SetBold(true).Font.SetFontSize(12)
                .Fill.SetBackgroundColor(ClosedXML.Excel.XLColor.FromHtml("#1e3a8a"))
                .Font.SetFontColor(ClosedXML.Excel.XLColor.White)
                .Alignment.SetHorizontal(ClosedXML.Excel.XLAlignmentHorizontalValues.Right);
            ws.Cell(row, 20).Value = records.Count;
            ws.Cell(row, 20).Style.Font.SetBold(true).Font.SetFontSize(12)
                .Fill.SetBackgroundColor(ClosedXML.Excel.XLColor.FromHtml("#1e3a8a"))
                .Font.SetFontColor(ClosedXML.Excel.XLColor.White)
                .Alignment.SetHorizontal(ClosedXML.Excel.XLAlignmentHorizontalValues.Center);

            ws.Columns().AdjustToContents();
            ws.Column(6).Width = Math.Max(ws.Column(6).Width, 20);
            ws.Column(11).Width = Math.Max(ws.Column(11).Width, 25);
            ws.Column(13).Width = Math.Max(ws.Column(13).Width, 30);

            using var ms = new System.IO.MemoryStream();
            workbook.SaveAs(ms);
            return ms.ToArray();
        }

        [HttpGet("economico")]
        public async Task<IActionResult> Economico(
            Guid? termId, Guid? modalityId, Guid? typeModalityId, Guid? typePostulantId,
            int page = 1, int pageSize = 50,
            CancellationToken ct = default)
        {
            var terms = await _terms.GetAllAsync(ct);
            var selectedTerm = termId.HasValue
                ? terms.FirstOrDefault(t => t.Id == termId.Value)
                : terms.FirstOrDefault(t => t.IsActive) ?? terms.FirstOrDefault();

            ViewBag.Terms = terms;
            ViewBag.Modalities = selectedTerm == null
                ? Array.Empty<ENTITIES.Models.Modality.Modality>()
                : await _modalities.GetEntitiesByTermAsync(selectedTerm.Id, ct);
            ViewBag.TypeModalities = modalityId.HasValue
                ? await _catalog.GetTypeModalitiesAsync(modalityId.Value, onlyActive: false, ct)
                : Array.Empty<TypeModalityOption>();
            ViewBag.TypePostulants = await _catalog.GetTypePostulantsAsync(ct);

            var report = await _economicReport.BuildAsync(new EconomicReportFilter
            {
                TermId = selectedTerm?.Id,
                ModalityId = modalityId,
                TypeModalityId = typeModalityId,
                TypePostulantId = typePostulantId,
                Page = page,
                PageSize = pageSize
            }, ct);

            return View("~/Pages/Admin/Reports/Economico/Index.cshtml", report);
        }

        [HttpGet("economico/export/excel")]
        public async Task<IActionResult> ExportEconomicoExcel(
            Guid? termId, Guid? modalityId, Guid? typeModalityId, Guid? typePostulantId,
            CancellationToken ct = default)
        {
            var items = await _economicReport.BuildAllAsync(new EconomicReportFilter
            {
                TermId = termId,
                ModalityId = modalityId,
                TypeModalityId = typeModalityId,
                TypePostulantId = typePostulantId
            }, ct);

            var bytes = _export.BuildEconomicoExcel(items);
            var fileName = $"Reporte_Economico_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        [HttpGet("economico/export/pdf")]
        public async Task<IActionResult> ExportEconomicoPdf(
            Guid? termId, Guid? modalityId, Guid? typeModalityId, Guid? typePostulantId,
            CancellationToken ct = default)
        {
            var items = await _economicReport.BuildAllAsync(new EconomicReportFilter
            {
                TermId = termId,
                ModalityId = modalityId,
                TypeModalityId = typeModalityId,
                TypePostulantId = typePostulantId
            }, ct);

            var bytes = _export.BuildEconomicoPdf(items);
            var fileName = $"Reporte_Economico_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
            return File(bytes, "application/pdf", fileName);
        }

        [HttpGet("vacantes")]
        public async Task<IActionResult> Vacantes(
            Guid? termId, string? reportType,
            CancellationToken ct = default)
        {
            var terms = await _terms.GetAllAsync(ct);
            var selectedTerm = termId.HasValue
                ? terms.FirstOrDefault(t => t.Id == termId.Value)
                : terms.FirstOrDefault(t => t.IsActive) ?? terms.FirstOrDefault();

            ViewBag.Terms = terms;
            ViewBag.ReportType = reportType ?? "vacantes";

            var report = await _vacantesReport.BuildAsync(new VacantesReportFilter
            {
                TermId = selectedTerm?.Id,
                ReportType = reportType ?? "vacantes"
            }, ct);

            return View("~/Pages/Admin/Reports/Vacantes/Index.cshtml", report);
        }

        [HttpGet("vacantes/export/excel")]
        public async Task<IActionResult> ExportVacantesExcel(
            Guid? termId, string? reportType,
            CancellationToken ct = default)
        {
            var report = await _vacantesReport.BuildAsync(new VacantesReportFilter
            {
                TermId = termId,
                ReportType = reportType ?? "vacantes"
            }, ct);

            var bytes = _export.BuildVacantesExcel(report);
            var typeLabel = GetReportTypeLabel(report.ReportType);
            var fileName = $"Reporte_{typeLabel}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        [HttpGet("vacantes/export/pdf")]
        public async Task<IActionResult> ExportVacantesPdf(
            Guid? termId, string? reportType,
            CancellationToken ct = default)
        {
            var report = await _vacantesReport.BuildAsync(new VacantesReportFilter
            {
                TermId = termId,
                ReportType = reportType ?? "vacantes"
            }, ct);

            var bytes = _export.BuildVacantesPdf(report);
            var typeLabel = GetReportTypeLabel(report.ReportType);
            var fileName = $"Reporte_{typeLabel}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
            return File(bytes, "application/pdf", fileName);
        }

        private static string GetReportTypeLabel(string reportType) => reportType?.ToLowerInvariant() switch
        {
            "postulantes" => "Postulantes",
            "ingresantes" => "Ingresantes",
            "consolidado" => "Consolidado",
            _ => "Vacantes"
        };

        //[HttpGet("resumen-postulantes")]
        //public IActionResult ResumenPostulantes()
        //{
        //    return View("~/Pages/Admin/Reports/ResumenPostulantes/Index.cshtml");
        //}

        //[HttpGet("resumen-ingresantes")]
        //public IActionResult ResumenIngresantes()
        //{
        //    return View("~/Pages/Admin/Reports/ResumenIngresantes/Index.cshtml");
        //}
    }
}
