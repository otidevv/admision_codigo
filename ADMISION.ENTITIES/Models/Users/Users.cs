using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ADMISION.ENTITIES.Models.Users
{
    [Table("Users", Schema = "Users")]
    public class Users
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string FirstNameFather { get; set; } = string.Empty;
        public string FirstNameMother { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Document { get; set; } = string.Empty;
        public string DocumentType { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? UserName { get; set; }
        public string? Password { get; set; }
        public string Genero { get; set; } = string.Empty;
        public string? CivilStatus { get; set; } = null;
        public string? Address { get; set; } = null;
        public string? PhotoUrl { get; set; } = null;
        public string? IsDisabled { get; set; } = null;
        public int TokenVersion { get; set; } = 0;
        public DateTimeOffset Birthdate { get; set; }
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? UpdatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public string? UpdatedBy { get; set; } = string.Empty;

        public virtual Models.Postulant.Postulant? Postulant { get; set; }
        public virtual ICollection<UserRol>? UserRols { get; set; }
        public virtual ICollection<Observations>? Observations { get; set; }
        public virtual ICollection<Teachers>? Teachers { get; set; }
    }
}
