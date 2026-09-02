using ADMISION.ENTITIES.Constants;
using ADMISION.ENTITIES.Models.EconomicManagement;
using ADMISION.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace admision.Controllers.Admin.EconomicManagementController
{
    [Authorize(Roles = AppConstants.Roles.SuperAdmin + "," + AppConstants.Roles.Admin)]
    [Route("admin/economic-management/payment-codes")]
    public class PaymentCodesController : Controller
    {
        private readonly IPaymentCodeService _paymentCodes;
        private readonly ICatalogService _catalog;

        public PaymentCodesController(IPaymentCodeService paymentCodes, ICatalogService catalog)
        {
            _paymentCodes = paymentCodes;
            _catalog = catalog;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index(CancellationToken ct)
        {
            var codes = await _paymentCodes.GetAllWithAssociationsAsync(ct);
            return View("~/Pages/Admin/EconomicManagement/PaymentCodes/Index.cshtml", codes);
        }

        [HttpGet("crear")]
        public async Task<IActionResult> Create(CancellationToken ct)
        {
            await PopulateFormDataAsync(null, ct);
            return View("~/Pages/Admin/EconomicManagement/PaymentCodes/Edit.cshtml", new PaymentCode());
        }

        [HttpGet("editar/{id}")]
        public async Task<IActionResult> Edit(Guid id, CancellationToken ct)
        {
            var code = await _paymentCodes.GetByIdWithAssociationsAsync(id, ct);
            if (code == null) return NotFound();

            await PopulateFormDataAsync(code.TermId, ct);
            return View("~/Pages/Admin/EconomicManagement/PaymentCodes/Edit.cshtml", code);
        }

        [HttpPost("save")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(PaymentCode paymentCode, List<PaymentCodeModality> modalities, CancellationToken ct)
        {
            if (!ModelState.IsValid)
            {
                await PopulateFormDataAsync(paymentCode.TermId, ct);
                return View("~/Pages/Admin/EconomicManagement/PaymentCodes/Edit.cshtml", paymentCode);
            }

            var isNew = paymentCode.Id == Guid.Empty;
            var ok = await _paymentCodes.SaveAsync(paymentCode, modalities, User.Identity?.Name ?? "Admin", ct);
            if (!ok) return NotFound();

            TempData["Success"] = isNew ? "Código de pago creado exitosamente." : "Código de pago actualizado exitosamente.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost("eliminar/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            var outcome = await _paymentCodes.DeleteAsync(id, ct);
            switch (outcome)
            {
                case DeleteOutcome.Deleted:
                    TempData["Success"] = "Código de pago eliminado exitosamente.";
                    break;
                case DeleteOutcome.HasDependencies:
                    TempData["Error"] = "No se puede eliminar el código de pago porque tiene registros asociados.";
                    break;
                default:
                    TempData["Error"] = "No se encontró el código de pago.";
                    break;
            }
            return RedirectToAction(nameof(Index));
        }

        // ============ Helpers privados ============
        private async Task PopulateFormDataAsync(Guid? selectedTermId, CancellationToken ct)
        {
            var terms = await _catalog.GetTermsAsync(ct: ct);
            ViewData["TermId"] = new SelectList(terms, nameof(CatalogOption.Id), nameof(CatalogOption.Name), selectedTermId);
            ViewData["Modalities"] = await _catalog.GetModalitiesAsync(onlyActive: true, ct: ct);
            ViewData["TypeModalities"] = await _catalog.GetAllTypeModalitiesAsync(onlyActive: true, ct: ct);
        }
    }
}
