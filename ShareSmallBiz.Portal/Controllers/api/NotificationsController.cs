using ShareSmallBiz.Portal.Infrastructure.Services;
using System.Security.Claims;

namespace ShareSmallBiz.Portal.Controllers.api;

[Route("api/notifications")]
public class NotificationsController(
    NotificationService notificationService,
    ILogger<NotificationsController> logger) : ApiControllerBase
{
    /// <summary>GET /api/notifications?unreadOnly=false&pageSize=20&pageNumber=1</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] bool unreadOnly = false,
        [FromQuery] int pageSize = 20,
        [FromQuery] int pageNumber = 1)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        pageSize = Math.Clamp(pageSize, 1, 100);
        pageNumber = Math.Max(1, pageNumber);

        try
        {
            var notifications = await notificationService.GetForUserAsync(userId, unreadOnly, pageSize, pageNumber);
            var unreadCount = await notificationService.GetUnreadCountAsync(userId);
            return Ok(new { notifications, unreadCount });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving notifications for user {UserId}", userId);
            return StatusCode(500, new { message = "Error retrieving notifications" });
        }
    }

    /// <summary>PUT /api/notifications/{id}/read — mark one notification as read</summary>
    [HttpPut("{id:int}/read")]
    public async Task<IActionResult> MarkRead(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        try
        {
            var success = await notificationService.MarkAsReadAsync(id, userId);
            return success ? NoContent() : NotFound(new { message = "Notification not found." });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error marking notification {Id} as read", id);
            return StatusCode(500, new { message = "Error updating notification" });
        }
    }

    /// <summary>POST /api/notifications/read-all — mark all notifications as read</summary>
    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllRead()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        try
        {
            var count = await notificationService.MarkAllAsReadAsync(userId);
            return Ok(new { markedRead = count });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error marking all notifications as read for user {UserId}", userId);
            return StatusCode(500, new { message = "Error updating notifications" });
        }
    }
}
