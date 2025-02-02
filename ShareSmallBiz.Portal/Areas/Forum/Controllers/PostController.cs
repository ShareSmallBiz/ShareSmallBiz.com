﻿using Microsoft.AspNetCore.Authorization;
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
    IPostProvider postService,
    UserManager<ShareSmallBizUser> userManager,
    ILogger<PostController> logger) : ForumBaseController
{
    public async Task<IActionResult> Index()
    {
        var posts = await postService.GetAllPostsAsync();
        return View(posts);
    }

    public IActionResult Create()
    {
        var postModel = new PostModel();
        return View("Edit",postModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PostModel postModel)
    {
        if (!ModelState.IsValid)
        {
            return View("Edit", postModel);
        }
        try
        {
            var user = await userManager.GetUserAsync(User);
            if (user == null)
            {
                logger.LogWarning("User not found when creating post.");
                return Unauthorized();
            }
            postModel.CreatedID = user.Id;
            await postService.CreatePostAsync(postModel);
            return RedirectToAction("Index");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating post");
            ModelState.AddModelError("", "An error occurred while creating the post.");
            return View("Edit", postModel);
        }
    }

    public async Task<IActionResult> Edit(int id)
    {
        var post = await postService.GetPostByIdAsync(id);
        if (post == null)
        {
            return NotFound();
        }
        return View(post);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(PostModel postModel)
    {
        if (!ModelState.IsValid)
        {
            return View(postModel);
        }

        try
        {
            var user = await userManager.GetUserAsync(User);
            if (user == null)
            {
                logger.LogWarning("User not found when updating post.");
                return Unauthorized();
            }

            postModel.ModifiedID = user.Id;
            var success = await postService.UpdatePostAsync(postModel);
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
            return View(postModel);
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
