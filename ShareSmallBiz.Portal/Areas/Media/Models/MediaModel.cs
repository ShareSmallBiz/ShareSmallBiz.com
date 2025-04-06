using ShareSmallBiz.Portal.Data.Enums;

namespace ShareSmallBiz.Portal.Areas.Media.Models;

public class MediaModel
{
    public MediaModel()
    {
        
    }

    public MediaModel(ShareSmallBiz.Portal.Data.Entities.Media media)
    {
        Id = media.Id;
        FileName = media.FileName;
        MediaType = media.MediaType;
        StorageProvider = media.StorageProvider;
        Url = media.Url;
        ContentType = media.ContentType;
        FileSize = media.FileSize;
        Description = media.Description;
        StorageMetadata = media.StorageMetadata;
        Attribution = media.Attribution;
        UserId = media.UserId;
        CreatedDate = media.CreatedDate;
        ModifiedDate = media.ModifiedDate;
        PostId = media.PostId;
        CommentId = media.CommentId;
        CreatedID = media.CreatedID;
        ModifiedID = media.ModifiedID;
        UserId = media.CreatedID;
    }


    public int Id { get; set; }
    public string FileName { get; set; }
    public MediaType MediaType { get; set; }
    public StorageProviderNames StorageProvider { get; set; }
    public string Url { get; set; }
    public string ContentType { get; set; }
    public long? FileSize { get; set; }
    public string Description { get; set; }
    public string StorageMetadata { get; set; }
    public string Attribution { get; set; }
    public string CreatedID { get;  set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public string ModifiedID { get; set; }
    public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;
}

