using ShareSmallBiz.Portal.Infrastructure.Services;
using System.Security.Claims;

namespace ShareSmallBiz.Portal.Controllers.api;

[Route("api/messages")]
public class DirectMessagesController(
    MessageService messageService,
    ILogger<DirectMessagesController> logger) : ApiControllerBase
{
    /// <summary>GET /api/messages/conversations — list conversations for the authenticated user</summary>
    [HttpGet("conversations")]
    public async Task<IActionResult> GetConversations()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        try
        {
            var conversations = await messageService.GetConversationsAsync(userId);
            return Ok(conversations);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving conversations for user {UserId}", userId);
            return StatusCode(500, new { message = "Error retrieving conversations" });
        }
    }

    /// <summary>GET /api/messages/conversations/{conversationId}?pageSize=30&pageNumber=1</summary>
    [HttpGet("conversations/{conversationId}")]
    public async Task<IActionResult> GetMessages(
        string conversationId,
        [FromQuery] int pageSize = 30,
        [FromQuery] int pageNumber = 1)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        pageSize = Math.Clamp(pageSize, 1, 100);
        pageNumber = Math.Max(1, pageNumber);

        try
        {
            var messages = await messageService.GetMessagesAsync(conversationId, userId, pageSize, pageNumber);
            return Ok(messages);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving messages for conversation {ConversationId}", conversationId);
            return StatusCode(500, new { message = "Error retrieving messages" });
        }
    }

    /// <summary>POST /api/messages — send a direct message</summary>
    [HttpPost]
    public async Task<IActionResult> Send([FromBody] SendMessageRequest request)
    {
        var senderId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(senderId))
            return Unauthorized();

        if (string.IsNullOrWhiteSpace(request.RecipientId))
            return BadRequest(new { message = "RecipientId is required." });

        if (string.IsNullOrWhiteSpace(request.Content))
            return BadRequest(new { message = "Content is required." });

        if (senderId == request.RecipientId)
            return BadRequest(new { message = "Cannot send a message to yourself." });

        try
        {
            var message = await messageService.SendMessageAsync(senderId, request.RecipientId, request.Content);
            if (message is null)
                return NotFound(new { message = "Recipient not found." });

            return Ok(message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error sending message from {SenderId} to {RecipientId}", senderId, request.RecipientId);
            return StatusCode(500, new { message = "Error sending message" });
        }
    }
}

public class SendMessageRequest
{
    public string RecipientId { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}
