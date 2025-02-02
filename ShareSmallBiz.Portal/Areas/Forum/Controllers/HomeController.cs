using Microsoft.AspNetCore.Mvc;
using ShareSmallBiz.Portal.Infrastructure.Services;

namespace ShareSmallBiz.Portal.Areas.Forum.Controllers;

[Area("Forum")]
[Route("Forum/[controller]")]
public class HomeController(IPostProvider postProvider) : ForumBaseController
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

        // Ensure the correct slug is in the URL (SEO)
        if (!string.Equals(post.Slug, slug, StringComparison.OrdinalIgnoreCase))
        {
            return RedirectToActionPermanent(nameof(ViewPost), new { id, slug = post.Slug });
        }

        return View(post);
    }
}
