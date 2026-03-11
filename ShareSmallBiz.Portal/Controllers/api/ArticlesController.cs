using Microsoft.AspNetCore.Authorization;
using ShareSmallBiz.Portal.Infrastructure.Services;

namespace ShareSmallBiz.Portal.Controllers.api;

[Route("api/articles")]
public class ArticlesController(
    ArticleService articleService,
    ILogger<ArticlesController> logger) : ApiControllerBase
{
    /// <summary>
    /// GET /api/articles?pageNumber=1&pageSize=10&category={name}&tag={name}&featured={bool}
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? category = null,
        [FromQuery] string? tag = null,
        [FromQuery] bool? featured = null)
    {
        try
        {
            var result = await articleService.GetArticlesAsync(pageNumber, pageSize, category, tag, featured);
            return Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving articles");
            return StatusCode(500, new { message = "Error retrieving articles" });
        }
    }

    /// <summary>GET /api/articles/featured?count=3</summary>
    [HttpGet("featured")]
    [AllowAnonymous]
    public async Task<IActionResult> GetFeatured([FromQuery] int count = 3)
    {
        try
        {
            var articles = await articleService.GetFeaturedAsync(count);
            return Ok(articles);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving featured articles");
            return StatusCode(500, new { message = "Error retrieving featured articles" });
        }
    }

    /// <summary>GET /api/articles/categories — distinct categories with article counts</summary>
    [HttpGet("categories")]
    [AllowAnonymous]
    public async Task<IActionResult> GetCategories()
    {
        try
        {
            var categories = await articleService.GetCategoriesAsync();
            return Ok(categories);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving article categories");
            return StatusCode(500, new { message = "Error retrieving categories" });
        }
    }

    /// <summary>GET /api/articles/related/{slug}?count=4 — articles in same category or with shared keywords</summary>
    [HttpGet("related/{slug}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetRelated(string slug, [FromQuery] int count = 4)
    {
        try
        {
            var articles = await articleService.GetRelatedAsync(slug, count);
            return Ok(articles);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving related articles for {Slug}", slug);
            return StatusCode(500, new { message = "Error retrieving related articles" });
        }
    }

    /// <summary>GET /api/articles/{slug} — get single article with full content</summary>
    [HttpGet("{slug}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetBySlug(string slug)
    {
        try
        {
            var article = await articleService.GetBySlugAsync(slug);
            return article is null ? NotFound(new { message = "Article not found." }) : Ok(article);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving article {Slug}", slug);
            return StatusCode(500, new { message = "Error retrieving article" });
        }
    }
}
