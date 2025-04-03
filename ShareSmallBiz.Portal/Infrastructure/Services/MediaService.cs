using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using ShareSmallBiz.Portal.Data;
using System.IO;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace ShareSmallBiz.Portal.Services;

public class MediaService
{
    private readonly IWebHostEnvironment _environment;
    private readonly IConfiguration _configuration;
    private readonly StorageProviderService _storageProviderService;
    private readonly ShareSmallBizUserContext _context;
    private readonly ILogger<MediaService> _logger;
    private readonly string _mediaRootPath;

    public MediaService(
        IWebHostEnvironment environment,
        IConfiguration configuration,
        StorageProviderService storageProviderService,
        ShareSmallBizUserContext context,
        ILogger<MediaService> logger)
    {
        _environment = environment;
        _configuration = configuration;
        _storageProviderService = storageProviderService;
        _context = context;
        _logger = logger;

        // Get media path from configuration, or use default
        _mediaRootPath = _configuration["MediaStorage:RootPath"] ?? Path.Combine("c:", "websites", "sharesmallbiz", "media");

        // Ensure media directory exists
        if (!Directory.Exists(_mediaRootPath))
        {
            Directory.CreateDirectory(_mediaRootPath);
        }
    }

    /// <summary>
    /// Gets all media for a specific user
    /// </summary>
    public async Task<IEnumerable<Media>> GetUserMediaAsync(string userId)
    {
        return await _context.Media
            .Where(m => m.UserId == userId)
            .OrderByDescending(m => m.CreatedDate)
            .ToListAsync();
    }

    /// <summary>
    /// Gets a specific media item by ID
    /// </summary>
    public async Task<Media?> GetMediaByIdAsync(int id)
    {
        return await _context.Media
            .Include(m => m.User)
            .FirstOrDefaultAsync(m => m.Id == id);
    }

    /// <summary>
    /// Gets a specific media item by ID and user ID (for security)
    /// </summary>
    public async Task<Media?> GetUserMediaByIdAsync(int id, string userId)
    {
        return await _context.Media
            .Include(m => m.User)
            .FirstOrDefaultAsync(m => m.Id == id && m.UserId == userId);
    }

    /// <summary>
    /// Converts a user's profile picture (byte array) to a Media entity
    /// </summary>
    public async Task<Media?> ConvertProfilePictureToMediaAsync(ShareSmallBizUser user)
    {
        if (user.ProfilePicture == null || user.ProfilePicture.Length == 0)
        {
            return null;
        }

        try
        {
            // Create a unique filename
            string fileName = $"profile_{user.Id}_{DateTime.UtcNow.Ticks}.jpg";
            string filePath = Path.Combine(_mediaRootPath, "profiles", fileName);

            // Ensure the profiles directory exists
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));

            // Save the image to disk
            await File.WriteAllBytesAsync(filePath, user.ProfilePicture);

            // Create the media entity
            var media = new Media
            {
                FileName = fileName,
                MediaType = MediaType.Image,
                StorageProvider = StorageProviderNames.LocalStorage,
                Url = filePath,
                ContentType = "image/jpeg",
                FileSize = user.ProfilePicture.Length,
                Description = $"{user.DisplayName}'s profile picture",
                StorageMetadata = "{\"type\":\"profile\"}",
                UserId = user.Id
            };

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
    /// Creates a thumbnail for an image
    /// </summary>
    public async Task<string> CreateThumbnailAsync(Media media, int width = 200, int height = 200)
    {
        if (media.MediaType != MediaType.Image)
        {
            return string.Empty;
        }

        try
        {
            // Generate thumbnail filename
            string thumbFileName = $"thumb_{width}x{height}_{Path.GetFileName(media.Url)}";
            string thumbDirectory = Path.Combine(_mediaRootPath, "thumbnails");
            string thumbPath = Path.Combine(thumbDirectory, thumbFileName);

            // Ensure the thumbnails directory exists
            Directory.CreateDirectory(thumbDirectory);

            // Check if thumbnail already exists
            if (File.Exists(thumbPath))
            {
                return thumbPath;
            }

            // Load the original image
            using (var originalImage = Image.FromFile(media.Url))
            {
                // Calculate dimensions while maintaining aspect ratio
                int newWidth, newHeight;
                if (originalImage.Width > originalImage.Height)
                {
                    newWidth = width;
                    newHeight = (int)(originalImage.Height * ((float)width / originalImage.Width));
                }
                else
                {
                    newHeight = height;
                    newWidth = (int)(originalImage.Width * ((float)height / originalImage.Height));
                }

                // Create the thumbnail
                using (var thumbnail = new Bitmap(newWidth, newHeight))
                {
                    using (var graphics = Graphics.FromImage(thumbnail))
                    {
                        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        graphics.SmoothingMode = SmoothingMode.HighQuality;
                        graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                        graphics.CompositingQuality = CompositingQuality.HighQuality;
                        graphics.DrawImage(originalImage, 0, 0, newWidth, newHeight);
                    }

                    // Save the thumbnail
                    thumbnail.Save(thumbPath, GetImageFormat(Path.GetExtension(media.Url)));
                }
            }

            return thumbPath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create thumbnail for media {MediaId}", media.Id);
            return string.Empty;
        }
    }

    /// <summary>
    /// Gets the appropriate ImageFormat for a file extension
    /// </summary>
    private ImageFormat GetImageFormat(string extension)
    {
        extension = extension.ToLowerInvariant();
        return extension switch
        {
            ".jpg" or ".jpeg" => ImageFormat.Jpeg,
            ".png" => ImageFormat.Png,
            ".gif" => ImageFormat.Gif,
            ".bmp" => ImageFormat.Bmp,
            _ => ImageFormat.Jpeg  // Default to JPEG
        };
    }

    /// <summary>
    /// Gets the URL for a media item
    /// </summary>
    public async Task<string> GetMediaUrlAsync(Media media)
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
    public async Task<string> GetThumbnailUrlAsync(Media media)
    {
        if (media.MediaType != MediaType.Image)
        {
            // Return appropriate icon URL based on media type
            return $"/images/{media.MediaType.ToString().ToLower()}-icon.png";
        }

        return $"/Media/Thumbnail/{media.Id}";
    }

    /// <summary>
    /// Deletes a media item
    /// </summary>
    public async Task DeleteMediaAsync(Media media)
    {
        try
        {
            // Remove any artifacts of the media
            await _storageProviderService.DeleteFileAsync(media, _mediaRootPath);

            // Remove from database
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
    /// Gets a file stream for a media item
    /// </summary>
    public async Task<Stream> GetFileStreamAsync(Media media)
    {
        if (media.StorageProvider == StorageProviderNames.LocalStorage)
        {
            return new FileStream(media.Url, FileMode.Open, FileAccess.Read);
        }

        // For other storage providers, use the storage provider service
        return await _storageProviderService.GetFileStreamAsync(media);
    }

    /// <summary>
    /// Gets a file stream for a media thumbnail
    /// </summary>
    public async Task<Stream> GetThumbnailStreamAsync(Media media, int width = 200, int height = 200)
    {
        if (media.MediaType != MediaType.Image)
        {
            // Return default icon for non-image media
            string iconPath = Path.Combine(_environment.WebRootPath, "images", $"{media.MediaType.ToString().ToLower()}-icon.png");
            if (!File.Exists(iconPath))
            {
                iconPath = Path.Combine(_environment.WebRootPath, "images", "placeholder.png");
            }

            return new FileStream(iconPath, FileMode.Open, FileAccess.Read);
        }

        // Create thumbnail if it doesn't exist
        string thumbPath = await CreateThumbnailAsync(media, width, height);
        if (string.IsNullOrEmpty(thumbPath) || !File.Exists(thumbPath))
        {
            // If thumbnail creation failed, return the original image
            return await GetFileStreamAsync(media);
        }

        return new FileStream(thumbPath, FileMode.Open, FileAccess.Read);
    }
}