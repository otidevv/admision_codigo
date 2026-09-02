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
    [Route("admin/info-postulant/cepre-match")]
    [Authorize(Roles = AppConstants.Roles.SuperAdmin)]
    public class CepreMatchController : Controller
    {
        private readonly IExamResultImportService _importService;
        private const string SessionKeyRows = "CepreMatch_Rows";
        private const string SessionKeyTerm = "CepreMatch_Term";
        private const string SessionKeyModality = "CepreMatch_Modality";

        public CepreMatchController(IExamResultImportService importService)
        {
            _importService = importService;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index(Guid? termId, Guid? modalityId, CancellationToken ct)
        {
            var model = new CepreMatchViewModel
            {
                SelectedTermId = termId,
                SelectedModalityId = modalityId,
                IsSuperAdmin = User.IsInRole(AppConstants.Roles.SuperAdmin)
            };

            await LoadTermsAsync(model, ct);

            if (termId.HasValue)
            {
                await LoadModalitiesAsync(model, termId.Value, ct);
                model.ImportHistory = await _importService.GetCepreMatchHistoryAsync(termId.Value, ct);
            }

            return View("~/Pages/Admin/Imports/CepreMatch.cshtml", model);
        }

        [HttpPost("preview")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Preview(Guid? SelectedTermId, Guid? SelectedModalityId, CancellationToken ct)
        {
            var model = new CepreMatchViewModel
            {
                SelectedTermId = SelectedTermId,
                SelectedModalityId = SelectedModalityId,
                IsSuperAdmin = User.IsInRole(AppConstants.Roles.SuperAdmin)
            };

            await LoadTermsAsync(model, ct);

            if (!SelectedTermId.HasValue)
            {
                TempData["Error"] = "Seleccione un período académico.";
                return View("~/Pages/Admin/Imports/CepreMatch.cshtml", model);
            }

            await LoadModalitiesAsync(model, SelectedTermId.Value, ct);

            if (!SelectedModalityId.HasValue)
            {
                TempData["Error"] = "Seleccione una modalidad.";
                return View("~/Pages/Admin/Imports/CepreMatch.cshtml", model);
            }

            model.Preview = await _importService.PreviewCepreMatchAsync(SelectedTermId.Value, SelectedModalityId.Value, ct);

            model.ImportHistory = await _importService.GetCepreMatchHistoryAsync(SelectedTermId.Value, ct);

            var validRows = model.Preview.Rows.Where(r => r.IsValid).ToList();
            HttpContext.Session.SetString(SessionKeyRows, JsonSerializer.Serialize(validRows));
            HttpContext.Session.SetString(SessionKeyTerm, SelectedTermId.Value.ToString());
            HttpContext.Session.SetString(SessionKeyModality, SelectedModalityId.Value.ToString());

            return View("~/Pages/Admin/Imports/CepreMatch.cshtml", model);
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

            var model = new CepreMatchViewModel
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
                return View("~/Pages/Admin/Imports/CepreMatch.cshtml", model);
            }

            model.SelectedTermId = termId;
            model.SelectedModalityId = modalityId;
            await LoadModalitiesAsync(model, termId, ct);

            var validRows = JsonSerializer.Deserialize<List<CepreMatchRow>>(rowsJson);

            if (validRows == null || validRows.Count == 0)
            {
                TempData["Error"] = "No hay filas válidas para importar.";
                return View("~/Pages/Admin/Imports/CepreMatch.cshtml", model);
            }

            var result = await _importService.ImportCepreMatchAsync(validRows, termId, modalityId, User.Identity?.Name ?? "Admin", ct);

            model.ImportHistory = await _importService.GetCepreMatchHistoryAsync(termId, ct);

            if (result.Errors.Count > 0)
            {
                TempData["Error"] = $"Importación completada con errores: {string.Join("; ", result.Errors)}";
            }
            else
            {
                TempData["Success"] = $"Match exitoso: {result.Imported} registros procesados.";
            }

            return View("~/Pages/Admin/Imports/CepreMatch.cshtml", model);
        }

        [HttpPost("revert")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Revert(Guid batchId, Guid? SelectedTermId, CancellationToken ct)
        {
            var model = new CepreMatchViewModel
            {
                SelectedTermId = SelectedTermId,
                IsSuperAdmin = User.IsInRole(AppConstants.Roles.SuperAdmin)
            };

            await LoadTermsAsync(model, ct);

            if (!SelectedTermId.HasValue)
            {
                TempData["Error"] = "Seleccione un período.";
                return View("~/Pages/Admin/Imports/CepreMatch.cshtml", model);
            }

            await LoadModalitiesAsync(model, SelectedTermId.Value, ct);

            var count = await _importService.RevertCepreMatchAsync(batchId, User.Identity?.Name ?? "Admin", ct);
            model.ImportHistory = await _importService.GetCepreMatchHistoryAsync(SelectedTermId.Value, ct);
            TempData["Success"] = $"Match revertido: {count} registros deshechos.";
            return View("~/Pages/Admin/Imports/CepreMatch.cshtml", model);
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

        private async Task LoadTermsAsync(CepreMatchViewModel model, CancellationToken ct)
        {
            using var scope = HttpContext.RequestServices.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            model.Terms = await context.Terms
                .AsNoTracking()
                .OrderByDescending(t => t.Year).ThenByDescending(t => t.Number)
                .ToListAsync(ct);
        }

        private async Task LoadModalitiesAsync(CepreMatchViewModel model, Guid termId, CancellationToken ct)
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
