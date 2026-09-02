using System.ComponentModel.DataAnnotations.Schema;

namespace ADMISION.ENTITIES.Models.Postulant
{
    [Table("Annulment", Schema = "Postulant")]
    public class Annulment
    {
        public Guid Id { get; set; }
        [ForeignKey("Postulant")]
        public Guid PostulantId { get; set; }
        public DateTimeOffset StartDate { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset EndDate { get; set; } = DateTimeOffset.UtcNow;
        public string Description { get; set; } = string.Empty;
        public string File { get; set; } = string.Empty;
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? UpdatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public string? UpdatedBy { get; set; } = string.Empty;

        public virtual Postulant? Postulant { get; set; }
    }
}
