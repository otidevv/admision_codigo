using ADMISION.ENTITIES.Constants;
using ADMISION.ENTITIES.Models.Info;
using ADMISION.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace admision.Controllers.Admin.InfoManagementController
{
    [Authorize(Roles = AppConstants.Roles.SuperAdmin + "," + AppConstants.Roles.Admin)]
    [Route("admin/info-management/public-infos")]
    public class PublicInfosController : Controller
    {
        private readonly IPublicInfoService _publicInfos;
        private readonly ICatalogService _catalog;

        public PublicInfosController(IPublicInfoService publicInfos, ICatalogService catalog)
        {
            _publicInfos = publicInfos;
            _catalog = catalog;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index(CancellationToken ct)
        {
            var infos = await _publicInfos.GetAllAsync(ct);
            return View("~/Pages/Admin/InfoManagement/PublicInfo/Index.cshtml", infos);
        }

        [HttpGet("crear")]
        public async Task<IActionResult> Create(CancellationToken ct)
        {
            await PopulateSelectsAsync(null, null, ct);
            return View("~/Pages/Admin/InfoManagement/PublicInfo/Create.cshtml", new PublicInfo { IsActive = true });
        }

        [HttpPost("crear")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PublicInfo publicInfo, CancellationToken ct)
        {
            if (!ModelState.IsValid)
            {
                await PopulateSelectsAsync(publicInfo.TermId, publicInfo.ModalityId, ct);
                return View("~/Pages/Admin/InfoManagement/PublicInfo/Create.cshtml", publicInfo);
            }

            await _publicInfos.CreateAsync(publicInfo, User.Identity?.Name ?? "Admin", ct);
            TempData["Success"] = "Información creada exitosamente.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet("editar/{id}")]
        public async Task<IActionResult> Edit(Guid id, CancellationToken ct)
        {
            var info = await _publicInfos.GetByIdAsync(id, ct);
            if (info == null) return NotFound();

            await PopulateSelectsAsync(info.TermId, info.ModalityId, ct);
            return View("~/Pages/Admin/InfoManagement/PublicInfo/Edit.cshtml", info);
        }

        [HttpPost("editar/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, PublicInfo publicInfo, CancellationToken ct)
        {
            if (id != publicInfo.Id) return NotFound();
            if (!ModelState.IsValid)
            {
                await PopulateSelectsAsync(publicInfo.TermId, publicInfo.ModalityId, ct);
                return View("~/Pages/Admin/InfoManagement/PublicInfo/Edit.cshtml", publicInfo);
            }

            var ok = await _publicInfos.UpdateAsync(publicInfo, User.Identity?.Name ?? "Admin", ct);
            if (!ok) return NotFound();

            TempData["Success"] = "Información actualizada exitosamente.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet("modalities-by-term")]
        public async Task<IActionResult> GetModalitiesByTerm(Guid termId, CancellationToken ct)
        {
            var modalities = await _catalog.GetModalitiesAsync(termId, ct: ct);
            return Json(modalities.Select(m => new { id = m.Id, name = m.Name }));
        }

        [HttpPost("eliminar/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            var outcome = await _publicInfos.DeleteAsync(id, ct);
            switch (outcome)
            {
                case DeleteOutcome.Deleted:
                    TempData["Success"] = "Información eliminada exitosamente.";
                    break;
                case DeleteOutcome.HasDependencies:
                    TempData["Error"] = "No se puede eliminar la información porque tiene registros asociados.";
                    break;
                default:
                    TempData["Error"] = "No se encontró el registro.";
                    break;
            }
            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateSelectsAsync(Guid? selectedTermId, Guid? selectedModalityId, CancellationToken ct)
        {
            var terms = await _catalog.GetTermsAsync(ct: ct);
            ViewBag.Terms = terms.Select(t => new { id = t.Id.ToString(), name = t.Name }).ToList();

            ViewBag.Modalities = selectedTermId.HasValue
                ? (await _catalog.GetModalitiesAsync(selectedTermId.Value, ct: ct))
                    .Select(m => new { id = m.Id.ToString(), name = m.Name }).ToList()
                : new List<object>().Select(_ => new { id = "", name = "" }).ToList();

            ViewBag.SelectedTermId = selectedTermId;
            ViewBag.SelectedModalityId = selectedModalityId;
        }
    }
}
