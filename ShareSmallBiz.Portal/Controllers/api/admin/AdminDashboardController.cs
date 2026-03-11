using Microsoft.AspNetCore.Authentication.JwtBearer;
using ShareSmallBiz.Portal.Data;
using ShareSmallBiz.Portal.Infrastructure.Services;

namespace ShareSmallBiz.Portal.Controllers.api.admin;

[ApiController]
[Route("api/admin/dashboard")]
[Authorize(Roles = "Admin", AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class AdminDashboardController(
    ShareSmallBizUserContext context,
    ShareSmallBizUserManager userManager,
    RoleManager<IdentityRole> roleManager,
    DiscussionProvider discussionProvider,
    ILogger<AdminDashboardController> logger) : ControllerBase
{
    /// <summary>GET /api/admin/dashboard — full dashboard stats snapshot</summary>
    [HttpGet]
    public async Task<IActionResult> GetDashboard()
    {
        var result = new
        {
            Users = await GetUserStatsAsync(),
            Discussions = await GetDiscussionStatsAsync(),
            Comments = await GetCommentStatsAsync(),
            RecentUsers = await GetRecentAsync<UserModel>(5, "users"),
            RecentDiscussions = await discussionProvider.MostRecentPostsAsync(5),
            RecentComments = await GetRecentCommentsAsync(5)
        };
        return Ok(result);
    }

    /// <summary>GET /api/admin/dashboard/users — user statistics</summary>
    [HttpGet("users")]
    public async Task<IActionResult> GetUserStats() => Ok(await GetUserStatsAsync());

    /// <summary>GET /api/admin/dashboard/discussions — discussion statistics</summary>
    [HttpGet("discussions")]
    public async Task<IActionResult> GetDiscussionStats() => Ok(await GetDiscussionStatsAsync());

    /// <summary>GET /api/admin/dashboard/comments — comment statistics</summary>
    [HttpGet("comments")]
    public async Task<IActionResult> GetCommentStats() => Ok(await GetCommentStatsAsync());

    // ---- private helpers (same logic as MVC DashboardController) ----

    private async Task<object> GetUserStatsAsync()
    {
        var totalUsers = await userManager.Users.CountAsync();
        var verifiedUsers = await userManager.Users.CountAsync(u => u.EmailConfirmed);

        var businessRoleId = await roleManager.Roles
            .Where(r => r.Name == "Business")
            .Select(r => r.Id)
            .FirstOrDefaultAsync();

        int businessUsers = string.IsNullOrEmpty(businessRoleId) ? 0
            : await context.UserRoles.CountAsync(ur => ur.RoleId == businessRoleId);

        var sixMonthsAgo = DateTime.UtcNow.AddMonths(-6);
        var byMonth = await userManager.Users
            .Where(u => u.LastModified >= sixMonthsAgo)
            .GroupBy(u => new { u.LastModified.Year, u.LastModified.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count() })
            .OrderBy(x => x.Year).ThenBy(x => x.Month)
            .ToListAsync();

        var monthly = BuildMonthlyCounts(byMonth.Select(x => (x.Year, x.Month, x.Count)));

        return new { TotalUsers = totalUsers, VerifiedUsers = verifiedUsers, BusinessUsers = businessUsers, RecentRegistrations = monthly };
    }

    private async Task<object> GetDiscussionStatsAsync()
    {
        var total = await context.Posts.CountAsync();
        var publicCount = await context.Posts.CountAsync(d => d.IsPublic);
        var featured = await context.Posts.CountAsync(d => d.IsFeatured);

        var popular = await context.Posts
            .OrderByDescending(d => d.PostViews).Take(5)
            .Select(d => new { d.Title, d.PostViews })
            .ToListAsync();

        var sixMonthsAgo = DateTime.UtcNow.AddMonths(-6);
        var byMonth = await context.Posts
            .Where(d => d.CreatedDate >= sixMonthsAgo)
            .GroupBy(d => new { d.CreatedDate.Year, d.CreatedDate.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count() })
            .OrderBy(x => x.Year).ThenBy(x => x.Month)
            .ToListAsync();

        return new
        {
            TotalDiscussions = total,
            PublicDiscussions = publicCount,
            FeaturedDiscussions = featured,
            PopularDiscussions = popular.ToDictionary(d => d.Title, d => d.PostViews),
            MonthlyDiscussions = BuildMonthlyCounts(byMonth.Select(x => (x.Year, x.Month, x.Count)))
        };
    }

    private async Task<object> GetCommentStatsAsync()
    {
        var total = await context.PostComments.CountAsync();

        var mostCommented = await context.Posts
            .Include(p => p.Comments).OrderByDescending(p => p.Comments.Count).Take(5)
            .Select(p => new { p.Title, CommentCount = p.Comments.Count })
            .ToListAsync();

        var sixMonthsAgo = DateTime.UtcNow.AddMonths(-6);
        var byMonth = await context.PostComments
            .Where(c => c.CreatedDate >= sixMonthsAgo)
            .GroupBy(c => new { c.CreatedDate.Year, c.CreatedDate.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count() })
            .OrderBy(x => x.Year).ThenBy(x => x.Month)
            .ToListAsync();

        return new
        {
            TotalComments = total,
            MostCommentedDiscussions = mostCommented.ToDictionary(p => p.Title, p => p.CommentCount),
            MonthlyComments = BuildMonthlyCounts(byMonth.Select(x => (x.Year, x.Month, x.Count)))
        };
    }

    private async Task<List<UserModel>> GetRecentAsync<T>(int count, string type)
    {
        var users = await userManager.Users
            .OrderByDescending(u => u.LastModified).Take(count).ToListAsync();
        return users.Select(u => new UserModel
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

    private async Task<List<PostCommentModel>> GetRecentCommentsAsync(int count)
    {
        var comments = await context.PostComments
            .Include(c => c.Author).Include(c => c.Post)
            .OrderByDescending(c => c.CreatedDate).Take(count)
            .ToListAsync();
        return comments.Select(c => new PostCommentModel(c)).ToList();
    }

    private static Dictionary<string, int> BuildMonthlyCounts(IEnumerable<(int Year, int Month, int Count)> data)
    {
        var dict = new Dictionary<string, int>();
        for (int i = 0; i < 6; i++)
        {
            var date = DateTime.UtcNow.AddMonths(-5 + i);
            var key = date.ToString("MMM yyyy");
            dict[key] = data.FirstOrDefault(x => x.Year == date.Year && x.Month == date.Month).Count;
        }
        return dict;
    }
}
