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
    [Route("admin/importaciones/cepre")]
    [Authorize(Roles = AppConstants.Roles.SuperAdmin + "," + AppConstants.Roles.Soporte)]
    public class CepreImportController : Controller
    {
        private readonly IExamResultImportService _importService;
        private const string SessionKeyRows = "CepreImport_Rows";
        private const string SessionKeyTerm = "CepreImport_Term";

        public CepreImportController(IExamResultImportService importService)
        {
            _importService = importService;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index(Guid? termId, CancellationToken ct)
        {
            var model = new CepreImportViewModel
            {
                SelectedTermId = termId,
                IsSuperAdmin = User.IsInRole(AppConstants.Roles.SuperAdmin)
            };

            await LoadTermsAsync(model, ct);

            if (termId.HasValue)
            {
                model.ImportHistory = await _importService.GetCepreImportHistoryAsync(termId.Value, ct);
                model.Versions = await _importService.GetVersionsAsync(termId.Value, ct);
                await CheckTurnAccessAsync(model, termId.Value, ct);
            }

            return View("~/Pages/Admin/Imports/CepreImport.cshtml", model);
        }

        [HttpGet("template")]
        public IActionResult Template()
        {
            var bytes = _importService.BuildCepreTemplate();
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Plantilla_Postulantes_CEPRE.xlsx");
        }

        [HttpPost("preview")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Preview(IFormFile? ExcelFile, Guid? SelectedTermId, CancellationToken ct)
        {
            var model = new CepreImportViewModel
            {
                SelectedTermId = SelectedTermId,
                IsSuperAdmin = User.IsInRole(AppConstants.Roles.SuperAdmin)
            };

            await LoadTermsAsync(model, ct);

            if (ExcelFile == null || ExcelFile.Length == 0)
            {
                TempData["Error"] = "Seleccione un archivo Excel válido.";
                return View("~/Pages/Admin/Imports/CepreImport.cshtml", model);
            }

            if (!SelectedTermId.HasValue)
            {
                TempData["Error"] = "Seleccione un período académico.";
                return View("~/Pages/Admin/Imports/CepreImport.cshtml", model);
            }

            await CheckTurnAccessAsync(model, SelectedTermId.Value, ct);
            if (!model.CanImport)
            {
                TempData["Error"] = "No tiene un turno activo para importar en este período.";
                return View("~/Pages/Admin/Imports/CepreImport.cshtml", model);
            }

            using var stream = ExcelFile.OpenReadStream();
            model.Preview = await _importService.PreviewCepreAsync(stream, ExcelFile.FileName, SelectedTermId.Value, ct);

            model.ImportHistory = await _importService.GetCepreImportHistoryAsync(SelectedTermId.Value, ct);
            model.Versions = await _importService.GetVersionsAsync(SelectedTermId.Value, ct);

            var validRows = model.Preview.Rows.Where(r => r.IsValid).ToList();
            HttpContext.Session.SetString(SessionKeyRows, JsonSerializer.Serialize(validRows));
            HttpContext.Session.SetString(SessionKeyTerm, SelectedTermId.Value.ToString());

            return View("~/Pages/Admin/Imports/CepreImport.cshtml", model);
        }

        [HttpPost("import")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Import(CancellationToken ct)
        {
            var rowsJson = HttpContext.Session.GetString(SessionKeyRows);
            var termIdStr = HttpContext.Session.GetString(SessionKeyTerm);
            HttpContext.Session.Remove(SessionKeyRows);
            HttpContext.Session.Remove(SessionKeyTerm);

            var model = new CepreImportViewModel
            {
                IsSuperAdmin = User.IsInRole(AppConstants.Roles.SuperAdmin)
            };

            await LoadTermsAsync(model, ct);

            if (string.IsNullOrEmpty(rowsJson) || string.IsNullOrEmpty(termIdStr) || !Guid.TryParse(termIdStr, out var termId))
            {
                TempData["Error"] = "Los datos de importación expiraron. Realice la vista previa nuevamente.";
                return View("~/Pages/Admin/Imports/CepreImport.cshtml", model);
            }

            model.SelectedTermId = termId;

            var validRows = JsonSerializer.Deserialize<List<CepreImportRow>>(rowsJson);

            if (validRows == null || validRows.Count == 0)
            {
                TempData["Error"] = "No hay filas válidas para importar.";
                return View("~/Pages/Admin/Imports/CepreImport.cshtml", model);
            }

            var result = await _importService.ImportCepreAsync(validRows, termId, User.Identity?.Name ?? "Admin", ct);

            model.ImportHistory = await _importService.GetCepreImportHistoryAsync(termId, ct);
            model.Versions = await _importService.GetVersionsAsync(termId, ct);
            await CheckTurnAccessAsync(model, termId, ct);

            if (result.Errors.Count > 0)
            {
                TempData["Error"] = $"Importación completada con errores: {string.Join("; ", result.Errors)}";
            }
            else
            {
                TempData["Success"] = $"Importación exitosa: {result.Imported} registros importados en la nueva versión.";
            }

            return View("~/Pages/Admin/Imports/CepreImport.cshtml", model);
        }

        [HttpPost("revert")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Revert(Guid batchId, Guid? SelectedTermId, CancellationToken ct)
        {
            var model = new CepreImportViewModel
            {
                SelectedTermId = SelectedTermId,
                IsSuperAdmin = User.IsInRole(AppConstants.Roles.SuperAdmin)
            };

            await LoadTermsAsync(model, ct);

            if (!SelectedTermId.HasValue)
            {
                TempData["Error"] = "Seleccione un período.";
                return View("~/Pages/Admin/Imports/CepreImport.cshtml", model);
            }

            var count = await _importService.RevertCepreImportAsync(batchId, User.Identity?.Name ?? "Admin", ct);
            model.ImportHistory = await _importService.GetCepreImportHistoryAsync(SelectedTermId.Value, ct);
            model.Versions = await _importService.GetVersionsAsync(SelectedTermId.Value, ct);
            TempData["Success"] = $"Importación revertida: {count} registros eliminados.";
            return View("~/Pages/Admin/Imports/CepreImport.cshtml", model);
        }

        private async Task LoadTermsAsync(CepreImportViewModel model, CancellationToken ct)
        {
            using var scope = HttpContext.RequestServices.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            model.Terms = await context.Terms
                .AsNoTracking()
                .OrderByDescending(t => t.Year).ThenByDescending(t => t.Number)
                .ToListAsync(ct);
        }

        private async Task CheckTurnAccessAsync(CepreImportViewModel model, Guid termId, CancellationToken ct)
        {
            if (model.IsSuperAdmin)
            {
                model.HasActiveTurn = true;
                model.CanImport = true;
                return;
            }

            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            {
                model.HasActiveTurn = false;
                model.CanImport = false;
                return;
            }

            model.HasActiveTurn = await _importService.HasActiveTurnAsync(termId, userId, ct);
            model.CanImport = model.HasActiveTurn;
        }
    }
}
