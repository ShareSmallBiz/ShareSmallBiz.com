using Microsoft.Extensions.Caching.Memory;
using ShareSmallBiz.Portal.Controllers.api;
using ShareSmallBiz.Portal.Data;
using ShareSmallBiz.Portal.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace ShareSmallBiz.Portal.Infrastructure.Services;

public class StatsService(
    ShareSmallBizUserContext context,
    ILogger<StatsService> logger,
    IMemoryCache memoryCache,
    UserManager<ShareSmallBizUser> userManager)
{
    private const string STATS_CACHE_KEY = "platform_stats";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    public async Task<StatsModel> GetStatsAsync()
    {
        // Try to get from cache first
        if (memoryCache.TryGetValue(STATS_CACHE_KEY, out StatsModel? cachedStats) && cachedStats != null)
        {
            return cachedStats;
        }

        try
        {
            var stats = new StatsModel
            {
                TotalMembers = await context.Users.CountAsync(),
                TotalDiscussions = await context.Posts.CountAsync(),
                TotalArticles = await context.Posts.Where(p => p.IsPublic).CountAsync(),
                TotalKeywords = await context.Keywords.CountAsync(),
                MemberGrowthThisMonth = await GetMemberGrowthThisMonthAsync()
            };

            // Cache the results
            memoryCache.Set(STATS_CACHE_KEY, stats, CacheDuration);

            return stats;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving platform statistics");
            throw;
        }
    }

    private async Task<int> GetMemberGrowthThisMonthAsync()
    {
        // Since we don't have explicit registration date tracking, return an estimate
        // In a production system, track CreatedDate on ShareSmallBizUser
        return await context.Users.CountAsync() / 12;  // Simple estimate: total users / 12 months
    }

    /// <summary>
    /// Clear the stats cache (useful after significant operations)
    /// </summary>
    public void ClearCache()
    {
        memoryCache.Remove(STATS_CACHE_KEY);
        logger.LogInformation("Stats cache cleared");
    }
}
