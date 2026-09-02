using System.ComponentModel.DataAnnotations.Schema;
using ADMISION.ENTITIES.Models.Modality;

namespace ADMISION.ENTITIES.Models.Exam
{
    [Table("PostulantTypeConfig", Schema = "Exam")]
    public class PostulantTypeConfig
    {
        public Guid Id { get; set; }
        public string Description { get; set; } = string.Empty;
        public int Index { get; set; }
        public Guid TermId { get; set; }
        public Guid? CareerId { get; set; }
        public Guid? ModalityId { get; set; }
        public Guid? TypeModalityId { get; set; }
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public string CreatedBy { get; set; } = string.Empty;

        [ForeignKey("TermId")]
        public virtual Term? Term { get; set; }
        [ForeignKey("CareerId")]
        public virtual Career? Career { get; set; }
        [ForeignKey("ModalityId")]
        public virtual Modality.Modality? Modality { get; set; }
        [ForeignKey("TypeModalityId")]
        public virtual TypeModality? TypeModality { get; set; }
    }
}
