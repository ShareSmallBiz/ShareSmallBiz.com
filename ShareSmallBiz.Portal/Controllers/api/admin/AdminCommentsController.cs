using Microsoft.AspNetCore.Authentication.JwtBearer;
using ShareSmallBiz.Portal.Infrastructure.Services;

namespace ShareSmallBiz.Portal.Controllers.api.admin;

[ApiController]
[Route("api/admin/comments")]
[Authorize(Roles = "Admin", AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class AdminCommentsController(
    AdminCommentService adminCommentService,
    ILogger<AdminCommentsController> logger) : ControllerBase
{
    /// <summary>GET /api/admin/comments — all comments for moderation</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var comments = await adminCommentService.GetAllCommentsAsync();
        return Ok(comments);
    }

    /// <summary>GET /api/admin/comments/{id}</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var comment = await adminCommentService.GetCommentByIdAsync(id);
        return comment is null ? NotFound() : Ok(comment);
    }

    /// <summary>PUT /api/admin/comments/{id} — edit any comment (bypasses ownership)</summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] AdminEditCommentRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Content))
            return BadRequest(new { Message = "Content is required." });

        var result = await adminCommentService.UpdateCommentAsync(id, request.Content);
        if (!result)
        {
            logger.LogWarning("Admin comment update failed for id {CommentId}", id);
            return NotFound();
        }

        return NoContent();
    }

    /// <summary>DELETE /api/admin/comments/{id} — delete any comment (bypasses ownership)</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await adminCommentService.DeleteCommentAsync(id);
        if (!result)
        {
            logger.LogWarning("Admin comment delete failed for id {CommentId}", id);
            return NotFound();
        }

        logger.LogInformation("Admin deleted comment {CommentId}", id);
        return NoContent();
    }
}

public class AdminEditCommentRequest
{
    public string Content { get; set; } = string.Empty;
}
