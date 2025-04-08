using ShareSmallBiz.Portal.Areas.Media.Models;
using ShareSmallBiz.Portal.Data.Enums;

namespace ShareSmallBiz.Portal.Areas.Media.Services;

/// <summary>
/// Extension methods for MediaService to support forum integration
/// </summary>
public static class MediaServiceExtensions
{
    /// <summary>
    /// Creates a media entity for an external link
    /// </summary>
    public static async Task<MediaModel> CreateExternalMediaAsync(
        this MediaService mediaService,
        string externalUrl,
        string fileName,
        MediaType mediaType,
        string userId,
        string attribution = "",
        string description = "",
        string storageMetaData = "")
    {
        if (string.IsNullOrEmpty(externalUrl))
        {
            throw new ArgumentException("External URL cannot be null or empty", nameof(externalUrl));
        }

        // Determine content type
        string contentType = mediaType switch
        {
            MediaType.Image => "image/jpeg",
            MediaType.Video => "video/mp4",
            MediaType.Audio => "audio/mpeg",
            MediaType.Document => "application/pdf",
            _ => "application/octet-stream"
        };

        // Create media entity
        MediaModel media = new()
        {
            FileName = fileName,
            ContentType = contentType,
            StorageProvider = StorageProviderNames.External,
            MediaType = mediaType,
            StorageMetadata = storageMetaData,
            Url = externalUrl,
            UserId = userId,
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow,
            Attribution = attribution,
            Description = description,
            FileSize = 0 // No actual file size for external links
        };

        // Save to database
        return await mediaService.CreateMediaAsync(media);
    }

    /// <summary>
    /// Links an existing media to a post
    /// </summary>
    public static async Task<bool> LinkMediaToPostAsync(
        this MediaService mediaService,
        int mediaId,
        int postId,
        string userId)
    {
        try
        {
            var media = await mediaService.GetMediaByIdAsync(mediaId);
            if (media == null || media.UserId != userId)
            {
                return false;
            }

            // Create a copy of the media model with the post ID set
            var updatedMedia = new MediaModel
            {
                Id = media.Id,
                FileName = media.FileName,
                MediaType = media.MediaType,
                StorageProvider = media.StorageProvider,
                Url = media.Url,
                ContentType = media.ContentType,
                FileSize = media.FileSize,
                Description = media.Description,
                StorageMetadata = media.StorageMetadata,
                Attribution = media.Attribution,
                UserId = media.UserId,
                CreatedDate = media.CreatedDate,
                ModifiedDate = DateTime.UtcNow,
                PostId = postId,
                CommentId = media.CommentId
            };

            return await mediaService.UpdateMediaAsync(updatedMedia);
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>
    /// Unlinks a media from a post
    /// </summary>
    public static async Task<bool> UnlinkMediaFromPostAsync(
        this MediaService mediaService,
        int mediaId,
        int postId,
        string userId)
    {
        try
        {
            var media = await mediaService.GetMediaByIdAsync(mediaId);
            if (media == null || media.UserId != userId || media.PostId != postId)
            {
                return false;
            }

            // Create a copy of the media model with the post ID set to null
            var updatedMedia = new MediaModel
            {
                Id = media.Id,
                FileName = media.FileName,
                MediaType = media.MediaType,
                StorageProvider = media.StorageProvider,
                Url = media.Url,
                ContentType = media.ContentType,
                FileSize = media.FileSize,
                Description = media.Description,
                StorageMetadata = media.StorageMetadata,
                Attribution = media.Attribution,
                UserId = media.UserId,
                CreatedDate = media.CreatedDate,
                ModifiedDate = DateTime.UtcNow,
                PostId = null,
                CommentId = media.CommentId
            };

            return await mediaService.UpdateMediaAsync(updatedMedia);
        }
        catch (Exception)
        {
            return false;
        }
    }
}