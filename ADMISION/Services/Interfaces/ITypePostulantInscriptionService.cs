using ADMISION.ENTITIES.Models.Postulant;
using ADMISION.Models.Shared;

namespace ADMISION.Services.Interfaces
{
    public interface ITypePostulantInscriptionService
    {
        Task<PagedResult<TypePostulantListItem>> ListAsync(ListQuery query, CancellationToken ct = default);
        Task<TypePostulantInscription?> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<TypePostulantInscription> CreateAsync(TypePostulantInscription entity, string actor, CancellationToken ct = default);
        Task<bool> UpdateAsync(TypePostulantInscription entity, string actor, CancellationToken ct = default);
        Task<DeleteOutcome> DeleteAsync(Guid id, CancellationToken ct = default);
    }

    public record TypePostulantListItem(
        Guid Id,
        string Name,
        string? Description,
        decimal DiscountPercentage,
        bool IsActive);
}
