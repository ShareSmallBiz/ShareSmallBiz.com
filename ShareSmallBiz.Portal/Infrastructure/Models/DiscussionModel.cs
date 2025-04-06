using ShareSmallBiz.Portal.Areas.Media.Models;
using ShareSmallBiz.Portal.Data.Entities;
using ShareSmallBiz.Portal.Data.Enums;

namespace ShareSmallBiz.Portal.Infrastructure.Models;

public class DiscussionListModel : List<DiscussionModel>
{
    public DiscussionListModel() { }
    public DiscussionListModel(IEnumerable<Post> posts)
    {
        AddRange(posts.Select(post => new DiscussionModel(post)));
    }
    public string Description { get; set; } = string.Empty;
}


public class DiscussionModel : BaseModel, IEquatable<DiscussionModel>
{

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
        CreatedID = post.CreatedID ?? post.AuthorId;
        ModifiedID = post.ModifiedID ?? post.CreatedID?? post.AuthorId;
        CreatedDate = post.CreatedDate;
        ModifiedDate = post.ModifiedDate;
        Comments = post.Comments?.Select(comment => new PostCommentModel(comment)).ToList() ?? [];
        Likes = post.Likes?.Select(like => new PostLikeModel(like)).ToList() ?? [];
        Creator = new UserModel(post.Author);
        Tags = post.PostCategories?.Select(x => x.Name).ToList() ?? [];
        Media = post.Media?.Select(media => new MediaModel(media)).ToList() ?? [];
    }

    public bool Equals(DiscussionModel other)
    {
        if (Id == other.Id)
            return true;

        return false;
    }

    public override bool Equals(object obj)
    {
        return Equals(obj as DiscussionModel);
    }
    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }

    public List<PostCommentModel> Comments { get; set; } = [];
    public List<PostLikeModel> Likes { get; set; } = [];
    public string Content { get; set; } = string.Empty;
    public string Cover { get; set; } = "https://sharesmallbiz.com/";
    public string Description { get; set; } = string.Empty;
    public int Id { get; set; }
    public bool IsFeatured { get; set; } = false;
    public bool IsPublic { get; set; } = true;
    public List<string> Keywords { get; set; } = [];
    public PostType PostType { get; set; } = PostType.Post;
    public int PostViews { get; set; } = 0;
    public DateTime Published { get; set; } = DateTime.UtcNow;
    public double Rating { get; set; } = 0;
    public bool Selected { get; set; } = false;
    public string Slug { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = [];
    public string Title { get; set; } = string.Empty;
    public List<MediaModel> Media { get; set; } = [];
}
