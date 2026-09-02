using ADMISION.ENTITIES.Models.Modality;
using Microsoft.AspNetCore.Http;

namespace ADMISION.Services.Interfaces
{
    public interface IExamResultService
    {
        Task<IReadOnlyList<ExamResult>> GetAllAsync(CancellationToken ct = default);
        Task<ExamResult?> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<ExamResult> CreateAsync(ExamResult result, IFormFile pdfFile, string actor, CancellationToken ct = default);
        Task<bool> UpdateAsync(ExamResult result, IFormFile? pdfFile, string actor, CancellationToken ct = default);
        Task<DeleteOutcome> DeleteAsync(Guid id, CancellationToken ct = default);
    }
}
