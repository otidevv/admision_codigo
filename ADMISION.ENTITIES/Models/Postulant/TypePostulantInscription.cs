using System.ComponentModel.DataAnnotations.Schema;

namespace ADMISION.ENTITIES.Models.Postulant
{
    [Table("TypePostulantInscription", Schema = "Postulant")]
    public class TypePostulantInscription
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal DiscountPercentage { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? UpdatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public string? UpdatedBy { get; set; } = string.Empty;

        public virtual ICollection<ADMISION.ENTITIES.Models.Postulante.Inscription>? Inscriptions { get; set; }
    }
}
