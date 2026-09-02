using ADMISION.ENTITIES.Constants;
using ADMISION.ENTITIES.Models.Postulant;
using ADMISION.Models.Shared;
using ADMISION.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace admision.Controllers.Admin.ExamManagementController
{
    [Authorize(Roles = AppConstants.Roles.SuperAdmin + "," + AppConstants.Roles.Admin)]
    [Route("admin/exam-management/applicant-types")]
    public class TypePostulantInscriptionController : Controller
    {
        private readonly ITypePostulantInscriptionService _types;

        public TypePostulantInscriptionController(ITypePostulantInscriptionService types)
        {
            _types = types;
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
                var paged = await _types.ListAsync(query, ct);
                return Json(new
                {
                    data = paged.Items.Select(x => new
                    {
                        x.Id,
                        x.Name,
                        x.Description,
                        x.DiscountPercentage,
                        x.IsActive
                    }),
                    recordsTotal = paged.TotalItems,
                    recordsFiltered = paged.TotalItems
                });
            }

            return View("~/Pages/Admin/ExamManagement/TypePostulantInscription/Index.cshtml");
        }

        [HttpGet("crear")]
        public IActionResult Create() => View("~/Pages/Admin/ExamManagement/TypePostulantInscription/Create.cshtml");

        [HttpPost("crear")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TypePostulantInscription typePostulant, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return View("~/Pages/Admin/ExamManagement/TypePostulantInscription/Create.cshtml", typePostulant);

            await _types.CreateAsync(typePostulant, User.Identity?.Name ?? "Admin", ct);
            TempData["Success"] = "Tipo de postulante creado exitosamente.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet("editar/{id}")]
        public async Task<IActionResult> Edit(Guid id, CancellationToken ct)
        {
            var entity = await _types.GetByIdAsync(id, ct);
            if (entity == null) return NotFound();
            return View("~/Pages/Admin/ExamManagement/TypePostulantInscription/Edit.cshtml", entity);
        }

        [HttpPost("editar/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, TypePostulantInscription typePostulant, CancellationToken ct)
        {
            if (id != typePostulant.Id) return NotFound();
            if (!ModelState.IsValid)
                return View("~/Pages/Admin/ExamManagement/TypePostulantInscription/Edit.cshtml", typePostulant);

            var ok = await _types.UpdateAsync(typePostulant, User.Identity?.Name ?? "Admin", ct);
            if (!ok) return NotFound();

            TempData["Success"] = "Tipo de postulante actualizado exitosamente.";
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
                    TempData["Success"] = "Tipo de postulante eliminado exitosamente.";
                    break;
                case DeleteOutcome.HasDependencies:
                    TempData["Error"] = "No se puede eliminar el tipo de postulante porque tiene registros asociados.";
                    break;
                default:
                    TempData["Error"] = "No se encontró el tipo de postulante.";
                    break;
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
