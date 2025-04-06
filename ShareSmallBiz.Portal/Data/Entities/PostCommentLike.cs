public class PostCommentLike : BaseEntity
{
    public int PostCommentId { get; set; }
    public PostComment PostComment { get; set; }
}