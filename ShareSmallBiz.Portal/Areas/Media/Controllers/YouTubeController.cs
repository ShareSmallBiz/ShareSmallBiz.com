using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ShareSmallBiz.Portal.Data;
using ShareSmallBiz.Portal.Services;
using ShareSmallBiz.Portal.Infrastructure.Configuration;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ShareSmallBiz.Portal.Areas.Media.Controllers;

[Area("Media")]
[Authorize]
[Route("Media/YouTube")]
public class YouTubeController : Controller
{
    private readonly ShareSmallBizUserContext _context;
    private readonly StorageProviderService _storageProviderService;
    private readonly ILogger<YouTubeController> _logger;
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _YouTubeApiKey;

    public YouTubeController(
        ShareSmallBizUserContext context,
        StorageProviderService storageProviderService,
        ILogger<YouTubeController> logger,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory)
    {
        _context = context;
        _storageProviderService = storageProviderService;
        _logger = logger;
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
        _YouTubeApiKey = _configuration["GOOGLE_YOUTUBE_API_KEY"] ?? throw new ArgumentNullException("GOOGLE_YOUTUBE_API_KEY is not configured");
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
            // Build the YouTube API request
            var searchQuery = Uri.EscapeDataString(viewModel.Query);
            var maxResults = viewModel.MaxResults > 0 ? viewModel.MaxResults : 10;
            var requestUrl = $"https://www.googleapis.com/youtube/v3/search?part=snippet&q={searchQuery}&maxResults={maxResults}&type=video&key={_YouTubeApiKey}";

            // Create HttpClient and send request
            var httpClient = _httpClientFactory.CreateClient();
            var response = await httpClient.GetAsync(requestUrl);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadFromJsonAsync<YouTubeSearchResponse>();

                if (content != null && content.Items != null)
                {
                    viewModel.SearchResults = content.Items.Select(item => new YouTubeVideoViewModel
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
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("YouTube API error: {StatusCode} - {Error}", response.StatusCode, errorContent);
                ModelState.AddModelError("", $"Error from YouTube API: {response.StatusCode}");
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
            // Build the YouTube API request
            var searchQuery = Uri.EscapeDataString(viewModel.Query);
            var maxResults = viewModel.MaxResults > 0 ? viewModel.MaxResults : 10;
            var requestUrl = $"https://www.googleapis.com/youtube/v3/search?part=snippet&q={searchQuery}&maxResults={maxResults}&type=video&key={_YouTubeApiKey}";

            // Create HttpClient and send request
            var httpClient = _httpClientFactory.CreateClient();
            var response = await httpClient.GetAsync(requestUrl);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadFromJsonAsync<YouTubeSearchResponse>();

                if (content != null && content.Items != null)
                {
                    viewModel.SearchResults = content.Items.Select(item => new YouTubeVideoViewModel
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
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("YouTube API error: {StatusCode} - {Error}", response.StatusCode, errorContent);
                ModelState.AddModelError("", $"Error from YouTube API: {response.StatusCode}");
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

            // Create HttpClient for API requests
            var httpClient = _httpClientFactory.CreateClient();

            // 1. Get channel details
            var channelUrl = $"https://www.googleapis.com/youtube/v3/channels?part=snippet,statistics&id={channelId}&key={_YouTubeApiKey}";
            var channelResponse = await httpClient.GetAsync(channelUrl);

            if (channelResponse.IsSuccessStatusCode)
            {
                var channelContent = await channelResponse.Content.ReadFromJsonAsync<YouTubeChannelResponse>();
                if (channelContent != null && channelContent.Items != null && channelContent.Items.Any())
                {
                    var channelInfo = channelContent.Items[0];
                    viewModel.ChannelTitle = channelInfo.Snippet.Title;
                    viewModel.ChannelDescription = channelInfo.Snippet.Description;
                    viewModel.ThumbnailUrl = channelInfo.Snippet.Thumbnails.Medium?.Url ?? channelInfo.Snippet.Thumbnails.Default.Url;
                    viewModel.SubscriberCount = channelInfo.Statistics.SubscriberCount;
                    viewModel.VideoCount = channelInfo.Statistics.VideoCount;
                    viewModel.ViewCount = channelInfo.Statistics.ViewCount;
                }
            }

            // 2. Get channel videos
            var videosUrl = $"https://www.googleapis.com/youtube/v3/search?part=snippet&channelId={channelId}&maxResults=12&order=date&type=video&key={apiKey}";
            var videosResponse = await httpClient.GetAsync(videosUrl);

            if (videosResponse.IsSuccessStatusCode)
            {
                var videosContent = await videosResponse.Content.ReadFromJsonAsync<YouTubeSearchResponse>();
                if (videosContent != null && videosContent.Items != null)
                {
                    viewModel.Videos = videosContent.Items.Select(item => new YouTubeVideoViewModel
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

// JSON response models for YouTube API
public class YouTubeSearchResponse
{
    [JsonPropertyName("items")]
    public List<YouTubeSearchItem> Items { get; set; } = new();

    [JsonPropertyName("nextPageToken")]
    public string NextPageToken { get; set; } = string.Empty;

    [JsonPropertyName("prevPageToken")]
    public string PrevPageToken { get; set; } = string.Empty;

    [JsonPropertyName("pageInfo")]
    public YouTubePageInfo PageInfo { get; set; } = new();
}

public class YouTubePageInfo
{
    [JsonPropertyName("totalResults")]
    public int TotalResults { get; set; }

    [JsonPropertyName("resultsPerPage")]
    public int ResultsPerPage { get; set; }
}

public class YouTubeSearchItem
{
    [JsonPropertyName("id")]
    public YouTubeVideoId Id { get; set; } = new();

    [JsonPropertyName("snippet")]
    public YouTubeSnippet Snippet { get; set; } = new();
}

public class YouTubeVideoId
{
    [JsonPropertyName("videoId")]
    public string VideoId { get; set; } = string.Empty;
}

public class YouTubeSnippet
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("publishedAt")]
    public DateTime PublishedAt { get; set; }

    [JsonPropertyName("thumbnails")]
    public YouTubeThumbnails Thumbnails { get; set; } = new();

    [JsonPropertyName("channelId")]
    public string ChannelId { get; set; } = string.Empty;

    [JsonPropertyName("channelTitle")]
    public string ChannelTitle { get; set; } = string.Empty;
}

public class YouTubeThumbnails
{
    [JsonPropertyName("default")]
    public YouTubeThumbnail Default { get; set; } = new();

    [JsonPropertyName("medium")]
    public YouTubeThumbnail Medium { get; set; } = new();

    [JsonPropertyName("high")]
    public YouTubeThumbnail High { get; set; } = new();
}

public class YouTubeThumbnail
{
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("width")]
    public int Width { get; set; }

    [JsonPropertyName("height")]
    public int Height { get; set; }
}

public class YouTubeChannelResponse
{
    [JsonPropertyName("items")]
    public List<YouTubeChannelItem> Items { get; set; } = new();
}

public class YouTubeChannelItem
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("snippet")]
    public YouTubeSnippet Snippet { get; set; } = new();

    [JsonPropertyName("statistics")]
    public YouTubeChannelStatistics Statistics { get; set; } = new();
}

public class YouTubeChannelStatistics
{
    [JsonPropertyName("viewCount")]
    public long ViewCount { get; set; }

    [JsonPropertyName("subscriberCount")]
    public long SubscriberCount { get; set; }

    [JsonPropertyName("hiddenSubscriberCount")]
    public bool HiddenSubscriberCount { get; set; }

    [JsonPropertyName("videoCount")]
    public long VideoCount { get; set; }
}