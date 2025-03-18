using ShareSmallBiz.Portal.Data;
namespace ShareSmallBiz.Portal.Infrastructure.Extensions;
public static class MediaExtensions
{
    /// <summary>
    /// Gets the URL for displaying the media
    /// </summary>
    public static string GetMediaUrl(this Media media, IUrlHelper urlHelper)
    {
        if (media == null) throw new ArgumentNullException(nameof(media));
        return media.StorageProvider == StorageProviderNames.External
            ? media.Url
            : $"/media/{media.Id}";
    }
    /// <summary>
    /// Gets the URL for displaying the thumbnail of the media
    /// </summary>
    public static string GetThumbnailUrl(this Media media, IUrlHelper urlHelper)
    {
        if (media == null) throw new ArgumentNullException(nameof(media));
        return media.StorageProvider == StorageProviderNames.External && media.MediaType == MediaType.Image
            ? media.Url
            : $"/media/thumbnail/{media.Id}";
    }
    /// <summary>
    /// Gets the appropriate HTML tag for displaying the media based on its type
    /// </summary>
    public static string ToHtml(this Media media, IUrlHelper urlHelper, string cssClass = "", string style = "", Dictionary<string, string> attributes = null)
    {
        if (media == null) throw new ArgumentNullException(nameof(media));
        string mediaUrl = media.GetMediaUrl(urlHelper);
        string tagAttributes = $"class=\"{cssClass}\" style=\"{style}\"";
        if (attributes != null)
        {
            foreach (var attr in attributes)
            {
                tagAttributes += $" {attr.Key}=\"{attr.Value}\"";
            }
        }
        return media.MediaType switch
        {
            MediaType.Image => $"<img src=\"{mediaUrl}\" alt=\"{media.Description}\" {tagAttributes} />",
            MediaType.Video => $"<video controls {tagAttributes}><source src=\"{mediaUrl}\" type=\"{media.ContentType}\">Your browser does not support the video tag.</video>",
            MediaType.Audio => $"<audio controls {tagAttributes}><source src=\"{mediaUrl}\" type=\"{media.ContentType}\">Your browser does not support the audio tag.</audio>",
            MediaType.Document => $"<a href=\"{mediaUrl}\" {tagAttributes}>Download {media.FileName}</a>",
            _ => $"<a href=\"{mediaUrl}\" {tagAttributes}>View {media.FileName}</a>"
        };
    }
}