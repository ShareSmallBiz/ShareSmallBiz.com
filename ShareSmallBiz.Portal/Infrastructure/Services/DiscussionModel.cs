using ShareSmallBiz.Portal.Data;

namespace ShareSmallBiz.Portal.Infrastructure.Services;

public class DiscussionModel : BaseModel, IEquatable<DiscussionModel>
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Cover { get; set; } = "https://sharesmallbiz.com/";
    public bool IsFeatured { get; set; } = false;
    public bool IsPublic { get; set; } = true;
    public Models.PostType PostType { get; set; } = Models.PostType.Post;
    public int PostViews { get; set; } = 0;
    public DateTime Published { get; set; } = DateTime.UtcNow;
    public double Rating { get; set; } = 0;
    public bool Selected { get; set; } = false;
    public string Slug { get; set; } = string.Empty;
    public string? CreatedID { get; set; }
    public string? ModifiedID { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;
    public List<PostCommentModel> Comments { get; set; } = [];
    public UserModel Author { get; set; } = new();
    public List<string> Tags { get; set; } = [];
    public List<string> Keywords { get; set; } = [];

    public DiscussionModel() { }

    public DiscussionModel(Post post)
    {
        Id = post.Id;
        Title = post.Title;
        Content = post.Content;
        Description = post.Description;
        Cover = post.Cover;
        IsFeatured = post.IsFeatured;
        IsPublic = post.IsPublic;
        PostType = post.PostType;
        PostViews = post.PostViews;
        Published = post.Published;
        Rating = post.Rating;
        Selected = post.Selected;
        Slug = post.Slug;
        CreatedID = post.CreatedID;
        ModifiedID = post.ModifiedID;
        CreatedDate = post.CreatedDate;
        ModifiedDate = post.ModifiedDate;
        CreatedID = post.AuthorId;
        Comments = [.. post.Comments.Select(comment => new PostCommentModel(comment))];
        Author = new UserModel(post.Author);
        Tags = [.. post.PostCategories.Select(x => x.Name)];
    }
    public bool Equals(DiscussionModel other)
    {
        if (Id == other.Id)
            return true;

        return false;
    }
    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }

    public override bool Equals(object obj)
    {
        return Equals(obj as DiscussionModel);
    }
}
