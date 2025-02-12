using Markdig.Extensions.MediaLinks;
using Microsoft.AspNetCore.Mvc;
using ShareSmallBiz.Portal.Infrastructure.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ShareSmallBiz.Portal.Controllers
{
    [Route("post")]
    public class PostController(ILogger<PostController> logger, PostProvider _postProvider) : Controller
    {
        [HttpGet("{id}/{slug}")]
        public async Task<IActionResult> ViewPost(int id, string slug)
        {
            var post = await _postProvider.GetPostByIdAsync(id);
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
            var model = await _postProvider.GetPostByIdAsync(5);
            return View(model);
        }

        // GET: /post/{postId}/comment
        [HttpGet("{postId}/comment")]
        public async Task<IActionResult> GetComments(int postId)
        {
            var model = await _postProvider.GetPostByIdAsync(postId);
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
            await _postProvider.CommentPostAsync(postId, comment, userPrincipal);
            var model = await _postProvider.GetPostByIdAsync(postId);
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
            await _postProvider.DeleteCommentAsync(postId, commentId, userPrincipal);
            var model = await _postProvider.GetPostByIdAsync(postId);
            return PartialView("_CommentsPartial", model.Comments);
        }
    }
}
