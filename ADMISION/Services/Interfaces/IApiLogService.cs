using ADMISION.ENTITIES.Models.Api;
using ADMISION.Models.Shared;

namespace ADMISION.Services.Interfaces
{
    public interface IApiLogService
    {
        Task<PagedResult<ApiRequestLog>> GetLogsAsync(string? userFilter, int page, int pageSize, CancellationToken ct = default);
    }
}
