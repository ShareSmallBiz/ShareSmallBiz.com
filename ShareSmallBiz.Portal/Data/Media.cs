// First, let's create the Media entity class
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShareSmallBiz.Portal.Data;

public enum MediaType
{
    Image,
    Video,
    Audio,
    Document,
    Other
}

public enum StorageProviderNames
{
    LocalStorage = 0,      // Stored on your server
    External = 1,   // Linked from external source (like Unsplash)
    AzureBlob =2 ,  // Stored in Azure Blob Storage
    S3 = 3,      // Stored in AWS S3
    YouTube = 4     // Stored on YouTube

}

public class Media : BaseEntity
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(255)]
    public string FileName { get; set; }

    [Required]
    public MediaType MediaType { get; set; }

    [Required]
    public StorageProviderNames StorageProvider { get; set; }

    [Required]
    [StringLength(2048)]
    public string Url { get; set; }  // URL or file path depending on storage type

    [StringLength(255)]
    public string ContentType { get; set; }  // MIME type

    public long? FileSize { get; set; }  // in bytes

    [StringLength(512)]
    public string Description { get; set; }

    [StringLength(1024)]
    public string StorageMetadata { get; set; }  // JSON string for additional storage-specific metadata

    // Attribution for external content
    [StringLength(255)]
    public string Attribution { get; set; }

    // Relationship with the user who uploaded or linked the media
    public string UserId { get; set; }
    public virtual ShareSmallBizUser User { get; set; }

    // Optional parent Post relationship
    public int? PostId { get; set; }
    public virtual Post Post { get; set; }

    // Optional parent Comment relationship
    public int? CommentId { get; set; }
    public virtual PostComment Comment { get; set; }
}

