using System.ComponentModel.DataAnnotations.Schema;
using ADMISION.ENTITIES.Models.Modality;

namespace ADMISION.ENTITIES.Models.Exam
{
    [Table("CepreTurn", Schema = "Exam")]
    public class CepreTurn
    {
        public Guid Id { get; set; }
        public Guid TermId { get; set; }
        public Guid UserId { get; set; }
        public DateTimeOffset StartDate { get; set; }
        public DateTimeOffset EndDate { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public string CreatedBy { get; set; } = "";
        public DateTimeOffset? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }

        [ForeignKey("TermId")]
        public virtual Term? Term { get; set; }

        [ForeignKey("UserId")]
        public virtual ADMISION.ENTITIES.Models.Users.Users? User { get; set; }
    }
}
