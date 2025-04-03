using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ShareSmallBiz.Portal.Data;
using ShareSmallBiz.Portal.Infrastructure.Configuration;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using ShareSmallBiz.Portal.Areas.Media.Services;

namespace ShareSmallBiz.Portal.Areas.Media.Controllers;

[Area("Media")]
[Authorize]
[Route("Media/YouTube")]
public class YouTubeController : Controller
{
    private readonly ShareSmallBizUserContext _context;
    private readonly StorageProviderService _storageProviderService;
    private readonly ILogger<YouTubeController> _logger;
    private readonly YouTubeService _youTubeService;

    public YouTubeController(
        ShareSmallBizUserContext context,
        StorageProviderService storageProviderService,
        ILogger<YouTubeController> logger,
        YouTubeService youTubeService)
    {
        _context = context;
        _storageProviderService = storageProviderService;
        _logger = logger;
        _youTubeService = youTubeService;
    }

    // GET: /Media/YouTube
    [HttpGet]
    public IActionResult Index()
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
        var recentYouTubeMedia = _context.Media
            .Where(m => m.UserId == userId && m.StorageProvider == StorageProviderNames.YouTube)
            .OrderByDescending(m => m.CreatedDate)
            .Take(4)
            .ToList();

        viewModel.RecentlyAdded = recentYouTubeMedia;

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
            var searchResponse = await _youTubeService.SearchVideosAsync(
                viewModel.Query,
                viewModel.MaxResults > 0 ? viewModel.MaxResults : 10);

            if (searchResponse != null && searchResponse.Items != null)
            {
                viewModel.SearchResults = searchResponse.Items.Select(item => new YouTubeVideoViewModel
                {
                    VideoId = item.Id.VideoId,
                    Title = item.Snippet.Title,
                    Description = item.Snippet.Description,
                    ThumbnailUrl = item.Snippet.Thumbnails.Medium.Url,
                    PublishedAt = item.Snippet.PublishedAt,
                    ChannelId = item.Snippet.ChannelId,
                    ChannelTitle = item.Snippet.ChannelTitle
                }).ToList();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching YouTube");
            ModelState.AddModelError("", $"Error searching YouTube: {ex.Message}");
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
            var searchResponse = await _youTubeService.SearchVideosAsync(
                viewModel.Query,
                viewModel.MaxResults > 0 ? viewModel.MaxResults : 10);

            if (searchResponse != null && searchResponse.Items != null)
            {
                viewModel.SearchResults = searchResponse.Items.Select(item => new YouTubeVideoViewModel
                {
                    VideoId = item.Id.VideoId,
                    Title = item.Snippet.Title,
                    Description = item.Snippet.Description,
                    ThumbnailUrl = item.Snippet.Thumbnails.Medium.Url,
                    PublishedAt = item.Snippet.PublishedAt,
                    ChannelId = item.Snippet.ChannelId,
                    ChannelTitle = item.Snippet.ChannelTitle
                }).ToList();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching YouTube");
            ModelState.AddModelError("", $"Error searching YouTube: {ex.Message}");
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

            // Create the media entry
            var media = await _storageProviderService.CreateExternalLinkAsync(
                youtubeUrl,
                viewModel.Title,
                MediaType.Video, // Force video type for YouTube
                userId,
                viewModel.ChannelTitle, // Use channel title as attribution
                description
            );

            // Update the storage provider to YouTube 
            // (in case CreateExternalLinkAsync didn't set it correctly)
            media.StorageProvider = StorageProviderNames.YouTube;

            // Add channel ID to metadata if available
            if (!string.IsNullOrEmpty(viewModel.ChannelId))
            {
                media.StorageMetadata = $"channelId:{viewModel.ChannelId}";
            }

            await _context.SaveChangesAsync();

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

        var viewModel = new YouTubeChannelViewModel
        {
            ChannelId = channelId
        };

        try
        {
            // 1. Get channel details
            var channelResponse = await _youTubeService.GetChannelDetailsAsync(channelId);

            if (channelResponse != null && channelResponse.Items != null && channelResponse.Items.Any())
            {
                var channelInfo = channelResponse.Items[0];
                viewModel.ChannelTitle = channelInfo.Snippet.Title;
                viewModel.ChannelDescription = channelInfo.Snippet.Description;
                viewModel.ThumbnailUrl = channelInfo.Snippet.Thumbnails.Medium?.Url ?? channelInfo.Snippet.Thumbnails.Default.Url;
                viewModel.SubscriberCount = channelInfo.Statistics.SubscriberCount;
                viewModel.VideoCount = channelInfo.Statistics.VideoCount;
                viewModel.ViewCount = channelInfo.Statistics.ViewCount;
            }

            // 2. Get channel videos
            var videosResponse = await _youTubeService.GetChannelVideosAsync(channelId);

            if (videosResponse != null && videosResponse.Items != null)
            {
                viewModel.Videos = videosResponse.Items.Select(item => new YouTubeVideoViewModel
                {
                    VideoId = item.Id.VideoId,
                    Title = item.Snippet.Title,
                    Description = item.Snippet.Description,
                    ThumbnailUrl = item.Snippet.Thumbnails.Medium.Url,
                    PublishedAt = item.Snippet.PublishedAt,
                    ChannelId = item.Snippet.ChannelId,
                    ChannelTitle = item.Snippet.ChannelTitle
                }).ToList();
            }

            // 3. Check if user has already added videos from this channel
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userVideosFromChannel = _context.Media
                .Where(m => m.UserId == userId &&
                       m.StorageProvider == StorageProviderNames.YouTube &&
                       m.StorageMetadata.Contains(channelId))
                .ToList();

            viewModel.UserVideosFromChannel = userVideosFromChannel;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving YouTube channel {ChannelId}", channelId);
            ModelState.AddModelError("", $"Error retrieving channel: {ex.Message}");
        }

        return View(viewModel);
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