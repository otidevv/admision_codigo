using ADMISION.ENTITIES.Models.Info;
using Microsoft.AspNetCore.Http;

namespace ADMISION.Services.Interfaces
{
    public interface ISponsorService
    {
        Task<IReadOnlyList<Sponsor>> GetAllAsync(CancellationToken ct = default);
        Task<List<Sponsor>> GetActiveSponsorsAsync(CancellationToken ct = default);
        Task<Sponsor?> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<Sponsor> CreateAsync(Sponsor sponsor, IFormFile? logo, string actor, CancellationToken ct = default);
        Task<bool> UpdateAsync(Sponsor sponsor, IFormFile? logo, string actor, CancellationToken ct = default);
        Task<DeleteOutcome> DeleteAsync(Guid id, CancellationToken ct = default);
    }
}
