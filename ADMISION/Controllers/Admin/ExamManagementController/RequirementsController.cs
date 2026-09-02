using ADMISION.ENTITIES.Constants;
using ADMISION.ENTITIES.Models.Requirement;
using ADMISION.Models.Shared;
using ADMISION.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace admision.Controllers.Admin.ExamManagementController
{
    [Authorize(Roles = AppConstants.Roles.SuperAdmin + "," + AppConstants.Roles.Admin)]
    [Route("admin/exam-management/documents")]
    public class RequirementsController : Controller
    {
        private readonly IFileRequirementService _requirements;

        public RequirementsController(IFileRequirementService requirements)
        {
            _requirements = requirements;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index(
            string? search,
            int page = 1, int pageSize = 20, string? sortBy = null, string? sortDir = "asc",
            CancellationToken ct = default)
        {
            var query = new ListQuery
            {
                Search = search,
                Page = page,
                PageSize = pageSize,
                SortBy = sortBy,
                SortDir = sortDir
            };

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                var paged = await _requirements.ListAsync(query, ct);
                return Json(new
                {
                    data = paged.Items.Select(x => new
                    {
                        x.Id,
                        x.Name,
                        x.Description,
                        x.MaxFileSizeMB,
                        x.FilePathExtencion
                    }),
                    recordsTotal = paged.TotalItems,
                    recordsFiltered = paged.TotalItems
                });
            }

            return View("~/Pages/Admin/ExamManagement/Requirements/Index.cshtml");
        }

        [HttpGet("crear")]
        public IActionResult Create()
        {
            PopulateFormData();
            return View("~/Pages/Admin/ExamManagement/Requirements/Create.cshtml");
        }

        [HttpPost("crear")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(FileRequirementManagement requirement, CancellationToken ct)
        {
            if (!ModelState.IsValid)
            {
                PopulateFormData();
                return View("~/Pages/Admin/ExamManagement/Requirements/Create.cshtml", requirement);
            }

            await _requirements.CreateAsync(requirement, User.Identity?.Name ?? "Admin", ct);
            TempData["Success"] = "Requisito creado exitosamente.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet("editar/{id}")]
        public async Task<IActionResult> Edit(Guid id, CancellationToken ct)
        {
            var entity = await _requirements.GetByIdAsync(id, ct);
            if (entity == null) return NotFound();

            PopulateFormData();
            return View("~/Pages/Admin/ExamManagement/Requirements/Edit.cshtml", entity);
        }

        [HttpPost("editar/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, FileRequirementManagement requirement, CancellationToken ct)
        {
            if (id != requirement.Id) return NotFound();
            if (!ModelState.IsValid)
            {
                PopulateFormData();
                return View("~/Pages/Admin/ExamManagement/Requirements/Edit.cshtml", requirement);
            }

            var ok = await _requirements.UpdateAsync(requirement, User.Identity?.Name ?? "Admin", ct);
            if (!ok) return NotFound();

            TempData["Success"] = "Requisito actualizado exitosamente.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost("eliminar/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            var outcome = await _requirements.DeleteAsync(id, ct);
            switch (outcome)
            {
                case RequirementDeleteOutcome.Deleted:
                    TempData["Success"] = "Requisito eliminado exitosamente.";
                    break;
                case RequirementDeleteOutcome.UsedByTypePostulant:
                    TempData["Error"] = "No se puede eliminar el requisito porque está vinculado a un tipo de postulante.";
                    break;
                case RequirementDeleteOutcome.HasOtherDependencies:
                    TempData["Error"] = "Ocurrió un error al eliminar el requisito.";
                    break;
                default:
                    TempData["Error"] = "No se encontró el requisito.";
                    break;
            }
            return RedirectToAction(nameof(Index));
        }

        private void PopulateFormData()
        {
            ViewBag.AllowedExtensions = AppConstants.FileExtensions.Allowed;
            ViewBag.StageOptions = AppConstants.RequirementStage.GetOptions();
        }
    }
}
