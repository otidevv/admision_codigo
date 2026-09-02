using ADMISION.ENTITIES.Constants;
using ADMISION.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace admision.Controllers.Admin.ConfigController
{
    [Authorize(Roles = AppConstants.Roles.SuperAdmin)]
    [Route("admin/config")]
    public class ConfigController : Controller
    {
        private readonly IConfigService _configService;
        private readonly IApiLogService _apiLogService;

        public ConfigController(IConfigService configService, IApiLogService apiLogService)
        {
            _configService = configService;
            _apiLogService = apiLogService;
        }

        // Menú con cards (Información del Sistema + APIs Externas + futuras secciones).
        [HttpGet("")]
        public IActionResult Index()
        {
            return View("~/Pages/Admin/Config/Index.cshtml");
        }

        // Edición de los parámetros del sistema (lo que antes vivía en /admin/config).
        [HttpGet("informacion")]
        public async Task<IActionResult> Edit()
        {
            var configs = await _configService.GetAllConfigsAsync();
            return View("~/Pages/Admin/Config/Information/Edit.cshtml", configs);
        }

        [HttpPost("informacion/update")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(Dictionary<string, string> configs)
        {
            var updatedBy = User.Identity?.Name ?? "Admin";
            await _configService.UpdateConfigsAsync(configs, updatedBy);
            TempData["SuccessMessage"] = "Configuraciones actualizadas correctamente.";
            return RedirectToAction(nameof(Edit));
        }

        // Registro de consultas API (ApiRequestLogs)
        [HttpGet("api-logs")]
        public async Task<IActionResult> ApiLogs(string? user, int page = 1)
        {
            const int pageSize = 30;

            var result = await _apiLogService.GetLogsAsync(user, page, pageSize);

            ViewBag.CurrentPage = result.Page;
            ViewBag.TotalPages = result.TotalPages;
            ViewBag.TotalItems = result.TotalItems;
            ViewBag.FilterUser = user;

            return View("~/Pages/Admin/Api/Logs.cshtml", result.Items);
        }
    }
}
