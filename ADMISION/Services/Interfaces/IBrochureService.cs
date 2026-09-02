using ADMISION.ENTITIES.Models.Info;
using Microsoft.AspNetCore.Http;

namespace ADMISION.Services.Interfaces
{
    public interface IBrochureService
    {
        Task<IReadOnlyList<Brochure>> GetAllAsync(CancellationToken ct = default);
        Task<Brochure?> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<Brochure?> GetActiveAsync(CancellationToken ct = default);
        Task<Brochure> CreateAsync(Brochure brochure, IFormFile? uploadFile, string actor, CancellationToken ct = default);
        Task<bool> UpdateAsync(Brochure brochure, IFormFile? uploadFile, string actor, CancellationToken ct = default);
        Task<DeleteOutcome> DeleteAsync(Guid id, CancellationToken ct = default);
    }
}
