using Microsoft.AspNetCore.Identity;
using ShareSmallBiz.Portal.Areas.Media.Services;
using ShareSmallBiz.Portal.Data.Entities;
using ShareSmallBiz.Portal.Data.Enums;
using ShareSmallBiz.Portal.Infrastructure.Services;
using System.Security.Claims;

namespace ShareSmallBiz.Portal.Controllers.api;

/// <summary>
/// REST API for managing the authenticated user's profile picture.
/// </summary>
[Route("api/media/profile")]
[RequestSizeLimit(10 * 1024 * 1024)]
[RequestFormLimits(MultipartBodyLengthLimit = 10 * 1024 * 1024)]
public class MediaProfileApiController(
    MediaService mediaService,
    FileUploadService fileUploadService,
    UnsplashService unsplashService,
    UserManager<ShareSmallBizUser> userManager,
    ProfileImageService profileImageService,
    ILogger<MediaProfileApiController> logger) : ApiControllerBase
{
    /// <summary>GET /api/media/profile — get current user's profile picture info</summary>
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var user = await userManager.FindByIdAsync(userId);
        if (user is null) return Unauthorized();

        return Ok(new
        {
            HasProfilePicture = !string.IsNullOrEmpty(user.ProfilePictureUrl),
            ProfilePictureUrl = user.ProfilePictureUrl
        });
    }

    /// <summary>
    /// POST /api/media/profile/upload — upload a profile picture (multipart/form-data).
    /// Form field: file
    /// </summary>
    [HttpPost("upload")]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { Message = "No file provided." });

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var user = await userManager.FindByIdAsync(userId);
        if (user is null) return Unauthorized();

        try
        {
            var media = await fileUploadService.UploadFileAsync(
                file,
                userId,
                StorageProviderNames.LocalStorage,
                "Profile picture",
                "Personal");

            if (media is null)
                return StatusCode(StatusCodes.Status500InternalServerError, new { Message = "Upload failed." });

            media.StorageMetadata = $"{{\"type\":\"profile\",\"userId\":\"{userId}\"}}";
            await mediaService.UpdateMediaAsync(media);

            user.ProfilePictureUrl = $"/Media/{media.Id}";
            await userManager.UpdateAsync(user);

            logger.LogInformation("User {UserId} updated profile picture via file upload", userId);
            return Ok(new { ProfilePictureUrl = user.ProfilePictureUrl, MediaId = media.Id });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }

    /// <summary>POST /api/media/profile/external — set an external URL as profile picture</summary>
    [HttpPost("external")]
    public async Task<IActionResult> SetExternal([FromBody] SetExternalProfileRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Url))
            return BadRequest(new { Message = "Url is required." });

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var user = await userManager.FindByIdAsync(userId);
        if (user is null) return Unauthorized();

        var fileName = $"profile_{user.Id}_{DateTime.UtcNow.Ticks}.jpg";
        var media = await fileUploadService.CreateExternalLinkAsync(
            request.Url,
            fileName,
            MediaType.Image,
            userId,
            "External Profile",
            request.Description ?? "Profile picture",
            $"{{\"type\":\"profile\",\"userId\":\"{userId}\"}}");

        if (media is null)
            return StatusCode(StatusCodes.Status500InternalServerError, new { Message = "Failed to set profile picture." });

        user.ProfilePictureUrl = $"/Media/{media.Id}";
        await userManager.UpdateAsync(user);

        return Ok(new { ProfilePictureUrl = user.ProfilePictureUrl, MediaId = media.Id });
    }

    /// <summary>POST /api/media/profile/unsplash — set an Unsplash photo as profile picture</summary>
    [HttpPost("unsplash")]
    public async Task<IActionResult> SetUnsplash([FromBody] SetUnsplashProfileRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.PhotoId))
            return BadRequest(new { Message = "PhotoId is required." });

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var user = await userManager.FindByIdAsync(userId);
        if (user is null) return Unauthorized();

        var photo = await unsplashService.GetPhotoAsync(request.PhotoId);
        if (photo is null)
            return NotFound(new { Message = "Unsplash photo not found." });

        var media = await unsplashService.CreateUnsplashMediaAsync(photo, userId);
        if (media is null)
            return StatusCode(StatusCodes.Status500InternalServerError, new { Message = "Failed to save Unsplash photo." });

        var metadata = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(media.StorageMetadata) ?? [];
        metadata["type"] = "profile";
        metadata["userId"] = userId;
        media.StorageMetadata = System.Text.Json.JsonSerializer.Serialize(metadata);
        await mediaService.UpdateMediaAsync(media);

        user.ProfilePictureUrl = $"/Media/{media.Id}";
        await userManager.UpdateAsync(user);

        return Ok(new { ProfilePictureUrl = user.ProfilePictureUrl, MediaId = media.Id });
    }

    /// <summary>DELETE /api/media/profile — remove profile picture</summary>
    [HttpDelete]
    public async Task<IActionResult> Remove()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var user = await userManager.FindByIdAsync(userId);
        if (user is null) return Unauthorized();

        if (!string.IsNullOrEmpty(user.ProfilePictureUrl))
        {
            var parts = user.ProfilePictureUrl.Split('/');
            if (int.TryParse(parts[^1], out var mediaId))
            {
                var media = await mediaService.GetUserMediaByIdAsync(mediaId, userId);
                if (media is not null)
                    await mediaService.DeleteMediaAsync(media);
            }
        }

        user.ProfilePictureUrl = null;
        await userManager.UpdateAsync(user);

        logger.LogInformation("User {UserId} removed their profile picture", userId);
        return NoContent();
    }
}

public class SetExternalProfileRequest
{
    public string Url { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class SetUnsplashProfileRequest
{
    public string PhotoId { get; set; } = string.Empty;
}
