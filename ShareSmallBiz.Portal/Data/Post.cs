﻿namespace ShareSmallBiz.Portal.Data;

public class Post : BaseEntity
{
    public ShareSmallBizUser Author { get; set; }
    public string AuthorId { get; set; }
    [Required]
    public string Content { get; set; } = string.Empty;
    public string Cover { get; set; }
    [Required]
    [StringLength(450)]
    public string Description { get; set; }
    public bool IsFeatured { get; set; }
    public bool IsPublic { get; set; }
    public virtual ICollection<PostLike> Likes { get; set; } = [];
    public virtual ICollection<Keyword> PostCategories { get; set; } = [];
    public Models.PostType PostType { get; set; } = Models.PostType.Post;
    public int PostViews { get; set; }
    public DateTime Published { get; set; }
    public double Rating { get; set; }
    public bool Selected { get; set; }
    [Required]
    [StringLength(160)]
    public string Slug { get; set; }
    [Required]
    [StringLength(160)]
    public string Title { get; set; }
    public virtual ICollection<PostComment> Comments { get; set; } = [];
}
