using ADMISION.ENTITIES.Models.Requirement;
using ADMISION.Models.Shared;

namespace ADMISION.Services.Interfaces
{
    public interface ITypePostulantRequisiteService
    {
        Task<PagedResult<TypePostulantRequisiteListItem>> ListAsync(TypePostulantRequisiteListQuery query, CancellationToken ct = default);
        Task<TypePostulantRequisiteCreateOutcome> CreateAsync(TypePostulantRequisite entity, string actor, CancellationToken ct = default);
        Task<DeleteAssignmentOutcome> DeleteAsync(Guid id, CancellationToken ct = default);
    }

    public class TypePostulantRequisiteListQuery : ListQuery
    {
        public Guid? TypePostulantInscriptionId { get; set; }
    }

    public record TypePostulantRequisiteListItem(
        Guid Id,
        string? TypePostulantName,
        string? RequirementName);

    public class TypePostulantRequisiteCreateOutcome
    {
        public bool Created { get; init; }
        public bool AlreadyExists { get; init; }
        public Guid TypePostulantInscriptionId { get; init; }

        public static TypePostulantRequisiteCreateOutcome Ok(Guid typePostulantId) =>
            new() { Created = true, TypePostulantInscriptionId = typePostulantId };
        public static TypePostulantRequisiteCreateOutcome Duplicate() =>
            new() { Created = false, AlreadyExists = true };
    }

    public class DeleteAssignmentOutcome
    {
        public DeleteOutcome Outcome { get; init; }
        public Guid? TypePostulantInscriptionId { get; init; }
    }
}
