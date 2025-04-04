using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace ShareSmallBiz.Portal.Areas.Media.Services;

/// <summary>
/// Service for interacting with the YouTube API and handling YouTube-specific operations
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
    /// Get details about a YouTube channel by username instead of channel ID
    /// </summary>
    /// <param name="username">YouTube channel username</param>
    /// <returns>Channel response with channel details</returns>
    public async Task<YouTubeChannelResponse?> GetChannelByUsernameAsync(string username)
    {
        try
        {
            var channelUrl = $"https://www.googleapis.com/youtube/v3/channels?part=snippet,statistics&forUsername={username}&key={_youTubeApiKey}";
            var httpClient = _httpClientFactory.CreateClient();
            var response = await httpClient.GetAsync(channelUrl);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<YouTubeChannelResponse>();
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("YouTube API error when fetching channel by username: {StatusCode} - {Error}", response.StatusCode, errorContent);
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting YouTube channel details for username {Username}", username);
            throw;
        }
    }

    /// <summary>
    /// Get videos from a specific YouTube channel
    /// </summary>
    /// <param name="channelId">YouTube channel ID</param>
    /// <param name="maxResults">Maximum number of results to return</param>
    /// <param name="pageToken">Token for pagination</param>
    /// <returns>Search response with channel's videos</returns>
    public async Task<YouTubeSearchResponse?> GetChannelVideosAsync(string channelId, int maxResults = 12, string? pageToken = null)
    {
        try
        {
            var videosUrl = $"https://www.googleapis.com/youtube/v3/search?part=snippet&channelId={channelId}&maxResults={maxResults}&order=date&type=video&key={_youTubeApiKey}";

            // Add page token if provided
            if (!string.IsNullOrEmpty(pageToken))
            {
                videosUrl += $"&pageToken={pageToken}";
            }

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
    /// Get details about a specific YouTube video
    /// </summary>
    /// <param name="videoId">YouTube video ID</param>
    /// <returns>Video details</returns>
    public async Task<YouTubeVideoDetailResponse?> GetVideoDetailsAsync(string videoId)
    {
        try
        {
            var videoUrl = $"https://www.googleapis.com/youtube/v3/videos?part=snippet,statistics,contentDetails&id={videoId}&key={_youTubeApiKey}";
            var httpClient = _httpClientFactory.CreateClient();
            var response = await httpClient.GetAsync(videoUrl);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<YouTubeVideoDetailResponse>();
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("YouTube API error when fetching video details: {StatusCode} - {Error}", response.StatusCode, errorContent);
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting details for YouTube video {VideoId}", videoId);
            throw;
        }
    }

    /// <summary>
    /// Get related videos for a specific YouTube video
    /// </summary>
    /// <param name="videoId">YouTube video ID</param>
    /// <param name="maxResults">Maximum number of results to return</param>
    /// <returns>Search response with related videos</returns>
    public async Task<YouTubeSearchResponse?> GetRelatedVideosAsync(string videoId, int maxResults = 10)
    {
        try
        {
            var relatedUrl = $"https://www.googleapis.com/youtube/v3/search?part=snippet&relatedToVideoId={videoId}&type=video&maxResults={maxResults}&key={_youTubeApiKey}";
            var httpClient = _httpClientFactory.CreateClient();
            var response = await httpClient.GetAsync(relatedUrl);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<YouTubeSearchResponse>();
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("YouTube API error when fetching related videos: {StatusCode} - {Error}", response.StatusCode, errorContent);
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting related videos for YouTube video {VideoId}", videoId);
            throw;
        }
    }

    /// <summary>
    /// Check if a URL is a YouTube URL
    /// </summary>
    public bool IsYouTubeUrl(string? url)
    {
        if (string.IsNullOrEmpty(url))
            return false;

        // Check various YouTube URL formats
        return url.Contains("youtube.com/watch") ||
               url.Contains("youtu.be/") ||
               url.Contains("youtube.com/embed/") ||
               url.Contains("youtube.com/v/");
    }

    /// <summary>
    /// Extract YouTube video ID from URL
    /// </summary>
    public string ExtractVideoIdFromUrl(string? url)
    {
        if (string.IsNullOrEmpty(url))
        {
            return string.Empty;
        }

        // Match standard YouTube URLs
        // Handles formats like:
        // - https://www.youtube.com/watch?v=VIDEO_ID
        // - https://youtu.be/VIDEO_ID
        // - https://youtube.com/watch?v=VIDEO_ID
        // - https://www.youtube.com/embed/VIDEO_ID
        var regex = new Regex(@"(?:https?:\/\/)?(?:www\.)?(?:youtube\.com\/(?:watch\?v=|embed\/|v\/)|youtu\.be\/)([a-zA-Z0-9_-]{11})");

        var match = regex.Match(url);
        if (match.Success && match.Groups.Count > 1)
        {
            return match.Groups[1].Value; // Return the VIDEO_ID
        }

        return string.Empty;
    }

    /// <summary>
    /// Extract YouTube channel ID from URL
    /// </summary>
    public string ExtractChannelIdFromUrl(string? url)
    {
        if (string.IsNullOrEmpty(url))
        {
            return string.Empty;
        }

        // Match standard YouTube channel URLs
        // Handles formats like:
        // - https://www.youtube.com/channel/CHANNEL_ID
        // - https://youtube.com/channel/CHANNEL_ID
        var regex = new Regex(@"(?:https?:\/\/)?(?:www\.)?youtube\.com\/channel\/([a-zA-Z0-9_-]+)");

        var match = regex.Match(url);
        if (match.Success && match.Groups.Count > 1)
        {
            return match.Groups[1].Value; // Return the CHANNEL_ID
        }

        return string.Empty;
    }

    /// <summary>
    /// Extract YouTube username from URL
    /// </summary>
    public string ExtractUsernameFromUrl(string? url)
    {
        if (string.IsNullOrEmpty(url))
        {
            return string.Empty;
        }

        // Match standard YouTube user URLs
        // Handles formats like:
        // - https://www.youtube.com/user/USERNAME
        // - https://youtube.com/user/USERNAME
        var regex = new Regex(@"(?:https?:\/\/)?(?:www\.)?youtube\.com\/user\/([a-zA-Z0-9_-]+)");

        var match = regex.Match(url);
        if (match.Success && match.Groups.Count > 1)
        {
            return match.Groups[1].Value; // Return the USERNAME
        }

        return string.Empty;
    }

    /// <summary>
    /// Get YouTube embed URL from a video ID
    /// </summary>
    public string GetEmbedUrlFromVideoId(string videoId)
    {
        if (string.IsNullOrEmpty(videoId))
            return string.Empty;

        return $"https://www.youtube.com/embed/{videoId}";
    }

    /// <summary>
    /// Get YouTube channel URL from a channel ID
    /// </summary>
    public string GetChannelUrl(string channelId)
    {
        if (string.IsNullOrEmpty(channelId))
            return string.Empty;

        return $"https://www.youtube.com/channel/{channelId}";
    }

    /// <summary>
    /// Get YouTube embed URL from a video URL
    /// </summary>
    public string GetEmbedUrlFromVideoUrl(string url)
    {
        string videoId = ExtractVideoIdFromUrl(url);
        if (string.IsNullOrEmpty(videoId))
        {
            return string.Empty;
        }

        return GetEmbedUrlFromVideoId(videoId);
    }

    /// <summary>
    /// Updates a YouTube embed URL with parameters
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
    /// Format duration from ISO 8601 format to user-friendly format
    /// </summary>
    public string FormatDuration(string isoDuration)
    {
        if (string.IsNullOrEmpty(isoDuration))
        {
            return "0:00";
        }

        try
        {
            // Parse the ISO 8601 duration
            var duration = System.Xml.XmlConvert.ToTimeSpan(isoDuration);

            // Format the duration
            if (duration.TotalHours >= 1)
            {
                return $"{(int)duration.TotalHours}:{duration.Minutes:D2}:{duration.Seconds:D2}";
            }
            else
            {
                return $"{duration.Minutes}:{duration.Seconds:D2}";
            }
        }
        catch
        {
            return "0:00";
        }
    }

    /// <summary>
    /// Format view count to user-friendly format
    /// </summary>
    public string FormatViewCount(string viewCount)
    {
        if (string.IsNullOrEmpty(viewCount) || !long.TryParse(viewCount, out long views))
        {
            return "0 views";
        }

        if (views >= 1_000_000_000)
        {
            return $"{views / 1_000_000_000.0:0.#}B views";
        }
        else if (views >= 1_000_000)
        {
            return $"{views / 1_000_000.0:0.#}M views";
        }
        else if (views >= 1_000)
        {
            return $"{views / 1_000.0:0.#}K views";
        }
        else
        {
            return $"{views} views";
        }
    }

    /// <summary>
    /// Creates a metadata dictionary for a YouTube video
    /// </summary>
    public Dictionary<string, string> CreateVideoMetadata(
        string videoId,
        string originalUrl,
        string channelId,
        string channelTitle,
        DateTime publishedDate,
        string duration = "",
        string viewCount = "")
    {
        return new Dictionary<string, string>
        {
            { "videoId", videoId },
            { "originalUrl", originalUrl },
            { "channelId", channelId },
            { "channelTitle", channelTitle },
            { "publishedDate", publishedDate.ToString("o") },
            { "duration", duration },
            { "viewCount", viewCount }
        };
    }

    /// <summary>
    /// Creates a metadata dictionary for a YouTube channel
    /// </summary>
    public Dictionary<string, string> CreateChannelMetadata(
        string channelId,
        string channelTitle)
    {
        return new Dictionary<string, string>
        {
            { "channelId", channelId },
            { "channelTitle", channelTitle },
            { "type", "youtube_channel" }
        };
    }

    /// <summary>
    /// Extract metadata from JSON string
    /// </summary>
    public Dictionary<string, string>? ExtractMetadataFromJson(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, string>>(json);
        }
        catch
        {
            return null;
        }
    }
}

#region YouTube API Models

/// <summary>
/// YouTube Search Response
/// </summary>
public class YouTubeSearchResponse
{
    [JsonPropertyName("items")]
    public List<YouTubeSearchItem> Items { get; set; } = new();

    [JsonPropertyName("nextPageToken")]
    public string NextPageToken { get; set; } = string.Empty;

    [JsonPropertyName("prevPageToken")]
    public string PrevPageToken { get; set; } = string.Empty;

    [JsonPropertyName("pageInfo")]
    public YouTubePageInfo PageInfo { get; set; } = new();
}

/// <summary>
/// YouTube Page Info
/// </summary>
public class YouTubePageInfo
{
    [JsonPropertyName("totalResults")]
    public int TotalResults { get; set; }

    [JsonPropertyName("resultsPerPage")]
    public int ResultsPerPage { get; set; }
}

/// <summary>
/// YouTube Search Item
/// </summary>
public class YouTubeSearchItem
{
    [JsonPropertyName("id")]
    public YouTubeVideoId Id { get; set; } = new();

    [JsonPropertyName("snippet")]
    public YouTubeSnippet Snippet { get; set; } = new();
}

/// <summary>
/// YouTube Video ID
/// </summary>
public class YouTubeVideoId
{
    [JsonPropertyName("videoId")]
    public string VideoId { get; set; } = string.Empty;
}

/// <summary>
/// YouTube Snippet
/// </summary>
public class YouTubeSnippet
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("publishedAt")]
    public DateTime PublishedAt { get; set; }

    [JsonPropertyName("thumbnails")]
    public YouTubeThumbnails Thumbnails { get; set; } = new();

    [JsonPropertyName("channelId")]
    public string ChannelId { get; set; } = string.Empty;

    [JsonPropertyName("channelTitle")]
    public string ChannelTitle { get; set; } = string.Empty;
}

/// <summary>
/// YouTube Thumbnails
/// </summary>
public class YouTubeThumbnails
{
    [JsonPropertyName("default")]
    public YouTubeThumbnail Default { get; set; } = new();

    [JsonPropertyName("medium")]
    public YouTubeThumbnail Medium { get; set; } = new();

    [JsonPropertyName("high")]
    public YouTubeThumbnail High { get; set; } = new();
}

/// <summary>
/// YouTube Thumbnail
/// </summary>
public class YouTubeThumbnail
{
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("width")]
    public int Width { get; set; }

    [JsonPropertyName("height")]
    public int Height { get; set; }
}

/// <summary>
/// YouTube Channel Response
/// </summary>
public class YouTubeChannelResponse
{
    [JsonPropertyName("items")]
    public List<YouTubeChannelItem> Items { get; set; } = new();
}

/// <summary>
/// YouTube Channel Item
/// </summary>
public class YouTubeChannelItem
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("snippet")]
    public YouTubeSnippet Snippet { get; set; } = new();

    [JsonPropertyName("statistics")]
    public YouTubeChannelStatistics Statistics { get; set; } = new();
}

/// <summary>
/// YouTube Channel Statistics
/// </summary>
public class YouTubeChannelStatistics
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

/// <summary>
/// YouTube Video Detail Response
/// </summary>
public class YouTubeVideoDetailResponse
{
    [JsonPropertyName("items")]
    public List<YouTubeVideoItem> Items { get; set; } = new();
}

/// <summary>
/// YouTube Video Item
/// </summary>
public class YouTubeVideoItem
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("snippet")]
    public YouTubeSnippet Snippet { get; set; } = new();

    [JsonPropertyName("contentDetails")]
    public YouTubeContentDetails ContentDetails { get; set; } = new();

    [JsonPropertyName("statistics")]
    public YouTubeVideoStatistics Statistics { get; set; } = new();
}

/// <summary>
/// YouTube Content Details
/// </summary>
public class YouTubeContentDetails
{
    [JsonPropertyName("duration")]
    public string Duration { get; set; } = string.Empty;

    [JsonPropertyName("dimension")]
    public string Dimension { get; set; } = string.Empty;

    [JsonPropertyName("definition")]
    public string Definition { get; set; } = string.Empty;

    [JsonPropertyName("caption")]
    public string Caption { get; set; } = string.Empty;

    [JsonPropertyName("licensedContent")]
    public bool LicensedContent { get; set; }
}

/// <summary>
/// YouTube Video Statistics
/// </summary>
public class YouTubeVideoStatistics
{
    [JsonPropertyName("viewCount")]
    public string ViewCount { get; set; } = string.Empty;

    [JsonPropertyName("likeCount")]
    public string LikeCount { get; set; } = string.Empty;

    [JsonPropertyName("favoriteCount")]
    public string FavoriteCount { get; set; } = string.Empty;

    [JsonPropertyName("commentCount")]
    public string CommentCount { get; set; } = string.Empty;
}

#endregion