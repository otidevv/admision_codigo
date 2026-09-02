using ADMISION.ENTITIES.Data;
using ADMISION.ENTITIES.Models.Api;
using ADMISION.Models.Shared;
using ADMISION.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ADMISION.Services.Implementations
{
    public class ApiLogService : IApiLogService
    {
        private readonly AppDbContext _context;

        public ApiLogService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<PagedResult<ApiRequestLog>> GetLogsAsync(string? userFilter, int page, int pageSize, CancellationToken ct = default)
        {
            var query = _context.ApiRequestLogs.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(userFilter))
            {
                query = query.Where(l => l.UserName.Contains(userFilter));
            }

            query = query.OrderByDescending(l => l.RequestedAt);

            return await PagedResult<ApiRequestLog>.CreateAsync(query, page, pageSize, ct);
        }
    }
}
