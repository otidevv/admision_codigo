using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace ADMISION.ENTITIES.Models.Postulant
{
    [Table("Observations", Schema = "Postulant")]
    public class Observations
    {
        public Guid Id { get; set; }
        [ForeignKey("Inscription")]
        public Guid InscriptionId { get; set; }
        public string Observation { get; set; } = string.Empty;
        public string? TipoObservacion { get; set; }
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? UpdatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public string? UpdatedBy { get; set; } = string.Empty;

        public virtual Postulante.Inscription? Inscription { get; set; }
    }
}
