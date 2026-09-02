using System.ComponentModel.DataAnnotations.Schema;
using ADMISION.ENTITIES.Models.Modality;
using ADMISION.ENTITIES.Models.Postulante;

namespace ADMISION.ENTITIES.Models.Exam
{
    [Table("ExamScoreRecord", Schema = "Exam")]
    public class ExamScoreRecord
    {
        public Guid Id { get; set; }
        public Guid InscriptionId { get; set; }
        public Guid? TematicAreaId { get; set; }
        public int Correctas { get; set; }
        public int Blancas { get; set; }
        public decimal Puntaje { get; set; }
        public decimal? Nota { get; set; }
        public bool EsIngresante { get; set; }
        public string Source { get; set; } = "";
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public string CreatedBy { get; set; } = "";
        public DateTimeOffset? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }

        [ForeignKey("InscriptionId")]
        public virtual Inscription? Inscription { get; set; }

        [ForeignKey("TematicAreaId")]
        public virtual TematicArea? TematicArea { get; set; }
    }
}
