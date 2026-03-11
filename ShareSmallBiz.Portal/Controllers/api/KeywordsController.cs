using ShareSmallBiz.Portal.Infrastructure.Services;

namespace ShareSmallBiz.Portal.Controllers.api;

[Route("api/keywords")]
public class KeywordsController(
    KeywordProvider keywordProvider,
    ILogger<KeywordsController> logger) : ApiControllerBase
{
    /// <summary>GET /api/keywords — list all keywords (public)</summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll()
    {
        var keywords = await keywordProvider.GetAllKeywordsAsync();
        return Ok(keywords);
    }

    /// <summary>GET /api/keywords/{id} — get single keyword (public)</summary>
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(int id)
    {
        var keyword = await keywordProvider.GetKeywordByIdAsync(id);
        return keyword is null ? NotFound() : Ok(keyword);
    }

    /// <summary>POST /api/keywords — create keyword (admin only)</summary>
    [HttpPost]
    [Authorize(Roles = "Admin", AuthenticationSchemes = "Bearer")]
    public async Task<IActionResult> Create([FromBody] KeywordModel model)
    {
        if (string.IsNullOrWhiteSpace(model.Name))
            return BadRequest(new { Message = "Name is required." });

        var created = await keywordProvider.CreateKeywordAsync(model);
        logger.LogInformation("Keyword created: {Name}", created.Name);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>PUT /api/keywords/{id} — update keyword (admin only)</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin", AuthenticationSchemes = "Bearer")]
    public async Task<IActionResult> Update(int id, [FromBody] KeywordModel model)
    {
        if (string.IsNullOrWhiteSpace(model.Name))
            return BadRequest(new { Message = "Name is required." });

        var updated = await keywordProvider.UpdateKeywordAsync(id, model);
        return updated is null ? NotFound() : Ok(updated);
    }

    /// <summary>DELETE /api/keywords/{id} — delete keyword (admin only)</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin", AuthenticationSchemes = "Bearer")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await keywordProvider.DeleteKeywordAsync(id);
        return result ? NoContent() : NotFound();
    }
}
