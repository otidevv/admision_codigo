using ADMISION.ENTITIES.Constants;
using ADMISION.ENTITIES.Models.Info;
using ADMISION.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace admision.Controllers.Admin.InfoManagementController
{
    [Authorize(Roles = AppConstants.Roles.SuperAdmin + "," + AppConstants.Roles.Admin)]
    [Route("admin/info-management/chatbot")]
    public class FaqController : Controller
    {
        private readonly IFaqService _faq;
        public FaqController(IFaqService faq) { _faq = faq; }

        [HttpGet("")]
        public async Task<IActionResult> Index(CancellationToken ct)
        {
            var list = await _faq.GetAllAsync(includeInactive: true, ct);
            return View("~/Pages/Admin/InfoManagement/Faq/Index.cshtml", list);
        }

        [HttpGet("crear")]
        public async Task<IActionResult> Create(CancellationToken ct)
        {
            ViewBag.ParentOptions = await BuildParentList(null, ct);
            return View("~/Pages/Admin/InfoManagement/Faq/Create.cshtml", new FaqItem { IsActive = true, DisplayOrder = 0 });
        }

        [HttpPost("crear")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(FaqItem item, CancellationToken ct)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.ParentOptions = await BuildParentList(null, ct);
                return View("~/Pages/Admin/InfoManagement/Faq/Create.cshtml", item);
            }

            await _faq.CreateAsync(item, User.Identity?.Name ?? "Admin", ct);
            TempData["Success"] = "Pregunta frecuente registrada exitosamente.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet("editar/{id}")]
        public async Task<IActionResult> Edit(Guid id, CancellationToken ct)
        {
            var item = await _faq.GetByIdAsync(id, ct);
            if (item == null) return NotFound();
            ViewBag.ParentOptions = await BuildParentList(id, ct);
            return View("~/Pages/Admin/InfoManagement/Faq/Edit.cshtml", item);
        }

        [HttpPost("editar/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, FaqItem item, CancellationToken ct)
        {
            if (id != item.Id) return NotFound();
            if (!ModelState.IsValid)
            {
                ViewBag.ParentOptions = await BuildParentList(id, ct);
                return View("~/Pages/Admin/InfoManagement/Faq/Edit.cshtml", item);
            }

            var ok = await _faq.UpdateAsync(item, User.Identity?.Name ?? "Admin", ct);
            if (!ok) return NotFound();

            TempData["Success"] = "Pregunta frecuente actualizada exitosamente.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost("eliminar/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            var ok = await _faq.DeleteAsync(id, ct);
            TempData[ok ? "Success" : "Error"] = ok
                ? "Pregunta frecuente eliminada."
                : "No se encontró la pregunta frecuente.";
            return RedirectToAction(nameof(Index));
        }

        private async Task<List<SelectListItem>> BuildParentList(Guid? excludeId, CancellationToken ct)
        {
            var all = await _faq.GetAllAsync(includeInactive: true, ct);
            var list = all
                .Where(f => f.Id != excludeId)
                .OrderBy(f => f.Category)
                .ThenBy(f => f.DisplayOrder)
                .ThenBy(f => f.Question)
                .Select(f => new SelectListItem
                {
                    Value = f.Id.ToString(),
                    Text = $"{(string.IsNullOrEmpty(f.Category) ? "" : "[" + f.Category + "] ")}{f.Question}"
                })
                .ToList();

            list.Insert(0, new SelectListItem { Value = "", Text = "— Opción raíz (sin padre) —" });
            return list;
        }
    }
}
