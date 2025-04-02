using Microsoft.EntityFrameworkCore;
using ShareSmallBiz.Portal.Data;

namespace ShareSmallBiz.Portal.Services;

public class StorageProviderService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<StorageProviderService> _logger;
    private readonly ShareSmallBizUserContext _dbContext;

    public StorageProviderService(
        IConfiguration configuration,
        ILogger<StorageProviderService> logger,
        ShareSmallBizUserContext dbContext)
    {
        _configuration = configuration;
        _logger = logger;
        _dbContext = dbContext;
    }

    public async Task<Media> UploadFileAsync(
        IFormFile file,
        string userId,
        StorageProviderNames provider,
        string description = null)
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

        // Determine media type based on content type
        var mediaType = GetMediaTypeFromContentType(file.ContentType);

        // Create a unique filename
        var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";

        // Initialize new Media entity
        var media = new Media
        {
            FileName = fileName,
            MediaType = mediaType,
            StorageProvider = provider,
            ContentType = file.ContentType,
            FileSize = file.Length,
            Description = description,
            StorageMetadata = description ?? string.Empty,
            Attribution = description ?? string.Empty,
            UserId = userId,
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow,
        };

        // Upload file based on provider
        switch (provider)
        {
            case StorageProviderNames.LocalStorage:
                media.Url = await UploadToLocalStorageAsync(file, fileName);
                break;
            default:
                throw new ArgumentException($"Unsupported storage provider: {provider}");
        }

        // Save to database
        _dbContext.Media.Add(media);
        await _dbContext.SaveChangesAsync();

        return media;
    }

    public async Task<Media> CreateExternalLinkAsync(
        string externalUrl,
        string fileName,
        MediaType mediaType,
        string userId,
        string attribution = "",
        string description = "")
    {
        // Check if this is a YouTube URL and process accordingly
        if (IsYouTubeUrl(externalUrl))
        {
            string videoId = ExtractYouTubeVideoId(externalUrl);
            if (string.IsNullOrEmpty(videoId))
            {
                throw new ArgumentException("Invalid YouTube URL format", nameof(externalUrl));
            }

            // Create a standardized YouTube embed URL
            string embedUrl = $"https://www.youtube.com/embed/{videoId}";

            // Create media record with YouTube as provider
            var media = new Media
            {
                FileName = fileName,
                ContentType = "video/youtube",
                StorageProvider = StorageProviderNames.YouTube,
                MediaType = MediaType.Video,
                Url = embedUrl,
                UserId = userId,
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow,
                Attribution = attribution,
                Description = description,
                StorageMetadata = externalUrl, // Store the original URL for reference
                FileSize = 0 // No actual file size for embedded content
            };

            _dbContext.Media.Add(media);
            await _dbContext.SaveChangesAsync();
            return media;
        }
        else
        {
            // Handle other external links as before
            var media = new Media
            {
                FileName = fileName,
                ContentType = DetermineContentType(externalUrl, mediaType),
                StorageProvider = StorageProviderNames.External,
                MediaType = mediaType,
                Url = externalUrl,
                UserId = userId,
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow,
                Attribution = attribution,
                Description = description,
                FileSize = 0 // No actual file size for external links
            };

            _dbContext.Media.Add(media);
            await _dbContext.SaveChangesAsync();
            return media;
        }
    }

    private string DetermineContentType(string externalUrl, MediaType mediaType)
    {
        if (string.IsNullOrEmpty(externalUrl))
            throw new ArgumentException("External URL cannot be null or empty", nameof(externalUrl));

        if (mediaType == MediaType.Image)
            return "image/jpeg"; // Default for images
        if (mediaType == MediaType.Video)
        {
            if (IsYouTubeUrl(externalUrl))
            {
                return "YouTube";
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

    public async Task<Stream> GetFileStreamAsync(Media media)
    {
        switch (media.StorageProvider)
        {
            case StorageProviderNames.LocalStorage:
                return await GetLocalFileStreamAsync(media.Url);
            case StorageProviderNames.External:
                throw new InvalidOperationException("Cannot get file stream for external links");
            default:
                throw new ArgumentException($"Unsupported storage provider: {media.StorageProvider}");
        }
    }

    public async Task DeleteFileAsync(Media media)
    {
        switch (media.StorageProvider)
        {
            case StorageProviderNames.LocalStorage:
                await DeleteLocalFileAsync(media.Url);
                break;
            case StorageProviderNames.External:
                // Nothing to delete for external links
                break;
            case StorageProviderNames.YouTube:
                // Nothing to delete for Youtube Links
                break;
            default:
                throw new ArgumentException($"Unsupported storage provider: {media.StorageProvider}");
        }

        _dbContext.Media.Remove(media);
        await _dbContext.SaveChangesAsync();
    }

    public async Task<bool> ValidateFileAsync(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return false;

        // Check file size (e.g., max 10MB)
        long maxFileSize = 10 * 1024 * 1024; // 10MB
        if (file.Length > maxFileSize)
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

    public async Task<string> GetPublicUrlAsync(Media media)
    {
        switch (media.StorageProvider)
        {
            case StorageProviderNames.LocalStorage:
                return $"{_configuration["AppUrl"]}/files/{Path.GetFileName(media.Url)}";
            case StorageProviderNames.YouTube:
                // YouTube embed URLs are already public
                return media.Url;
            case StorageProviderNames.External:
                return media.Url;
            case StorageProviderNames.AzureBlob:
                return media.Url;
            case StorageProviderNames.S3:
                return media.Url;
            default:
                throw new ArgumentException($"Unsupported storage provider: {media.StorageProvider}");
        }
    }

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

    public static string ExtractYouTubeVideoId(string url)
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

            return null;
        }
        catch
        {
            return null;
        }
    }



    #region Private Helper Methods

    private async Task<string> UploadToLocalStorageAsync(IFormFile file, string fileName)
    {
        // Get upload path from configuration
        var uploadPath = _configuration["ShareSmallBizMedia:UploadPath"]
            ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");

        // Ensure directory exists
        if (!Directory.Exists(uploadPath))
        {
            Directory.CreateDirectory(uploadPath);
        }

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

    private async Task DeleteLocalFileAsync(string filePath)
    {
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
    }

    private MediaType GetMediaTypeFromContentType(string contentType)
    {
        if (contentType.StartsWith("image/"))
            return MediaType.Image;
        if (contentType.StartsWith("video/"))
            return MediaType.Video;
        if (contentType.StartsWith("audio/"))
            return MediaType.Audio;
        if (contentType.StartsWith("application/pdf") || contentType.StartsWith("application/msword") || contentType.StartsWith("application/vnd.openxmlformats-officedocument"))
            return MediaType.Document;

        return MediaType.Other;
    }

    private string GetContentTypeFromMediaType(MediaType mediaType)
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

    #endregion
}