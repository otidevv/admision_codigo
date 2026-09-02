using ADMISION.ENTITIES.Constants;
using ADMISION.ENTITIES.Models.Info;
using ADMISION.Extensions;
using ADMISION.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace admision.Controllers.Admin.InfoManagementController
{
    [Authorize(Roles = AppConstants.Roles.SuperAdmin + "," + AppConstants.Roles.Admin)]
    [Route("admin/info-management/brochures")]
    public class BrochuresController : Controller
    {
        private const string Label = "Brochure";
        private const string ViewFolder = "Brochure";

        private readonly IBrochureService _brochures;
        private readonly ILogger<BrochuresController> _logger;

        public BrochuresController(IBrochureService brochures, ILogger<BrochuresController> logger)
        {
            _brochures = brochures;
            _logger = logger;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index(CancellationToken ct)
        {
            var list = await _brochures.GetAllAsync(ct);
            return View($"~/Pages/Admin/InfoManagement/{ViewFolder}/Index.cshtml", list);
        }

        [HttpGet("crear")]
        public IActionResult Create() => View($"~/Pages/Admin/InfoManagement/{ViewFolder}/Create.cshtml");

        [HttpPost("crear")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Brochure brochure, IFormFile? uploadFile, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return View($"~/Pages/Admin/InfoManagement/{ViewFolder}/Create.cshtml", brochure);

            try
            {
                await _brochures.CreateAsync(brochure, uploadFile, User.Identity?.Name ?? "Admin", ct);
                TempData["Success"] = $"{Label} creado exitosamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                this.SetSaveError(ex, _logger);
                return View($"~/Pages/Admin/InfoManagement/{ViewFolder}/Create.cshtml", brochure);
            }
        }

        [HttpGet("editar/{id}")]
        public async Task<IActionResult> Edit(Guid id, CancellationToken ct)
        {
            var brochure = await _brochures.GetByIdAsync(id, ct);
            if (brochure == null) return NotFound();
            return View($"~/Pages/Admin/InfoManagement/{ViewFolder}/Edit.cshtml", brochure);
        }

        [HttpPost("editar/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, Brochure brochure, IFormFile? uploadFile, CancellationToken ct)
        {
            if (id != brochure.Id) return NotFound();
            if (!ModelState.IsValid)
                return View($"~/Pages/Admin/InfoManagement/{ViewFolder}/Edit.cshtml", brochure);

            try
            {
                var ok = await _brochures.UpdateAsync(brochure, uploadFile, User.Identity?.Name ?? "Admin", ct);
                if (!ok) return NotFound();

                TempData["Success"] = $"{Label} actualizado exitosamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                this.SetSaveError(ex, _logger);
                return View($"~/Pages/Admin/InfoManagement/{ViewFolder}/Edit.cshtml", brochure);
            }
        }

        [HttpPost("eliminar/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            var outcome = await _brochures.DeleteAsync(id, ct);
            switch (outcome)
            {
                case DeleteOutcome.Deleted:
                    TempData["Success"] = $"{Label} eliminado exitosamente.";
                    break;
                case DeleteOutcome.HasDependencies:
                    TempData["Error"] = $"No se puede eliminar el {Label.ToLower()} porque tiene registros asociados.";
                    break;
                default:
                    TempData["Error"] = $"No se encontró el {Label.ToLower()}.";
                    break;
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
