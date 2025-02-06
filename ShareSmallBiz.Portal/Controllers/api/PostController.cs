using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using ShareSmallBiz.Portal.Infrastructure.Services;
using ShareSmallBiz.Portal.Data;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ShareSmallBiz.Portal.Controllers.api;

[ApiController]
[Route("api/[controller]")]
public class PostController(PostProvider postProvider) : ControllerBase
{

    // GET: api/Post
    [HttpGet]
    public async Task<ActionResult<IEnumerable<PostModel>>> GetAllPosts(bool onlyPublic = true)
    {
        var posts = await postProvider.GetAllPostsAsync(onlyPublic);
        return Ok(posts);
    }

    // GET: api/Post/5
    [HttpGet("{id}")]
    public async Task<ActionResult<PostModel>> GetPostById(int id)
    {
        var post = await postProvider.GetPostByIdAsync(id);
        if (post == null)
        {
            return NotFound();
        }
        return Ok(post);
    }

    // POST: api/Post
    [HttpPost]
    [Authorize]
    public async Task<ActionResult<PostModel>> CreatePost(PostModel postModel)
    {
        var userPrincipal = HttpContext.User;
        var post = await postProvider.CreatePostAsync(postModel, userPrincipal);
        if (post == null)
        {
            return BadRequest("Unable to create post.");
        }
        return CreatedAtAction(nameof(GetPostById), new { id = post.Id }, post);
    }

    // PUT: api/Post/5
    [HttpPut("{id}")]
    [Authorize]
    public async Task<IActionResult> UpdatePost(int id, PostModel postModel)
    {
        if (id != postModel.Id)
        {
            return BadRequest();
        }

        var userPrincipal = HttpContext.User;
        var result = await postProvider.UpdatePostAsync(postModel, userPrincipal);
        if (!result)
        {
            return NotFound();
        }
        return NoContent();
    }

    // DELETE: api/Post/5
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

    // POST: api/Post/5/Comment
    [HttpPost("{id}/Comment")]
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

    // POST: api/Post/5/Like
    [HttpPost("{id}/Like")]
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