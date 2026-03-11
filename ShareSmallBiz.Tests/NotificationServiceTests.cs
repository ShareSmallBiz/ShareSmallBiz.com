using ShareSmallBiz.Portal.Infrastructure.Services;

namespace ShareSmallBiz.Tests;

[TestClass]
public class NotificationServiceTests
{
    private static NotificationService CreateService(ShareSmallBiz.Portal.Data.ShareSmallBizUserContext ctx)
        => new(ctx, TestHelper.CreateLogger<NotificationService>());

    [TestMethod]
    public async Task CreateAsync_AddsNotificationToDatabase()
    {
        using var context = TestHelper.CreateContext();
        var user = TestHelper.CreateUser();
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var service = CreateService(context);
        await service.CreateAsync(user.Id, "follow", "Someone followed you.");

        Assert.AreEqual(1, context.Notifications.Count());
        Assert.AreEqual("follow", context.Notifications.First().Type);
        Assert.IsFalse(context.Notifications.First().IsRead);
    }

    [TestMethod]
    public async Task CreateAsync_SelfNotification_IsSkipped()
    {
        using var context = TestHelper.CreateContext();
        var user = TestHelper.CreateUser();
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var service = CreateService(context);
        // actorId == recipientId — should be a no-op
        await service.CreateAsync(user.Id, "like", "You liked your own post.", actorId: user.Id);

        Assert.AreEqual(0, context.Notifications.Count());
    }

    [TestMethod]
    public async Task GetForUserAsync_ReturnsOnlyRecipientNotifications()
    {
        using var context = TestHelper.CreateContext();
        var userA = TestHelper.CreateUser();
        var userB = TestHelper.CreateUser();
        context.Users.AddRange(userA, userB);
        await context.SaveChangesAsync();

        var service = CreateService(context);
        await service.CreateAsync(userA.Id, "comment", "New comment on your post.");
        await service.CreateAsync(userA.Id, "like",    "Someone liked your post.");
        await service.CreateAsync(userB.Id, "follow",  "New follower.");

        var forA = await service.GetForUserAsync(userA.Id);
        var forB = await service.GetForUserAsync(userB.Id);

        Assert.AreEqual(2, forA.Count);
        Assert.AreEqual(1, forB.Count);
    }

    [TestMethod]
    public async Task GetForUserAsync_UnreadOnly_FiltersReadNotifications()
    {
        using var context = TestHelper.CreateContext();
        var user = TestHelper.CreateUser();
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var service = CreateService(context);
        await service.CreateAsync(user.Id, "comment", "Comment 1.");
        await service.CreateAsync(user.Id, "like",    "Like 1.");

        // Mark first one read
        var notifications = await service.GetForUserAsync(user.Id);
        await service.MarkAsReadAsync(notifications[0].Id, user.Id);

        var unread = await service.GetForUserAsync(user.Id, unreadOnly: true);

        Assert.AreEqual(1, unread.Count);
        Assert.IsFalse(unread[0].IsRead);
    }

    [TestMethod]
    public async Task MarkAsReadAsync_SetsIsReadTrue()
    {
        using var context = TestHelper.CreateContext();
        var user = TestHelper.CreateUser();
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var service = CreateService(context);
        await service.CreateAsync(user.Id, "follow", "New follower.");

        var notifications = await service.GetForUserAsync(user.Id);
        var id = notifications[0].Id;

        var result = await service.MarkAsReadAsync(id, user.Id);

        Assert.IsTrue(result);
        Assert.IsTrue(context.Notifications.First(n => n.Id == id).IsRead);
    }

    [TestMethod]
    public async Task MarkAsReadAsync_WrongUser_ReturnsFalse()
    {
        using var context = TestHelper.CreateContext();
        var userA = TestHelper.CreateUser();
        var userB = TestHelper.CreateUser();
        context.Users.AddRange(userA, userB);
        await context.SaveChangesAsync();

        var service = CreateService(context);
        await service.CreateAsync(userA.Id, "like", "Liked.");

        var notifications = await service.GetForUserAsync(userA.Id);

        // userB tries to mark userA's notification — should fail
        var result = await service.MarkAsReadAsync(notifications[0].Id, userB.Id);

        Assert.IsFalse(result);
    }

    [TestMethod]
    public async Task MarkAllAsReadAsync_MarksOnlyTargetUserNotifications()
    {
        using var context = TestHelper.CreateContext();
        var userA = TestHelper.CreateUser();
        var userB = TestHelper.CreateUser();
        context.Users.AddRange(userA, userB);
        await context.SaveChangesAsync();

        var service = CreateService(context);
        await service.CreateAsync(userA.Id, "comment", "Comment.");
        await service.CreateAsync(userA.Id, "like",    "Like.");
        await service.CreateAsync(userB.Id, "follow",  "Follow.");

        var count = await service.MarkAllAsReadAsync(userA.Id);

        Assert.AreEqual(2, count);
        Assert.IsTrue(context.Notifications.Where(n => n.UserId == userA.Id).All(n => n.IsRead));
        Assert.IsFalse(context.Notifications.First(n => n.UserId == userB.Id).IsRead);
    }

    [TestMethod]
    public async Task GetUnreadCountAsync_ReturnsCorrectCount()
    {
        using var context = TestHelper.CreateContext();
        var user = TestHelper.CreateUser();
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var service = CreateService(context);
        await service.CreateAsync(user.Id, "a", "First.");
        await service.CreateAsync(user.Id, "b", "Second.");
        await service.CreateAsync(user.Id, "c", "Third.");

        Assert.AreEqual(3, await service.GetUnreadCountAsync(user.Id));

        await service.MarkAllAsReadAsync(user.Id);

        Assert.AreEqual(0, await service.GetUnreadCountAsync(user.Id));
    }

    [TestMethod]
    public async Task GetForUserAsync_Pagination_LimitsResults()
    {
        using var context = TestHelper.CreateContext();
        var user = TestHelper.CreateUser();
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var service = CreateService(context);
        for (var i = 0; i < 10; i++)
            await service.CreateAsync(user.Id, "like", $"Like {i}.");

        var page1 = await service.GetForUserAsync(user.Id, pageSize: 4, pageNumber: 1);
        var page2 = await service.GetForUserAsync(user.Id, pageSize: 4, pageNumber: 2);

        Assert.AreEqual(4, page1.Count);
        Assert.AreEqual(4, page2.Count);
        CollectionAssert.AreNotEquivalent(
            page1.Select(n => n.Id).ToArray(),
            page2.Select(n => n.Id).ToArray());
    }
}
