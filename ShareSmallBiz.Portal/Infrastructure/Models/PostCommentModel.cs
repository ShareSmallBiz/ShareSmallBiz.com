using ShareSmallBiz.Portal.Data.Entities;

namespace ShareSmallBiz.Portal.Infrastructure.Models;

public class PostCommentModel : BaseModel
{
    public int PostId { get; set; } // Tied to the original post
    public string Content { get; set; } = string.Empty;
    public int? ParentPostId { get; set; } // Optional parent post
    public int LikeCount { get; set; } = 0; // Count of likes on the comment
    public UserModel? Author { get; set; } // The author of the comment
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
        Author = GetAuthor(comment.Creator);

        // Map Likes
        Likes = comment.Likes != null ?
            [.. comment.Likes.Select(like => new PostCommentLikeModel
            {
                Id = like.Id,
                PostCommentId = like.PostCommentId,
                CreatedID = like.CreatedID
            })] : [];
    }

    public UserModel GetAuthor(ShareSmallBizUser user)
    {
        return new UserModel()
        {
            Id = user.Id,
            UserName = user.UserName ?? string.Empty,
            DisplayName = user.DisplayName,
            ProfilePictureUrl = user.ProfilePictureUrl
        };
    }


}
