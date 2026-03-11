using ShareSmallBiz.Portal.Data.Entities;
using ShareSmallBiz.Portal.Infrastructure.Services;

namespace ShareSmallBiz.Tests;

[TestClass]
public class MessageServiceTests
{
    private static MessageService CreateService(ShareSmallBiz.Portal.Data.ShareSmallBizUserContext ctx)
    {
        var notificationService = new NotificationService(ctx, TestHelper.CreateLogger<NotificationService>());
        return new MessageService(ctx, TestHelper.CreateLogger<MessageService>(), notificationService);
    }

    [TestMethod]
    public async Task SendMessageAsync_CreatesMessageInDatabase()
    {
        using var context = TestHelper.CreateContext();
        var sender = TestHelper.CreateUser();
        var recipient = TestHelper.CreateUser();
        context.Users.AddRange(sender, recipient);
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var message = await service.SendMessageAsync(sender.Id, recipient.Id, "Hello there!");

        Assert.IsNotNull(message);
        Assert.AreEqual("Hello there!", message.Content);
        Assert.AreEqual(sender.Id, message.SenderId);
        Assert.IsFalse(message.IsRead);
        Assert.AreEqual(1, context.DirectMessages.Count());
    }

    [TestMethod]
    public async Task SendMessageAsync_UnknownRecipient_ReturnsNull()
    {
        using var context = TestHelper.CreateContext();
        var sender = TestHelper.CreateUser();
        context.Users.Add(sender);
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var result = await service.SendMessageAsync(sender.Id, "nonexistent-id", "Hello?");

        Assert.IsNull(result);
        Assert.AreEqual(0, context.DirectMessages.Count());
    }

    [TestMethod]
    public async Task SendMessageAsync_SetsCorrectConversationId()
    {
        using var context = TestHelper.CreateContext();
        var userA = TestHelper.CreateUser(id: "aaa");
        var userB = TestHelper.CreateUser(id: "bbb");
        context.Users.AddRange(userA, userB);
        await context.SaveChangesAsync();

        var service = CreateService(context);
        await service.SendMessageAsync(userA.Id, userB.Id, "Hi B!");
        await service.SendMessageAsync(userB.Id, userA.Id, "Hi A!");

        var messages = context.DirectMessages.ToList();
        // Both messages should share the same conversation ID
        Assert.AreEqual(messages[0].ConversationId, messages[1].ConversationId);
    }

    [TestMethod]
    public async Task SendMessageAsync_TrimsWhitespace()
    {
        using var context = TestHelper.CreateContext();
        var sender = TestHelper.CreateUser();
        var recipient = TestHelper.CreateUser();
        context.Users.AddRange(sender, recipient);
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var message = await service.SendMessageAsync(sender.Id, recipient.Id, "  Hello!  ");

        Assert.AreEqual("Hello!", message!.Content);
    }

    [TestMethod]
    public async Task SendMessageAsync_SendsNotificationToRecipient()
    {
        using var context = TestHelper.CreateContext();
        var sender = TestHelper.CreateUser();
        var recipient = TestHelper.CreateUser();
        context.Users.AddRange(sender, recipient);
        await context.SaveChangesAsync();

        var service = CreateService(context);
        await service.SendMessageAsync(sender.Id, recipient.Id, "You have mail!");

        // A notification should be created for the recipient
        Assert.AreEqual(1, context.Notifications.Count(n => n.UserId == recipient.Id));
        Assert.AreEqual("message", context.Notifications.First().Type);
    }

    [TestMethod]
    public async Task GetConversationsAsync_ReturnsConversationForParticipant()
    {
        using var context = TestHelper.CreateContext();
        var userA = TestHelper.CreateUser();
        var userB = TestHelper.CreateUser();
        context.Users.AddRange(userA, userB);
        await context.SaveChangesAsync();

        var service = CreateService(context);
        await service.SendMessageAsync(userA.Id, userB.Id, "First message");
        await service.SendMessageAsync(userA.Id, userB.Id, "Second message");

        var conversationsA = await service.GetConversationsAsync(userA.Id);
        var conversationsB = await service.GetConversationsAsync(userB.Id);

        Assert.AreEqual(1, conversationsA.Count);
        Assert.AreEqual(1, conversationsB.Count);
        Assert.AreEqual(conversationsA[0].ConversationId, conversationsB[0].ConversationId);
    }

    [TestMethod]
    public async Task GetConversationsAsync_ShowsLatestMessageAsPreview()
    {
        using var context = TestHelper.CreateContext();
        var userA = TestHelper.CreateUser();
        var userB = TestHelper.CreateUser();
        context.Users.AddRange(userA, userB);
        await context.SaveChangesAsync();

        var service = CreateService(context);
        await service.SendMessageAsync(userA.Id, userB.Id, "First");
        await service.SendMessageAsync(userA.Id, userB.Id, "Latest");

        var conversations = await service.GetConversationsAsync(userA.Id);

        Assert.AreEqual("Latest", conversations[0].LastMessage);
    }

    [TestMethod]
    public async Task GetConversationsAsync_UnreadCountIsCorrect()
    {
        using var context = TestHelper.CreateContext();
        var userA = TestHelper.CreateUser();
        var userB = TestHelper.CreateUser();
        context.Users.AddRange(userA, userB);
        await context.SaveChangesAsync();

        var service = CreateService(context);
        // B sends 3 messages to A
        await service.SendMessageAsync(userB.Id, userA.Id, "Msg 1");
        await service.SendMessageAsync(userB.Id, userA.Id, "Msg 2");
        await service.SendMessageAsync(userB.Id, userA.Id, "Msg 3");

        // A's inbox should show 3 unread
        var conversations = await service.GetConversationsAsync(userA.Id);

        Assert.AreEqual(3, conversations[0].UnreadCount);
    }

    [TestMethod]
    public async Task GetMessagesAsync_ReturnsMessagesForConversation()
    {
        using var context = TestHelper.CreateContext();
        var userA = TestHelper.CreateUser();
        var userB = TestHelper.CreateUser();
        context.Users.AddRange(userA, userB);
        await context.SaveChangesAsync();

        var service = CreateService(context);
        await service.SendMessageAsync(userA.Id, userB.Id, "Hello");
        await service.SendMessageAsync(userB.Id, userA.Id, "Hi back");

        var convId = DirectMessage.BuildConversationId(userA.Id, userB.Id);
        var messages = await service.GetMessagesAsync(convId, userA.Id);

        Assert.AreEqual(2, messages.Count);
    }

    [TestMethod]
    public async Task GetMessagesAsync_MarksIncomingMessagesAsRead()
    {
        using var context = TestHelper.CreateContext();
        var userA = TestHelper.CreateUser();
        var userB = TestHelper.CreateUser();
        context.Users.AddRange(userA, userB);
        await context.SaveChangesAsync();

        var service = CreateService(context);
        await service.SendMessageAsync(userB.Id, userA.Id, "Hello A!");

        // Before reading — 1 unread for A
        Assert.AreEqual(1, context.DirectMessages.Count(m => m.RecipientId == userA.Id && !m.IsRead));

        var convId = DirectMessage.BuildConversationId(userA.Id, userB.Id);
        await service.GetMessagesAsync(convId, userA.Id); // A reads the conversation

        // After reading — 0 unread
        Assert.AreEqual(0, context.DirectMessages.Count(m => m.RecipientId == userA.Id && !m.IsRead));
    }
}
