namespace ShareSmallBiz.Portal.Areas.Media.Models
{

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
}
