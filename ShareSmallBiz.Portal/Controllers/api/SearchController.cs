using ShareSmallBiz.Portal.Infrastructure.Services;

namespace ShareSmallBiz.Portal.Controllers.api;

/// <summary>
/// Search controller providing unified search across discussions, profiles, and keywords.
/// </summary>
[Route("api/search")]
[ApiController]
public class SearchController(
    SearchService searchService,
    ILogger<SearchController> logger) : ControllerBase
{
    /// <summary>
    /// Searches across discussions, profiles, and keywords.
    /// </summary>
    /// <param name="q">Search query (minimum 2 characters required)</param>
    /// <param name="type">Optional filter: "discussions", "profiles", "keywords". If omitted, returns all types.</param>
    /// <param name="pageSize">Number of results per type (default: 5)</param>
    /// <returns>SearchResultModel with grouped results</returns>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Search(
        [FromQuery] string q,
        [FromQuery] string? type = null,
        [FromQuery] int pageSize = 5)
    {
        // Validate query
        if (string.IsNullOrWhiteSpace(q))
            return BadRequest(new { message = "Query parameter 'q' is required." });

        if (q.Length < 2)
            return BadRequest(new { message = "Query must be at least 2 characters long." });

        try
        {
            var results = await searchService.SearchAsync(q, type, pageSize);
            return Ok(results);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error performing search for query: {Query}", q);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "Error performing search." });
        }
    }
}

/// <summary>
/// Model representing grouped search results across discussions, profiles, and keywords.
/// </summary>
public class SearchResultModel
{
    /// <summary>
    /// Discussions matching the search query.
    /// </summary>
    public List<DiscussionModel> Discussions { get; set; } = [];

    /// <summary>
    /// User profiles matching the search query.
    /// </summary>
    public List<UserModel> Profiles { get; set; } = [];

    /// <summary>
    /// Keywords matching the search query.
    /// </summary>
    public List<KeywordModel> Keywords { get; set; } = [];
}
