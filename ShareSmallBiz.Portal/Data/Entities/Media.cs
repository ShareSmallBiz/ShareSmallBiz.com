// First, let's create the Media entity class
using ShareSmallBiz.Portal.Data.Enums;
using System;

namespace ShareSmallBiz.Portal.Data.Entities;

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

    public virtual ShareSmallBizUser User { get; set; }

}

