using System.Security.Claims;
using ADMISION.ENTITIES.Models.Integrations;
using ADMISION.Models.Shared;

namespace ADMISION.Services.Interfaces
{
    public interface IExternalApiService
    {
        // ───────── Invocación (ya existente) ─────────
        Task<ApiInvocationResult> InvokeAsync(
            Guid apiId,
            IDictionary<string, string?> parameters,
            ClaimsPrincipal user,
            string? remoteIp,
            CancellationToken ct = default);

        // ───────── Admin CRUD ─────────
        Task<IReadOnlyList<ExternalApi>> GetAllAsync(CancellationToken ct = default);
        Task<ExternalApi?> GetByIdAsync(Guid id, bool tracking = false, CancellationToken ct = default);
        Task<SaveResult> CreateAsync(ExternalApi model, string actor, CancellationToken ct = default);
        Task<SaveResult> UpdateAsync(ExternalApi model, string actor, CancellationToken ct = default);
        Task<ExternalApiDeleteOutcome> DeleteAsync(Guid id, string actor, CancellationToken ct = default);

        // ───────── Logs / auditoría ─────────
        Task<PagedResult<ApiQueryLog>> GetLogsAsync(Guid? apiId, int page, int pageSize, CancellationToken ct = default);
        Task<ApiQueryLog?> GetLogByIdAsync(Guid logId, CancellationToken ct = default);

        // ───────── Persistencia estructurada ─────────
        Task SaveAcademicInfoAsync(IEnumerable<ExternalAcademicInfo> records, CancellationToken ct = default);
        Task SavePaymentVouchersAsync(IEnumerable<ExternalPaymentVoucher> vouchers, CancellationToken ct = default);

        // ───────── Lectura de datos persistidos ─────────
        Task<IReadOnlyList<ExternalAcademicInfo>> GetAcademicInfoByDniAsync(string dni, CancellationToken ct = default);
        Task<IReadOnlyList<ExternalPaymentVoucher>> GetPaymentVouchersByDniAsync(string dni, CancellationToken ct = default);

        // ───────── Fetch + upsert/insert-only desde API externa ─────────
        Task<AcademicFetchResult> FetchAndSaveAcademicAsync(Guid apiId, string dni, ClaimsPrincipal user, string? remoteIp, CancellationToken ct = default);
        Task<PaymentFetchResult> FetchAndSavePaymentsAsync(Guid apiId, string dni, ClaimsPrincipal user, string? remoteIp, CancellationToken ct = default);

        // ───────── Búsqueda de API por categoría ─────────
        Task<ExternalApi?> FindApiByCategoryAsync(string category, CancellationToken ct = default);

        // ───────── Verificación de penalizados (pre-inscripción) ─────────
        /// <summary>
        /// Consulta la API "CONSULTA_PENALIZADOS" (Integrations.ExternalApi) por DNI y determina
        /// si el postulante debe ser impedido de inscribirse: porque figura Sancionado/Expulsado
        /// en el campo StudentStatus, o porque la carrera devuelta coincide con la que postula.
        /// La API puede devolver un solo "infoStudent" o varios con el mismo DNI (dos carreras);
        /// cada registro se evalúa con su propio estado y carrera. Ante cualquier fallo de la
        /// integración NO bloquea (fail-open) y reporta Error.
        /// </summary>
        Task<SanctionCheckResult> CheckSanctionsAsync(
            string dni,
            string inscribingCareerName,
            string? remoteIp,
            CancellationToken ct = default);
    }

    /// <summary>
    /// Un registro estudiante↔carrera tal como aparece en la respuesta de la API.
    /// Una persona con dos carreras aparecerá dos veces, cada uno con su propio estado.
    /// </summary>
    public class StudentSanctionRecord
    {
        /// <summary>Valor de StudentStatus (ej. "Invicto", "Sancionado", "Expulsado").</summary>
        public string? StudentStatus { get; set; }

        /// <summary>Nombre de la carrera asociada en ese registro.</summary>
        public string? CareerName { get; set; }
    }

    /// <summary>
    /// Resultado de la consulta de penalizados previa a la inscripción pública.
    /// </summary>
    public class SanctionCheckResult
    {
        /// <summary>true si debe impedirse el registro del postulante.</summary>
        public bool Blocked { get; set; }

        /// <summary>Mensaje para mostrar al postulante cuando está bloqueado.</summary>
        public string? Message { get; set; }

        /// <summary>true si la carrera a la que postula coincide con una carrera devuelta por la API.</summary>
        public bool CareerMatch { get; set; }

        /// <summary>Estado detectado (ej. "Sancionado", "Expulsado") que provocó el bloqueo.</summary>
        public string? StudentStatus { get; set; }

        /// <summary>Carrera vinculada al estado bloqueante (contexto cuando el bloqueo lo causó StudentStatus).</summary>
        public string? StudentCareer { get; set; }

        /// <summary>Carrera coincidente detectada en la respuesta de la API.</summary>
        public string? CareerName { get; set; }

        /// <summary>Registros estudiante↔carrera detectados (1 o varios con el mismo DNI).</summary>
        public List<StudentSanctionRecord> Records { get; } = new();

        /// <summary>Indicador de fallo de la integración (no bloquea). En él se registra el motivo.</summary>
        public string? Error { get; set; }

        /// <summary>Respuesta cruda de la API (para auditoría/debug).</summary>
        public string? RawResponse { get; set; }
    }

    public class AcademicFetchResult
    {
        public bool Success { get; set; }
        public string? Error { get; set; }
        public int Count { get; set; }
        public Guid? LogId { get; set; }
        public List<ExternalAcademicInfo> Records { get; set; } = new();
    }

    public class PaymentFetchResult
    {
        public bool Success { get; set; }
        public string? Error { get; set; }
        public int VouchersCount { get; set; }
        public int PaymentsCount { get; set; }
        public Guid? LogId { get; set; }
        public List<ExternalPaymentVoucher> Records { get; set; } = new();
    }

    public class ApiInvocationResult
    {
        public bool Success { get; set; }
        public int StatusCode { get; set; }
        public string? RawResponse { get; set; }
        public string? Error { get; set; }
        public Guid LogId { get; set; }
        public int DurationMs { get; set; }

        // Filas extraídas para mostrar en una tabla user-friendly.
        public IList<ApiResultRow> Rows { get; set; } = new List<ApiResultRow>();
    }

    public class ApiResultRow
    {
        public string Label { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }

    /// <summary>
    /// Distingue el soft-delete (cuando hay logs auditables) del borrado real.
    /// </summary>
    public enum ExternalApiDeleteOutcome
    {
        Deleted,
        SoftDeleted, // Se marcó IsActive = false porque ya tenía consultas registradas.
        NotFound
    }
}
