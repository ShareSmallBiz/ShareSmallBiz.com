using ShareSmallBiz.Portal.Infrastructure.Services;

namespace ShareSmallBiz.Portal.Areas.Forum.Controllers;

[Area("Forum")]
[Route("[controller]/[action]")]
public class CommentsController : ForumBaseController
{
    private readonly CommentProvider _commentProvider;
    private readonly ILogger<CommentsController> _logger;

    public CommentsController(CommentProvider commentProvider, ILogger<CommentsController> logger)
    {
        _commentProvider = commentProvider;
        _logger = logger;
    }

    // 1. Get Comments (returns JSON)
    [HttpGet]
    public async Task<IActionResult> GetComments(int postId)
    {
        var comments = await _commentProvider.GetCommentsForPostAsync(postId);

        // Convert to model
        var commentModels = comments
            .Select(c => new PostCommentModel(c))
            .ToList();

        return Json(commentModels);
    }

    // 2. Add Comment
    [Authorize]
    [HttpPost]
    public async Task<IActionResult> AddComment(int postId, string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return BadRequest("Content cannot be empty.");
        }

        var success = await _commentProvider.AddCommentAsync(postId, content, User);
        if (!success)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                              "Unable to add the comment. Check logs for more details.");
        }

        return Ok("Comment added successfully.");
    }

    // 3. Edit Comment
    [Authorize]
    [HttpPost]
    public async Task<IActionResult> EditComment(int commentId, string updatedContent)
    {
        if (string.IsNullOrWhiteSpace(updatedContent))
        {
            return BadRequest("Updated content cannot be empty.");
        }

        var success = await _commentProvider.UpdateCommentAsync(commentId, updatedContent, User);
        if (!success)
        {
            return Forbid("You do not have permission to edit this comment, or the comment does not exist.");
        }

        return Ok("Comment updated successfully.");
    }

    // 4. Delete Comment
    [Authorize]
    [HttpPost]
    public async Task<IActionResult> DeleteComment(int commentId)
    {
        var success = await _commentProvider.DeleteCommentAsync(commentId, User);
        if (!success)
        {
            return Forbid("You do not have permission to delete this comment, or the comment does not exist.");
        }

        return Ok("Comment deleted successfully.");
    }

    // 5. Render Partial View
    //    This returns a partial that can display (and optionally manage) comments for a given PostId.
    [HttpGet]
    public IActionResult CommentsPartial(int postId)
    {
        // You could load a list of comments here, or you can retrieve them via AJAX. 
        // For simplicity, we’re just passing the PostId to the partial.
        return PartialView("_CommentsPartial", postId);
    }

}
