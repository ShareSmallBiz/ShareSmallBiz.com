namespace ShareSmallBiz.Portal.Data.Entities;

public class PostCommentLike : BaseEntity
{
    public ShareSmallBizUser User { get; set; }
    public int PostCommentId { get; set; }
    public PostComment PostComment { get; set; }
}
