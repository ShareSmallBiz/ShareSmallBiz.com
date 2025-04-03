using ShareSmallBiz.Portal.Areas.Media.Controllers;
using ShareSmallBiz.Portal.Data;

namespace ShareSmallBiz.Portal.Areas.Media.Services;

/// <summary>
/// Factory service that coordinates between specialized media services
/// to create the appropriate media entity based on the input
/// </summary>
public class MediaFactoryService
{
    private readonly ILogger<MediaFactoryService> _logger;
    private readonly MediaService _mediaService;
    private readonly FileUploadService _fileUploadService;
    private readonly YouTubeMediaService _youtubeMediaService;
    private readonly StorageProviderService _storageProviderService;

    public MediaFactoryService(
        ILogger<MediaFactoryService> logger,
        MediaService mediaService,
        FileUploadService fileUploadService,
        YouTubeMediaService youtubeMediaService,
        StorageProviderService storageProviderService)
    {
        _logger = logger;
        _mediaService = mediaService;
        _fileUploadService = fileUploadService;
        _youtubeMediaService = youtubeMediaService;
        _storageProviderService = storageProviderService;
    }

    /// <summary>
    /// Creates a media entity from the appropriate service based on input parameters
    /// </summary>
    public async Task<ShareSmallBiz.Portal.Data.Media> CreateMediaAsync(
        LibraryMediaViewModel viewModel,
        string userId)
    {
        try
        {
            // Handle YouTube videos
            if (viewModel.IsYouTube && !string.IsNullOrEmpty(viewModel.YouTubeUrl))
            {
                return await _youtubeMediaService.CreateYouTubeMediaAsync(
                    viewModel.YouTubeUrl,
                    viewModel.FileName,
                    viewModel.Description,
                    viewModel.Attribution,
                    userId);
            }
            // Handle external links
            else if (viewModel.IsExternalLink && !string.IsNullOrEmpty(viewModel.ExternalUrl))
            {
                return await _fileUploadService.CreateExternalLinkAsync(
                    viewModel.ExternalUrl,
                    viewModel.FileName,
                    (MediaType)viewModel.MediaType,
                    userId,
                    viewModel.Attribution,
                    viewModel.Description);
            }
            // Handle file uploads
            else if (viewModel.File != null && viewModel.File.Length > 0)
            {
                return await _fileUploadService.UploadFileAsync(
                    viewModel.File,
                    userId,
                    (StorageProviderNames)viewModel.StorageProvider,
                    viewModel.Description,
                    viewModel.Attribution);
            }
            else
            {
                throw new ArgumentException("Invalid media creation parameters");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating media for user {UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// Updates an existing media entity
    /// </summary>
    public async Task<bool> UpdateMediaAsync(
        int id,
        LibraryMediaViewModel viewModel,
        string userId)
    {
        try
        {
            // Get the existing media
            var media = await _mediaService.GetUserMediaByIdAsync(id, userId);
            if (media == null)
            {
                return false;
            }

            // Update basic properties
            media.FileName = viewModel.FileName;
            media.Description = viewModel.Description;
            media.Attribution = viewModel.Attribution;
            media.MediaType = (MediaType)viewModel.MediaType;
            media.ModifiedDate = DateTime.UtcNow;

            // Handle URL updates for external links and YouTube videos
            if (viewModel.IsExternalLink)
            {
                // If changing to YouTube or updating YouTube URL
                if (viewModel.IsYouTube && !string.IsNullOrEmpty(viewModel.YouTubeUrl))
                {
                    string videoId = _storageProviderService.ExtractYouTubeVideoId(viewModel.YouTubeUrl);
                    if (string.IsNullOrEmpty(videoId))
                    {
                        return false;
                    }

                    media.Url = _storageProviderService.GetYouTubeEmbedUrl(videoId);
                    media.StorageProvider = StorageProviderNames.YouTube;
                    media.MediaType = MediaType.Video;
                    media.StorageMetadata = $"{{\"videoId\":\"{videoId}\",\"originalUrl\":\"{viewModel.YouTubeUrl}\"}}";
                }
                else if (!string.IsNullOrEmpty(viewModel.ExternalUrl))
                {
                    media.Url = viewModel.ExternalUrl;
                    media.StorageProvider = StorageProviderNames.External;
                }
            }

            // Save changes
            return await _mediaService.UpdateMediaAsync(media);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating media {MediaId} for user {UserId}", id, userId);
            return false;
        }
    }

    /// <summary>
    /// Gets a media URL with appropriate formatting
    /// </summary>
    public async Task<string> GetMediaUrlAsync(ShareSmallBiz.Portal.Data.Media media)
    {
        return _mediaService.GetMediaUrl(media);
    }

    /// <summary>
    /// Gets a media thumbnail URL
    /// </summary>
    public async Task<string> GetThumbnailUrlAsync(ShareSmallBiz.Portal.Data.Media media)
    {
        return _mediaService.GetThumbnailUrl(media);
    }

    /// <summary>
    /// Gets a file stream for a media item
    /// </summary>
    public async Task<Stream> GetFileStreamAsync(ShareSmallBiz.Portal.Data.Media media)
    {
        return await _storageProviderService.GetFileStreamAsync(media);
    }

    /// <summary>
    /// Gets a thumbnail stream for a media item
    /// </summary>
    public async Task<Stream> GetThumbnailStreamAsync(ShareSmallBiz.Portal.Data.Media media, int width, int height)
    {
        return await _storageProviderService.GetThumbnailStreamAsync(media, width, height);
    }

    /// <summary>
    /// Deletes a media item
    /// </summary>
    public async Task DeleteMediaAsync(int id, string userId)
    {
        var media = await _mediaService.GetUserMediaByIdAsync(id, userId);
        if (media != null)
        {
            await _mediaService.DeleteMediaAsync(media);
        }
    }
}