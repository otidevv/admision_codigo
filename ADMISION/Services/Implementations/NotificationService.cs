using ADMISION.ENTITIES.Data;
using ADMISION.ENTITIES.Models.Notifications;
using ADMISION.Hubs;
using ADMISION.Services.Interfaces;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace ADMISION.Services.Implementations
{
    public class NotificationService : INotificationService
    {
        private readonly AppDbContext _context;
        private readonly IHubContext<NotificationHub> _hub;

        public NotificationService(AppDbContext context, IHubContext<NotificationHub> hub)
        {
            _context = context;
            _hub = hub;
        }

        public async Task CreateInscriptionNotificationAsync(Guid inscriptionId)
        {
            var info = await _context.Inscriptions.AsNoTracking()
                .Where(i => i.Id == inscriptionId)
                .Select(i => new
                {
                    i.Id,
                    i.PostulantId,
                    i.CodePostulant,
                    FullName = i.Postulant != null && i.Postulant.User != null ? i.Postulant.User.FullName : null,
                    ModalityName = i.Modality != null ? i.Modality.Name : null,
                    TypeModalityName = i.TypeModality != null ? i.TypeModality.Name : null
                })
                .FirstOrDefaultAsync();

            if (info == null) return;

            var modalityText = !string.IsNullOrWhiteSpace(info.TypeModalityName)
                ? $"{info.ModalityName} · {info.TypeModalityName}"
                : info.ModalityName ?? "—";

            var notif = new Notification
            {
                Id = Guid.NewGuid(),
                Type = "Inscription",
                Title = "Nueva inscripción registrada",
                Message = $"{info.FullName ?? "Postulante sin nombre"} se inscribió en {modalityText}.",
                // La ruta de resumen espera el Id del postulante, no el de la inscripción.
                ActionUrl = $"/admin/info-postulant/postulant/postulant-resum/{info.PostulantId}",
                EntityType = "Inscription",
                EntityId = info.Id,
                IconClass = "ti ti-user-plus",
                ColorScheme = "primary",
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedBy = "system"
            };

            _context.Notifications.Add(notif);
            await _context.SaveChangesAsync();

            // Broadcast
            var dto = new NotificationDto
            {
                Id = notif.Id,
                Type = notif.Type,
                Title = notif.Title,
                Message = notif.Message,
                ActionUrl = notif.ActionUrl,
                IconClass = notif.IconClass,
                ColorScheme = notif.ColorScheme,
                CreatedAt = notif.CreatedAt,
                IsRead = false,
                RelativeTime = "ahora"
            };

            try
            {
                await _hub.Clients.Group(NotificationHub.GroupAdmins)
                    .SendAsync("NotificationReceived", dto);
            }
            catch
            {
                // Si SignalR falla, la notificación queda persistida; el cliente la verá al refrescar.
            }
        }

        public async Task<List<NotificationDto>> GetForUserAsync(Guid userId, int take = 20)
        {
            var rows = await _context.Notifications.AsNoTracking()
                .OrderByDescending(n => n.CreatedAt)
                .Take(take)
                .Select(n => new
                {
                    n.Id,
                    n.Type,
                    n.Title,
                    n.Message,
                    n.ActionUrl,
                    n.IconClass,
                    n.ColorScheme,
                    n.CreatedAt,
                    Viewed = _context.NotificationViews.Any(v => v.NotificationId == n.Id && v.UserId == userId)
                })
                .ToListAsync();

            return rows.Select(r => new NotificationDto
            {
                Id = r.Id,
                Type = r.Type,
                Title = r.Title,
                Message = r.Message,
                ActionUrl = r.ActionUrl,
                IconClass = r.IconClass,
                ColorScheme = r.ColorScheme,
                CreatedAt = r.CreatedAt,
                IsRead = r.Viewed,
                RelativeTime = Humanize(r.CreatedAt)
            }).ToList();
        }

        public async Task<int> CountUnreadAsync(Guid userId)
        {
            return await _context.Notifications
                .Where(n => !_context.NotificationViews.Any(v => v.NotificationId == n.Id && v.UserId == userId))
                .CountAsync();
        }

        public async Task MarkAsViewedAsync(Guid notificationId, Guid userId)
        {
            var exists = await _context.NotificationViews
                .AnyAsync(v => v.NotificationId == notificationId && v.UserId == userId);
            if (exists) return;

            _context.NotificationViews.Add(new NotificationView
            {
                Id = Guid.NewGuid(),
                NotificationId = notificationId,
                UserId = userId,
                ViewedAt = DateTimeOffset.UtcNow
            });
            await _context.SaveChangesAsync();
        }

        public async Task<int> MarkAllAsViewedAsync(Guid userId)
        {
            var unread = await _context.Notifications
                .Where(n => !_context.NotificationViews.Any(v => v.NotificationId == n.Id && v.UserId == userId))
                .Select(n => n.Id)
                .ToListAsync();

            if (unread.Count == 0) return 0;

            var now = DateTimeOffset.UtcNow;
            var newViews = unread.Select(nid => new NotificationView
            {
                Id = Guid.NewGuid(),
                NotificationId = nid,
                UserId = userId,
                ViewedAt = now
            });
            _context.NotificationViews.AddRange(newViews);
            await _context.SaveChangesAsync();
            return unread.Count;
        }

        private static string Humanize(DateTimeOffset t)
        {
            var diff = DateTimeOffset.UtcNow - t;
            if (diff.TotalSeconds < 60) return "hace unos segundos";
            if (diff.TotalMinutes < 60) return $"hace {(int)diff.TotalMinutes} min";
            if (diff.TotalHours < 24) return $"hace {(int)diff.TotalHours} h";
            if (diff.TotalDays < 7) return $"hace {(int)diff.TotalDays} d";
            return t.ToLocalTime().ToString("dd MMM yyyy HH:mm", new System.Globalization.CultureInfo("es-PE"));
        }
    }
}
