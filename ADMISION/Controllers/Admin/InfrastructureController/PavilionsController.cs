using ADMISION.ENTITIES.Constants;
using ADMISION.ENTITIES.Data;
using ADMISION.ENTITIES.Models.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace admision.Controllers.Admin.InfrastructureController
{
    [Authorize(Roles = AppConstants.Roles.SuperAdmin + "," + AppConstants.Roles.Admin)]
    [Route("admin/infrastructure/pavilions")]
    public class PavilionsController : Controller
    {
        private readonly AppDbContext _context;

        public PavilionsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            var pavilions = await _context.Pavilions
                .Include(p => p.Classrooms)
                .OrderBy(p => p.Code).ThenBy(p => p.Name)
                .ToListAsync();
            return View("~/Pages/Admin/Infrastructure/Pavilions/Index.cshtml", pavilions);
        }

        [HttpGet("crear")]
        public IActionResult Create()
        {
            return View("~/Pages/Admin/Infrastructure/Pavilions/Create.cshtml", new Pavilion());
        }

        [HttpPost("crear")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Pavilion pavilion)
        {
            if (!ModelState.IsValid)
                return View("~/Pages/Admin/Infrastructure/Pavilions/Create.cshtml", pavilion);

            pavilion.Id = Guid.NewGuid();
            pavilion.CreatedAt = DateTimeOffset.UtcNow;
            pavilion.CreatedBy = User.Identity?.Name ?? "Admin";
            _context.Pavilions.Add(pavilion);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Pabellón registrado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet("editar/{id}")]
        public async Task<IActionResult> Edit(Guid id)
        {
            var pavilion = await _context.Pavilions.FindAsync(id);
            if (pavilion == null) return NotFound();
            return View("~/Pages/Admin/Infrastructure/Pavilions/Edit.cshtml", pavilion);
        }

        [HttpPost("editar/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, Pavilion pavilion)
        {
            if (id != pavilion.Id) return NotFound();
            if (!ModelState.IsValid)
                return View("~/Pages/Admin/Infrastructure/Pavilions/Edit.cshtml", pavilion);

            var existing = await _context.Pavilions.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
            if (existing == null) return NotFound();

            pavilion.CreatedAt = existing.CreatedAt;
            pavilion.CreatedBy = existing.CreatedBy;
            pavilion.UpdatedAt = DateTimeOffset.UtcNow;
            pavilion.UpdatedBy = User.Identity?.Name ?? "Admin";
            _context.Update(pavilion);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Pabellón actualizado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost("eliminar/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            var pavilion = await _context.Pavilions.Include(p => p.Classrooms).FirstOrDefaultAsync(p => p.Id == id);
            if (pavilion == null) { TempData["Error"] = "Pabellón no encontrado."; return RedirectToAction(nameof(Index)); }

            if (pavilion.Classrooms.Any())
            {
                TempData["Error"] = "No se puede eliminar: el pabellón tiene salones registrados.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                _context.Pavilions.Remove(pavilion);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Pabellón eliminado.";
            }
            catch (DbUpdateException)
            {
                TempData["Error"] = "No se pudo eliminar por dependencias existentes.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
