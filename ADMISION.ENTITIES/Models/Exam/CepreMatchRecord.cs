using System.ComponentModel.DataAnnotations.Schema;

namespace ADMISION.ENTITIES.Models.Exam
{
    [Table("CepreMatchRecord", Schema = "Exam")]
    public class CepreMatchRecord
    {
        public Guid Id { get; set; }
        public Guid TermId { get; set; }
        public Guid ModalityId { get; set; }
        public Guid CepreVersionId { get; set; }
        public Guid? InscriptionId { get; set; }
        public Guid? ExamResultId { get; set; }

        public int Nro { get; set; }
        public string? Dni { get; set; }
        public string? CodigoCarrera { get; set; }
        public string? CarreraProfesional { get; set; }
        public string? ApellidosNombres { get; set; }
        public decimal? NotaFinal { get; set; }
        public string? Estado { get; set; }
        public bool IsAdmission { get; set; }

        public bool IsValid { get; set; }
        public string? ValidationError { get; set; }
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public string CreatedBy { get; set; } = "";

        [ForeignKey("CepreVersionId")]
        public virtual CepreImportVersion? Version { get; set; }

        [ForeignKey("InscriptionId")]
        public virtual ADMISION.ENTITIES.Models.Postulante.Inscription? Inscription { get; set; }

        [ForeignKey("ExamResultId")]
        public virtual ExamScoreRecord? ExamResult { get; set; }
    }
}
