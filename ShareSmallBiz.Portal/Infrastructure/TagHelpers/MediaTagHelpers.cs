using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using ShareSmallBiz.Portal.Areas.Media.Services;
using ShareSmallBiz.Portal.Data.Enums;
using ShareSmallBiz.Portal.Infrastructure.Extensions;

namespace ShareSmallBiz.Portal.Infrastructure.TagHelpers;

/// <summary>
/// Tag helper for rendering media items
/// Usage: <media id="1" size="md"></media>
/// </summary>
[HtmlTargetElement("media")]
public class MediaTagHelper(
        MediaService mediaService,
        IUrlHelperFactory urlHelperFactory) : TagHelper
{

    [ViewContext]
    [HtmlAttributeNotBound]
    public ViewContext? ViewContext { get; set; }

    /// <summary>
    /// The ID of the media item to render
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The size of the media (xs, sm, md, lg)
    /// </summary>
    public string Size { get; set; } = "md";

    /// <summary>
    /// Whether to show the media as a thumbnail
    /// </summary>
    public bool Thumbnail { get; set; } = false;

    /// <summary>
    /// CSS classes to apply to the media container
    /// </summary>
    public string Class { get; set; } = string.Empty;

    /// <summary>
    /// Additional attributes to apply to the media container
    /// </summary>
    public string Style { get; set; } = string.Empty;

    /// <summary>
    /// Alt text for images
    /// </summary>
    public string Alt { get; set; } = string.Empty;

    /// <summary>
    /// Whether to show controls for audio and video
    /// </summary>
    public bool Controls { get; set; } = true;

    /// <summary>
    /// Whether to link the media to its details page
    /// </summary>
    public bool Link { get; set; } = false;

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        var urlHelper = urlHelperFactory.GetUrlHelper(ViewContext);
        var media = await mediaService.GetMediaByIdAsync(Id);

        if (media == null)
        {
            output.TagName = "div";
            output.Content.SetHtmlContent("<span class=\"text-danger\">Media not found</span>");
            return;
        }

        string url = Thumbnail
            ? media.GetThumbnailUrl(urlHelper, Size)
            : media.GetPublicUrl(urlHelper);

        output.TagName = "div";
        output.Attributes.SetAttribute("class", $"media-container {Class}");

        if (!string.IsNullOrEmpty(Style))
        {
            output.Attributes.SetAttribute("style", Style);
        }

        var content = new TagBuilder("div");

        if (media.MediaType == MediaType.Image)
        {
            var img = new TagBuilder("img");
            img.Attributes.Add("src", url);
            img.Attributes.Add("alt", !string.IsNullOrEmpty(Alt) ? Alt : media.FileName);
            img.Attributes.Add("class", "img-fluid");

            if (Link)
            {
                var a = new TagBuilder("a");
                a.Attributes.Add("href", media.GetPublicUrl(urlHelper));
                a.Attributes.Add("target", "_blank");
                a.InnerHtml.AppendHtml(img);
                content.InnerHtml.AppendHtml(a);
            }
            else
            {
                content.InnerHtml.AppendHtml(img);
            }
        }
        else if (media.MediaType == MediaType.Video)
        {
            if (media.StorageProvider == StorageProviderNames.YouTube)
            {
                var iframe = new TagBuilder("iframe");
                iframe.Attributes.Add("src", media.GetYouTubeEmbedUrl());
                iframe.Attributes.Add("title", media.FileName);
                iframe.Attributes.Add("frameborder", "0");
                iframe.Attributes.Add("allowfullscreen", "true");
                iframe.Attributes.Add("class", "w-100");

                // Apply responsive aspect ratio container
                var container = new TagBuilder("div");
                container.Attributes.Add("class", "ratio ratio-16x9");
                container.InnerHtml.AppendHtml(iframe);

                content.InnerHtml.AppendHtml(container);
            }
            else
            {
                var video = new TagBuilder("video");
                if (Controls)
                {
                    video.Attributes.Add("controls", "controls");
                }
                video.Attributes.Add("class", "w-100");

                var source = new TagBuilder("source");
                source.Attributes.Add("src", url);
                source.Attributes.Add("type", media.ContentType);

                video.InnerHtml.AppendHtml(source);
                video.InnerHtml.AppendHtml("Your browser does not support the video tag.");

                content.InnerHtml.AppendHtml(video);
            }
        }
        else if (media.MediaType == MediaType.Audio)
        {
            var audio = new TagBuilder("audio");
            if (Controls)
            {
                audio.Attributes.Add("controls", "controls");
            }
            audio.Attributes.Add("class", "w-100");

            var source = new TagBuilder("source");
            source.Attributes.Add("src", url);
            source.Attributes.Add("type", media.ContentType);

            audio.InnerHtml.AppendHtml(source);
            audio.InnerHtml.AppendHtml("Your browser does not support the audio tag.");

            content.InnerHtml.AppendHtml(audio);
        }
        else
        {
            // For documents and other file types, just show a link
            var icon = new TagBuilder("i");
            icon.Attributes.Add("class", $"bi {media.MediaType.GetMediaTypeIcon()} me-2");

            var a = new TagBuilder("a");
            a.Attributes.Add("href", url);
            a.Attributes.Add("target", "_blank");
            a.InnerHtml.AppendHtml(icon);
            a.InnerHtml.Append(media.FileName);

            content.InnerHtml.AppendHtml(a);
        }

        output.Content.SetHtmlContent(content.InnerHtml);
    }
}