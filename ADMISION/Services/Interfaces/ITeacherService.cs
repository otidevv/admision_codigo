using ADMISION.Models.Shared;
using ADMISION.Models.ViewModels.Admin;
using ADMISION.ENTITIES.Models.Users;

namespace ADMISION.Services.Interfaces
{
    public interface ITeacherService
    {
        Task<IReadOnlyList<Teachers>> GetAllAsync(CancellationToken ct = default);
        Task<Teachers?> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<TeacherFormViewModel?> GetForEditAsync(Guid id, CancellationToken ct = default);
        Task<SaveResult> SaveAsync(TeacherFormViewModel model, string actor, CancellationToken ct = default);
        Task<DeleteOutcome> DeleteAsync(Guid id, CancellationToken ct = default);
        Task<Teachers?> ToggleActiveAsync(Guid id, string actor, CancellationToken ct = default);
        Task<bool> ExistsDocumentAsync(string document, Guid? excludeId = null, CancellationToken ct = default);
        Task<TeacherImportResult> ImportFromExcelAsync(Stream excelStream, string actor, CancellationToken ct = default);
    }
}
