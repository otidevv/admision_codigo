using ADMISION.ENTITIES.Constants;
using ADMISION.ENTITIES.Models.Modality;
using ADMISION.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace admision.Controllers.Admin.InfoManagementController
{
    [Authorize(Roles = AppConstants.Roles.SuperAdmin + "," + AppConstants.Roles.Admin)]
    [Route("admin/info-management/beneficiaries")]
    public class BeneficiariesController : Controller
    {
        private readonly IBeneficiaryService _beneficiaries;
        private readonly ICatalogService _catalog;

        public BeneficiariesController(IBeneficiaryService beneficiaries, ICatalogService catalog)
        {
            _beneficiaries = beneficiaries;
            _catalog = catalog;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index(CancellationToken ct)
        {
            var list = await _beneficiaries.GetAllAsync(ct);
            return View("~/Pages/Admin/InfoManagement/Beneficiaries/Index.cshtml", list);
        }

        [HttpGet("crear")]
        public async Task<IActionResult> Create(CancellationToken ct)
        {
            await PopulateTermsAsync(null, ct);
            return View("~/Pages/Admin/InfoManagement/Beneficiaries/Create.cshtml");
        }

        [HttpPost("crear")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Beneficiarie beneficiarie, CancellationToken ct)
        {
            if (!ModelState.IsValid)
            {
                await PopulateTermsAsync(beneficiarie.TermId, ct);
                return View("~/Pages/Admin/InfoManagement/Beneficiaries/Create.cshtml", beneficiarie);
            }

            await _beneficiaries.CreateAsync(beneficiarie, User.Identity?.Name ?? "Admin", ct);
            TempData["Success"] = "Beneficiario creado exitosamente.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet("editar/{id}")]
        public async Task<IActionResult> Edit(Guid id, CancellationToken ct)
        {
            var b = await _beneficiaries.GetByIdAsync(id, ct);
            if (b == null) return NotFound();

            await PopulateTermsAsync(b.TermId, ct);
            return View("~/Pages/Admin/InfoManagement/Beneficiaries/Edit.cshtml", b);
        }

        [HttpPost("editar/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, Beneficiarie beneficiarie, CancellationToken ct)
        {
            if (id != beneficiarie.Id) return NotFound();
            if (!ModelState.IsValid)
            {
                await PopulateTermsAsync(beneficiarie.TermId, ct);
                return View("~/Pages/Admin/InfoManagement/Beneficiaries/Edit.cshtml", beneficiarie);
            }

            var ok = await _beneficiaries.UpdateAsync(beneficiarie, User.Identity?.Name ?? "Admin", ct);
            if (!ok) return NotFound();

            TempData["Success"] = "Beneficiario actualizado exitosamente.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost("eliminar/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            var outcome = await _beneficiaries.DeleteAsync(id, ct);
            switch (outcome)
            {
                case DeleteOutcome.Deleted:
                    TempData["Success"] = "Beneficiario eliminado exitosamente.";
                    break;
                case DeleteOutcome.HasDependencies:
                    TempData["Error"] = "No se puede eliminar el beneficiario porque tiene registros asociados.";
                    break;
                default:
                    TempData["Error"] = "No se encontró el beneficiario.";
                    break;
            }
            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateTermsAsync(Guid? selectedTermId, CancellationToken ct)
        {
            var terms = await _catalog.GetTermsAsync(ct: ct);
            ViewData["TermId"] = new SelectList(terms, nameof(CatalogOption.Id), nameof(CatalogOption.Name), selectedTermId);
        }
    }
}
