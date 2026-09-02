using System.Text.Json;
using ADMISION.ENTITIES.Constants;
using ADMISION.Models.ViewModels.Admin;
using ADMISION.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ADMISION.Controllers.Admin
{
    [Authorize(Roles = AppConstants.Roles.SuperAdmin + "," + AppConstants.Roles.Admin + "," + AppConstants.Roles.Consultor)]
    [Route("admin")]
    public class AdminController : Controller
    {
        private readonly IDashboardService _dashboard;

        public AdminController(IDashboardService dashboard)
        {
            _dashboard = dashboard;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index(
            Guid? termId,
            Guid? modalityId,
            Guid? typeModalityId,
            Guid? careerId,
            Guid? tematicAreaId,
            CancellationToken ct)
        {
            var terms = await _dashboard.GetTermsAsync(ct);
            ViewBag.Terms = terms;

            var selected = termId.HasValue
                ? terms.FirstOrDefault(t => t.Id == termId.Value)
                : terms.FirstOrDefault(t => t.IsActive) ?? terms.FirstOrDefault();

            if (selected == null)
                return View("~/Pages/Admin/Index.cshtml", new AdminDashboardDto());

            var dto = await _dashboard.BuildDashboardAsync(
                selected.Id, modalityId, typeModalityId, careerId, tematicAreaId, ct);
            return View("~/Pages/Admin/Index.cshtml", dto);
        }

        [HttpGet("terms-search")]
        public async Task<IActionResult> GetTerms(CancellationToken ct)
        {
            var terms = await _dashboard.GetTermsAsync(ct);
            return Json(terms.Select(t => new { id = t.Id, name = t.Name }));
        }

        // Endpoint AJAX para el dashboard: recibe los mismos filtros que Index
        // y devuelve el DTO completo en JSON camelCase. El cliente lo usa para
        // refrescar KPIs, gráficos, mapas y tablas sin recargar la página.
        [HttpGet("dashboard-data")]
        public async Task<IActionResult> DashboardData(
            Guid? termId,
            Guid? modalityId,
            Guid? typeModalityId,
            Guid? careerId,
            Guid? tematicAreaId,
            CancellationToken ct)
        {
            var terms = await _dashboard.GetTermsAsync(ct);
            var selected = termId.HasValue
                ? terms.FirstOrDefault(t => t.Id == termId.Value)
                : terms.FirstOrDefault(t => t.IsActive) ?? terms.FirstOrDefault();

            if (selected == null)
                return Json(new AdminDashboardDto());

            var dto = await _dashboard.BuildDashboardAsync(
                selected.Id, modalityId, typeModalityId, careerId, tematicAreaId, ct);

            return new JsonResult(dto, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
        }
    }
}
