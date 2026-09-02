using ADMISION.ENTITIES.Constants;
using ADMISION.ENTITIES.Data;
using ADMISION.ENTITIES.Models.Exam;
using ADMISION.Models.ViewModels.Admin;
using ADMISION.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ADMISION.Controllers.Admin.ConfigController
{
    [Route("admin/config/cepre-import")]
    [Authorize(Roles = AppConstants.Roles.SuperAdmin)]
    public class CepreTurnosController : Controller
    {
        private readonly IExamResultImportService _importService;

        public CepreTurnosController(IExamResultImportService importService)
        {
            _importService = importService;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index(Guid? termId, CancellationToken ct)
        {
            var model = new CepreTurnosViewModel
            {
                SelectedTermId = termId
            };

            await LoadDataAsync(model, ct);

            return View("~/Pages/Admin/Config/CepreImport/Turnos.cshtml", model);
        }

        [HttpPost("create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CepreTurnosViewModel form, CancellationToken ct)
        {
            var model = new CepreTurnosViewModel
            {
                SelectedTermId = form.SelectedTermId
            };

            await LoadDataAsync(model, ct);

            if (!form.SelectedTermId.HasValue || !form.SelectedUserId.HasValue)
            {
                TempData["Error"] = "Seleccione un período y un usuario.";
                return View("~/Pages/Admin/Config/CepreImport/Turnos.cshtml", model);
            }

            if (form.TurnStartDate == null || form.TurnEndDate == null)
            {
                TempData["Error"] = "Ingrese las fechas de inicio y fin del turno.";
                return View("~/Pages/Admin/Config/CepreImport/Turnos.cshtml", model);
            }

            if (form.TurnStartDate >= form.TurnEndDate)
            {
                TempData["Error"] = "La fecha de inicio debe ser anterior a la fecha de fin.";
                return View("~/Pages/Admin/Config/CepreImport/Turnos.cshtml", model);
            }

            var turn = new CepreTurn
            {
                Id = Guid.NewGuid(),
                TermId = form.SelectedTermId.Value,
                UserId = form.SelectedUserId.Value,
                StartDate = form.TurnStartDate.Value.ToUniversalTime(),
                EndDate = form.TurnEndDate.Value.ToUniversalTime(),
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedBy = User.Identity?.Name ?? "Admin"
            };

            var success = await _importService.CreateTurnAsync(turn, ct);
            if (!success)
            {
                TempData["Error"] = "Ya existe un turno para este usuario en el período seleccionado.";
                return View("~/Pages/Admin/Config/CepreImport/Turnos.cshtml", model);
            }

            TempData["Success"] = "Turno creado exitosamente.";
            return RedirectToAction(nameof(Index), new { termId = form.SelectedTermId });
        }

        [HttpPost("delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid turnId, Guid? SelectedTermId, CancellationToken ct)
        {
            var success = await _importService.DeleteTurnAsync(turnId, ct);
            if (success)
                TempData["Success"] = "Turno eliminado exitosamente.";
            else
                TempData["Error"] = "No se pudo eliminar el turno.";

            return RedirectToAction(nameof(Index), new { termId = SelectedTermId });
        }

        private async Task LoadDataAsync(CepreTurnosViewModel model, CancellationToken ct)
        {
            using var scope = HttpContext.RequestServices.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            model.Terms = await context.Terms
                .AsNoTracking()
                .OrderByDescending(t => t.Year).ThenByDescending(t => t.Number)
                .ToListAsync(ct);

            model.SupportUsers = await context.Users
                .AsNoTracking()
                .Where(u => u.UserRols.Any(ur => ur.Rol.Name == AppConstants.Roles.Soporte))
                .OrderBy(u => u.FullName)
                .ToListAsync(ct);

            if (model.SelectedTermId.HasValue)
            {
                model.Turns = await _importService.GetTurnsByTermAsync(model.SelectedTermId.Value, ct);
            }
        }
    }
}
