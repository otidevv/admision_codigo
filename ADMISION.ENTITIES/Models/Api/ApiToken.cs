using System.ComponentModel.DataAnnotations.Schema;

namespace ADMISION.ENTITIES.Models.Api
{
    [Table("ApiTokens", Schema = "Api")]
    public class ApiToken
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string JwtId { get; set; } = string.Empty;
        public DateTimeOffset IssuedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset ExpiresAt { get; set; }
        public bool IsRevoked { get; set; }
        public DateTimeOffset? RevokedAt { get; set; }
        public string? RevokedByIp { get; set; }
        public string CreatedByIp { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        public virtual Users.Users? User { get; set; }
    }
}
