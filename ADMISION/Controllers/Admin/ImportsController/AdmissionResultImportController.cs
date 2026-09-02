using System.Text.Json;
using ADMISION.ENTITIES.Constants;
using ADMISION.ENTITIES.Data;
using ADMISION.Models.ViewModels.Admin;
using ADMISION.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ADMISION.Controllers.Admin.ImportsController
{
    [Route("admin/info-postulant/ingresantes")]
    [Authorize(Roles = AppConstants.Roles.SuperAdmin)]
    public class AdmissionResultImportController : Controller
    {
        private readonly IExamResultImportService _importService;
        private const string SessionKeyRows = "AdmissionImport_Rows";
        private const string SessionKeyTerm = "AdmissionImport_Term";
        private const string SessionKeyModality = "AdmissionImport_Modality";

        public AdmissionResultImportController(IExamResultImportService importService)
        {
            _importService = importService;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index(Guid? termId, Guid? modalityId, CancellationToken ct)
        {
            var model = new AdmissionImportViewModel
            {
                SelectedTermId = termId,
                SelectedModalityId = modalityId,
                IsSuperAdmin = User.IsInRole(AppConstants.Roles.SuperAdmin)
            };

            await LoadTermsAsync(model, ct);

            if (termId.HasValue)
            {
                await LoadModalitiesAsync(model, termId.Value, ct);
                model.ImportHistory = await _importService.GetAdmissionImportHistoryAsync(termId.Value, ct);
            }

            return View("~/Pages/Admin/Imports/AdmissionResultImport.cshtml", model);
        }

        [HttpGet("template")]
        public IActionResult Template()
        {
            var bytes = _importService.BuildAdmissionTemplate();
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Plantilla_Resultados_Modalidad.xlsx");
        }

        [HttpPost("preview")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Preview(IFormFile? ExcelFile, Guid? SelectedTermId, Guid? SelectedModalityId, CancellationToken ct)
        {
            var model = new AdmissionImportViewModel
            {
                SelectedTermId = SelectedTermId,
                SelectedModalityId = SelectedModalityId,
                IsSuperAdmin = User.IsInRole(AppConstants.Roles.SuperAdmin)
            };

            await LoadTermsAsync(model, ct);

            if (ExcelFile == null || ExcelFile.Length == 0)
            {
                TempData["Error"] = "Seleccione un archivo Excel válido.";
                return View("~/Pages/Admin/Imports/AdmissionResultImport.cshtml", model);
            }

            if (!SelectedTermId.HasValue)
            {
                TempData["Error"] = "Seleccione un período académico.";
                return View("~/Pages/Admin/Imports/AdmissionResultImport.cshtml", model);
            }

            await LoadModalitiesAsync(model, SelectedTermId.Value, ct);

            if (!SelectedModalityId.HasValue)
            {
                TempData["Error"] = "Seleccione una modalidad.";
                return View("~/Pages/Admin/Imports/AdmissionResultImport.cshtml", model);
            }

            using var stream = ExcelFile.OpenReadStream();
            model.Preview = await _importService.PreviewAdmissionAsync(stream, ExcelFile.FileName, SelectedTermId.Value, SelectedModalityId.Value, ct);

            model.ImportHistory = await _importService.GetAdmissionImportHistoryAsync(SelectedTermId.Value, ct);

            var validRows = model.Preview.Rows.Where(r => r.IsValid).ToList();
            HttpContext.Session.SetString(SessionKeyRows, JsonSerializer.Serialize(validRows));
            HttpContext.Session.SetString(SessionKeyTerm, SelectedTermId.Value.ToString());
            HttpContext.Session.SetString(SessionKeyModality, SelectedModalityId.Value.ToString());

            return View("~/Pages/Admin/Imports/AdmissionResultImport.cshtml", model);
        }

        [HttpPost("import")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Import(CancellationToken ct)
        {
            var rowsJson = HttpContext.Session.GetString(SessionKeyRows);
            var termIdStr = HttpContext.Session.GetString(SessionKeyTerm);
            var modalityIdStr = HttpContext.Session.GetString(SessionKeyModality);
            HttpContext.Session.Remove(SessionKeyRows);
            HttpContext.Session.Remove(SessionKeyTerm);
            HttpContext.Session.Remove(SessionKeyModality);

            var model = new AdmissionImportViewModel
            {
                IsSuperAdmin = User.IsInRole(AppConstants.Roles.SuperAdmin)
            };

            await LoadTermsAsync(model, ct);

            if (string.IsNullOrEmpty(rowsJson) || string.IsNullOrEmpty(termIdStr)
                || !Guid.TryParse(termIdStr, out var termId)
                || string.IsNullOrEmpty(modalityIdStr)
                || !Guid.TryParse(modalityIdStr, out var modalityId))
            {
                TempData["Error"] = "Los datos de importación expiraron. Realice la vista previa nuevamente.";
                return View("~/Pages/Admin/Imports/AdmissionResultImport.cshtml", model);
            }

            model.SelectedTermId = termId;
            model.SelectedModalityId = modalityId;
            await LoadModalitiesAsync(model, termId, ct);

            var validRows = JsonSerializer.Deserialize<List<AdmissionImportRow>>(rowsJson);

            if (validRows == null || validRows.Count == 0)
            {
                TempData["Error"] = "No hay filas válidas para importar.";
                return View("~/Pages/Admin/Imports/AdmissionResultImport.cshtml", model);
            }

            var result = await _importService.ImportAdmissionAsync(validRows, termId, modalityId, User.Identity?.Name ?? "Admin", ct);

            model.ImportHistory = await _importService.GetAdmissionImportHistoryAsync(termId, ct);

            if (result.Errors.Count > 0)
            {
                TempData["Error"] = $"Importación completada con errores: {string.Join("; ", result.Errors)}";
            }
            else
            {
                TempData["Success"] = $"Importación exitosa: {result.Imported} registros importados, {result.Skipped} omitidos.";
            }

            return View("~/Pages/Admin/Imports/AdmissionResultImport.cshtml", model);
        }

        [HttpPost("revert")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Revert(Guid batchId, Guid? SelectedTermId, CancellationToken ct)
        {
            var model = new AdmissionImportViewModel
            {
                SelectedTermId = SelectedTermId,
                IsSuperAdmin = User.IsInRole(AppConstants.Roles.SuperAdmin)
            };

            await LoadTermsAsync(model, ct);

            if (!SelectedTermId.HasValue)
            {
                TempData["Error"] = "Seleccione un período.";
                return View("~/Pages/Admin/Imports/AdmissionResultImport.cshtml", model);
            }

            await LoadModalitiesAsync(model, SelectedTermId.Value, ct);

            var count = await _importService.RevertAdmissionImportAsync(batchId, User.Identity?.Name ?? "Admin", ct);
            model.ImportHistory = await _importService.GetAdmissionImportHistoryAsync(SelectedTermId.Value, ct);
            TempData["Success"] = $"Importación revertida: {count} registros eliminados.";
            return View("~/Pages/Admin/Imports/AdmissionResultImport.cshtml", model);
        }

        [HttpGet("modalities-by-term/{termId}")]
        public async Task<IActionResult> GetModalitiesByTerm(Guid termId, CancellationToken ct)
        {
            using var scope = HttpContext.RequestServices.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var modalities = await context.Modalities
                .AsNoTracking()
                .Where(m => m.TermId == termId)
                .OrderBy(m => m.DisplayOrder)
                .Select(m => new { id = m.Id, name = m.Name })
                .ToListAsync(ct);
            return Json(modalities);
        }

        private async Task LoadTermsAsync(AdmissionImportViewModel model, CancellationToken ct)
        {
            using var scope = HttpContext.RequestServices.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            model.Terms = await context.Terms
                .AsNoTracking()
                .OrderByDescending(t => t.Year).ThenByDescending(t => t.Number)
                .ToListAsync(ct);
        }

        private async Task LoadModalitiesAsync(AdmissionImportViewModel model, Guid termId, CancellationToken ct)
        {
            using var scope = HttpContext.RequestServices.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            model.Modalities = await context.Modalities
                .AsNoTracking()
                .Where(m => m.TermId == termId)
                .OrderBy(m => m.DisplayOrder)
                .ToListAsync(ct);
        }
    }
}
