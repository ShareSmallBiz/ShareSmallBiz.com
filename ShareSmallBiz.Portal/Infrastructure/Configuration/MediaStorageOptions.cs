namespace ShareSmallBiz.Portal.Infrastructure.Configuration;

public class MediaStorageOptions
{
    public const string MediaStorage = "MediaStorage";

    /// <summary>
    /// Root path for media storage (default: c:\websites\sharesmallbiz\media\)
    /// </summary>
    public string RootPath { get; set; } = Path.Combine("c:", "websites", "sharesmallbiz", "media");

    /// <summary>
    /// Maximum upload file size in bytes (default: 10MB)
    /// </summary>
    public long MaxFileSize { get; set; } = 10 * 1024 * 1024;

    /// <summary>
    /// Allowed file extensions for upload
    /// </summary>
    public List<string> AllowedExtensions { get; set; } = new List<string>
    {
        ".jpg", ".jpeg", ".png", ".gif", ".webp",  // Images
        ".mp4", ".webm", ".mov", ".avi",           // Videos
        ".mp3", ".wav", ".ogg", ".flac",           // Audio
        ".pdf", ".doc", ".docx", ".xls", ".xlsx",  // Documents
        ".ppt", ".pptx", ".txt", ".md", ".csv"     // More documents
    };

    /// <summary>
    /// Default thumbnail size
    /// </summary>
    public int DefaultThumbnailSize { get; set; } = 200;

    /// <summary>
    /// Available thumbnail sizes
    /// </summary>
    public Dictionary<string, int> ThumbnailSizes { get; set; } = new Dictionary<string, int>
    {
        { "xs", 100 },
        { "sm", 200 },
        { "md", 400 },
        { "lg", 800 }
    };
}