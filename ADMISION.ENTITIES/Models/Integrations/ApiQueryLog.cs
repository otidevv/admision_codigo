using System.ComponentModel.DataAnnotations.Schema;

namespace ADMISION.ENTITIES.Models.Integrations
{
    [Table("ApiQueryLog", Schema = "Integrations")]
    public class ApiQueryLog
    {
        public Guid Id { get; set; }

        public Guid ApiId { get; set; }
        [ForeignKey("ApiId")]
        public virtual ExternalApi? Api { get; set; }

        // Identidad del usuario que hizo la petición.
        public string? UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string? IpAddress { get; set; }

        // Parámetros usados (JSON). No se persisten secretos del request (header auth, etc).
        public string? RequestParametersJson { get; set; }

        public int ResponseStatus { get; set; }
        public bool ResponseSuccess { get; set; }
        // Recorte del body de respuesta para auditoría (limitado a ~8KB).
        public string? ResponseExcerpt { get; set; }
        public string? ErrorMessage { get; set; }
        public int DurationMs { get; set; }

        public DateTimeOffset QueriedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}
