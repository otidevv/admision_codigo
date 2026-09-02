using System.ComponentModel.DataAnnotations.Schema;
using ADMISION.ENTITIES.Models.Modality;
using ADMISION.ENTITIES.Models.Postulante;
using ADMISION.ENTITIES.Models.Users;

namespace ADMISION.ENTITIES.Models.Infrastructure
{
    [Table("ExamAssignment", Schema = "Infrastructure")]
    public class ExamAssignment
    {
        public Guid Id { get; set; }
        public Guid InscriptionId { get; set; }
        public Guid ClassroomId { get; set; }
        public Guid TermId { get; set; }
        public Guid ModalityId { get; set; }
        public Guid? TematicAreaId { get; set; }
        public int SeatNumber { get; set; }

        public Guid? ExamScheduleId { get; set; }
        public Guid? TeacherId { get; set; }
        public int FolderNumber { get; set; }

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public string CreatedBy { get; set; } = string.Empty;

        [ForeignKey("InscriptionId")]
        public virtual Inscription? Inscription { get; set; }

        [ForeignKey("ClassroomId")]
        public virtual Classroom? Classroom { get; set; }

        [ForeignKey("TermId")]
        public virtual Term? Term { get; set; }

        [ForeignKey("ModalityId")]
        public virtual ADMISION.ENTITIES.Models.Modality.Modality? Modality { get; set; }

        [ForeignKey("TematicAreaId")]
        public virtual TematicArea? TematicArea { get; set; }

        [ForeignKey("ExamScheduleId")]
        public virtual ExamSchedule? ExamSchedule { get; set; }

        [ForeignKey("TeacherId")]
        public virtual Teachers? Teacher { get; set; }
    }
}
