using ADMISION.ENTITIES.Constants;
using ADMISION.ENTITIES.Models.Info;
using ADMISION.Extensions;
using ADMISION.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace admision.Controllers.Admin.InfoManagementController
{
    [Authorize(Roles = AppConstants.Roles.SuperAdmin + "," + AppConstants.Roles.Admin)]
    [Route("admin/info-management/banners")]
    public class BannersController : Controller
    {
        private readonly IBannerService _banners;
        private readonly ILogger<BannersController> _logger;

        public BannersController(IBannerService banners, ILogger<BannersController> logger)
        {
            _banners = banners;
            _logger = logger;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index(CancellationToken ct)
        {
            var banners = await _banners.GetAllAsync(ct);
            return View("~/Pages/Admin/InfoManagement/Banner/Index.cshtml", banners);
        }

        [HttpGet("crear")]
        public IActionResult Create() => View("~/Pages/Admin/InfoManagement/Banner/Create.cshtml");

        [HttpPost("crear")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Banner banner, IFormFile? imageHorizontal, IFormFile? imageVertical, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return View("~/Pages/Admin/InfoManagement/Banner/Create.cshtml", banner);

            try
            {
                await _banners.CreateAsync(banner, imageHorizontal, imageVertical, User.Identity?.Name ?? "Admin", ct);
                TempData["Success"] = "Banner creado exitosamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                this.SetSaveError(ex, _logger);
                return View("~/Pages/Admin/InfoManagement/Banner/Create.cshtml", banner);
            }
        }

        [HttpGet("editar/{id}")]
        public async Task<IActionResult> Edit(Guid id, CancellationToken ct)
        {
            var banner = await _banners.GetByIdAsync(id, ct);
            if (banner == null) return NotFound();
            return View("~/Pages/Admin/InfoManagement/Banner/Edit.cshtml", banner);
        }

        [HttpPost("editar/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, Banner banner, IFormFile? imageHorizontal, IFormFile? imageVertical, CancellationToken ct)
        {
            if (id != banner.Id) return NotFound();
            if (!ModelState.IsValid)
                return View("~/Pages/Admin/InfoManagement/Banner/Edit.cshtml", banner);

            try
            {
                var ok = await _banners.UpdateAsync(banner, imageHorizontal, imageVertical, User.Identity?.Name ?? "Admin", ct);
                if (!ok) return NotFound();

                TempData["Success"] = "Banner actualizado exitosamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                this.SetSaveError(ex, _logger);
                return View("~/Pages/Admin/InfoManagement/Banner/Edit.cshtml", banner);
            }
        }

        [HttpPost("eliminar/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            var outcome = await _banners.DeleteAsync(id, ct);
            switch (outcome)
            {
                case DeleteOutcome.Deleted:
                    TempData["Success"] = "Banner eliminado exitosamente.";
                    break;
                case DeleteOutcome.HasDependencies:
                    TempData["Error"] = "No se puede eliminar el banner porque tiene registros asociados.";
                    break;
                default:
                    TempData["Error"] = "No se encontró el banner.";
                    break;
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
