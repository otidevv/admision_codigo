using ADMISION.ENTITIES.Models.Exam;

namespace ADMISION.Services.Interfaces
{
    public class AdmissionImportRow
    {
        public int Nro { get; set; }
        public string? Codigo { get; set; }
        public string? ApellidosNombres { get; set; }
        public string? CarreraProfesional { get; set; }
        public string? Grupo { get; set; }
        public string? Correctas { get; set; }
        public string? Blancas { get; set; }
        public string? Puntaje { get; set; }
        public string? Nota { get; set; }
        public string? Condicion { get; set; }
        public bool IsValid { get; set; } = true;
        public string? ValidationError { get; set; }
    }

    public class CepreImportRow
    {
        public int Nro { get; set; }
        public string? Ciclo { get; set; }
        public string? Codigo { get; set; }
        public string? Dni { get; set; }
        public string? TDocumento { get; set; }
        public string? Apaterno { get; set; }
        public string? Amaterno { get; set; }
        public string? Nombres { get; set; }
        public string? ApellidosNombres { get; set; }
        public string? Sexo { get; set; }
        public string? FechaNacimiento { get; set; }
        public string? Direccion { get; set; }
        public string? EstadoCivil { get; set; }
        public string? AnioEgreso { get; set; }
        public string? Correo { get; set; }
        public string? Celular { get; set; }
        public string? Colegio { get; set; }
        public string? NombreColegio { get; set; }
        public string? UbigeoColegio { get; set; }
        public string? DireccionColegio { get; set; }
        public string? Ubigeo { get; set; }
        public string? Departamento { get; set; }
        public string? Provincia { get; set; }
        public string? Distrito { get; set; }
        public string? LugarNacimiento { get; set; }
        public string? Modalidad { get; set; }
        public string? CodigoCarrera { get; set; }
        public string? CarreraProfesional { get; set; }
        public string? Grupo { get; set; }
        public string? ModalidadPago { get; set; }
        public decimal? Monto { get; set; }
        public decimal? Puntaje01 { get; set; }
        public decimal? Nota01 { get; set; }
        public decimal? Puntaje02 { get; set; }
        public decimal? Nota02 { get; set; }
        public decimal? Puntaje03 { get; set; }
        public decimal? Nota03 { get; set; }
        public decimal? NotaFinal { get; set; }
        public decimal? Puntaje { get; set; }
        public string? Estado { get; set; }
        public bool IsValid { get; set; } = true;
        public string? ValidationError { get; set; }
    }

    public class ImportPreviewResult<T>
    {
        public int TotalRows { get; set; }
        public int ValidRows { get; set; }
        public int InvalidRows { get; set; }
        public List<T> Rows { get; set; } = new();
        public List<string> Errors { get; set; } = new();
    }

    public class ImportCommitResult
    {
        public int Imported { get; set; }
        public int Skipped { get; set; }
        public List<string> Errors { get; set; } = new();
    }

    public class ImportBatchDto
    {
        public Guid Id { get; set; }
        public int RecordCount { get; set; }
        public string CreatedBy { get; set; } = "";
        public DateTimeOffset CreatedAt { get; set; }
    }

    public class CepreMatchRow
    {
        public int Nro { get; set; }
        public string? Dni { get; set; }
        public string? CodigoCarrera { get; set; }
        public string? CarreraProfesional { get; set; }
        public string? ApellidosNombres { get; set; }
        public decimal? NotaFinal { get; set; }
        public string? Estado { get; set; }
        public string? InscriptionCode { get; set; }
        public bool IsValid { get; set; } = true;
        public string? ValidationError { get; set; }
    }

    public interface IExamResultImportService
    {
        Task<ImportPreviewResult<AdmissionImportRow>> PreviewAdmissionAsync(Stream excelStream, string fileName, Guid termId, Guid modalityId, CancellationToken ct = default);
        Task<ImportCommitResult> ImportAdmissionAsync(List<AdmissionImportRow> rows, Guid termId, Guid modalityId, string actor, CancellationToken ct = default);
        byte[] BuildAdmissionTemplate();

        Task<ImportPreviewResult<CepreImportRow>> PreviewCepreAsync(Stream excelStream, string fileName, Guid termId, CancellationToken ct = default);
        Task<ImportCommitResult> ImportCepreAsync(List<CepreImportRow> rows, Guid termId, string actor, CancellationToken ct = default);
        byte[] BuildCepreTemplate();

        Task<List<ImportBatchDto>> GetAdmissionImportHistoryAsync(Guid termId, CancellationToken ct = default);
        Task<List<ImportBatchDto>> GetCepreImportHistoryAsync(Guid termId, CancellationToken ct = default);
        Task<int> RevertAdmissionImportAsync(Guid batchId, string actor, CancellationToken ct = default);
        Task<int> RevertCepreImportAsync(Guid batchId, string actor, CancellationToken ct = default);

        // Turnos
        Task<List<CepreTurn>> GetTurnsByTermAsync(Guid termId, CancellationToken ct = default);
        Task<CepreTurn?> GetActiveTurnAsync(Guid termId, Guid userId, CancellationToken ct = default);
        Task<bool> HasActiveTurnAsync(Guid termId, Guid userId, CancellationToken ct = default);
        Task<bool> CreateTurnAsync(CepreTurn turn, CancellationToken ct = default);
        Task<bool> DeleteTurnAsync(Guid turnId, CancellationToken ct = default);

        // Versiones
        Task<List<CepreImportVersion>> GetVersionsAsync(Guid termId, CancellationToken ct = default);
        Task<CepreImportVersion?> GetLatestVersionAsync(Guid termId, CancellationToken ct = default);

        // CEPRE Match
        Task<ImportPreviewResult<CepreMatchRow>> PreviewCepreMatchAsync(Guid termId, Guid modalityId, CancellationToken ct = default);
        Task<ImportCommitResult> ImportCepreMatchAsync(List<CepreMatchRow> rows, Guid termId, Guid modalityId, string actor, CancellationToken ct = default);
        Task<List<ImportBatchDto>> GetCepreMatchHistoryAsync(Guid termId, CancellationToken ct = default);
        Task<int> RevertCepreMatchAsync(Guid batchId, string actor, CancellationToken ct = default);
    }
}
