using System.ComponentModel.DataAnnotations.Schema;

namespace ADMISION.ENTITIES.Models.Exam
{
    [Table("CepreImportRecord", Schema = "Exam")]
    public class CepreImportRecord
    {
        public Guid Id { get; set; }
        public Guid TermId { get; set; }
        public Guid VersionId { get; set; }

        // Columnas del Excel tal cual
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
        public string? Modalidad { get; set; }
        public string? CodigoCarrera { get; set; }
        public string? CarreraProfesional { get; set; }
        public string? Grupo { get; set; }
        public string? ModalidadPago { get; set; }
        public decimal? Monto { get; set; }
        public decimal? Nota01 { get; set; }
        public decimal? Puntaje01 { get; set; }
        public decimal? Nota02 { get; set; }
        public decimal? Puntaje02 { get; set; }
        public decimal? Nota03 { get; set; }
        public decimal? Puntaje03 { get; set; }
        public decimal? NotaFinal { get; set; }
        public decimal? Puntaje { get; set; }
        public string? Ubigeo { get; set; }
        public string? Departamento { get; set; }
        public string? Provincia { get; set; }
        public string? Distrito { get; set; }
        public string? LugarNacimiento { get; set; }
        public string? Estado { get; set; }

        // Auditoría
        public bool IsValid { get; set; }
        public string? ValidationError { get; set; }
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public string CreatedBy { get; set; } = "";

        [ForeignKey("VersionId")]
        public virtual CepreImportVersion? Version { get; set; }
    }
}
