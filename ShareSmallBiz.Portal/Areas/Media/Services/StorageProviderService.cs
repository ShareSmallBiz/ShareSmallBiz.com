using Microsoft.Extensions.Options;
using ShareSmallBiz.Portal.Areas.Media.Models;
using ShareSmallBiz.Portal.Data.Enums;
using ShareSmallBiz.Portal.Infrastructure.Configuration;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace ShareSmallBiz.Portal.Areas.Media.Services;

/// <summary>
/// Service for file operations and external storage providers
/// </summary>
public class StorageProviderService
{
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<StorageProviderService> _logger;
    private readonly MediaStorageOptions _mediaOptions;
    private readonly string _mediaRootPath;


    public StorageProviderService(
        IConfiguration configuration,
        ILogger<StorageProviderService> logger,
        IWebHostEnvironment environment,
        IOptions<MediaStorageOptions> mediaOptions)
    {
        _configuration = configuration;
        _logger = logger;
        _environment = environment;
        _mediaOptions = mediaOptions.Value;

        // Get media path from configuration, or use default
        _mediaRootPath = _configuration["MediaStorage:RootPath"] ??
            Path.Combine("c:", "websites", "sharesmallbiz", "media");

        // Ensure media directory exists
        EnsureDirectoryExists(_mediaRootPath);
        EnsureDirectoryExists(Path.Combine(_mediaRootPath, "thumbnails"));
        EnsureDirectoryExists(Path.Combine(_mediaRootPath, "profiles"));
    }

    /// <summary>
    /// Creates a thumbnail for an image
    /// </summary>
    public async Task<string> CreateThumbnailAsync(string imagePath, int width = 200, int height = 200)
    {
        try
        {
            // Generate thumbnail filename
            string thumbFileName = $"thumb_{width}x{height}_{Path.GetFileName(imagePath)}";
            string thumbDirectory = Path.Combine(_mediaRootPath, "thumbnails");
            string thumbPath = Path.Combine(thumbDirectory, thumbFileName);

            // Ensure the thumbnails directory exists
            EnsureDirectoryExists(thumbDirectory);

            // Check if thumbnail already exists
            if (File.Exists(thumbPath))
            {
                return thumbPath;
            }

            // Load the original image
            using (var originalImage = Image.FromFile(imagePath))
            {
                // Calculate dimensions while maintaining aspect ratio
                int newWidth, newHeight;
                if (originalImage.Width > originalImage.Height)
                {
                    newWidth = width;
                    newHeight = (int)(originalImage.Height * ((float)width / originalImage.Width));
                }
                else
                {
                    newHeight = height;
                    newWidth = (int)(originalImage.Width * ((float)height / originalImage.Height));
                }

                // Create the thumbnail
                using (var thumbnail = new Bitmap(newWidth, newHeight))
                {
                    using (var graphics = Graphics.FromImage(thumbnail))
                    {
                        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        graphics.SmoothingMode = SmoothingMode.HighQuality;
                        graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                        graphics.CompositingQuality = CompositingQuality.HighQuality;
                        graphics.DrawImage(originalImage, 0, 0, newWidth, newHeight);
                    }

                    // Save the thumbnail
                    thumbnail.Save(thumbPath, GetImageFormat(Path.GetExtension(imagePath)));
                }
            }

            return thumbPath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create thumbnail for image {ImagePath}", imagePath);
            return string.Empty;
        }
    }

    /// <summary>
    /// Deletes a file from the storage provider
    /// </summary>
    public async Task DeleteFileAsync(MediaModel media)
    {
        switch (media.StorageProvider)
        {
            case StorageProviderNames.LocalStorage:
                // Delete the physical file
                if (File.Exists(media.Url))
                {
                    File.Delete(media.Url);
                }

                // Delete any thumbnails
                string thumbDirectory = Path.Combine(_mediaRootPath, "thumbnails");
                string fileNameWithoutPath = Path.GetFileName(media.Url);
                var thumbFiles = Directory.GetFiles(thumbDirectory, $"thumb_*_{fileNameWithoutPath}");
                foreach (var thumbFile in thumbFiles)
                {
                    File.Delete(thumbFile);
                }
                break;
            case StorageProviderNames.External:
            case StorageProviderNames.YouTube:
                // Nothing to delete for external links
                break;
            default:
                throw new ArgumentException($"Unsupported storage provider: {media.StorageProvider}");
        }
    }

    /// <summary>
    /// Creates a media entity for an external link
    /// </summary>
    public string DetermineContentType(string externalUrl, MediaType mediaType)
    {
        if (string.IsNullOrEmpty(externalUrl))
            throw new ArgumentException("External URL cannot be null or empty", nameof(externalUrl));

        if (mediaType == MediaType.Image)
            return "image/jpeg"; // Default for images
        if (mediaType == MediaType.Video)
        {
            if (IsYouTubeUrl(externalUrl))
            {
                return "video/youtube";
            }
            return "video/mp4"; // Default for videos
        }
        if (mediaType == MediaType.Audio)
            return "audio/mpeg"; // Default for audio

        if (mediaType == MediaType.Document)
            return "application/pdf"; // Default for documents

        // For other types, return a generic content type
        return "application/octet-stream";
    }

    /// <summary>
    /// Extracts the video ID from a YouTube URL
    /// </summary>
    public string ExtractYouTubeVideoId(string url)
    {
        try
        {
            // Handle youtu.be short links
            if (url.Contains("youtu.be/"))
            {
                var uri = new Uri(url);
                return uri.AbsolutePath.TrimStart('/');
            }

            // Handle standard youtube.com URLs
            var videoIdParam = "v=";
            int startIndex = url.IndexOf(videoIdParam);

            if (startIndex >= 0)
            {
                startIndex += videoIdParam.Length;
                int endIndex = url.IndexOf('&', startIndex);
                return endIndex >= 0
                    ? url.Substring(startIndex, endIndex - startIndex)
                    : url.Substring(startIndex);
            }

            // Handle embed URLs
            if (url.Contains("/embed/") || url.Contains("/v/"))
            {
                var segments = new Uri(url).Segments;
                return segments[segments.Length - 1];
            }

            return string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// Gets the content type from a file name
    /// </summary>
    public string GetContentTypeFromFileName(string fileName)
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

    /// <summary>
    /// Gets the content type from a media type
    /// </summary>
    public string GetContentTypeFromMediaType(MediaType mediaType)
    {
        return mediaType switch
        {
            MediaType.Image => "image/jpeg",
            MediaType.Video => "video/mp4",
            MediaType.Audio => "audio/mpeg",
            MediaType.Document => "application/pdf",
            _ => "application/octet-stream"
        };
    }

    /// <summary>
    /// Gets the file stream for a media item
    /// </summary>
    public async Task<Stream> GetFileStreamAsync(MediaModel media)
    {
        return media.StorageProvider switch
        {
            StorageProviderNames.LocalStorage => await GetLocalFileStreamAsync(media.Url),
            StorageProviderNames.External or StorageProviderNames.YouTube =>
                throw new InvalidOperationException("Cannot get file stream for external links"),
            _ => throw new ArgumentException($"Unsupported storage provider: {media.StorageProvider}")
        };
    }
    public string GetMediaRootPath()
    {
        return _mediaRootPath;
    }

    /// <summary>
    /// Determines the media type from content type
    /// </summary>
    public MediaType GetMediaTypeFromContentType(string contentType)
    {
        if (contentType.StartsWith("image/"))
            return MediaType.Image;
        if (contentType.StartsWith("video/"))
            return MediaType.Video;
        if (contentType.StartsWith("audio/"))
            return MediaType.Audio;
        if (contentType.StartsWith("application/pdf") || contentType.StartsWith("application/msword") ||
            contentType.StartsWith("application/vnd.openxmlformats-officedocument"))
            return MediaType.Document;

        return MediaType.Other;
    }

    /// <summary>
    /// Gets a file stream for a thumbnail
    /// </summary>
    public async Task<Stream> GetThumbnailStreamAsync(MediaModel media, int width = 200, int height = 200)
    {
        if (media.MediaType != MediaType.Image || media.StorageProvider != StorageProviderNames.LocalStorage)
        {
            // Return default icon for non-image media
            string iconPath = Path.Combine(_environment.WebRootPath, "images", $"{media.MediaType.ToString().ToLower()}-icon.png");
            if (!File.Exists(iconPath))
            {
                iconPath = Path.Combine(_environment.WebRootPath, "images", "placeholder.png");
            }

            return new FileStream(iconPath, FileMode.Open, FileAccess.Read);
        }

        // Create thumbnail if it doesn't exist
        string thumbPath = await CreateThumbnailAsync(media.Url, width, height);
        if (string.IsNullOrEmpty(thumbPath) || !File.Exists(thumbPath))
        {
            // If thumbnail creation failed, return the original image
            return await GetFileStreamAsync(media);
        }

        return new FileStream(thumbPath, FileMode.Open, FileAccess.Read);
    }

    /// <summary>
    /// Gets the YouTube embed URL from a video ID
    /// </summary>
    public string GetYouTubeEmbedUrl(string videoId)
    {
        if (string.IsNullOrEmpty(videoId))
            return string.Empty;

        return $"https://www.youtube.com/embed/{videoId}";
    }

    /// <summary>
    /// Gets the YouTube embed URL from a video URL
    /// </summary>
    public string GetYouTubeEmbedUrlFromVideoUrl(string url)
    {
        string videoId = ExtractYouTubeVideoId(url);
        return GetYouTubeEmbedUrl(videoId);
    }

    /// <summary>
    /// Checks if a URL is a YouTube URL
    /// </summary>
    public bool IsYouTubeUrl(string url)
    {
        if (string.IsNullOrEmpty(url))
            return false;

        // Check various YouTube URL formats
        return url.Contains("youtube.com/watch") ||
               url.Contains("youtu.be/") ||
               url.Contains("youtube.com/embed/") ||
               url.Contains("youtube.com/v/");
    }

    /// <summary>
    /// Saves a profile picture as a file
    /// </summary>
    public async Task SaveProfilePictureAsync(byte[] profilePicture, MediaModel media)
    {
        string filePath = Path.Combine(_mediaRootPath, "profiles", media.FileName);

        // Ensure the profiles directory exists
        EnsureDirectoryExists(Path.GetDirectoryName(filePath));

        // Save the image to disk
        await File.WriteAllBytesAsync(filePath, profilePicture);

        // Update the URL in the media entity
        media.Url = filePath;
    }

    /// <summary>
    /// Uploads a file to the appropriate storage provider
    /// </summary>
    public async Task<string> UploadFileAsync(
        IFormFile file,
        StorageProviderNames provider,
        string fileName)
    {
        if (file == null || file.Length == 0)
        {
            throw new ArgumentException("File is null or empty", nameof(file));
        }

        // Validate file
        if (!await ValidateFileAsync(file))
        {
            throw new ArgumentException("Invalid file type or size", nameof(file));
        }

        // Upload file based on provider
        return provider switch
        {
            StorageProviderNames.LocalStorage => await UploadToLocalStorageAsync(file, fileName),
            _ => throw new ArgumentException($"Unsupported storage provider: {provider}")
        };
    }

    /// <summary>
    /// Validates a file for upload
    /// </summary>
    public async Task<bool> ValidateFileAsync(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return false;

        // Check file size against configured maximum
        if (file.Length > _mediaOptions.MaxFileSize)
            return false;

        // Check file extension against configured allowed extensions
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!_mediaOptions.AllowedExtensions.Contains(extension))
            return false;

        // Check file type (whitelist approach)
        var allowedTypes = new[] {
            "image/jpeg", "image/png", "image/gif", "image/webp", // Images
            "video/mp4", "video/webm", "video/ogg", // Videos
            "audio/mpeg", "audio/ogg", "audio/wav", // Audio
            "application/pdf", "application/msword", "application/vnd.openxmlformats-officedocument.wordprocessingml.document" // Documents
        };

        return Array.Exists(allowedTypes, type => type.Equals(file.ContentType, StringComparison.OrdinalIgnoreCase));
    }

    #region Private Helper Methods

    private void EnsureDirectoryExists(string directoryPath)
    {
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }
    }

    private async Task<string> UploadToLocalStorageAsync(IFormFile file, string fileName)
    {
        // Get upload path from configuration
        var uploadPath = _configuration["MediaStorage:UploadPath"]
            ?? Path.Combine(_mediaRootPath, "uploads");

        // Ensure directory exists
        EnsureDirectoryExists(uploadPath);

        // Create the full file path
        var filePath = Path.Combine(uploadPath, fileName);

        // Write the file
        using (var fileStream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(fileStream);
        }

        return filePath;
    }

    private async Task<Stream> GetLocalFileStreamAsync(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("File not found", filePath);
        }

        return new FileStream(filePath, FileMode.Open, FileAccess.Read);
    }

    private ImageFormat GetImageFormat(string extension)
    {
        extension = extension.ToLowerInvariant();
        return extension switch
        {
            ".jpg" or ".jpeg" => ImageFormat.Jpeg,
            ".png" => ImageFormat.Png,
            ".gif" => ImageFormat.Gif,
            ".bmp" => ImageFormat.Bmp,
            _ => ImageFormat.Jpeg  // Default to JPEG
        };
    }

    #endregion
}




public static class StorageProviderServiceExtensions
{
    /// <summary>
    /// Creates a media entity for an external image URL specifically for post covers
    /// </summary>
    /// <param name="service">The StorageProviderService instance</param>
    /// <param name="externalUrl">URL to the external image</param>
    /// <param name="fileName">File name to use for the media</param>
    /// <param name="mediaType">Type of media (usually Image for covers)</param>
    /// <param name="userId">User ID who is creating the post</param>
    /// <param name="attribution">Attribution for the image</param>
    /// <param name="description">Description for the media item</param>
    /// <param name="storageMetadata">Additional metadata in JSON format</param>
    /// <returns>The created MediaModel or null if creation failed</returns>
    public static async Task<MediaModel> CreateExternalMediaAsync(
        this StorageProviderService service,
        string externalUrl,
        string fileName,
        MediaType mediaType,
        string userId,
        string attribution = "",
        string description = "",
        string storageMetadata = "")
    {
        if (string.IsNullOrEmpty(externalUrl))
        {
            throw new ArgumentException("External URL cannot be null or empty", nameof(externalUrl));
        }

        // Determine content type
        string contentType = service.DetermineContentType(externalUrl, mediaType);

        // Create media entity
        MediaModel media = new()
        {
            FileName = fileName,
            ContentType = contentType,
            StorageProvider = StorageProviderNames.External,
            MediaType = mediaType,
            StorageMetadata = storageMetadata,
            Url = externalUrl,
            CreatedID = userId,
            CreatedDate = DateTime.UtcNow,
            ModifiedID = userId,
            ModifiedDate = DateTime.UtcNow,
            Attribution = attribution,
            Description = description,
            FileSize = 0 // No actual file size for external links
        };

        // Get MediaService to save to database
        var serviceProvider = service.GetServiceProvider();
        var mediaService = serviceProvider.GetRequiredService<MediaService>();

        // Save to database
        return await mediaService.CreateMediaAsync(media);
    }

    // Add extension method to get the service provider
    private static IServiceProvider GetServiceProvider(this StorageProviderService service)
    {
        // Use reflection to get access to the service provider
        var field = typeof(StorageProviderService).GetField("_serviceProvider",
            System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.Instance);

        if (field != null)
        {
            return (IServiceProvider)field.GetValue(service);
        }

        // Fallback: Try to get from the environment
        var serviceProviderProp = typeof(StorageProviderService).GetProperty("RequestServices",
            System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.Instance);

        if (serviceProviderProp != null)
        {
            return (IServiceProvider)serviceProviderProp.GetValue(service);
        }

        throw new InvalidOperationException("Cannot access the service provider from StorageProviderService");
    }
}
