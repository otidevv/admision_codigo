using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ADMISION.Controllers.Admin
{
    [Authorize]
    [Route("admin")]
    public class AccessController : Controller
    {
        [HttpGet("restringido")]
        public IActionResult Restringido(string? from = null)
        {
            ViewBag.From = from;
            return View("~/Pages/Admin/Access/Restringido.cshtml");
        }

        // Renueva explícitamente la cookie de autenticación. El JS del layout lo
        // llama cuando el usuario confirma "Continuar sesión" en el modal de aviso,
        // o de forma proactiva ante actividad si la cookie está cerca de expirar.
        [HttpGet("session/ping")]
        public async Task<IActionResult> Ping()
        {
            var auth = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            if (!auth.Succeeded || auth.Principal is null)
            {
                return Unauthorized(new { ok = false });
            }

            var isPersistent = auth.Properties?.IsPersistent ?? false;
            var newExpiry = isPersistent
                ? DateTimeOffset.UtcNow.AddDays(14)
                : DateTimeOffset.UtcNow.AddHours(2);

            var newProps = new AuthenticationProperties
            {
                IsPersistent = isPersistent,
                IssuedUtc = DateTimeOffset.UtcNow,
                ExpiresUtc = newExpiry,
                AllowRefresh = true
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                auth.Principal,
                newProps);

            return Json(new
            {
                ok = true,
                expiresAt = newExpiry,
                secondsRemaining = (int)(newExpiry - DateTimeOffset.UtcNow).TotalSeconds
            });
        }

        // Devuelve cuántos segundos quedan antes que expire la cookie. Útil para
        // que el cliente sincronice su contador con el servidor (no se confíe sólo
        // del reloj local).
        [HttpGet("session/info")]
        public async Task<IActionResult> Info()
        {
            var auth = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            if (!auth.Succeeded || auth.Properties?.ExpiresUtc is null)
            {
                return Unauthorized(new { ok = false });
            }

            var expires = auth.Properties.ExpiresUtc.Value;
            var seconds = (int)Math.Max(0, (expires - DateTimeOffset.UtcNow).TotalSeconds);
            return Json(new { ok = true, expiresAt = expires, secondsRemaining = seconds });
        }
    }
}
