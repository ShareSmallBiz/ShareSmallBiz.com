using Microsoft.AspNetCore.Identity;
using ShareSmallBiz.Portal.Areas.Media.Models;
using ShareSmallBiz.Portal.Areas.Media.Services;
using ShareSmallBiz.Portal.Data.Entities;
using ShareSmallBiz.Portal.Data.Enums;
using System.Security.Claims;

namespace ShareSmallBiz.Portal.Controllers.api;

/// <summary>
/// REST API for the authenticated user's media library.
/// Mirrors the MVC Media/Library area but returns JSON for React clients.
/// </summary>
[Route("api/media")]
[RequestSizeLimit(50 * 1024 * 1024)] // 50 MB
[RequestFormLimits(MultipartBodyLengthLimit = 50 * 1024 * 1024)]
public class MediaLibraryApiController(
    MediaService mediaService,
    FileUploadService fileUploadService,
    UserManager<ShareSmallBizUser> userManager,
    ILogger<MediaLibraryApiController> logger) : ApiControllerBase
{
    /// <summary>GET /api/media — list authenticated user's media library</summary>
    [HttpGet]
    public async Task<IActionResult> GetLibrary(
        [FromQuery] string? search,
        [FromQuery] MediaType? mediaType,
        [FromQuery] StorageProviderNames? storageProvider)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        IEnumerable<MediaModel> items = string.IsNullOrWhiteSpace(search)
            ? await mediaService.GetUserMediaAsync(userId)
            : await mediaService.SearchMediaAsync(userId, search);

        if (mediaType.HasValue)
            items = items.Where(m => m.MediaType == mediaType.Value);

        if (storageProvider.HasValue)
            items = items.Where(m => m.StorageProvider == storageProvider.Value);

        return Ok(items.OrderByDescending(m => m.CreatedDate).ToList());
    }

    /// <summary>GET /api/media/{id} — get single media item (must belong to caller)</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var item = await mediaService.GetUserMediaByIdAsync(id, userId);
        return item is null ? NotFound() : Ok(item);
    }

    /// <summary>
    /// POST /api/media/upload — upload a file (multipart/form-data).
    /// Form fields: file (required), description, attribution
    /// </summary>
    [HttpPost("upload")]
    public async Task<IActionResult> Upload(
        IFormFile file,
        [FromForm] string? description,
        [FromForm] string? attribution)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { Message = "No file provided." });

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        try
        {
            var media = await fileUploadService.UploadFileAsync(
                file,
                userId,
                StorageProviderNames.LocalStorage,
                description ?? string.Empty,
                attribution ?? string.Empty);

            if (media is null)
                return StatusCode(StatusCodes.Status500InternalServerError, new { Message = "Upload failed." });

            logger.LogInformation("User {UserId} uploaded media {MediaId}", userId, media.Id);
            return CreatedAtAction(nameof(GetById), new { id = media.Id }, media);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }

    /// <summary>PUT /api/media/{id} — update media metadata (description, attribution)</summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateMediaRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var item = await mediaService.GetUserMediaByIdAsync(id, userId);
        if (item is null) return NotFound();

        item.Description = request.Description ?? item.Description;
        item.Attribution = request.Attribution ?? item.Attribution;
        item.FileName = request.FileName ?? item.FileName;

        var ok = await mediaService.UpdateMediaAsync(item);
        return ok ? NoContent() : StatusCode(StatusCodes.Status500InternalServerError);
    }

    /// <summary>DELETE /api/media/{id} — delete a media item</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var item = await mediaService.GetUserMediaByIdAsync(id, userId);
        if (item is null) return NotFound();

        await mediaService.DeleteMediaAsync(item);
        logger.LogInformation("User {UserId} deleted media {MediaId}", userId, id);
        return NoContent();
    }

    /// <summary>POST /api/media/external — add an external URL as a media item</summary>
    [HttpPost("external")]
    public async Task<IActionResult> AddExternal([FromBody] AddExternalMediaRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Url))
            return BadRequest(new { Message = "Url is required." });

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var user = await userManager.FindByIdAsync(userId);
        if (user is null) return Unauthorized();

        var fileName = $"external_{user.Id}_{DateTime.UtcNow.Ticks}.jpg";

        var media = await fileUploadService.CreateExternalLinkAsync(
            request.Url,
            fileName,
            request.MediaType ?? MediaType.Image,
            userId,
            request.Attribution ?? "External",
            request.Description ?? string.Empty,
            string.Empty);

        if (media is null)
            return StatusCode(StatusCodes.Status500InternalServerError, new { Message = "Failed to add external media." });

        return CreatedAtAction(nameof(GetById), new { id = media.Id }, media);
    }
}

public class UpdateMediaRequest
{
    public string? FileName { get; set; }
    public string? Description { get; set; }
    public string? Attribution { get; set; }
}

public class AddExternalMediaRequest
{
    public string Url { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Attribution { get; set; }
    public MediaType? MediaType { get; set; }
}
