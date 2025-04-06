namespace ShareSmallBiz.Portal.Data.Entities;

public class UserFollow : BaseEntity
{
    public ShareSmallBizUser Follower { get; set; }
    public string FollowingId { get; set; }
    public ShareSmallBizUser Following { get; set; }
}

