using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace ADMISION.ENTITIES.Models.Infrastructure
{
    [Table("Clasroom", Schema = "Infrastructure")]
    public class Classroom
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Group { get; set; }
        public int Capacity { get; set; }
        public int Floor { get; set; } = 1;
        public bool IsActive { get; set; } = true;
        public Guid PavilionId { get; set; }
        [ForeignKey("PavilionId")]
        public virtual Pavilion? Pavilion { get; set; }
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? UpdatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public string? UpdatedBy { get; set; } = string.Empty;
    }
}
