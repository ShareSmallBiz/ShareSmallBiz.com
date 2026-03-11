using Microsoft.AspNetCore.Authorization;
using ShareSmallBiz.Portal.Infrastructure.Services;

namespace ShareSmallBiz.Portal.Controllers.api;

[Route("api/events")]
public class EventsController(
    EventService eventService,
    ILogger<EventsController> logger) : ApiControllerBase
{
    /// <summary>GET /api/events?from={date}&count=10 — upcoming events (default today)</summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetUpcoming(
        [FromQuery] DateTime? from = null,
        [FromQuery] int count = 10)
    {
        try
        {
            var events = await eventService.GetUpcomingAsync(from, count);
            return Ok(events);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving events");
            return StatusCode(500, new { message = "Error retrieving events" });
        }
    }

    /// <summary>GET /api/events/{id} — get a single event</summary>
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(int id)
    {
        try
        {
            var ev = await eventService.GetByIdAsync(id);
            return ev is null ? NotFound(new { message = "Event not found." }) : Ok(ev);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving event {Id}", id);
            return StatusCode(500, new { message = "Error retrieving event" });
        }
    }
}
