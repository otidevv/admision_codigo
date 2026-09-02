using ADMISION.ENTITIES.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace ADMISION.Hubs
{
    /// <summary>
    /// Hub para notificaciones en tiempo real hacia los paneles administrativos.
    /// Solo aceptan conexión los usuarios autenticados con los roles permitidos.
    /// </summary>
    [Authorize(Roles = AppConstants.Roles.SuperAdmin + "," + AppConstants.Roles.Admin + "," + AppConstants.Roles.Soporte + "," + AppConstants.Roles.Consultor)]
    public class NotificationHub : Hub
    {
        public const string GroupAdmins = "admins";

        public override async Task OnConnectedAsync()
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, GroupAdmins);
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupAdmins);
            await base.OnDisconnectedAsync(exception);
        }
    }
}
