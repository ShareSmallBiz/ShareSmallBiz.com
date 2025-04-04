namespace ShareSmallBiz.Portal.Data.Entities;

public partial class Keyword : BaseEntity
{
    public required string Name { get; set; }
    public required string Description { get; set; }
    public virtual ICollection<Post> Posts { get; set; } = [];
}
