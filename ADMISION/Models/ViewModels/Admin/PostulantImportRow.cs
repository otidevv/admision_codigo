namespace ADMISION.Models.ViewModels.Admin;

public class PostulantImportRow
{
    public int RowNumber { get; set; }
    public string? Periodo { get; set; }
    public string? FechaInicio { get; set; }
    public string? FechaFin { get; set; }
    public string? Modalidad { get; set; }
    public string? TipoModalidad { get; set; }
    public string? CodigoPostulante { get; set; }
    public string? CodigoCarrera { get; set; }
    public string? FechaInscripcion { get; set; }
    public string? Dni { get; set; }
    public string? Apaterno { get; set; }
    public string? Amaterno { get; set; }
    public string? Nombres { get; set; }
    public string? Sexo { get; set; }
    public string? FechaNacimiento { get; set; }
    public string? Direccion { get; set; }
    public string? EstadoCivil { get; set; }
    public string? Correo { get; set; }
    public string? Celular { get; set; }
    public string? Colegio { get; set; }
    public string? TipoPostulante { get; set; }
    public string? TipoDiscapacidad { get; set; }
    public string? Pais { get; set; }
    public string? CodUbigeo { get; set; }
    public string? DniApoderado { get; set; }
    public string? ApellidosApoderado { get; set; }
    public string? NombresApoderado { get; set; }
    public string? TelfCelApoderado { get; set; }

    public List<string> Errors { get; set; } = new();
    public bool IsValid => Errors.Count == 0;
}
