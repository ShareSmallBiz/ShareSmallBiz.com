using System.Security.Claims;

namespace ShareSmallBiz.Portal.Infrastructure.Services;

/// <summary>
/// Defines the supported sorting options.
/// </summary>
public enum SortType
{
    Recent,
    Popular,
    All
}

/// <summary>
/// A container for a list of posts plus metadata about the current page.
/// </summary>
public class PaginatedPostResult
{
    public List<DiscussionModel> Posts { get; set; } = [];
    public int CurrentPage { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}
