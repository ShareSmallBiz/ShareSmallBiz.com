using Microsoft.AspNetCore.Mvc;
using ShareSmallBiz.Portal.Data;
using ShareSmallBiz.Portal.Services;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace ShareSmallBiz.Portal.Controllers
{
    [Route("[controller]")]
    public class MediaController : Controller
    {
        private readonly ShareSmallBizUserContext _context;
        private readonly StorageProviderService _storageProviderService;
        private readonly ILogger<MediaController> _logger;

        public MediaController(
            ShareSmallBizUserContext context,
            StorageProviderService storageProviderService,
            ILogger<MediaController> logger)
        {
            _context = context;
            _storageProviderService = storageProviderService;
            _logger = logger;
        }

        // GET: /Media/{id}
        [HttpGet("{id:int}")]
        [ResponseCache(Duration = 86400)] // Cache for 24 hours
        public async Task<IActionResult> GetMedia(int id)
        {
            try
            {
                var media = await _context.Media
                    .FirstOrDefaultAsync(m => m.Id == id);

                // Check if media is restricted and user is authorized
                // Uncomment and customize this check based on your access control requirements
                /*
                if (media.IsRestricted)
                {
                    // If media is restricted, check if user is authenticated
                    if (!User.Identity.IsAuthenticated)
                    {
                        return Unauthorized();
                    }
                    
                    // Additional checks if needed (e.g., ownership, role-based access)
                    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    if (media.UserId != userId && !User.IsInRole("Admin"))
                    {
                        return Forbid();
                    }
                }
                */

                if (media == null)
                {
                    _logger.LogWarning("Media with ID {MediaId} not found", id);
                    return NotFound();
                }

                // For external links, redirect to the external URL
                if (media.StorageProvider == StorageProviderNames.External)
                {
                    return Redirect(media.Url);
                }

                // For local files, return the file stream
                var stream = await _storageProviderService.GetFileStreamAsync(media);

                // Determine if this content should be displayed inline or as an attachment
                var contentDisposition = media.MediaType == MediaType.Image ||
                                         media.MediaType == MediaType.Video ||
                                         media.MediaType == MediaType.Audio
                    ? "inline"
                    : "attachment; filename=" + media.FileName;

                Response.Headers.Add("Content-Disposition", contentDisposition);

                // Return the file with the appropriate content type
                return File(stream, media.ContentType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving media with ID {MediaId}", id);
                return StatusCode(500, "Error retrieving media");
            }
        }

        // GET: /Media/Thumbnail/{id}
        [HttpGet("thumbnail/{id:int}")]
        [ResponseCache(Duration = 86400)] // Cache for 24 hours
        public async Task<IActionResult> GetThumbnail(int id)
        {
            try
            {
                var media = await _context.Media
                    .FirstOrDefaultAsync(m => m.Id == id);

                if (media == null)
                {
                    return NotFound();
                }

                // For non-image media, return a type-specific placeholder or icon
                if (media.MediaType != MediaType.Image)
                {
                    // This could be enhanced to return different placeholders based on media type
                    var placeholderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", $"{media.MediaType.ToString().ToLower()}-icon.png");
                    if (System.IO.File.Exists(placeholderPath))
                    {
                        return PhysicalFile(placeholderPath, "image/png");
                    }

                    // Fall back to default placeholder
                    placeholderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "placeholder.png");
                    if (System.IO.File.Exists(placeholderPath))
                    {
                        return PhysicalFile(placeholderPath, "image/png");
                    }

                    // If no placeholder exists, proceed to return the actual media
                }

                // For external links to images, redirect
                if (media.StorageProvider == StorageProviderNames.External && media.MediaType == MediaType.Image)
                {
                    return Redirect(media.Url);
                }

                // For local files, return the file stream
                // Note: In a production environment, you would generate and cache actual thumbnails
                // instead of serving the full image
                var stream = await _storageProviderService.GetFileStreamAsync(media);
                return File(stream, media.ContentType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving thumbnail for media with ID {MediaId}", id);
                return StatusCode(500, "Error retrieving thumbnail");
            }
        }
    }
}