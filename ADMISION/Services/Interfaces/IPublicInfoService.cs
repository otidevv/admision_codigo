using ADMISION.ENTITIES.Models.Info;

namespace ADMISION.Services.Interfaces
{
    public interface IPublicInfoService
    {
        Task<IReadOnlyList<PublicInfo>> GetAllAsync(CancellationToken ct = default);
        Task<PublicInfo?> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<PublicInfo> CreateAsync(PublicInfo info, string actor, CancellationToken ct = default);
        Task<bool> UpdateAsync(PublicInfo info, string actor, CancellationToken ct = default);
        Task<DeleteOutcome> DeleteAsync(Guid id, CancellationToken ct = default);
    }
}
