using ShareSmallBiz.Portal.Data.Enums;

public class MediaEntity : BaseEntity
{
    [Required]
    [StringLength(255)]
    public string FileName { get; set; }

    [Required]
    public MediaType MediaType { get; set; }

    [Required]
    public StorageProviderNames StorageProvider { get; set; }

    [Required]
    [StringLength(2048)]
    public string Url { get; set; }

    [StringLength(255)]
    public string ContentType { get; set; }

    public long? FileSize { get; set; }

    [StringLength(512)]
    public string Description { get; set; }

    [StringLength(1024)]
    public string StorageMetadata { get; set; }

    [StringLength(255)]
    public string Attribution { get; set; }

}