using ADMISION.ENTITIES.Models.Modality;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace ADMISION.ENTITIES.Models.Info
{
    [Table("PublicInfo", Schema = "Info")]
    public class PublicInfo
    {
        [Key]
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? Url { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public int DisplayOrder { get; set; }

        public Guid TermId { get; set; }
        // Vincula la pieza de información con una modalidad específica.
        // Si es null, se considera contenido general del periodo.
        public Guid? ModalityId { get; set; }

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? UpdatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public string? UpdatedBy { get; set; } = string.Empty;

        [ForeignKey("TermId")]
        public virtual Term? Term { get; set; }

        [ForeignKey("ModalityId")]
        public virtual ADMISION.ENTITIES.Models.Modality.Modality? Modality { get; set; }
    }
}
