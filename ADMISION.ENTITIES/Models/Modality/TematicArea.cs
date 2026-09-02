using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace ADMISION.ENTITIES.Models.Modality
{
    [Table("TematicArea", Schema = "Modality")]
    public class TematicArea
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? UpdatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public string? UpdatedBy { get; set; } = string.Empty;

        public virtual ICollection<TematicAreaCareer>? TematicAreaCareers { get; set; }

    }
}
