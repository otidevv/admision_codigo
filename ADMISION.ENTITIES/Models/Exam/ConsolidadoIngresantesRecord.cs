using System.ComponentModel.DataAnnotations.Schema;
using ADMISION.ENTITIES.Models.Modality;
using ADMISION.ENTITIES.Models.Postulante;

namespace ADMISION.ENTITIES.Models.Exam
{
    [Table("ConsolidadoIngresantesRecord", Schema = "Exam")]
    public class ConsolidadoIngresantesRecord
    {
        public Guid Id { get; set; }
        public Guid TermId { get; set; }
        public Guid VersionId { get; set; }
        public Guid? InscriptionId { get; set; }

        // Datos del consolidado
        public string CodigoEstudiante { get; set; } = "";
        public string CodigoCarrera { get; set; } = "";
        public string? SegundaCarrera { get; set; }
        public string? Semestre { get; set; }
        public string Nombres { get; set; } = "";
        public string Paterno { get; set; } = "";
        public string Materno { get; set; } = "";
        public string? DType { get; set; }
        public string? DNI { get; set; }
        public string? Email { get; set; }
        public string? Celular { get; set; }
        public string? Direccion { get; set; }
        public string? FechaNacimiento { get; set; }
        public string? Sexo { get; set; }
        public string? EstadoCivil { get; set; }
        public string? Ubigeo { get; set; }
        public string? TipoPostulante { get; set; }
        public string? TipoObs { get; set; }
        public string? Observaciones { get; set; }

        // Auditoría
        public int Nro { get; set; }
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public string CreatedBy { get; set; } = "";

        [ForeignKey("TermId")]
        public virtual Term? Term { get; set; }

        [ForeignKey("VersionId")]
        public virtual ConsolidadoIngresantesVersion? Version { get; set; }

        [ForeignKey("InscriptionId")]
        public virtual Inscription? Inscription { get; set; }
    }
}
