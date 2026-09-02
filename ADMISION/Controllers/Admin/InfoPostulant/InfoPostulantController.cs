using ADMISION.ENTITIES.Constants;
using ADMISION.Models.ViewModels.Admin;
using ADMISION.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace admision.Controllers.Admin.InfoPostulant
{
    [Route("admin/info-postulant")]
    [Authorize(Roles = AppConstants.Roles.SuperAdmin + "," + AppConstants.Roles.Admin + "," + AppConstants.Roles.Consultor)]
    public class InfoPostulantController : Controller
    {
        private readonly IConsolidadoConfigService _consolidadoConfigService;
        private readonly IConsolidadoService _consolidadoService;

        public InfoPostulantController(
            IConsolidadoConfigService consolidadoConfigService,
            IConsolidadoService consolidadoService)
        {
            _consolidadoConfigService = consolidadoConfigService;
            _consolidadoService = consolidadoService;
        }

        [HttpGet("")]
        public IActionResult Index()
        {
            return View("~/Pages/Admin/InfoPostulant/Index.cshtml");
        }

        [Authorize(Roles = AppConstants.Roles.SuperAdmin)]
        [HttpGet("ingresantes-consolidado")]
        public async Task<IActionResult> Consolidado(Guid? selectedTermId)
        {
            var remoteIp = HttpContext.Connection.RemoteIpAddress?.ToString();
            var model = await _consolidadoService.GetPreviewAsync(selectedTermId, User, remoteIp);
            model.IsSuperAdmin = User.IsInRole(AppConstants.Roles.SuperAdmin);
            return View("~/Pages/Admin/InfoPostulant/IngresantesConsolidado/Index.cshtml", model);
        }

        [Authorize(Roles = AppConstants.Roles.SuperAdmin)]
        [HttpPost("ingresantes-consolidado/confirmar")]
        [ValidateAntiForgeryToken]
        [RequestFormLimits(ValueCountLimit = 100_000)]
        public async Task<IActionResult> ConfirmarConsolidado(Guid selectedTermId, List<ConsolidadoPreviewItem>? items)
        {
            var result = await _consolidadoService.ConfirmAsync(selectedTermId, User.Identity?.Name ?? "Admin", items);

            if (Request.Headers.XRequestedWith == "XMLHttpRequest")
            {
                return Json(new { success = result.Success, message = result.Message });
            }

            TempData[result.Success ? "Success" : "Error"] = result.Message;
            return RedirectToAction(nameof(Consolidado), new { selectedTermId });
        }

        [Authorize(Roles = AppConstants.Roles.SuperAdmin)]
        [HttpPost("ingresantes-consolidado/agregar-ingresante")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AgregarIngresante(Guid selectedTermId, string? codePostulant)
        {
            var result = await _consolidadoService.AddIngresanteAsync(
                selectedTermId, codePostulant, User.Identity?.Name ?? "Admin");

            if (Request.Headers.XRequestedWith == "XMLHttpRequest")
            {
                return Json(new { success = result.Success, message = result.Message });
            }

            TempData[result.Success ? "Success" : "Error"] = result.Message;
            return RedirectToAction(nameof(Consolidado), new { selectedTermId });
        }

        [Authorize(Roles = AppConstants.Roles.SuperAdmin)]
        [HttpGet("ingresantes-consolidado/editar")]
        public async Task<IActionResult> EditarConsolidado(Guid? selectedTermId)
        {
            var model = await _consolidadoService.GetEditAsync(selectedTermId);
            model.IsSuperAdmin = User.IsInRole(AppConstants.Roles.SuperAdmin);
            return View("~/Pages/Admin/InfoPostulant/IngresantesConsolidado/Editar.cshtml", model);
        }

        [Authorize(Roles = AppConstants.Roles.SuperAdmin)]
        [HttpPost("ingresantes-consolidado/editar/guardar")]
        [ValidateAntiForgeryToken]
        [RequestFormLimits(ValueCountLimit = 100_000)]
        public async Task<IActionResult> GuardarEdicionConsolidado(Guid selectedTermId, List<ConsolidadoPreviewItem>? items)
        {
            var result = await _consolidadoService.SaveEditsAsync(
                selectedTermId, User.Identity?.Name ?? "Admin", items);

            if (Request.Headers.XRequestedWith == "XMLHttpRequest")
            {
                return Json(new { success = result.Success, message = result.Message });
            }

            TempData[result.Success ? "Success" : "Error"] = result.Message;
            return RedirectToAction(nameof(EditarConsolidado), new { selectedTermId });
        }

        [Authorize(Roles = AppConstants.Roles.SuperAdmin)]
        [HttpGet("ingresantes-consolidado/config")]
        public async Task<IActionResult> ConsolidadoConfig(Guid? selectedTermId)
        {
            var model = new ConsolidadoConfigViewModel
            {
                SelectedTermId = selectedTermId,
                IsSuperAdmin = User.IsInRole(AppConstants.Roles.SuperAdmin),
                Terms = await _consolidadoConfigService.GetTermsAsync()
            };

            if (selectedTermId.HasValue)
            {
                model.Careers = await _consolidadoConfigService.GetCareersAsync();
                model.Modalities = await _consolidadoConfigService.GetModalitiesAsync(selectedTermId.Value);
                model.TypeModalities = await _consolidadoConfigService.GetTypeModalitiesAsync(selectedTermId.Value);
                model.Configurations = await _consolidadoConfigService.GetConfigurationsAsync(selectedTermId.Value);
            }

            return View("~/Pages/Admin/InfoPostulant/IngresantesConsolidado/Config.cshtml", model);
        }

        [Authorize(Roles = AppConstants.Roles.SuperAdmin)]
        [HttpPost("ingresantes-consolidado/config/guardar")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveConsolidadoConfig(
            Guid selectedTermId, int index, string description,
            Guid? careerId, Guid? modalityId, Guid? typeModalityId)
        {
            if (string.IsNullOrWhiteSpace(description))
            {
                TempData["Error"] = "La descripción es obligatoria.";
                return RedirectToAction(nameof(ConsolidadoConfig), new { selectedTermId });
            }

            if (!careerId.HasValue && !modalityId.HasValue && !typeModalityId.HasValue)
            {
                TempData["Error"] = "Debe asignar una carrera, una modalidad o un tipo de modalidad.";
                return RedirectToAction(nameof(ConsolidadoConfig), new { selectedTermId });
            }

            if (careerId.HasValue && modalityId.HasValue)
            {
                TempData["Error"] = "Debe asignar solo una carrera o una modalidad, no ambas.";
                return RedirectToAction(nameof(ConsolidadoConfig), new { selectedTermId });
            }

            if (await _consolidadoConfigService.ExistsConfigurationAsync(selectedTermId, index))
            {
                TempData["Error"] = $"Ya existe una configuración con el índice {index} para este período.";
                return RedirectToAction(nameof(ConsolidadoConfig), new { selectedTermId });
            }

            await _consolidadoConfigService.CreateConfigurationAsync(
                selectedTermId, index, description.Trim(),
                careerId, modalityId, typeModalityId,
                User.Identity?.Name ?? "Admin");

            TempData["Success"] = "Configuración agregada exitosamente.";
            return RedirectToAction(nameof(ConsolidadoConfig), new { selectedTermId });
        }

        [Authorize(Roles = AppConstants.Roles.SuperAdmin)]
        [HttpPost("ingresantes-consolidado/config/eliminar/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConsolidadoConfig(Guid id, Guid? selectedTermId)
        {
            var deleted = await _consolidadoConfigService.DeleteConfigurationAsync(id);
            TempData[deleted ? "Success" : "Error"] = deleted
                ? "Configuración eliminada."
                : "No se encontró la configuración.";
            return RedirectToAction(nameof(ConsolidadoConfig), new { selectedTermId });
        }
    }
}
