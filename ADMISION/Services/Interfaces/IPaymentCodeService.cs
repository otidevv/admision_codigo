using ADMISION.ENTITIES.Models.EconomicManagement;

namespace ADMISION.Services.Interfaces
{
    public interface IPaymentCodeService
    {
        Task<IReadOnlyList<PaymentCode>> GetAllWithAssociationsAsync(CancellationToken ct = default);
        Task<PaymentCode?> GetByIdWithAssociationsAsync(Guid id, CancellationToken ct = default);
        Task<bool> SaveAsync(PaymentCode paymentCode, IList<PaymentCodeModality> associations, string actor, CancellationToken ct = default);
        Task<DeleteOutcome> DeleteAsync(Guid id, CancellationToken ct = default);
    }
}
