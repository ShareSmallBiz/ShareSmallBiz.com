namespace ShareSmallBiz.Portal.Data.Entities;

public class Notification : BaseEntity
{
    /// <summary>The user receiving this notification</summary>
    public string UserId { get; set; } = string.Empty;
    public virtual ShareSmallBizUser User { get; set; } = null!;

    /// <summary>The user who triggered the notification (null for system notifications)</summary>
    public string? ActorId { get; set; }
    public virtual ShareSmallBizUser? Actor { get; set; }

    /// <summary>Type: comment, like, follow, mention</summary>
    public string Type { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; } = false;

    /// <summary>Optional post/comment ID related to this notification</summary>
    public int? TargetId { get; set; }

    /// <summary>Type of target entity: post, comment, user</summary>
    public string? TargetType { get; set; }
}
