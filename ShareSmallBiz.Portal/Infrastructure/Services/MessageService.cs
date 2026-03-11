using Microsoft.EntityFrameworkCore;
using ShareSmallBiz.Portal.Data;
using ShareSmallBiz.Portal.Data.Entities;

namespace ShareSmallBiz.Portal.Infrastructure.Services;

public class MessageService(
    ShareSmallBizUserContext context,
    ILogger<MessageService> logger,
    NotificationService notificationService)
{
    /// <summary>
    /// Get all distinct conversations for a user, with the most recent message and unread count.
    /// </summary>
    public async Task<List<ConversationModel>> GetConversationsAsync(string userId)
    {
        try
        {
            // Get all conversations this user participates in
            var messages = await context.DirectMessages
                .AsNoTracking()
                .Where(dm => dm.SenderId == userId || dm.RecipientId == userId)
                .OrderByDescending(dm => dm.CreatedDate)
                .ToListAsync();

            // Group by conversationId and project to ConversationModel
            var conversations = messages
                .GroupBy(dm => dm.ConversationId)
                .Select(g =>
                {
                    var latest = g.First();
                    var otherUserId = latest.SenderId == userId ? latest.RecipientId : latest.SenderId;
                    return new ConversationModel
                    {
                        ConversationId = g.Key,
                        OtherUserId = otherUserId,
                        LastMessage = latest.Content,
                        LastMessageDate = latest.CreatedDate,
                        UnreadCount = g.Count(m => m.RecipientId == userId && !m.IsRead)
                    };
                })
                .OrderByDescending(c => c.LastMessageDate)
                .ToList();

            // Populate other user display names in one query
            var otherUserIds = conversations.Select(c => c.OtherUserId).Distinct().ToList();
            var users = await context.Users
                .AsNoTracking()
                .Where(u => otherUserIds.Contains(u.Id))
                .Select(u => new { u.Id, u.DisplayName, u.ProfilePictureUrl, u.Slug })
                .ToDictionaryAsync(u => u.Id);

            foreach (var conv in conversations)
            {
                if (users.TryGetValue(conv.OtherUserId, out var user))
                {
                    conv.OtherUserDisplayName = user.DisplayName;
                    conv.OtherUserProfilePictureUrl = user.ProfilePictureUrl;
                    conv.OtherUserSlug = user.Slug;
                }
            }

            return conversations;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving conversations for user {UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// Get paginated messages for a specific conversation. Marks messages as read.
    /// </summary>
    public async Task<List<MessageModel>> GetMessagesAsync(
        string conversationId, string userId, int pageSize = 30, int pageNumber = 1)
    {
        try
        {
            // Verify user is a participant (conversationId encodes both user IDs)
            var messages = await context.DirectMessages
                .AsNoTracking()
                .Where(dm => dm.ConversationId == conversationId
                    && (dm.SenderId == userId || dm.RecipientId == userId))
                .OrderByDescending(dm => dm.CreatedDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(dm => new MessageModel
                {
                    Id = dm.Id,
                    SenderId = dm.SenderId,
                    Content = dm.Content,
                    SentDate = dm.CreatedDate,
                    IsRead = dm.IsRead
                })
                .ToListAsync();

            // Mark unread messages as read (messages sent to this user)
            var unreadIds = await context.DirectMessages
                .Where(dm => dm.ConversationId == conversationId
                    && dm.RecipientId == userId
                    && !dm.IsRead)
                .Select(dm => dm.Id)
                .ToListAsync();

            if (unreadIds.Count > 0)
            {
                var unread = await context.DirectMessages
                    .Where(dm => unreadIds.Contains(dm.Id))
                    .ToListAsync();
                foreach (var dm in unread)
                    dm.IsRead = true;
                await context.SaveChangesAsync();
            }

            return messages;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving messages for conversation {ConversationId}", conversationId);
            throw;
        }
    }

    /// <summary>
    /// Send a direct message from senderId to recipientId.
    /// </summary>
    public async Task<MessageModel?> SendMessageAsync(string senderId, string recipientId, string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return null;

        // Verify recipient exists
        var recipient = await context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == recipientId);

        if (recipient is null)
        {
            logger.LogWarning("Recipient {RecipientId} not found", recipientId);
            return null;
        }

        try
        {
            var message = new DirectMessage
            {
                SenderId = senderId,
                RecipientId = recipientId,
                Content = content.Trim(),
                ConversationId = DirectMessage.BuildConversationId(senderId, recipientId),
                IsRead = false
            };

            context.DirectMessages.Add(message);
            await context.SaveChangesAsync();

            // Notify recipient
            await notificationService.CreateAsync(
                recipientUserId: recipientId,
                type: "message",
                message: "You have a new direct message.",
                actorId: senderId,
                targetType: "conversation");

            logger.LogInformation("Message sent from {SenderId} to {RecipientId}", senderId, recipientId);

            return new MessageModel
            {
                Id = message.Id,
                SenderId = message.SenderId,
                Content = message.Content,
                SentDate = message.CreatedDate,
                IsRead = false
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error sending message from {SenderId} to {RecipientId}", senderId, recipientId);
            throw;
        }
    }
}

public class ConversationModel
{
    public string ConversationId { get; set; } = string.Empty;
    public string OtherUserId { get; set; } = string.Empty;
    public string? OtherUserDisplayName { get; set; }
    public string? OtherUserProfilePictureUrl { get; set; }
    public string? OtherUserSlug { get; set; }
    public string LastMessage { get; set; } = string.Empty;
    public DateTime LastMessageDate { get; set; }
    public int UnreadCount { get; set; }
}

public class MessageModel
{
    public int Id { get; set; }
    public string SenderId { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime SentDate { get; set; }
    public bool IsRead { get; set; }
}
