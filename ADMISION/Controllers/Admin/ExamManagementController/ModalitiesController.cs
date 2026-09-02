using ADMISION.ENTITIES.Constants;
using ADMISION.ENTITIES.Data;
using ADMISION.ENTITIES.Models.Modality;
using ADMISION.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace admision.Controllers.Admin.ExamManagementController
{
    [Authorize(Roles = AppConstants.Roles.SuperAdmin + "," + AppConstants.Roles.Admin)]
    [Route("admin/exam-management/modalities")]
    public class ModalitiesController : Controller
    {
        private readonly IModalityService _modalities;
        private readonly ICatalogService _catalog;
        private readonly AppDbContext _context;

        public ModalitiesController(IModalityService modalities, ICatalogService catalog, AppDbContext context)
        {
            _modalities = modalities;
            _catalog = catalog;
            _context = context;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index(
            Guid? termId, string? search,
            int page = 1, int pageSize = 20, string? sortBy = null, string? sortDir = "asc",
            CancellationToken ct = default)
        {
            var query = new ModalityListQuery
            {
                TermId = termId,
                Search = search,
                Page = page,
                PageSize = pageSize,
                SortBy = sortBy,
                SortDir = sortDir
            };

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                var paged = await _modalities.ListAsync(query, ct);
                return Json(new
                {
                    data = paged.Items.Select(x => new
                    {
                        x.Id,
                        x.Name,
                        x.Description,
                        x.IsActive,
                        x.Orden,
                        x.IsCepreExam,
                        x.RequiresProfilePhoto,
                        x.IsMockExam,
                        x.RequiresEducationalLevel,
                        x.RequiresGrade,
                        x.StartDate,
                        x.EndDate,
                        x.StartTime,
                        x.EndTime,
                        Term = x.TermName == null ? null : new { Name = x.TermName }
                    }),
                    recordsTotal = paged.TotalItems,
                    recordsFiltered = paged.TotalItems
                });
            }

            var terms = await _catalog.GetTermsAsync(ct: ct);
            ViewBag.Terms = terms;
            ViewBag.DefaultTermId = await GetDefaultTermIdAsync(ct);

            return View("~/Pages/Admin/ExamManagement/Modalities/Index.cshtml");
        }

        [HttpGet("crear")]
        public async Task<IActionResult> Create(CancellationToken ct)
        {
            await PopulateFormDataAsync(null, ct);
            return View("~/Pages/Admin/ExamManagement/Modalities/Create.cshtml");
        }

        [HttpPost("crear")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Modality modality, List<Guid>? careerIds, CancellationToken ct)
        {
            var result = await _modalities.CreateAsync(modality, User.Identity?.Name ?? "Admin", ct);
            ApplyValidationErrors(result);

            if (!result.Succeeded || !ModelState.IsValid)
            {
                await PopulateFormDataAsync(modality.TermId, ct);
                return View("~/Pages/Admin/ExamManagement/Modalities/Create.cshtml", modality);
            }

            await _modalities.SaveCareerAssociationsAsync(modality.Id, careerIds ?? new List<Guid>(), ct);

            TempData["Success"] = "Modalidad creada exitosamente.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet("editar/{id}")]
        public async Task<IActionResult> Edit(Guid id, CancellationToken ct)
        {
            var modality = await _modalities.GetByIdAsync(id, ct);
            if (modality == null) return NotFound();

            await PopulateFormDataAsync(modality.TermId, ct);
            ViewBag.AssociatedCareerIds = await _modalities.GetCareerIdsAsync(id, ct);
            return View("~/Pages/Admin/ExamManagement/Modalities/Edit.cshtml", modality);
        }

        [HttpPost("editar/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, Modality modality, List<Guid>? careerIds, CancellationToken ct)
        {
            if (id != modality.Id) return NotFound();

            var result = await _modalities.UpdateAsync(modality, User.Identity?.Name ?? "Admin", ct);
            if (result.NotFound) return NotFound();

            ApplyValidationErrors(result);

            if (!result.Succeeded || !ModelState.IsValid)
            {
                await PopulateFormDataAsync(modality.TermId, ct);
                return View("~/Pages/Admin/ExamManagement/Modalities/Edit.cshtml", modality);
            }

            await _modalities.SaveCareerAssociationsAsync(modality.Id, careerIds ?? new List<Guid>(), ct);

            TempData["Success"] = "Modalidad actualizada exitosamente.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost("eliminar/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            var outcome = await _modalities.DeleteAsync(id, ct);
            switch (outcome)
            {
                case DeleteOutcome.Deleted:
                    TempData["Success"] = "Modalidad eliminada exitosamente.";
                    break;
                case DeleteOutcome.HasDependencies:
                    TempData["Error"] = "No se puede eliminar la modalidad porque tiene registros asociados.";
                    break;
                default:
                    TempData["Error"] = "No se encontró la modalidad.";
                    break;
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpGet("get-by-term/{termId}")]
        public async Task<IActionResult> GetByTerm(Guid termId, CancellationToken ct)
        {
            var modalities = await _modalities.GetByTermAsync(termId, ct);
            return Json(modalities.Select(m => new { id = m.Id, name = m.Name }));
        }

        // ============ Helpers privados ============
        private async Task PopulateFormDataAsync(Guid? selectedTermId, CancellationToken ct)
        {
            var terms = await _catalog.GetTermsAsync(ct: ct);
            ViewData["TermId"] = new SelectList(terms, nameof(CatalogOption.Id), nameof(CatalogOption.Name), selectedTermId);
            ViewBag.BadgeOptions = AppConstants.ModalityBadge.GetOptions();
            ViewBag.IconOptions = AppConstants.ModalityIcon.GetOptions();

            var careers = await _context.Careers
                .AsNoTracking()
                .Include(c => c.Faculty)
                .OrderBy(c => c.Name)
                .ToListAsync(ct);
            ViewBag.AllCareers = careers;
        }

        private async Task<Guid?> GetDefaultTermIdAsync(CancellationToken ct)
        {
            var activeTerms = await _catalog.GetTermsAsync(onlyActive: true, ct: ct);
            if (activeTerms.Any()) return activeTerms.First().Id;

            var allTerms = await _catalog.GetTermsAsync(ct: ct);
            return allTerms.FirstOrDefault()?.Id;
        }

        private void ApplyValidationErrors(ADMISION.Models.Shared.SaveResult result)
        {
            foreach (var err in result.Errors)
            {
                ModelState.AddModelError(err.Field, err.Message);
            }
        }
    }
}
