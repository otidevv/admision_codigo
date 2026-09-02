using ADMISION.ENTITIES.Constants;
using ADMISION.ENTITIES.Models.Infrastructure;
using ADMISION.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace admision.Controllers.Admin.InfrastructureController
{
    [Authorize(Roles = AppConstants.Roles.SuperAdmin + "," + AppConstants.Roles.Admin)]
    [Route("admin/infrastructure/classrooms")]
    public class ClassroomsController : Controller
    {
        private readonly IClassroomService _classrooms;

        public ClassroomsController(IClassroomService classrooms)
        {
            _classrooms = classrooms;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index(Guid? pavilionId, CancellationToken ct)
        {
            var list = await _classrooms.GetAllAsync(pavilionId, ct);

            ViewBag.Pavilions = await _classrooms.GetActivePavilionsAsync(ct);
            ViewBag.SelectedPavilionId = pavilionId;
            return View("~/Pages/Admin/Infrastructure/Classrooms/Index.cshtml", list);
        }

        [HttpGet("crear")]
        public async Task<IActionResult> Create(CancellationToken ct)
        {
            await PopulatePavilionsAsync(ct);
            return View("~/Pages/Admin/Infrastructure/Classrooms/Create.cshtml",
                new Classroom { Floor = 1, Capacity = 30, IsActive = true });
        }

        [HttpPost("crear")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Classroom classroom, CancellationToken ct)
        {
            if (!ModelState.IsValid)
            {
                await PopulatePavilionsAsync(ct);
                return View("~/Pages/Admin/Infrastructure/Classrooms/Create.cshtml", classroom);
            }

            await _classrooms.CreateAsync(classroom, User.Identity?.Name ?? "Admin", ct);
            TempData["Success"] = "Salón registrado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet("editar/{id}")]
        public async Task<IActionResult> Edit(Guid id, CancellationToken ct)
        {
            var classroom = await _classrooms.GetByIdAsync(id, ct);
            if (classroom == null) return NotFound();

            await PopulatePavilionsAsync(ct);
            return View("~/Pages/Admin/Infrastructure/Classrooms/Edit.cshtml", classroom);
        }

        [HttpPost("editar/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, Classroom classroom, CancellationToken ct)
        {
            if (id != classroom.Id) return NotFound();
            if (!ModelState.IsValid)
            {
                await PopulatePavilionsAsync(ct);
                return View("~/Pages/Admin/Infrastructure/Classrooms/Edit.cshtml", classroom);
            }

            var ok = await _classrooms.UpdateAsync(classroom, User.Identity?.Name ?? "Admin", ct);
            if (!ok) return NotFound();

            TempData["Success"] = "Salón actualizado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost("eliminar/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            var outcome = await _classrooms.DeleteAsync(id, ct);
            switch (outcome)
            {
                case DeleteOutcome.Deleted:
                    TempData["Success"] = "Salón eliminado.";
                    break;
                case DeleteOutcome.HasDependencies:
                    TempData["Error"] = "No se pudo eliminar: el salón tiene asignaciones asociadas.";
                    break;
                default:
                    TempData["Error"] = "Salón no encontrado.";
                    break;
            }
            return RedirectToAction(nameof(Index));
        }

        private async Task PopulatePavilionsAsync(CancellationToken ct)
        {
            var pavilions = await _classrooms.GetActivePavilionsAsync(ct);
            ViewBag.Pavilions = pavilions
                .Select(p => new { p.Id, Name = p.Name + (string.IsNullOrEmpty(p.Code) ? "" : " (" + p.Code + ")") })
                .ToList();
        }
    }
}
