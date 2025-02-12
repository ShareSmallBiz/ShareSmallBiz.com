using Markdig.Extensions.MediaLinks;
using Microsoft.AspNetCore.Mvc;
using ShareSmallBiz.Portal.Infrastructure.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
            return NotFound();
        }

        if (!string.Equals(post.Slug, slug, StringComparison.OrdinalIgnoreCase))
        {
            return RedirectToActionPermanent(nameof(ViewPost), new { id, slug = post.Slug });
        }

        return View(post);
    }

    // GET: /post
    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        return View();
    }

    // GET: /post/{postId}/comment
    [HttpGet("{postId}/comment")]
    public async Task<IActionResult> GetComments(int postId)
    {
        var model = await postProvider.GetPostByIdAsync(postId);
        return PartialView("_CommentsPartial", model.Comments);
    }

    // POST: /post/{postId}/comment
    [HttpPost("{postId}/comment")]
    public async Task<IActionResult> AddComment(int postId, [FromForm] string comment)
    {
        if (!User.Identity.IsAuthenticated)
        {
            return Unauthorized();
        }
        var userPrincipal = HttpContext.User;
        await postProvider.CommentPostAsync(postId, comment, userPrincipal);
        var model = await postProvider.GetPostByIdAsync(postId);
        return PartialView("_CommentsPartial", model.Comments);
    }

    // DELETE: /post/{postId}/comment/{commentId}
    [HttpDelete("{postId}/comment/{commentId}")]
    public async Task<IActionResult> DeleteComment(int postId, int commentId)
    {
        if (!User.Identity.IsAuthenticated)
        {
            return Unauthorized();
        }
        var userPrincipal = HttpContext.User;
        await postProvider.DeleteCommentAsync(postId, commentId, userPrincipal);
        var model = await postProvider.GetPostByIdAsync(postId);
        return PartialView("_CommentsPartial", model.Comments);
    }



    [HttpGet("all")]
    public async Task<IActionResult> GetAllPosts()
    {
        var posts = await postProvider.GetAllPostsAsync();
        return Ok(posts);
    }

    [HttpGet("my/{count}")]
    public async Task<IActionResult> MyPosts(int count = 100)
    {
        var posts = await postProvider.GetAllUserPostsAsync();
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
        var result = await postProvider.GetPostsAsync(pageNumber, pageSize, SortType.Recent);
        return PartialView("_postList", result.Posts);
    }


}
