using System.ComponentModel.DataAnnotations.Schema;
using ADMISION.ENTITIES.Models.Modality;
using ADMISION.ENTITIES.Models.Users;

namespace ADMISION.ENTITIES.Models.Infrastructure
{
    [Table("ExamScheduleRoom", Schema = "Infrastructure")]
    public class ExamScheduleRoom
    {
        public Guid Id { get; set; }
        public Guid ExamScheduleId { get; set; }
        public Guid ClassroomId { get; set; }
        public Guid? TeacherId { get; set; }
        public Guid TematicAreaId { get; set; }
        public int AssignedCapacity { get; set; }
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public string CreatedBy { get; set; } = string.Empty;

        [ForeignKey("ExamScheduleId")]
        public virtual ExamSchedule? ExamSchedule { get; set; }

        [ForeignKey("ClassroomId")]
        public virtual Classroom? Classroom { get; set; }

        [ForeignKey("TeacherId")]
        public virtual Teachers? Teacher { get; set; }

        [ForeignKey("TematicAreaId")]
        public virtual TematicArea? TematicArea { get; set; }
    }
}
