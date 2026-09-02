using ADMISION.ENTITIES.Constants;
using ADMISION.ENTITIES.Models.Integrations;
using ADMISION.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace admision.Controllers.Admin.ConfigController
{
    [Authorize(Roles = AppConstants.Roles.SuperAdmin)]
    [Route("admin/config/apis")]
    public class ExternalApisController : Controller
    {
        private readonly IExternalApiService _apis;
        private readonly ILogger<ExternalApisController> _logger;

        public ExternalApisController(IExternalApiService apis, ILogger<ExternalApisController> logger)
        {
            _apis = apis;
            _logger = logger;
        }

        // ============ Listado ============
        [HttpGet("")]
        public async Task<IActionResult> Index(CancellationToken ct)
        {
            var apis = await _apis.GetAllAsync(ct);
            return View("~/Pages/Admin/Config/Apis/Index.cshtml", apis);
        }

        // ============ Alta ============
        [HttpGet("nuevo")]
        public IActionResult Create()
        {
            return View("~/Pages/Admin/Config/Apis/Create.cshtml",
                new ExternalApi { IsActive = true, HttpMethod = "GET", AuthType = "Bearer", Category = "Generic" });
        }

        [HttpPost("nuevo")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ExternalApi model, CancellationToken ct)
        {
            var result = await _apis.CreateAsync(model, User.Identity?.Name ?? "Admin", ct);
            if (!result.Succeeded)
            {
                foreach (var err in result.Errors) ModelState.AddModelError(err.Field, err.Message);
                return View("~/Pages/Admin/Config/Apis/Create.cshtml", model);
            }

            TempData["Success"] = "API registrada exitosamente.";
            return RedirectToAction(nameof(Index));
        }

        // ============ Edición ============
        [HttpGet("editar/{id:guid}")]
        public async Task<IActionResult> Edit(Guid id, CancellationToken ct)
        {
            var api = await _apis.GetByIdAsync(id, tracking: false, ct);
            if (api == null) return NotFound();
            return View("~/Pages/Admin/Config/Apis/Edit.cshtml", api);
        }

        [HttpPost("editar/{id:guid}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, ExternalApi model, CancellationToken ct)
        {
            if (id != model.Id) return BadRequest();

            var result = await _apis.UpdateAsync(model, User.Identity?.Name ?? "Admin", ct);
            if (result.NotFound) return NotFound();

            if (!result.Succeeded)
            {
                foreach (var err in result.Errors) ModelState.AddModelError(err.Field, err.Message);
                return View("~/Pages/Admin/Config/Apis/Edit.cshtml", model);
            }

            TempData["Success"] = "API actualizada.";
            return RedirectToAction(nameof(Index));
        }

        // ============ Eliminación ============
        [HttpPost("eliminar/{id:guid}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            var outcome = await _apis.DeleteAsync(id, User.Identity?.Name ?? "Admin", ct);
            switch (outcome)
            {
                case ExternalApiDeleteOutcome.Deleted:
                    TempData["Success"] = "API eliminada.";
                    break;
                case ExternalApiDeleteOutcome.SoftDeleted:
                    TempData["Success"] = "La API tenía consultas registradas, se desactivó en lugar de eliminar.";
                    break;
                default:
                    return NotFound();
            }
            return RedirectToAction(nameof(Index));
        }

        // ============ Consulta (Test) ============
        [HttpGet("consultar/{id:guid}")]
        public async Task<IActionResult> Test(Guid id, CancellationToken ct)
        {
            var api = await _apis.GetByIdAsync(id, tracking: false, ct);
            if (api == null) return NotFound();
            return View("~/Pages/Admin/Config/Apis/Test.cshtml", api);
        }

        [HttpGet("consultar")]
        public IActionResult Consult()
        {
            return Redirect("/admin/config/apis");
        }

        [HttpPost("consultar/{id:guid}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TestExecute(Guid id, [FromForm] Dictionary<string, string?> parameters, CancellationToken ct)
        {
            try
            {
                var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
                var result = await _apis.InvokeAsync(id, parameters ?? new(), User, ip, ct);
                return Json(new
                {
                    success = result.Success,
                    statusCode = result.StatusCode,
                    durationMs = result.DurationMs,
                    rows = result.Rows.Select(r => new { r.Label, r.Value }),
                    raw = result.RawResponse,
                    error = result.Error,
                    logId = result.LogId
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fallo invocando API {ApiId}", id);
                return StatusCode(500, new { success = false, message = "Error inesperado al invocar la API." });
            }
        }

        // ============ Logs ============
        [HttpGet("logs")]
        public async Task<IActionResult> Logs(Guid? apiId, int page = 1, int pageSize = 20, CancellationToken ct = default)
        {
            var paged = await _apis.GetLogsAsync(apiId, page, pageSize, ct);
            var apis = await _apis.GetAllAsync(ct);

            ViewBag.CurrentPage = paged.Page;
            ViewBag.PageSize = paged.PageSize;
            ViewBag.TotalItems = paged.TotalItems;
            ViewBag.TotalPages = paged.TotalPages;
            ViewBag.SelectedApiId = apiId;
            ViewBag.Apis = apis;

            return View("~/Pages/Admin/Config/Apis/Logs.cshtml", paged.Items);
        }
    }
}
