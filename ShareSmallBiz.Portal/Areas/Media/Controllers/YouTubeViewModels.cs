using System.ComponentModel.DataAnnotations;

namespace ShareSmallBiz.Portal.Areas.Media.Controllers;

/// <summary>
/// View model for YouTube search results and index page
/// </summary>
public class YouTubeSearchViewModel
{
    [Display(Name = "Search Query")]
    [Required(ErrorMessage = "Please enter a search term")]
    public string Query { get; set; } = string.Empty;

    [Display(Name = "Max Results")]
    [Range(1, 50, ErrorMessage = "Please enter a value between 1 and 50")]
    public int MaxResults { get; set; } = 12;

    [Display(Name = "Page")]
    public int Page { get; set; } = 1;

    public List<YouTubeVideoViewModel> SearchResults { get; set; } = new();

    public List<string> PopularCategories { get; set; } = new();

    public List<ShareSmallBiz.Portal.Data.Media> RecentlyAdded { get; set; } = new();

    public List<YouTubeChannelListItemViewModel> PopularChannels { get; set; } = new();
}

/// <summary>
/// View model for a YouTube video in lists and search results
/// </summary>
public class YouTubeVideoViewModel
{
    public string VideoId { get; set; } = string.Empty;

    [Required(ErrorMessage = "Title is required")]
    [StringLength(255, ErrorMessage = "Title cannot exceed 255 characters")]
    public string Title { get; set; } = string.Empty;

    [StringLength(512, ErrorMessage = "Description cannot exceed 512 characters")]
    public string Description { get; set; } = string.Empty;

    public string ThumbnailUrl { get; set; } = string.Empty;

    public DateTime PublishedAt { get; set; }

    public string ChannelId { get; set; } = string.Empty;

    public string ChannelTitle { get; set; } = string.Empty;

    // Additional helper properties
    public string FormattedPublishDate => PublishedAt.ToString("MMM d, yyyy");

    public string TruncatedTitle => Title.Length > 60
        ? Title.Substring(0, 57) + "..."
        : Title;

    public string TruncatedDescription => Description?.Length > 120
        ? Description.Substring(0, 117) + "..."
        : Description ?? string.Empty;

    public string VideoUrl => $"https://www.youtube.com/watch?v={VideoId}";

    public string EmbedUrl => $"https://www.youtube.com/embed/{VideoId}";

    public string ChannelUrl => $"https://www.youtube.com/channel/{ChannelId}";

    // Methods for display formatting
    public string GetTruncatedTitle(int length = 60)
    {
        if (string.IsNullOrEmpty(Title) || Title.Length <= length)
            return Title;

        return Title.Substring(0, length - 3) + "...";
    }

    public string GetTruncatedDescription(int length = 120)
    {
        if (string.IsNullOrEmpty(Description) || Description.Length <= length)
            return Description ?? string.Empty;

        return Description.Substring(0, length - 3) + "...";
    }

    public string GetTimeAgo()
    {
        var timeSpan = DateTime.UtcNow - PublishedAt;

        if (timeSpan.TotalDays > 365)
            return $"{(int)(timeSpan.TotalDays / 365)} years ago";
        if (timeSpan.TotalDays > 30)
            return $"{(int)(timeSpan.TotalDays / 30)} months ago";
        if (timeSpan.TotalDays > 7)
            return $"{(int)(timeSpan.TotalDays / 7)} weeks ago";
        if (timeSpan.TotalDays >= 1)
            return $"{(int)timeSpan.TotalDays} days ago";
        if (timeSpan.TotalHours >= 1)
            return $"{(int)timeSpan.TotalHours} hours ago";
        if (timeSpan.TotalMinutes >= 1)
            return $"{(int)timeSpan.TotalMinutes} minutes ago";

        return "Just now";
    }
}

/// <summary>
/// Detailed view model for a specific YouTube video
/// </summary>
public class YouTubeVideoDetailViewModel
{
    public string VideoId { get; set; } = string.Empty;

    [Required(ErrorMessage = "Title is required")]
    [StringLength(255, ErrorMessage = "Title cannot exceed 255 characters")]
    public string Title { get; set; } = string.Empty;

    [StringLength(4000, ErrorMessage = "Description cannot exceed 4000 characters")]
    public string Description { get; set; } = string.Empty;

    public string ThumbnailUrl { get; set; } = string.Empty;

    public DateTime PublishedAt { get; set; }

    public string ChannelId { get; set; } = string.Empty;

    public string ChannelTitle { get; set; } = string.Empty;

    // Additional properties
    public string Duration { get; set; } = string.Empty;

    public string ViewCount { get; set; } = string.Empty;

    public string LikeCount { get; set; } = string.Empty;

    public string CommentCount { get; set; } = string.Empty;

    // Related videos
    public List<YouTubeVideoViewModel> RelatedVideos { get; set; } = new();

    // Helper properties
    public string FormattedPublishDate => PublishedAt.ToString("MMMM d, yyyy");

    public string ShortDescription => Description.Length > 300
        ? Description.Substring(0, 297) + "..."
        : Description;

    public string VideoUrl => $"https://www.youtube.com/watch?v={VideoId}";

    public string EmbedUrl => $"https://www.youtube.com/embed/{VideoId}";

    public string ChannelUrl => $"https://www.youtube.com/channel/{ChannelId}";

    // Additional helper methods
    public bool HasDescription => !string.IsNullOrWhiteSpace(Description);

    public bool HasRelatedVideos => RelatedVideos != null && RelatedVideos.Any();

    public string GetTruncatedTitle(int maxLength = 60) =>
        Title.Length > maxLength ? Title.Substring(0, maxLength - 3) + "..." : Title;

    public string GetFormattedDuration() => string.IsNullOrEmpty(Duration) ? "0:00" : Duration;

    public string GetFormattedViewCount() => string.IsNullOrEmpty(ViewCount) ? "0 views" : ViewCount;
}

/// <summary>
/// View model for YouTube channel list items
/// </summary>
public class YouTubeChannelListItemViewModel
{
    public string ChannelId { get; set; } = string.Empty;
    public string ChannelTitle { get; set; } = string.Empty;
    public int VideoCount { get; set; }
    public string ThumbnailUrl { get; set; } = string.Empty;

    // Helper properties
    public string ChannelUrl => $"https://www.youtube.com/channel/{ChannelId}";
}

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

    public List<ShareSmallBiz.Portal.Data.Media> UserVideosFromChannel { get; set; } = new();

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