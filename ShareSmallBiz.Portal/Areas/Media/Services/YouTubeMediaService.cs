using ShareSmallBiz.Portal.Areas.Media.Controllers;
using ShareSmallBiz.Portal.Data;

namespace ShareSmallBiz.Portal.Areas.Media.Services;

/// <summary>
/// Service for YouTube-specific media operations
/// </summary>
public class YouTubeMediaService
{
    private readonly ILogger<YouTubeMediaService> _logger;
    private readonly StorageProviderService _storageProviderService;
    private readonly MediaService _mediaService;
    private readonly YouTubeService _youTubeService;

    public YouTubeMediaService(
        ILogger<YouTubeMediaService> logger,
        StorageProviderService storageProviderService,
        MediaService mediaService,
        YouTubeService youTubeService)
    {
        _logger = logger;
        _storageProviderService = storageProviderService;
        _mediaService = mediaService;
        _youTubeService = youTubeService;
    }

    /// <summary>
    /// Creates a media entity for a YouTube video
    /// </summary>
    public async Task<ShareSmallBiz.Portal.Data.Media> CreateYouTubeMediaAsync(
        string youtubeUrl,
        string title,
        string description,
        string attribution,
        string userId)
    {
        // Validate YouTube URL
        string videoId = _storageProviderService.ExtractYouTubeVideoId(youtubeUrl);
        if (string.IsNullOrEmpty(videoId))
        {
            throw new ArgumentException("Invalid YouTube URL format", nameof(youtubeUrl));
        }

        // Create a standardized YouTube embed URL
        string embedUrl = _storageProviderService.GetYouTubeEmbedUrl(videoId);

        // Create media entity
        var media = new ShareSmallBiz.Portal.Data.Media
        {
            FileName = title,
            ContentType = "video/youtube",
            StorageProvider = StorageProviderNames.YouTube,
            MediaType = MediaType.Video,
            Url = embedUrl,
            UserId = userId,
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow,
            Attribution = attribution,
            Description = description,
            StorageMetadata = $"{{\"videoId\":\"{videoId}\",\"originalUrl\":\"{youtubeUrl}\"}}",
            FileSize = 0 // No actual file size for embedded content
        };

        // Save to database
        return await _mediaService.CreateMediaAsync(media);
    }

    /// <summary>
    /// Searches YouTube for videos matching a query
    /// </summary>
    public async Task<IEnumerable<YouTubeVideoViewModel>> SearchVideosAsync(string query, int maxResults = 10)
    {
        try
        {
            var searchResponse = await _youTubeService.SearchVideosAsync(query, maxResults);

            if (searchResponse?.Items == null)
            {
                return Enumerable.Empty<YouTubeVideoViewModel>();
            }

            return searchResponse.Items.Select(item => new YouTubeVideoViewModel
            {
                VideoId = item.Id.VideoId,
                Title = item.Snippet.Title,
                Description = item.Snippet.Description,
                ThumbnailUrl = item.Snippet.Thumbnails.Medium.Url,
                PublishedAt = item.Snippet.PublishedAt,
                ChannelId = item.Snippet.ChannelId,
                ChannelTitle = item.Snippet.ChannelTitle
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching YouTube videos with query: {Query}", query);
            throw;
        }
    }

    /// <summary>
    /// Gets details about a YouTube channel
    /// </summary>
    public async Task<YouTubeChannelViewModel?> GetChannelDetailsAsync(string channelId)
    {
        try
        {
            var channelResponse = await _youTubeService.GetChannelDetailsAsync(channelId);

            if (channelResponse?.Items == null || !channelResponse.Items.Any())
            {
                return null;
            }

            var channelInfo = channelResponse.Items[0];
            var videosResponse = await _youTubeService.GetChannelVideosAsync(channelId);

            var viewModel = new YouTubeChannelViewModel
            {
                ChannelId = channelId,
                ChannelTitle = channelInfo.Snippet.Title,
                ChannelDescription = channelInfo.Snippet.Description,
                ThumbnailUrl = channelInfo.Snippet.Thumbnails.Medium?.Url ?? channelInfo.Snippet.Thumbnails.Default.Url,
                SubscriberCount = channelInfo.Statistics.SubscriberCount,
                VideoCount = channelInfo.Statistics.VideoCount,
                ViewCount = channelInfo.Statistics.ViewCount
            };

            // Convert videos to view models
            if (videosResponse?.Items != null)
            {
                viewModel.Videos = videosResponse.Items.Select(item => new YouTubeVideoViewModel
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

            return viewModel;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting YouTube channel details for channel ID: {ChannelId}", channelId);
            throw;
        }
    }

    /// <summary>
    /// Gets user's media items that are from a specific YouTube channel
    /// </summary>
    public async Task<IEnumerable<ShareSmallBiz.Portal.Data.Media>> GetUserMediaFromChannelAsync(string userId, string channelId)
    {
        var allYouTubeMedia = await _mediaService.GetMediaByStorageProviderAsync(userId, StorageProviderNames.YouTube);

        return allYouTubeMedia.Where(m =>
            m.StorageMetadata.Contains(channelId) ||
            (m.Attribution?.Contains(channelId) ?? false));
    }

    /// <summary>
    /// Updates a YouTube video embed URL with parameters
    /// </summary>
    public string UpdateYouTubeEmbedUrl(string embedUrl, string parameters)
    {
        if (string.IsNullOrEmpty(embedUrl))
            return string.Empty;

        // Check if URL already has parameters
        if (embedUrl.Contains('?'))
        {
            return $"{embedUrl}&{parameters}";
        }
        else
        {
            return $"{embedUrl}?{parameters}";
        }
    }

    /// <summary>
    /// Gets recently added YouTube videos for a user
    /// </summary>
    public async Task<IEnumerable<ShareSmallBiz.Portal.Data.Media>> GetRecentlyAddedVideosAsync(string userId, int count = 4)
    {
        var youtubeMedia = await _mediaService.GetMediaByStorageProviderAsync(userId, StorageProviderNames.YouTube);
        return youtubeMedia.OrderByDescending(m => m.CreatedDate).Take(count);
    }
}