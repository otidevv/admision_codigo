namespace ADMISION.Models.ViewModels.Admin
{
    public class UserProfileDetailViewModel
    {
        public Guid Id { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Document { get; set; } = string.Empty;
        public string DocumentType { get; set; } = string.Empty;
        public string? PhotoUrl { get; set; }
        public string Status { get; set; } = string.Empty; // Activo / Bloqueado
        public List<string> Roles { get; set; } = new();
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? LastLogin { get; set; }
        public string? LastLoginIp { get; set; }

        public int TotalAccessSuccess { get; set; }
        public int TotalAccessFailure { get; set; }
        public int TotalNotificationsViewed { get; set; }

        public List<AccessLogItem> RecentAccess { get; set; } = new();
        public List<NotificationViewedItem> NotificationsViewed { get; set; } = new();

        // Datos para gráficos
        public int SelectedYear { get; set; }
        public List<int> AvailableYears { get; set; } = new();
        public List<int> LoginsByMonth { get; set; } = new(new int[12]); // índice 0 = enero
        public int SelectedMonth { get; set; }
        public int DaysInSelectedMonth { get; set; }
        public List<int> LoginsByDayOfSelectedMonth { get; set; } = new();
    }

    public class AccessLogItem
    {
        public DateTimeOffset Timestamp { get; set; }
        public string Action { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? IpAddress { get; set; }
        public string? Details { get; set; }
        public int? ResponseCode { get; set; }
    }

    public class NotificationViewedItem
    {
        public Guid NotificationId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? IconClass { get; set; }
        public string? ColorScheme { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset ViewedAt { get; set; }
    }
}
