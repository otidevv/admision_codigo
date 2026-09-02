using System.ComponentModel.DataAnnotations.Schema;

namespace ADMISION.ENTITIES.Models.Notifications
{
    /// <summary>
    /// Registra que un usuario ya visualizó una notificación, con la fecha de visualización.
    /// Hay a lo sumo una fila por (NotificationId, UserId).
    /// </summary>
    [Table("NotificationView", Schema = "Notifications")]
    public class NotificationView
    {
        public Guid Id { get; set; }

        public Guid NotificationId { get; set; }
        public Guid UserId { get; set; }

        public DateTimeOffset ViewedAt { get; set; } = DateTimeOffset.UtcNow;

        [ForeignKey("NotificationId")]
        public virtual Notification? Notification { get; set; }

        [ForeignKey("UserId")]
        public virtual ADMISION.ENTITIES.Models.Users.Users? User { get; set; }
    }
}
