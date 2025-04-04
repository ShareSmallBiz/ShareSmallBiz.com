namespace ShareSmallBiz.Portal.Areas.Media.Models;


/// <summary>
/// View model for a full YouTube channel page
/// </summary>
public class YouTubeChannelViewModel
{
    public string ChannelId { get; set; } = string.Empty;

    public string ChannelTitle { get; set; } = string.Empty;

    public string ChannelDescription { get; set; } = string.Empty;

    public string ThumbnailUrl { get; set; } = string.Empty;

    public long SubscriberCount { get; set; }

    public long VideoCount { get; set; }

    public long ViewCount { get; set; }

    public List<YouTubeVideoViewModel> Videos { get; set; } = new();

    public List<MediaModel> UserVideosFromChannel { get; set; } = new();

    // Pagination properties
    public int CurrentPage { get; set; } = 1;

    public int PageSize { get; set; } = 12;

    public int TotalPages => (int)Math.Ceiling((double)VideoCount / PageSize);

    // Helper properties
    public string FormattedSubscriberCount => SubscriberCount >= 1_000_000
        ? $"{SubscriberCount / 1_000_000.0:0.#}M"
        : SubscriberCount >= 1_000
            ? $"{SubscriberCount / 1_000.0:0.#}K"
            : SubscriberCount.ToString();

    public string FormattedVideoCount => VideoCount.ToString("N0");

    public string FormattedViewCount => ViewCount.ToString("N0");

    public string ChannelUrl => $"https://www.youtube.com/channel/{ChannelId}";

    public bool HasDescription => !string.IsNullOrWhiteSpace(ChannelDescription);

    public bool HasVideos => Videos != null && Videos.Any();

    public bool HasUserVideos => UserVideosFromChannel != null && UserVideosFromChannel.Any();

    public string GetTruncatedDescription(int maxLength = 300) =>
        ChannelDescription?.Length > maxLength
            ? ChannelDescription.Substring(0, maxLength - 3) + "..."
            : ChannelDescription ?? string.Empty;
}