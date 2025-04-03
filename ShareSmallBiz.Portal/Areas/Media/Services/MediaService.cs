using ShareSmallBiz.Portal.Data;

namespace ShareSmallBiz.Portal.Areas.Media.Services;

/// <summary>
/// Service for database operations on media entities
/// </summary>
public class MediaService
{
    private readonly ShareSmallBizUserContext _context;
    private readonly ILogger<MediaService> _logger;
    private readonly StorageProviderService _storageProviderService;

    public MediaService(
        ShareSmallBizUserContext context,
        ILogger<MediaService> logger,
        StorageProviderService storageProviderService)
    {
        _context = context;
        _logger = logger;
        _storageProviderService = storageProviderService;
    }

    /// <summary>
    /// Gets all media for a specific user
    /// </summary>
    public async Task<IEnumerable<ShareSmallBiz.Portal.Data.Media>> GetUserMediaAsync(string userId)
    {
        return await _context.Media
            .Where(m => m.UserId == userId)
            .OrderByDescending(m => m.CreatedDate)
            .ToListAsync();
    }

    /// <summary>
    /// Gets a specific media item by ID
    /// </summary>
    public async Task<ShareSmallBiz.Portal.Data.Media?> GetMediaByIdAsync(int id)
    {
        return await _context.Media
            .Include(m => m.User)
            .FirstOrDefaultAsync(m => m.Id == id);
    }

    /// <summary>
    /// Gets a specific media item by ID and user ID (for security)
    /// </summary>
    public async Task<ShareSmallBiz.Portal.Data.Media?> GetUserMediaByIdAsync(int id, string userId)
    {
        return await _context.Media
            .Include(m => m.User)
            .FirstOrDefaultAsync(m => m.Id == id && m.UserId == userId);
    }

    /// <summary>
    /// Creates a new media entity in the database
    /// </summary>
    public async Task<ShareSmallBiz.Portal.Data.Media> CreateMediaAsync(ShareSmallBiz.Portal.Data.Media media)
    {
        _context.Media.Add(media);
        await _context.SaveChangesAsync();
        return media;
    }

    /// <summary>
    /// Updates an existing media entity in the database
    /// </summary>
    public async Task<bool> UpdateMediaAsync(ShareSmallBiz.Portal.Data.Media media)
    {
        try
        {
            media.ModifiedDate = DateTime.UtcNow;
            _context.Update(media);
            await _context.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogError(ex, "Concurrency exception when updating media with ID {MediaId}", media.Id);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating media with ID {MediaId}", media.Id);
            return false;
        }
    }

    /// <summary>
    /// Deletes a media entity and its associated files
    /// </summary>
    public async Task DeleteMediaAsync(ShareSmallBiz.Portal.Data.Media media)
    {
        try
        {
            // First, delete any files associated with the media
            await _storageProviderService.DeleteFileAsync(media);

            // Then remove from database
            _context.Media.Remove(media);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete media {MediaId}", media.Id);
            throw;
        }
    }

    /// <summary>
    /// Converts a user's profile picture to a Media entity
    /// </summary>
    public async Task<ShareSmallBiz.Portal.Data.Media?> ConvertProfilePictureToMediaAsync(ShareSmallBizUser user)
    {
        if (user.ProfilePicture == null || user.ProfilePicture.Length == 0)
        {
            return null;
        }

        try
        {
            // Create a media entity for the profile picture
            var media = new ShareSmallBiz.Portal.Data.Media
            {
                FileName = $"profile_{user.Id}_{DateTime.UtcNow.Ticks}.jpg",
                MediaType = MediaType.Image,
                StorageProvider = StorageProviderNames.LocalStorage,
                ContentType = "image/jpeg",
                FileSize = user.ProfilePicture.Length,
                Description = $"{user.DisplayName}'s profile picture",
                StorageMetadata = "{\"type\":\"profile\"}",
                UserId = user.Id,
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow
            };

            // Save the file using the storage provider service
            await _storageProviderService.SaveProfilePictureAsync(user.ProfilePicture, media);

            // Add the media entity to the database
            _context.Media.Add(media);
            await _context.SaveChangesAsync();

            // Update the user's ProfilePictureUrl to point to the media
            user.ProfilePictureUrl = $"/Media/{media.Id}";
            await _context.SaveChangesAsync();

            return media;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to convert profile picture to media for user {UserId}", user.Id);
            return null;
        }
    }

    /// <summary>
    /// Gets the URL for a media item
    /// </summary>
    public string GetMediaUrl(ShareSmallBiz.Portal.Data.Media media)
    {
        if (media.StorageProvider == StorageProviderNames.External ||
            media.StorageProvider == StorageProviderNames.YouTube)
        {
            return media.Url;
        }

        return $"/Media/{media.Id}";
    }

    /// <summary>
    /// Gets the URL for a media thumbnail
    /// </summary>
    public string GetThumbnailUrl(ShareSmallBiz.Portal.Data.Media media)
    {
        if (media.MediaType != MediaType.Image)
        {
            // Return appropriate icon URL based on media type
            return $"/images/{media.MediaType.ToString().ToLower()}-icon.png";
        }

        return $"/Media/Thumbnail/{media.Id}";
    }

    /// <summary>
    /// Checks if a media entity exists
    /// </summary>
    public bool MediaExists(int id)
    {
        return _context.Media.Any(e => e.Id == id);
    }

    /// <summary>
    /// Gets media items by media type
    /// </summary>
    public async Task<IEnumerable<ShareSmallBiz.Portal.Data.Media>> GetMediaByTypeAsync(string userId, MediaType mediaType)
    {
        return await _context.Media
            .Where(m => m.UserId == userId && m.MediaType == mediaType)
            .OrderByDescending(m => m.CreatedDate)
            .ToListAsync();
    }

    /// <summary>
    /// Gets media items by storage provider
    /// </summary>
    public async Task<IEnumerable<ShareSmallBiz.Portal.Data.Media>> GetMediaByStorageProviderAsync(string userId, StorageProviderNames storageProvider)
    {
        return await _context.Media
            .Where(m => m.UserId == userId && m.StorageProvider == storageProvider)
            .OrderByDescending(m => m.CreatedDate)
            .ToListAsync();
    }

    /// <summary>
    /// Searches for media by keywords
    /// </summary>
    public async Task<IEnumerable<ShareSmallBiz.Portal.Data.Media>> SearchMediaAsync(string userId, string searchTerm)
    {
        return await _context.Media
            .Where(m => m.UserId == userId &&
                  (m.FileName.Contains(searchTerm) ||
                   m.Description.Contains(searchTerm) ||
                   m.Attribution.Contains(searchTerm)))
            .OrderByDescending(m => m.CreatedDate)
            .ToListAsync();
    }
}