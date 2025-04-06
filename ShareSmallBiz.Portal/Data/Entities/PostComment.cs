using ShareSmallBiz.Portal.Data.Entities;
using System.ComponentModel.DataAnnotations.Schema;

public class PostComment : BaseEntity
{
    // Remove: public ShareSmallBizUser Author { get; set; }
    [ForeignKey("Post")]
    public int PostId { get; set; }
    public virtual Post Post { get; set; } = null!;
    public string Content { get; set; } = string.Empty;
    // Remove redundant: public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    [ForeignKey("ParentPost")]
    public int? ParentPostId { get; set; }
    public Post? ParentPost { get; set; }
    public virtual ICollection<PostCommentLike> Likes { get; set; } = [];
    public virtual ICollection<MediaEntity> Media { get; set; } = [];
}