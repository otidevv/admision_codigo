using ADMISION.Services.Interfaces;
using ADMISION.ENTITIES.Data;
using Microsoft.AspNetCore.Http;

namespace ADMISION.Services.Implementations
{
    public class AuthService : IAuthService
    {
        private readonly AppDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuthService(AppDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<bool> LoginAsync(string username, string password)
        {
            var ipAddress = _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString() ?? "Unknown";
            var status = "Failure";
            var details = "Invalid credentials";
            bool isAuthenticated = false;

            // Log the attempt
            var accessLog = new ADMISION.ENTITIES.Models.System.AccessLog
            {
                Id = Guid.NewGuid(),
                UserId = null, // Or resolve to ID if found
                IpAddress = ipAddress,
                Action = "Login",
                UserName = username,
                RequestPath = _httpContextAccessor.HttpContext?.Request.Path,
                Status = status,
                ResponseCode = isAuthenticated ? 200 : 401,
                Details = details,
                Timestamp = DateTimeOffset.UtcNow
            };

            _context.AccessLogs.Add(accessLog);
            await _context.SaveChangesAsync();

            return await Task.FromResult(isAuthenticated);
        }

        public Task LogoutAsync()
        {
            // clear authentication cookies/session
            return Task.CompletedTask;
        }
    }
}
