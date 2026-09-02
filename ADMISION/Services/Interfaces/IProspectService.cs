using ADMISION.ENTITIES.Models.Info;
using Microsoft.AspNetCore.Http;

namespace ADMISION.Services.Interfaces
{
    public interface IProspectService
    {
        Task<IReadOnlyList<Prospect>> GetAllAsync(CancellationToken ct = default);
        Task<Prospect?> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<Prospect> CreateAsync(Prospect prospect, IFormFile? pdfFile, string actor, CancellationToken ct = default);
        Task<bool> UpdateAsync(Prospect prospect, IFormFile? pdfFile, string actor, CancellationToken ct = default);
        Task<DeleteOutcome> DeleteAsync(Guid id, CancellationToken ct = default);
    }
}
