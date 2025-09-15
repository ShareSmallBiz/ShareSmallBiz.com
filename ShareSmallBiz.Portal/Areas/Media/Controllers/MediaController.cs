using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ShareSmallBiz.Portal.Areas.Media.Models;
using ShareSmallBiz.Portal.Areas.Media.Services;
using ShareSmallBiz.Portal.Data.Enums;
using ShareSmallBiz.Portal.Infrastructure.Configuration;
using System.Security.Claims;
using System.Text.Json;

namespace ShareSmallBiz.Portal.Areas.Media.Controllers;

[Area("Media")]
[Route("[controller]")]
public class MediaController : Controller
{
    private readonly ILogger<MediaController> _logger;
    private readonly MediaService _mediaService;
    private readonly StorageProviderService _storageProviderService;
    private readonly MediaFactoryService _mediaFactoryService;
    private readonly YouTubeService _youTubeService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly MediaStorageOptions _mediaOptions;

    public MediaController(
        ILogger<MediaController> logger,
        MediaService mediaService,
        StorageProviderService storageProviderService,
        MediaFactoryService mediaFactoryService,
        YouTubeService youTubeService,
        IHttpClientFactory httpClientFactory,
        IOptions<MediaStorageOptions> mediaOptions)
    {
        _logger = logger;
        _mediaService = mediaService;
        _storageProviderService = storageProviderService;
        _mediaFactoryService = mediaFactoryService;
        _youTubeService = youTubeService;
        _httpClientFactory = httpClientFactory;
        _mediaOptions = mediaOptions.Value;
    }

    // GET: /Media/{id}
    [HttpGet("{id:int}")]
    [ResponseCache(Duration = 86400)] // Cache for 24 hours
    public async Task<IActionResult> Index(int id)
    {
        try
        {
            var media = await _mediaService.GetMediaByIdAsync(id);

            if (media == null)
            {
                _logger.LogWarning("Media with ID {MediaId} not found", id);
                return NotFound();
            }

            // For external links, redirect to the external URL (guard against null/empty Url)
            if (media.StorageProvider == StorageProviderNames.External)
            {
                if (string.IsNullOrWhiteSpace(media.Url))
                {
                    _logger.LogWarning("External media {MediaId} has no URL", media.Id);
                    return NotFound();
                }
                return Redirect(media.Url);
            }

            // For YouTube videos, redirect to the YouTube URL (guard against null/empty Url)
            if (media.StorageProvider == StorageProviderNames.YouTube)
            {
                if (string.IsNullOrWhiteSpace(media.Url))
                {
                    _logger.LogWarning("YouTube media {MediaId} has no URL", media.Id);
                    return NotFound();
                }
                return Redirect(media.Url);
            }

            // For local files, return the file stream
            var stream = await _mediaFactoryService.GetFileStreamAsync(media);

            // Explicitly set content type
            string contentType = !string.IsNullOrEmpty(media.ContentType)
                ? media.ContentType
                : _storageProviderService.GetContentTypeFromFileName(media.FileName);

            // Determine if this content should be displayed inline or as an attachment
            var contentDisposition = media.MediaType == MediaType.Image ||
                                    media.MediaType == MediaType.Video ||
                                    media.MediaType == MediaType.Audio
                ? "inline"
                : "attachment; filename=" + media.FileName;

            // Use indexer assignment instead of Add to avoid duplicate header exceptions and analyzer warning ASP0019
            Response.Headers["Content-Disposition"] = contentDisposition;

            // Return the file with the appropriate content type
            return File(stream, contentType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving media with ID {MediaId}", id);
            return StatusCode(500, "Error retrieving media");
        }
    }

    // GET: /Media/Thumbnail/{id}
    [HttpGet("thumbnail/{id:int}")]
    [ResponseCache(Duration = 86400)] // Cache for 24 hours
    public async Task<IActionResult> Thumbnail(int id, string size = "sm")
    {
        try
        {
            var media = await _mediaService.GetMediaByIdAsync(id);

            if (media == null)
            {
                return NotFound();
            }

            // Get thumbnail size from options
            int thumbnailSize = _mediaOptions.DefaultThumbnailSize;
            if (_mediaOptions.ThumbnailSizes.TryGetValue(size, out int requestedSize))
            {
                thumbnailSize = requestedSize;
            }

            // For YouTube videos, fetch and return the thumbnail (validate Url if needed by downstream logic)
            if (media.StorageProvider == StorageProviderNames.YouTube)
            {
                if (string.IsNullOrWhiteSpace(media.Url))
                {
                    _logger.LogWarning("YouTube media {MediaId} has no URL for thumbnail", media.Id);
                    return GetMediaTypeIcon(MediaType.Video);
                }
                return await GetYouTubeThumbnailAsync(media);
            }

            // For non-image media, return appropriate icons
            if (media.MediaType != MediaType.Image)
            {
                return GetMediaTypeIcon(media.MediaType);
            }

            // For external links to images, redirect (guard against null/empty Url)
            if (media.StorageProvider == StorageProviderNames.External && media.MediaType == MediaType.Image)
            {
                if (string.IsNullOrWhiteSpace(media.Url))
                {
                    _logger.LogWarning("External image media {MediaId} has no URL", media.Id);
                    return NotFound();
                }
                return Redirect(media.Url);
            }

            // For local files, return the thumbnail stream
            var stream = await _mediaFactoryService.GetThumbnailStreamAsync(media, thumbnailSize, thumbnailSize);
            return File(stream, "image/jpeg");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving thumbnail for media with ID {MediaId}", id);
            return StatusCode(500, "Error retrieving thumbnail");
        }
    }

    private async Task<IActionResult> GetYouTubeThumbnailAsync(MediaModel media)
    {
        try
        {
            // Extract video ID from media URL or metadata
            string videoId = string.Empty;

            // First try to extract from metadata
            if (!string.IsNullOrEmpty(media.StorageMetadata))
            {
                try
                {
                    var metadataObj = JsonSerializer.Deserialize<Dictionary<string, string>>(media.StorageMetadata);
                    if (metadataObj != null && metadataObj.TryGetValue("videoId", out string id))
                    {
                        videoId = id;
                    }
                }
                catch (JsonException) { /* Ignore if metadata is not valid JSON */ }
            }

            // If not found in metadata, extract from URL
            if (string.IsNullOrEmpty(videoId))
            {
                videoId = _storageProviderService.ExtractYouTubeVideoId(media.Url);
            }

            if (string.IsNullOrEmpty(videoId))
            {
                // Fallback to default icon if we can't determine the video ID
                return GetMediaTypeIcon(MediaType.Video);
            }

            // Construct YouTube thumbnail URL
            // Use maxresdefault first, and if that fails, fall back to mqdefault
            string thumbnailUrl = $"https://img.youtube.com/vi/{videoId}/maxresdefault.jpg";

            // Create HTTP client and fetch the thumbnail
            var httpClient = _httpClientFactory.CreateClient();
            var response = await httpClient.GetAsync(thumbnailUrl);

            // If maxresdefault doesn't exist, try mqdefault
            if (!response.IsSuccessStatusCode)
            {
                thumbnailUrl = $"https://img.youtube.com/vi/{videoId}/mqdefault.jpg";
                response = await httpClient.GetAsync(thumbnailUrl);

                // If that also fails, fall back to default image
                if (!response.IsSuccessStatusCode)
                {
                    return GetMediaTypeIcon(MediaType.Video);
                }
            }

            // Read the image data
            var imageData = await response.Content.ReadAsByteArrayAsync();

            // Return the image
            return File(imageData, "image/jpeg");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving YouTube thumbnail for media {MediaId}", media.Id);
            // Fallback to media type icon
            return GetMediaTypeIcon(MediaType.Video);
        }
    }

    private IActionResult GetMediaTypeIcon(MediaType mediaType)
    {
        var iconName = mediaType switch
        {
            MediaType.Video => "video-icon.png",
            MediaType.Audio => "audio-icon.png",
            MediaType.Document => "document-icon.png",
            _ => "file-icon.png"
        };

        var iconPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", iconName);

        if (System.IO.File.Exists(iconPath))
        {
            return PhysicalFile(iconPath, "image/png");
        }

        // Fallback to default placeholder
        iconPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "placeholder.png");

        if (System.IO.File.Exists(iconPath))
        {
            return PhysicalFile(iconPath, "image/png");
        }

        return NotFound();
    }


    /// <summary>
    /// Get the user's media library
    /// </summary>
    [HttpGet("UserMedia")]
    public async Task<ActionResult<IEnumerable<MediaModel>>> GetUserMedia(string? searchTerm = null, string? mediaType = null)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User not authenticated");
            }

            // Get all media for user
            var allMedia = await _mediaService.GetUserMediaAsync(userId);

            // Apply search filter
            if (!string.IsNullOrEmpty(searchTerm))
            {
                allMedia = allMedia.Where(m =>
                    m.FileName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    (m.Description != null && m.Description.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)));
            }

            // Apply media type filter
            if (!string.IsNullOrEmpty(mediaType) && int.TryParse(mediaType, out int mediaTypeInt))
            {
                var mediaTypeEnum = (MediaType)mediaTypeInt;
                allMedia = allMedia.Where(m => m.MediaType == mediaTypeEnum);
            }

            // Order by most recently added
            return Ok(allMedia.OrderByDescending(m => m.CreatedDate));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user media");
            return StatusCode(500, "An error occurred while retrieving media");
        }
    }

    /// <summary>
    /// Get media by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<MediaModel>> GetMedia(int id)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User not authenticated");
            }

            var media = await _mediaService.GetUserMediaByIdAsync(id, userId);
            if (media == null)
            {
                return NotFound();
            }

            return Ok(media);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving media {Id}", id);
            return StatusCode(500, "An error occurred while retrieving media");
        }
    }

    /// <summary>
    /// Link media to a post
    /// </summary>
    [HttpPost("LinkToPost")]
    public async Task<IActionResult> LinkMediaToPost([FromBody] LinkMediaRequest request)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User not authenticated");
            }

            if (request.MediaId <= 0 || request.PostId <= 0)
            {
                return BadRequest("Invalid media or post ID");
            }

            var result = await _mediaService.LinkMediaToPostAsync(request.MediaId, request.PostId, userId);
            if (!result)
            {
                return BadRequest("Failed to link media to post");
            }

            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error linking media {MediaId} to post {PostId}",
                request?.MediaId, request?.PostId);
            return StatusCode(500, "An error occurred while linking media to post");
        }
    }

    /// <summary>
    /// Unlink media from a post
    /// </summary>
    [HttpPost("UnlinkFromPost")]
    public async Task<IActionResult> UnlinkMediaFromPost([FromBody] LinkMediaRequest request)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User not authenticated");
            }

            if (request.MediaId <= 0 || request.PostId <= 0)
            {
                return BadRequest("Invalid media or post ID");
            }

            var result = await _mediaService.UnlinkMediaFromPostAsync(request.MediaId, request.PostId, userId);
            if (!result)
            {
                return BadRequest("Failed to unlink media from post");
            }

            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unlinking media {MediaId} from post {PostId}",
                request?.MediaId, request?.PostId);
            return StatusCode(500, "An error occurred while unlinking media from post");
        }
    }


}



/// <summary>
/// Request model for linking media to posts
/// </summary>
public class LinkMediaRequest
{
    public int MediaId { get; set; }
    public int PostId { get; set; }
}