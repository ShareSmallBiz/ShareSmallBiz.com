using System.ComponentModel.DataAnnotations;

namespace ShareSmallBiz.Portal.Areas.Media.Models;

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

    public List<MediaModel> RecentlyAdded { get; set; } = new();

    public List<YouTubeChannelListItemViewModel> PopularChannels { get; set; } = new();

    public YouTubeChannelViewModel? ChannelResult { get; set; }
}
