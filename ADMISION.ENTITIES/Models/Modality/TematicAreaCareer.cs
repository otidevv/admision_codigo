using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace ADMISION.ENTITIES.Models.Modality
{
    [Table("TematicAreaCareer", Schema = "Modality")]
    public class TematicAreaCareer
    {
        public Guid Id { get; set; }
        public Guid TematicAreaId { get; set; }
        public Guid CareerId { get; set; }
        public Guid TermId { get; set; }
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? UpdatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public string? UpdatedBy { get; set; } = string.Empty;

        [ForeignKey("TematicAreaId")]
        public virtual TematicArea? TematicArea { get; set; }

        [ForeignKey("CareerId")]
        public virtual Career? Career { get; set; }

        [ForeignKey("TermId")]
        public virtual Term? Term { get; set; }

    }
}
