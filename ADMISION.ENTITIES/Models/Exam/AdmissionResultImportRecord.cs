using System.ComponentModel.DataAnnotations.Schema;
using ADMISION.ENTITIES.Models.Postulante;

namespace ADMISION.ENTITIES.Models.Exam
{
    [Table("AdmissionResultImportRecord", Schema = "Exam")]
    public class AdmissionResultImportRecord
    {
        public Guid Id { get; set; }
        public Guid TermId { get; set; }
        public Guid? InscriptionId { get; set; }
        public Guid? ExamResultId { get; set; }

        // Columnas del Excel tal cual
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

        // Auditoría
        public bool IsValid { get; set; }
        public string? ValidationError { get; set; }
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public string CreatedBy { get; set; } = "";

        [ForeignKey("InscriptionId")]
        public virtual Inscription? Inscription { get; set; }

        [ForeignKey("ExamResultId")]
        public virtual ExamScoreRecord? ExamResult { get; set; }
    }
}
