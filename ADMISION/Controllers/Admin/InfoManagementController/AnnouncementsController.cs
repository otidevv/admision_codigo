using ADMISION.ENTITIES.Constants;
using ADMISION.ENTITIES.Models.Info;
using ADMISION.Extensions;
using ADMISION.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace admision.Controllers.Admin.InfoManagementController
{
    [Authorize(Roles = AppConstants.Roles.SuperAdmin + "," + AppConstants.Roles.Admin)]
    [Route("admin/info-management/announcements")]
    public class AnnouncementsController : Controller
    {
        private readonly IAnnouncementService _announcements;
        private readonly ILogger<AnnouncementsController> _logger;

        public AnnouncementsController(IAnnouncementService announcements, ILogger<AnnouncementsController> logger)
        {
            _announcements = announcements;
            _logger = logger;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index(CancellationToken ct)
        {
            var announcements = await _announcements.GetAllAsync(ct);
            return View("~/Pages/Admin/InfoManagement/Announcement/Index.cshtml", announcements);
        }

        [HttpGet("crear")]
        public IActionResult Create() => View("~/Pages/Admin/InfoManagement/Announcement/Create.cshtml");

        [HttpPost("crear")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Announcement announcement, IFormFile? image, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return View("~/Pages/Admin/InfoManagement/Announcement/Create.cshtml", announcement);

            try
            {
                await _announcements.CreateAsync(announcement, image, User.Identity?.Name ?? "Admin", ct);
                TempData["Success"] = "Anuncio creado exitosamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                this.SetSaveError(ex, _logger);
                return View("~/Pages/Admin/InfoManagement/Announcement/Create.cshtml", announcement);
            }
        }

        [HttpGet("editar/{id}")]
        public async Task<IActionResult> Edit(Guid id, CancellationToken ct)
        {
            var announcement = await _announcements.GetByIdAsync(id, ct);
            if (announcement == null) return NotFound();
            return View("~/Pages/Admin/InfoManagement/Announcement/Edit.cshtml", announcement);
        }

        [HttpPost("editar/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, Announcement announcement, IFormFile? image, CancellationToken ct)
        {
            if (id != announcement.Id) return NotFound();
            if (!ModelState.IsValid)
                return View("~/Pages/Admin/InfoManagement/Announcement/Edit.cshtml", announcement);

            try
            {
                var ok = await _announcements.UpdateAsync(announcement, image, User.Identity?.Name ?? "Admin", ct);
                if (!ok) return NotFound();

                TempData["Success"] = "Anuncio actualizado exitosamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                this.SetSaveError(ex, _logger);
                return View("~/Pages/Admin/InfoManagement/Announcement/Edit.cshtml", announcement);
            }
        }

        [HttpPost("eliminar/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            var outcome = await _announcements.DeleteAsync(id, ct);
            switch (outcome)
            {
                case DeleteOutcome.Deleted:
                    TempData["Success"] = "Anuncio eliminado exitosamente.";
                    break;
                case DeleteOutcome.HasDependencies:
                    TempData["Error"] = "No se puede eliminar el anuncio porque tiene registros asociados.";
                    break;
                default:
                    TempData["Error"] = "No se encontró el anuncio.";
                    break;
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
