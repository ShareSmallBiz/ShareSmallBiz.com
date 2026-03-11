using ShareSmallBiz.Portal.Infrastructure.Services;

namespace ShareSmallBiz.Portal.Controllers.api;

[Route("api/stats")]
public class StatsController(
    StatsService statsService,
    ILogger<StatsController> logger) : ApiControllerBase
{
    /// <summary>GET /api/stats — Get platform-wide community statistics (public, cached)</summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetStats()
    {
        try
        {
            var stats = await statsService.GetStatsAsync();
            return Ok(stats);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving stats");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "Error retrieving statistics" });
        }
    }
}
