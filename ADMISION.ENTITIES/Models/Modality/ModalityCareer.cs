using System.ComponentModel.DataAnnotations.Schema;

namespace ADMISION.ENTITIES.Models.Modality
{
    [Table("ModalityCareer", Schema = "Modality")]
    public class ModalityCareer
    {
        public Guid Id { get; set; }
        public Guid ModalityId { get; set; }
        public Guid CareerId { get; set; }

        [ForeignKey("ModalityId")]
        public virtual Modality? Modality { get; set; }

        [ForeignKey("CareerId")]
        public virtual Career? Career { get; set; }
    }
}
