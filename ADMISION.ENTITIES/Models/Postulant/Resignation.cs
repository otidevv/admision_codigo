using System.ComponentModel.DataAnnotations.Schema;

namespace ADMISION.ENTITIES.Models.Postulant
{
    [Table("Resignation", Schema = "Postulant")]
    public class Resignation
    {
        public Guid Id { get; set; }
        [ForeignKey("Inscription")]
        public Guid InscriptionId { get; set; }
        public DateTimeOffset DateResignation { get; set; } = DateTimeOffset.UtcNow;
        public string Description { get; set; } = string.Empty;
        public string File {  get; set; } = string.Empty;
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? UpdatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public string? UpdatedBy { get; set; } = string.Empty;

        public virtual Postulante.Inscription? Inscription { get; set; }
    }
}
