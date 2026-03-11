using ShareSmallBiz.Portal.Data.Entities;
using ShareSmallBiz.Portal.Infrastructure.Services;

namespace ShareSmallBiz.Tests;

[TestClass]
public class EventServiceTests
{
    private static EventService CreateService(ShareSmallBiz.Portal.Data.ShareSmallBizUserContext ctx)
        => new(ctx, TestHelper.CreateLogger<EventService>(), TestHelper.CreateCache());

    private static Event MakeEvent(string title, DateTime start, bool isOnline = false)
        => new()
        {
            Title = title,
            Description = $"Description for {title}",
            IsOnline = isOnline,
            StartDate = start,
            EndDate = start.AddHours(2),
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        };

    [TestMethod]
    public async Task GetUpcomingAsync_ReturnsEventsFromToday()
    {
        using var context = TestHelper.CreateContext();
        var today = DateTime.UtcNow.Date;
        context.Events.AddRange(
            MakeEvent("Past Event",   today.AddDays(-1)),
            MakeEvent("Today Event",  today),
            MakeEvent("Future Event", today.AddDays(5)));
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var events = await service.GetUpcomingAsync(from: today);

        Assert.AreEqual(2, events.Count);
        Assert.IsTrue(events.All(e => e.StartDate >= today));
    }

    [TestMethod]
    public async Task GetUpcomingAsync_OrderedByStartDate()
    {
        using var context = TestHelper.CreateContext();
        var today = DateTime.UtcNow.Date;
        context.Events.AddRange(
            MakeEvent("Later",  today.AddDays(10)),
            MakeEvent("Sooner", today.AddDays(1)),
            MakeEvent("Middle", today.AddDays(5)));
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var events = await service.GetUpcomingAsync(from: today);

        Assert.AreEqual("Sooner", events[0].Title);
        Assert.AreEqual("Middle", events[1].Title);
        Assert.AreEqual("Later",  events[2].Title);
    }

    [TestMethod]
    public async Task GetUpcomingAsync_RespectsCountLimit()
    {
        using var context = TestHelper.CreateContext();
        var today = DateTime.UtcNow.Date;
        for (var i = 0; i < 20; i++)
            context.Events.Add(MakeEvent($"Event {i}", today.AddDays(i)));
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var events = await service.GetUpcomingAsync(count: 5);

        Assert.AreEqual(5, events.Count);
    }

    [TestMethod]
    public async Task GetUpcomingAsync_DefaultFrom_IsToday()
    {
        using var context = TestHelper.CreateContext();
        var today = DateTime.UtcNow.Date;
        context.Events.AddRange(
            MakeEvent("Yesterday", today.AddDays(-1)),
            MakeEvent("Tomorrow",  today.AddDays(1)));
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var events = await service.GetUpcomingAsync(); // no 'from' → defaults to today

        Assert.AreEqual(1, events.Count);
        Assert.AreEqual("Tomorrow", events[0].Title);
    }

    [TestMethod]
    public async Task GetUpcomingAsync_EmptyDatabase_ReturnsEmptyList()
    {
        using var context = TestHelper.CreateContext();
        var service = CreateService(context);

        var events = await service.GetUpcomingAsync();

        Assert.AreEqual(0, events.Count);
    }

    [TestMethod]
    public async Task GetUpcomingAsync_CachesResults()
    {
        using var context = TestHelper.CreateContext();
        var today = DateTime.UtcNow.Date;
        context.Events.Add(MakeEvent("Cached Event", today.AddDays(1)));
        await context.SaveChangesAsync();

        var cache = TestHelper.CreateCache();
        var service = new EventService(context, TestHelper.CreateLogger<EventService>(), cache);

        var first = await service.GetUpcomingAsync(from: today, count: 10);

        // Add more data after caching
        context.Events.Add(MakeEvent("New Event", today.AddDays(2)));
        await context.SaveChangesAsync();

        var second = await service.GetUpcomingAsync(from: today, count: 10);

        Assert.AreEqual(first.Count, second.Count);
    }

    [TestMethod]
    public async Task GetByIdAsync_ReturnsCorrectEvent()
    {
        using var context = TestHelper.CreateContext();
        var ev = MakeEvent("My Event", DateTime.UtcNow.AddDays(3), isOnline: true);
        context.Events.Add(ev);
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var result = await service.GetByIdAsync(ev.Id);

        Assert.IsNotNull(result);
        Assert.AreEqual("My Event", result.Title);
        Assert.IsTrue(result.IsOnline);
    }

    [TestMethod]
    public async Task GetByIdAsync_NotFound_ReturnsNull()
    {
        using var context = TestHelper.CreateContext();
        var service = CreateService(context);

        var result = await service.GetByIdAsync(9999);

        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task GetByIdAsync_MapsAllFields()
    {
        using var context = TestHelper.CreateContext();
        var start = DateTime.UtcNow.AddDays(7).Date;
        var ev = new Event
        {
            Title = "Full Event",
            Description = "Full description",
            Location = "123 Main St",
            IsOnline = false,
            StartDate = start,
            EndDate = start.AddHours(3),
            RegistrationUrl = "https://example.com/register",
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        };
        context.Events.Add(ev);
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var result = await service.GetByIdAsync(ev.Id);

        Assert.AreEqual("Full Event",             result!.Title);
        Assert.AreEqual("123 Main St",            result.Location);
        Assert.AreEqual("https://example.com/register", result.RegistrationUrl);
        Assert.AreEqual(start,                    result.StartDate);
    }
}
