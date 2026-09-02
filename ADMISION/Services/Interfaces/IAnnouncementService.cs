using ADMISION.ENTITIES.Models.Info;
using Microsoft.AspNetCore.Http;

namespace ADMISION.Services.Interfaces
{
    public interface IAnnouncementService
    {
        Task<IReadOnlyList<Announcement>> GetAllAsync(CancellationToken ct = default);
        Task<List<Announcement>> GetActiveAnnouncementsAsync(CancellationToken ct = default);
        Task<Announcement?> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<Announcement> CreateAsync(Announcement announcement, IFormFile? image, string actor, CancellationToken ct = default);
        Task<bool> UpdateAsync(Announcement announcement, IFormFile? image, string actor, CancellationToken ct = default);
        Task<DeleteOutcome> DeleteAsync(Guid id, CancellationToken ct = default);
    }
}
