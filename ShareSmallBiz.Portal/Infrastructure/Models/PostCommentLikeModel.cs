﻿using ShareSmallBiz.Portal.Data;
using ShareSmallBiz.Portal.Infrastructure.Services;

namespace ShareSmallBiz.Portal.Infrastructure.Models;

public class PostCommentLikeModel : BaseModel
{
    public int PostCommentId { get; set; }
}
public class PostLikeModel : BaseModel
{
    public PostLikeModel()
    {
        
    }
    public PostLikeModel(PostLike like)
    {
        if (like == null) return;
        Id = like.Id;
        PostId = like.PostId;
        CreatedID = like.CreatedID;
        CreatedDate = like.CreatedDate;
        ModifiedID = like.ModifiedID;
        ModifiedDate = like.ModifiedDate;
        CreatedUser = new UserModel(like.User);
    }

    public int PostId { get; set; }
}
