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
            case StorageProviderNames.Local:
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

    public async Task<Media> CreateExternalLinkAsync(string url, string fileName, MediaType mediaType, string userId, string attribution = null, string description = null)
    {
        if (string.IsNullOrEmpty(url))
        {
            throw new ArgumentException("URL is required", nameof(url));
        }

        var media = new Media
        {
            FileName = fileName,
            MediaType = mediaType,
            StorageProvider = StorageProviderNames.External,
            Url = url,
            ContentType = GetContentTypeFromMediaType(mediaType),
            Description = description,
            Attribution = attribution,
            StorageMetadata = string.Empty,
            UserId = userId,
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        };

        _dbContext.Media.Add(media);
        await _dbContext.SaveChangesAsync();

        return media;
    }

    public async Task<Stream> GetFileStreamAsync(Media media)
    {
        switch (media.StorageProvider)
        {
            case StorageProviderNames.Local:
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
            case StorageProviderNames.Local:
                await DeleteLocalFileAsync(media.Url);
                break;
            case StorageProviderNames.External:
                // Nothing to delete for external links
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
            case StorageProviderNames.Local:
                return $"{_configuration["AppUrl"]}/files/{Path.GetFileName(media.Url)}";
            case StorageProviderNames.External:
                return media.Url;
            case StorageProviderNames.AzureBlob:
                return media.Url;
            case StorageProviderNames.AwsS3:
                return media.Url;
            default:
                throw new ArgumentException($"Unsupported storage provider: {media.StorageProvider}");
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