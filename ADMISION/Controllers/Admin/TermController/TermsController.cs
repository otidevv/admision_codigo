using ADMISION.ENTITIES.Constants;
using ADMISION.ENTITIES.Models.Modality;
using ADMISION.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace admision.Controllers.Admin.TermController
{
    [Authorize(Roles = AppConstants.Roles.SuperAdmin)]
    [Route("admin/periodos")]
    public class TermsController : Controller
    {
        private readonly ITermService _terms;

        public TermsController(ITermService terms)
        {
            _terms = terms;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index(CancellationToken ct)
        {
            var terms = await _terms.GetAllAsync(ct);
            var activeTerm = await _terms.GetActiveWithModalitiesAsync(ct);
            ViewBag.ActiveTerm = activeTerm;

            // Checklist de configuración del periodo activo (si existe).
            if (activeTerm != null)
            {
                ViewBag.ConfigChecklist = await _terms.GetConfigChecklistAsync(activeTerm.Id, ct);
            }

            return View("~/Pages/Admin/Terms/Index.cshtml", terms);
        }

        [HttpGet("crear")]
        public IActionResult Create() => View("~/Pages/Admin/Terms/Create.cshtml");

        [HttpPost("crear")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Term term, [FromForm(Name = "Replication")] TermReplicationOptions? replication, CancellationToken ct = default)
        {
            if (!ModelState.IsValid)
                return View("~/Pages/Admin/Terms/Create.cshtml", term);

            var options = replication ?? TermReplicationOptions.None();
            await _terms.CreateAsync(term, options, User.Identity?.Name ?? "Admin", ct);
            TempData["Success"] = options.Enabled
                ? "Periodo creado y configuración replicada exitosamente."
                : "Periodo creado exitosamente.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet("editar/{id}")]
        public async Task<IActionResult> Edit(Guid id, CancellationToken ct)
        {
            var term = await _terms.GetByIdAsync(id, ct);
            if (term == null) return NotFound();
            return View("~/Pages/Admin/Terms/Edit.cshtml", term);
        }

        [HttpPost("editar/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, Term term, CancellationToken ct)
        {
            if (id != term.Id) return NotFound();
            if (!ModelState.IsValid)
                return View("~/Pages/Admin/Terms/Edit.cshtml", term);

            var ok = await _terms.UpdateAsync(term, User.Identity?.Name ?? "Admin", ct);
            if (!ok) return NotFound();

            TempData["Success"] = "Periodo actualizado exitosamente.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost("eliminar/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            var outcome = await _terms.DeleteAsync(id, ct);
            switch (outcome)
            {
                case DeleteOutcome.Deleted:
                    TempData["Success"] = "Periodo eliminado exitosamente.";
                    break;
                case DeleteOutcome.HasDependencies:
                    TempData["Error"] = "No se puede eliminar el periodo porque tiene registros asociados (modalidades, postulantes, etc.).";
                    break;
                default:
                    TempData["Error"] = "No se encontró el periodo.";
                    break;
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
