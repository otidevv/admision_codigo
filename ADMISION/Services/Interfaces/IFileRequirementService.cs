using ADMISION.ENTITIES.Models.Requirement;
using ADMISION.Models.Shared;

namespace ADMISION.Services.Interfaces
{
    public interface IFileRequirementService
    {
        Task<PagedResult<FileRequirementListItem>> ListAsync(ListQuery query, CancellationToken ct = default);
        Task<FileRequirementManagement?> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<FileRequirementManagement> CreateAsync(FileRequirementManagement entity, string actor, CancellationToken ct = default);
        Task<bool> UpdateAsync(FileRequirementManagement entity, string actor, CancellationToken ct = default);
        Task<RequirementDeleteOutcome> DeleteAsync(Guid id, CancellationToken ct = default);
    }

    public record FileRequirementListItem(
        Guid Id,
        string Name,
        string? Description,
        decimal MaxFileSizeMB,
        string? FilePathExtencion);

    /// <summary>
    /// Outcome específico que distingue el FK contra TypePostulantRequisite (mensaje al
    /// usuario más útil que un genérico "tiene registros asociados").
    /// </summary>
    public enum RequirementDeleteOutcome
    {
        Deleted,
        NotFound,
        UsedByTypePostulant,
        HasOtherDependencies
    }
}
