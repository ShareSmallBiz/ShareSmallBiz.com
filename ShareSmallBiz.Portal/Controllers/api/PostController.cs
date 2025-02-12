using Microsoft.AspNetCore.Authentication.JwtBearer;
using ShareSmallBiz.Portal.Infrastructure.Services;

namespace ShareSmallBiz.Portal.Controllers.api;


[ApiController]
[Route("api/[controller]")]
public class PostController(DiscussionProvider postProvider) : ControllerBase
{

    [HttpGet]
    public async Task<ActionResult<IEnumerable<DiscussionModel>>> GetAllPosts(bool onlyPublic = true)
    {
        var posts = await postProvider.GetAllPostsAsync(onlyPublic);
        return Ok(posts);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<DiscussionModel>> GetPostById(int id)
    {
        var post = await postProvider.GetPostByIdAsync(id);
        if (post == null)
        {
            return NotFound();
        }
        return Ok(post);
    }

    [HttpGet("paged")]
    public async Task<ActionResult<PaginatedPostResult>> GetPosts(int pageNumber, int pageSize, SortType sortType)
    {
        var result = await postProvider.GetPostsAsync(pageNumber, pageSize, sortType);
        return Ok(result);
    }

    [HttpGet("featured/{count}")]
    public async Task<ActionResult<List<DiscussionModel>>> GetFeaturedPosts(int count)
    {
        var posts = await postProvider.FeaturedPostsAsync(count);
        return Ok(posts);
    }

    [HttpGet("most-commented/{count}")]
    public async Task<ActionResult<List<DiscussionModel>>> GetMostCommentedPosts(int count)
    {
        var posts = await postProvider.MostCommentedPostsAsync(count);
        return Ok(posts);
    }

    [HttpGet("most-popular/{count}")]
    public async Task<ActionResult<List<DiscussionModel>>> GetMostPopularPosts(int count)
    {
        var posts = await postProvider.MostPopularPostsAsync(count);
        return Ok(posts);
    }

    [HttpGet("most-recent/{count}")]
    public async Task<ActionResult<List<DiscussionModel>>> GetMostRecentPosts(int count)
    {
        var posts = await postProvider.MostRecentPostsAsync(count);
        return Ok(posts);
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<DiscussionModel>> CreatePost(DiscussionModel discussionModel)
    {
        var userPrincipal = HttpContext.User;
        var post = await postProvider.CreatePostAsync(discussionModel, userPrincipal);
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
        var result = await postProvider.UpdatePostAsync(discussionModel, userPrincipal);
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
        var result = await postProvider.DeletePostAsync(id);
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
        var result = await postProvider.CommentPostAsync(id, comment, userPrincipal);
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
        var result = await postProvider.LikePostAsync(id, userPrincipal);
        if (!result)
        {
            return BadRequest("Unable to like post.");
        }
        return NoContent();
    }
}