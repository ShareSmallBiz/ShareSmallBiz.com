using ShareSmallBiz.Portal.Areas.Media.Services;
using ShareSmallBiz.Portal.Data;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace ShareSmallBiz.Portal.Areas.Media.Controllers;

[Area("Media")]
[Authorize]
[Route("Media/YouTube")]
public class YouTubeController : Controller
{
    private readonly ShareSmallBizUserContext _context;
    private readonly ILogger<YouTubeController> _logger;
    private readonly YouTubeMediaService _youTubeMediaService;
    private readonly MediaService _mediaService;
    private readonly MediaFactoryService _mediaFactoryService;

    public YouTubeController(
        ShareSmallBizUserContext context,
        ILogger<YouTubeController> logger,
        YouTubeMediaService youTubeMediaService,
        MediaService mediaService,
        MediaFactoryService mediaFactoryService)
    {
        _context = context;
        _logger = logger;
        _youTubeMediaService = youTubeMediaService;
        _mediaService = mediaService;
        _mediaFactoryService = mediaFactoryService;
    }

    // GET: /Media/YouTube
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var viewModel = new YouTubeSearchViewModel();

        // Add popular categories for small businesses
        viewModel.PopularCategories = new List<string>
        {
            "small business tips",
            "marketing tutorial",
            "social media strategy",
            "web design tutorial",
            "product photography",
            "presentation skills"
        };

        // Get any recently added YouTube videos (for the "Recently Added" section)
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var recentYouTubeMedia = await _youTubeMediaService.GetRecentlyAddedVideosAsync(userId, 4);

        viewModel.RecentlyAdded = recentYouTubeMedia.ToList();

        return View(viewModel);
    }

    // GET: /Media/YouTube/Search
    [HttpGet("Search")]
    public async Task<IActionResult> Search(string? query = "")
    {
        var viewModel = new YouTubeSearchViewModel();
        if (string.IsNullOrEmpty(query))
        {
            return View(viewModel);
        }

        viewModel.Query = query;

        try
        {
            var searchResults = await _youTubeMediaService.SearchVideosAsync(
                viewModel.Query,
                viewModel.MaxResults > 0 ? viewModel.MaxResults : 10);

            viewModel.SearchResults = searchResults.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching YouTube");
            ModelState.AddModelError(string.Empty, $"Error searching YouTube: {ex.Message}");
        }

        return View(viewModel);
    }

    // POST: /Media/YouTube/Search
    [HttpPost("Search")]
    public async Task<IActionResult> Search(YouTubeSearchViewModel viewModel)
    {
        if (string.IsNullOrWhiteSpace(viewModel.Query))
        {
            ModelState.AddModelError("Query", "Please enter a search term");
            return View(viewModel);
        }

        try
        {
            var searchResults = await _youTubeMediaService.SearchVideosAsync(
                viewModel.Query,
                viewModel.MaxResults > 0 ? viewModel.MaxResults : 10);

            viewModel.SearchResults = searchResults.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching YouTube");
            ModelState.AddModelError(string.Empty, $"Error searching YouTube: {ex.Message}");
        }

        return View(viewModel);
    }

    // POST: /Media/YouTube/Save
    [HttpPost("Save")]
    public async Task<IActionResult> Save(YouTubeVideoViewModel viewModel)
    {
        if (string.IsNullOrEmpty(viewModel.VideoId) || string.IsNullOrEmpty(viewModel.Title))
        {
            return BadRequest("Video ID and title are required");
        }

        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Build YouTube URL from video ID
            var youtubeUrl = $"https://www.youtube.com/watch?v={viewModel.VideoId}";

            // If no description was provided, use a default one
            var description = !string.IsNullOrEmpty(viewModel.Description)
                ? viewModel.Description
                : $"YouTube video added on {DateTime.UtcNow:g}";

            // Create the media entry using the YouTubeMediaService
            var media = await _youTubeMediaService.CreateYouTubeMediaAsync(
                youtubeUrl,
                viewModel.Title,
                description,
                viewModel.ChannelTitle, // Use channel title as attribution
                userId);

            TempData["SuccessMessage"] = "YouTube video added successfully.";
            return RedirectToAction("Details", "Library", new { id = media.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving YouTube video");
            TempData["ErrorMessage"] = $"Error saving YouTube video: {ex.Message}";
            return RedirectToAction("Search");
        }
    }

    // GET: /Media/YouTube/Channel/{channelId}
    [HttpGet("Channel/{channelId}")]
    public async Task<IActionResult> Channel(string channelId)
    {
        if (string.IsNullOrEmpty(channelId))
        {
            return NotFound();
        }

        try
        {
            // 1. Get channel details and videos from YouTubeMediaService
            var viewModel = await _youTubeMediaService.GetChannelDetailsAsync(channelId);

            if (viewModel == null)
            {
                return NotFound("Channel not found or unavailable");
            }

            // 2. Check if user has already added videos from this channel
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userVideosFromChannel = await _youTubeMediaService.GetUserMediaFromChannelAsync(userId, channelId);

            viewModel.UserVideosFromChannel = userVideosFromChannel.ToList();

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving YouTube channel {ChannelId}", channelId);
            ModelState.AddModelError(string.Empty, $"Error retrieving channel: {ex.Message}");

            // Return a basic view model with just the channel ID
            return View(new YouTubeChannelViewModel { ChannelId = channelId });
        }
    }
}
// View Models
public class YouTubeSearchViewModel
{
    [Display(Name = "Search Query")]
    [Required(ErrorMessage = "Please enter a search term")]
    public string Query { get; set; } = string.Empty;

    [Display(Name = "Max Results")]
    [Range(1, 50, ErrorMessage = "Please enter a value between 1 and 50")]
    public int MaxResults { get; set; } = 10;

    public List<YouTubeVideoViewModel> SearchResults { get; set; } = new();

    public List<string> PopularCategories { get; set; } = new();

    public List<ShareSmallBiz.Portal.Data.Media> RecentlyAdded { get; set; } = new();
}

public class YouTubeVideoViewModel
{
    public string VideoId { get; set; } = string.Empty;

    [Required(ErrorMessage = "Title is required")]
    [StringLength(255, ErrorMessage = "Title cannot exceed 255 characters")]
    public string Title { get; set; } = string.Empty;

    [StringLength(512, ErrorMessage = "Description cannot exceed 512 characters")]
    public string Description { get; set; } = string.Empty;

    public string ThumbnailUrl { get; set; } = string.Empty;

    public DateTime PublishedAt { get; set; }

    public string ChannelId { get; set; } = string.Empty;

    public string ChannelTitle { get; set; } = string.Empty;
}

public class YouTubeChannelViewModel
{
    public string ChannelId { get; set; } = string.Empty;

    public string ChannelTitle { get; set; } = string.Empty;

    public string ChannelDescription { get; set; } = string.Empty;

    public string ThumbnailUrl { get; set; } = string.Empty;

    public long SubscriberCount { get; set; }

    public long VideoCount { get; set; }

    public long ViewCount { get; set; }

    public List<YouTubeVideoViewModel> Videos { get; set; } = new();

    public List<ShareSmallBiz.Portal.Data.Media> UserVideosFromChannel { get; set; } = new();
}