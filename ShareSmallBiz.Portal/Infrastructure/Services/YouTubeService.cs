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
        // Step 1: Get the uploads playlist ID for the channel
        var uploadsPlaylistId = await GetChannelUploadsPlaylistIdAsync(channelId);
        if (string.IsNullOrEmpty(uploadsPlaylistId))
            return new List<YouTubeVideo>();

        // Step 2: Get the videos from the uploads playlist
        var videos = await GetPlaylistVideosAsync(uploadsPlaylistId, maxResults);

        // Store in cache for one hour
        var cacheEntryOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(CacheDuration);

        _cache.Set(cacheKey, videos, cacheEntryOptions);

        return videos;
    }

    private async Task<string> GetChannelUploadsPlaylistIdAsync(string channelId)
    {
        var httpClient = _httpClientFactory.CreateClient("YouTubeApi");

        var url = $"channels?part=contentDetails&id={channelId}&key={_options.ApiKey}";

        var response = await httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var channelResponse = await response.Content.ReadFromJsonAsync<ChannelResponse>();

        if (channelResponse?.Items == null || channelResponse.Items.Count == 0)
            return null;

        return channelResponse.Items[0].ContentDetails.RelatedPlaylists.Uploads;
    }

    private async Task<List<YouTubeVideo>> GetPlaylistVideosAsync(string playlistId, int maxResults)
    {
        var httpClient = _httpClientFactory.CreateClient("YouTubeApi");

        var url = $"playlistItems?part=snippet&maxResults={maxResults}&playlistId={playlistId}&key={_options.ApiKey}";

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
                ThumbnailUrl = snippet.Thumbnails.Medium.Url
            });
        }

        return videos;
    }
}

public interface IYouTubeService
{
    Task<List<YouTubeVideo>> GetChannelVideosAsync(string channelId, int maxResults = 10);
}

public class YouTubeApiOptions
{
    public string ApiKey { get; set; }
    public string BaseUrl { get; set; } = "https://www.googleapis.com/youtube/v3/";
}

public class YouTubeVideo
{
    public string VideoId { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public DateTime PublishedAt { get; set; }
    public string ThumbnailUrl { get; set; }

    public string VideoUrl => $"https://www.youtube.com/watch?v={VideoId}";
    public string EmbedUrl => $"https://www.youtube.com/embed/{VideoId}";
}

#region Response Models
public class ChannelResponse
{
    [JsonPropertyName("items")]
    public List<ChannelItem> Items { get; set; }
}

public class ChannelItem
{
    [JsonPropertyName("contentDetails")]
    public ContentDetails ContentDetails { get; set; }
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
}

public class PlaylistItemsResponse
{
    [JsonPropertyName("items")]
    public List<PlaylistItem> Items { get; set; }
}

public class PlaylistItem
{
    [JsonPropertyName("snippet")]
    public PlaylistItemSnippet Snippet { get; set; }
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
}

public class Thumbnails
{
    [JsonPropertyName("medium")]
    public Thumbnail Medium { get; set; }
}

public class Thumbnail
{
    [JsonPropertyName("url")]
    public string Url { get; set; }
}

public class ResourceId
{
    [JsonPropertyName("videoId")]
    public string VideoId { get; set; }
}
#endregion
