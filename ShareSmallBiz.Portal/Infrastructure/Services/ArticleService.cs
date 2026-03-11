using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using ShareSmallBiz.Portal.Data;
using ShareSmallBiz.Portal.Data.Entities;

namespace ShareSmallBiz.Portal.Infrastructure.Services;

/// <summary>
/// Articles are public posts (IsPublic=true) — no separate entity needed.
/// </summary>
public class ArticleService(
    ShareSmallBizUserContext context,
    ILogger<ArticleService> logger,
    IMemoryCache memoryCache)
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(15);
    private const string CATEGORIES_CACHE_KEY = "article_categories";

    /// <summary>
    /// Get paginated articles with optional filters for category, tag, and featured status.
    /// </summary>
    public async Task<ArticleListResult> GetArticlesAsync(
        int pageNumber = 1,
        int pageSize = 10,
        string? category = null,
        string? tag = null,
        bool? featured = null)
    {
        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Clamp(pageSize, 1, 50);

        try
        {
            var query = context.Posts
                .AsNoTracking()
                .Include(p => p.Author)
                .Include(p => p.PostCategories)
                .Where(p => p.IsPublic);

            if (!string.IsNullOrWhiteSpace(category))
                query = query.Where(p => p.Category != null && p.Category.ToLower() == category.ToLower());

            if (!string.IsNullOrWhiteSpace(tag))
                query = query.Where(p => p.PostCategories.Any(k => k.Name.ToLower() == tag.ToLower()));

            if (featured.HasValue)
                query = query.Where(p => p.IsFeatured == featured.Value);

            var totalCount = await query.CountAsync();

            var posts = await query
                .OrderByDescending(p => p.Published)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new ArticleListResult
            {
                Articles = [.. posts.Select(p => MapToArticleModel(p, includeContent: false))],
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving articles");
            throw;
        }
    }

    /// <summary>
    /// Get a single article by slug, with full content.
    /// </summary>
    public async Task<ArticleModel?> GetBySlugAsync(string slug)
    {
        try
        {
            var post = await context.Posts
                .AsNoTracking()
                .Include(p => p.Author)
                .Include(p => p.PostCategories)
                .FirstOrDefaultAsync(p => p.IsPublic && p.Slug == slug);

            if (post is null)
                return null;

            // Increment view count
            var tracked = await context.Posts.FindAsync(post.Id);
            if (tracked is not null)
            {
                tracked.PostViews++;
                await context.SaveChangesAsync();
            }

            return MapToArticleModel(post, includeContent: true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving article by slug {Slug}", slug);
            throw;
        }
    }

    /// <summary>
    /// Get featured articles.
    /// </summary>
    public async Task<List<ArticleModel>> GetFeaturedAsync(int count = 3)
    {
        count = Math.Clamp(count, 1, 20);
        try
        {
            var posts = await context.Posts
                .AsNoTracking()
                .Include(p => p.Author)
                .Include(p => p.PostCategories)
                .Where(p => p.IsPublic && p.IsFeatured)
                .OrderByDescending(p => p.Published)
                .Take(count)
                .ToListAsync();

            return [.. posts.Select(p => MapToArticleModel(p, includeContent: false))];
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving featured articles");
            throw;
        }
    }

    /// <summary>
    /// Get distinct categories with article counts (cached).
    /// </summary>
    public async Task<List<ArticleCategoryModel>> GetCategoriesAsync()
    {
        if (memoryCache.TryGetValue(CATEGORIES_CACHE_KEY, out List<ArticleCategoryModel>? cached) && cached != null)
            return cached;

        try
        {
            var categories = await context.Posts
                .AsNoTracking()
                .Where(p => p.IsPublic && p.Category != null)
                .GroupBy(p => p.Category!)
                .Select(g => new ArticleCategoryModel
                {
                    Name = g.Key,
                    ArticleCount = g.Count()
                })
                .OrderBy(c => c.Name)
                .ToListAsync();

            memoryCache.Set(CATEGORIES_CACHE_KEY, categories, CacheDuration);
            return categories;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving article categories");
            throw;
        }
    }

    /// <summary>
    /// Get related articles based on same category or shared keywords.
    /// </summary>
    public async Task<List<ArticleModel>> GetRelatedAsync(string slug, int count = 4)
    {
        count = Math.Clamp(count, 1, 20);
        try
        {
            var source = await context.Posts
                .AsNoTracking()
                .Include(p => p.PostCategories)
                .FirstOrDefaultAsync(p => p.IsPublic && p.Slug == slug);

            if (source is null)
                return [];

            var sourceKeywordIds = source.PostCategories.Select(k => k.Id).ToList();

            var related = await context.Posts
                .AsNoTracking()
                .Include(p => p.Author)
                .Include(p => p.PostCategories)
                .Where(p => p.IsPublic
                    && p.Id != source.Id
                    && (p.Category == source.Category
                        || p.PostCategories.Any(k => sourceKeywordIds.Contains(k.Id))))
                .OrderByDescending(p => p.Published)
                .Take(count)
                .ToListAsync();

            return [.. related.Select(p => MapToArticleModel(p, includeContent: false))];
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving related articles for slug {Slug}", slug);
            throw;
        }
    }

    public void InvalidateCache()
    {
        memoryCache.Remove(CATEGORIES_CACHE_KEY);
    }

    private static ArticleModel MapToArticleModel(Post post, bool includeContent)
    {
        // Reading time: ~200 words per minute
        var wordCount = (post.Content ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        var readingTimeMinutes = Math.Max(1, wordCount / 200);

        return new ArticleModel
        {
            Id = post.Id,
            Title = post.Title,
            Slug = post.Slug,
            Description = post.Description,
            Content = includeContent ? post.Content : null,
            CoverImageUrl = post.Cover,
            Category = post.Category,
            Tags = post.PostCategories?.Select(k => k.Name).ToList() ?? [],
            AuthorId = post.AuthorId,
            AuthorDisplayName = post.Author?.DisplayName ?? string.Empty,
            AuthorProfilePictureUrl = post.Author?.ProfilePictureUrl,
            PublishedDate = post.Published,
            ModifiedDate = post.ModifiedDate,
            ReadingTimeMinutes = readingTimeMinutes,
            ViewCount = post.PostViews,
            IsFeatured = post.IsFeatured,
            ShareCount = post.ShareCount
        };
    }
}

public class ArticleListResult
{
    public List<ArticleModel> Articles { get; set; } = [];
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}

public class ArticleModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Content { get; set; }
    public string? CoverImageUrl { get; set; }
    public string? Category { get; set; }
    public List<string> Tags { get; set; } = [];
    public string AuthorId { get; set; } = string.Empty;
    public string AuthorDisplayName { get; set; } = string.Empty;
    public string? AuthorProfilePictureUrl { get; set; }
    public DateTime PublishedDate { get; set; }
    public DateTime ModifiedDate { get; set; }
    public int ReadingTimeMinutes { get; set; }
    public int ViewCount { get; set; }
    public bool IsFeatured { get; set; }
    public int ShareCount { get; set; }
}

public class ArticleCategoryModel
{
    public string Name { get; set; } = string.Empty;
    public int ArticleCount { get; set; }
}
