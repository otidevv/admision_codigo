using System.ComponentModel.DataAnnotations.Schema;

namespace ADMISION.ENTITIES.Models.Users
{
    [Table("UserRol", Schema = "Users")]
    public class UserRol
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid RolsId { get; set; }
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public string CreatedBy { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        public virtual Users? User { get; set; }
        [ForeignKey("RolsId")]
        public virtual Rols? Rol { get; set; }

    }
}
