using ShareSmallBiz.Portal.Data;

namespace ShareSmallBiz.Portal.Infrastructure.Services;

public class PostModel : BaseModel, IEquatable<PostModel>
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

    public PostModel() { }

    public PostModel(Post post)
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
    }
    public bool Equals(PostModel other)
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
        return Equals(obj as PostModel);
    }
}

public abstract class BaseModel
{
    public int Id { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;
    public string? CreatedID { get; set; }
    public string? ModifiedID { get; set; }

    // Automatically ties entity to the user who created it
    public UserModel? CreatedUser { get; set; }
}

public class UserModel
{
    public string Id { get; set; }
    public string UserName { get; set; } = string.Empty;

    // User statistics
    public int PostCount { get; set; } = 0;
    public int LikeCount { get; set; } = 0;

    public UserModel() { }

    public UserModel(string createdId, string username, int postCount, int likeCount)
    {
        Id = createdId;
        UserName = username;
        PostCount = postCount;
        LikeCount = likeCount;
    }

    public UserModel(ShareSmallBizUser author)
    {
        if(author == null)
            return; 

        Id = author.Id;
        UserName = author.UserName;
        PostCount = author.Posts.Count;
        LikeCount = author.LikedPosts.Count;
    }
}
public class UserFollowModel : BaseModel
{
    public string FollowingId { get; set; } = string.Empty;
}

public class PostLikeModel : BaseModel
{
    public int PostId { get; set; }
}

public class PostCommentLikeModel : BaseModel
{
    public int PostCommentId { get; set; }
}

public class PostCommentModel : BaseModel
{
    public int PostId { get; set; } // Tied to the original post
    public string Content { get; set; } = string.Empty;
    public int? ParentPostId { get; set; } // Optional parent post

    public int LikeCount { get; set; } = 0; // Count of likes on the comment

    public UserModel? CreatedUser { get; set; } // The author of the comment

    public List<PostCommentLikeModel> Likes { get; set; } = [];

    public PostCommentModel() { }

    public PostCommentModel(PostComment comment)
    {
        if (comment == null)
            return;

        Id = comment.Id;
        PostId = comment.PostId;
        Content = comment.Content;
        CreatedDate = comment.CreatedDate;
        ModifiedDate = comment.ModifiedDate;
        ParentPostId = comment.ParentPostId;
        LikeCount = comment.Likes?.Count ?? 0;

        // Map User
        CreatedUser = new UserModel
        {
            Id = comment.Author?.Id,
            UserName = comment.Author?.UserName ?? "Anonymous"
        };

        // Map Likes
        Likes = comment.Likes != null ?
            comment.Likes.Select(like => new PostCommentLikeModel
            {
                Id = like.Id,
                PostCommentId = like.PostCommentId,
                CreatedID = like.CreatedID
            }).ToList() : [];
    }
}
