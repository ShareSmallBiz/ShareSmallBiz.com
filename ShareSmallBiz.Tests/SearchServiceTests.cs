using ShareSmallBiz.Portal.Data.Entities;
using ShareSmallBiz.Portal.Data.Enums;
using ShareSmallBiz.Portal.Infrastructure.Services;

namespace ShareSmallBiz.Tests;

[TestClass]
public class SearchServiceTests
{
    private static async Task<ShareSmallBiz.Portal.Data.ShareSmallBizUserContext> SeedSearchDataAsync()
    {
        var context = TestHelper.CreateContext();

        var user = TestHelper.CreateUser(displayName: "Jane Smith", visibility: ProfileVisibility.Public);
        context.Users.Add(user);

        context.Posts.Add(TestHelper.CreatePost(user, title: "LinkedIn Marketing Tips", isPublic: true));
        context.Posts.Add(TestHelper.CreatePost(user, title: "Private Finance Strategy", isPublic: false));
        context.Posts.Add(TestHelper.CreatePost(user, title: "Social Media for Retail", isPublic: true));

        context.Keywords.Add(new Keyword { Name = "Marketing", Description = "Marketing topics" });
        context.Keywords.Add(new Keyword { Name = "Finance", Description = "Finance topics" });

        await context.SaveChangesAsync();
        return context;
    }

    [TestMethod]
    public async Task SearchAsync_FindsDiscussionByTitleMatch()
    {
        using var context = await SeedSearchDataAsync();
        var service = new SearchService(context, TestHelper.CreateLogger<SearchService>());

        var result = await service.SearchAsync("linkedin");

        Assert.AreEqual(1, result.Discussions.Count);
        Assert.IsTrue(result.Discussions[0].Title.Contains("LinkedIn", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public async Task SearchAsync_ExcludesPrivatePosts()
    {
        using var context = await SeedSearchDataAsync();
        var service = new SearchService(context, TestHelper.CreateLogger<SearchService>());

        var result = await service.SearchAsync("finance");

        // "Finance" keyword exists but post "Private Finance Strategy" is not public
        Assert.AreEqual(0, result.Discussions.Count);
        Assert.AreEqual(1, result.Keywords.Count);
    }

    [TestMethod]
    public async Task SearchAsync_FindsUserProfileByDisplayName()
    {
        using var context = await SeedSearchDataAsync();
        var service = new SearchService(context, TestHelper.CreateLogger<SearchService>());

        var result = await service.SearchAsync("jane");

        Assert.AreEqual(1, result.Profiles.Count);
        Assert.AreEqual("Jane Smith", result.Profiles[0].DisplayName);
    }

    [TestMethod]
    public async Task SearchAsync_FindsKeywordByName()
    {
        using var context = await SeedSearchDataAsync();
        var service = new SearchService(context, TestHelper.CreateLogger<SearchService>());

        var result = await service.SearchAsync("market");

        Assert.IsTrue(result.Keywords.Count >= 1);
        Assert.IsTrue(result.Keywords.Any(k => k.Name.Contains("Market", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public async Task SearchAsync_TypeFilter_OnlyReturnsDiscussions()
    {
        using var context = await SeedSearchDataAsync();
        var service = new SearchService(context, TestHelper.CreateLogger<SearchService>());

        var result = await service.SearchAsync("marketing", type: "discussions");

        Assert.IsTrue(result.Discussions.Count >= 1);
        Assert.AreEqual(0, result.Profiles.Count);
        Assert.AreEqual(0, result.Keywords.Count);
    }

    [TestMethod]
    public async Task SearchAsync_TypeFilter_OnlyReturnsKeywords()
    {
        using var context = await SeedSearchDataAsync();
        var service = new SearchService(context, TestHelper.CreateLogger<SearchService>());

        var result = await service.SearchAsync("finance", type: "keywords");

        Assert.AreEqual(0, result.Discussions.Count);
        Assert.AreEqual(0, result.Profiles.Count);
        Assert.AreEqual(1, result.Keywords.Count);
    }

    [TestMethod]
    public async Task SearchAsync_CaseInsensitive_MatchesUpperCase()
    {
        using var context = await SeedSearchDataAsync();
        var service = new SearchService(context, TestHelper.CreateLogger<SearchService>());

        var result = await service.SearchAsync("LINKEDIN");

        Assert.AreEqual(1, result.Discussions.Count);
    }

    [TestMethod]
    public async Task SearchAsync_NoMatches_ReturnsEmptyCollections()
    {
        using var context = await SeedSearchDataAsync();
        var service = new SearchService(context, TestHelper.CreateLogger<SearchService>());

        var result = await service.SearchAsync("xyznonexistentquery");

        Assert.AreEqual(0, result.Discussions.Count);
        Assert.AreEqual(0, result.Profiles.Count);
        Assert.AreEqual(0, result.Keywords.Count);
    }

    [TestMethod]
    public async Task SearchAsync_PageSize_LimitsResults()
    {
        using var context = TestHelper.CreateContext();
        var user = TestHelper.CreateUser();
        context.Users.Add(user);
        for (var i = 0; i < 10; i++)
            context.Posts.Add(TestHelper.CreatePost(user, title: $"matching post {i}", isPublic: true));
        await context.SaveChangesAsync();

        var service = new SearchService(context, TestHelper.CreateLogger<SearchService>());

        var result = await service.SearchAsync("matching post", pageSize: 3);

        Assert.IsTrue(result.Discussions.Count <= 3);
    }
}
