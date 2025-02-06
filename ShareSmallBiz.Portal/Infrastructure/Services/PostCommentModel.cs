using ShareSmallBiz.Portal.Data;

namespace ShareSmallBiz.Portal.Infrastructure.Services
{
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
}
