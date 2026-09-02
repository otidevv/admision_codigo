using System.ComponentModel.DataAnnotations.Schema;

namespace ADMISION.ENTITIES.Models.Api
{
    [Table("ApiRequestLogs", Schema = "Api")]
    public class ApiRequestLog
    {
        public Guid Id { get; set; }
        public Guid? UserId { get; set; }
        public string? JwtId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string HttpMethod { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public string? QueryString { get; set; }
        public int StatusCode { get; set; }
        public string IpAddress { get; set; } = string.Empty;
        public string? Origin { get; set; }
        public string? UserAgent { get; set; }
        public int DurationMs { get; set; }
        public DateTimeOffset RequestedAt { get; set; } = DateTimeOffset.UtcNow;

        [ForeignKey("UserId")]
        public virtual Users.Users? User { get; set; }
    }
}
