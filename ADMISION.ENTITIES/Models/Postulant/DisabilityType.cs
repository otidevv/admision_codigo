using System.ComponentModel.DataAnnotations;

namespace ADMISION.ENTITIES.Models.Postulant
{
    public class DisabilityType
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(250)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public string? CreatedBy { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }

        // Navigation property
        public virtual ICollection<PostulantDisability>? PostulantDisabilities { get; set; }
    }
}
