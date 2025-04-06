namespace ShareSmallBiz.Portal.Data.Entities;

public class PostLike : BaseEntity
{
    public string UserId { get; set; }
    public ShareSmallBizUser User { get; set; }
    public int PostId { get; set; }
    public Post Post { get; set; }
}

public class PostCommentLike : BaseEntity
{
    public ShareSmallBizUser User { get; set; }
    public int PostCommentId { get; set; }
    public PostComment PostComment { get; set; }
}
