using ADMISION.Models.ViewModels.Public;
using Microsoft.AspNetCore.Http;

namespace ADMISION.Services.Interfaces
{
    /// <summary>
    /// Encapsula el alta de inscripción pública: transacción EF, get-or-create de
    /// User+Postulant, validación de duplicado, persistencia del comprobante y de
    /// los archivos de requisitos dinámicos. El controller solo construye el input
    /// (parseando Request.Form.Files) y mapea el Outcome a JSON.
    /// </summary>
    public interface IInscriptionService
    {
        Task<InscriptionRegisterResult> RegisterAsync(InscriptionRegisterInput input, CancellationToken ct = default);
    }

    public class InscriptionRegisterInput
    {
        public required EnrollmentViewModel Model { get; init; }
        public IList<RequirementFile> RequirementFiles { get; init; } = new List<RequirementFile>();
        public string CreatedBy { get; init; } = "PublicPortal";
        public string? RemoteIp { get; init; }
    }

    public record RequirementFile(Guid RequirementId, IFormFile File);

    public enum InscriptionOutcome
    {
        Success,
        Duplicate,
        InvalidFile,
        Blocked,
        Error
    }

    public class InscriptionRegisterResult
    {
        public InscriptionOutcome Outcome { get; init; }
        public Guid? InscriptionId { get; init; }
        public string? Message { get; init; }

        // InvalidFile
        public string? FileName { get; init; }
        public string? FileReason { get; init; }
        public string? FileContextLabel { get; init; }

        // Error
        public string? CorrelationId { get; init; }
        public Exception? Exception { get; init; }
    }
}
