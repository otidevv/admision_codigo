using ADMISION.ENTITIES.Models.Info;
using Microsoft.AspNetCore.Http;

namespace ADMISION.Services.Interfaces
{
    public interface IBannerService
    {
        Task<List<Banner>> GetActiveBannersAsync();

        // Admin CRUD
        Task<IReadOnlyList<Banner>> GetAllAsync(CancellationToken ct = default);
        Task<Banner?> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<Banner> CreateAsync(Banner banner, IFormFile? imageHorizontal, IFormFile? imageVertical, string actor, CancellationToken ct = default);
        Task<bool> UpdateAsync(Banner banner, IFormFile? imageHorizontal, IFormFile? imageVertical, string actor, CancellationToken ct = default);
        Task<DeleteOutcome> DeleteAsync(Guid id, CancellationToken ct = default);
    }
}
