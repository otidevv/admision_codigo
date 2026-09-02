using ADMISION.ENTITIES.Models.Modality;
using ADMISION.Models.Shared;

namespace ADMISION.Services.Interfaces
{
    public interface ITematicAreaService
    {
        Task<PagedResult<TematicAreaListItem>> ListAsync(ListQuery query, CancellationToken ct = default);
        Task<TematicArea?> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<TematicArea> CreateAsync(TematicArea area, string actor, CancellationToken ct = default);
        Task<bool> UpdateAsync(TematicArea area, string actor, CancellationToken ct = default);
        Task<DeleteOutcome> DeleteAsync(Guid id, CancellationToken ct = default);

        Task<IReadOnlyList<TematicArea>> GetAllAsync(CancellationToken ct = default);

        // Matriz de asignación carrera × área temática para un término.
        Task<IReadOnlyList<CareerWithTematicAreas>> GetMatrixAsync(Guid termId, CancellationToken ct = default);
        Task SaveMatrixAsync(Guid termId, IList<CareerTematicAreaAssignment> assignments, string actor, CancellationToken ct = default);
        Task SaveCareerAssignmentsAsync(Guid termId, Guid careerId, IList<Guid> selectedAreaIds, string actor, CancellationToken ct = default);
    }

    public record TematicAreaListItem(Guid Id, string Code, DateTimeOffset CreatedAt);

    public record CareerWithTematicAreas(Guid Id, string Name, IReadOnlyList<Guid> TematicAreaIds);

    public class CareerTematicAreaAssignment
    {
        public Guid CareerId { get; set; }
        public List<Guid> TematicAreaIds { get; set; } = new();
    }
}
