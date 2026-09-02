using System.ComponentModel.DataAnnotations.Schema;

namespace ADMISION.ENTITIES.Models.Modality
{
    [Table("TypeModality", Schema = "Modality")]
    public class TypeModality
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal DiscountPercentage { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? UpdatedAt { get; set; }
        public Guid ModalityId { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public string? UpdatedBy { get; set; } = string.Empty;

        [ForeignKey("ModalityId")]
        public virtual Modality? Modality { get; set; }

        public virtual ICollection<TypeModalityCareer>? TypeModalityCareers { get; set; }

    }
}
