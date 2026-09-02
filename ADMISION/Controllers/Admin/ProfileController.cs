using ADMISION.ENTITIES.Constants;
using ADMISION.Models.ViewModels.Admin;
using ADMISION.Services.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ADMISION.Controllers.Admin
{
    [Authorize(Roles = AppConstants.Roles.SuperAdmin + "," + AppConstants.Roles.Admin + "," + AppConstants.Roles.Soporte + "," + AppConstants.Roles.Consultor + "," + AppConstants.Roles.ApiConsumer)]
    [Route("admin/profile")]
    public class ProfileController : Controller
    {
        private readonly IProfileService _profile;

        public ProfileController(IProfileService profile)
        {
            _profile = profile;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index(CancellationToken ct)
        {
            var id = GetCurrentUserId();
            if (id == null) return RedirectToAction("Index", "Login");

            var vm = await _profile.GetProfileAsync(id.Value, ct);
            if (vm == null) return RedirectToAction("Index", "Login");
            return View("~/Pages/Admin/Profile/Index.cshtml", vm);
        }

        [HttpPost("")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(ProfileViewModel input, CancellationToken ct)
        {
            var id = GetCurrentUserId();
            if (id == null) return RedirectToAction("Index", "Login");

            if (!ModelState.IsValid)
                return await ReloadWithInputsAsync(id.Value, input, ct);

            var result = await _profile.UpdateAsync(id.Value, input, User.Identity?.Name ?? "Admin", ct);
            if (result.NotFound) return RedirectToAction("Index", "Login");

            if (!result.Succeeded)
            {
                foreach (var err in result.Errors) ModelState.AddModelError(err.Field, err.Message);
                return await ReloadWithInputsAsync(id.Value, input, ct);
            }

            TempData["Success"] = "Datos actualizados correctamente.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost("password")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel input, CancellationToken ct)
        {
            var id = GetCurrentUserId();
            if (id == null) return RedirectToAction("Index", "Login");

            if (!ModelState.IsValid)
            {
                TempData["Error"] = string.Join(" · ",
                    ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return RedirectToAction(nameof(Index));
            }

            var outcome = await _profile.ChangePasswordAsync(id.Value, input, User.Identity?.Name ?? "Admin", ct);
            switch (outcome)
            {
                case ChangePasswordOutcome.Success:
                    await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    return Redirect("/login?pwd=1");
                case ChangePasswordOutcome.WrongCurrentPassword:
                    TempData["Error"] = "La contraseña actual es incorrecta.";
                    break;
                case ChangePasswordOutcome.SameAsCurrent:
                    TempData["Error"] = "La nueva contraseña no puede ser igual a la actual.";
                    break;
                case ChangePasswordOutcome.UserNotFound:
                    return RedirectToAction("Index", "Login");
            }

            return RedirectToAction(nameof(Index));
        }

        // ============ Helpers ============
        private Guid? GetCurrentUserId()
        {
            var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(raw, out var id) ? id : null;
        }

        // Recarga el VM y conserva los inputs de email/teléfono que el usuario tipeó.
        private async Task<IActionResult> ReloadWithInputsAsync(Guid userId, ProfileViewModel input, CancellationToken ct)
        {
            var reloaded = await _profile.GetProfileAsync(userId, ct);
            if (reloaded == null) return RedirectToAction("Index", "Login");

            reloaded.Email = input.Email;
            reloaded.PhoneNumber = input.PhoneNumber;
            return View("~/Pages/Admin/Profile/Index.cshtml", reloaded);
        }
    }
}
