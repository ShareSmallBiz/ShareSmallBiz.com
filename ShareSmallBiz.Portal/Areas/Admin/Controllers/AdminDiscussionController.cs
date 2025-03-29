using Microsoft.AspNetCore.Mvc;

namespace ShareSmallBiz.Portal.Areas.Admin.Controllers;

using global::ShareSmallBiz.Portal.Areas.Forum.Controllers;
using global::ShareSmallBiz.Portal.Infrastructure.Services;
using ShareSmallBiz.Portal.Data;
using ShareSmallBiz.Portal.Infrastructure.Models;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;

public class AdminDiscussionModel : DiscussionModel
{
    public string AuthorId { get; set; }
    public List<UserModel>? Users { get; set; } = [];
}



[Authorize]
[Area("Admin")]
public class AdminDiscussionController(
    ShareSmallBizUserContext _context,
    ShareSmallBizUserManager userManager,
    RoleManager<IdentityRole> _roleManager,
    ILogger<AdminDiscussionController> logger,
    KeywordProvider keywordService,
    UserProvider userService,
    DiscussionProvider postService) : AdminBaseController(_context, userManager, _roleManager)
{

    // GET: /Forum/Post/Create
    public async Task<IActionResult> Create()
    {
        var discussionModel = new AdminDiscussionModel
        {
            Keywords = await GetCachedKeywordNamesAsync(),
            Users = await GetAllUsersAsync()
        };
        return View("Edit", discussionModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AdminDiscussionModel discussionModel)
    {
        ClaimsPrincipal currentUser = this.User;
        if (!ModelState.IsValid)
        {
            // In case of error, also pass cached keywords and users back to the view
            discussionModel.Keywords = await GetCachedKeywordNamesAsync();
            discussionModel.Users = await GetAllUsersAsync();
            return View("Edit", discussionModel);
        }
        try
        {
            var user = await userManager.GetUserAsync(User);
            if (user == null)
            {
                logger.LogWarning("User not found when creating discussionModel.");
                return Unauthorized();
            }
            discussionModel.CreatedID = user.Id;

            // If AuthorId is provided, use it instead of the current user's ID
            if (!string.IsNullOrEmpty(discussionModel.AuthorId))
            {
                discussionModel.CreatedID = discussionModel.AuthorId;
            }

            await postService.CreateDiscussionAsync(discussionModel, currentUser);
            return RedirectToAction("Index");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating discussionModel");
            ModelState.AddModelError(string.Empty, "An error occurred while creating the discussionModel.");
            discussionModel.Keywords = await GetCachedKeywordNamesAsync();
            discussionModel.Users = await GetAllUsersAsync();
            return View("Edit", discussionModel);
        }
    }

    // GET: /Forum/Post/Edit/{id}
    public async Task<IActionResult> Edit(int id)
    {
        var discussionModel = await postService.GetPostByIdAsync(id);
        if (discussionModel == null)
        {
            return NotFound();
        }
        var user = await userManager.GetUserAsync(User);

        if (!User.IsInRole("Admin"))
        {
            if (discussionModel.Author.Id != user.Id)
            {
                return RedirectToAction("Index");
            }
        }

        // Convert to AdminDiscussionModel and populate required properties
        var adminDiscussionModel = new AdminDiscussionModel
        {
            // Copy all properties from discussionModel
            Id = discussionModel.Id,
            Title = discussionModel.Title,
            Content = discussionModel.Content,
            Description = discussionModel.Description,
            Cover = discussionModel.Cover,
            IsFeatured = discussionModel.IsFeatured,
            IsPublic = discussionModel.IsPublic,
            PostType = discussionModel.PostType,
            PostViews = discussionModel.PostViews,
            Published = discussionModel.Published,
            Rating = discussionModel.Rating,
            Selected = discussionModel.Selected,
            Comments = discussionModel.Comments,
            CreatedDate = discussionModel.CreatedDate,
            ModifiedDate = discussionModel.ModifiedDate,
            CreatedID = discussionModel.CreatedID,
            ModifiedID = discussionModel.ModifiedID,
            Tags = discussionModel.Tags,
            Author = discussionModel.Author,
            Target = discussionModel.Target,
            TargetId = discussionModel.TargetId,

            // Set AuthorId explicitly 
            AuthorId = discussionModel.Author?.Id,

            // Pass cached keyword names and all users to the view
            Keywords = await GetCachedKeywordNamesAsync(),
            Users = await GetAllUsersAsync()
        };

        return View(adminDiscussionModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(AdminDiscussionModel discussionModel)
    {
        ClaimsPrincipal currentUser = this.User;
        if (!ModelState.IsValid)
        {
            discussionModel.Keywords = await GetCachedKeywordNamesAsync();
            discussionModel.Users = await GetAllUsersAsync();
            return View(discussionModel);
        }
        try
        {
            var user = await userManager.GetUserAsync(User);
            if (user == null)
            {
                logger.LogWarning("User not found when updating discussionModel.");
                return Unauthorized();
            }

            discussionModel.ModifiedID = user.Id;

            // If an AuthorId is specified and user is Admin, allow changing the author
            if (User.IsInRole("Admin") && !string.IsNullOrEmpty(discussionModel.AuthorId))
            {
                var author = await userManager.FindByIdAsync(discussionModel.AuthorId);
                if(author == null)
                {
                    logger.LogWarning("Author not found when updating discussionModel.");
                    return Unauthorized();
                }
                discussionModel.CreatedID = discussionModel.AuthorId;
                discussionModel.Author = new UserModel(author);
            }

            var success = await postService.UpdatePostAsync(discussionModel, currentUser);
            if (!success)
            {
                logger.LogError("Error updating discussionModel");
                discussionModel.Keywords = await GetCachedKeywordNamesAsync();
                discussionModel.Users = await GetAllUsersAsync();
                return View(discussionModel);
            }
            return RedirectToAction("Index");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating discussionModel");
            ModelState.AddModelError(string.Empty, "An error occurred while updating the discussionModel.");
            discussionModel.Keywords = await GetCachedKeywordNamesAsync();
            discussionModel.Users = await GetAllUsersAsync();
            return View(discussionModel);
        }
    }

    /// <summary>
    /// Retrieves all users as UserModel objects for dropdown selection
    /// </summary>
    private async Task<List<UserModel>> GetAllUsersAsync()
    {
        var users = await userService.GetAllPublicUsersAsync().ConfigureAwait(true);
        return users;
    }



    // GET: /Forum/Post/Index
    public async Task<IActionResult> Index()
    {
        if (User.IsInRole("Admin"))
        {
            return View(await postService.GetAllDiscussionsAsync());
        }
        return View(await postService.GetAllUserDiscussionsAsync());
    }

    [HttpGet, ActionName("MyPosts")]
    public async Task<IActionResult> MyPosts()
    {
        var post = await postService.GetAllUserDiscussionsAsync();
        if (post == null)
        {
            return NotFound();
        }
        return View("index", post);
    }


    // GET: /Forum/Post/Delete/{id}
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
            var success = await postService.DeleteDiscussionAsync(id);
            if (!success)
            {
                return NotFound();
            }
            return RedirectToAction("Index");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting discussionModel");
            return RedirectToAction("Index");
        }
    }

    /// <summary>
    /// Retrieves a cached list of keyword names from the session.
    /// If not present, fetches from the KeywordProvider, caches it, and returns the list.
    /// </summary>
    private async Task<List<string>> GetCachedKeywordNamesAsync()
    {
        const string cacheKey = "CachedKeywordNames";
        var cachedJson = HttpContext.Session.GetString(cacheKey);
        List<string> keywordNames;
        if (!string.IsNullOrEmpty(cachedJson))
        {
            keywordNames = JsonSerializer.Deserialize<List<string>>(cachedJson);
        }
        else
        {
            // Assume GetAllKeywordsAsync returns a collection of keyword entities with a Name property
            var keywords = await keywordService.GetAllKeywordsAsync();
            keywordNames = keywords.Select(k => k.Name).ToList();
            HttpContext.Session.SetString(cacheKey, JsonSerializer.Serialize(keywordNames));
        }
        return keywordNames;
    }
}

