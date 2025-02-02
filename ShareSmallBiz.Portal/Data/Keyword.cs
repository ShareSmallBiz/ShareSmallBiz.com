namespace ShareSmallBiz.Portal.Data
{
    public partial class Keyword : BaseEntity
    {
        public required string Name { get; set; }
        public required string Description { get; set; }
        public virtual ICollection<Menu> Menus { get; set; } = [];
        public virtual ICollection<ContentPart> ContentParts { get; set; } = [];
        public virtual ICollection<Post> Posts { get; set; } = [];
    }
}
