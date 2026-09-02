using ADMISION.ENTITIES.Data;
using ADMISION.ENTITIES.Models.EconomicManagement;
using ADMISION.ENTITIES.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace admision.Controllers.Admin.EconomicManagementController
{
    [Authorize(Roles = AppConstants.Roles.SuperAdmin + "," + AppConstants.Roles.Admin)]
    [Route("admin/economic-management/methods-payment")]
    public class MethodsPaymentController : Controller
    {
        private readonly AppDbContext _context;

        public MethodsPaymentController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            var methods = await _context.MethodPayments
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();
            return View("~/Pages/Admin/EconomicManagement/MethodsPayment/Index.cshtml", methods);
        }

        [HttpGet("crear")]
        public IActionResult Create()
        {
            return View("~/Pages/Admin/EconomicManagement/MethodsPayment/Edit.cshtml", new MethodPayment());
        }

        [HttpGet("editar/{id}")]
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null) return NotFound();

            var method = await _context.MethodPayments.FindAsync(id);
            if (method == null) return NotFound();
            
            return View("~/Pages/Admin/EconomicManagement/MethodsPayment/Edit.cshtml", method);
        }

        [HttpPost("save")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(MethodPayment method)
        {
            if (ModelState.IsValid)
            {
                bool isNew = method.Id == Guid.Empty;
                
                if (isNew)
                {
                    method.Id = Guid.NewGuid();
                    method.CreatedAt = DateTimeOffset.UtcNow;
                    method.CreatedBy = User.Identity?.Name ?? "Admin";
                    _context.Add(method);
                    TempData["Success"] = "Método de pago creado exitosamente.";
                }
                else
                {
                    var existing = await _context.MethodPayments.AsNoTracking().FirstOrDefaultAsync(m => m.Id == method.Id);
                    if (existing == null) return NotFound();

                    method.CreatedAt = existing.CreatedAt;
                    method.CreatedBy = existing.CreatedBy;
                    method.UpdatedAt = DateTimeOffset.UtcNow;
                    method.UpdatedBy = User.Identity?.Name ?? "Admin";
                    _context.Update(method);
                    TempData["Success"] = "Método de pago actualizado exitosamente.";
                }
                
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View("~/Pages/Admin/EconomicManagement/MethodsPayment/Edit.cshtml", method);
        }

        [HttpPost("eliminar/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            var method = await _context.MethodPayments.FindAsync(id);
            if (method != null)
            {
                try
                {
                    _context.MethodPayments.Remove(method);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Método de pago eliminado exitosamente.";
                }
                catch (DbUpdateException)
                {
                    TempData["Error"] = "No se puede eliminar el método de pago porque tiene registros asociados.";
                }
            }
            else
            {
                TempData["Error"] = "No se encontró el método de pago.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
