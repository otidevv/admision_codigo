using ADMISION.ENTITIES.Models.Infrastructure;

namespace ADMISION.Services.Interfaces
{
    public interface IClassroomService
    {
        Task<IReadOnlyList<Classroom>> GetAllAsync(Guid? pavilionId, CancellationToken ct = default);
        Task<Classroom?> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<Classroom> CreateAsync(Classroom classroom, string actor, CancellationToken ct = default);
        Task<bool> UpdateAsync(Classroom classroom, string actor, CancellationToken ct = default);
        Task<DeleteOutcome> DeleteAsync(Guid id, CancellationToken ct = default);

        Task<IReadOnlyList<PavilionOption>> GetActivePavilionsAsync(CancellationToken ct = default);
    }

    public record PavilionOption(Guid Id, string Name, string? Code);
}
