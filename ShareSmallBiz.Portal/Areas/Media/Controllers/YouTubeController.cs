using ShareSmallBiz.Portal.Areas.Media.Services;
using ShareSmallBiz.Portal.Data;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ShareSmallBiz.Portal.Data.Enums;
using ShareSmallBiz.Portal.Areas.Media.Models;

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
    private readonly YouTubeService _youTubeService;

    public YouTubeController(
        ShareSmallBizUserContext context,
        ILogger<YouTubeController> logger,
        YouTubeMediaService youTubeMediaService,
        MediaService mediaService,
        MediaFactoryService mediaFactoryService,
        YouTubeService youTubeService)
    {
        _context = context;
        _logger = logger;
        _youTubeMediaService = youTubeMediaService;
        _mediaService = mediaService;
        _mediaFactoryService = mediaFactoryService;
        _youTubeService = youTubeService;
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
            "presentation skills",
            "customer service tips",
            "sales techniques"
        };

        // Get any recently added YouTube videos (for the "Recently Added" section)
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var recentYouTubeMedia = await _youTubeMediaService.GetRecentlyAddedVideosAsync(userId, 4);

        viewModel.RecentlyAdded = recentYouTubeMedia.ToList();

        // Get popular channels if available
        try
        {
            var popularChannels = await _context.Media
                .Where(m => m.UserId == userId && m.StorageProvider == StorageProviderNames.YouTube)
                .Where(m => !string.IsNullOrEmpty(m.StorageMetadata))
                .OrderByDescending(m => m.CreatedDate)
                .Take(20)
                .ToListAsync();

            // Extract unique channels
            var channels = new Dictionary<string, (string ChannelId, string ChannelTitle, int Count)>();

            foreach (var media in popularChannels)
            {
                try
                {
                    var metadata = JsonSerializer.Deserialize<Dictionary<string, string>>(media.StorageMetadata);
                    if (metadata != null && metadata.TryGetValue("channelId", out string channelId) &&
                        metadata.TryGetValue("channelTitle", out string channelTitle))
                    {
                        if (channels.TryGetValue(channelId, out var existing))
                        {
                            channels[channelId] = (channelId, channelTitle, existing.Count + 1);
                        }
                        else
                        {
                            channels[channelId] = (channelId, channelTitle, 1);
                        }
                    }
                }
                catch
                {
                    // Skip this media
                }
            }

            // Get top 4 most frequently used channels
            viewModel.PopularChannels = channels.Values
                .OrderByDescending(c => c.Count)
                .Take(4)
                .Select(c => new YouTubeChannelListItemViewModel
                {
                    ChannelId = c.ChannelId,
                    ChannelTitle = c.ChannelTitle,
                    VideoCount = c.Count
                })
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving popular channels");
        }

        return View(viewModel);
    }

    // GET: /Media/YouTube/Search
    [HttpGet("Search")]
    public async Task<IActionResult> Search(string? query = "", int pageNumber = 1, int maxResults = 12)
    {
        var viewModel = new YouTubeSearchViewModel();
        if (string.IsNullOrEmpty(query))
        {
            return View(viewModel);
        }

        viewModel.Query = query;
        viewModel.MaxResults = maxResults;
        viewModel.Page = pageNumber; // Set the page property with the pageNumber parameter

        try
        {
            var searchResults = await _youTubeMediaService.SearchVideosAsync(
                viewModel.Query,
                viewModel.MaxResults > 0 ? viewModel.MaxResults : 12);

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
                viewModel.MaxResults > 0 ? viewModel.MaxResults : 12);

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

    // GET: /Media/YouTube/Video/{videoId}
    [HttpGet("Video/{videoId}")]
    public async Task<IActionResult> Video(string videoId)
    {
        if (string.IsNullOrEmpty(videoId))
        {
            return NotFound();
        }

        try
        {
            // Get video details
            var videoDetails = await _youTubeMediaService.GetVideoDetailsAsync(videoId);
            if (videoDetails == null)
            {
                return NotFound();
            }

            // Check if user already has this video in their library
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var existingMedia = await _context.Media
                .Where(m => m.UserId == userId && m.StorageProvider == StorageProviderNames.YouTube)
                .Where(m => m.StorageMetadata.Contains(videoId))
                .FirstOrDefaultAsync();

            if (existingMedia != null)
            {
                ViewBag.ExistingMediaId = existingMedia.Id;
            }

            return View(videoDetails);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving YouTube video {VideoId}", videoId);
            TempData["ErrorMessage"] = "Error retrieving video details.";
            return RedirectToAction("Search");
        }
    }

    // GET: /Media/YouTube/Channel/{channelId}
    [HttpGet("Channel/{channelId}")]
    public async Task<IActionResult> Channel(string channelId, int pageNumber = 1, int pageSize = 12)
    {
        if (string.IsNullOrEmpty(channelId))
        {
            return NotFound();
        }

        try
        {
            // 1. Get channel details and videos from YouTubeMediaService
            var viewModel = await _youTubeMediaService.GetChannelDetailsAsync(channelId, pageSize);

            if (viewModel == null)
            {
                return NotFound("Channel not found or unavailable");
            }

            // 2. Check if user has already added videos from this channel
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            IEnumerable<MediaModel> userVideosFromChannel = await _youTubeMediaService.GetUserMediaFromChannelAsync(userId, channelId);

            // Add the user's videos to the viewModel
            viewModel.UserVideosFromChannel = userVideosFromChannel.ToList();

            // Set the page number - using CurrentPage property instead of Page to avoid method group error
            viewModel.CurrentPage = pageNumber;

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

    // POST: /Media/YouTube/SaveChannel
    [HttpPost("SaveChannel")]
    public async Task<IActionResult> SaveChannel(string channelId, string channelTitle, string channelDescription)
    {
        if (string.IsNullOrEmpty(channelId) || string.IsNullOrEmpty(channelTitle))
        {
            return BadRequest("Channel ID and title are required");
        }

        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Create the media entry for the channel
            var media = await _youTubeMediaService.CreateYouTubeChannelMediaAsync(
                channelId,
                channelTitle,
                channelDescription,
                userId);

            TempData["SuccessMessage"] = "YouTube channel added successfully to your library.";
            return RedirectToAction("Details", "Library", new { id = media.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving YouTube channel");
            TempData["ErrorMessage"] = $"Error saving channel: {ex.Message}";
            return RedirectToAction("Channel", new { channelId });
        }
    }
}