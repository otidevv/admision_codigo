using ADMISION.ENTITIES.Constants;
using ADMISION.ENTITIES.Models.Modality;
using ADMISION.Extensions;
using ADMISION.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace admision.Controllers.Admin.ExamManagementController
{
    [Authorize(Roles = AppConstants.Roles.SuperAdmin + "," + AppConstants.Roles.Admin)]
    [Route("admin/exam-management/careers")]
    public class CareersController : Controller
    {
        private readonly ICareerService _careers;
        private readonly ICatalogService _catalog;
        private readonly ILogger<CareersController> _logger;

        public CareersController(ICareerService careers, ICatalogService catalog, ILogger<CareersController> logger)
        {
            _careers = careers;
            _catalog = catalog;
            _logger = logger;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index(Guid? facultyId, string? search, int page = 1, int pageSize = 20, string? sortBy = null, string? sortDir = "asc", CancellationToken ct = default)
        {
            var listQuery = new CareerListQuery
            {
                FacultyId = facultyId,
                Search = search,
                Page = page,
                PageSize = pageSize,
                SortBy = sortBy,
                SortDir = sortDir
            };

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                var paged = await _careers.ListAsync(listQuery, ct);
                return Json(new
                {
                    data = paged.Items.Select(x => new
                    {
                        x.Id,
                        x.Name,
                        x.Code,
                        x.ProgramNumber,
                        x.IsActive,
                        Faculty = x.FacultyName == null ? null : new { Name = x.FacultyName }
                    }),
                    recordsTotal = paged.TotalItems,
                    recordsFiltered = paged.TotalItems
                });
            }

            ViewBag.Faculties = await _catalog.GetFacultiesAsync(ct);
            return View("~/Pages/Admin/ExamManagement/Careers/Index.cshtml");
        }
        [HttpGet("crear")]
        public async Task<IActionResult> Create(CancellationToken ct)
        {
            ViewData["FacultyId"] = await BuildFacultySelectAsync(null, ct);
            return View("~/Pages/Admin/ExamManagement/Careers/Create.cshtml");
        }
        [HttpPost("crear")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Career career, IFormFile? logoFile, IFormFile? bannerFile, IFormFile? studyPlanFile, List<IFormFile>? galleryFiles, CancellationToken ct)
        {
            if (!ModelState.IsValid)
            {
                ViewData["FacultyId"] = await BuildFacultySelectAsync(career.FacultyId, ct);
                return View("~/Pages/Admin/ExamManagement/Careers/Create.cshtml", career);
            }

            try
            {
                var files = new CareerFiles(logoFile, bannerFile, studyPlanFile, galleryFiles);
                await _careers.CreateAsync(career, files, User.Identity?.Name ?? "Admin", ct);
                TempData["Success"] = "Carrera creada exitosamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                this.SetSaveError(ex, _logger);
                ViewData["FacultyId"] = await BuildFacultySelectAsync(career.FacultyId, ct);
                return View("~/Pages/Admin/ExamManagement/Careers/Create.cshtml", career);
            }
        }
        [HttpGet("editar/{id}")]
        public async Task<IActionResult> Edit(Guid id, CancellationToken ct)
        {
            var career = await _careers.GetByIdAsync(id, ct);
            if (career == null) return NotFound();

            ViewData["FacultyId"] = await BuildFacultySelectAsync(career.FacultyId, ct);
            return View("~/Pages/Admin/ExamManagement/Careers/Edit.cshtml", career);
        }
        [HttpPost("editar/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, Career career, IFormFile? logoFile, IFormFile? bannerFile, IFormFile? studyPlanFile, List<IFormFile>? galleryFiles, CancellationToken ct)
        {
            if (id != career.Id) return NotFound();

            if (!ModelState.IsValid)
            {
                ViewData["FacultyId"] = await BuildFacultySelectAsync(career.FacultyId, ct);
                return View("~/Pages/Admin/ExamManagement/Careers/Edit.cshtml", career);
            }

            try
            {
                var files = new CareerFiles(logoFile, bannerFile, studyPlanFile, galleryFiles);
                var ok = await _careers.UpdateAsync(career, files, User.Identity?.Name ?? "Admin", ct);
                if (!ok) return NotFound();

                TempData["Success"] = "Carrera actualizada exitosamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                this.SetSaveError(ex, _logger);
                ViewData["FacultyId"] = await BuildFacultySelectAsync(career.FacultyId, ct);
                return View("~/Pages/Admin/ExamManagement/Careers/Edit.cshtml", career);
            }
        }

        [HttpPost("{id:guid}/imagenes/{imageId:guid}/eliminar")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteImage(Guid id, Guid imageId, CancellationToken ct)
        {
            var ok = await _careers.DeleteImageAsync(id, imageId, ct);
            if (ok) TempData["Success"] = "Imagen eliminada.";
            else TempData["Error"] = "No se encontró la imagen.";
            return RedirectToAction(nameof(Edit), new { id });
        }

        [HttpPost("eliminar/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            var outcome = await _careers.DeleteAsync(id, ct);
            switch (outcome)
            {
                case DeleteOutcome.Deleted:
                    TempData["Success"] = "Carrera eliminada exitosamente.";
                    break;
                case DeleteOutcome.HasDependencies:
                    TempData["Error"] = "No se puede eliminar la carrera porque tiene registros asociados.";
                    break;
                default:
                    TempData["Error"] = "No se encontró la carrera.";
                    break;
            }
            return RedirectToAction(nameof(Index));
        }

        private async Task<SelectList> BuildFacultySelectAsync(Guid? selected, CancellationToken ct)
        {
            var faculties = await _catalog.GetFacultiesAsync(ct);
            return new SelectList(faculties, nameof(CatalogOption.Id), nameof(CatalogOption.Name), selected);
        }
    }
}
