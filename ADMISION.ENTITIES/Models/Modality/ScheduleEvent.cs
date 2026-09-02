using System.ComponentModel.DataAnnotations.Schema;

namespace ADMISION.ENTITIES.Models.Modality
{
    [Table("ScheduleEvent", Schema = "Modality")]
    public class ScheduleEvent
    {
        public Guid Id { get; set; }
        public Guid TermId { get; set; }

        public string Phase { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateOnly StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
        public string Schedule { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; } = true;

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? UpdatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public string? UpdatedBy { get; set; }

        [ForeignKey("TermId")]
        public virtual Term? Term { get; set; }
    }
}
