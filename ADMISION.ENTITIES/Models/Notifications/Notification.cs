using System.ComponentModel.DataAnnotations.Schema;

namespace ADMISION.ENTITIES.Models.Notifications
{
    [Table("Notification", Schema = "Notifications")]
    public class Notification
    {
        public Guid Id { get; set; }

        /// <summary>Clasificador: "Inscription", "Payment", "System", etc.</summary>
        public string Type { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;

        /// <summary>URL interna a la que navegar al hacer click (opcional).</summary>
        public string? ActionUrl { get; set; }

        /// <summary>Entidad relacionada para auditoría (ej. "Inscription").</summary>
        public string? EntityType { get; set; }
        public Guid? EntityId { get; set; }

        /// <summary>Clase de icono FontAwesome (ej. "fa-solid fa-user-plus").</summary>
        public string? IconClass { get; set; }

        /// <summary>Paleta: "primary" | "secondary" | "success" | "warning" | "danger".</summary>
        public string? ColorScheme { get; set; }

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public string? CreatedBy { get; set; }

        public virtual ICollection<NotificationView>? Views { get; set; }
    }
}
