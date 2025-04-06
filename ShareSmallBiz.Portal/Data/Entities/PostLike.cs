using ShareSmallBiz.Portal.Data.Entities;

public class PostLike : BaseEntity
{
    public int PostId { get; set; }
    public Post Post { get; set; }
}