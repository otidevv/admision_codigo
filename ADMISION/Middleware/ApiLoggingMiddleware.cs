using System.Diagnostics;
using System.Security.Claims;
using ADMISION.ENTITIES.Data;
using ADMISION.ENTITIES.Models.Api;

namespace ADMISION.Middleware
{
    public class ApiLoggingMiddleware : IMiddleware
    {
        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            if (!context.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase))
            {
                await next(context);
                return;
            }

            var sw = Stopwatch.StartNew();
            var originalBodyStream = context.Response.Body;

            using var responseBody = new MemoryStream();
            context.Response.Body = responseBody;

            try
            {
                await next(context);
            }
            finally
            {
                sw.Stop();
                context.Response.Body = originalBodyStream;
                responseBody.Seek(0, SeekOrigin.Begin);
                await responseBody.CopyToAsync(originalBodyStream);

                var db = context.RequestServices.GetRequiredService<AppDbContext>();

                var userId = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userName = context.User?.Identity?.Name ?? "anonymous";
                var jti = context.User?.FindFirst("jti")?.Value;

                var log = new ApiRequestLog
                {
                    Id = Guid.NewGuid(),
                    UserId = !string.IsNullOrEmpty(userId) && Guid.TryParse(userId, out var uid) ? uid : null,
                    JwtId = jti,
                    UserName = userName,
                    HttpMethod = context.Request.Method,
                    Path = context.Request.Path,
                    QueryString = context.Request.QueryString.Value,
                    StatusCode = context.Response.StatusCode,
                    IpAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    Origin = context.Request.Headers["Origin"].FirstOrDefault(),
                    UserAgent = context.Request.Headers["User-Agent"].FirstOrDefault(),
                    DurationMs = (int)sw.ElapsedMilliseconds,
                    RequestedAt = DateTimeOffset.UtcNow
                };

                try
                {
                    db.ApiRequestLogs.Add(log);
                    await db.SaveChangesAsync();
                }
                catch
                {
                    // No interrumpir la respuesta si falla el logging
                }
            }
        }
    }
}
