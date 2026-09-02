using ADMISION.ENTITIES.Constants;
using ADMISION.Services.Interfaces;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;

namespace admision.Controllers.Admin.InfrastructureController
{
    [Authorize(Roles = AppConstants.Roles.SuperAdmin + "," + AppConstants.Roles.Admin)]
    [Route("admin/infrastructure/exam-assignment")]
    public class ExamAssignmentController : Controller
    {
        private readonly IExamAssignmentService _service;
        private readonly IExamScheduleService _scheduleService;
        private readonly ITermService _terms;
        private readonly IModalityService _modalities;

        public ExamAssignmentController(
            IExamAssignmentService service,
            IExamScheduleService scheduleService,
            ITermService terms,
            IModalityService modalities)
        {
            _service = service;
            _scheduleService = scheduleService;
            _terms = terms;
            _modalities = modalities;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index(Guid? termId, Guid? modalityId, CancellationToken ct)
        {
            var terms = await _terms.GetAllAsync(ct);
            var selectedTerm = termId.HasValue
                ? terms.FirstOrDefault(t => t.Id == termId.Value)
                : terms.FirstOrDefault(t => t.IsActive) ?? terms.FirstOrDefault();

            ViewBag.Terms = terms;
            ViewBag.SelectedTermId = selectedTerm?.Id;

            var modalities = selectedTerm == null
                ? Array.Empty<ADMISION.ENTITIES.Models.Modality.Modality>()
                : await _modalities.GetEntitiesByTermAsync(selectedTerm.Id, ct);
            ViewBag.Modalities = modalities;

            var selectedModality = modalityId.HasValue
                ? modalities.FirstOrDefault(m => m.Id == modalityId.Value)
                : null;
            ViewBag.SelectedModalityId = selectedModality?.Id;
            ViewBag.SelectedModality = selectedModality;

            if (selectedModality != null)
            {
                var schedule = await _scheduleService.GetByModalityAsync(selectedModality.Id, ct);
                ViewBag.Schedule = schedule;

                if (schedule != null)
                {
                    var existing = await _service.CountByScheduleAsync(schedule.Id, ct);
                    ViewBag.ExistingAssignments = existing;

                    if (existing > 0)
                    {
                        ViewBag.Assignments = await _service.GetByScheduleAsync(schedule.Id, ct);
                    }
                }
            }

            return View("~/Pages/Admin/Infrastructure/ExamAssignment/Index.cshtml");
        }

        [HttpPost("preview")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Preview(Guid examScheduleId)
        {
            var summary = await _service.PreviewAsync(examScheduleId);
            return Json(summary);
        }

        [HttpPost("execute")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Execute(Guid examScheduleId, CancellationToken ct)
        {
            var schedule = await _scheduleService.GetByIdAsync(examScheduleId, ct);
            if (schedule == null)
            {
                TempData["Error"] = "Horario de examen no encontrado.";
                return RedirectToAction(nameof(Index));
            }

            var user = User.Identity?.Name ?? "Admin";
            var summary = await _service.ExecuteAsync(examScheduleId, user, ct);
            TempData["Success"] = $"Sorteo generado: {summary.TotalAsignadas} postulantes asignados.";
            return RedirectToAction(nameof(Index), new { termId = schedule.TermId, modalityId = schedule.ModalityId });
        }

        [HttpPost("clear")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Clear(Guid examScheduleId, CancellationToken ct)
        {
            var schedule = await _scheduleService.GetByIdAsync(examScheduleId, ct);
            if (schedule == null)
            {
                TempData["Error"] = "Horario de examen no encontrado.";
                return RedirectToAction(nameof(Index));
            }

            await _service.ClearAsync(examScheduleId, ct);
            TempData["Success"] = "Asignaciones del sorteo eliminadas. Puede volver a ejecutar.";
            return RedirectToAction(nameof(Index), new { termId = schedule.TermId, modalityId = schedule.ModalityId });
        }

        [HttpGet("export")]
        public async Task<IActionResult> Export(Guid examScheduleId, CancellationToken ct)
        {
            var data = await _service.GetExportDataAsync(examScheduleId, ct);
            if (data == null)
            {
                TempData["Error"] = "Horario de examen no encontrado.";
                return RedirectToAction(nameof(Index));
            }

            if (data.Assignments.Count == 0)
            {
                TempData["Error"] = "No hay asignaciones registradas para este sorteo.";
                var scheduleFallback = await _scheduleService.GetByIdAsync(examScheduleId, ct);
                return RedirectToAction(nameof(Index), new { termId = scheduleFallback?.TermId, modalityId = scheduleFallback?.ModalityId });
            }

            var bytes = BuildExcelExport(data);
            var safeName = string.Concat(data.Modality.Name.Where(c => char.IsLetterOrDigit(c) || c == '_' || c == '-')).ToUpper();
            if (string.IsNullOrWhiteSpace(safeName)) safeName = "MODALIDAD";
            var fileName = $"Sorteo_{safeName}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        private static byte[] BuildExcelExport(ExamAssignmentExportData data)
        {
            var modality = data.Modality;
            var assignments = data.Assignments;
            var culture = new CultureInfo("es-PE");

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Asignaciones");

            ws.Cell(1, 1).Value = "SORTEO DE SALONES";
            ws.Range(1, 1, 1, 12).Merge().Style
                .Font.SetBold(true).Font.SetFontSize(14)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                .Fill.SetBackgroundColor(XLColor.FromHtml("#1e3a8a"))
                .Font.SetFontColor(XLColor.White);

            ws.Cell(2, 1).Value = $"Modalidad: {modality.Name}";
            ws.Cell(2, 5).Value = $"Periodo: {modality.Term?.Name}";
            ws.Cell(2, 9).Value = modality.ExamDate.HasValue
                ? $"Examen: {modality.ExamDate.Value.ToString("dd/MM/yyyy", culture)}"
                : "Examen: —";
            ws.Range(2, 1, 2, 12).Style.Font.SetBold(true).Font.SetFontSize(10)
                .Fill.SetBackgroundColor(XLColor.FromHtml("#eff6ff"));

            var headers = new[] { "N°", "Pabellón", "Piso", "Salón", "Silla", "Carpeta", "Área", "Docente", "Código Postulante", "DNI", "Apellidos y Nombres", "Carrera" };
            for (int i = 0; i < headers.Length; i++)
            {
                var cell = ws.Cell(4, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.SetBold(true).Font.SetFontColor(XLColor.White)
                    .Fill.SetBackgroundColor(XLColor.FromHtml("#2563eb"))
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                    .Alignment.SetVertical(XLAlignmentVerticalValues.Center)
                    .Border.SetOutsideBorder(XLBorderStyleValues.Thin);
            }
            ws.Row(4).Height = 22;

            int row = 5;
            int idx = 1;
            foreach (var a in assignments)
            {
                ws.Cell(row, 1).Value = idx++;
                ws.Cell(row, 2).Value = $"{a.Classroom?.Pavilion?.Code} · {a.Classroom?.Pavilion?.Name}";
                ws.Cell(row, 3).Value = a.Classroom?.Floor;
                ws.Cell(row, 4).Value = a.Classroom?.Name;
                ws.Cell(row, 5).Value = a.SeatNumber;
                ws.Cell(row, 6).Value = a.FolderNumber;
                ws.Cell(row, 7).Value = a.TematicArea?.Code ?? "—";
                ws.Cell(row, 8).Value = a.Teacher?.User?.FullName ?? "Sin docente";
                ws.Cell(row, 9).Value = a.Inscription?.CodePostulant;
                ws.Cell(row, 10).Value = a.Inscription?.Postulant?.User?.Document;
                ws.Cell(row, 11).Value = $"{a.Inscription?.Postulant?.User?.FirstNameFather} " + $"{a.Inscription?.Postulant?.User?.FirstNameMother}, " + $"{a.Inscription?.Postulant?.User?.Name}";
                ws.Cell(row, 12).Value = a.Inscription?.Career?.Name;

                var rowRange = ws.Range(row, 1, row, headers.Length);
                rowRange.Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin)
                    .Border.SetInsideBorder(XLBorderStyleValues.Thin);
                if (idx % 2 == 0)
                    rowRange.Style.Fill.SetBackgroundColor(XLColor.FromHtml("#f8fafc"));

                ws.Cell(row, 1).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                ws.Cell(row, 3).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                ws.Cell(row, 5).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                ws.Cell(row, 6).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                ws.Cell(row, 7).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                ws.Cell(row, 9).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                ws.Cell(row, 10).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

                row++;
            }

            ws.Columns().AdjustToContents();
            ws.Column(11).Width = Math.Max(ws.Column(11).Width, 38);
            ws.Column(12).Width = Math.Max(ws.Column(12).Width, 32);
            ws.SheetView.FreezeRows(4);

            using var ms = new MemoryStream();
            workbook.SaveAs(ms);
            return ms.ToArray();
        }
    }
}
