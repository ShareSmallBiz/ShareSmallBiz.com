using Microsoft.EntityFrameworkCore;
using ShareSmallBiz.Portal.Controllers.api;
using ShareSmallBiz.Portal.Data;
using ShareSmallBiz.Portal.Infrastructure.Models;
using System.Diagnostics.CodeAnalysis;

namespace ShareSmallBiz.Portal.Infrastructure.Services;

public class SearchService(
    ShareSmallBizUserContext context,
    ILogger<SearchService> logger)
{
    /// <summary>
    /// Searches across discussions, profiles, and keywords based on query and optional type filter.
    /// </summary>
    /// <param name="query">Search query (minimum 2 characters)</param>
    /// <param name="type">Optional filter: "discussions", "profiles", "keywords". If null, searches all types.</param>
    /// <param name="pageSize">Number of results per type</param>
    /// <returns>SearchResultModel with grouped results</returns>
    public async Task<SearchResultModel> SearchAsync(string query, string? type = null, int pageSize = 5)
    {
        var result = new SearchResultModel();

        try
        {
            // Normalize query for case-insensitive search
            var lowerQuery = query.ToLower();

            // Search discussions if type is null or matches
            if (string.IsNullOrEmpty(type) || type.Equals("discussions", StringComparison.OrdinalIgnoreCase))
            {
                result.Discussions = await SearchDiscussionsAsync(lowerQuery, pageSize);
            }

            // Search profiles if type is null or matches
            if (string.IsNullOrEmpty(type) || type.Equals("profiles", StringComparison.OrdinalIgnoreCase))
            {
                result.Profiles = await SearchProfilesAsync(lowerQuery, pageSize);
            }

            // Search keywords if type is null or matches
            if (string.IsNullOrEmpty(type) || type.Equals("keywords", StringComparison.OrdinalIgnoreCase))
            {
                result.Keywords = await SearchKeywordsAsync(lowerQuery, pageSize);
            }

            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error performing search for query: {Query}", query);
            throw;
        }
    }

    /// <summary>
    /// Searches for discussions/posts matching the query.
    /// </summary>
    private async Task<List<DiscussionModel>> SearchDiscussionsAsync(string lowerQuery, int pageSize)
    {
        var discussions = await context.Posts
            .Where(p => p.IsPublic &&
                   (p.Title.ToLower().Contains(lowerQuery) ||
                    p.Description.ToLower().Contains(lowerQuery) ||
                    p.Content.ToLower().Contains(lowerQuery)))
            .Include(p => p.Author)
            .Include(p => p.Comments)
            .Include(p => p.Likes)
            .Include(p => p.PostCategories)
            .Include(p => p.Media)
            .Take(pageSize)
            .AsNoTracking()
            .Select(p => new DiscussionModel(p))
            .ToListAsync();

        return discussions ?? [];
    }

    /// <summary>
    /// Searches for user profiles matching the query.
    /// </summary>
    private async Task<List<UserModel>> SearchProfilesAsync(string lowerQuery, int pageSize)
    {
        var profiles = await context.Users
            .Where(u => u.ProfileVisibility == Data.Enums.ProfileVisibility.Public &&
                   (u.DisplayName.ToLower().Contains(lowerQuery) ||
                    u.UserName!.ToLower().Contains(lowerQuery) ||
                    (u.Bio != null && u.Bio.ToLower().Contains(lowerQuery))))
            .Take(pageSize)
            .AsNoTracking()
            .Select(u => new UserModel(u))
            .ToListAsync();

        return profiles ?? [];
    }

    /// <summary>
    /// Searches for keywords matching the query.
    /// </summary>
    private async Task<List<KeywordModel>> SearchKeywordsAsync(string lowerQuery, int pageSize)
    {
        var keywords = await context.Keywords
            .Where(k => k.Name.ToLower().Contains(lowerQuery))
            .Include(k => k.Posts)
            .Take(pageSize)
            .AsNoTracking()
            .Select(k => new KeywordModel
            {
                Id = k.Id,
                Name = k.Name,
                Description = k.Description,
                PostCount = k.Posts.Count(p => p.IsPublic)
            })
            .ToListAsync();

        return keywords ?? [];
    }
}
