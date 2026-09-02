using ADMISION.ENTITIES.Constants;
using ADMISION.ENTITIES.Models.Info;
using ADMISION.Extensions;
using ADMISION.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace admision.Controllers.Admin.InfoManagementController
{
    [Authorize(Roles = AppConstants.Roles.SuperAdmin + "," + AppConstants.Roles.Admin)]
    [Route("admin/info-management/prospects")]
    public class ProspectsController : Controller
    {
        private readonly IProspectService _prospects;
        private readonly ICatalogService _catalog;
        private readonly ILogger<ProspectsController> _logger;

        public ProspectsController(IProspectService prospects, ICatalogService catalog, ILogger<ProspectsController> logger)
        {
            _prospects = prospects;
            _catalog = catalog;
            _logger = logger;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index(CancellationToken ct)
        {
            var prospects = await _prospects.GetAllAsync(ct);
            return View("~/Pages/Admin/InfoManagement/Prospect/Index.cshtml", prospects);
        }

        [HttpGet("crear")]
        public async Task<IActionResult> Create(CancellationToken ct)
        {
            await PopulateTermsAsync(null, ct);
            return View("~/Pages/Admin/InfoManagement/Prospect/Create.cshtml");
        }

        [HttpPost("crear")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Prospect prospect, IFormFile? pdfFile, CancellationToken ct)
        {
            if (!ModelState.IsValid)
            {
                await PopulateTermsAsync(prospect.TermId, ct);
                return View("~/Pages/Admin/InfoManagement/Prospect/Create.cshtml", prospect);
            }

            try
            {
                await _prospects.CreateAsync(prospect, pdfFile, User.Identity?.Name ?? "Admin", ct);
                TempData["Success"] = "Prospecto creado exitosamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                this.SetSaveError(ex, _logger);
                await PopulateTermsAsync(prospect.TermId, ct);
                return View("~/Pages/Admin/InfoManagement/Prospect/Create.cshtml", prospect);
            }
        }

        [HttpGet("editar/{id}")]
        public async Task<IActionResult> Edit(Guid id, CancellationToken ct)
        {
            var prospect = await _prospects.GetByIdAsync(id, ct);
            if (prospect == null) return NotFound();

            await PopulateTermsAsync(prospect.TermId, ct);
            return View("~/Pages/Admin/InfoManagement/Prospect/Edit.cshtml", prospect);
        }

        [HttpPost("editar/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, Prospect prospect, IFormFile? pdfFile, CancellationToken ct)
        {
            if (id != prospect.Id) return NotFound();
            if (!ModelState.IsValid)
            {
                await PopulateTermsAsync(prospect.TermId, ct);
                return View("~/Pages/Admin/InfoManagement/Prospect/Edit.cshtml", prospect);
            }

            try
            {
                var ok = await _prospects.UpdateAsync(prospect, pdfFile, User.Identity?.Name ?? "Admin", ct);
                if (!ok) return NotFound();

                TempData["Success"] = "Prospecto actualizado exitosamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                this.SetSaveError(ex, _logger);
                await PopulateTermsAsync(prospect.TermId, ct);
                return View("~/Pages/Admin/InfoManagement/Prospect/Edit.cshtml", prospect);
            }
        }

        [HttpPost("eliminar/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            var outcome = await _prospects.DeleteAsync(id, ct);
            switch (outcome)
            {
                case DeleteOutcome.Deleted:
                    TempData["Success"] = "Prospecto eliminado exitosamente.";
                    break;
                case DeleteOutcome.HasDependencies:
                    TempData["Error"] = "No se puede eliminar el prospecto porque tiene registros asociados.";
                    break;
                default:
                    TempData["Error"] = "No se encontró el prospecto.";
                    break;
            }
            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateTermsAsync(Guid? selectedTermId, CancellationToken ct)
        {
            var terms = await _catalog.GetTermsAsync(ct: ct);
            ViewBag.Terms = new SelectList(terms, nameof(CatalogOption.Id), nameof(CatalogOption.Name), selectedTermId);
        }
    }
}
