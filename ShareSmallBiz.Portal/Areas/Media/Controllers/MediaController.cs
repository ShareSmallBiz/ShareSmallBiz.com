using Microsoft.AspNetCore.Mvc;
using ShareSmallBiz.Portal.Data;
using ShareSmallBiz.Portal.Services;
using ShareSmallBiz.Portal.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace ShareSmallBiz.Portal.Areas.Media.Controllers;

[Area("Media")]
[Route("[controller]")]
public class MediaController : Controller
{
    private readonly ShareSmallBizUserContext _context;
    private readonly MediaService _mediaService;
    private readonly ILogger<MediaController> _logger;
    private readonly MediaStorageOptions _mediaOptions;

    public MediaController(
        ShareSmallBizUserContext context,
        MediaService mediaService,
        ILogger<MediaController> logger,
        IOptions<MediaStorageOptions> mediaOptions)
    {
        _context = context;
        _mediaService = mediaService;
        _logger = logger;
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
            if (media.StorageProvider == StorageProviderNames.External ||
                media.StorageProvider == StorageProviderNames.YouTube)
            {
                return Redirect(media.Url);
            }

            // For local files, return the file stream
            var stream = await _mediaService.GetFileStreamAsync(media);

            // Explicitly set content type
            string contentType = !string.IsNullOrEmpty(media.ContentType)
                ? media.ContentType
                : GetContentTypeFromFileName(media.FileName);

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

    // Helper method to determine content type from file extension
    private string GetContentTypeFromFileName(string fileName)
    {
        string extension = Path.GetExtension(fileName).ToLowerInvariant();

        return extension switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".svg" => "image/svg+xml",
            ".mp4" => "video/mp4",
            ".webm" => "video/webm",
            ".mp3" => "audio/mpeg",
            ".wav" => "audio/wav",
            ".pdf" => "application/pdf",
            ".doc" or ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".xls" or ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".txt" => "text/plain",
            _ => "application/octet-stream"  // Default binary content type
        };
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
            var stream = await _mediaService.GetThumbnailStreamAsync(media, thumbnailSize, thumbnailSize);
            return File(stream, "image/jpeg");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving thumbnail for media with ID {MediaId}", id);
            return StatusCode(500, "Error retrieving thumbnail");
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