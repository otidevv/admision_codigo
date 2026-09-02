namespace ADMISION.Services.Interfaces
{
    /// <summary>
    /// Genera el PDF "Constancia de Inscripción" del postulante a partir de
    /// una inscripción ya registrada. Encapsula la lectura de los datos
    /// relacionados (postulante, carrera, modalidad, área temática, periodo)
    /// y la generación del código QR de verificación.
    /// </summary>
    public interface IInscriptionDocumentService
    {
        Task<DocumentResult?> BuildConstanciaAsync(Guid inscriptionId, string? verificationBaseUrl, bool onlyIfMockExam = false, CancellationToken ct = default);

        /// <summary>
        /// Datos mínimos para mostrar la pantalla de verificación pública
        /// cuando el QR es escaneado. Devuelve <c>null</c> si la inscripción
        /// no existe.
        /// </summary>
        Task<InscriptionVerificationDto?> GetVerificationAsync(string codePostulant, CancellationToken ct = default);
    }

    public class InscriptionVerificationDto
    {
        public bool Found { get; set; }
        public string CodePostulant { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string DocumentNumber { get; set; } = string.Empty;
        public string CareerName { get; set; } = string.Empty;
        public string ModalityName { get; set; } = string.Empty;
        public string? TypeModalityName { get; set; }
        public string TermName { get; set; } = string.Empty;
        public string? TematicAreaCode { get; set; }
        public string State { get; set; } = string.Empty;
        public DateTimeOffset InscriptionDate { get; set; }
    }
}
