using ADMISION.ENTITIES.Constants;
using ADMISION.ENTITIES.Models.Postulant;
using ADMISION.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ADMISION.Controllers.Admin.ExamManagementController
{
    [Authorize(Roles = AppConstants.Roles.SuperAdmin + "," + AppConstants.Roles.Admin)]
    [Route("admin/exam-management/disability-types")]
    public class DisabilityTypeController : Controller
    {
        private readonly IDisabilityTypeService _disabilities;

        public DisabilityTypeController(IDisabilityTypeService disabilities)
        {
            _disabilities = disabilities;
        }

        [HttpGet]
        public IActionResult Index() => View("~/Pages/Admin/ExamManagement/DisabilityTypes/Index.cshtml");

        [HttpGet("list")]
        public async Task<IActionResult> List(string? search, bool? isActive, CancellationToken ct)
        {
            var data = await _disabilities.ListAsync(search, isActive, ct);
            return Json(new
            {
                data = data.Select(x => new
                {
                    id = x.Id,
                    name = x.Name,
                    description = x.Description,
                    isActive = x.IsActive
                })
            });
        }

        [HttpGet("create")]
        public IActionResult Create() => View("~/Pages/Admin/ExamManagement/DisabilityTypes/Create.cshtml");

        [HttpPost("create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DisabilityType model, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return View("~/Pages/Admin/ExamManagement/DisabilityTypes/Create.cshtml", model);

            await _disabilities.CreateAsync(model, User.Identity?.Name ?? "Admin", ct);
            return RedirectToAction(nameof(Index));
        }

        [HttpGet("edit/{id}")]
        public async Task<IActionResult> Edit(Guid id, CancellationToken ct)
        {
            var type = await _disabilities.GetByIdAsync(id, ct);
            if (type == null) return NotFound();
            return View("~/Pages/Admin/ExamManagement/DisabilityTypes/Edit.cshtml", type);
        }

        [HttpPost("edit/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, DisabilityType model, CancellationToken ct)
        {
            if (id != model.Id) return BadRequest();
            if (!ModelState.IsValid)
                return View("~/Pages/Admin/ExamManagement/DisabilityTypes/Edit.cshtml", model);

            var ok = await _disabilities.UpdateAsync(model, User.Identity?.Name ?? "Admin", ct);
            if (!ok) return NotFound();

            return RedirectToAction(nameof(Index));
        }

        [HttpPost("delete/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            var outcome = await _disabilities.DeleteAsync(id, ct);
            switch (outcome)
            {
                case DeleteOutcome.Deleted:
                    TempData["Success"] = "Tipo de discapacidad eliminado exitosamente.";
                    break;
                case DeleteOutcome.HasDependencies:
                    TempData["Error"] = "No se puede eliminar el tipo de discapacidad porque tiene registros asociados.";
                    break;
                default:
                    TempData["Error"] = "No se encontró el tipo de discapacidad.";
                    break;
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
