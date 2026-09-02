using System.Security.Claims;
using ADMISION.Models.ViewModels.Admin;

namespace ADMISION.Services.Interfaces;

public interface IConsolidadoService
{
    Task<ConsolidadoPreviewViewModel> GetPreviewAsync(Guid? selectedTermId, ClaimsPrincipal? currentUser = null, string? remoteIp = null, CancellationToken ct = default);
    Task<ConsolidadoResult> ConfirmAsync(Guid termId, string createdBy, List<ConsolidadoPreviewItem>? previewItems = null);

    /// <summary>
    /// Agrega manualmente un ingresante al consolidado (solo super admin).
    /// Genera la siguiente versión tomando todos los registros de la versión
    /// anterior y adicionando este nuevo estudiante al final de su programa académico.
    /// </summary>
    Task<ConsolidadoResult> AddIngresanteAsync(Guid termId, string? codePostulant, string createdBy, CancellationToken ct = default);

    /// <summary>
    /// Carga los registros de la última versión del consolidado (solo super admin)
    /// para poder editarlos (observaciones y segunda carrera) antes de guardar
    /// una nueva versión con las modificaciones aplicadas.
    /// </summary>
    Task<ConsolidadoPreviewViewModel> GetEditAsync(Guid? selectedTermId, CancellationToken ct = default);

    /// <summary>
    /// Guarda las modificaciones (observaciones y segunda carrera) de los registros
    /// del consolidado creando una nueva versión con los cambios aplicados.
    /// </summary>
    Task<ConsolidadoResult> SaveEditsAsync(Guid termId, string createdBy, List<ConsolidadoPreviewItem>? editItems, CancellationToken ct = default);
}

public class ConsolidadoResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int RecordsSaved { get; set; }
    public int VersionNumber { get; set; }
}
