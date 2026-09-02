using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using ADMISION.ENTITIES.Models.Postulante;

namespace ADMISION.ENTITIES.Models.Postulant
{
    [Table("Postulant", Schema = "Postulant")]
    public class Postulant
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public bool IsDisabled { get; set; }
        public DateTimeOffset? DisableDate { get; set; }
        public string? DisableReason { get; set; }
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? UpdatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public string? UpdatedBy { get; set; } = string.Empty;
        public string? ConadisNumber { get; set; }

        public virtual Users.Users? User { get; set; }
        public virtual ICollection<Inscription>? Inscriptions { get; set; }
        public virtual ICollection<PostulantDisability>? Disabilities { get; set; }
        public virtual ICollection<Biometrics.PostulantPhoto>? Photos { get; set; }
        public virtual ICollection<Annulment>? Annulments { get; set; }

    }
}