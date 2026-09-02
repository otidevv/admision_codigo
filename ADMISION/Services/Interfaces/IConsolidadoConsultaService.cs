using ADMISION.ENTITIES.Models.Exam;
using admision.Models.ViewModels.Api;

namespace ADMISION.Services.Interfaces;

public interface IConsolidadoConsultaService
{
    Task<ConsolidadoIngresantesVersion?> GetLatestVersionAsync(CancellationToken ct = default);
    Task<ConsolidadoIngresantesVersion?> GetLatestVersionByTermAsync(Guid termId, CancellationToken ct = default);
    Task<List<ConsolidadoIngresantesRecordDto>> GetRecordsByVersionAsync(Guid versionId, CancellationToken ct = default);
}