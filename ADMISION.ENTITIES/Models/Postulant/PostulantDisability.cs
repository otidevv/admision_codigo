using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ADMISION.ENTITIES.Models.Postulant
{
    public class PostulantDisability
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid PostulantId { get; set; }

        [Required]
        public Guid DisabilityTypeId { get; set; }

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public string? CreatedBy { get; set; }

        // Navigation properties
        [ForeignKey("PostulantId")]
        public virtual Postulant? Postulant { get; set; }

        [ForeignKey("DisabilityTypeId")]
        public virtual DisabilityType? DisabilityType { get; set; }
    }
}
