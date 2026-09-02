namespace ADMISION.Services.Interfaces
{
    public class NotificationDto
    {
        public Guid Id { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? ActionUrl { get; set; }
        public string? IconClass { get; set; }
        public string? ColorScheme { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public bool IsRead { get; set; }
        public string RelativeTime { get; set; } = string.Empty;
    }

    public interface INotificationService
    {
        /// <summary>Crea la notificación de "nueva inscripción" y la difunde por SignalR.</summary>
        Task CreateInscriptionNotificationAsync(Guid inscriptionId);

        /// <summary>Notificaciones para mostrar en la campana del admin. IsRead indica si el usuario ya la vio.</summary>
        Task<List<NotificationDto>> GetForUserAsync(Guid userId, int take = 20);

        /// <summary>Cantidad de notificaciones no leídas para el usuario.</summary>
        Task<int> CountUnreadAsync(Guid userId);

        /// <summary>Marca como vista (inserta fila en NotificationView si no existe).</summary>
        Task MarkAsViewedAsync(Guid notificationId, Guid userId);

        /// <summary>Marca todas las notificaciones no vistas como vistas.</summary>
        Task<int> MarkAllAsViewedAsync(Guid userId);
    }
}
