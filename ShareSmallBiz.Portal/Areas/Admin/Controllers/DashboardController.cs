using ShareSmallBiz.Portal.Data;
using ShareSmallBiz.Portal.Infrastructure.Services;

namespace ShareSmallBiz.Portal.Areas.Admin.Controllers;

public class DashboardController(
    ShareSmallBizUserContext _context,
    ShareSmallBizUserManager _userManager,
    RoleManager<IdentityRole> _roleManager,
    DiscussionProvider _discussionProvider,
    ILogger<DashboardController> _logger) : AdminBaseController(_context, _userManager, _roleManager)
{
    public async Task<IActionResult> Index()
    {
        var dashboardViewModel = new DashboardViewModel
        {
            UserStats = await GetUserStatsAsync(),
            DiscussionStats = await GetDiscussionStatsAsync(),
            CommentStats = await GetCommentStatsAsync(),
            RecentUsers = await GetRecentUsersAsync(5),
            RecentDiscussions = await GetRecentDiscussionsAsync(5),
            RecentComments = await GetRecentCommentsAsync(5)
        };

        return View(dashboardViewModel);
    }

    private async Task<UserStatsViewModel> GetUserStatsAsync()
    {
        var totalUsers = await _userManager.Users.CountAsync();
        var verifiedUsers = await _userManager.Users.CountAsync(u => u.EmailConfirmed);
        var businessRoleId = await _roleManager.Roles
            .Where(r => r.Name == "Business")
            .Select(r => r.Id)
            .FirstOrDefaultAsync();

        int businessUsers = 0;

        if (!string.IsNullOrEmpty(businessRoleId))
        {
            businessUsers = await _context.UserRoles
                .CountAsync(ur => ur.RoleId == businessRoleId);
        }

        // Get user registration counts for the last 6 months
        var sixMonthsAgo = DateTime.UtcNow.AddMonths(-6);
        var usersByMonth = await _userManager.Users
            .Where(u => u.LastModified >= sixMonthsAgo)
            .GroupBy(u => new { u.LastModified.Year, u.LastModified.Month })
            .Select(g => new
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                Count = g.Count()
            })
            .OrderBy(x => x.Year)
            .ThenBy(x => x.Month)
            .ToListAsync();

        var monthlyCounts = new Dictionary<string, int>();
        for (int i = 0; i < 6; i++)
        {
            var date = DateTime.UtcNow.AddMonths(-5 + i);
            var monthYear = date.ToString("MMM yyyy");
            var count = usersByMonth
                .FirstOrDefault(x => x.Year == date.Year && x.Month == date.Month)?.Count ?? 0;
            monthlyCounts[monthYear] = count;
        }

        return new UserStatsViewModel
        {
            TotalUsers = totalUsers,
            VerifiedUsers = verifiedUsers,
            BusinessUsers = businessUsers,
            RecentRegistrations = monthlyCounts
        };
    }

    private async Task<DiscussionStatsViewModel> GetDiscussionStatsAsync()
    {
        var totalDiscussions = await _context.Posts.CountAsync();
        var publicDiscussions = await _context.Posts.CountAsync(d => d.IsPublic);
        var featuredDiscussions = await _context.Posts.CountAsync(d => d.IsFeatured);

        // Get popular discussions
        var popularDiscussions = await _context.Posts
            .OrderByDescending(d => d.PostViews)
            .Take(5)
            .Select(d => new { d.Id, d.Title, d.PostViews })
            .ToListAsync();

        var popularDiscussionStats = popularDiscussions
            .ToDictionary(
                d => d.Title,
                d => d.PostViews);

        // Get discussion counts for the last 6 months
        var sixMonthsAgo = DateTime.UtcNow.AddMonths(-6);
        var discussionsByMonth = await _context.Posts
            .Where(d => d.CreatedDate >= sixMonthsAgo)
            .GroupBy(d => new { d.CreatedDate.Year, d.CreatedDate.Month })
            .Select(g => new
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                Count = g.Count()
            })
            .OrderBy(x => x.Year)
            .ThenBy(x => x.Month)
            .ToListAsync();

        var monthlyCounts = new Dictionary<string, int>();
        for (int i = 0; i < 6; i++)
        {
            var date = DateTime.UtcNow.AddMonths(-5 + i);
            var monthYear = date.ToString("MMM yyyy");
            var count = discussionsByMonth
                .FirstOrDefault(x => x.Year == date.Year && x.Month == date.Month)?.Count ?? 0;
            monthlyCounts[monthYear] = count;
        }

        return new DiscussionStatsViewModel
        {
            TotalDiscussions = totalDiscussions,
            PublicDiscussions = publicDiscussions,
            FeaturedDiscussions = featuredDiscussions,
            PopularDiscussions = popularDiscussionStats,
            MonthlyDiscussions = monthlyCounts
        };
    }

    private async Task<CommentStatsViewModel> GetCommentStatsAsync()
    {
        var totalComments = await _context.PostComments.CountAsync();

        // Get most commented discussions
        var mostCommentedPosts = await _context.Posts
            .Include(p => p.Comments)
            .OrderByDescending(p => p.Comments.Count)
            .Take(5)
            .Select(p => new { p.Id, p.Title, CommentCount = p.Comments.Count })
            .ToListAsync();

        var mostCommentedStats = mostCommentedPosts
            .ToDictionary(
                p => p.Title,
                p => p.CommentCount);

        // Get comment counts for the last 6 months
        var sixMonthsAgo = DateTime.UtcNow.AddMonths(-6);
        var commentsByMonth = await _context.PostComments
            .Where(c => c.CreatedDate >= sixMonthsAgo)
            .GroupBy(c => new { c.CreatedDate.Year, c.CreatedDate.Month })
            .Select(g => new
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                Count = g.Count()
            })
            .OrderBy(x => x.Year)
            .ThenBy(x => x.Month)
            .ToListAsync();

        var monthlyCounts = new Dictionary<string, int>();
        for (int i = 0; i < 6; i++)
        {
            var date = DateTime.UtcNow.AddMonths(-5 + i);
            var monthYear = date.ToString("MMM yyyy");
            var count = commentsByMonth
                .FirstOrDefault(x => x.Year == date.Year && x.Month == date.Month)?.Count ?? 0;
            monthlyCounts[monthYear] = count;
        }

        return new CommentStatsViewModel
        {
            TotalComments = totalComments,
            MostCommentedDiscussions = mostCommentedStats,
            MonthlyComments = monthlyCounts
        };
    }

    private async Task<List<UserModel>> GetRecentUsersAsync(int count)
    {
        var recentUsers = await _userManager.Users
            .OrderByDescending(u => u.LastModified)
            .Take(count)
            .ToListAsync();

        return recentUsers.Select(u => new UserModel
        {
            Id = u.Id,
            UserName = u.UserName,
            Email = u.Email,
            DisplayName = u.DisplayName,
            IsEmailConfirmed = u.EmailConfirmed,
            FirstName = u.FirstName,
            LastName = u.LastName,
            ProfilePictureUrl = u.ProfilePictureUrl
        }).ToList();
    }

    private async Task<List<DiscussionModel>> GetRecentDiscussionsAsync(int count)
    {
        return await _discussionProvider.MostRecentPostsAsync(count);
    }

    private async Task<List<PostCommentModel>> GetRecentCommentsAsync(int count)
    {
        var recentComments = await _context.PostComments
            .Include(c => c.Creator)
            .Include(c => c.Post)
            .OrderByDescending(c => c.CreatedDate)
            .Take(count)
            .ToListAsync();

        return recentComments.Select(c => new PostCommentModel(c)).ToList();
    }

    // Additional action methods for specific dashboard sections
    public async Task<IActionResult> UserStatistics()
    {
        var userStats = await GetUserStatsAsync();
        return View(userStats);
    }

    public async Task<IActionResult> DiscussionStatistics()
    {
        var discussionStats = await GetDiscussionStatsAsync();
        return View(discussionStats);
    }

    public async Task<IActionResult> CommentStatistics()
    {
        var commentStats = await GetCommentStatsAsync();
        return View(commentStats);
    }
}

// View models for the dashboard
public class DashboardViewModel
{
    public UserStatsViewModel UserStats { get; set; }
    public DiscussionStatsViewModel DiscussionStats { get; set; }
    public CommentStatsViewModel CommentStats { get; set; }
    public List<UserModel> RecentUsers { get; set; }
    public List<DiscussionModel> RecentDiscussions { get; set; }
    public List<PostCommentModel> RecentComments { get; set; }
}

public class UserStatsViewModel
{
    public int TotalUsers { get; set; }
    public int VerifiedUsers { get; set; }
    public int BusinessUsers { get; set; }
    public Dictionary<string, int> RecentRegistrations { get; set; } = new();
}

public class DiscussionStatsViewModel
{
    public int TotalDiscussions { get; set; }
    public int PublicDiscussions { get; set; }
    public int FeaturedDiscussions { get; set; }
    public Dictionary<string, int> PopularDiscussions { get; set; } = new();
    public Dictionary<string, int> MonthlyDiscussions { get; set; } = new();
}

public class CommentStatsViewModel
{
    public int TotalComments { get; set; }
    public Dictionary<string, int> MostCommentedDiscussions { get; set; } = new();
    public Dictionary<string, int> MonthlyComments { get; set; } = new();
}