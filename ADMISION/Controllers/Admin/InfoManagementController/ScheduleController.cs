using ADMISION.ENTITIES.Constants;
using ADMISION.ENTITIES.Models.Modality;
using ADMISION.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace admision.Controllers.Admin.InfoManagementController
{
    [Authorize(Roles = AppConstants.Roles.SuperAdmin + "," + AppConstants.Roles.Admin)]
    [Route("admin/info")]
    public class ScheduleController : Controller
    {
        private readonly IScheduleEventService _events;
        private readonly ITermService _terms;

        public ScheduleController(IScheduleEventService events, ITermService terms)
        {
            _events = events;
            _terms = terms;
        }
        [HttpGet("")]
        public IActionResult Index()
        {
            return Redirect("/admin");
        }

        [HttpGet("cronograma")]
        public async Task<IActionResult> Index(Guid? termId, CancellationToken ct)
        {
            var terms = await _terms.GetAllAsync(ct);
            var selected = termId.HasValue
                ? terms.FirstOrDefault(t => t.Id == termId.Value)
                : terms.FirstOrDefault(t => t.IsActive) ?? terms.FirstOrDefault();

            var events = selected != null
                ? await _events.GetByTermAsync(selected.Id, ct)
                : Array.Empty<ScheduleEvent>();

            ViewBag.Terms = terms;
            ViewBag.SelectedTerm = selected;
            ViewBag.PhaseLabels = AppConstants.SchedulePhase.Labels;
            ViewBag.PhaseOrder = AppConstants.SchedulePhase.Order;
            return View("~/Pages/Admin/InfoManagement/Schedule/Index.cshtml", events);
        }

        [HttpGet("cronograma/crear")]
        public async Task<IActionResult> Create(Guid? termId, CancellationToken ct)
        {
            await PopulateFormDataAsync(termId, ct);
            var model = new ScheduleEvent { TermId = termId ?? Guid.Empty, IsActive = true };
            return View("~/Pages/Admin/InfoManagement/Schedule/Create.cshtml", model);
        }

        [HttpPost("cronograma/crear")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ScheduleEvent model, CancellationToken ct)
        {
            if (!ModelState.IsValid)
            {
                await PopulateFormDataAsync(model.TermId, ct);
                return View("~/Pages/Admin/InfoManagement/Schedule/Create.cshtml", model);
            }

            await _events.CreateAsync(model, User.Identity?.Name ?? "Admin", ct);
            TempData["Success"] = "Evento de cronograma creado.";
            return RedirectToAction(nameof(Index), new { termId = model.TermId });
        }

        [HttpGet("cronograma/editar/{id}")]
        public async Task<IActionResult> Edit(Guid id, CancellationToken ct)
        {
            var ev = await _events.GetByIdAsync(id, ct);
            if (ev == null) return NotFound();

            await PopulateFormDataAsync(ev.TermId, ct);
            return View("~/Pages/Admin/InfoManagement/Schedule/Edit.cshtml", ev);
        }

        [HttpPost("cronograma/editar/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, ScheduleEvent model, CancellationToken ct)
        {
            if (id != model.Id) return NotFound();

            if (!ModelState.IsValid)
            {
                await PopulateFormDataAsync(model.TermId, ct);
                return View("~/Pages/Admin/InfoManagement/Schedule/Edit.cshtml", model);
            }

            var ok = await _events.UpdateAsync(model, User.Identity?.Name ?? "Admin", ct);
            if (!ok) return NotFound();

            TempData["Success"] = "Evento de cronograma actualizado.";
            return RedirectToAction(nameof(Index), new { termId = model.TermId });
        }

        [HttpPost("cronograma/eliminar/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            var ev = await _events.GetByIdAsync(id, ct);
            if (ev == null)
            {
                TempData["Error"] = "Evento no encontrado.";
                return RedirectToAction(nameof(Index));
            }

            var termId = ev.TermId;
            var outcome = await _events.DeleteAsync(id, ct);
            if (outcome == DeleteOutcome.Deleted) TempData["Success"] = "Evento eliminado.";
            else TempData["Error"] = "No se pudo eliminar el evento.";

            return RedirectToAction(nameof(Index), new { termId });
        }

        private async Task PopulateFormDataAsync(Guid? selectedTermId, CancellationToken ct)
        {
            var terms = await _terms.GetAllAsync(ct);
            ViewData["TermId"] = new SelectList(terms, "Id", "Name", selectedTermId);
            ViewBag.PhaseOptions = AppConstants.SchedulePhase.GetOptions();
        }
    }
}
