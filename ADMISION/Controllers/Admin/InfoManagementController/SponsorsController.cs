using ADMISION.ENTITIES.Constants;
using ADMISION.ENTITIES.Models.Info;
using ADMISION.Extensions;
using ADMISION.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace admision.Controllers.Admin.InfoManagementController
{
    [Authorize(Roles = AppConstants.Roles.SuperAdmin + "," + AppConstants.Roles.Admin)]
    [Route("admin/info-management/sponsors")]
    public class SponsorsController : Controller
    {
        private readonly ISponsorService _sponsors;
        private readonly ILogger<SponsorsController> _logger;

        public SponsorsController(ISponsorService sponsors, ILogger<SponsorsController> logger)
        {
            _sponsors = sponsors;
            _logger = logger;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index(CancellationToken ct)
        {
            var sponsors = await _sponsors.GetAllAsync(ct);
            return View("~/Pages/Admin/InfoManagement/Sponsor/Index.cshtml", sponsors);
        }

        [HttpGet("crear")]
        public IActionResult Create() => View("~/Pages/Admin/InfoManagement/Sponsor/Create.cshtml");

        [HttpPost("crear")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Sponsor sponsor, IFormFile? logo, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return View("~/Pages/Admin/InfoManagement/Sponsor/Create.cshtml", sponsor);

            try
            {
                await _sponsors.CreateAsync(sponsor, logo, User.Identity?.Name ?? "Admin", ct);
                TempData["Success"] = "Sponsor creado exitosamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                this.SetSaveError(ex, _logger);
                return View("~/Pages/Admin/InfoManagement/Sponsor/Create.cshtml", sponsor);
            }
        }

        [HttpGet("editar/{id}")]
        public async Task<IActionResult> Edit(Guid id, CancellationToken ct)
        {
            var sponsor = await _sponsors.GetByIdAsync(id, ct);
            if (sponsor == null) return NotFound();
            return View("~/Pages/Admin/InfoManagement/Sponsor/Edit.cshtml", sponsor);
        }

        [HttpPost("editar/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, Sponsor sponsor, IFormFile? logo, CancellationToken ct)
        {
            if (id != sponsor.Id) return NotFound();
            if (!ModelState.IsValid)
                return View("~/Pages/Admin/InfoManagement/Sponsor/Edit.cshtml", sponsor);

            try
            {
                var ok = await _sponsors.UpdateAsync(sponsor, logo, User.Identity?.Name ?? "Admin", ct);
                if (!ok) return NotFound();

                TempData["Success"] = "Sponsor actualizado exitosamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                this.SetSaveError(ex, _logger);
                return View("~/Pages/Admin/InfoManagement/Sponsor/Edit.cshtml", sponsor);
            }
        }

        [HttpPost("eliminar/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            var outcome = await _sponsors.DeleteAsync(id, ct);
            switch (outcome)
            {
                case DeleteOutcome.Deleted:
                    TempData["Success"] = "Sponsor eliminado exitosamente.";
                    break;
                case DeleteOutcome.HasDependencies:
                    TempData["Error"] = "No se puede eliminar el sponsor porque tiene registros asociados.";
                    break;
                default:
                    TempData["Error"] = "No se encontró el sponsor.";
                    break;
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
