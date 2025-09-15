using ShareSmallBiz.Portal.Data.Enums;

namespace ShareSmallBiz.Portal.Data.Entities;
public class Post : BaseEntity
{
    public ShareSmallBizUser Author { get; set; } = null!; // required
    public ShareSmallBizUser? Target { get; set; }
    public string? TargetId { get; set; }
    public string AuthorId { get; set; } = string.Empty;
    [Required]
    public string Content { get; set; } = string.Empty;
    public string Cover { get; set; } = string.Empty;
    [Required]
    [StringLength(450)]
    public string Description { get; set; } = string.Empty;
    public bool IsFeatured { get; set; }
    public bool IsPublic { get; set; }
    public PostType PostType { get; set; } = PostType.Post;
    public int PostViews { get; set; }
    public DateTime Published { get; set; }
    public double Rating { get; set; }
    public bool Selected { get; set; }
    [Required]
    [StringLength(160)]
    public string Slug { get; set; } = string.Empty;
    [Required]
    [StringLength(160)]
    public string Title { get; set; } = string.Empty;
    public virtual ICollection<PostLike> Likes { get; set; } = new List<PostLike>();
    public virtual ICollection<Keyword> PostCategories { get; set; } = new List<Keyword>();
    public virtual ICollection<PostComment> Comments { get; set; } = new List<PostComment>();
    public virtual ICollection<Media> Media { get; set; } = new List<Media>();
}
