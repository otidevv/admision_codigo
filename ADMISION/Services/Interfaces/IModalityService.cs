using ADMISION.ENTITIES.Models.Modality;
using ADMISION.Models.Shared;

namespace ADMISION.Services.Interfaces
{
    public interface IModalityService
    {
        Task<PagedResult<ModalityListItem>> ListAsync(ModalityListQuery query, CancellationToken ct = default);
        Task<Modality?> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<SaveResult> CreateAsync(Modality modality, string actor, CancellationToken ct = default);
        Task<SaveResult> UpdateAsync(Modality modality, string actor, CancellationToken ct = default);
        Task<DeleteOutcome> DeleteAsync(Guid id, CancellationToken ct = default);
        Task<IReadOnlyList<NamedOption>> GetByTermAsync(Guid termId, CancellationToken ct = default);
        Task<IReadOnlyList<Modality>> GetEntitiesByTermAsync(Guid termId, CancellationToken ct = default);
        Task<IReadOnlyList<Guid>> GetCareerIdsAsync(Guid modalityId, CancellationToken ct = default);
        Task SaveCareerAssociationsAsync(Guid modalityId, List<Guid> careerIds, CancellationToken ct = default);
    }

    public class ModalityListQuery : ListQuery
    {
        public Guid? TermId { get; set; }
    }

    public record ModalityListItem(
        Guid Id,
        string Name,
        string? Description,
        bool IsActive,
        int Orden,
        bool IsCepreExam,
        bool RequiresProfilePhoto,
        bool IsMockExam,
        bool RequiresEducationalLevel,
        bool RequiresGrade,
        DateOnly StartDate,
        DateOnly EndDate,
        TimeOnly StartTime,
        TimeOnly EndTime,
        string? TermName);
}
