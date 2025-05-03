using ShareSmallBiz.Portal.Infrastructure.Services;
using ShareSmallBiz.Portal.Areas.Media.Services;
using ShareSmallBiz.Portal.Data.Enums;
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
        private readonly MediaService _mediaService;
        private readonly MediaFactoryService _mediaFactoryService;
        private readonly UnsplashService _unsplashService;
        private readonly ILogger<PostController> _logger;

        public PostController(
            DiscussionProvider postService,
            ShareSmallBizUserManager userManager,
            KeywordProvider keywordService,
            MediaService mediaService,
            MediaFactoryService mediaFactoryService,
            UnsplashService unsplashService,
            ILogger<PostController> logger)
        {
            _postService = postService;
            _userManager = userManager;
            _keywordService = keywordService;
            _mediaService = mediaService;
            _mediaFactoryService = mediaFactoryService;
            _unsplashService = unsplashService;
            _logger = logger;
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
                discussionModel.CreatedID = user.Id;

                // Handle cover image if it's from Unsplash or external URL
                await ProcessCoverImageAsync(discussionModel);

                // Process content for any embedded media attribution
                discussionModel.Content = ProcessContentForMediaAttribution(discussionModel.Content);

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

                discussionModel.ModifiedID = user.Id;

                // Get the existing post to compare changes
                var existingPost = await _postService.GetPostByIdAsync(discussionModel.Id);

                // Handle cover image if it has changed
                if (existingPost.Cover != discussionModel.Cover)
                {
                    await ProcessCoverImageAsync(discussionModel);
                }

                // Process content for any embedded media attribution
                discussionModel.Content = ProcessContentForMediaAttribution(discussionModel.Content);

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
                // Get the post to be deleted so we can clean up associated media
                var post = await _postService.GetPostByIdAsync(id);
                if (post != null)
                {
                    // Clean up media associations and possibly delete media files
                    await CleanupMediaAsync(post);
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
        /// Processes the cover image URL, converting from direct URLs to media service routes if needed
        /// </summary>
        private async Task ProcessCoverImageAsync(DiscussionModel discussionModel)
        {
            if (string.IsNullOrEmpty(discussionModel.Cover) || discussionModel.Cover.StartsWith("/Media/"))
            {
                // Already using the media service route or no cover
                return;
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Check if it's an Unsplash URL
            if (discussionModel.Cover.Contains("unsplash.com"))
            {
                try
                {
                    // Extract photo ID from URL
                    string photoId = ExtractUnsplashPhotoId(discussionModel.Cover);
                    if (!string.IsNullOrEmpty(photoId))
                    {
                        // Get photo details from Unsplash API
                        var photo = await _unsplashService.GetPhotoAsync(photoId);
                        if (photo != null)
                        {
                            // Create media using UnsplashService
                            var media = await _unsplashService.CreateUnsplashMediaAsync(photo, userId);
                            if (media != null)
                            {
                                // Update the cover URL to use the media service
                                discussionModel.Cover = $"/Media/{media.Id}";
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing Unsplash cover image for discussion {Id}", discussionModel.Id);
                    // Continue with the original URL if there's an error
                }
            }
            else if (Uri.IsWellFormedUriString(discussionModel.Cover, UriKind.Absolute))
            {
                try
                {
                    // It's an external URL, create a media entry for it
                    var fileName = $"cover_{Path.GetRandomFileName()}.jpg";
                    var media = await _mediaService.CreateExternalMediaAsync(
                        discussionModel.Cover,
                        fileName,
                        MediaType.Image,
                        userId,
                        "Cover image for " + discussionModel.Title,
                        "Post cover image"
                    );

                    if (media != null)
                    {
                        // Update the cover URL to use the media service
                        discussionModel.Cover = $"/Media/{media.Id}";
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing external cover image for discussion {Id}", discussionModel.Id);
                    // Continue with the original URL if there's an error
                }
            }
        }

        /// <summary>
        /// Processes the content to handle any embedded media and ensure proper attribution
        /// </summary>
        private string ProcessContentForMediaAttribution(string content)
        {
            if (string.IsNullOrEmpty(content))
            {
                return content;
            }

            // This is a placeholder for future enhancement
            // In a more comprehensive implementation, you might:
            // 1. Parse the HTML content to find image tags
            // 2. Extract unsplash image URLs
            // 3. Register those with the media service
            // 4. Replace direct image URLs with media service URLs
            // 5. Ensure attribution blocks are properly maintained

            return content;
        }

        /// <summary>
        /// Cleans up media associated with a post that's being deleted
        /// </summary>
        private async Task CleanupMediaAsync(DiscussionModel post)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                // Check if the cover image is using our media service
                if (!string.IsNullOrEmpty(post.Cover) && post.Cover.StartsWith("/Media/"))
                {
                    var mediaIdStr = post.Cover.Replace("/Media/", "");
                    if (int.TryParse(mediaIdStr, out int mediaId))
                    {
                        var media = await _mediaService.GetUserMediaByIdAsync(mediaId, userId);
                        if (media != null)
                        {
                            // Optionally delete the media or just remove the association
                            // await _mediaService.DeleteMediaAsync(media);
                        }
                    }
                }

                // Clean up any media items associated with this post
                if (post.Media != null && post.Media.Any())
                {
                    foreach (var media in post.Media)
                    {
                        // Optionally delete the media or just remove the association
                        // await _mediaService.DeleteMediaAsync(media);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up media for post {Id}", post.Id);
                // Continue with deletion even if media cleanup fails
            }
        }

        /// <summary>
        /// Extracts the Unsplash photo ID from a URL
        /// </summary>
        private string ExtractUnsplashPhotoId(string url)
        {
            try
            {
                // Handle various Unsplash URL formats
                // Examples:
                // https://unsplash.com/photos/{photo_id}
                // https://unsplash.com/photos/{photo_id}/download
                // https://unsplash.com/photos/{photo_id}?utm_source=...

                var uri = new Uri(url);

                // Check if this is an Unsplash URL
                if (uri.Host != "unsplash.com" && !uri.Host.EndsWith(".unsplash.com"))
                    return string.Empty;

                // Split path segments
                var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);

                // Look for 'photos' segment followed by the ID
                for (int i = 0; i < segments.Length - 1; i++)
                {
                    if (segments[i].Equals("photos", StringComparison.OrdinalIgnoreCase))
                    {
                        return segments[i + 1];
                    }
                }

                return string.Empty;
            }
            catch
            {
                return string.Empty;
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
                var keywords = await _keywordService.GetAllKeywordsAsync();
                keywordNames = keywords.Select(k => k.Name).ToList();
                HttpContext.Session.SetString(cacheKey, JsonSerializer.Serialize(keywordNames));
            }
            return keywordNames;
        }
    }
}