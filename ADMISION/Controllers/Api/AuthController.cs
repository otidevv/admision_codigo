using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ADMISION.ENTITIES.Constants;
using ADMISION.ENTITIES.Data;
using ADMISION.ENTITIES.Models.Api;
using ADMISION.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace ADMISION.Controllers.Api
{
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            AppDbContext context,
            IPasswordHasher passwordHasher,
            IConfiguration configuration,
            ILogger<AuthController> logger)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _configuration = configuration;
            _logger = logger;
        }

        [HttpPost("login")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (request == null)
            {
                return BadRequest(new { error = "El cuerpo de la solicitud debe ser JSON con Content-Type: application/json." });
            }

            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new { error = "Usuario y contraseña son requeridos." });
            }

            var user = await _context.Users
                .Include(u => u.UserRols!)
                .ThenInclude(ur => ur.Rol)
                .FirstOrDefaultAsync(u => u.UserName == request.Username);

            if (user == null
                || string.IsNullOrEmpty(user.Password)
                || !_passwordHasher.VerifyPassword(request.Password, user.Password))
            {
                return Unauthorized(new { error = "Credenciales inválidas." });
            }

            if (user.IsDisabled != AppConstants.Usuarios.Activo)
            {
                return Unauthorized(new { error = "Usuario deshabilitado." });
            }

            var hasApiRole = user.UserRols?
                .Any(ur => ur.Rol?.Name == AppConstants.Roles.ApiConsumer) ?? false;

            if (!hasApiRole)
            {
                return Unauthorized(new { error = "El usuario no tiene permisos de API." });
            }

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var jti = Guid.NewGuid().ToString("N");
            var now = DateTime.UtcNow;
            var expireMinutes = _configuration.GetValue<int>("Jwt:ExpireMinutes", 60);
            var expires = now.AddMinutes(expireMinutes);

            var jwtSection = _configuration.GetSection("Jwt");
            var secretKey = Encoding.UTF8.GetBytes(jwtSection["SecretKey"]!);
            var issuer = jwtSection["Issuer"]!;
            var audience = jwtSection["Audience"]!;

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Name, user.UserName ?? string.Empty),
                new(ClaimTypes.Role, AppConstants.Roles.ApiConsumer),
                new("jti", jti),
                new("token_version", user.TokenVersion.ToString()),
                new("iat", new DateTimeOffset(now).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
            };

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: expires,
                signingCredentials: new SigningCredentials(
                    new SymmetricSecurityKey(secretKey),
                    SecurityAlgorithms.HmacSha256)
            );

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

            _context.ApiTokens.Add(new ApiToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                JwtId = jti,
                IssuedAt = now,
                ExpiresAt = expires,
                CreatedByIp = ipAddress
            });

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al guardar ApiToken para {User}", user.UserName);
                return StatusCode(500, new { error = "Error interno al generar token." });
            }

            return Ok(new
            {
                access_token = tokenString,
                token_type = "Bearer",
                expires_in = expireMinutes * 60,
                issued_at = now
            });
        }

        public class LoginRequest
        {
            public string Username { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
        }
    }
}
