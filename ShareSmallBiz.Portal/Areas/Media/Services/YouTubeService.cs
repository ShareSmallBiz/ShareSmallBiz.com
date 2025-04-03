using System.Net.Http.Json;
using System.Text.Json;

namespace ShareSmallBiz.Portal.Areas.Media.Services;

/// <summary>
/// Service for interacting with the YouTube API
/// </summary>
public class YouTubeService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<YouTubeService> _logger;
    private readonly string _youTubeApiKey;

    public YouTubeService(
        IHttpClientFactory httpClientFactory,
        ILogger<YouTubeService> logger,
        IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _youTubeApiKey = configuration["GOOGLE_YOUTUBE_API_KEY"] ??
            throw new ArgumentNullException("GOOGLE_YOUTUBE_API_KEY is not configured");
    }

    /// <summary>
    /// Search YouTube videos with the given query
    /// </summary>
    /// <param name="query">Search query</param>
    /// <param name="maxResults">Maximum number of results to return</param>
    /// <returns>Search response with video results</returns>
    public async Task<YouTubeSearchResponse?> SearchVideosAsync(string query, int maxResults = 10)
    {
        try
        {
            // Build the YouTube API request
            var searchQuery = Uri.EscapeDataString(query);
            maxResults = maxResults > 0 ? maxResults : 10;
            var requestUrl = $"https://www.googleapis.com/youtube/v3/search?part=snippet&q={searchQuery}&maxResults={maxResults}&type=video&key={_youTubeApiKey}";

            // Create HttpClient and send request
            var httpClient = _httpClientFactory.CreateClient();
            var response = await httpClient.GetAsync(requestUrl);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<YouTubeSearchResponse>();
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("YouTube API error: {StatusCode} - {Error}", response.StatusCode, errorContent);
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching YouTube videos");
            throw;
        }
    }

    /// <summary>
    /// Get details about a YouTube channel
    /// </summary>
    /// <param name="channelId">YouTube channel ID</param>
    /// <returns>Channel response with channel details</returns>
    public async Task<YouTubeChannelResponse?> GetChannelDetailsAsync(string channelId)
    {
        try
        {
            var channelUrl = $"https://www.googleapis.com/youtube/v3/channels?part=snippet,statistics&id={channelId}&key={_youTubeApiKey}";
            var httpClient = _httpClientFactory.CreateClient();
            var response = await httpClient.GetAsync(channelUrl);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<YouTubeChannelResponse>();
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("YouTube API error when fetching channel: {StatusCode} - {Error}", response.StatusCode, errorContent);
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting YouTube channel details for {ChannelId}", channelId);
            throw;
        }
    }

    /// <summary>
    /// Get videos from a specific YouTube channel
    /// </summary>
    /// <param name="channelId">YouTube channel ID</param>
    /// <param name="maxResults">Maximum number of results to return</param>
    /// <returns>Search response with channel's videos</returns>
    public async Task<YouTubeSearchResponse?> GetChannelVideosAsync(string channelId, int maxResults = 12)
    {
        try
        {
            var videosUrl = $"https://www.googleapis.com/youtube/v3/search?part=snippet&channelId={channelId}&maxResults={maxResults}&order=date&type=video&key={_youTubeApiKey}";
            var httpClient = _httpClientFactory.CreateClient();
            var response = await httpClient.GetAsync(videosUrl);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<YouTubeSearchResponse>();
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("YouTube API error when fetching channel videos: {StatusCode} - {Error}", response.StatusCode, errorContent);
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting videos for YouTube channel {ChannelId}", channelId);
            throw;
        }
    }

    /// <summary>
    /// Extract YouTube video ID from URL
    /// </summary>
    /// <param name="url">YouTube URL</param>
    /// <returns>Video ID or null if invalid</returns>
    public static string? ExtractVideoIdFromUrl(string? url)
    {
        if (string.IsNullOrEmpty(url))
        {
            return null;
        }

        // Match standard YouTube URLs
        // Handles formats like:
        // - https://www.youtube.com/watch?v=VIDEO_ID
        // - https://youtu.be/VIDEO_ID
        // - https://youtube.com/watch?v=VIDEO_ID
        // - https://www.youtube.com/embed/VIDEO_ID
        var regex = new System.Text.RegularExpressions.Regex(@"(?:https?:\/\/)?(?:www\.)?(?:youtube\.com\/(?:watch\?v=|embed\/)|youtu\.be\/)([a-zA-Z0-9_-]{11})");

        var match = regex.Match(url);
        if (match.Success && match.Groups.Count > 1)
        {
            return match.Groups[1].Value; // Return the VIDEO_ID
        }

        return null; // Return null if not a valid YouTube URL
    }

    /// <summary>
    /// Get YouTube embed URL from a YouTube video URL
    /// </summary>
    /// <param name="url">YouTube URL</param>
    /// <returns>Embed URL or empty string if invalid</returns>
    public static string GetEmbedUrlFromVideoUrl(string url)
    {
        var videoId = ExtractVideoIdFromUrl(url);
        if (string.IsNullOrEmpty(videoId))
        {
            return string.Empty;
        }

        return $"https://www.youtube.com/embed/{videoId}";
    }
}

// JSON response models for YouTube API
// These were previously in YouTubeController, moved here for better organization
public class YouTubeSearchResponse
{
    [System.Text.Json.Serialization.JsonPropertyName("items")]
    public List<YouTubeSearchItem> Items { get; set; } = new();

    [System.Text.Json.Serialization.JsonPropertyName("nextPageToken")]
    public string NextPageToken { get; set; } = string.Empty;

    [System.Text.Json.Serialization.JsonPropertyName("prevPageToken")]
    public string PrevPageToken { get; set; } = string.Empty;

    [System.Text.Json.Serialization.JsonPropertyName("pageInfo")]
    public YouTubePageInfo PageInfo { get; set; } = new();
}

public class YouTubePageInfo
{
    [System.Text.Json.Serialization.JsonPropertyName("totalResults")]
    public int TotalResults { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("resultsPerPage")]
    public int ResultsPerPage { get; set; }
}

public class YouTubeSearchItem
{
    [System.Text.Json.Serialization.JsonPropertyName("id")]
    public YouTubeVideoId Id { get; set; } = new();

    [System.Text.Json.Serialization.JsonPropertyName("snippet")]
    public YouTubeSnippet Snippet { get; set; } = new();
}

public class YouTubeVideoId
{
    [System.Text.Json.Serialization.JsonPropertyName("videoId")]
    public string VideoId { get; set; } = string.Empty;
}

public class YouTubeSnippet
{
    [System.Text.Json.Serialization.JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [System.Text.Json.Serialization.JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [System.Text.Json.Serialization.JsonPropertyName("publishedAt")]
    public DateTime PublishedAt { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("thumbnails")]
    public YouTubeThumbnails Thumbnails { get; set; } = new();

    [System.Text.Json.Serialization.JsonPropertyName("channelId")]
    public string ChannelId { get; set; } = string.Empty;

    [System.Text.Json.Serialization.JsonPropertyName("channelTitle")]
    public string ChannelTitle { get; set; } = string.Empty;
}

public class YouTubeThumbnails
{
    [System.Text.Json.Serialization.JsonPropertyName("default")]
    public YouTubeThumbnail Default { get; set; } = new();

    [System.Text.Json.Serialization.JsonPropertyName("medium")]
    public YouTubeThumbnail Medium { get; set; } = new();

    [System.Text.Json.Serialization.JsonPropertyName("high")]
    public YouTubeThumbnail High { get; set; } = new();
}

public class YouTubeThumbnail
{
    [System.Text.Json.Serialization.JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [System.Text.Json.Serialization.JsonPropertyName("width")]
    public int Width { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("height")]
    public int Height { get; set; }
}

public class YouTubeChannelResponse
{
    [System.Text.Json.Serialization.JsonPropertyName("items")]
    public List<YouTubeChannelItem> Items { get; set; } = new();
}

public class YouTubeChannelItem
{
    [System.Text.Json.Serialization.JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [System.Text.Json.Serialization.JsonPropertyName("snippet")]
    public YouTubeSnippet Snippet { get; set; } = new();

    [System.Text.Json.Serialization.JsonPropertyName("statistics")]
    public YouTubeChannelStatistics Statistics { get; set; } = new();
}

public class YouTubeChannelStatistics
{
    [System.Text.Json.Serialization.JsonPropertyName("viewCount")]
    public long ViewCount { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("subscriberCount")]
    public long SubscriberCount { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("hiddenSubscriberCount")]
    public bool HiddenSubscriberCount { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("videoCount")]
    public long VideoCount { get; set; }
}