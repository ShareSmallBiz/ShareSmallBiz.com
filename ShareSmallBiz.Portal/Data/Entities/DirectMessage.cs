namespace ShareSmallBiz.Portal.Data.Entities;

public class DirectMessage : BaseEntity
{
    public string SenderId { get; set; } = string.Empty;
    public virtual ShareSmallBizUser Sender { get; set; } = null!;

    public string RecipientId { get; set; } = string.Empty;
    public virtual ShareSmallBizUser Recipient { get; set; } = null!;

    public string Content { get; set; } = string.Empty;
    public bool IsRead { get; set; } = false;

    /// <summary>
    /// Deterministic conversation key: min(senderId, recipientId) + "_" + max(senderId, recipientId).
    /// Allows efficient bidirectional conversation lookup.
    /// </summary>
    public string ConversationId { get; set; } = string.Empty;

    public static string BuildConversationId(string userA, string userB)
        => string.Compare(userA, userB, StringComparison.Ordinal) < 0
            ? $"{userA}_{userB}"
            : $"{userB}_{userA}";
}
