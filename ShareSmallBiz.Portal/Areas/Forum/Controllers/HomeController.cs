using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShareSmallBiz.Portal.Infrastructure.Services;
using System.Security.Claims;

namespace ShareSmallBiz.Portal.Areas.Forum.Controllers;

[Area("Forum")]
[Route("Forum/[controller]")]
public class HomeController(PostProvider postProvider) : ForumBaseController
{
    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var posts = await postProvider.MostPopularPostsAsync(3);
        return View(posts);
    }

    [HttpGet("{id}/{slug}")]
    public async Task<IActionResult> ViewPost(int id, string slug)
    {
        var post = await postProvider.GetPostByIdAsync(id);
        if (post == null)
        {
            return NotFound();
        }

        if (!string.Equals(post.Slug, slug, StringComparison.OrdinalIgnoreCase))
        {
            return RedirectToActionPermanent(nameof(ViewPost), new { id, slug = post.Slug });
        }

        return View(post);
    }

    [Authorize]
    [HttpPost("create")]
    public async Task<IActionResult> CreatePost([FromBody] PostModel postModel)
    {
        var user = User;
        var createdPost = await postProvider.CreatePostAsync(postModel, user);
        if (createdPost == null)
        {
            return BadRequest("Post creation failed.");
        }
        return Ok(createdPost);
    }

    [Authorize]
    [HttpPut("update")]
    public async Task<IActionResult> UpdatePost([FromBody] PostModel postModel)
    {
        var user = User;
        var result = await postProvider.UpdatePostAsync(postModel, user);
        return result ? Ok() : BadRequest("Post update failed.");
    }

    [Authorize]
    [HttpDelete("delete/{id}")]
    public async Task<IActionResult> DeletePost(int id)
    {
        var result = await postProvider.DeletePostAsync(id);
        return result ? Ok() : NotFound("Post not found or could not be deleted.");
    }

    [Authorize]
    [HttpPost("like/{postId}")]
    public async Task<IActionResult> LikePost(int postId)
    {
        var result = await postProvider.LikePostAsync(postId, User);
        return result ? Json(new { success = true }) : Json(new { success = false, message = "Failed to like the post or already liked." });
    }

    [Authorize]
    [HttpPost("comment/{postId}")]
    public async Task<IActionResult> CommentPost(int postId, [FromBody] string comment)
    {
        var result = await postProvider.CommentPostAsync(postId, comment, User);
        if (!result)
        {
            return BadRequest("Failed to add comment.");
        }
        var updatedComments = await postProvider.GetPostByIdAsync(postId);
        return PartialView("_PostCommentsPartial", updatedComments?.Comments);
    }

    [HttpGet("all")]
    public async Task<IActionResult> GetAllPosts()
    {
        var posts = await postProvider.GetAllPostsAsync();
        return Ok(posts);
    }

    [HttpGet("recent/{count}")]
    public async Task<IActionResult> MostRecentPosts(int count)
    {
        var posts = await postProvider.MostRecentPostsAsync(count);
        return Ok(posts);
    }

    [HttpGet("popular/{count}")]
    public async Task<IActionResult> MostPopularPosts(int count)
    {
        var posts = await postProvider.MostPopularPostsAsync(count);
        return Ok(posts);
    }

    [HttpGet("paged")]
    public async Task<IActionResult> GetPosts([FromQuery] int pageNumber, [FromQuery] int pageSize, [FromQuery] SortType sortType)
    {
        var result = await postProvider.GetPostsAsync(pageNumber, pageSize, sortType);
        return Ok(result);
    }
}
