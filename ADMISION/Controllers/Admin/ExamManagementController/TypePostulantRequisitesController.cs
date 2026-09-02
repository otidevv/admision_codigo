using ADMISION.ENTITIES.Constants;
using ADMISION.ENTITIES.Models.Requirement;
using ADMISION.Models.Shared;
using ADMISION.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace admision.Controllers.Admin.ExamManagementController
{
    [Authorize(Roles = AppConstants.Roles.SuperAdmin + "," + AppConstants.Roles.Admin)]
    [Route("admin/exam-management/requirements-by-type-postulant")]
    public class TypePostulantRequisitesController : Controller
    {
        private readonly ITypePostulantRequisiteService _assignments;
        private readonly ITypePostulantInscriptionService _types;
        private readonly IFileRequirementService _requirements;

        public TypePostulantRequisitesController(
            ITypePostulantRequisiteService assignments,
            ITypePostulantInscriptionService types,
            IFileRequirementService requirements)
        {
            _assignments = assignments;
            _types = types;
            _requirements = requirements;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index(
            Guid? typePostulantInscriptionId,
            string? search,
            int page = 1, int pageSize = 20, string? sortBy = null, string? sortDir = "asc",
            CancellationToken ct = default)
        {
            var query = new TypePostulantRequisiteListQuery
            {
                TypePostulantInscriptionId = typePostulantInscriptionId,
                Search = search,
                Page = page,
                PageSize = pageSize,
                SortBy = sortBy,
                SortDir = sortDir
            };

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                var paged = await _assignments.ListAsync(query, ct);
                return Json(new
                {
                    data = paged.Items.Select(x => new
                    {
                        x.Id,
                        TypePostulant = x.TypePostulantName == null ? null : new { Name = x.TypePostulantName },
                        Requirement = x.RequirementName == null ? null : new { Name = x.RequirementName }
                    }),
                    recordsTotal = paged.TotalItems,
                    recordsFiltered = paged.TotalItems
                });
            }

            // Lista completa de tipos de postulante para el filtro principal de la vista.
            var typeList = await _types.ListAsync(new ListQuery { PageSize = 500 }, ct);
            ViewBag.PostulantTypes = typeList.Items;
            return View("~/Pages/Admin/ExamManagement/TypePostulantRequisites/Index.cshtml");
        }

        [HttpGet("crear")]
        public async Task<IActionResult> Create(Guid? typePostulantInscriptionId, CancellationToken ct)
        {
            await PopulateFormDataAsync(ct);
            ViewBag.SelectedTypePostulantId = typePostulantInscriptionId;
            return View("~/Pages/Admin/ExamManagement/TypePostulantRequisites/Create.cshtml");
        }

        [HttpPost("crear")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TypePostulantRequisite typePostulantRequisite, CancellationToken ct)
        {
            if (!ModelState.IsValid)
            {
                await PopulateFormDataAsync(ct);
                return View("~/Pages/Admin/ExamManagement/TypePostulantRequisites/Create.cshtml", typePostulantRequisite);
            }

            var outcome = await _assignments.CreateAsync(typePostulantRequisite, User.Identity?.Name ?? "Admin", ct);
            if (outcome.AlreadyExists)
            {
                ModelState.AddModelError("", "Este requisito ya está asignado a este tipo de postulante.");
                await PopulateFormDataAsync(ct);
                return View("~/Pages/Admin/ExamManagement/TypePostulantRequisites/Create.cshtml", typePostulantRequisite);
            }

            TempData["Success"] = "Requisito asignado correctamente.";
            return RedirectToAction(nameof(Index), new { typePostulantInscriptionId = outcome.TypePostulantInscriptionId });
        }

        [HttpPost("eliminar/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            var result = await _assignments.DeleteAsync(id, ct);
            switch (result.Outcome)
            {
                case DeleteOutcome.Deleted:
                    TempData["Success"] = "Asignación eliminada correctamente.";
                    break;
                case DeleteOutcome.HasDependencies:
                    TempData["Error"] = "No se puede eliminar la asignación porque tiene registros asociados.";
                    break;
                default:
                    TempData["Error"] = "No se encontró el registro.";
                    return RedirectToAction(nameof(Index));
            }
            return RedirectToAction(nameof(Index), new { typePostulantInscriptionId = result.TypePostulantInscriptionId });
        }

        private async Task PopulateFormDataAsync(CancellationToken ct)
        {
            var types = await _types.ListAsync(new ListQuery { PageSize = 500 }, ct);
            var reqs = await _requirements.ListAsync(new ListQuery { PageSize = 500 }, ct);
            ViewData["PostulantTypes"] = types.Items;
            ViewData["Requirements"] = reqs.Items;
        }
    }
}
