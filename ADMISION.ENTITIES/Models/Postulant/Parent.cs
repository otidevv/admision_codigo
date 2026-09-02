using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace ADMISION.ENTITIES.Models.Postulant
{
    [Table("Parent", Schema = "Postulant")]
    public class Parent
    {
        public Guid Id { get; set; }
        [ForeignKey("Postulant")]
        public Guid PostulantId { get; set; }
        [ForeignKey("Inscription")]
        public Guid InscriptionId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string FirstNameFather { get; set; } = string.Empty;
        public string FirstNameMother { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string TypeDocument { get; set; } = string.Empty;
        public string NumberDocument { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string? Email { get; set; }
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? UpdatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public string? UpdatedBy { get; set; } = string.Empty;

        public virtual Postulant? Postulant { get; set; }
        public virtual Postulante.Inscription? Inscription { get; set; }
    }
}
