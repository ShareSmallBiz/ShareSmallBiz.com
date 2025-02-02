namespace ShareSmallBiz.Portal.Data
{
    public class PostLike : BaseEntity
    {
        public string UserId { get; set; }
        public ShareSmallBizUser User { get; set; }
        public int PostId { get; set; }
        public Post Post { get; set; }
    }
}

