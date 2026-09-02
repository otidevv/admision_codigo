using ADMISION.ENTITIES.Models.Postulant;

namespace ADMISION.Services.Interfaces
{
    public interface IDisabilityTypeService
    {
        Task<IReadOnlyList<DisabilityTypeListItem>> ListAsync(string? search, bool? isActive, CancellationToken ct = default);
        Task<DisabilityType?> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<DisabilityType> CreateAsync(DisabilityType entity, string actor, CancellationToken ct = default);
        Task<bool> UpdateAsync(DisabilityType entity, string actor, CancellationToken ct = default);
        Task<DeleteOutcome> DeleteAsync(Guid id, CancellationToken ct = default);
    }

    public record DisabilityTypeListItem(Guid Id, string Name, string? Description, bool IsActive);
}
