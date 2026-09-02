using ADMISION.ENTITIES.Constants;
using ADMISION.ENTITIES.Data;
using ADMISION.ENTITIES.Models.Modality;
using ADMISION.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace admision.Controllers.Admin.ExamManagementController
{
    [Authorize(Roles = AppConstants.Roles.SuperAdmin + "," + AppConstants.Roles.Admin)]
    [Route("admin/exam-management/modality-types")]
    public class TypeModalitiesController : Controller
    {
        private readonly ITypeModalityService _types;
        private readonly ICatalogService _catalog;
        private readonly AppDbContext _context;

        public TypeModalitiesController(ITypeModalityService types, ICatalogService catalog, AppDbContext context)
        {
            _types = types;
            _catalog = catalog;
            _context = context;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index(
            Guid? termId, Guid? modalityId, string? search,
            int page = 1, int pageSize = 20, string? sortBy = null, string? sortDir = "asc",
            CancellationToken ct = default)
        {
            var query = new TypeModalityListQuery
            {
                TermId = termId,
                ModalityId = modalityId,
                Search = search,
                Page = page,
                PageSize = pageSize,
                SortBy = sortBy,
                SortDir = sortDir
            };

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                var paged = await _types.ListAsync(query, ct);
                return Json(new
                {
                    data = paged.Items.Select(x => new
                    {
                        x.Id,
                        x.Name,
                        x.Description,
                        x.DiscountPercentage,
                        x.IsActive,
                        Modality = x.ModalityName == null ? null : new { Name = x.ModalityName }
                    }),
                    recordsTotal = paged.TotalItems,
                    recordsFiltered = paged.TotalItems
                });
            }

            var terms = await _catalog.GetTermsAsync(ct: ct);
            var activeTermId = await GetDefaultTermIdAsync(ct);

            ViewBag.Terms = terms;
            ViewBag.DefaultTermId = activeTermId;
            ViewBag.Modalities = activeTermId.HasValue
                ? await _catalog.GetModalitiesAsync(activeTermId.Value, ct: ct)
                : Array.Empty<CatalogOption>();

            return View("~/Pages/Admin/ExamManagement/TypeModalities/Index.cshtml");
        }

        [HttpGet("crear")]
        public async Task<IActionResult> Create(CancellationToken ct)
        {
            await PopulateFormDataAsync(currentTermId: null, ct);
            return View("~/Pages/Admin/ExamManagement/TypeModalities/Create.cshtml");
        }

        [HttpPost("crear")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TypeModality typeModality, List<Guid>? careerIds, CancellationToken ct)
        {
            if (!ModelState.IsValid)
            {
                await PopulateFormDataAsync(currentTermId: null, ct);
                return View("~/Pages/Admin/ExamManagement/TypeModalities/Create.cshtml", typeModality);
            }

            var created = await _types.CreateAsync(typeModality, User.Identity?.Name ?? "Admin", ct);
            await _types.SaveCareerAssociationsAsync(created.Id, careerIds ?? new List<Guid>(), ct);
            TempData["Success"] = "Tipo de modalidad creado exitosamente.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet("editar/{id}")]
        public async Task<IActionResult> Edit(Guid id, CancellationToken ct)
        {
            var entity = await _types.GetByIdAsync(id, includeModality: true, ct);
            if (entity == null) return NotFound();

            await PopulateFormDataAsync(entity.Modality?.TermId, ct);
            ViewBag.AssociatedCareerIds = await _types.GetCareerIdsAsync(id, ct);
            return View("~/Pages/Admin/ExamManagement/TypeModalities/Edit.cshtml", entity);
        }

        [HttpPost("editar/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, TypeModality typeModality, List<Guid>? careerIds, CancellationToken ct)
        {
            if (id != typeModality.Id) return NotFound();

            if (!ModelState.IsValid)
            {
                var modality = await _types.GetByIdAsync(typeModality.Id, includeModality: true, ct);
                await PopulateFormDataAsync(modality?.Modality?.TermId, ct);
                ViewBag.AssociatedCareerIds = await _types.GetCareerIdsAsync(typeModality.Id, ct);
                return View("~/Pages/Admin/ExamManagement/TypeModalities/Edit.cshtml", typeModality);
            }

            var ok = await _types.UpdateAsync(typeModality, User.Identity?.Name ?? "Admin", ct);
            if (!ok) return NotFound();

            await _types.SaveCareerAssociationsAsync(typeModality.Id, careerIds ?? new List<Guid>(), ct);
            TempData["Success"] = "Tipo de modalidad actualizado exitosamente.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost("eliminar/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            var outcome = await _types.DeleteAsync(id, ct);
            switch (outcome)
            {
                case DeleteOutcome.Deleted:
                    TempData["Success"] = "Tipo de modalidad eliminado exitosamente.";
                    break;
                case DeleteOutcome.HasDependencies:
                    TempData["Error"] = "No se puede eliminar el tipo de modalidad porque tiene registros asociados.";
                    break;
                default:
                    TempData["Error"] = "No se encontró el tipo de modalidad.";
                    break;
            }
            return RedirectToAction(nameof(Index));
        }

        // ============ Helpers privados ============
        private async Task PopulateFormDataAsync(Guid? currentTermId, CancellationToken ct)
        {
            var terms = await _catalog.GetTermsAsync(ct: ct);
            ViewData["Terms"] = terms;
            ViewData["CurrentTermId"] = currentTermId ?? await GetDefaultTermIdAsync(ct);

            if (currentTermId.HasValue)
            {
                ViewData["Modalities"] = await _catalog.GetModalitiesAsync(currentTermId.Value, ct: ct);
            }

            var careers = await _context.Careers
                .AsNoTracking()
                .Include(c => c.Faculty)
                .OrderBy(c => c.Name)
                .ToListAsync(ct);
            ViewBag.AllCareers = careers;

            var modalityCareerMap = await _context.ModalityCareers
                .AsNoTracking()
                .GroupBy(mc => mc.ModalityId)
                .ToDictionaryAsync(g => g.Key, g => g.Select(mc => mc.CareerId).ToList(), ct);
            ViewBag.ModalityCareerMap = modalityCareerMap;
        }

        private async Task<Guid?> GetDefaultTermIdAsync(CancellationToken ct)
        {
            var active = await _catalog.GetTermsAsync(onlyActive: true, ct: ct);
            if (active.Any()) return active.First().Id;

            var all = await _catalog.GetTermsAsync(ct: ct);
            return all.FirstOrDefault()?.Id;
        }
    }
}
