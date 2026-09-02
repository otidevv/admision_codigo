using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ADMISION.ENTITIES.Models.Biometrics
{
    [Table("PostulantPhoto", Schema = "Postulant")]
    public class PostulantPhoto
    {
        [Key]
        public Guid Id { get; set; }
        public Guid PostulantId { get; set; }
        public string PhotoUrl { get; set; } = string.Empty;
        public bool IsPrimary { get; set; }
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public string CreatedBy { get; set; } = string.Empty;

        [ForeignKey("PostulantId")]
        public virtual Postulant.Postulant? Postulant { get; set; }
    }
}
