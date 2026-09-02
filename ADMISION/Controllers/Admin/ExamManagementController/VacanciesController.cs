using ADMISION.ENTITIES.Constants;
using ADMISION.ENTITIES.Data;
using ADMISION.Models.ViewModels.Admin;
using ADMISION.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace admision.Controllers.Admin.ExamManagementController
{
    [Authorize(Roles = AppConstants.Roles.SuperAdmin + "," + AppConstants.Roles.Admin)]
    [Route("admin/exam-management/vacancies")]
    public class VacanciesController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IVacancyService _vacancies;
        private readonly ICatalogService _catalog;

        public VacanciesController(AppDbContext context, IVacancyService vacancies, ICatalogService catalog)
        {
            _context = context;
            _vacancies = vacancies;
            _catalog = catalog;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index(Guid? termId, Guid? modalityId, CancellationToken ct)
        {
            var terms = await _context.Terms.OrderByDescending(t => t.Year).ThenByDescending(t => t.Number).ToListAsync(ct);
            var activeTermId = termId ?? terms.FirstOrDefault(t => t.IsActive)?.Id ?? terms.FirstOrDefault()?.Id;

            var modalityQuery = _context.Modalities.Where(m => m.IsActive && !m.IsMockExam);
            if (activeTermId.HasValue)
            {
                modalityQuery = modalityQuery.Where(m => m.TermId == activeTermId.Value);
            }
            var modalities = await modalityQuery.OrderByDescending(m => m.CreatedAt).ToListAsync(ct);

            var viewModel = new VacanciesIndexViewModel
            {
                Modalities = modalities,
                SelectedModalityId = modalityId
            };

            ViewBag.Terms = terms;
            ViewBag.DefaultTermId = activeTermId;

            if (modalityId.HasValue)
            {
                viewModel.Matrix = await _vacancies.BuildMatrixAsync(modalityId.Value, ct);
            }
            else if (viewModel.Modalities.Any())
            {
                // Auto-seleccionar la primera modalidad del término actual.
                return RedirectToAction(nameof(Index), new { termId = activeTermId, modalityId = viewModel.Modalities.First().Id });
            }

            return View("~/Pages/Admin/ExamManagement/Vacancies/Index.cshtml", viewModel);
        }

        [HttpGet("gestionar")]
        public async Task<IActionResult> Manage(Guid modalityId, CancellationToken ct)
        {
            var matrix = await _vacancies.BuildMatrixAsync(modalityId, ct);
            if (matrix == null) return NotFound();

            return View("~/Pages/Admin/ExamManagement/Vacancies/Manage.cshtml", matrix);
        }

        [HttpPost("gestionar")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveMatrix(Guid modalityId, Dictionary<string, int> quantities, CancellationToken ct)
        {
            await _vacancies.SaveMatrixAsync(modalityId, quantities, User.Identity?.Name ?? "Admin", ct);
            TempData["Success"] = "Vacantes actualizadas exitosamente.";
            return RedirectToAction(nameof(Index), new { modalityId });
        }
    }
}
