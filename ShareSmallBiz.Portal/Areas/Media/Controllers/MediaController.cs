using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ShareSmallBiz.Portal.Areas.Media.Models;
using ShareSmallBiz.Portal.Areas.Media.Services;
using ShareSmallBiz.Portal.Data.Enums;
using ShareSmallBiz.Portal.Infrastructure.Configuration;
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

            // For external links, redirect to the external URL
            if (media.StorageProvider == StorageProviderNames.External)
            {
                return Redirect(media.Url);
            }

            // For YouTube videos, redirect to the YouTube URL
            if (media.StorageProvider == StorageProviderNames.YouTube)
            {
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

            Response.Headers.Add("Content-Disposition", contentDisposition);

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

            // For YouTube videos, fetch and return the thumbnail
            if (media.StorageProvider == StorageProviderNames.YouTube)
            {
                return await GetYouTubeThumbnailAsync(media);
            }

            // For non-image media, return appropriate icons
            if (media.MediaType != MediaType.Image)
            {
                return await GetMediaTypeIconAsync(media.MediaType);
            }

            // For external links to images, redirect
            if (media.StorageProvider == StorageProviderNames.External && media.MediaType == MediaType.Image)
            {
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
                return await GetMediaTypeIconAsync(MediaType.Video);
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
                    return await GetMediaTypeIconAsync(MediaType.Video);
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
            return await GetMediaTypeIconAsync(MediaType.Video);
        }
    }

    private async Task<IActionResult> GetMediaTypeIconAsync(MediaType mediaType)
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
}