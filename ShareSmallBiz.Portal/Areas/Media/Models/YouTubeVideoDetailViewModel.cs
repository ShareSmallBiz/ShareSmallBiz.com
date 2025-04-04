namespace ShareSmallBiz.Portal.Areas.Media.Models
{

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
}
