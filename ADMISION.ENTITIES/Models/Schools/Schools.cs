using ADMISION.ENTITIES.Models.Ubigeo;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace ADMISION.ENTITIES.Models.Schools
{
    [Table("Schools", Schema = "Schools")]
    public class Schools
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string? UgelName { get; set; }
        public string? Modality { get; set; }
        public string? Level { get; set; }
        public string? Management { get; set; }
        public string? Address { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
        public string? Website { get; set; }
        public bool IsActive { get; set; } = true;
        public string? Director { get; set; }
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? UpdatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public string? UpdatedBy { get; set; }
        public Guid? DistritId { get; set; }
        [ForeignKey("DistritId")]
        public virtual Distrit? Distrit { get; set; }

        public virtual ICollection<Models.Postulante.Inscription>? Inscriptions { get; set; }

    }
}
