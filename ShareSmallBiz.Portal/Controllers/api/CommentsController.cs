using ShareSmallBiz.Portal.Infrastructure.Services;
using System.Security.Claims;

namespace ShareSmallBiz.Portal.Controllers.api;

[Route("api/comments")]
public class CommentsController(
    CommentProvider commentProvider,
    ILogger<CommentsController> logger) : ApiControllerBase
{
    /// <summary>GET /api/comments?postId=123 — list all comments for a post (public)</summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetByPost([FromQuery] int postId)
    {
        var comments = await commentProvider.GetCommentsForPostAsync(postId);
        var models = comments.Select(c => new PostCommentModel(c)).ToList();
        return Ok(models);
    }

    /// <summary>GET /api/comments/{id} — get a single comment</summary>
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(int id)
    {
        var comment = await commentProvider.GetCommentByIdAsync(id);
        return comment is null ? NotFound() : Ok(comment);
    }

    /// <summary>POST /api/comments — add a comment (auth required)</summary>
    [HttpPost]
    public async Task<IActionResult> Add([FromBody] AddCommentRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Content))
            return BadRequest(new { Message = "Content is required." });

        var result = await commentProvider.AddCommentAsync(request.PostId, request.Content, User);
        if (!result)
        {
            logger.LogWarning("Failed to add comment for postId {PostId}", request.PostId);
            return BadRequest(new { Message = "Unable to add comment." });
        }

        return Ok(new { Message = "Comment added." });
    }

    /// <summary>PUT /api/comments/{id} — edit own comment (auth required)</summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Edit(int id, [FromBody] EditCommentRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Content))
            return BadRequest(new { Message = "Content is required." });

        var result = await commentProvider.UpdateCommentAsync(id, request.Content, User);
        if (!result)
            return Forbid(); // ownership check failed or not found

        return NoContent();
    }

    /// <summary>DELETE /api/comments/{id} — delete own comment (auth required)</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await commentProvider.DeleteCommentAsync(id, User);
        if (!result)
            return Forbid(); // ownership check failed or not found

        return NoContent();
    }

    /// <summary>POST /api/comments/{id}/like — toggle like on comment (auth required)</summary>
    [HttpPost("{id:int}/like")]
    public async Task<IActionResult> Like(int id)
    {
        var userId = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var result = await commentProvider.LikeCommentAsync(id, userId);
        if (!result)
        {
            logger.LogWarning("Failed to like comment {CommentId}", id);
            return NotFound(new { Message = "Comment not found." });
        }

        return Ok(new { Message = "Like toggled successfully." });
    }
}

public class AddCommentRequest
{
    public int PostId { get; set; }
    public string Content { get; set; } = string.Empty;
}

public class EditCommentRequest
{
    public string Content { get; set; } = string.Empty;
}
