using ADMISION.ENTITIES.Constants;
using ADMISION.ENTITIES.Models.Info;
using ADMISION.Extensions;
using ADMISION.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace admision.Controllers.Admin.InfoManagementController
{
    [Authorize(Roles = AppConstants.Roles.SuperAdmin + "," + AppConstants.Roles.Admin)]
    [Route("admin/info-management/other-files")]
    public class OtherFilesController : Controller
    {
        private const string Category = AppConstants.OtherFileCategory.Otros;
        private const string StorageModule = "OtherFiles";
        private const string ViewFolder = "OtherFiles";
        private const string Label = "Archivo";

        private readonly IOtherFilesService _files;
        private readonly ILogger<OtherFilesController> _logger;

        public OtherFilesController(IOtherFilesService files, ILogger<OtherFilesController> logger)
        {
            _files = files;
            _logger = logger;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index(CancellationToken ct)
        {
            var list = await _files.GetByCategoryAsync(Category, ct);
            return View($"~/Pages/Admin/InfoManagement/{ViewFolder}/Index.cshtml", list);
        }

        [HttpGet("crear")]
        public IActionResult Create() => View($"~/Pages/Admin/InfoManagement/{ViewFolder}/Create.cshtml");

        [HttpPost("crear")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(OtherFiles otherFile, IFormFile? uploadFile, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return View($"~/Pages/Admin/InfoManagement/{ViewFolder}/Create.cshtml", otherFile);

            try
            {
                await _files.CreateAsync(otherFile, uploadFile, Category, StorageModule, User.Identity?.Name ?? "Admin", ct);
                TempData["Success"] = $"{Label} creado exitosamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                this.SetSaveError(ex, _logger);
                return View($"~/Pages/Admin/InfoManagement/{ViewFolder}/Create.cshtml", otherFile);
            }
        }

        [HttpGet("editar/{id}")]
        public async Task<IActionResult> Edit(Guid id, CancellationToken ct)
        {
            var file = await _files.GetByIdAsync(id, Category, ct);
            if (file == null) return NotFound();
            return View($"~/Pages/Admin/InfoManagement/{ViewFolder}/Edit.cshtml", file);
        }

        [HttpPost("editar/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, OtherFiles otherFile, IFormFile? uploadFile, CancellationToken ct)
        {
            if (id != otherFile.Id) return NotFound();
            if (!ModelState.IsValid)
                return View($"~/Pages/Admin/InfoManagement/{ViewFolder}/Edit.cshtml", otherFile);

            try
            {
                var ok = await _files.UpdateAsync(otherFile, uploadFile, Category, StorageModule, User.Identity?.Name ?? "Admin", ct);
                if (!ok) return NotFound();

                TempData["Success"] = $"{Label} actualizado exitosamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                this.SetSaveError(ex, _logger);
                return View($"~/Pages/Admin/InfoManagement/{ViewFolder}/Edit.cshtml", otherFile);
            }
        }

        [HttpPost("eliminar/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            var outcome = await _files.DeleteAsync(id, Category, ct);
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
