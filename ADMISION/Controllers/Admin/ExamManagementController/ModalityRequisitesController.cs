using ADMISION.ENTITIES.Constants;
using ADMISION.ENTITIES.Models.Requirement;
using ADMISION.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace admision.Controllers.Admin.ExamManagementController
{
    [Authorize(Roles = AppConstants.Roles.SuperAdmin + "," + AppConstants.Roles.Admin)]
    [Route("admin/exam-management/requirements-by-modality")]
    public class ModalityRequisitesController : Controller
    {
        private readonly IModalityRequisiteService _assignments;
        private readonly ICatalogService _catalog;
        private readonly IFileRequirementService _requirements;

        public ModalityRequisitesController(
            IModalityRequisiteService assignments,
            ICatalogService catalog,
            IFileRequirementService requirements)
        {
            _assignments = assignments;
            _catalog = catalog;
            _requirements = requirements;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index(
            Guid? termId, Guid? modalityId, Guid? typeModalityId, string? search,
            int page = 1, int pageSize = 20, string? sortBy = null, string? sortDir = "asc",
            CancellationToken ct = default)
        {
            var query = new ModalityRequisiteListQuery
            {
                TermId = termId,
                ModalityId = modalityId,
                TypeModalityId = typeModalityId,
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
                        Modality = x.ModalityName == null ? null : new { Name = x.ModalityName },
                        TypeModality = x.TypeModalityName == null ? null : new { Name = x.TypeModalityName },
                        Requirement = x.RequirementName == null ? null : new { Name = x.RequirementName }
                    }),
                    recordsTotal = paged.TotalItems,
                    recordsFiltered = paged.TotalItems
                });
            }

            var terms = await _catalog.GetTermsAsync(ct: ct);
            var activeTermId = (await _catalog.GetTermsAsync(onlyActive: true, ct: ct)).FirstOrDefault()?.Id
                               ?? terms.FirstOrDefault()?.Id;

            ViewBag.Terms = terms;
            ViewBag.DefaultTermId = activeTermId;
            ViewBag.Modalities = activeTermId.HasValue
                ? await _catalog.GetModalitiesAsync(activeTermId.Value, ct: ct)
                : Array.Empty<CatalogOption>();

            return View("~/Pages/Admin/ExamManagement/ModalityRequisites/Index.cshtml");
        }

        [HttpGet("crear")]
        public async Task<IActionResult> Create(CancellationToken ct)
        {
            await PopulateCreateLookupsAsync(ct);
            return View("~/Pages/Admin/ExamManagement/ModalityRequisites/Create.cshtml");
        }

        [HttpGet("api/types/{modalityId}")]
        public async Task<IActionResult> GetTypesByModality(Guid modalityId, CancellationToken ct)
        {
            var types = await _catalog.GetTypeModalitiesAsync(modalityId, onlyActive: true, ct);
            return Json(types.Select(t => new { t.Id, t.Name }));
        }

        // Grilla de asignación masiva: modalidades del periodo + tipos, con flag `alreadyAssigned`
        // para los que ya están vinculados al requisito.
        [HttpGet("api/grid")]
        public async Task<IActionResult> GetAssignmentGrid(Guid termId, Guid requirementId, CancellationToken ct)
        {
            var grid = await _assignments.BuildAssignmentGridAsync(termId, requirementId, ct);
            return Json(grid.Select(m => new
            {
                modalityId = m.ModalityId,
                modalityName = m.ModalityName,
                alreadyAssigned = m.AlreadyAssigned,
                types = m.Types.Select(t => new
                {
                    id = t.Id,
                    name = t.Name,
                    alreadyAssigned = t.AlreadyAssigned
                })
            }));
        }

        // Asignación masiva: recibe un requisito + lista de (modalityId, typeModalityId?).
        [HttpPost("bulk")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateBulk([FromForm] Guid requirementId, [FromForm] List<string> selections, CancellationToken ct)
        {
            if (requirementId == Guid.Empty)
            {
                TempData["Error"] = "Debes seleccionar un requisito.";
                return RedirectToAction(nameof(Create));
            }

            // Cada selección viene como "modalityId" o "modalityId:typeModalityId"
            var parsed = new List<BulkAssignmentSelection>();
            foreach (var raw in selections ?? new List<string>())
            {
                if (string.IsNullOrWhiteSpace(raw)) continue;
                var parts = raw.Split(':');
                if (!Guid.TryParse(parts[0], out var modalityId) || modalityId == Guid.Empty) continue;
                Guid? typeId = null;
                if (parts.Length > 1 && Guid.TryParse(parts[1], out var t) && t != Guid.Empty) typeId = t;
                parsed.Add(new BulkAssignmentSelection(modalityId, typeId));
            }

            if (parsed.Count == 0)
            {
                TempData["Error"] = "Selecciona al menos una modalidad o tipo de modalidad.";
                return RedirectToAction(nameof(Create));
            }

            var result = await _assignments.CreateBulkAsync(requirementId, parsed, User.Identity?.Name ?? "Admin", ct);

            if (result.Created > 0 && result.Skipped > 0)
                TempData["Success"] = $"Se crearon {result.Created} asignación(es). {result.Skipped} ya existían y se omitieron.";
            else if (result.Created > 0)
                TempData["Success"] = $"Se crearon {result.Created} asignación(es).";
            else
                TempData["Error"] = "Todas las selecciones ya estaban asignadas.";

            return RedirectToAction(nameof(Index));
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
            return RedirectToAction(nameof(Index), new { modalityId = result.ModalityId, typeModalityId = result.TypeModalityId });
        }

        private async Task PopulateCreateLookupsAsync(CancellationToken ct)
        {
            var terms = await _catalog.GetTermsAsync(ct: ct);
            ViewData["Terms"] = terms ?? new List<CatalogOption>();
            ViewData["DefaultTermId"] = (await _catalog.GetTermsAsync(onlyActive: true, ct: ct)).FirstOrDefault()?.Id
                                         ?? terms?.FirstOrDefault()?.Id;

            var requirementsList = await _requirements.ListAsync(new ADMISION.Models.Shared.ListQuery { PageSize = 500 }, ct);
            ViewData["Requirements"] = requirementsList?.Items ?? new List<FileRequirementListItem>();
        }
    }
}
