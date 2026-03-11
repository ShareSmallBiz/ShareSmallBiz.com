using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using ShareSmallBiz.Portal.Data;

namespace ShareSmallBiz.Portal.Infrastructure.Services;

public class EventService(
    ShareSmallBizUserContext context,
    ILogger<EventService> logger,
    IMemoryCache memoryCache)
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    private static string CacheKey(DateTime from, int count) => $"events_{from:yyyyMMdd}_{count}";

    /// <summary>
    /// Get upcoming events starting from a given date, ordered by StartDate.
    /// </summary>
    public async Task<List<EventModel>> GetUpcomingAsync(DateTime? from = null, int count = 10)
    {
        count = Math.Clamp(count, 1, 50);
        var fromDate = (from ?? DateTime.UtcNow).Date;
        var cacheKey = CacheKey(fromDate, count);

        if (memoryCache.TryGetValue(cacheKey, out List<EventModel>? cached) && cached != null)
            return cached;

        try
        {
            var events = await context.Events
                .AsNoTracking()
                .Where(e => e.StartDate >= fromDate)
                .OrderBy(e => e.StartDate)
                .Take(count)
                .Select(e => new EventModel
                {
                    Id = e.Id,
                    Title = e.Title,
                    Description = e.Description,
                    Location = e.Location,
                    IsOnline = e.IsOnline,
                    StartDate = e.StartDate,
                    EndDate = e.EndDate,
                    RegistrationUrl = e.RegistrationUrl,
                    CreatedDate = e.CreatedDate
                })
                .ToListAsync();

            memoryCache.Set(cacheKey, events, CacheDuration);
            return events;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving upcoming events from {From}", fromDate);
            throw;
        }
    }

    /// <summary>
    /// Get a single event by ID.
    /// </summary>
    public async Task<EventModel?> GetByIdAsync(int id)
    {
        try
        {
            var e = await context.Events
                .AsNoTracking()
                .FirstOrDefaultAsync(ev => ev.Id == id);

            if (e is null) return null;

            return new EventModel
            {
                Id = e.Id,
                Title = e.Title,
                Description = e.Description,
                Location = e.Location,
                IsOnline = e.IsOnline,
                StartDate = e.StartDate,
                EndDate = e.EndDate,
                RegistrationUrl = e.RegistrationUrl,
                CreatedDate = e.CreatedDate
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving event {Id}", id);
            throw;
        }
    }
}

public class EventModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Location { get; set; }
    public bool IsOnline { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? RegistrationUrl { get; set; }
    public DateTime CreatedDate { get; set; }
}
