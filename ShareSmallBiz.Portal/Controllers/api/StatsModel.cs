namespace ShareSmallBiz.Portal.Controllers.api;

/// <summary>
/// Platform-wide community statistics
/// </summary>
public class StatsModel
{
    public int TotalMembers { get; set; }
    public int TotalDiscussions { get; set; }
    public int TotalArticles { get; set; }
    public int TotalKeywords { get; set; }
    public int MemberGrowthThisMonth { get; set; }
}
