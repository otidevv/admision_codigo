using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace ADMISION.ENTITIES.Models.Modality
{
    [Table("CareerImage", Schema = "Modality")]
    public class CareerImage
    {
        public Guid Id { get; set; }
        public Guid CareerId { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public string? Caption { get; set; }
        public int DisplayOrder { get; set; }

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public string CreatedBy { get; set; } = string.Empty;

        [ForeignKey("CareerId")]
        public virtual Career? Career { get; set; }
    }
}
