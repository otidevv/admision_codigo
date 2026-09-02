using ADMISION.ENTITIES.Constants;
using ADMISION.Models.ViewModels.Admin;
using ADMISION.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Encodings.Web;
using System.Threading;

namespace admision.Controllers.Admin.UserController
{
    [Route("admin/usuarios")]
    [Authorize(Roles = AppConstants.Roles.SuperAdmin)]
    public class UsersController : Controller
    {
        private readonly IUserManagementService _users;
        private readonly IEmailService _email;
        private readonly IConfigService _config;

        public UsersController(IUserManagementService users, IEmailService email, IConfigService config)
        {
            _users = users;
            _email = email;
            _config = config;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index(CancellationToken ct)
        {
            var users = await _users.ListAdminUsersAsync(ct);
            return View("~/Pages/Admin/Users/Index.cshtml", users);
        }

        [HttpPost("toggle-block/{id}")]
        public async Task<IActionResult> ToggleBlock(Guid id, string reason, CancellationToken ct)
        {
            var ok = await _users.ToggleBlockAsync(id, reason, User.Identity?.Name ?? "System", ct);
            return ok ? Ok() : NotFound();
        }

        [HttpPost("assign-role/{userId}")]
        public async Task<IActionResult> AssignRole(Guid userId, Guid roleId, CancellationToken ct)
        {
            await _users.AssignRoleAsync(userId, roleId, ct);
            return Ok();
        }

        [HttpPost("remove-role/{userId}")]
        public async Task<IActionResult> RemoveRole(Guid userId, Guid roleId, CancellationToken ct)
        {
            await _users.RemoveRoleAsync(userId, roleId, ct);
            return Ok();
        }

        [HttpGet("get-user/{id}")]
        public async Task<IActionResult> GetUser(Guid id, CancellationToken ct)
        {
            var model = await _users.GetForEditAsync(id, ct);
            if (model == null) return NotFound();
            return Json(model);
        }

        [HttpGet("check-username")]
        public async Task<IActionResult> CheckUsername([FromQuery] string username, CancellationToken ct)
        {
            var taken = await _users.IsUserNameTakenAsync(username, ct);
            return Json(new { taken });
        }

        [HttpGet("lookup-by-document/{document}")]
        public async Task<IActionResult> LookupByDocument(string document, CancellationToken ct)
        {
            var model = await _users.LookupByDocumentAsync(document, ct);
            if (model == null) return NotFound();
            return Json(model);
        }

        [HttpPost("save")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save([FromBody] UserFormViewModel model, CancellationToken ct)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                return BadRequest(new { errors });
            }

            var result = await _users.SaveAsync(model, User.Identity?.Name ?? "Admin", ct);
            if (result.NotFound) return NotFound();
            if (!result.Succeeded)
            {
                return BadRequest(new { errors = result.Errors.Select(e => e.Message) });
            }

            return Ok();
        }

        [HttpPost("delete/{id}")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            var outcome = await _users.DeleteAsync(id, User.Identity?.Name ?? string.Empty, ct);
            return outcome switch
            {
                UserDeleteOutcome.Deleted => Ok(),
                UserDeleteOutcome.NotFound => NotFound(),
                UserDeleteOutcome.SelfDeletion => BadRequest("No puedes eliminar tu propia cuenta."),
                UserDeleteOutcome.HasDependencies => BadRequest("No se puede eliminar el usuario porque tiene registros asociados."),
                _ => StatusCode(500)
            };
        }

        [HttpGet("all-roles")]
        public async Task<IActionResult> GetAllRoles(CancellationToken ct)
        {
            var roles = await _users.GetActiveRolesAsync(ct);
            return Json(roles.Select(r => new { r.Id, r.Name }));
        }

        // ───────── Restablecimiento de credenciales por correo ─────────
        [HttpGet("password-reset-candidates")]
        public async Task<IActionResult> GetPasswordResetCandidates(CancellationToken ct)
        {
            var candidates = await _users.ListPasswordResetCandidatesAsync(ct);
            return Json(candidates.Select(c => new { c.UserId, c.UserName, c.FullName, c.Email }));
        }

        [HttpPost("reset-password/{id:guid}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(Guid id, CancellationToken ct)
        {
            var actor = User.Identity?.Name ?? "System";
            var result = await _users.ResetPasswordAsync(id, actor, ct);
            if (!result.Success)
            {
                return BadRequest(new { error = result.Error });
            }

            var emailSent = false;
            string? emailError = null;
            if (!string.IsNullOrWhiteSpace(result.Email) && !string.IsNullOrWhiteSpace(result.TempPassword))
            {
                var emailResult = await _email.SendEmailAsync(
                    result.Email,
                    "Credenciales restablecidas",
                    await BuildResetEmailAsync(result.FullName, result.UserName!, result.TempPassword!, ct),
                    ct);
                emailSent = emailResult.Success;
                emailError = emailResult.Error;
            }

            return Ok(new { result.UserName, result.TempPassword, emailSent, emailError });
        }

        [HttpPost("reset-password")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPasswordBulk([FromBody] BulkResetRequest request, CancellationToken ct)
        {
            var ids = request?.UserIds?.Where(id => id != Guid.Empty).Distinct().ToList() ?? new List<Guid>();
            if (ids.Count == 0)
            {
                return BadRequest(new { error = "No se seleccionaron usuarios." });
            }

            var actor = User.Identity?.Name ?? "System";
            var sent = 0;
            var failed = new List<string>();

            foreach (var id in ids)
            {
                var result = await _users.ResetPasswordAsync(id, actor, ct);
                if (!result.Success)
                {
                    failed.Add($"{result.UserName ?? id.ToString()}: {result.Error}");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(result.Email) || string.IsNullOrWhiteSpace(result.TempPassword))
                {
                    failed.Add($"{result.UserName}: sin correo registrado.");
                    continue;
                }

                var emailResult = await _email.SendEmailAsync(
                    result.Email,
                    "Credenciales restablecidas",
                    await BuildResetEmailAsync(result.FullName, result.UserName!, result.TempPassword!, ct),
                    ct);

                if (emailResult.Success)
                {
                    sent++;
                }
                else
                {
                    failed.Add($"{result.UserName}: {emailResult.Error}");
                }
            }

            return Ok(new { sent, failed });
        }

        private async Task<string> BuildResetEmailAsync(string? fullName, string userName, string tempPassword, CancellationToken ct)
        {
            var institution = await _config.GetConfigValueAsync(ConfigGeneral.NombreInstitucion);
            Func<string, string> escape = HtmlEncoder.Default.Encode;
            return $@"
                <div style=""font-family:Segoe UI,Roboto,Arial,sans-serif;max-width:600px;margin:0 auto;padding:24px"">
                    <h2 style=""color:#1d4ed8;margin:0 0 16px"">{escape(institution)}</h2>
                    <p>Hola {escape(fullName ?? string.Empty)}:</p>
                    <p>Tu contraseña de acceso al sistema ha sido restablecida por el administrador. Usa las siguientes credenciales para ingresar:</p>
                    <table style=""border-collapse:collapse;margin:16px 0"">
                        <tr>
                            <td style=""padding:8px 12px;background:#f3f4f6;border:1px solid #e5e7eb""><strong>Usuario</strong></td>
                            <td style=""padding:8px 12px;border:1px solid #e5e7eb"">{escape(userName)}</td>
                        </tr>
                        <tr>
                            <td style=""padding:8px 12px;background:#f3f4f6;border:1px solid #e5e7eb""><strong>Contraseña temporal</strong></td>
                            <td style=""padding:8px 12px;border:1px solid #e5e7eb;font-family:Consolas,monospace"">{escape(tempPassword)}</td>
                        </tr>
                    </table>
                    <p style=""color:#4b5563;font-size:13px"">Por seguridad, cambia tu contraseña en tu próxima sesión.</p>
                </div>";
        }

        // ───────── Perfil de usuario (accesos + notificaciones + estadísticas) ─────────
        [HttpGet("perfil/{id:guid}")]
        public async Task<IActionResult> Profile(Guid id, int? year = null, int? month = null, CancellationToken ct = default)
        {
            var vm = await _users.GetProfileAsync(id, year, month, ct);
            if (vm == null) return NotFound();
            return View("~/Pages/Admin/Users/Profile.cshtml", vm);
        }
    }

    public record BulkResetRequest(List<Guid> UserIds);
}
