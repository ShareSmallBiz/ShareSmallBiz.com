using ShareSmallBiz.Portal.Areas.Media.Models;
using ShareSmallBiz.Portal.Data.Enums;
using System;

namespace ShareSmallBiz.Portal.Infrastructure.Extensions;

public static class MediaExtensions
{
    /// <summary>
    /// Gets the URL for a media thumbnail
    /// </summary>
    public static string GetThumbnailUrl(this MediaModel media, IUrlHelper urlHelper, string size = "sm")
    {
        if (media == null)
        {
            return string.Empty;
        }

        // For external links to images, return the external URL
        if (media.StorageProvider == StorageProviderNames.External && media.MediaType == MediaType.Image)
        {
            return media.Url;
        }

        // For YouTube thumbnails, generate a YouTube thumbnail URL
        if (media.StorageProvider == StorageProviderNames.YouTube)
        {
            string videoId = GetYouTubeVideoId(media.Url);
            if (!string.IsNullOrEmpty(videoId))
            {
                return $"https://img.youtube.com/vi/{videoId}/mqdefault.jpg";
            }
        }

        // For local media, return the thumbnail URL
        return urlHelper.Action("Thumbnail", "Media", new { id = media.Id, size = size });
    }

    /// <summary>
    /// Gets the public URL for a media item
    /// </summary>
    public static string GetPublicUrl(this MediaModel media, IUrlHelper urlHelper)
    {
        if (media == null)
        {
            return string.Empty;
        }

        // For external links, return the external URL
        if (media.StorageProvider == StorageProviderNames.External || media.StorageProvider == StorageProviderNames.YouTube)
        {
            return media.Url;
        }

        // For local media, return the media URL
        return urlHelper.Action("Index", "Media", new { id = media.Id });
    }

    /// <summary>
    /// Gets the embed URL for a YouTube video
    /// </summary>
    public static string GetYouTubeEmbedUrl(this MediaModel media)
    {
        if (media == null || media.StorageProvider != StorageProviderNames.YouTube)
        {
            return string.Empty;
        }

        string videoId = GetYouTubeVideoId(media.Url);
        if (!string.IsNullOrEmpty(videoId))
        {
            return $"https://www.youtube.com/embed/{videoId}";
        }

        return string.Empty;
    }

    /// <summary>
    /// Extracts the YouTube video ID from a YouTube URL
    /// </summary>
    private static string GetYouTubeVideoId(string url)
    {
        if (string.IsNullOrEmpty(url))
        {
            return string.Empty;
        }

        // Match standard YouTube URLs
        // Handles formats like:
        // - https://www.youtube.com/watch?v=VIDEO_ID
        // - https://youtu.be/VIDEO_ID
        // - https://youtube.com/watch?v=VIDEO_ID
        // - https://www.youtube.com/embed/VIDEO_ID
        var regex = new System.Text.RegularExpressions.Regex(@"(?:https?:\/\/)?(?:www\.)?(?:youtube\.com\/(?:watch\?v=|embed\/)|youtu\.be\/)([a-zA-Z0-9_-]{11})");
        var match = regex.Match(url);

        if (match.Success && match.Groups.Count > 1)
        {
            return match.Groups[1].Value;
        }

        return string.Empty;
    }

    /// <summary>
    /// Gets the appropriate icon for a media type
    /// </summary>
    public static string GetMediaTypeIcon(this MediaType mediaType)
    {
        return mediaType switch
        {
            MediaType.Image => "bi-file-image",
            MediaType.Video => "bi-film",
            MediaType.Audio => "bi-file-earmark-music",
            MediaType.Document => "bi-file-earmark-text",
            _ => "bi-file-earmark"
        };
    }

    /// <summary>
    /// Gets the file extension from a file name
    /// </summary>
    public static string GetFileExtension(this string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
        {
            return string.Empty;
        }

        return Path.GetExtension(fileName).ToLowerInvariant();
    }

    /// <summary>
    /// Gets the appropriate media type for a file extension
    /// </summary>
    public static MediaType GetMediaTypeFromExtension(this string extension)
    {
        if (string.IsNullOrEmpty(extension))
        {
            return MediaType.Other;
        }

        extension = extension.ToLowerInvariant();

        // Image extensions
        if (extension is ".jpg" or ".jpeg" or ".png" or ".gif" or ".webp" or ".svg" or ".bmp")
        {
            return MediaType.Image;
        }

        // Video extensions
        if (extension is ".mp4" or ".webm" or ".mov" or ".avi" or ".mkv" or ".wmv")
        {
            return MediaType.Video;
        }

        // Audio extensions
        if (extension is ".mp3" or ".wav" or ".ogg" or ".flac" or ".m4a" or ".aac")
        {
            return MediaType.Audio;
        }

        // Document extensions
        if (extension is ".pdf" or ".doc" or ".docx" or ".xls" or ".xlsx" or ".ppt" or ".pptx" or ".txt" or ".md" or ".csv")
        {
            return MediaType.Document;
        }

        // Default
        return MediaType.Other;
    }
}