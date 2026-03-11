namespace ShareSmallBiz.Portal.Data.Entities;

public class PostSave : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public virtual ShareSmallBizUser User { get; set; } = null!;

    public int PostId { get; set; }
    public virtual Post Post { get; set; } = null!;
}
