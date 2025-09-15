using System.ComponentModel.DataAnnotations.Schema;

namespace ShareSmallBiz.Portal.Data.Entities;

public class PostComment : BaseEntity
{
    public ShareSmallBizUser Author { get; set; } = null!; // required
    [ForeignKey("Post")]
    public int PostId { get; set; } // Foreign Key to Post
    public virtual Post Post { get; set; } = null!;
    public string Content { get; set; } = string.Empty;
    // CreatedDate provided by BaseEntity
    // Optional: Allow some comments to turn into standalone posts
    [ForeignKey("ParentPost")]
    public int? ParentPostId { get; set; }
    public Post? ParentPost { get; set; }
    public virtual ICollection<PostCommentLike> Likes { get; set; } = new List<PostCommentLike>();
    public virtual ICollection<Media> Media { get; set; } = new List<Media>();
}
