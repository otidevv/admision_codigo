using ADMISION.ENTITIES.Models.Integrations;
using ADMISION.ENTITIES.Models.Postulant;
using ADMISION.ENTITIES.Models.Postulante;
using ADMISION.ENTITIES.Models.Users;

namespace ADMISION.Services.Interfaces
{
    /// <summary>
    /// Resumen completo del postulante: búsqueda, detalle, todas sus secciones
    /// (inscripciones, pagos, observaciones, retiros, resultados, biométricos)
    /// y operaciones SuperAdmin sobre la nota de admisión.
    /// </summary>
    public interface IPostulantResumeService
    {
        Task<IReadOnlyList<Postulant>> SearchAsync(string query, CancellationToken ct = default);
        Task<Postulant?> GetByIdAsync(Guid postulantId, CancellationToken ct = default);

        Task<IReadOnlyList<Inscription>> GetInscriptionsAsync(Guid postulantId, CancellationToken ct = default);
        Task<IReadOnlyList<Inscription>> GetPaymentsAsync(Guid postulantId, CancellationToken ct = default);
        Task<PostulantObservationsResult> GetObservationsAsync(Guid postulantId, CancellationToken ct = default);
        Task<IReadOnlyList<ObservationSearchItem>> SearchObservationsAsync(Guid postulantId, string? searchTerm, CancellationToken ct = default);
        Task<IReadOnlyList<Inscription>> GetResignationsAsync(Guid postulantId, CancellationToken ct = default);
        Task<IReadOnlyList<Inscription>> GetResultsAsync(Guid postulantId, CancellationToken ct = default);
        Task<Dictionary<Guid, string>> GetTematicAreaCodesAsync(Guid postulantId, CancellationToken ct = default);
        Task<IReadOnlyList<Inscription>> GetForBiometricsAsync(Guid postulantId, CancellationToken ct = default);

        /// <summary>Apoderado(s) registrados para el postulante (tabla Postulant.Parent).</summary>
        Task<IReadOnlyList<Parent>> GetParentsAsync(Guid postulantId, CancellationToken ct = default);

        /// <summary>
        /// Constancias y demás documentos oficiales emitidos al postulante.
        /// </summary>
        Task<IReadOnlyList<IssuedDocumentItem>> GetIssuedDocumentsAsync(Guid postulantId, CancellationToken ct = default);

        Task<bool> AddObservationAsync(Guid postulantId, string scope, Guid? inscriptionId, string observation, string actor, string? tipoObservacion = null, CancellationToken ct = default);

        /// <summary>
        /// Actualiza una observación asociada a una inscripción del postulante.
        /// Solo aplica a observaciones de inscripción (no a las de usuario).
        /// </summary>
        Task<bool> UpdateInscriptionObservationAsync(Guid observationId, Guid postulantId, string observation, string? tipoObservacion, string actor, CancellationToken ct = default);

        // ── Carga de archivos de requisitos pendientes (SuperAdmin) ──────────
        /// <summary>
        /// Requisitos que la inscripción debería tener según su modalidad/tipo de
        /// modalidad (y tipo de postulante) pero que aún no tienen archivo subido.
        /// </summary>
        Task<IReadOnlyList<RequirementOption>> GetPendingRequirementsAsync(Guid inscriptionId, Guid postulantId, CancellationToken ct = default);
        Task<UploadRequirementFileResult> UploadRequirementFileAsync(Guid inscriptionId, Guid postulantId, Guid requirementId, IFormFile file, string actor, CancellationToken ct = default);

        Task<GradeUpdateOutcome> SetInscriptionGradeAsync(Guid postulantId, Guid inscriptionId, decimal? gradeAdmission, bool isAdmission, string actor, CancellationToken ct = default);
        Task<bool> ClearInscriptionGradeAsync(Guid postulantId, Guid inscriptionId, string actor, CancellationToken ct = default);

        // Fotos
        Task<PhotoCaptureResult> SavePhotoAsync(Guid postulantId, string base64Image, string actor, string photosWebRoot, CancellationToken ct = default);
        Task<IReadOnlyList<PostulantPhotoListItem>> GetPhotosAsync(Guid postulantId, CancellationToken ct = default);
        Task<bool> SetPrimaryPhotoAsync(Guid postulantId, Guid photoId, CancellationToken ct = default);
        Task<DeletePhotoResult> DeletePhotoAsync(Guid postulantId, Guid photoId, string wwwRoot, CancellationToken ct = default);

        // Huellas
        Task<IReadOnlyList<FingerprintListItem>> GetFingerprintsAsync(Guid postulantId, CancellationToken ct = default);
        Task<FingerprintCaptureOutcome> SaveFingerprintAsync(string actor,Guid postulantId, string template, string? imageBase64, string? deviceIp, CancellationToken ct = default);
        Task<bool> DeleteFingerprintAsync(Guid postulantId, Guid fingerprintId, CancellationToken ct = default);

        // ── Validación de archivos del expediente ──────────────────────────
        Task<PostulantValidationDto?> GetValidationAsync(Guid postulantId, CancellationToken ct = default);
        Task<ValidationToggleResult> SetFileValidatedAsync(Guid fileId, bool isValidated, string? note, string actor, CancellationToken ct = default);
        Task<ValidationToggleResult> SetPaymentApprovedAsync(Guid paymentId, bool isApproved, string? note, string actor, CancellationToken ct = default);
        Task<ReplaceFileResult> ReplaceFileSubmissionAsync(Guid fileId, IFormFile newFile, Guid postulantId, string actor, CancellationToken ct = default);

        // ── Edición de comprobante de pago ──────────────────────────────
        Task<EditPaymentResult> EditPaymentAsync(Guid paymentId, Guid postulantId, string? operationCode, IFormFile? newFile, Guid? externalPaymentVoucherId, bool disassociate, string actor, CancellationToken ct = default);
        Task<IReadOnlyList<ExternalPaymentVoucher>> GetUnassociatedExternalPaymentsAsync(Guid postulantId, CancellationToken ct = default);

        // ── Edición de datos personales del postulante ─────────────────────
        Task<ADMISION.ENTITIES.Models.Users.Users?> GetUserForEditAsync(Guid postulantId, CancellationToken ct = default);
        Task<bool> UpdatePersonalDataAsync(Guid postulantId, ADMISION.ENTITIES.Models.Users.Users updated, List<Guid>? disabilityTypeIds, string? conadisNumber, string actor, CancellationToken ct = default);

        // ── Edición de inscripción desde el expediente ─────────────────────
        Task<Inscription?> GetInscriptionForEditAsync(Guid postulantId, Guid inscriptionId, CancellationToken ct = default);
        Task<bool> UpdateInscriptionAsync(Guid postulantId, Inscription updated, string actor, CancellationToken ct = default);
        Task<List<Guid>> GetModalityCareerIdsAsync(Guid modalityId, CancellationToken ct = default);

        // ── Propagación de ubigeo a todas las inscripciones del postulante ─
        Task<int> PropagateUbigeoAsync(Guid postulantId, Guid currentInscriptionId, Guid? countryId, Guid? distritId, string actor, CancellationToken ct = default);

        // ── Renuncias ─────────────────────────────────────────────────────
        Task<SaveResignationResult> SaveResignationAsync(Guid inscriptionId, DateTimeOffset dateResignation, string description, IFormFile? file, string actor, CancellationToken ct = default);

        // ── Anulaciones de inscripción (por postulante) ───────────────────
        Task<IReadOnlyList<Annulment>> GetAnnulmentsAsync(Guid postulantId, CancellationToken ct = default);
        Task<SaveAnnulmentResult> SaveAnnulmentAsync(Guid postulantId, DateTimeOffset startDate, DateTimeOffset endDate, string description, IFormFile? file, string actor, CancellationToken ct = default);
        Task<SaveAnnulmentResult> DeleteAnnulmentAsync(Guid postulantId, Guid annulmentId, CancellationToken ct = default);
    }

    public class SaveAnnulmentResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class SaveResignationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    /// Resumen de archivos subidos por inscripción, agrupados para el checklist
    /// de validación. Incluye comprobantes de pago y archivos de requisitos.
    /// </summary>
    public class PostulantValidationDto
    {
        public Guid PostulantId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public List<InscriptionValidationGroup> Inscriptions { get; set; } = new();
    }

    public class InscriptionValidationGroup
    {
        public Guid InscriptionId { get; set; }
        public string CodePostulant { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string? TermName { get; set; }
        public string? ModalityName { get; set; }
        public string? TypeModalityName { get; set; }
        public string? CareerName { get; set; }
        public string? FacultyName { get; set; }
        public string? TypePostulantName { get; set; }
        public DateTimeOffset CreatedAt { get; set; }

        public List<ValidationFileItem> Files { get; set; } = new();
        public int TotalFiles => Files.Count;
        public int ValidatedCount => Files.Count(f => f.IsValidated);
        public int PercentComplete => TotalFiles == 0 ? 0 : (int)Math.Round(ValidatedCount * 100.0 / TotalFiles);
    }

    /// <summary>
    /// Resultado de marcar/desmarcar la validación de un archivo o pago.
    /// Cuando todos los archivos de la inscripción quedan validados, el
    /// servicio aprueba automáticamente la inscripción y reporta el cambio.
    /// </summary>
    public class ValidationToggleResult
    {
        public bool Found { get; set; }
        public Guid? InscriptionId { get; set; }
        public string? PreviousState { get; set; }
        public string? NewState { get; set; }
        public bool StateChanged => PreviousState != NewState && NewState != null;
        public int ValidatedCount { get; set; }
        public int TotalCount { get; set; }
        public bool AllValidated => TotalCount > 0 && ValidatedCount == TotalCount;
    }

    public class ValidationFileItem
    {
        public Guid Id { get; set; }
        /// <summary>"requirement" | "payment"</summary>
        public string Kind { get; set; } = "requirement";
        /// <summary>Etiqueta legible del campo (ej. "Comprobante de pago", "Certificado de estudios").</summary>
        public string FieldLabel { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string? FileSize { get; set; }
        public string? FileType { get; set; }
        public bool IsValidated { get; set; }
        public DateTimeOffset? ValidatedAt { get; set; }
        public string? ValidatedBy { get; set; }
        public string? ValidationNote { get; set; }
        /// <summary>Código de operación / número de comprobante del pago.</summary>
        public string OperationCode { get; set; } = string.Empty;
        /// <summary>Nombre del método de pago (ej. "Yape", "Plin").</summary>
        public string? PaymentMethodName { get; set; }
        /// <summary>Monto del pago.</summary>
        public decimal Amount { get; set; }
        /// <summary>Indica si el pago está asociado a un voucher externo de la API.</summary>
        public bool HasExternalAssociation { get; set; }
        /// <summary>Datos completos del voucher externo asociado (para mostrar en modal).</summary>
        public ExternalPaymentVoucher? ExternalVoucher { get; set; }
    }

    public record PostulantObservationsResult(
        IReadOnlyList<Inscription> Inscriptions,
        IReadOnlyList<ADMISION.ENTITIES.Models.Users.Observations> UserObservations);

    public class ObservationSearchItem
    {
        public Guid Id { get; set; }
        public string Kind { get; set; } = string.Empty;
        public string Observation { get; set; } = string.Empty;
        public string? TipoObservacion { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public string Context { get; set; } = string.Empty;
        public string? CodePostulant { get; set; }
    }

    /// <summary>
    /// Resultado del registro de un archivo de requisito faltante desde el panel
    /// (Solo SuperAdmin). Indica si ya existía un archivo para el requisito.
    /// </summary>
    public class UploadRequirementFileResult
    {
        public bool Success { get; set; }
        public bool NotFound { get; set; }
        public bool AlreadyExists { get; set; }
        public bool NotRequired { get; set; }
        public string? ErrorMessage { get; set; }
        public string? NewFileName { get; set; }
        public string? NewFilePath { get; set; }
        public string? NewFileSize { get; set; }
    }

    public enum GradeUpdateOutcome
    {
        Updated,
        NotFound,
        InvalidGrade
    }

    public class PhotoCaptureResult
    {
        public bool Success { get; init; }
        public string? PhotoUrl { get; init; }
        public string? ErrorMessage { get; init; }
        public bool PostulantNotFound { get; init; }
    }

    public record PostulantPhotoListItem(Guid Id, string PhotoUrl, bool IsPrimary, DateTimeOffset CreatedAt);

    /// <summary>
    /// Resultado de la eliminación de una foto del expediente. Indica si se
    /// eliminó la foto primaria (la vista debe recargar el avatar) y la URL
    /// de la nueva foto primaria si se promovió otra automáticamente.
    /// </summary>
    public class DeletePhotoResult
    {
        public bool Success { get; init; }
        public bool NotFound { get; init; }
        public bool DeletedPrimary { get; init; }
        public string? NewPrimaryPhotoUrl { get; init; }
    }

    public record FingerprintListItem(Guid Id, int FingerIndex, DateTimeOffset CreatedAt, string? ImageBase64);

    /// <summary>
    /// Fila plana de la bitácora de documentos emitidos para mostrar dentro del
    /// expediente del postulante. Trae nombres ya resueltos (DocumentType,
    /// inscripción, modalidad) para que la vista no haga Includes adicionales.
    /// </summary>
    public class IssuedDocumentItem
    {
        public Guid Id { get; set; }
        public string DocumentTypeName { get; set; } = string.Empty;
        public string DocumentTypeCode { get; set; } = string.Empty;
        public string CorrelativeDisplay { get; set; } = string.Empty;
        public int Year { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public string? WatermarkText { get; set; }
        public Guid? InscriptionId { get; set; }
        public string? InscriptionCode { get; set; }
        public string? ModalityName { get; set; }
        public string? TermName { get; set; }
    }

    public class FingerprintCaptureOutcome
    {
        public bool Success { get; init; }
        public string? ErrorMessage { get; init; }
        public bool PostulantNotFound { get; init; }
        public bool LimitReached { get; init; }
    }

    public class ReplaceFileResult
    {
        public bool Success { get; init; }
        public bool NotFound { get; init; }
        public string? NewFilePath { get; init; }
        public string? NewFileName { get; init; }
        public string? NewFileSize { get; init; }
        public string? ErrorMessage { get; init; }
    }

    public class EditPaymentResult
    {
        public bool Success { get; set; }
        public bool NotFound { get; set; }
        public string? ErrorMessage { get; set; }
        public string? NewFileName { get; set; }
        public string? NewFilePath { get; set; }
        public string? NewFileSize { get; set; }
        public string OperationCode { get; set; } = string.Empty;
        public bool HasExternalAssociation { get; set; }
    }
}
