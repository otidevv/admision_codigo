using ADMISION.ENTITIES.Constants;
using ADMISION.ENTITIES.Models.Modality;
using ADMISION.Extensions;
using ADMISION.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace admision.Controllers.Admin.ExamManagementController
{
    [Authorize(Roles = AppConstants.Roles.SuperAdmin + "," + AppConstants.Roles.Admin)]
    [Route("admin/exam-management/results")]
    public class ExamResultsController : Controller
    {
        private readonly IExamResultService _results;
        private readonly ICatalogService _catalog;
        private readonly ILogger<ExamResultsController> _logger;

        public ExamResultsController(IExamResultService results, ICatalogService catalog, ILogger<ExamResultsController> logger)
        {
            _results = results;
            _catalog = catalog;
            _logger = logger;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index(CancellationToken ct)
        {
            await PopulateSelectsAsync(ct);
            var list = await _results.GetAllAsync(ct);
            return View("~/Pages/Admin/ExamManagement/ExamResults/Index.cshtml", list);
        }

        [HttpGet("crear")]
        public async Task<IActionResult> Create(CancellationToken ct)
        {
            await PopulateSelectsAsync(ct);
            return View("~/Pages/Admin/ExamManagement/ExamResults/Create.cshtml", new ExamResult { IsActive = true });
        }

        [HttpPost("crear")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ExamResult result, IFormFile? pdfFile, CancellationToken ct)
        {
            if (pdfFile == null || pdfFile.Length == 0)
                ModelState.AddModelError(nameof(pdfFile), "Debe adjuntar el documento PDF de resultados.");
            if (result.TermId == Guid.Empty)
                ModelState.AddModelError(nameof(result.TermId), "Seleccione un periodo académico.");
            if (result.ModalityId == Guid.Empty)
                ModelState.AddModelError(nameof(result.ModalityId), "Seleccione una modalidad.");

            if (!ModelState.IsValid)
            {
                await PopulateSelectsAsync(ct);
                return View("~/Pages/Admin/ExamManagement/ExamResults/Create.cshtml", result);
            }

            try
            {
                await _results.CreateAsync(result, pdfFile!, User.Identity?.Name ?? "Admin", ct);
                TempData["Success"] = "Documento de resultados publicado exitosamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                this.SetSaveError(ex, _logger);
                await PopulateSelectsAsync(ct);
                return View("~/Pages/Admin/ExamManagement/ExamResults/Create.cshtml", result);
            }
        }

        [HttpGet("editar/{id}")]
        public async Task<IActionResult> Edit(Guid id, CancellationToken ct)
        {
            var result = await _results.GetByIdAsync(id, ct);
            if (result == null) return NotFound();

            await PopulateSelectsAsync(ct);
            return View("~/Pages/Admin/ExamManagement/ExamResults/Edit.cshtml", result);
        }

        [HttpPost("editar/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, ExamResult result, IFormFile? pdfFile, CancellationToken ct)
        {
            if (id != result.Id) return NotFound();

            if (result.TermId == Guid.Empty)
                ModelState.AddModelError(nameof(result.TermId), "Seleccione un periodo académico.");
            if (result.ModalityId == Guid.Empty)
                ModelState.AddModelError(nameof(result.ModalityId), "Seleccione una modalidad.");

            if (!ModelState.IsValid)
            {
                await PopulateSelectsAsync(ct);
                return View("~/Pages/Admin/ExamManagement/ExamResults/Edit.cshtml", result);
            }

            try
            {
                var ok = await _results.UpdateAsync(result, pdfFile, User.Identity?.Name ?? "Admin", ct);
                if (!ok) return NotFound();

                TempData["Success"] = "Documento de resultados actualizado.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                this.SetSaveError(ex, _logger);
                await PopulateSelectsAsync(ct);
                return View("~/Pages/Admin/ExamManagement/ExamResults/Edit.cshtml", result);
            }
        }

        [HttpPost("eliminar/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            var outcome = await _results.DeleteAsync(id, ct);
            switch (outcome)
            {
                case DeleteOutcome.Deleted:
                    TempData["Success"] = "Documento eliminado.";
                    break;
                case DeleteOutcome.HasDependencies:
                    TempData["Error"] = "No se puede eliminar el documento.";
                    break;
                default:
                    TempData["Error"] = "No se encontró el documento.";
                    break;
            }
            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateSelectsAsync(CancellationToken ct)
        {
            var terms = await _catalog.GetTermsAsync(ct: ct);
            var modalities = await _catalog.GetModalitiesAsync(ct: ct);
            ViewBag.Terms = terms.Select(t => new { id = t.Id, name = t.Name }).ToList();
            ViewBag.Modalities = modalities.Select(m => new { id = m.Id, name = m.Name }).ToList();
        }
    }
}
