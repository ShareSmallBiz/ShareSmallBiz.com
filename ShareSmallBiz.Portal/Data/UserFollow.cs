namespace ShareSmallBiz.Portal.Data
{
    public class UserFollow : BaseEntity
    {
        public string FollowerId { get; set; }
        public ShareSmallBizUser Follower { get; set; }
        public string FollowingId { get; set; }
        public ShareSmallBizUser Following { get; set; }
    }
}

