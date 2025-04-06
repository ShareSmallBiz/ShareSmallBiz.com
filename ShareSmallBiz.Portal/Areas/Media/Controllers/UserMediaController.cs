using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ShareSmallBiz.Portal.Areas.Media.Models;
using ShareSmallBiz.Portal.Areas.Media.Services;
using ShareSmallBiz.Portal.Data.Entities;
using ShareSmallBiz.Portal.Data.Enums;
using System.Security.Claims;

namespace ShareSmallBiz.Portal.Areas.Media.Controllers;

[Area("MediaEntity")]
[Authorize]
[Route("MediaEntity/User")]
public class UserMediaController : Controller
{
    private readonly MediaService _mediaService;
    private readonly MediaFactoryService _mediaFactoryService;
    private readonly FileUploadService _fileUploadService;
    private readonly UnsplashService _unsplashService;
    private readonly UserManager<ShareSmallBizUser> _userManager;
    private readonly ILogger<UserMediaController> _logger;

    public UserMediaController(
        MediaService mediaService,
        MediaFactoryService mediaFactoryService,
        FileUploadService fileUploadService,
        UnsplashService unsplashService,
        UserManager<ShareSmallBizUser> userManager,
        ILogger<UserMediaController> logger)
    {
        _mediaService = mediaService;
        _mediaFactoryService = mediaFactoryService;
        _fileUploadService = fileUploadService;
        _unsplashService = unsplashService;
        _userManager = userManager;
        _logger = logger;
    }

    // GET: /MediaEntity/User
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var userMedia = await _mediaService.GetUserMediaAsync(userId);

        var viewModel = new UserMediaViewModel
        {
            Media = userMedia
        };

        return View(viewModel);
    }

    // GET: /MediaEntity/User/Profile
    [HttpGet("Profile")]
    public async Task<IActionResult> Profile()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var user = await _userManager.FindByIdAsync(userId);

        if (user == null)
        {
            return NotFound();
        }

        var viewModel = new ProfileMediaViewModel
        {
            HasProfilePicture = !string.IsNullOrEmpty(user.ProfilePictureUrl),
            ProfilePictureUrl = user.ProfilePictureUrl,
        };

        return View(viewModel);
    }

    // POST: /MediaEntity/User/UploadProfile
    [HttpPost("UploadProfile")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadProfile(IFormFile profilePicture)
    {
        if (profilePicture == null || profilePicture.Length == 0)
        {
            ModelState.AddModelError(string.Empty, "Please select a file to upload.");
            return RedirectToAction(nameof(Profile));
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var user = await _userManager.FindByIdAsync(userId);

        if (user == null)
        {
            return NotFound();
        }

        try
        {
            // Create media directly using file upload service
            var media = await _fileUploadService.UploadFileAsync(
                profilePicture,
                userId,
                StorageProviderNames.LocalStorage,
                "Profile picture",
                "Personal"
            );

            if (media != null)
            {
                // Update user's profile picture URL
                user.ProfilePictureUrl = $"/MediaEntity/{media.Id}";

                // Update metadata for the media to indicate it's a profile picture
                media.StorageMetadata = $"{{\"type\":\"profile\",\"userId\":\"{userId}\"}}";
                await _mediaService.UpdateMediaAsync(media);

                // Save user changes
                await _userManager.UpdateAsync(user);

                TempData["SuccessMessage"] = "Profile picture updated successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to update profile picture.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading profile picture for user {CreatedID}", userId);
            TempData["ErrorMessage"] = $"Error uploading profile picture: {ex.Message}";
        }

        return RedirectToAction(nameof(Profile));
    }

    // POST: /MediaEntity/User/UseExternalProfile
    [HttpPost("UseExternalProfile")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UseExternalProfile(string externalUrl, string description = "Profile picture")
    {
        if (string.IsNullOrEmpty(externalUrl))
        {
            ModelState.AddModelError(string.Empty, "External URL is required.");
            return RedirectToAction(nameof(Profile));
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var user = await _userManager.FindByIdAsync(userId);

        if (user == null)
        {
            return NotFound();
        }

        try
        {
            // Create external media
            var fileName = $"profile_{user.Id}_{DateTime.UtcNow.Ticks}.jpg";
            var media = await _fileUploadService.CreateExternalLinkAsync(
                externalUrl,
                fileName,
                MediaType.Image,
                userId,
                "External Profile", // Attribution
                description, // Description 
                $"{{\"type\":\"profile\",\"userId\":\"{userId}\"}}"
            );

            if (media != null)
            {
                // Update user's profile picture URL
                user.ProfilePictureUrl = $"/MediaEntity/{media.Id}";

                // Save user changes
                await _userManager.UpdateAsync(user);

                TempData["SuccessMessage"] = "Profile picture updated successfully using external image.";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to update profile picture.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting external profile picture for user {CreatedID}", userId);
            TempData["ErrorMessage"] = $"Error setting profile picture: {ex.Message}";
        }

        return RedirectToAction(nameof(Profile));
    }

    // POST: /MediaEntity/User/UseUnsplashProfile
    [HttpPost("UseUnsplashProfile")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UseUnsplashProfile(string photoId)
    {
        if (string.IsNullOrEmpty(photoId))
        {
            ModelState.AddModelError(string.Empty, "Unsplash photo ID is required.");
            return RedirectToAction(nameof(Profile));
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var user = await _userManager.FindByIdAsync(userId);

        if (user == null)
        {
            return NotFound();
        }

        try
        {
            // Fetch photo details from Unsplash
            var photo = await _unsplashService.GetPhotoAsync(photoId);
            if (photo == null)
            {
                TempData["ErrorMessage"] = "Failed to retrieve Unsplash photo.";
                return RedirectToAction(nameof(Profile));
            }

            // Create media for the Unsplash photo
            var media = await _unsplashService.CreateUnsplashMediaAsync(photo, userId);

            // Update the media metadata to indicate it's a profile picture
            if (media != null)
            {
                // Update existing metadata to add profile indicator
                var metadataObj = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(media.StorageMetadata);
                metadataObj["type"] = "profile";
                metadataObj["userId"] = userId;
                media.StorageMetadata = System.Text.Json.JsonSerializer.Serialize(metadataObj);

                // Update the media
                await _mediaService.UpdateMediaAsync(media);

                // Update user's profile picture URL
                user.ProfilePictureUrl = $"/MediaEntity/{media.Id}";

                // Save user changes
                await _userManager.UpdateAsync(user);

                TempData["SuccessMessage"] = "Profile picture updated successfully using Unsplash image.";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to update profile picture.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting Unsplash profile picture for user {CreatedID}", userId);
            TempData["ErrorMessage"] = $"Error setting profile picture: {ex.Message}";
        }

        return RedirectToAction(nameof(Profile));
    }

    // POST: /MediaEntity/User/RemoveProfile
    [HttpPost("RemoveProfile")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveProfile()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var user = await _userManager.FindByIdAsync(userId);

        if (user == null)
        {
            return NotFound();
        }

        try
        {
            // If user has a ProfilePictureUrl, extract the media ID to delete the associated media
            if (!string.IsNullOrEmpty(user.ProfilePictureUrl))
            {
                var urlParts = user.ProfilePictureUrl.Split('/');
                if (urlParts.Length > 0 && int.TryParse(urlParts[urlParts.Length - 1], out int mediaId))
                {
                    // Try to find and delete the media
                    var media = await _mediaService.GetUserMediaByIdAsync(mediaId, userId);
                    if (media != null)
                    {
                        await _mediaService.DeleteMediaAsync(media);
                    }
                }
            }

            // Remove profile picture URL
            user.ProfilePictureUrl = null;
            await _userManager.UpdateAsync(user);

            // Find and remove any associated media with profile metadata
            var profileMediaItems = await _mediaService.SearchMediaAsync(userId, "profile");
            foreach (var media in profileMediaItems.Where(m => m.StorageMetadata.Contains("profile")))
            {
                await _mediaService.DeleteMediaAsync(media);
            }

            TempData["SuccessMessage"] = "Profile picture removed successfully.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing profile picture for user {CreatedID}", userId);
            TempData["ErrorMessage"] = $"Error removing profile picture: {ex.Message}";
        }

        return RedirectToAction(nameof(Profile));
    }
}