using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace ADMISION.ENTITIES.Models.Ubigeo
{
    [Table("Distrit", Schema = "Ubigeo")]
    public class Distrit
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public Guid ProvinceId { get; set; }
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? UpdatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public string? UpdatedBy { get; set; }

        [ForeignKey("ProvinceId")]
        public virtual Provincie? Province { get; set; }
        public virtual ICollection<Models.Postulante.Inscription>? Inscriptions { get; set; }

    }
}
