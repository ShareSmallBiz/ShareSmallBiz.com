namespace ShareSmallBiz.Portal.Areas.Admin.Controllers;

using global::ShareSmallBiz.Portal.Infrastructure.Services;
using ShareSmallBiz.Portal.Data;
using ShareSmallBiz.Portal.Infrastructure.Models;
using ShareSmallBiz.Portal.Areas.Media.Models;
using ShareSmallBiz.Portal.Areas.Media.Services;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;

public class AdminDiscussionModel : DiscussionModel
{
    public string? AuthorId { get; set; }
    public List<UserModel>? Users { get; set; } = [];
    // Add this property to store available media
    public List<MediaModel>? AvailableMedia { get; set; } = [];
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
    MediaService _mediaService,
    DiscussionProvider postService) : AdminBaseController(_context, userManager, _roleManager)
{
    // GET: /Forum/Post/Create
    public async Task<IActionResult> Create()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var discussionModel = new AdminDiscussionModel
        {
            Keywords = await GetCachedKeywordNamesAsync(),
            Users = await GetAllUsersAsync(),
            Media = [],
            AvailableMedia = await GetUserMediaAsync(userId)
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
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            discussionModel.AvailableMedia = await GetUserMediaAsync(userId);
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
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            discussionModel.AvailableMedia = await GetUserMediaAsync(userId);
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
        var userId = user.Id;

        if (!User.IsInRole("Admin"))
        {
            if (discussionModel.Creator.Id != user.Id)
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
            Creator = discussionModel.Creator,

            // Set AuthorId explicitly 
            AuthorId = discussionModel.Creator?.Id,

            // Pass cached keyword names and all users to the view
            Keywords = await GetCachedKeywordNamesAsync(),
            Users = await GetAllUsersAsync(),

            // Add media properties
            Media = discussionModel.Media ?? [],
            AvailableMedia = await GetUserMediaAsync(userId)
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
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            discussionModel.AvailableMedia = await GetUserMediaAsync(userId);
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
                if (author == null)
                {
                    logger.LogWarning("Creator not found when updating discussionModel.");
                    return Unauthorized();
                }
                discussionModel.CreatedID = discussionModel.AuthorId;
                discussionModel.Creator = new UserModel(author);
            }

            var success = await postService.UpdatePostAsync(discussionModel, currentUser);
            if (!success)
            {
                logger.LogError("Error updating discussionModel");
                discussionModel.Keywords = await GetCachedKeywordNamesAsync();
                discussionModel.Users = await GetAllUsersAsync();
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                discussionModel.AvailableMedia = await GetUserMediaAsync(userId);
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
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            discussionModel.AvailableMedia = await GetUserMediaAsync(userId);
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

    /// <summary>
    /// Retrieves media for a user
    /// </summary>
    private async Task<List<MediaModel>> GetUserMediaAsync(string userId)
    {
        var mediaItems = await _mediaService.GetUserMediaAsync(userId);
        return mediaItems.ToList();
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
    /// Adds a media item to a discussion
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddMedia(int discussionId, int mediaId)
    {
        var discussionModel = await postService.GetPostByIdAsync(discussionId);
        if (discussionModel == null)
        {
            return NotFound();
        }

        // Get the media item
        var mediaItem = await _mediaService.GetMediaByIdAsync(mediaId);
        if (mediaItem == null)
        {
            return NotFound();
        }

        // If this is the first media item, set it as cover
        if (discussionModel.Media == null || !discussionModel.Media.Any())
        {
            discussionModel.Cover = $"/Media/{mediaItem.Id}";
        }

        // Add media to the discussion
        if (discussionModel.Media == null)
        {
            discussionModel.Media = new List<MediaModel>();
        }

        // Check if the media is already attached
        if (!discussionModel.Media.Any(m => m.Id == mediaItem.Id))
        {
            discussionModel.Media.Add(mediaItem);
        }

        // Save changes
        var success = await postService.UpdatePostAsync(discussionModel, this.User);

        return RedirectToAction("Edit", new { id = discussionId });
    }

    /// <summary>
    /// Removes a media item from a discussion
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveMedia(int discussionId, int mediaId)
    {
        var discussionModel = await postService.GetPostByIdAsync(discussionId);
        if (discussionModel == null)
        {
            return NotFound();
        }

        // Remove the media
        if (discussionModel.Media != null)
        {
            var mediaToRemove = discussionModel.Media.FirstOrDefault(m => m.Id == mediaId);
            if (mediaToRemove != null)
            {
                discussionModel.Media.Remove(mediaToRemove);

                // If the removed media was the cover, update the cover to the first available media or empty
                if (discussionModel.Cover == $"/Media/{mediaId}")
                {
                    discussionModel.Cover = discussionModel.Media.Any()
                        ? $"/Media/{discussionModel.Media.First().Id}"
                        : "";
                }
            }
        }

        // Save changes
        var success = await postService.UpdatePostAsync(discussionModel, this.User);

        return RedirectToAction("Edit", new { id = discussionId });
    }

    /// <summary>
    /// Sets a media item as the cover for a discussion
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetCover(int discussionId, int mediaId)
    {
        var discussionModel = await postService.GetPostByIdAsync(discussionId);
        if (discussionModel == null)
        {
            return NotFound();
        }

        // Update the cover
        discussionModel.Cover = $"/Media/{mediaId}";

        // Save changes
        var success = await postService.UpdatePostAsync(discussionModel, this.User);

        return RedirectToAction("Edit", new { id = discussionId });
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