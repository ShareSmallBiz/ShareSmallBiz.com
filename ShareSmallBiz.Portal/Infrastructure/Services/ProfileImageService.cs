using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace ShareSmallBiz.Portal.Infrastructure.Services;

/// <summary>
/// Service for optimizing profile images and creating different sized versions
/// </summary>
public class ProfileImageService
{
    private readonly ILogger<ProfileImageService> _logger;
    private readonly IWebHostEnvironment _environment;
    
    public ProfileImageService(
        ILogger<ProfileImageService> logger,
        IWebHostEnvironment environment)
    {
        _logger = logger;
        _environment = environment;
    }
    
    /// <summary>
    /// Processes a profile image, optimizing it and creating different sized versions
    /// </summary>
    /// <param name="imageFile">The uploaded image file</param>
    /// <param name="userId">The user ID for file naming</param>
    /// <returns>A tuple with the main image URL and a dictionary of different sized versions</returns>
    public async Task<(string MainImageUrl, Dictionary<string, string> Versions)> ProcessProfileImageAsync(IFormFile imageFile, string userId)
    {
        try
        {
            if (imageFile == null || imageFile.Length == 0)
            {
                _logger.LogWarning("No image file provided for processing");
                return (null, new Dictionary<string, string>());
            }
            
            // Create directory if it doesn't exist
            var profileImagesDirectory = Path.Combine(_environment.WebRootPath, "images", "profiles");
            if (!Directory.Exists(profileImagesDirectory))
            {
                Directory.CreateDirectory(profileImagesDirectory);
            }
            
            // Create versions directory
            var versionsDirectory = Path.Combine(profileImagesDirectory, "versions");
            if (!Directory.Exists(versionsDirectory))
            {
                Directory.CreateDirectory(versionsDirectory);
            }
            
            // Sanitize user ID for filenames
            var safeUserId = userId.Replace("-", "").Substring(0, Math.Min(userId.Length, 10));
            
            // Create a timestamp to ensure uniqueness and prevent caching issues
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            
            // Main image filename
            var mainFilename = $"{safeUserId}_{timestamp}.jpg";
            var mainImagePath = Path.Combine(profileImagesDirectory, mainFilename);
            
            // Process and save the main image (400x400 pixels)
            using (var image = await Image.LoadAsync(imageFile.OpenReadStream()))
            {
                // Determine if we need to crop to square
                if (image.Width != image.Height)
                {
                    var size = Math.Min(image.Width, image.Height);
                    var x = (image.Width - size) / 2;
                    var y = (image.Height - size) / 2;
                    
                    // Crop to square from center
                    image.Mutate(i => i.Crop(new Rectangle(x, y, size, size)));
                }
                
                // Resize to 400x400 for main profile image
                image.Mutate(i => i.Resize(new ResizeOptions
                {
                    Size = new Size(400, 400),
                    Mode = ResizeMode.Max
                }));
                
                // Save with optimization
                await image.SaveAsJpegAsync(mainImagePath, new JpegEncoder
                {
                    Quality = 80 // Good balance of quality and size
                });
            }
            
            // Create and save different sized versions
            var versions = new Dictionary<string, string>
            {
                { "small", await CreateVersionAsync(mainImagePath, versionsDirectory, safeUserId, timestamp, 50) },
                { "medium", await CreateVersionAsync(mainImagePath, versionsDirectory, safeUserId, timestamp, 150) },
                { "large", await CreateVersionAsync(mainImagePath, versionsDirectory, safeUserId, timestamp, 300) }
            };
            
            // Generate URLs
            var mainImageUrl = $"/images/profiles/{mainFilename}";
            var versionUrls = versions.ToDictionary(
                kv => kv.Key,
                kv => $"/images/profiles/versions/{kv.Value}"
            );
            
            _logger.LogInformation("Successfully processed profile image for user {UserId}", userId);
            
            return (mainImageUrl, versionUrls);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing profile image for user {UserId}", userId);
            return (null, new Dictionary<string, string>());
        }
    }
    
    /// <summary>
    /// Creates a resized version of the profile image
    /// </summary>
    private async Task<string> CreateVersionAsync(string sourcePath, string outputDirectory, string safeUserId, string timestamp, int size)
    {
        var filename = $"{safeUserId}_{timestamp}_{size}.jpg";
        var outputPath = Path.Combine(outputDirectory, filename);
        
        using (var image = await Image.LoadAsync(sourcePath))
        {
            image.Mutate(i => i.Resize(new ResizeOptions
            {
                Size = new Size(size, size),
                Mode = ResizeMode.Max
            }));
            
            await image.SaveAsJpegAsync(outputPath, new JpegEncoder
            {
                Quality = size < 100 ? 70 : 75 // Lower quality for smaller images is acceptable
            });
        }
        
        return filename;
    }
}