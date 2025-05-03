using ShareSmallBiz.Portal.Infrastructure.Services;

namespace ShareSmallBiz.Portal.Areas.Forum.Controllers;

[Area("Forum")]
[Route("Forum/[controller]")]
public class HomeController(DiscussionProvider postProvider, ILogger<HomeController> logger) : ForumBaseController
{
    [HttpGet("")]
    public IActionResult Index()
    {
        return View();
    }

    [HttpGet("{id}/{slug}")]
    public async Task<IActionResult> ViewPost(int id, string slug)
    {
        logger.LogInformation("Call to Forum-Home-ViewPost");
        var post = await postProvider.GetPostByIdAsync(id);
        if (post == null)
        {
            return NotFound();
        }
        string Url = $"/Discussions/{id}/{post.Slug}";
        return RedirectPermanent(Url);
    }

    [Authorize]
    [HttpPost("create")]
    public async Task<IActionResult> CreateDiscussion([FromBody] DiscussionModel discussionModel)
    {
        var user = User;
        var createdPost = await postProvider.CreateDiscussionAsync(discussionModel, user);
        if (createdPost == null)
        {
            return BadRequest("Discussion creation failed.");
        }
        return Ok(createdPost);
    }

    [Authorize]
    [HttpPut("update")]
    public async Task<IActionResult> UpdatePost([FromBody] DiscussionModel discussionModel)
    {
        var user = User;
        var result = await postProvider.UpdatePostAsync(discussionModel, user);
        return result ? Ok() : BadRequest("Discussion update failed.");
    }

    [Authorize]
    [HttpDelete("delete/{id}")]
    public async Task<IActionResult> DeletePost(int id)
    {
        var result = await postProvider.DeleteDiscussionAsync(id);
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
        var result = await postProvider.DiscussionCommentPostAsync(postId, comment, User);
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
        var posts = await postProvider.GetAllDiscussionsAsync();
        return Ok(posts);
    }

    [HttpGet("my/{count}")]
    public async Task<IActionResult> MyPosts(int count = 100)
    {
        var posts = await postProvider.GetAllUserDiscussionsAsync();
        return PartialView("_postList", posts);
    }


    [HttpGet("commented/{count}")]
    public async Task<IActionResult> MostCommentedPosts(int count)
    {
        var posts = await postProvider.MostCommentedPostsAsync(count);
        return PartialView("_postList", posts);
    }

    [HttpGet("popular/{count}")]
    public async Task<IActionResult> MostPopularPosts(int count)
    {
        var posts = await postProvider.MostPopularPostsAsync(count);
        return PartialView("_postList", posts);
    }

    [HttpGet("recent/{count}")]
    public async Task<IActionResult> MostRecentPosts(int count)
    {
        var posts = await postProvider.MostRecentPostsAsync(count);
        return PartialView("_postList", posts);
    }

    [HttpGet("paged")]
    public async Task<IActionResult> GetPosts([FromQuery] int pageNumber, [FromQuery] int pageSize)
    {
        var result = await postProvider.GetDiscussionsAsync(pageNumber, pageSize, SortType.Recent);
        return PartialView("_postList", result.Posts);
    }
}
