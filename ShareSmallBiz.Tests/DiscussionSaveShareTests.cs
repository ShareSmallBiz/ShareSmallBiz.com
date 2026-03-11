using ShareSmallBiz.Portal.Infrastructure.Services;

namespace ShareSmallBiz.Tests;

[TestClass]
public class DiscussionSaveShareTests
{
    private static DiscussionProvider CreateProvider(ShareSmallBiz.Portal.Data.ShareSmallBizUserContext ctx)
        => new(ctx, TestHelper.CreateLogger<DiscussionProvider>(), null!, null!);

    [TestMethod]
    public async Task SaveDiscussionAsync_SavesPost_ReturnsTrue()
    {
        using var context = TestHelper.CreateContext();
        var user = TestHelper.CreateUser();
        context.Users.Add(user);
        var post = TestHelper.CreatePost(user);
        context.Posts.Add(post);
        await context.SaveChangesAsync();

        var provider = CreateProvider(context);
        var saved = await provider.SaveDiscussionAsync(post.Id, user.Id);

        Assert.IsTrue(saved);
        Assert.AreEqual(1, context.PostSaves.Count());
    }

    [TestMethod]
    public async Task SaveDiscussionAsync_SecondCall_TogglesOff_ReturnsFalse()
    {
        using var context = TestHelper.CreateContext();
        var user = TestHelper.CreateUser();
        context.Users.Add(user);
        var post = TestHelper.CreatePost(user);
        context.Posts.Add(post);
        await context.SaveChangesAsync();

        var provider = CreateProvider(context);
        await provider.SaveDiscussionAsync(post.Id, user.Id);    // save
        var unsaved = await provider.SaveDiscussionAsync(post.Id, user.Id);  // toggle off

        Assert.IsFalse(unsaved);
        Assert.AreEqual(0, context.PostSaves.Count());
    }

    [TestMethod]
    public async Task SaveDiscussionAsync_NonExistentPost_ReturnsFalse()
    {
        using var context = TestHelper.CreateContext();
        var user = TestHelper.CreateUser();
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var provider = CreateProvider(context);
        var result = await provider.SaveDiscussionAsync(9999, user.Id);

        Assert.IsFalse(result);
    }

    [TestMethod]
    public async Task IsDiscussionSavedAsync_ReturnsTrueWhenSaved()
    {
        using var context = TestHelper.CreateContext();
        var user = TestHelper.CreateUser();
        context.Users.Add(user);
        var post = TestHelper.CreatePost(user);
        context.Posts.Add(post);
        await context.SaveChangesAsync();

        var provider = CreateProvider(context);
        await provider.SaveDiscussionAsync(post.Id, user.Id);

        Assert.IsTrue(await provider.IsDiscussionSavedAsync(post.Id, user.Id));
    }

    [TestMethod]
    public async Task IsDiscussionSavedAsync_ReturnsFalseWhenNotSaved()
    {
        using var context = TestHelper.CreateContext();
        var user = TestHelper.CreateUser();
        context.Users.Add(user);
        var post = TestHelper.CreatePost(user);
        context.Posts.Add(post);
        await context.SaveChangesAsync();

        var provider = CreateProvider(context);

        Assert.IsFalse(await provider.IsDiscussionSavedAsync(post.Id, user.Id));
    }

    [TestMethod]
    public async Task GetSavedDiscussionsAsync_ReturnsOnlySavedPosts()
    {
        using var context = TestHelper.CreateContext();
        var user = TestHelper.CreateUser();
        context.Users.Add(user);
        var post1 = TestHelper.CreatePost(user, title: "Saved Post");
        var post2 = TestHelper.CreatePost(user, title: "Unsaved Post");
        context.Posts.AddRange(post1, post2);
        await context.SaveChangesAsync();

        var provider = CreateProvider(context);
        await provider.SaveDiscussionAsync(post1.Id, user.Id);

        var saved = await provider.GetSavedDiscussionsAsync(user.Id);

        Assert.AreEqual(1, saved.Count);
        Assert.AreEqual(post1.Id, saved[0].Id);
    }

    [TestMethod]
    public async Task GetSavedDiscussionsAsync_EmptyForNewUser()
    {
        using var context = TestHelper.CreateContext();
        var user = TestHelper.CreateUser();
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var provider = CreateProvider(context);
        var saved = await provider.GetSavedDiscussionsAsync(user.Id);

        Assert.AreEqual(0, saved.Count);
    }

    [TestMethod]
    public async Task ShareDiscussionAsync_IncrementsShareCount()
    {
        using var context = TestHelper.CreateContext();
        var user = TestHelper.CreateUser();
        context.Users.Add(user);
        var post = TestHelper.CreatePost(user);
        context.Posts.Add(post);
        await context.SaveChangesAsync();

        var provider = CreateProvider(context);
        var result = await provider.ShareDiscussionAsync(post.Id, user.Id);

        Assert.IsTrue(result);
        Assert.AreEqual(1, context.Posts.Find(post.Id)!.ShareCount);
    }

    [TestMethod]
    public async Task ShareDiscussionAsync_MultipleShares_AccumulatesCount()
    {
        using var context = TestHelper.CreateContext();
        var user = TestHelper.CreateUser();
        context.Users.Add(user);
        var post = TestHelper.CreatePost(user);
        context.Posts.Add(post);
        await context.SaveChangesAsync();

        var provider = CreateProvider(context);
        await provider.ShareDiscussionAsync(post.Id, user.Id);
        await provider.ShareDiscussionAsync(post.Id, user.Id);
        await provider.ShareDiscussionAsync(post.Id, user.Id);

        Assert.AreEqual(3, context.Posts.Find(post.Id)!.ShareCount);
    }

    [TestMethod]
    public async Task ShareDiscussionAsync_NonExistentPost_ReturnsFalse()
    {
        using var context = TestHelper.CreateContext();
        var user = TestHelper.CreateUser();
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var provider = CreateProvider(context);
        var result = await provider.ShareDiscussionAsync(9999, user.Id);

        Assert.IsFalse(result);
    }
}
