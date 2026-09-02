using System.ComponentModel.DataAnnotations.Schema;
using ADMISION.ENTITIES.Models.Modality;

namespace ADMISION.ENTITIES.Models.Infrastructure
{
    [Table("ExamSchedule", Schema = "Infrastructure")]
    public class ExamSchedule
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public Guid ModalityId { get; set; }
        public Guid TermId { get; set; }
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public string CreatedBy { get; set; } = string.Empty;
        public DateTimeOffset? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }

        [ForeignKey("ModalityId")]
        public virtual ADMISION.ENTITIES.Models.Modality.Modality? Modality { get; set; }

        [ForeignKey("TermId")]
        public virtual Term? Term { get; set; }

        public virtual ICollection<ExamScheduleRoom>? Rooms { get; set; }
    }
}
