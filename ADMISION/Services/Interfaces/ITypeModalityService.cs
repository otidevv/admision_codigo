using ADMISION.ENTITIES.Models.Modality;
using ADMISION.Models.Shared;

namespace ADMISION.Services.Interfaces
{
    public interface ITypeModalityService
    {
        Task<PagedResult<TypeModalityListItem>> ListAsync(TypeModalityListQuery query, CancellationToken ct = default);
        Task<TypeModality?> GetByIdAsync(Guid id, bool includeModality = false, CancellationToken ct = default);
        Task<TypeModality> CreateAsync(TypeModality entity, string actor, CancellationToken ct = default);
        Task<bool> UpdateAsync(TypeModality entity, string actor, CancellationToken ct = default);
        Task<DeleteOutcome> DeleteAsync(Guid id, CancellationToken ct = default);
        Task<List<Guid>> GetCareerIdsAsync(Guid typeModalityId, CancellationToken ct = default);
        Task SaveCareerAssociationsAsync(Guid typeModalityId, List<Guid> careerIds, CancellationToken ct = default);
    }

    public class TypeModalityListQuery : ListQuery
    {
        public Guid? TermId { get; set; }
        public Guid? ModalityId { get; set; }
    }

    public record TypeModalityListItem(
        Guid Id,
        string Name,
        string? Description,
        decimal DiscountPercentage,
        bool IsActive,
        string? ModalityName);
}
