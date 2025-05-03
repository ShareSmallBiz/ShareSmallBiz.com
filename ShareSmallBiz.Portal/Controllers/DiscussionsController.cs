using ShareSmallBiz.Portal.Infrastructure.Services;
using System;
using System.Linq;

namespace ShareSmallBiz.Portal.Controllers;

[Route("Discussions")]
public class DiscussionsController(ILogger<DiscussionsController> logger, DiscussionProvider postProvider) : Controller
{
    [HttpGet("{id}/{slug}")]
    public async Task<IActionResult> ViewPost(int id, string slug)
    {
        var post = await postProvider.GetPostByIdAsync(id);
        if (post == null)
        {
            logger.LogWarning("ViewPost Not Found. {Id}:{Slug}", id, slug);
            return RedirectToAction("Index");
        }
        if (!string.Equals(post.Slug, slug, StringComparison.OrdinalIgnoreCase))
        {
            return RedirectToActionPermanent(nameof(ViewPost), new { id, slug = post.Slug });
        }
        return View(post);
    }
    // GET: /post
    [HttpGet("tag/{id}")]
    public async Task<IActionResult> tag(string id)
    {
        DiscussionListModel discussionModels = [.. await postProvider.GetDiscussionsByTagAsync(id).ConfigureAwait(true)];

        if (discussionModels == null || discussionModels?.Count == 0)
        {
            logger.LogWarning("TagPost Not Found. {id}", id);
            return RedirectToAction("Index");
        }
        discussionModels.Description = $"Discussions tagged with {id}";
        return View(discussionModels);
    }

    // GET: /post
    [HttpGet("")]
    public IActionResult Index()
    {
        return View();
    }

    // GET: /post/{id}/comment
    [HttpGet("{id}/comment")]
    public async Task<IActionResult> GetComments(int postId)
    {
        var model = await postProvider.GetPostByIdAsync(postId);
        return PartialView("_CommentsPartial", model.Comments);
    }

    // POST: /post/{id}/comment
    [HttpPost("{id}/comment")]
    public async Task<IActionResult> AddComment(int id, [FromForm] string comment)
    {
        if (!User.Identity.IsAuthenticated)
        {
            logger.LogWarning("Unauthorized Comment. User is not authenticated id:{id} - {comment}", id, comment);
            return RedirectToAction("Index");
        }
        var userPrincipal = HttpContext.User;
        await postProvider.DiscussionCommentPostAsync(id, comment, userPrincipal);
        var model = await postProvider.GetPostByIdAsync(id);
        return PartialView("_CommentsPartial", model.Comments);
    }

    // DELETE: /post/{id}/comment/{commentId}
    [HttpDelete("{id}/comment/{commentId}")]
    public async Task<IActionResult> DeleteComment(int id, int commentId, CancellationToken ct = default)
    {
        if (!User?.Identity?.IsAuthenticated ?? true)
        {
            logger.LogWarning("Unauthorized Delete Comment. User is not authenticated");
            return RedirectToAction("Index");
        }
        var userPrincipal = HttpContext.User;
        await postProvider.DeleteCommentAsync(id, commentId, userPrincipal, ct).ConfigureAwait(false);
        var model = await postProvider.GetPostByIdAsync(id).ConfigureAwait(false);
        return PartialView("_CommentsPartial", model.Comments);
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
