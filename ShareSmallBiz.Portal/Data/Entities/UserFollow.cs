using ShareSmallBiz.Portal.Data.Entities;

public class UserFollow : BaseEntity
{
    public string FollowingId { get; set; }
    public ShareSmallBizUser Following { get; set; }
}