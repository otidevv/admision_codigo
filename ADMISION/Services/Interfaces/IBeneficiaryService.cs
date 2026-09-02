using ADMISION.ENTITIES.Models.Modality;

namespace ADMISION.Services.Interfaces
{
    public interface IBeneficiaryService
    {
        Task<IReadOnlyList<Beneficiarie>> GetAllAsync(CancellationToken ct = default);
        Task<Beneficiarie?> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<Beneficiarie> CreateAsync(Beneficiarie entity, string actor, CancellationToken ct = default);
        Task<bool> UpdateAsync(Beneficiarie entity, string actor, CancellationToken ct = default);
        Task<DeleteOutcome> DeleteAsync(Guid id, CancellationToken ct = default);
    }
}
