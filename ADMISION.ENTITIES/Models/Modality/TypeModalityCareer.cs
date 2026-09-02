using System.ComponentModel.DataAnnotations.Schema;

namespace ADMISION.ENTITIES.Models.Modality
{
    [Table("TypeModalityCareer", Schema = "Modality")]
    public class TypeModalityCareer
    {
        public Guid Id { get; set; }
        public Guid TypeModalityId { get; set; }
        public Guid CareerId { get; set; }

        [ForeignKey("TypeModalityId")]
        public virtual TypeModality? TypeModality { get; set; }

        [ForeignKey("CareerId")]
        public virtual Career? Career { get; set; }
    }
}
