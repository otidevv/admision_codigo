using ADMISION.ENTITIES.Constants;
using ADMISION.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ADMISION.Controllers.Admin
{
    [Authorize(Roles = AppConstants.Roles.SuperAdmin + "," + AppConstants.Roles.Admin + "," + AppConstants.Roles.Consultor)]
    [Route("admin/notifications")]
    public class NotificationsController : Controller
    {
        private readonly INotificationService _service;

        public NotificationsController(INotificationService service)
        {
            _service = service;
        }

        [HttpGet("list")]
        public async Task<IActionResult> List(int take = 20)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var items = await _service.GetForUserAsync(userId.Value, take);
            var unread = await _service.CountUnreadAsync(userId.Value);
            return Json(new { items, unread });
        }

        [HttpGet("unread-count")]
        public async Task<IActionResult> UnreadCount()
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();
            var unread = await _service.CountUnreadAsync(userId.Value);
            return Json(new { unread });
        }

        [HttpPost("mark-read/{id:guid}")]
        public async Task<IActionResult> MarkRead(Guid id)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();
            await _service.MarkAsViewedAsync(id, userId.Value);
            return Json(new { ok = true });
        }

        [HttpPost("mark-all-read")]
        public async Task<IActionResult> MarkAllRead()
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();
            var marked = await _service.MarkAllAsViewedAsync(userId.Value);
            return Json(new { ok = true, marked });
        }

        private Guid? GetUserId()
        {
            var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(raw, out var id) ? id : null;
        }
    }
}
