using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ShareSmallBiz.Portal.Areas.Media.Models;
using ShareSmallBiz.Portal.Data;
using ShareSmallBiz.Portal.Data.Entities;
using ShareSmallBiz.Portal.Data.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ShareSmallBiz.Portal.Areas.Media.Services;

/// <summary>
/// Service for database operations on media models.
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

    // Helper method to map from MediaEntity entity to MediaModel.
    private MediaModel MapToModel(MediaEntity entity)
    {
        if (entity == null)
        {
            return null;
        }
        return new MediaModel
        {
            Id = entity.Id,
            FileName = entity.FileName,
            MediaType = entity.MediaType,
            StorageProvider = entity.StorageProvider,
            Url = entity.Url,
            ContentType = entity.ContentType,
            FileSize = entity.FileSize,
            Description = entity.Description,
            StorageMetadata = entity.StorageMetadata,
            Attribution = entity.Attribution,
            CreatedID = entity.CreatedID,
            CreatedDate = entity.CreatedDate,
            ModifiedDate = entity.ModifiedDate,
            ModifiedID = entity.ModifiedID,

        };
    }

    // Helper method to map from MediaModel to MediaEntity entity.
    private MediaEntity MapToEntity(MediaModel model)
    {
        if (model == null)
        {
            return null;
        }
        return new MediaEntity
        {
            Id = model.Id,
            FileName = model.FileName,
            MediaType = model.MediaType,
            StorageProvider = model.StorageProvider,
            Url = model.Url,
            ContentType = model.ContentType,
            FileSize = model.FileSize,
            Description = model.Description,
            StorageMetadata = model.StorageMetadata,
            Attribution = model.Attribution,
            CreatedDate = model.CreatedDate,
            CreatedID = model.CreatedID,
            ModifiedDate = model.ModifiedDate,
            ModifiedID = model.ModifiedID
        };
    }

    /// <summary>
    /// Gets all media for a specific user.
    /// </summary>
    public async Task<IEnumerable<MediaModel>> GetUserMediaAsync(string userId)
    {
        var entities = await _context.Media
            .Where(m => m.CreatedID == userId)
            .OrderByDescending(m => m.CreatedDate)
            .ToListAsync();
        return entities.Select(MapToModel);
    }

    /// <summary>
    /// Gets a specific media item by ID.
    /// </summary>
    public async Task<MediaModel> GetMediaByIdAsync(int id)
    {
        var entity = await _context.Media
            .Include(m => m.Creator)
            .FirstOrDefaultAsync(m => m.Id == id);
        return MapToModel(entity);
    }

    /// <summary>
    /// Gets a specific media item by ID and user ID (for security).
    /// </summary>
    public async Task<MediaModel> GetUserMediaByIdAsync(int id, string userId)
    {
        var entity = await _context.Media
            .Include(m => m.Creator)
            .FirstOrDefaultAsync(m => m.Id == id && m.CreatedID == userId);
        return MapToModel(entity);
    }

    /// <summary>
    /// Creates a new media model in the database.
    /// </summary>
    public async Task<MediaModel> CreateMediaAsync(MediaModel mediaModel)
    {
        // Map model to entity.
        var entity = MapToEntity(mediaModel);
        // Set creation timestamps.
        entity.CreatedDate = DateTime.UtcNow;
        entity.ModifiedDate = DateTime.UtcNow;

        _context.Media.Add(entity);
        await _context.SaveChangesAsync();
        // Map back to model (to include new generated fields like Id).
        return MapToModel(entity);
    }

    /// <summary>
    /// Updates an existing media model in the database.
    /// </summary>
    public async Task<bool> UpdateMediaAsync(MediaModel mediaModel)
    {
        try
        {
            // Retrieve the existing entity.
            var entity = await _context.Media.FindAsync(mediaModel.Id);
            if (entity == null)
            {
                return false;
            }

            // Update properties (excluding immutable ones like Id, CreatedDate).
            entity.FileName = mediaModel.FileName;
            entity.MediaType = mediaModel.MediaType;
            entity.StorageProvider = mediaModel.StorageProvider;
            entity.Url = mediaModel.Url;
            entity.ContentType = mediaModel.ContentType;
            entity.FileSize = mediaModel.FileSize;
            entity.Description = mediaModel.Description;
            entity.StorageMetadata = mediaModel.StorageMetadata;
            entity.Attribution = mediaModel.Attribution;
            entity.ModifiedDate = DateTime.UtcNow;
            entity.ModifiedID = mediaModel.ModifiedID;

            _context.Update(entity);
            await _context.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogError(ex, "Concurrency exception when updating media with ID {MediaId}", mediaModel.Id);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating media with ID {MediaId}", mediaModel.Id);
            return false;
        }
    }

    /// <summary>
    /// Deletes a media model and its associated files.
    /// </summary>
    public async Task DeleteMediaAsync(MediaModel mediaModel)
    {
        try
        {
            // Retrieve the entity from the database.
            var entity = await _context.Media.FindAsync(mediaModel.Id);
            if (entity == null)
            {
                throw new Exception($"MediaEntity with ID {mediaModel.Id} not found.");
            }

            // First, delete any files associated with the media.
            await _storageProviderService.DeleteFileAsync(mediaModel);

            // Then remove from database.
            _context.Media.Remove(entity);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete media {MediaId}", mediaModel.Id);
            throw;
        }
    }

    /// <summary>
    /// Gets the URL for a media item.
    /// </summary>
    public string GetMediaUrl(MediaModel mediaModel)
    {
        if (mediaModel.StorageProvider == StorageProviderNames.External ||
            mediaModel.StorageProvider == StorageProviderNames.YouTube)
        {
            return mediaModel.Url;
        }
        return $"/MediaEntity/{mediaModel.Id}";
    }

    /// <summary>
    /// Gets the URL for a media thumbnail.
    /// </summary>
    public string GetThumbnailUrl(MediaModel mediaModel)
    {
        if (mediaModel.MediaType != MediaType.Image)
        {
            // Return appropriate icon URL based on media type.
            return $"/images/{mediaModel.MediaType.ToString().ToLower()}-icon.png";
        }
        return $"/MediaEntity/Thumbnail/{mediaModel.Id}";
    }

    /// <summary>
    /// Checks if a media entity exists.
    /// </summary>
    public bool MediaExists(int id)
    {
        return _context.Media.Any(e => e.Id == id);
    }

    /// <summary>
    /// Gets media items by media type.
    /// </summary>
    public async Task<IEnumerable<MediaModel>> GetMediaByTypeAsync(string userId, MediaType mediaType)
    {
        var entities = await _context.Media
            .Where(m => m.CreatedID == userId && m.MediaType == mediaType)
            .OrderByDescending(m => m.CreatedDate)
            .ToListAsync();
        return entities.Select(MapToModel);
    }

    /// <summary>
    /// Gets media items by storage provider.
    /// </summary>
    public async Task<IEnumerable<MediaModel>> GetMediaByStorageProviderAsync(string userId, StorageProviderNames storageProvider)
    {
        var entities = await _context.Media
            .Where(m => m.CreatedID == userId && m.StorageProvider == storageProvider)
            .OrderByDescending(m => m.CreatedDate)
            .ToListAsync();
        return entities.Select(MapToModel);
    }

    /// <summary>
    /// Searches for media by keywords.
    /// </summary>
    public async Task<IEnumerable<MediaModel>> SearchMediaAsync(string userId, string searchTerm)
    {
        var entities = await _context.Media
            .Where(m => m.CreatedID == userId &&
                  (m.FileName.Contains(searchTerm) ||
                   m.Description.Contains(searchTerm) ||
                   m.Attribution.Contains(searchTerm)))
            .OrderByDescending(m => m.CreatedDate)
            .ToListAsync();
        return entities.Select(MapToModel);
    }
}