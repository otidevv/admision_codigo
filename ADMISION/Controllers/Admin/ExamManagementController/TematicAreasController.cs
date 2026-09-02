using ADMISION.ENTITIES.Constants;
using ADMISION.ENTITIES.Models.Modality;
using ADMISION.Models.Shared;
using ADMISION.Models.ViewModels.Admin;
using ADMISION.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace admision.Controllers.Admin.ExamManagementController
{
    [Authorize(Roles = AppConstants.Roles.SuperAdmin + "," + AppConstants.Roles.Admin)]
    [Route("admin/exam-management/tematic-areas")]
    public class TematicAreasController : Controller
    {
        private readonly ITematicAreaService _areas;
        private readonly ICatalogService _catalog;

        public TematicAreasController(ITematicAreaService areas, ICatalogService catalog)
        {
            _areas = areas;
            _catalog = catalog;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index(string? search, int page = 1, int pageSize = 20, string? sortBy = null, string? sortDir = "asc", CancellationToken ct = default)
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
                var paged = await _areas.ListAsync(query, ct);
                return Json(new
                {
                    data = paged.Items.Select(x => new { x.Id, x.Code, x.CreatedAt }),
                    recordsTotal = paged.TotalItems,
                    recordsFiltered = paged.TotalItems
                });
            }

            return View("~/Pages/Admin/ExamManagement/TematicAreas/Index.cshtml");
        }

        [HttpGet("crear")]
        public IActionResult Create() => View("~/Pages/Admin/ExamManagement/TematicAreas/Create.cshtml");

        [HttpPost("crear")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TematicArea area, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return View("~/Pages/Admin/ExamManagement/TematicAreas/Create.cshtml", area);

            await _areas.CreateAsync(area, User.Identity?.Name ?? "Admin", ct);
            TempData["Success"] = "Área temática creada exitosamente.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet("editar/{id}")]
        public async Task<IActionResult> Edit(Guid id, CancellationToken ct)
        {
            var area = await _areas.GetByIdAsync(id, ct);
            if (area == null) return NotFound();
            return View("~/Pages/Admin/ExamManagement/TematicAreas/Edit.cshtml", area);
        }

        [HttpPost("editar/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, TematicArea area, CancellationToken ct)
        {
            if (id != area.Id) return NotFound();
            if (!ModelState.IsValid)
                return View("~/Pages/Admin/ExamManagement/TematicAreas/Edit.cshtml", area);

            var ok = await _areas.UpdateAsync(area, User.Identity?.Name ?? "Admin", ct);
            if (!ok) return NotFound();

            TempData["Success"] = "Área temática actualizada exitosamente.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost("eliminar/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            var outcome = await _areas.DeleteAsync(id, ct);
            switch (outcome)
            {
                case DeleteOutcome.Deleted:
                    TempData["Success"] = "Área temática eliminada exitosamente.";
                    break;
                case DeleteOutcome.HasDependencies:
                    TempData["Error"] = "No se puede eliminar el área temática porque tiene carreras asociadas.";
                    break;
                default:
                    TempData["Error"] = "No se encontró el área temática.";
                    break;
            }
            return RedirectToAction(nameof(Index));
        }

        // ============ Asignaciones (matriz carrera × área) ============
        [HttpGet("asignaciones")]
        public async Task<IActionResult> Assignments(Guid? termId, CancellationToken ct)
        {
            var terms = await _catalog.GetTermsAsync(ct: ct);
            ViewBag.Terms = terms;
            ViewBag.DefaultTermId = termId
                ?? (await _catalog.GetTermsAsync(onlyActive: true, ct: ct)).FirstOrDefault()?.Id
                ?? terms.FirstOrDefault()?.Id;
            ViewBag.TematicAreas = await _areas.GetAllAsync(ct);

            return View("~/Pages/Admin/ExamManagement/TematicAreas/Assignments.cshtml");
        }

        [HttpGet("get-matrix")]
        public async Task<IActionResult> GetMatrixData(Guid termId, CancellationToken ct)
        {
            var matrix = await _areas.GetMatrixAsync(termId, ct);
            return Json(new
            {
                data = matrix.Select(c => new { c.Id, c.Name, Assignments = c.TematicAreaIds })
            });
        }

        [HttpPost("save-matrix")]
        public async Task<IActionResult> SaveMatrix([FromBody] BulkAssignmentRequest request, CancellationToken ct)
        {
            if (request == null || request.TermId == Guid.Empty)
                return BadRequest(new { success = false, message = "Debe indicar un periodo válido." });

            if (request.Assignments == null || request.Assignments.Count == 0)
                return Json(new { success = true, message = "Sin cambios." });

            var assignments = request.Assignments
                .Select(a => new CareerTematicAreaAssignment { CareerId = a.CareerId, TematicAreaIds = a.TematicAreaIds ?? new() })
                .ToList();

            await _areas.SaveMatrixAsync(request.TermId, assignments, User.Identity?.Name ?? "Admin", ct);
            return Json(new { success = true });
        }

        [HttpPost("asignaciones")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Assignments(TematicAreaAssignmentViewModel model, CancellationToken ct)
        {
            if (model.TermId == Guid.Empty || model.CareerId == Guid.Empty)
            {
                TempData["Error"] = "Debe seleccionar un periodo y una carrera.";
                return RedirectToAction(nameof(Assignments), new { termId = model.TermId, careerId = model.CareerId });
            }

            var selectedIds = model.Selections.Where(s => s.IsSelected).Select(s => s.TematicAreaId).ToList();
            await _areas.SaveCareerAssignmentsAsync(model.TermId, model.CareerId, selectedIds, User.Identity?.Name ?? "Admin", ct);

            TempData["Success"] = "Asignaciones actualizadas exitosamente.";
            return RedirectToAction(nameof(Assignments), new { termId = model.TermId, careerId = model.CareerId });
        }

        // ============ DTOs del POST bulk ============
        public class BulkAssignmentRequest
        {
            public Guid TermId { get; set; }
            public List<CareerAssignment> Assignments { get; set; } = new();
        }

        public class CareerAssignment
        {
            public Guid CareerId { get; set; }
            public List<Guid> TematicAreaIds { get; set; } = new();
        }
    }
}
