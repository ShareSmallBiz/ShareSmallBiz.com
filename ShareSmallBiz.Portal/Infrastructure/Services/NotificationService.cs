using Microsoft.EntityFrameworkCore;
using ShareSmallBiz.Portal.Data;
using ShareSmallBiz.Portal.Data.Entities;

namespace ShareSmallBiz.Portal.Infrastructure.Services;

public class NotificationService(
    ShareSmallBizUserContext context,
    ILogger<NotificationService> logger)
{
    /// <summary>
    /// Get notifications for a user with optional unread filter and pagination.
    /// </summary>
    public async Task<List<NotificationModel>> GetForUserAsync(
        string userId, bool unreadOnly = false, int pageSize = 20, int pageNumber = 1)
    {
        try
        {
            var query = context.Notifications
                .AsNoTracking()
                .Where(n => n.UserId == userId);

            if (unreadOnly)
                query = query.Where(n => !n.IsRead);

            var notifications = await query
                .OrderByDescending(n => n.CreatedDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(n => new NotificationModel
                {
                    Id = n.Id,
                    Type = n.Type,
                    Message = n.Message,
                    IsRead = n.IsRead,
                    CreatedDate = n.CreatedDate,
                    TargetId = n.TargetId,
                    TargetType = n.TargetType,
                    ActorId = n.ActorId
                })
                .ToListAsync();

            return notifications;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving notifications for user {UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// Get unread notification count for a user.
    /// </summary>
    public async Task<int> GetUnreadCountAsync(string userId)
    {
        try
        {
            return await context.Notifications
                .CountAsync(n => n.UserId == userId && !n.IsRead);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting unread count for user {UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// Mark a single notification as read. Returns false if not found or not owned by user.
    /// </summary>
    public async Task<bool> MarkAsReadAsync(int notificationId, string userId)
    {
        try
        {
            var notification = await context.Notifications
                .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);

            if (notification is null)
                return false;

            notification.IsRead = true;
            await context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error marking notification {Id} as read", notificationId);
            throw;
        }
    }

    /// <summary>
    /// Mark all notifications for a user as read.
    /// </summary>
    public async Task<int> MarkAllAsReadAsync(string userId)
    {
        try
        {
            var unread = await context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();

            foreach (var n in unread)
                n.IsRead = true;

            await context.SaveChangesAsync();
            logger.LogInformation("Marked {Count} notifications as read for user {UserId}", unread.Count, userId);
            return unread.Count;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error marking all notifications as read for user {UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// Create a new notification. Safe to call fire-and-forget style — errors are logged, not thrown.
    /// </summary>
    public async Task CreateAsync(
        string recipientUserId,
        string type,
        string message,
        string? actorId = null,
        int? targetId = null,
        string? targetType = null)
    {
        // Don't notify users about their own actions
        if (recipientUserId == actorId)
            return;

        try
        {
            var notification = new Notification
            {
                UserId = recipientUserId,
                ActorId = actorId,
                Type = type,
                Message = message,
                TargetId = targetId,
                TargetType = targetType,
                IsRead = false
            };

            context.Notifications.Add(notification);
            await context.SaveChangesAsync();
            logger.LogDebug("Created {Type} notification for user {UserId}", type, recipientUserId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating {Type} notification for user {UserId}", type, recipientUserId);
            // Swallow — notification failure should not break primary operation
        }
    }
}

public class NotificationModel
{
    public int Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime CreatedDate { get; set; }
    public int? TargetId { get; set; }
    public string? TargetType { get; set; }
    public string? ActorId { get; set; }
}
