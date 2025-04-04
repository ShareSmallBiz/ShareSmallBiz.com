namespace ShareSmallBiz.Portal.Areas.Media.Models
{

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
}
