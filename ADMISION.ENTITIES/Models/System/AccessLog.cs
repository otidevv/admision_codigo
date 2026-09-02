using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace ADMISION.ENTITIES.Models.System
{
    [Table("AccessLog", Schema = "System")]
    public class AccessLog
    {
        public Guid Id { get; set; }
        public string? UserId { get; set; } // Nullable because login might fail or be anonymous
        public string UserName { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty; // e.g., "Login", "Logout", "Failed Login"
        public string? RequestPath { get; set; }
        public string Status { get; set; } = string.Empty; // "Success", "Failure"
        public int? ResponseCode { get; set; }
        public string? Details { get; set; } // Error message or other info
        public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    }
}
