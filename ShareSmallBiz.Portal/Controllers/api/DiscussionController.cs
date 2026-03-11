using ShareSmallBiz.Portal.Infrastructure.Services;
using System.Security.Claims;

namespace ShareSmallBiz.Portal.Controllers.api;


[ApiController]
[Route("api/[controller]")]
public class DiscussionController(DiscussionProvider discussionProvider) : ControllerBase
{

    [HttpGet]
    public async Task<ActionResult<IEnumerable<DiscussionModel>>> GetAllDiscussions(bool onlyPublic = true)
    {
        var posts = await discussionProvider.GetAllDiscussionsAsync(onlyPublic);
        return Ok(posts);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<DiscussionModel>> GetPostById(int id)
    {
        var post = await discussionProvider.GetPostByIdAsync(id);
        if (post == null)
        {
            return NotFound();
        }
        return Ok(post);
    }

    [HttpGet("paged")]
    public async Task<ActionResult<PaginatedPostResult>> GetPosts(int pageNumber, int pageSize, SortType sortType)
    {
        var result = await discussionProvider.GetDiscussionsAsync(pageNumber, pageSize, sortType);
        return Ok(result);
    }

    [HttpGet("featured/{count}")]
    public async Task<ActionResult<List<DiscussionModel>>> GetFeaturedPosts(int count)
    {
        var posts = await discussionProvider.FeaturedPostsAsync(count);
        return Ok(posts);
    }

    [HttpGet("most-commented/{count}")]
    public async Task<ActionResult<List<DiscussionModel>>> GetMostCommentedPosts(int count)
    {
        var posts = await discussionProvider.MostCommentedPostsAsync(count);
        return Ok(posts);
    }

    [HttpGet("most-popular/{count}")]
    public async Task<ActionResult<List<DiscussionModel>>> GetMostPopularPosts(int count)
    {
        var posts = await discussionProvider.MostPopularPostsAsync(count);
        return Ok(posts);
    }

    [HttpGet("most-recent/{count}")]
    public async Task<ActionResult<List<DiscussionModel>>> GetMostRecentPosts(int count)
    {
        var posts = await discussionProvider.MostRecentPostsAsync(count);
        return Ok(posts);
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<DiscussionModel>> CreatePost(DiscussionModel discussionModel)
    {
        var userPrincipal = HttpContext.User;
        var post = await discussionProvider.CreateDiscussionAsync(discussionModel, userPrincipal);
        if (post == null)
        {
            return BadRequest("Unable to create post.");
        }
        return CreatedAtAction(nameof(GetPostById), new { id = post.Id }, post);
    }

    [HttpPut("{id}")]
    [Authorize]
    public async Task<IActionResult> UpdatePost(int id, DiscussionModel discussionModel)
    {
        if (id != discussionModel.Id)
        {
            return BadRequest();
        }

        var userPrincipal = HttpContext.User;
        var result = await discussionProvider.UpdatePostAsync(discussionModel, userPrincipal);
        if (!result)
        {
            return NotFound();
        }
        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize]
    public async Task<IActionResult> DeletePost(int id)
    {
        var result = await discussionProvider.DeleteDiscussionAsync(id);
        if (!result)
        {
            return NotFound();
        }
        return NoContent();
    }

    [HttpPost("{id}/comment")]
    [Authorize]
    public async Task<IActionResult> CommentPost(int id, [FromBody] string comment)
    {
        var userPrincipal = HttpContext.User;
        var result = await discussionProvider.DiscussionCommentPostAsync(id, comment, userPrincipal);
        if (!result)
        {
            return BadRequest("Unable to add comment.");
        }
        return NoContent();
    }

    [HttpPost("{id}/like")]
    [Authorize]
    public async Task<IActionResult> LikePost(int id)
    {
        var userPrincipal = HttpContext.User;
        var result = await discussionProvider.LikePostAsync(id, userPrincipal);
        if (!result)
        {
            return BadRequest("Unable to like post.");
        }
        return NoContent();
    }

    /// <summary>POST /api/discussion/{id}/save — toggle save/bookmark (auth required)</summary>
    [HttpPost("{id}/save")]
    [Authorize]
    public async Task<IActionResult> SavePost(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var saved = await discussionProvider.SaveDiscussionAsync(id, userId);
        return Ok(new { saved, message = saved ? "Discussion saved." : "Discussion unsaved." });
    }

    /// <summary>GET /api/discussion/saved — get saved discussions for current user (auth required)</summary>
    [HttpGet("saved")]
    [Authorize]
    public async Task<IActionResult> GetSaved()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var posts = await discussionProvider.GetSavedDiscussionsAsync(userId);
        return Ok(posts);
    }

    /// <summary>POST /api/discussion/{id}/share — record a share, increments counter (auth required)</summary>
    [HttpPost("{id}/share")]
    [Authorize]
    public async Task<IActionResult> SharePost(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var result = await discussionProvider.ShareDiscussionAsync(id, userId);
        return result ? Ok(new { message = "Share recorded." }) : NotFound(new { message = "Discussion not found." });
    }
}