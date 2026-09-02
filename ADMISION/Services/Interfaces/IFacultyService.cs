using ADMISION.ENTITIES.Models.Modality;

namespace ADMISION.Services.Interfaces
{
    /// <summary>
    /// CRUD de Facultades. Centraliza la persistencia para que el controller
    /// solo manipule HTTP/UI.
    /// </summary>
    public interface IFacultyService
    {
        Task<IReadOnlyList<Faculty>> GetAllAsync(CancellationToken ct = default);
        Task<Faculty?> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<Faculty> CreateAsync(Faculty faculty, string actor, CancellationToken ct = default);
        Task<bool> UpdateAsync(Faculty faculty, string actor, CancellationToken ct = default);
        Task<DeleteOutcome> DeleteAsync(Guid id, CancellationToken ct = default);
    }

    public enum DeleteOutcome
    {
        Deleted,
        NotFound,
        HasDependencies
    }
}
