using ShareSmallBiz.Portal.Areas.Media.Controllers;
using ShareSmallBiz.Portal.Data;
using System.Text.Json;

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

        // Fetch additional video details if possible
        string channelId = string.Empty;
        string channelTitle = attribution;
        DateTime publishedDate = DateTime.UtcNow;
        string duration = string.Empty;
        string viewCount = string.Empty;

        try
        {
            var videoDetails = await _youTubeService.GetVideoDetailsAsync(videoId);
            if (videoDetails != null && videoDetails.Items.Count > 0)
            {
                var video = videoDetails.Items[0];
                channelId = video.Snippet.ChannelId;
                channelTitle = video.Snippet.ChannelTitle;
                publishedDate = video.Snippet.PublishedAt;
                duration = video.ContentDetails.Duration;
                viewCount = video.Statistics.ViewCount;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unable to fetch additional YouTube details for video {VideoId}", videoId);
            // Continue with basic information
        }

        // Create metadata
        var metadata = new Dictionary<string, string>
        {
            { "videoId", videoId },
            { "originalUrl", youtubeUrl },
            { "channelId", channelId },
            { "channelTitle", channelTitle },
            { "publishedDate", publishedDate.ToString("o") },
            { "duration", duration },
            { "viewCount", viewCount }
        };

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
            Attribution = channelTitle,
            Description = description,
            StorageMetadata = JsonSerializer.Serialize(metadata),
            FileSize = 0 // No actual file size for embedded content
        };

        // Save to database
        return await _mediaService.CreateMediaAsync(media);
    }

    /// <summary>
    /// Creates a media entity for a YouTube channel
    /// </summary>
    public async Task<ShareSmallBiz.Portal.Data.Media> CreateYouTubeChannelMediaAsync(
        string channelId,
        string channelTitle,
        string description,
        string userId)
    {
        if (string.IsNullOrEmpty(channelId))
        {
            throw new ArgumentException("Channel ID cannot be empty", nameof(channelId));
        }

        // Create a YouTube channel URL
        string channelUrl = $"https://www.youtube.com/channel/{channelId}";

        // Create metadata
        var metadata = new Dictionary<string, string>
        {
            { "channelId", channelId },
            { "channelTitle", channelTitle },
            { "type", "youtube_channel" }
        };

        // Create media entity
        var media = new ShareSmallBiz.Portal.Data.Media
        {
            FileName = $"YouTube Channel: {channelTitle}",
            ContentType = "application/youtube.channel",
            StorageProvider = StorageProviderNames.YouTube,
            MediaType = MediaType.Other,
            Url = channelUrl,
            UserId = userId,
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow,
            Attribution = "YouTube",
            Description = description,
            StorageMetadata = JsonSerializer.Serialize(metadata),
            FileSize = 0 // No actual file size for embedded content
        };

        // Save to database
        return await _mediaService.CreateMediaAsync(media);
    }

    /// <summary>
    /// Extracts channel ID from media
    /// </summary>
    public string ExtractChannelIdFromMedia(ShareSmallBiz.Portal.Data.Media media)
    {
        if (media.StorageProvider != StorageProviderNames.YouTube || string.IsNullOrEmpty(media.StorageMetadata))
        {
            return string.Empty;
        }

        try
        {
            var metadata = JsonSerializer.Deserialize<Dictionary<string, string>>(media.StorageMetadata);
            if (metadata != null && metadata.TryGetValue("channelId", out string channelId))
            {
                return channelId;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting channel ID from media {MediaId}", media.Id);
        }

        return string.Empty;
    }

    /// <summary>
    /// Extracts video ID from media
    /// </summary>
    public string ExtractVideoIdFromMedia(ShareSmallBiz.Portal.Data.Media media)
    {
        if (media.StorageProvider != StorageProviderNames.YouTube || string.IsNullOrEmpty(media.StorageMetadata))
        {
            return string.Empty;
        }

        try
        {
            var metadata = JsonSerializer.Deserialize<Dictionary<string, string>>(media.StorageMetadata);
            if (metadata != null && metadata.TryGetValue("videoId", out string videoId))
            {
                return videoId;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting video ID from media {MediaId}", media.Id);
        }

        // Fall back to extracting from URL
        return _storageProviderService.ExtractYouTubeVideoId(media.Url);
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
    public async Task<YouTubeChannelViewModel?> GetChannelDetailsAsync(string channelId, int videoCount = 12)
    {
        try
        {
            var channelResponse = await _youTubeService.GetChannelDetailsAsync(channelId);

            if (channelResponse?.Items == null || !channelResponse.Items.Any())
            {
                return null;
            }

            var channelInfo = channelResponse.Items[0];
            var videosResponse = await _youTubeService.GetChannelVideosAsync(channelId, videoCount);

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
    /// Gets details about a specific YouTube video
    /// </summary>
    public async Task<YouTubeVideoDetailViewModel?> GetVideoDetailsAsync(string videoId)
    {
        try
        {
            var videoResponse = await _youTubeService.GetVideoDetailsAsync(videoId);

            if (videoResponse?.Items == null || !videoResponse.Items.Any())
            {
                return null;
            }

            var video = videoResponse.Items[0];

            // Get related videos
            var relatedVideosResponse = await _youTubeService.GetRelatedVideosAsync(videoId, 6);

            var viewModel = new YouTubeVideoDetailViewModel
            {
                VideoId = video.Id,
                Title = video.Snippet.Title,
                Description = video.Snippet.Description,
                ThumbnailUrl = video.Snippet.Thumbnails.High?.Url ?? video.Snippet.Thumbnails.Medium.Url,
                PublishedAt = video.Snippet.PublishedAt,
                ChannelId = video.Snippet.ChannelId,
                ChannelTitle = video.Snippet.ChannelTitle,
                Duration = YouTubeService.FormatDuration(video.ContentDetails.Duration),
                ViewCount = YouTubeService.FormatViewCount(video.Statistics.ViewCount),
                LikeCount = video.Statistics.LikeCount,
                CommentCount = video.Statistics.CommentCount
            };

            // Convert related videos to view models
            if (relatedVideosResponse?.Items != null)
            {
                viewModel.RelatedVideos = relatedVideosResponse.Items.Select(item => new YouTubeVideoViewModel
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
            _logger.LogError(ex, "Error getting YouTube video details for video ID: {VideoId}", videoId);
            throw;
        }
    }

    /// <summary>
    /// Gets user's media items that are from a specific YouTube channel
    /// </summary>
    public async Task<IEnumerable<ShareSmallBiz.Portal.Data.Media>> GetUserMediaFromChannelAsync(string userId, string channelId)
    {
        // Get all YouTube videos
        var allYouTubeMedia = await _mediaService.GetMediaByStorageProviderAsync(userId, StorageProviderNames.YouTube);

        // Filter by channel ID in metadata
        return allYouTubeMedia.Where(m =>
        {
            if (string.IsNullOrEmpty(m.StorageMetadata))
            {
                return false;
            }

            try
            {
                var metadata = JsonSerializer.Deserialize<Dictionary<string, string>>(m.StorageMetadata);
                return metadata != null && metadata.TryGetValue("channelId", out string storedChannelId) && storedChannelId == channelId;
            }
            catch
            {
                return false;
            }
        });
    }

    /// <summary>
    /// Gets the channel details for a YouTube video
    /// </summary>
    public async Task<YouTubeChannelViewModel?> GetChannelForVideoAsync(string videoId)
    {
        try
        {
            // First get the video details to extract the channel ID
            var videoDetails = await _youTubeService.GetVideoDetailsAsync(videoId);
            if (videoDetails?.Items == null || !videoDetails.Items.Any())
            {
                return null;
            }

            string channelId = videoDetails.Items[0].Snippet.ChannelId;

            // Then get the channel details
            return await GetChannelDetailsAsync(channelId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting channel for YouTube video: {VideoId}", videoId);
            return null;
        }
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