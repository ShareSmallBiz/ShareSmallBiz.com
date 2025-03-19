namespace ShareSmallBiz.Portal.Infrastructure.Services;

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

public class YouTubeService : IYouTubeService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly YouTubeApiOptions _options;
    private readonly IMemoryCache _cache;
    private const string CacheKeyPrefix = "YouTube_ChannelVideos_";
    private const string ChannelCacheKeyPrefix = "YouTube_Channel_";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(1);

    public YouTubeService(
        IMemoryCache cache,
        IHttpClientFactory httpClientFactory,
        IOptions<YouTubeApiOptions> options)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    }

    /// <summary>
    /// Gets detailed information about a YouTube channel
    /// </summary>
    /// <param name="channelId">The YouTube channel ID</param>
    /// <returns>The channel details</returns>
    public async Task<YouTubeChannel> GetChannelDetailsAsync(string channelId)
    {
        if (string.IsNullOrWhiteSpace(channelId))
            throw new ArgumentNullException(nameof(channelId));

        // Create a cache key for the channel
        string cacheKey = $"{ChannelCacheKeyPrefix}{channelId}";

        // Try to get the channel details from cache first
        if (_cache.TryGetValue(cacheKey, out YouTubeChannel cachedChannel))
        {
            return cachedChannel;
        }

        // Not in cache, fetch from API
        var httpClient = _httpClientFactory.CreateClient("YouTubeApi");

        // Get comprehensive channel data with multiple parts
        var url = $"channels?part=snippet,contentDetails,statistics,brandingSettings,status,topicDetails&id={channelId}&key={_options.ApiKey}";

        var response = await httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var channelResponse = await response.Content.ReadFromJsonAsync<ChannelResponse>();

        if (channelResponse?.Items == null || channelResponse.Items.Count == 0)
            return null;

        var channelItem = channelResponse.Items[0];
        var channel = new YouTubeChannel
        {
            ChannelId = channelId,

            // Basic details
            Title = channelItem.Snippet?.Title,
            Description = channelItem.Snippet?.Description,
            CustomUrl = channelItem.Snippet?.CustomUrl,
            PublishedAt = channelItem.Snippet?.PublishedAt ?? DateTime.MinValue,

            // Thumbnails/images
            ThumbnailUrl = channelItem.Snippet?.Thumbnails?.Medium?.Url,
            BannerUrl = channelItem.BrandingSettings?.Image?.BannerExternalUrl,

            // Statistics
            SubscriberCount = channelItem.Statistics?.SubscriberCount ?? 0,
            ViewCount = channelItem.Statistics?.ViewCount ?? 0,
            VideoCount = channelItem.Statistics?.VideoCount ?? 0,

            // Content Details
            UploadsPlaylistId = channelItem.ContentDetails?.RelatedPlaylists?.Uploads,

            // Additional metadata
            Country = channelItem.Snippet?.Country,
            DefaultLanguage = channelItem.Snippet?.DefaultLanguage,
            Keywords = channelItem.BrandingSettings?.Channel?.Keywords,
            TopicCategories = channelItem.TopicDetails?.TopicCategories,
            IsVerified = channelItem.Status?.IsLinked ?? false,
            MadeForKids = channelItem.Status?.MadeForKids ?? false,
            PrivacyStatus = channelItem.Status?.PrivacyStatus,

            // Branding elements
            FeaturedChannelIds = channelItem.BrandingSettings?.Channel?.FeaturedChannelsUrls,
            UnsubscribedTrailer = channelItem.BrandingSettings?.Channel?.UnsubscribedTrailer
        };

        // Store in cache
        var cacheEntryOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(CacheDuration);

        _cache.Set(cacheKey, channel, cacheEntryOptions);

        return channel;
    }

    /// <summary>
    /// Gets videos from a specific YouTube channel
    /// </summary>
    /// <param name="channelId">The YouTube channel ID</param>
    /// <param name="maxResults">Maximum number of results to return (default: 10)</param>
    /// <returns>A list of YouTube videos</returns>
    public async Task<List<YouTubeVideo>> GetChannelVideosAsync(string channelId, int maxResults = 10)
    {
        if (string.IsNullOrWhiteSpace(channelId))
            throw new ArgumentNullException(nameof(channelId));

        // Create a cache key based on the parameters
        string cacheKey = $"{CacheKeyPrefix}{channelId}_{maxResults}";

        // Try to get the videos from cache first
        if (_cache.TryGetValue(cacheKey, out List<YouTubeVideo> cachedVideos))
        {
            return cachedVideos;
        }

        // Not in cache, fetch from API
        // Step 1: Get the channel details which includes the uploads playlist ID
        var channel = await GetChannelDetailsAsync(channelId);
        if (channel == null || string.IsNullOrEmpty(channel.UploadsPlaylistId))
            return new List<YouTubeVideo>();

        // Step 2: Get the videos from the uploads playlist
        var videos = await GetPlaylistVideosAsync(channel.UploadsPlaylistId, maxResults);

        // Store in cache for one hour
        var cacheEntryOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(CacheDuration);

        _cache.Set(cacheKey, videos, cacheEntryOptions);

        return videos;
    }

    private async Task<List<YouTubeVideo>> GetPlaylistVideosAsync(string playlistId, int maxResults)
    {
        var httpClient = _httpClientFactory.CreateClient("YouTubeApi");

        // Enhanced to get more video details
        var url = $"playlistItems?part=snippet,contentDetails,status&maxResults={maxResults}&playlistId={playlistId}&key={_options.ApiKey}";

        var response = await httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var playlistResponse = await response.Content.ReadFromJsonAsync<PlaylistItemsResponse>();

        var videos = new List<YouTubeVideo>();

        if (playlistResponse?.Items == null)
            return videos;

        foreach (var item in playlistResponse.Items)
        {
            var snippet = item.Snippet;
            videos.Add(new YouTubeVideo
            {
                VideoId = snippet.ResourceId.VideoId,
                Title = snippet.Title,
                Description = snippet.Description,
                PublishedAt = snippet.PublishedAt,
                ThumbnailUrl = snippet.Thumbnails.Medium?.Url,
                ChannelId = snippet.ChannelId,
                ChannelTitle = snippet.ChannelTitle,
                Position = snippet.Position,
                PrivacyStatus = item.Status?.PrivacyStatus,
                PlaylistId = playlistId
            });
        }

        return videos;
    }

    /// <summary>
    /// Gets more detailed information about specific videos
    /// </summary>
    /// <param name="videoIds">List of video IDs</param>
    /// <returns>A list of detailed YouTube videos</returns>
    public async Task<List<YouTubeVideoDetail>> GetVideoDetailsAsync(List<string> videoIds)
    {
        if (videoIds == null || videoIds.Count == 0)
            return new List<YouTubeVideoDetail>();

        var httpClient = _httpClientFactory.CreateClient("YouTubeApi");

        // Join video IDs with comma
        var videoIdsString = string.Join(",", videoIds);

        // Get comprehensive video data
        var url = $"videos?part=snippet,contentDetails,statistics,status,topicDetails&id={videoIdsString}&key={_options.ApiKey}";

        var response = await httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var videoResponse = await response.Content.ReadFromJsonAsync<VideoDetailResponse>();

        var videos = new List<YouTubeVideoDetail>();

        if (videoResponse?.Items == null)
            return videos;

        foreach (var item in videoResponse.Items)
        {
            videos.Add(new YouTubeVideoDetail
            {
                VideoId = item.Id,
                Title = item.Snippet?.Title,
                Description = item.Snippet?.Description,
                PublishedAt = item.Snippet?.PublishedAt ?? DateTime.MinValue,
                ThumbnailUrl = item.Snippet?.Thumbnails?.Medium?.Url,
                ChannelId = item.Snippet?.ChannelId,
                ChannelTitle = item.Snippet?.ChannelTitle,

                // Additional properties from ContentDetails
                Duration = item.ContentDetails?.Duration,
                Dimension = item.ContentDetails?.Dimension,
                Definition = item.ContentDetails?.Definition,
                Caption = item.ContentDetails?.Caption,
                LicensedContent = item.ContentDetails?.LicensedContent ?? false,

                // Statistics
                ViewCount = item.Statistics?.ViewCount ?? 0,
                LikeCount = item.Statistics?.LikeCount ?? 0,
                DislikeCount = item.Statistics?.DislikeCount ?? 0,
                FavoriteCount = item.Statistics?.FavoriteCount ?? 0,
                CommentCount = item.Statistics?.CommentCount ?? 0,

                // Status
                UploadStatus = item.Status?.UploadStatus,
                PrivacyStatus = item.Status?.PrivacyStatus,
                MadeForKids = item.Status?.MadeForKids ?? false,

                // Categories
                CategoryId = item.Snippet?.CategoryId,
                Tags = item.Snippet?.Tags,
                TopicCategories = item.TopicDetails?.TopicCategories
            });
        }

        return videos;
    }
}

public interface IYouTubeService
{
    Task<List<YouTubeVideo>> GetChannelVideosAsync(string channelId, int maxResults = 10);
    Task<YouTubeChannel> GetChannelDetailsAsync(string channelId);
    Task<List<YouTubeVideoDetail>> GetVideoDetailsAsync(List<string> videoIds);
}

public class YouTubeApiOptions
{
    public string ApiKey { get; set; }
    public string BaseUrl { get; set; } = "https://www.googleapis.com/youtube/v3/";
}

public class YouTubeChannel
{
    // Core identification
    public string ChannelId { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public string CustomUrl { get; set; }
    public DateTime PublishedAt { get; set; }

    // Visual elements
    public string ThumbnailUrl { get; set; }
    public string BannerUrl { get; set; }

    // Statistics
    public long SubscriberCount { get; set; }
    public long ViewCount { get; set; }
    public long VideoCount { get; set; }

    // Content playlists
    public string UploadsPlaylistId { get; set; }

    // Metadata
    public string Country { get; set; }
    public string DefaultLanguage { get; set; }
    public string Keywords { get; set; }
    public List<string> TopicCategories { get; set; }

    // Status information
    public bool IsVerified { get; set; }
    public bool MadeForKids { get; set; }
    public string PrivacyStatus { get; set; }

    // Branding settings
    public List<string> FeaturedChannelIds { get; set; }
    public string UnsubscribedTrailer { get; set; }

    // Derived properties
    public string ChannelUrl => $"https://www.youtube.com/channel/{ChannelId}";
    public string UploadsUrl => $"https://www.youtube.com/playlist?list={UploadsPlaylistId}";
}

public class YouTubeVideo
{
    public string VideoId { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public DateTime PublishedAt { get; set; }
    public string ThumbnailUrl { get; set; }
    public string ChannelId { get; set; }
    public string ChannelTitle { get; set; }
    public int Position { get; set; }
    public string PrivacyStatus { get; set; }
    public string PlaylistId { get; set; }

    public string VideoUrl => $"https://www.youtube.com/watch?v={VideoId}";
    public string EmbedUrl => $"https://www.youtube.com/embed/{VideoId}";
}

public class YouTubeVideoDetail : YouTubeVideo
{
    // Content details
    public string Duration { get; set; }
    public string Dimension { get; set; }
    public string Definition { get; set; }
    public string Caption { get; set; }
    public bool LicensedContent { get; set; }

    // Statistics
    public long ViewCount { get; set; }
    public long LikeCount { get; set; }
    public long DislikeCount { get; set; }
    public long FavoriteCount { get; set; }
    public long CommentCount { get; set; }

    // Status information
    public string UploadStatus { get; set; }
    public bool MadeForKids { get; set; }

    // Categories and tags
    public string CategoryId { get; set; }
    public List<string> Tags { get; set; }
    public List<string> TopicCategories { get; set; }
}

#region Response Models
public class ChannelResponse
{
    [JsonPropertyName("items")]
    public List<ChannelItem> Items { get; set; }
}

public class ChannelItem
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("snippet")]
    public ChannelSnippet Snippet { get; set; }

    [JsonPropertyName("contentDetails")]
    public ContentDetails ContentDetails { get; set; }

    [JsonPropertyName("statistics")]
    public ChannelStatistics Statistics { get; set; }

    [JsonPropertyName("topicDetails")]
    public TopicDetails TopicDetails { get; set; }

    [JsonPropertyName("status")]
    public ChannelStatus Status { get; set; }

    [JsonPropertyName("brandingSettings")]
    public BrandingSettings BrandingSettings { get; set; }
}

public class ChannelSnippet
{
    [JsonPropertyName("title")]
    public string Title { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; }

    [JsonPropertyName("customUrl")]
    public string CustomUrl { get; set; }

    [JsonPropertyName("publishedAt")]
    public DateTime PublishedAt { get; set; }

    [JsonPropertyName("thumbnails")]
    public Thumbnails Thumbnails { get; set; }

    [JsonPropertyName("country")]
    public string Country { get; set; }

    [JsonPropertyName("defaultLanguage")]
    public string DefaultLanguage { get; set; }
}

public class ChannelStatistics
{
    [JsonPropertyName("viewCount")]
    public long ViewCount { get; set; }

    [JsonPropertyName("subscriberCount")]
    public long SubscriberCount { get; set; }

    [JsonPropertyName("hiddenSubscriberCount")]
    public bool HiddenSubscriberCount { get; set; }

    [JsonPropertyName("videoCount")]
    public long VideoCount { get; set; }
}

public class ContentDetails
{
    [JsonPropertyName("relatedPlaylists")]
    public RelatedPlaylists RelatedPlaylists { get; set; }
}

public class RelatedPlaylists
{
    [JsonPropertyName("uploads")]
    public string Uploads { get; set; }

    [JsonPropertyName("likes")]
    public string Likes { get; set; }
}

public class TopicDetails
{
    [JsonPropertyName("topicIds")]
    public List<string> TopicIds { get; set; }

    [JsonPropertyName("topicCategories")]
    public List<string> TopicCategories { get; set; }
}

public class ChannelStatus
{
    [JsonPropertyName("privacyStatus")]
    public string PrivacyStatus { get; set; }

    [JsonPropertyName("isLinked")]
    public bool IsLinked { get; set; }

    [JsonPropertyName("longUploadsStatus")]
    public string LongUploadsStatus { get; set; }

    [JsonPropertyName("madeForKids")]
    public bool MadeForKids { get; set; }
}

public class BrandingSettings
{
    [JsonPropertyName("channel")]
    public ChannelBranding Channel { get; set; }

    [JsonPropertyName("image")]
    public ChannelImage Image { get; set; }
}

public class ChannelBranding
{
    [JsonPropertyName("title")]
    public string Title { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; }

    [JsonPropertyName("keywords")]
    public string Keywords { get; set; }

    [JsonPropertyName("unsubscribedTrailer")]
    public string UnsubscribedTrailer { get; set; }

    [JsonPropertyName("featuredChannelsUrls")]
    public List<string> FeaturedChannelsUrls { get; set; }
}

public class ChannelImage
{
    [JsonPropertyName("bannerExternalUrl")]
    public string BannerExternalUrl { get; set; }
}

public class PlaylistItemsResponse
{
    [JsonPropertyName("items")]
    public List<PlaylistItem> Items { get; set; }

    [JsonPropertyName("pageInfo")]
    public PageInfo PageInfo { get; set; }

    [JsonPropertyName("nextPageToken")]
    public string NextPageToken { get; set; }
}

public class PageInfo
{
    [JsonPropertyName("totalResults")]
    public int TotalResults { get; set; }

    [JsonPropertyName("resultsPerPage")]
    public int ResultsPerPage { get; set; }
}

public class PlaylistItem
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("snippet")]
    public PlaylistItemSnippet Snippet { get; set; }

    [JsonPropertyName("contentDetails")]
    public PlaylistItemContentDetails ContentDetails { get; set; }

    [JsonPropertyName("status")]
    public PlaylistItemStatus Status { get; set; }
}

public class PlaylistItemSnippet
{
    [JsonPropertyName("title")]
    public string Title { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; }

    [JsonPropertyName("publishedAt")]
    public DateTime PublishedAt { get; set; }

    [JsonPropertyName("thumbnails")]
    public Thumbnails Thumbnails { get; set; }

    [JsonPropertyName("resourceId")]
    public ResourceId ResourceId { get; set; }

    [JsonPropertyName("channelId")]
    public string ChannelId { get; set; }

    [JsonPropertyName("channelTitle")]
    public string ChannelTitle { get; set; }

    [JsonPropertyName("position")]
    public int Position { get; set; }
}

public class PlaylistItemContentDetails
{
    [JsonPropertyName("videoId")]
    public string VideoId { get; set; }

    [JsonPropertyName("startAt")]
    public string StartAt { get; set; }

    [JsonPropertyName("endAt")]
    public string EndAt { get; set; }

    [JsonPropertyName("note")]
    public string Note { get; set; }
}

public class PlaylistItemStatus
{
    [JsonPropertyName("privacyStatus")]
    public string PrivacyStatus { get; set; }
}

public class Thumbnails
{
    [JsonPropertyName("default")]
    public Thumbnail Default { get; set; }

    [JsonPropertyName("medium")]
    public Thumbnail Medium { get; set; }

    [JsonPropertyName("high")]
    public Thumbnail High { get; set; }

    [JsonPropertyName("standard")]
    public Thumbnail Standard { get; set; }

    [JsonPropertyName("maxres")]
    public Thumbnail Maxres { get; set; }
}

public class Thumbnail
{
    [JsonPropertyName("url")]
    public string Url { get; set; }

    [JsonPropertyName("width")]
    public int Width { get; set; }

    [JsonPropertyName("height")]
    public int Height { get; set; }
}

public class ResourceId
{
    [JsonPropertyName("kind")]
    public string Kind { get; set; }

    [JsonPropertyName("videoId")]
    public string VideoId { get; set; }
}

public class VideoDetailResponse
{
    [JsonPropertyName("items")]
    public List<VideoItem> Items { get; set; }
}

public class VideoItem
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("snippet")]
    public VideoSnippet Snippet { get; set; }

    [JsonPropertyName("contentDetails")]
    public VideoContentDetails ContentDetails { get; set; }

    [JsonPropertyName("statistics")]
    public VideoStatistics Statistics { get; set; }

    [JsonPropertyName("status")]
    public VideoStatus Status { get; set; }

    [JsonPropertyName("topicDetails")]
    public TopicDetails TopicDetails { get; set; }
}

public class VideoSnippet
{
    [JsonPropertyName("publishedAt")]
    public DateTime PublishedAt { get; set; }

    [JsonPropertyName("channelId")]
    public string ChannelId { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; }

    [JsonPropertyName("thumbnails")]
    public Thumbnails Thumbnails { get; set; }

    [JsonPropertyName("channelTitle")]
    public string ChannelTitle { get; set; }

    [JsonPropertyName("tags")]
    public List<string> Tags { get; set; }

    [JsonPropertyName("categoryId")]
    public string CategoryId { get; set; }

    [JsonPropertyName("liveBroadcastContent")]
    public string LiveBroadcastContent { get; set; }

    [JsonPropertyName("defaultLanguage")]
    public string DefaultLanguage { get; set; }

    [JsonPropertyName("defaultAudioLanguage")]
    public string DefaultAudioLanguage { get; set; }
}

public class VideoContentDetails
{
    [JsonPropertyName("duration")]
    public string Duration { get; set; }

    [JsonPropertyName("dimension")]
    public string Dimension { get; set; }

    [JsonPropertyName("definition")]
    public string Definition { get; set; }

    [JsonPropertyName("caption")]
    public string Caption { get; set; }

    [JsonPropertyName("licensedContent")]
    public bool LicensedContent { get; set; }

    [JsonPropertyName("contentRating")]
    public ContentRating ContentRating { get; set; }

    [JsonPropertyName("projection")]
    public string Projection { get; set; }
}

public class ContentRating
{
    // Content rating properties (like YouTube, MPAA, etc.)
    // Added based on need
}

public class VideoStatistics
{
    [JsonPropertyName("viewCount")]
    public long ViewCount { get; set; }

    [JsonPropertyName("likeCount")]
    public long LikeCount { get; set; }

    [JsonPropertyName("dislikeCount")]
    public long DislikeCount { get; set; }

    [JsonPropertyName("favoriteCount")]
    public long FavoriteCount { get; set; }

    [JsonPropertyName("commentCount")]
    public long CommentCount { get; set; }
}

public class VideoStatus
{
    [JsonPropertyName("uploadStatus")]
    public string UploadStatus { get; set; }

    [JsonPropertyName("privacyStatus")]
    public string PrivacyStatus { get; set; }

    [JsonPropertyName("license")]
    public string License { get; set; }

    [JsonPropertyName("embeddable")]
    public bool Embeddable { get; set; }

    [JsonPropertyName("publicStatsViewable")]
    public bool PublicStatsViewable { get; set; }

    [JsonPropertyName("madeForKids")]
    public bool MadeForKids { get; set; }
}
#endregion