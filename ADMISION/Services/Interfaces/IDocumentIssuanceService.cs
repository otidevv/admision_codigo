using ADMISION.ENTITIES.Models.Exam;

namespace ADMISION.Services.Interfaces
{
    public interface IDocumentIssuanceService
    {
        Task<IReadOnlyList<ConsolidadoRow>> GetIngresantesAsync(Guid versionId, CancellationToken ct = default);
        Task<ConsolidadoRow?> GetIngresanteByIdAsync(Guid recordId, CancellationToken ct = default);

        Task<DocumentIssueResult> IssueIndividualAsync(Guid recordId, bool watermark, string? actor, CancellationToken ct = default);
        Task<DocumentIssueResult> IssueBulkAsync(List<Guid> recordIds, bool watermark, string? actor, CancellationToken ct = default);
    }

    public class ConsolidadoRow
    {
        public Guid Id { get; set; }
        public Guid? InscriptionId { get; set; }
        public int Nro { get; set; }
        public string CodigoEstudiante { get; set; } = string.Empty;
        public string Nombres { get; set; } = string.Empty;
        public string Paterno { get; set; } = string.Empty;
        public string Materno { get; set; } = string.Empty;
        public string FullName => $"{Paterno} {Materno}, {Nombres}".Trim();
        public string DNI { get; set; } = string.Empty;
        public string CodigoCarrera { get; set; } = string.Empty;
        public string CareerName { get; set; } = string.Empty;
        public string? SegundaCarrera { get; set; }
        public string? Email { get; set; }
        public string? Celular { get; set; }
        public string? Sexo { get; set; }
        public string? TipoPostulante { get; set; }
        public Guid TermId { get; set; }
        public string? TermName { get; set; }
    }

    public class DocumentIssueResult
    {
        public bool Success { get; init; }
        public bool NotFound { get; init; }
        public byte[]? PdfBytes { get; init; }
        public string? FileName { get; init; }
        public byte[]? ZipBytes { get; init; }
        public int TotalCount { get; init; }
        public int SuccessCount { get; init; }
        public int ErrorCount { get; init; }
    }
}
