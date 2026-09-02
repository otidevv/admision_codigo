using ADMISION.ENTITIES.Data;
using ADMISION.ENTITIES.Models.EconomicManagement;
using ADMISION.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ADMISION.Services.Implementations
{
    public class PaymentCodeService : IPaymentCodeService
    {
        private readonly AppDbContext _context;

        public PaymentCodeService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IReadOnlyList<PaymentCode>> GetAllWithAssociationsAsync(CancellationToken ct = default)
        {
            return await _context.PaymentCodes
                .AsNoTracking()
                .Include(p => p.Term)
                .Include(p => p.PaymentCodeModalities)
                    .ThenInclude(pm => pm.Modality)
                .Include(p => p.PaymentCodeModalities)
                    .ThenInclude(pm => pm.TypeModality)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync(ct);
        }

        public Task<PaymentCode?> GetByIdWithAssociationsAsync(Guid id, CancellationToken ct = default)
        {
            return _context.PaymentCodes
                .Include(p => p.PaymentCodeModalities)
                .FirstOrDefaultAsync(p => p.Id == id, ct);
        }

        public async Task<bool> SaveAsync(PaymentCode paymentCode, IList<PaymentCodeModality> associations, string actor, CancellationToken ct = default)
        {
            var isNew = paymentCode.Id == Guid.Empty;
            var now = DateTimeOffset.UtcNow;

            if (isNew)
            {
                paymentCode.Id = Guid.NewGuid();
                paymentCode.CreatedAt = now;
                paymentCode.CreatedBy = actor;
                _context.PaymentCodes.Add(paymentCode);
            }
            else
            {
                var existing = await _context.PaymentCodes
                    .Include(p => p.PaymentCodeModalities)
                    .FirstOrDefaultAsync(p => p.Id == paymentCode.Id, ct);
                if (existing == null) return false;

                existing.Code = paymentCode.Code;
                existing.Description = paymentCode.Description;
                existing.IsActive = paymentCode.IsActive;
                existing.TermId = paymentCode.TermId;
                existing.UpdatedAt = now;
                existing.UpdatedBy = actor;

                // Reemplaza las asociaciones (más simple que diff por simplicidad).
                _context.PaymentCodesModalities.RemoveRange(existing.PaymentCodeModalities);
                paymentCode = existing;
            }

            if (associations != null)
            {
                foreach (var assoc in associations.Where(m => m.Amount > 0))
                {
                    assoc.Id = Guid.NewGuid();
                    assoc.PaymentCodeId = paymentCode.Id;
                    assoc.CreatedAt = now;
                    assoc.CreatedBy = actor;
                    _context.PaymentCodesModalities.Add(assoc);
                }
            }

            await _context.SaveChangesAsync(ct);
            return true;
        }

        public async Task<DeleteOutcome> DeleteAsync(Guid id, CancellationToken ct = default)
        {
            var code = await _context.PaymentCodes
                .Include(p => p.PaymentCodeModalities)
                .FirstOrDefaultAsync(p => p.Id == id, ct);

            if (code == null) return DeleteOutcome.NotFound;

            try
            {
                _context.PaymentCodesModalities.RemoveRange(code.PaymentCodeModalities);
                _context.PaymentCodes.Remove(code);
                await _context.SaveChangesAsync(ct);
                return DeleteOutcome.Deleted;
            }
            catch (DbUpdateException)
            {
                return DeleteOutcome.HasDependencies;
            }
        }
    }
}
