using ADMISION.ENTITIES.Constants;
using ADMISION.ENTITIES.Models.Exam;
using ADMISION.Models.Shared;
using ADMISION.Models.ViewModels.Admin;
using ADMISION.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace admision.Controllers.Admin.ExamManagementController
{
    [Authorize(Roles = AppConstants.Roles.SuperAdmin + "," + AppConstants.Roles.Admin)]
    [Route("admin/exam-management/scoring-profiles")]
    public class ScoringProfilesController : Controller
    {
        private readonly IScoringProfileService _profiles;
        private readonly ICatalogService _catalog;

        public ScoringProfilesController(IScoringProfileService profiles, ICatalogService catalog)
        {
            _profiles = profiles;
            _catalog = catalog;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index(
            Guid? termId, Guid? modalityId, Guid? typeModalityId, bool? isWeighted, bool? isActive,
            string? search, int page = 1, int pageSize = 20, string? sortBy = null, string? sortDir = "asc",
            CancellationToken ct = default)
        {
            var query = new ScoringProfileListQuery
            {
                TermId = termId,
                ModalityId = modalityId,
                TypeModalityId = typeModalityId,
                IsWeighted = isWeighted,
                IsActive = isActive,
                Search = search,
                Page = page,
                PageSize = pageSize,
                SortBy = sortBy,
                SortDir = sortDir
            };

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                var paged = await _profiles.ListAsync(query, ct);
                return Json(new
                {
                    data = paged.Items.Select(x => new
                    {
                        x.Id,
                        Name = x.Name,
                        Modo = x.IsWeighted ? "Ponderado" : "Simple",
                        PuntosCorrecta = x.PuntosCorrecta,
                        Term = x.TermName == null ? null : new { Name = x.TermName },
                        Modality = x.ModalityName == null ? null : new { Name = x.ModalityName },
                        RangeCount = x.RangeCount,
                        IsActive = x.IsActive
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

            return View("~/Pages/Admin/ExamManagement/ScoringProfiles/Index.cshtml");
        }

        [HttpGet("crear")]
        public async Task<IActionResult> Create(CancellationToken ct)
        {
            await PopulateLookupsAsync(ct: ct);
            return View("~/Pages/Admin/ExamManagement/ScoringProfiles/Create.cshtml", new ScoringProfileFormModel { IsActive = true });
        }

        [HttpPost("crear")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ScoringProfileFormModel model, CancellationToken ct)
        {
            var profile = Map(model);
            var ranges = model.Ranges
                .Where(r => r.FromQuestion >= 1 || r.ToQuestion >= 1 || r.PuntosCorrecta > 0)
                .Select(r => new ScoringProfileRange
                {
                    FromQuestion = r.FromQuestion,
                    ToQuestion = r.ToQuestion,
                    PuntosCorrecta = r.PuntosCorrecta
                })
                .ToList();

            var result = await _profiles.CreateAsync(profile, ranges, User.Identity?.Name ?? "Admin", ct);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError(error.Field, error.Message);

                await PopulateLookupsAsync(model, ct: ct);
                return View("~/Pages/Admin/ExamManagement/ScoringProfiles/Create.cshtml", model);
            }

            TempData["Success"] = "Perfil de calificación creado exitosamente.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet("editar/{id}")]
        public async Task<IActionResult> Edit(Guid id, CancellationToken ct)
        {
            var detail = await _profiles.GetByIdAsync(id, ct);
            if (detail == null) return NotFound();

            var model = new ScoringProfileFormModel
            {
                Id = detail.Id,
                Name = detail.Name,
                Description = detail.Description,
                IsWeighted = detail.IsWeighted,
                PuntosCorrecta = detail.PuntosCorrecta,
                PuntosBlanco = detail.PuntosBlanco,
                PuntosIncorrecta = detail.PuntosIncorrecta,
                NotaMinimaIngreso = detail.NotaMinimaIngreso,
                AplicarVigesimal = detail.AplicarVigesimal,
                ManejoAnuladas = detail.ManejoAnuladas,
                TermId = detail.TermId,
                ModalityId = detail.ModalityId,
                TypeModalityId = detail.TypeModalityId,
                CareerId = detail.CareerId,
                IsActive = detail.IsActive,
                Ranges = detail.Ranges
                    .Select(r => new ScoringProfileRangeFormModel
                    {
                        FromQuestion = r.FromQuestion,
                        ToQuestion = r.ToQuestion,
                        PuntosCorrecta = r.PuntosCorrecta
                    })
                    .ToList()
            };

            await PopulateLookupsAsync(model, ct: ct);
            return View("~/Pages/Admin/ExamManagement/ScoringProfiles/Edit.cshtml", model);
        }

        [HttpPost("editar/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, ScoringProfileFormModel model, CancellationToken ct)
        {
            if (id != model.Id) return NotFound();

            var profile = Map(model);
            var ranges = model.Ranges
                .Where(r => r.FromQuestion >= 1 || r.ToQuestion >= 1 || r.PuntosCorrecta > 0)
                .Select(r => new ScoringProfileRange
                {
                    FromQuestion = r.FromQuestion,
                    ToQuestion = r.ToQuestion,
                    PuntosCorrecta = r.PuntosCorrecta
                })
                .ToList();

            var result = await _profiles.UpdateAsync(profile, ranges, User.Identity?.Name ?? "Admin", ct);

            if (result.NotFound) return NotFound();

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError(error.Field, error.Message);

                await PopulateLookupsAsync(model, ct: ct);
                return View("~/Pages/Admin/ExamManagement/ScoringProfiles/Edit.cshtml", model);
            }

            TempData["Success"] = "Perfil de calificación actualizado exitosamente.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost("eliminar/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            var outcome = await _profiles.DeleteAsync(id, ct);
            switch (outcome)
            {
                case DeleteOutcome.Deleted:
                    TempData["Success"] = "Perfil de calificación eliminado exitosamente.";
                    break;
                case DeleteOutcome.HasDependencies:
                    TempData["Error"] = "No se puede eliminar el perfil porque tiene registros asociados.";
                    break;
                default:
                    TempData["Error"] = "No se encontró el perfil de calificación.";
                    break;
            }
            return RedirectToAction(nameof(Index));
        }

        // Tipos de modalidad en cascada (para el formulario de perfil).
        [HttpGet("api/types/{modalityId}")]
        public async Task<IActionResult> GetTypesByModality(Guid modalityId, CancellationToken ct)
        {
            var types = await _catalog.GetTypeModalitiesAsync(modalityId, onlyActive: true, ct);
            return Json(types.Select(t => new { t.Id, t.Name }));
        }

        private static ScoringProfile Map(ScoringProfileFormModel model)
        {
            return new ScoringProfile
            {
                Id = model.Id,
                Name = model.Name.Trim(),
                Description = model.Description,
                IsWeighted = model.IsWeighted,
                PuntosCorrecta = model.PuntosCorrecta,
                PuntosBlanco = model.PuntosBlanco,
                PuntosIncorrecta = model.PuntosIncorrecta,
                NotaMinimaIngreso = model.NotaMinimaIngreso,
                AplicarVigesimal = model.AplicarVigesimal,
                ManejoAnuladas = string.IsNullOrWhiteSpace(model.ManejoAnuladas) ? "Ignorar" : model.ManejoAnuladas,
                TermId = model.TermId,
                ModalityId = model.ModalityId,
                TypeModalityId = model.TypeModalityId,
                CareerId = model.CareerId,
                IsActive = model.IsActive
            };
        }

        private async Task PopulateLookupsAsync(ScoringProfileFormModel? model = null, CancellationToken ct = default)
        {
            var terms = await _catalog.GetTermsAsync(ct: ct);
            ViewData["Terms"] = terms ?? new List<CatalogOption>();
            ViewData["Careers"] = await _catalog.GetCareersAsync(ct: ct);
            ViewData["DefaultTermId"] = model?.TermId.HasValue == true
                ? model.TermId!.Value.ToString()
                : (await _catalog.GetTermsAsync(onlyActive: true, ct: ct)).FirstOrDefault()?.Id.ToString()
                  ?? terms?.FirstOrDefault()?.Id.ToString();

            // Modalidades del periodo seleccionado (para preseleccionar en edición).
            ViewData["Modalities"] = model?.TermId.HasValue == true
                ? await _catalog.GetModalitiesAsync(model.TermId!.Value, ct: ct)
                : Array.Empty<CatalogOption>();

            // Tipos de la modalidad seleccionada (para preseleccionar en edición).
            ViewData["TypeModalities"] = model?.ModalityId.HasValue == true
                ? await _catalog.GetTypeModalitiesAsync(model.ModalityId!.Value, onlyActive: true, ct)
                : Array.Empty<TypeModalityOption>();
        }
    }
}
