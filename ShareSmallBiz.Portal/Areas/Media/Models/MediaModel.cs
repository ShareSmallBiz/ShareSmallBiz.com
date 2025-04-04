using ShareSmallBiz.Portal.Data.Enums;

namespace ShareSmallBiz.Portal.Areas.Media.Models;

public class MediaModel
{
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
    public string UserId { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;
    public int? PostId { get; set; }
    public int? CommentId { get; set; }
}

