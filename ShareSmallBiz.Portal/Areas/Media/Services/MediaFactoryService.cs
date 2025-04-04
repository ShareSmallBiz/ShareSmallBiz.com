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
    private readonly UnsplashService _unsplashService;
    private readonly StorageProviderService _storageProviderService;

    public MediaFactoryService(
        ILogger<MediaFactoryService> logger,
        MediaService mediaService,
        FileUploadService fileUploadService,
        YouTubeMediaService youtubeMediaService,
        UnsplashService unsplashService,
        StorageProviderService storageProviderService)
    {
        _logger = logger;
        _mediaService = mediaService;
        _fileUploadService = fileUploadService;
        _youtubeMediaService = youtubeMediaService;
        _unsplashService = unsplashService;
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
                // Check if this is an Unsplash URL
                if (viewModel.ExternalUrl.Contains("unsplash.com"))
                {
                    // Extract photo ID from URL
                    string photoId = ExtractUnsplashPhotoId(viewModel.ExternalUrl);
                    if (!string.IsNullOrEmpty(photoId))
                    {
                        // Get photo details from Unsplash API
                        var photo = await _unsplashService.GetPhotoAsync(photoId);
                        if (photo != null)
                        {
                            // Create media using UnsplashService
                            return await _unsplashService.CreateUnsplashMediaAsync(
                                photo,
                                userId);
                        }
                    }
                }

                // Default external link handling if not an Unsplash photo or ID extraction failed
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
                    // Check for Unsplash URL
                    if (viewModel.ExternalUrl.Contains("unsplash.com"))
                    {
                        string photoId = ExtractUnsplashPhotoId(viewModel.ExternalUrl);
                        if (!string.IsNullOrEmpty(photoId))
                        {
                            var photo = await _unsplashService.GetPhotoAsync(photoId);
                            if (photo != null)
                            {
                                media.Url = photo.Urls.Full;
                                media.StorageProvider = StorageProviderNames.External;
                                media.MediaType = MediaType.Image;

                                // Update metadata
                                var metadata = new Dictionary<string, string>
                                {
                                    { "photoId", photo.Id },
                                    { "source", "unsplash" },
                                    { "username", photo.User.Username },
                                    { "name", photo.User.Name },
                                    { "downloadLocation", photo.Links.Download }
                                };
                                media.StorageMetadata = System.Text.Json.JsonSerializer.Serialize(metadata);

                                // Update attribution
                                if (string.IsNullOrEmpty(viewModel.Attribution))
                                {
                                    media.Attribution = $"Photo by {photo.User.Name} on Unsplash";
                                }
                            }
                        }
                    }

                    // Default handling for other external URLs
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

    /// <summary>
    /// Extracts the Unsplash photo ID from a URL
    /// </summary>
    private string ExtractUnsplashPhotoId(string url)
    {
        try
        {
            // Handle various Unsplash URL formats
            // Examples:
            // https://unsplash.com/photos/{photo_id}
            // https://unsplash.com/photos/{photo_id}/download
            // https://unsplash.com/photos/{photo_id}?utm_source=...

            var uri = new Uri(url);

            // Check if this is an Unsplash URL
            if (uri.Host != "unsplash.com" && !uri.Host.EndsWith(".unsplash.com"))
                return string.Empty;

            // Split path segments
            var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);

            // Look for 'photos' segment followed by the ID
            for (int i = 0; i < segments.Length - 1; i++)
            {
                if (segments[i].Equals("photos", StringComparison.OrdinalIgnoreCase))
                {
                    return segments[i + 1];
                }
            }

            return string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }
}