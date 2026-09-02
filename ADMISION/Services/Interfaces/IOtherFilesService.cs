using ADMISION.ENTITIES.Models.Info;
using Microsoft.AspNetCore.Http;

namespace ADMISION.Services.Interfaces
{
    /// <summary>
    /// CRUD genérico sobre la tabla `OtherFiles` parametrizado por categoría
    /// (Temario, Reglamento, Otros). Cada controller pasa su categoría y la
    /// carpeta de almacenamiento a usar para los archivos.
    /// </summary>
    public interface IOtherFilesService
    {
        Task<IReadOnlyList<OtherFiles>> GetByCategoryAsync(string category, CancellationToken ct = default);
        Task<OtherFiles?> GetByIdAsync(Guid id, string category, CancellationToken ct = default);
        Task<OtherFiles> CreateAsync(OtherFiles file, IFormFile? uploadFile, string category, string storageModule, string actor, CancellationToken ct = default);
        Task<bool> UpdateAsync(OtherFiles file, IFormFile? uploadFile, string category, string storageModule, string actor, CancellationToken ct = default);
        Task<DeleteOutcome> DeleteAsync(Guid id, string category, CancellationToken ct = default);
    }
}
