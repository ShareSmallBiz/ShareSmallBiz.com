using ShareSmallBiz.Portal.Data.Entities;
using ShareSmallBiz.Portal.Infrastructure.Services;

namespace ShareSmallBiz.Tests;

[TestClass]
public class StatsServiceTests
{
    [TestMethod]
    public async Task GetStatsAsync_EmptyDatabase_ReturnsZeroCounts()
    {
        using var context = TestHelper.CreateContext();
        var service = new StatsService(context, TestHelper.CreateLogger<StatsService>(), TestHelper.CreateCache(), null!);

        var stats = await service.GetStatsAsync();

        Assert.AreEqual(0, stats.TotalMembers);
        Assert.AreEqual(0, stats.TotalDiscussions);
        Assert.AreEqual(0, stats.TotalArticles);
        Assert.AreEqual(0, stats.TotalKeywords);
    }

    [TestMethod]
    public async Task GetStatsAsync_CountsAllPostsForDiscussions()
    {
        using var context = TestHelper.CreateContext();
        var user = TestHelper.CreateUser();
        context.Users.Add(user);
        context.Posts.Add(TestHelper.CreatePost(user, isPublic: true));
        context.Posts.Add(TestHelper.CreatePost(user, isPublic: false));
        await context.SaveChangesAsync();

        var service = new StatsService(context, TestHelper.CreateLogger<StatsService>(), TestHelper.CreateCache(), null!);
        var stats = await service.GetStatsAsync();

        Assert.AreEqual(1, stats.TotalMembers);
        Assert.AreEqual(2, stats.TotalDiscussions);  // all posts
        Assert.AreEqual(1, stats.TotalArticles);     // only public posts
    }

    [TestMethod]
    public async Task GetStatsAsync_CountsKeywords()
    {
        using var context = TestHelper.CreateContext();
        context.Keywords.AddRange(
            new Keyword { Name = "Marketing", Description = "Marketing topics" },
            new Keyword { Name = "Finance", Description = "Finance topics" });
        await context.SaveChangesAsync();

        var service = new StatsService(context, TestHelper.CreateLogger<StatsService>(), TestHelper.CreateCache(), null!);
        var stats = await service.GetStatsAsync();

        Assert.AreEqual(2, stats.TotalKeywords);
    }

    [TestMethod]
    public async Task GetStatsAsync_CachesResult_SubsequentCallIgnoresNewData()
    {
        using var context = TestHelper.CreateContext();
        var cache = TestHelper.CreateCache();
        var service = new StatsService(context, TestHelper.CreateLogger<StatsService>(), cache, null!);

        // First call — DB is empty
        var first = await service.GetStatsAsync();
        Assert.AreEqual(0, first.TotalMembers);

        // Add a user AFTER the first call
        context.Users.Add(TestHelper.CreateUser());
        await context.SaveChangesAsync();

        // Second call should return cached result (still 0)
        var second = await service.GetStatsAsync();
        Assert.AreEqual(0, second.TotalMembers);
    }

    [TestMethod]
    public async Task ClearCache_AllowsFreshRead()
    {
        using var context = TestHelper.CreateContext();
        var cache = TestHelper.CreateCache();
        var service = new StatsService(context, TestHelper.CreateLogger<StatsService>(), cache, null!);

        var _ = await service.GetStatsAsync(); // prime cache (0 members)

        context.Users.Add(TestHelper.CreateUser());
        await context.SaveChangesAsync();

        service.ClearCache();

        var stats = await service.GetStatsAsync();
        Assert.AreEqual(1, stats.TotalMembers);
    }

    [TestMethod]
    public async Task GetStatsAsync_MultipleUsers_CorrectMemberCount()
    {
        using var context = TestHelper.CreateContext();
        for (var i = 0; i < 5; i++)
            context.Users.Add(TestHelper.CreateUser());
        await context.SaveChangesAsync();

        var service = new StatsService(context, TestHelper.CreateLogger<StatsService>(), TestHelper.CreateCache(), null!);
        var stats = await service.GetStatsAsync();

        Assert.AreEqual(5, stats.TotalMembers);
    }
}
