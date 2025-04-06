namespace ShareSmallBiz.Portal.Data.Entities;

public class PostLike : BaseEntity
{
    public ShareSmallBizUser User { get; set; }
    public int PostId { get; set; }
    public Post Post { get; set; }
}
