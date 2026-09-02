using ADMISION.ENTITIES.Models.Exam;
using ADMISION.ENTITIES.Models.Modality;

namespace ADMISION.Models.ViewModels.Admin;

public class ConsolidadoPreviewViewModel
{
    public List<Term> Terms { get; set; } = new();
    public Guid? SelectedTermId { get; set; }
    public string? TermName { get; set; }
    public List<ConsolidadoPreviewItem> Items { get; set; } = new();
    public List<ConsolidadoIngresantesVersion> Versions { get; set; } = new();
    public bool IsSuperAdmin { get; set; }
    public bool HasExistingVersions => Versions.Count > 0;
    public string? LastVersionInfo => Versions
        .OrderByDescending(v => v.VersionNumber)
        .Select(v => $"V{v.VersionNumber} — {v.RecordCount} registros — {v.CreatedBy} — {v.CreatedAt:dd/MM/yyyy HH:mm}")
        .FirstOrDefault();

    public List<string> DuplicateDnis { get; set; } = new();
    public bool HasDuplicates => DuplicateDnis.Count > 0;
}

public class ConsolidadoPreviewItem
{
    public int Nro { get; set; }
    public string CodigoEstudiante { get; set; } = string.Empty;
    public string CodigoCarrera { get; set; } = string.Empty;
    public string SegundaCarrera { get; set; } = "0";
    public string Semestre { get; set; } = string.Empty;
    public string Nombres { get; set; } = string.Empty;
    public string Paterno { get; set; } = string.Empty;
    public string Materno { get; set; } = string.Empty;
    public string DType { get; set; } = string.Empty;
    public string DNI { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Celular { get; set; } = string.Empty;
    public string Direccion { get; set; } = string.Empty;
    public string FechaNacimiento { get; set; } = string.Empty;
    public string Sexo { get; set; } = string.Empty;
    public string EstadoCivil { get; set; } = string.Empty;
    public string Ubigeo { get; set; } = string.Empty;
    public string TipoPostulante { get; set; } = string.Empty;
    public string? TipoObs { get; set; }
    public string? Observaciones { get; set; }

    // Reference for saving
    public Guid InscriptionId { get; set; }
}
