using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ShareSmallBiz.Portal.Data;
using ShareSmallBiz.Portal.Infrastructure.Services;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ShareSmallBiz.Portal.Areas.Forum.Controllers;

[Authorize]
[Area("Forum")]
public class PostController(
    DiscussionProvider postService,
    ShareSmallBizUserManager userManager,
    ILogger<PostController> logger) : ForumBaseController
{
    public async Task<IActionResult> Index()
    {
        if (User.IsInRole("Admin"))
        {
            return View(await postService.GetAllPostsAsync());
        }
        return View(await postService.GetAllUserPostsAsync());
    }
    [HttpGet, ActionName("MyPosts")]
    public async Task<IActionResult> MyPosts()
    {
        var post = await postService.GetAllUserPostsAsync();
        if (post == null)
        {
            return NotFound();
        }
        return View("index",post);
    }

    public IActionResult Create()
    {
        var discussionModel = new DiscussionModel();
        return View("Edit",discussionModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(DiscussionModel discussionModel)
    {
        ClaimsPrincipal currentUser = this.User;
        

        if (!ModelState.IsValid)
        {
            return View("Edit", discussionModel);
        }
        try
        {
            var user = await userManager.GetUserAsync(User);
            if (user == null)
            {
                logger.LogWarning("User not found when creating post.");
                return Unauthorized();
            }
            discussionModel.CreatedID = user.Id;
            await postService.CreatePostAsync(discussionModel,currentUser);
            return RedirectToAction("Index");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating post");
            ModelState.AddModelError("", "An error occurred while creating the post.");
            return View("Edit", discussionModel);
        }
    }

    public async Task<IActionResult> Edit(int id)
    {
        var post = await postService.GetPostByIdAsync(id);
        if (post == null)
        {
            return NotFound();
        }
        var user = await userManager.GetUserAsync(User);
        if (post.Author.Id != user.Id)
        {
            return RedirectToAction("Index");
        }
        return View(post);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(DiscussionModel discussionModel)
    {
        ClaimsPrincipal currentUser = this.User;
        if (!ModelState.IsValid)
        {
            return View(discussionModel);
        }

        try
        {
            var user = await userManager.GetUserAsync(User);
            if (user == null)
            {
                logger.LogWarning("User not found when updating post.");
                return Unauthorized();
            }

            discussionModel.ModifiedID = user.Id;
            var success = await postService.UpdatePostAsync(discussionModel, currentUser);
            if (!success)
            {
                return NotFound();
            }
            return RedirectToAction("Index");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating post");
            ModelState.AddModelError("", "An error occurred while updating the post.");
            return View(discussionModel);
        }
    }

    public async Task<IActionResult> Delete(int id)
    {
        var post = await postService.GetPostByIdAsync(id);
        if (post == null)
        {
            return NotFound();
        }
        return View(post);
    }

    [HttpPost, ActionName("DeleteConfirmed")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        try
        {
            var success = await postService.DeletePostAsync(id);
            if (!success)
            {
                return NotFound();
            }
            return RedirectToAction("Index");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting post");
            return RedirectToAction("Index");
        }
    }
}
