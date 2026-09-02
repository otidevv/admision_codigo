using ADMISION.ENTITIES.Constants;
using ADMISION.ENTITIES.Models.Modality;
using ADMISION.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace admision.Controllers.Admin.ExamManagementController
{
    [Authorize(Roles = AppConstants.Roles.SuperAdmin + "," + AppConstants.Roles.Admin)]
    [Route("admin/exam-management/faculties")]
    public class FacultiesController : Controller
    {
        private readonly IFacultyService _faculties;

        public FacultiesController(IFacultyService faculties)
        {
            _faculties = faculties;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index(CancellationToken ct)
        {
            var list = await _faculties.GetAllAsync(ct);
            return View("~/Pages/Admin/ExamManagement/Faculties/Index.cshtml", list);
        }

        [HttpGet("crear")]
        public IActionResult Create()
        {
            return View("~/Pages/Admin/ExamManagement/Faculties/Create.cshtml");
        }

        [HttpPost("crear")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Faculty faculty, CancellationToken ct)
        {
            if (!ModelState.IsValid)
            {
                return View("~/Pages/Admin/ExamManagement/Faculties/Create.cshtml", faculty);
            }

            await _faculties.CreateAsync(faculty, User.Identity?.Name ?? "Admin", ct);
            TempData["Success"] = "Facultad creada exitosamente.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet("editar/{id}")]
        public async Task<IActionResult> Edit(Guid id, CancellationToken ct)
        {
            var faculty = await _faculties.GetByIdAsync(id, ct);
            if (faculty == null) return NotFound();
            return View("~/Pages/Admin/ExamManagement/Faculties/Edit.cshtml", faculty);
        }

        [HttpPost("editar/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, Faculty faculty, CancellationToken ct)
        {
            if (id != faculty.Id) return NotFound();
            if (!ModelState.IsValid)
            {
                return View("~/Pages/Admin/ExamManagement/Faculties/Edit.cshtml", faculty);
            }

            var ok = await _faculties.UpdateAsync(faculty, User.Identity?.Name ?? "Admin", ct);
            if (!ok) return NotFound();

            TempData["Success"] = "Facultad actualizada exitosamente.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost("eliminar/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            var outcome = await _faculties.DeleteAsync(id, ct);
            switch (outcome)
            {
                case DeleteOutcome.Deleted:
                    TempData["Success"] = "Facultad eliminada exitosamente.";
                    break;
                case DeleteOutcome.HasDependencies:
                    TempData["Error"] = "No se puede eliminar la facultad porque tiene registros asociados.";
                    break;
                default:
                    TempData["Error"] = "No se encontró la facultad.";
                    break;
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
