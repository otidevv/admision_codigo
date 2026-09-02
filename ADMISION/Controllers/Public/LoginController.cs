using ADMISION.ENTITIES.Constants;
using ADMISION.ENTITIES.Data;
using ADMISION.ENTITIES.Models.System;
using ADMISION.Models.ViewModels.Admin;
using ADMISION.Services.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Encodings.Web;

namespace ADMISION.Controllers.Public
{
    [Route("login")]
    public class LoginController : Controller
    {
        private const string InvalidCredentialsMessage = "Credenciales inválidas.";

        private const string SessionKeyForgotUser = "ForgotPwd_UserName";
        private const string SessionKeyForgotCode = "ForgotPwd_Code";
        private const string SessionKeyForgotExpires = "ForgotPwd_Expires";
        private static readonly TimeSpan ForgotCodeLifetime = TimeSpan.FromMinutes(10);

        private readonly AppDbContext _context;
        private readonly ADMISION.Services.Interfaces.IPasswordHasher _passwordHasher;
        private readonly ADMISION.Services.Interfaces.ICaptchaService _captcha;
        private readonly IEmailService _email;
        private readonly ILogger<LoginController> _logger;

        public LoginController(
            AppDbContext context,
            ADMISION.Services.Interfaces.IPasswordHasher passwordHasher,
            ADMISION.Services.Interfaces.ICaptchaService captcha,
            IEmailService email,
            ILogger<LoginController> logger)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _captcha = captcha;
            _email = email;
            _logger = logger;
        }

        [HttpGet("")]
        public IActionResult Index()
        {
            if (User.Identity!.IsAuthenticated)
            {
                return RedirectToAction("Index", "Admin");
            }
            ViewBag.CaptchaEnabled = _captcha.IsEnabled;
            ViewBag.CaptchaSiteKey = _captcha.SiteKey;
            ViewBag.CaptchaProvider = _captcha.Provider;
            return View("~/Pages/Public/Login.cshtml");
        }

        [HttpPost("")]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("login")]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            ViewBag.CaptchaEnabled = _captcha.IsEnabled;
            ViewBag.CaptchaSiteKey = _captcha.SiteKey;
            ViewBag.CaptchaProvider = _captcha.Provider;

            if (!ModelState.IsValid)
            {
                return View("~/Pages/Public/Login.cshtml", model);
            }

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            // Captcha — verifica antes de tocar la BD para no gastar lookups en bots.
            if (_captcha.IsEnabled)
            {
                var captchaToken = Request.Form["cf-turnstile-response"].FirstOrDefault()
                    ?? Request.Form["g-recaptcha-response"].FirstOrDefault();
                var captchaResult = await _captcha.VerifyAsync(captchaToken, ipAddress, HttpContext.RequestAborted);
                if (!captchaResult.Success)
                {
                    await LogAccessAttemptAsync(null, model.UserName, "Failure", $"Captcha failed: {captchaResult.ErrorCode}", ipAddress, 400);
                    ModelState.AddModelError(string.Empty, "Verificación anti-bot fallida. Vuelva a intentarlo.");
                    return View("~/Pages/Public/Login.cshtml", model);
                }
            }

            var user = await _context.Users
                .Include(u => u.UserRols!)
                .ThenInclude(ur => ur.Rol)
                .FirstOrDefaultAsync(u => u.UserName == model.UserName);

            if (user == null
                || string.IsNullOrEmpty(user.Password)
                || !_passwordHasher.VerifyPassword(model.Password, user.Password))
            {
                await LogAccessAttemptAsync(user?.Id, model.UserName, "Failure", "Invalid credentials", ipAddress, 401);
                ModelState.AddModelError(string.Empty, InvalidCredentialsMessage);
                return View("~/Pages/Public/Login.cshtml", model);
            }

            if (user.IsDisabled == AppConstants.Usuarios.Bloqueado)
            {
                var lastObservation = await _context.UserObservations
                    .Where(o => o.UserId == user.Id)
                    .OrderByDescending(o => o.CreatedAt)
                    .Select(o => o.Observation)
                    .FirstOrDefaultAsync();

                await LogAccessAttemptAsync(user.Id, model.UserName, "Blocked", "User blocked", ipAddress, 403);
                TempData["BlockedObservation"] = lastObservation ?? "Su cuenta ha sido bloqueada. Por favor, contacte con soporte.";
                return View("~/Pages/Public/Login.cshtml", model);
            }

            if (user.IsDisabled != AppConstants.Usuarios.Activo)
            {
                await LogAccessAttemptAsync(user.Id, model.UserName, "Failure", "Inactive user", ipAddress, 403);
                ModelState.AddModelError(string.Empty, InvalidCredentialsMessage);
                return View("~/Pages/Public/Login.cshtml", model);
            }

            var validRoles = new HashSet<string>
            {
                AppConstants.Roles.Admin,
                AppConstants.Roles.Soporte,
                AppConstants.Roles.SuperAdmin,
                AppConstants.Roles.Consultor,
                AppConstants.Roles.ApiConsumer
            };

            var userRoles = user.UserRols?
                .Select(ur => ur.Rol?.Name)
                .Where(r => !string.IsNullOrEmpty(r))
                .Cast<string>()
                .ToList() ?? new List<string>();

            if (!userRoles.Any(validRoles.Contains))
            {
                await LogAccessAttemptAsync(user.Id, model.UserName, "Failure", "Unauthorized role", ipAddress, 403);
                ModelState.AddModelError(string.Empty, "No tiene permisos para acceder.");
                return View("~/Pages/Public/Login.cshtml", model);
            }

            var claims = new List<Claim>
            {
                new(ClaimTypes.Name, user.UserName ?? string.Empty),
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new("token_version", user.TokenVersion.ToString())
            };

            foreach (var role in userRoles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = model.RememberMe,
                ExpiresUtc = model.RememberMe
                    ? DateTimeOffset.UtcNow.AddDays(14)
                    : DateTimeOffset.UtcNow.AddHours(2)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            await LogAccessAttemptAsync(user.Id, model.UserName, "Success", "Login OK", ipAddress, 200);
            return RedirectToAction("Index", "Admin");
        }

        [HttpGet("logout")]
        public async Task<IActionResult> Logout(string? expired = null)
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            if (expired == "1")
            {
                return Redirect("/login?expired=1");
            }
            return RedirectToAction("Index");
        }

        // ───────── Restablecimiento de contraseña (autoservicio) ─────────
        [HttpPost("forgot-password/request")]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("login")]
        public async Task<IActionResult> ForgotPasswordRequest([FromBody] ForgotPasswordRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.UserName))
            {
                return BadRequest(new { error = "Ingrese su nombre de usuario." });
            }

            var userName = request.UserName.Trim();
            var user = await _context.Users
                .Include(u => u.UserRols!)
                .ThenInclude(ur => ur.Rol)
                .FirstOrDefaultAsync(u => u.UserName == userName);

            if (user == null)
            {
                return BadRequest(new { error = "Usuario no encontrado." });
            }

            if (string.IsNullOrEmpty(user.Password))
            {
                return BadRequest(new { error = "El usuario no tiene una cuenta administrativa." });
            }

            if (user.IsDisabled != AppConstants.Usuarios.Activo)
            {
                return BadRequest(new { error = "El usuario está inactivo o bloqueado. Contacte con soporte." });
            }

            var userRoles = user.UserRols?
                .Select(ur => ur.Rol?.Name)
                .Where(r => !string.IsNullOrEmpty(r))
                .Cast<string>()
                .ToList() ?? new List<string>();

            if (userRoles.Count == 0)
            {
                return BadRequest(new { error = "El usuario no tiene roles asignados. Contacte con soporte." });
            }

            if (string.IsNullOrWhiteSpace(user.Email))
            {
                return BadRequest(new { error = "El usuario no tiene un correo electrónico registrado. Contacte con soporte." });
            }

            var code = RandomNumberGenerator.GetInt32(100000, 1000000).ToString("D6");
            HttpContext.Session.SetString(SessionKeyForgotUser, user.UserName ?? string.Empty);
            HttpContext.Session.SetString(SessionKeyForgotCode, code);
            HttpContext.Session.SetString(SessionKeyForgotExpires, DateTimeOffset.UtcNow.Add(ForgotCodeLifetime).ToString("O"));

            var emailResult = await _email.SendEmailAsync(
                user.Email,
                "Código de restablecimiento de contraseña",
                BuildResetCodeEmail(user.FullName, user.UserName ?? string.Empty, code),
                HttpContext.RequestAborted);

            if (!emailResult.Success)
            {
                ClearForgotSession();
                _logger.LogWarning("No se pudo enviar el código de restablecimiento a {UserName}: {Error}", userName, emailResult.Error);
                return BadRequest(new { error = "No se pudo enviar el correo. Verifique la configuración SMTP del sistema." });
            }

            return Ok(new { message = "Se envió un código de verificación a su correo electrónico.", maskedEmail = MaskEmail(user.Email) });
        }

        [HttpPost("forgot-password/reset")]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("login")]
        public async Task<IActionResult> ForgotPasswordReset([FromBody] ForgotPasswordResetRequest request)
        {
            var sessionUser = HttpContext.Session.GetString(SessionKeyForgotUser);
            var sessionCode = HttpContext.Session.GetString(SessionKeyForgotCode);
            var sessionExpires = HttpContext.Session.GetString(SessionKeyForgotExpires);

            if (string.IsNullOrEmpty(sessionUser) || string.IsNullOrEmpty(sessionCode))
            {
                return BadRequest(new { error = "La solicitud de restablecimiento no está activa. Inicie el proceso nuevamente." });
            }

            if (string.IsNullOrWhiteSpace(request?.UserName)
                || !string.Equals(request.UserName.Trim(), sessionUser, StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { error = "El usuario no coincide con la solicitud de restablecimiento." });
            }

            if (string.IsNullOrWhiteSpace(request.Code)
                || !string.Equals(request.Code.Trim(), sessionCode, StringComparison.Ordinal))
            {
                return BadRequest(new { error = "El código de verificación es incorrecto." });
            }

            if (DateTimeOffset.TryParse(sessionExpires, out var expires) && DateTimeOffset.UtcNow > expires)
            {
                ClearForgotSession();
                return BadRequest(new { error = "El código ha expirado. Solicite uno nuevo." });
            }

            if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 6)
            {
                return BadRequest(new { error = "La contraseña debe tener al menos 6 caracteres." });
            }

            if (request.NewPassword != request.ConfirmPassword)
            {
                return BadRequest(new { error = "Las contraseñas no coinciden." });
            }

            var user = await _context.Users
                .Include(u => u.UserRols!)
                .ThenInclude(ur => ur.Rol)
                .FirstOrDefaultAsync(u => u.UserName == sessionUser);

            if (user == null
                || string.IsNullOrEmpty(user.Password)
                || user.IsDisabled != AppConstants.Usuarios.Activo
                || (user.UserRols?.Any(ur => ur.Rol != null && ur.Rol.State) ?? false) == false)
            {
                ClearForgotSession();
                return BadRequest(new { error = "El usuario ya no cumple las condiciones para restablecer la contraseña. Contacte con soporte." });
            }

            user.Password = _passwordHasher.HashPassword(request.NewPassword);
            user.TokenVersion++;
            user.UpdatedAt = DateTimeOffset.UtcNow;
            user.UpdatedBy = "self-service";
            await _context.SaveChangesAsync();

            ClearForgotSession();
            return Ok(new { message = "Contraseña actualizada correctamente. Ya puede iniciar sesión." });
        }

        private void ClearForgotSession()
        {
            HttpContext.Session.Remove(SessionKeyForgotUser);
            HttpContext.Session.Remove(SessionKeyForgotCode);
            HttpContext.Session.Remove(SessionKeyForgotExpires);
        }

        private static string MaskEmail(string email)
        {
            var at = email.IndexOf('@');
            if (at <= 1) return email;
            return $"{email[..1]}***{email[at..]}";
        }

        private static string BuildResetCodeEmail(string? fullName, string userName, string code)
        {
            Func<string, string> escape = HtmlEncoder.Default.Encode;
            return $@"
                <div style=""font-family:Segoe UI,Roboto,Arial,sans-serif;max-width:560px;margin:0 auto;padding:24px"">
                    <h2 style=""color:#1d4ed8;margin:0 0 16px"">Restablecimiento de contraseña</h2>
                    <p>Hola {escape(fullName ?? string.Empty)}:</p>
                    <p>Has solicitado restablecer la contraseña de tu cuenta <strong>{escape(userName)}</strong>. Usa el siguiente código de verificación:</p>
                    <div style=""display:inline-block;padding:14px 24px;background:#f3f4f6;border:1px solid #e5e7eb;border-radius:8px;font-size:28px;font-family:Consolas,monospace;letter-spacing:6px;margin:12px 0"">{escape(code)}</div>
                    <p style=""color:#4b5563;font-size:13px"">El código es válido por 10 minutos. Si no solicitaste este cambio, ignora este correo.</p>
                </div>";
        }

        private async Task LogAccessAttemptAsync(Guid? userId, string userName, string status, string details, string? ipAddress, int responseCode)
        {
            try
            {
                _context.AccessLogs.Add(new AccessLog
                {
                    Id = Guid.NewGuid(),
                    UserId = userId?.ToString(),
                    UserName = userName ?? string.Empty,
                    Action = "Login",
                    Status = status,
                    Details = details,
                    IpAddress = ipAddress ?? "Unknown",
                    RequestPath = Request.Path,
                    ResponseCode = responseCode,
                    Timestamp = DateTimeOffset.UtcNow
                });
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "No se pudo registrar AccessLog para {UserName}", userName);
            }
        }
    }

    public record ForgotPasswordRequest(string UserName);

    public record ForgotPasswordResetRequest(string UserName, string Code, string NewPassword, string ConfirmPassword);
}
