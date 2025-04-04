using ShareSmallBiz.Portal.Data;
using ShareSmallBiz.Portal.Infrastructure.Configuration;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace ShareSmallBiz.Portal.Areas.Media.Services;

/// <summary>
/// Service for handling file uploads including validation and processing
/// </summary>
public class FileUploadService
{
    private readonly ILogger<FileUploadService> _logger;
    private readonly StorageProviderService _storageProviderService;
    private readonly MediaService _mediaService;
    private readonly MediaStorageOptions _mediaOptions;

    public FileUploadService(
        ILogger<FileUploadService> logger,
        StorageProviderService storageProviderService,
        MediaService mediaService,
        IOptions<MediaStorageOptions> mediaOptions)
    {
        _logger = logger;
        _storageProviderService = storageProviderService;
        _mediaService = mediaService;
        _mediaOptions = mediaOptions.Value;
    }

    /// <summary>
    /// Uploads a file and creates a media entity
    /// </summary>
    public async Task<ShareSmallBiz.Portal.Data.Media> UploadFileAsync(
        IFormFile file,
        string userId,
        StorageProviderNames storageProvider,
        string description = "",
        string attribution = "")
    {
        if (file == null || file.Length == 0)
        {
            throw new ArgumentException("File is null or empty", nameof(file));
        }

        // Validate file
        await ValidateFileAsync(file);

        // Generate a unique filename to prevent collisions
        string fileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";

        // Determine media type based on content type
        var mediaType = _storageProviderService.GetMediaTypeFromContentType(file.ContentType);

        // Upload file to storage provider
        string fileUrl = await _storageProviderService.UploadFileAsync(file, storageProvider, fileName);

        var storageMetaData = new Dictionary<string, string>()
        {
            {  "FileName", fileName },
            { "StorageProvider", storageProvider.ToString() },
            { "MediaType", mediaType.ToString() },
            { "Description", description },
            { "Attribution", attribution },
            { "UserId", userId },
            { "CreatedDate", DateTime.UtcNow.ToString("o") },
            { "ModifiedDate", DateTime.UtcNow.ToString("o") }
        };
        string metaDataJson = JsonSerializer.Serialize(storageMetaData);


        // Create media entity
        var media = new ShareSmallBiz.Portal.Data.Media
        {
            FileName = fileName,
            MediaType = mediaType,
            StorageProvider = storageProvider,
            ContentType = file.ContentType,
            FileSize = file.Length,
            Description = description,
            Attribution = attribution,
            Url = fileUrl,
            UserId = userId,
            StorageMetadata = metaDataJson,
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        };

        // Save to database
        return await _mediaService.CreateMediaAsync(media);
    }

    /// <summary>
    /// Creates a media entity for an external link
    /// </summary>
    public async Task<ShareSmallBiz.Portal.Data.Media> CreateExternalLinkAsync(
        string externalUrl,
        string fileName,
        MediaType mediaType,
        string userId,
        string attribution = "",
        string description = "",
        string storageMetaData = "")
    {
        if (string.IsNullOrEmpty(externalUrl))
        {
            throw new ArgumentException("External URL cannot be null or empty", nameof(externalUrl));
        }

        // Check if this is a YouTube URL 
        if (_storageProviderService.IsYouTubeUrl(externalUrl))
        {
            throw new InvalidOperationException("YouTube URLs should be handled by YouTubeMediaService");
        }

        // Determine content type
        string contentType = _storageProviderService.DetermineContentType(externalUrl, mediaType);

        // Create media entity
        var media = new ShareSmallBiz.Portal.Data.Media
        {
            FileName = fileName,
            ContentType = contentType,
            StorageProvider = StorageProviderNames.External,
            MediaType = mediaType,
            StorageMetadata = storageMetaData,
            Url = externalUrl,
            UserId = userId,
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow,
            Attribution = attribution,
            Description = description,
            FileSize = 0 // No actual file size for external links
        };

        // Save to database
        return await _mediaService.CreateMediaAsync(media);
    }

    /// <summary>
    /// Validates a file for upload
    /// </summary>
    private async Task ValidateFileAsync(IFormFile file)
    {
        // Check file size against configured maximum
        if (file.Length > _mediaOptions.MaxFileSize)
        {
            throw new ArgumentException($"File size exceeds the maximum allowed size of {_mediaOptions.MaxFileSize / (1024 * 1024)}MB");
        }

        // Check file extension against configured allowed extensions
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!_mediaOptions.AllowedExtensions.Contains(extension))
        {
            throw new ArgumentException($"File type {extension} is not allowed");
        }

        // Perform deeper validation with StorageProviderService
        if (!await _storageProviderService.ValidateFileAsync(file))
        {
            throw new ArgumentException("File validation failed - invalid file type or format");
        }
    }
}