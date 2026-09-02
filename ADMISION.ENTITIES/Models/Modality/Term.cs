using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace ADMISION.ENTITIES.Models.Modality
{
    [Table("Terms", Schema = "Modality")]
    public class Term
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Number { get; set; }
        public string Year { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? UpdatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public string? UpdatedBy { get; set; } = string.Empty;

        public virtual ICollection<Modality>? Modalities { get; set; }


    }
}
