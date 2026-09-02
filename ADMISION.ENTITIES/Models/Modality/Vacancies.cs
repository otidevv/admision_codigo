using System.ComponentModel.DataAnnotations.Schema;
using System.Security.Cryptography.X509Certificates;

namespace ADMISION.ENTITIES.Models.Modality
{
    [Table("Vacancies", Schema = "Modality")]
    public class Vacancies
    {
        public Guid Id { get; set; }
        public Guid ModalityId { get; set; }
        public Guid CareerId { get; set; }
        public Guid? TypeModalityId { get; set; }
        public int Quantity { get; set; }
        public int Available { get; set; }
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? UpdatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public string? UpdatedBy { get; set; } = string.Empty;

        [ForeignKey("ModalityId")]
        public virtual Models.Modality.Modality? Modality { get; set; }
        [ForeignKey("CareerId")]
        public virtual Models.Modality.Career? Career { get; set; }
        [ForeignKey("TypeModalityId")]
        public virtual Models.Modality.TypeModality? TypeModality { get; set; }


    }
}
