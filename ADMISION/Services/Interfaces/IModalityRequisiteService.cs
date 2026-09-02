using ADMISION.ENTITIES.Models.Requirement;
using ADMISION.Models.Shared;

namespace ADMISION.Services.Interfaces
{
    public interface IModalityRequisiteService
    {
        Task<PagedResult<ModalityRequisiteListItem>> ListAsync(ModalityRequisiteListQuery query, CancellationToken ct = default);
        Task<ModalityRequisiteCreateOutcome> CreateAsync(ModalityRequisite entity, string actor, CancellationToken ct = default);
        Task<ModalityRequisiteDeleteResult> DeleteAsync(Guid id, CancellationToken ct = default);

        // Grilla para asignación masiva: lista de modalidades del periodo (con tipos)
        // marcando cuáles ya tienen asignado el requisito indicado.
        Task<IReadOnlyList<AssignmentGridItem>> BuildAssignmentGridAsync(Guid termId, Guid requirementId, CancellationToken ct = default);

        // Asignación en masa: crea registros para los pares (modalityId, typeModalityId?)
        // que aún no existan. Devuelve cuántos se crearon y cuántos se omitieron.
        Task<BulkAssignmentResult> CreateBulkAsync(Guid requirementId, IReadOnlyList<BulkAssignmentSelection> selections, string actor, CancellationToken ct = default);
    }

    public class ModalityRequisiteListQuery : ListQuery
    {
        public Guid? TermId { get; set; }
        public Guid? ModalityId { get; set; }
        public Guid? TypeModalityId { get; set; }
    }

    public record ModalityRequisiteListItem(
        Guid Id,
        string? ModalityName,
        string? TypeModalityName,
        string? RequirementName);

    public class ModalityRequisiteCreateOutcome
    {
        public bool Created { get; init; }
        public bool AlreadyExists { get; init; }
        public Guid ModalityId { get; init; }
        public Guid? TypeModalityId { get; init; }

        public static ModalityRequisiteCreateOutcome Ok(Guid modalityId, Guid? typeModalityId) =>
            new() { Created = true, ModalityId = modalityId, TypeModalityId = typeModalityId };
        public static ModalityRequisiteCreateOutcome Duplicate() => new() { Created = false, AlreadyExists = true };
    }

    public class ModalityRequisiteDeleteResult
    {
        public DeleteOutcome Outcome { get; init; }
        public Guid? ModalityId { get; init; }
        public Guid? TypeModalityId { get; init; }
    }

    public record AssignmentGridItem(
        Guid ModalityId,
        string ModalityName,
        bool AlreadyAssigned,
        IReadOnlyList<AssignmentGridTypeItem> Types);

    public record AssignmentGridTypeItem(
        Guid Id,
        string Name,
        bool AlreadyAssigned);

    public record BulkAssignmentSelection(Guid ModalityId, Guid? TypeModalityId);

    public record BulkAssignmentResult(int Created, int Skipped);
}
