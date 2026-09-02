using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ADMISION.ENTITIES.Models.Users
{
    [Table("Rols", Schema = "Users")]
    public class Rols
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool State { get; set; } = true;
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? UpdatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public string? UpdatedBy { get; set; } = string.Empty;

        public virtual ICollection<UserRol>? UserRols { get; set; }

    }
}
