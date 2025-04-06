using ShareSmallBiz.Portal.Infrastructure.Services;
using ShareSmallBiz.Portal.Areas.Media.Services;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;

namespace ShareSmallBiz.Portal.Areas.Forum.Controllers
{
    [Authorize]
    [Area("Forum")]
    public class PostController : ForumBaseController
    {
        private readonly DiscussionProvider _postService;
        private readonly ShareSmallBizUserManager _userManager;
        private readonly KeywordProvider _keywordService;
        private readonly ILogger<PostController> _logger;
        private readonly MediaService _mediaService;  // Added media service
        private readonly StorageProviderService _storageProviderService;  // Added storage provider service

        public PostController(
            DiscussionProvider postService,
            ShareSmallBizUserManager userManager,
            KeywordProvider keywordService,
            ILogger<PostController> logger,
            MediaService mediaService,  // Injected media service
            StorageProviderService storageProviderService)  // Injected storage provider service
        {
            _postService = postService;
            _userManager = userManager;
            _keywordService = keywordService;
            _logger = logger;
            _mediaService = mediaService;
            _storageProviderService = storageProviderService;
        }

        // GET: /Forum/Post/Index
        public async Task<IActionResult> Index()
        {
            if (User.IsInRole("Admin"))
            {
                return View(await _postService.GetAllDiscussionsAsync());
            }
            return View(await _postService.GetAllUserDiscussionsAsync());
        }

        [HttpGet, ActionName("MyPosts")]
        public async Task<IActionResult> MyPosts()
        {
            var post = await _postService.GetAllUserDiscussionsAsync();
            if (post == null)
            {
                return NotFound();
            }
            return View("index", post);
        }

        // GET: /Forum/Post/Create
        public async Task<IActionResult> Create()
        {
            var discussionModel = new DiscussionModel();
            discussionModel.Keywords = await GetCachedKeywordNamesAsync();
            return View("Edit", discussionModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DiscussionModel discussionModel)
        {
            ClaimsPrincipal currentUser = this.User;
            if (!ModelState.IsValid)
            {
                // In case of error, also pass cached keywords back to the view
                discussionModel.Keywords = await GetCachedKeywordNamesAsync();
                return View("Edit", discussionModel);
            }
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    _logger.LogWarning("User not found when creating discussionModel.");
                    return Unauthorized();
                }

                // Process cover image if it exists and is a valid URL
                if (!string.IsNullOrEmpty(discussionModel.Cover) && !discussionModel.Cover.StartsWith("/Media/"))
                {
                    var mediaId = await ProcessCoverImage(discussionModel.Cover, user.Id, discussionModel.Title);
                    if (mediaId > 0)
                    {
                        discussionModel.Cover = $"/Media/{mediaId}";
                    }
                }

                discussionModel.CreatedID = user.Id;
                await _postService.CreateDiscussionAsync(discussionModel, currentUser);
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating discussionModel");
                ModelState.AddModelError(string.Empty, "An error occurred while creating the discussionModel.");
                discussionModel.Keywords = await GetCachedKeywordNamesAsync();
                return View("Edit", discussionModel);
            }
        }

        // GET: /Forum/Post/Edit/{id}
        public async Task<IActionResult> Edit(int id)
        {
            var discussionModel = await _postService.GetPostByIdAsync(id);
            if (discussionModel == null)
            {
                return NotFound();
            }
            var user = await _userManager.GetUserAsync(User);

            if (!User.IsInRole("Admin"))
            {
                if (discussionModel.Creator.Id != user.Id)
                {
                    return RedirectToAction("Index");
                }
            }

            // Pass cached keyword names to the view
            discussionModel.Keywords = await GetCachedKeywordNamesAsync();
            return View(discussionModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(DiscussionModel discussionModel)
        {
            ClaimsPrincipal currentUser = this.User;
            if (!ModelState.IsValid)
            {
                discussionModel.Keywords = await GetCachedKeywordNamesAsync();
                return View(discussionModel);
            }
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    _logger.LogWarning("User not found when updating discussionModel.");
                    return Unauthorized();
                }

                // Get the existing post to check if cover image has changed
                var existingPost = await _postService.GetPostByIdAsync(discussionModel.Id);

                // Process cover image if it has changed and is a valid URL, but not already a Media URL
                if (!string.IsNullOrEmpty(discussionModel.Cover) &&
                    discussionModel.Cover != existingPost.Cover &&
                    !discussionModel.Cover.StartsWith("/Media/"))
                {
                    var mediaId = await ProcessCoverImage(discussionModel.Cover, user.Id, discussionModel.Title);
                    if (mediaId > 0)
                    {
                        discussionModel.Cover = $"/Media/{mediaId}";
                    }
                }

                discussionModel.ModifiedID = user.Id;
                var success = await _postService.UpdatePostAsync(discussionModel, currentUser);
                if (!success)
                {
                    _logger.LogError("Error updating discussionModel");
                    discussionModel.Keywords = await GetCachedKeywordNamesAsync();
                    return View(discussionModel);
                }
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating discussionModel");
                ModelState.AddModelError(string.Empty, "An error occurred while updating the discussionModel.");
                discussionModel.Keywords = await GetCachedKeywordNamesAsync();
                return View(discussionModel);
            }
        }

        // GET: /Forum/Post/Delete/{id}
        public async Task<IActionResult> Delete(int id)
        {
            var post = await _postService.GetPostByIdAsync(id);
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
                // Get the post to check if it has a cover image that needs to be cleaned up
                var post = await _postService.GetPostByIdAsync(id);
                if (post != null && !string.IsNullOrEmpty(post.Cover) && post.Cover.StartsWith("/Media/"))
                {
                    // We don't actually delete the media here as it might be used elsewhere
                    // But in a real implementation, you could track usage and delete if not referenced elsewhere
                }

                var success = await _postService.DeleteDiscussionAsync(id);
                if (!success)
                {
                    return NotFound();
                }
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting discussionModel");
                return RedirectToAction("Index");
            }
        }

        /// <summary>
        /// Process a cover image URL and store it in the media library
        /// </summary>
        /// <returns>The ID of the media item, or 0 if processing failed</returns>
        private async Task<int> ProcessCoverImage(string coverUrl, string userId, string postTitle)
        {
            try
            {
                // Determine if this is an external URL or an Unsplash URL
                bool isUnsplashUrl = coverUrl.Contains("unsplash.com");

                // Create basic metadata
                var metadata = new Dictionary<string, string>
                {
                    { "source", isUnsplashUrl ? "unsplash" : "external" },
                    { "usage", "post_cover" },
                    { "postTitle", postTitle }
                };

                // Convert to JSON
                var metadataJson = JsonSerializer.Serialize(metadata);

                // Create a unique filename
                var fileName = $"cover_{Guid.NewGuid():N}_{Path.GetFileName(coverUrl)}";

                // For Unsplash URLs, we should also include proper attribution
                string attribution = string.Empty;
                if (isUnsplashUrl)
                {
                    attribution = "Photo from Unsplash";
                    try
                    {
                        // Try to extract username from URL (simplified - in production, use the Unsplash API)
                        var uri = new Uri(coverUrl);
                        var segments = uri.AbsolutePath.Split('/');
                        int photoIndex = Array.IndexOf(segments, "photos");
                        if (photoIndex >= 0 && photoIndex < segments.Length - 2)
                        {
                            attribution = $"Photo by {segments[photoIndex + 1]} on Unsplash";
                        }
                    }
                    catch
                    {
                        // If extraction fails, use generic attribution
                    }
                }

                // Create as external link in the media library
                var media = await _storageProviderService.CreateExternalMediaAsync(
                    coverUrl,
                    fileName,
                    Data.Enums.MediaType.Image,
                    userId,
                    attribution,
                    $"Cover image for: {postTitle}",
                    metadataJson);

                if (media != null)
                {
                    return media.Id;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing cover image: {Url}", coverUrl);
            }

            return 0;
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
                var keywords = await _keywordService.GetAllKeywordsAsync();
                keywordNames = keywords.Select(k => k.Name).ToList();
                HttpContext.Session.SetString(cacheKey, JsonSerializer.Serialize(keywordNames));
            }
            return keywordNames;
        }
    }
}