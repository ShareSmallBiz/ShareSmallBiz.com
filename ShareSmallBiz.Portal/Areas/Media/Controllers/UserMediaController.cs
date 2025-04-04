using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ShareSmallBiz.Portal.Areas.Media.Models;
using ShareSmallBiz.Portal.Areas.Media.Services;
using ShareSmallBiz.Portal.Data.Entities;
using System.Security.Claims;

namespace ShareSmallBiz.Portal.Areas.Media.Controllers;

[Area("Media")]
[Authorize]
[Route("Media/User")]
public class UserMediaController : Controller
{
    private readonly MediaService _mediaService;
    private readonly UserManager<ShareSmallBizUser> _userManager;
    private readonly ILogger<UserMediaController> _logger;

    public UserMediaController(
        MediaService mediaService,
        UserManager<ShareSmallBizUser> userManager,
        ILogger<UserMediaController> logger)
    {
        _mediaService = mediaService;
        _userManager = userManager;
        _logger = logger;
    }

    // GET: /Media/User
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

    // GET: /Media/User/Profile
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
            HasProfilePicture = user.ProfilePicture != null && user.ProfilePicture.Length > 0,
            ProfilePictureUrl = user.ProfilePictureUrl
        };

        return View(viewModel);
    }

    // POST: /Media/User/UploadProfile
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
            // Read the uploaded file into a byte array
            using var memoryStream = new MemoryStream();
            await profilePicture.CopyToAsync(memoryStream);
            user.ProfilePicture = memoryStream.ToArray();

            // Convert profile picture to media
            var media = await _mediaService.ConvertProfilePictureToMediaAsync(user);

            if (media != null)
            {
                TempData["SuccessMessage"] = "Profile picture updated successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to update profile picture.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading profile picture for user {UserId}", userId);
            TempData["ErrorMessage"] = $"Error uploading profile picture: {ex.Message}";
        }

        return RedirectToAction(nameof(Profile));
    }

    // POST: /Media/User/RemoveProfile
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
            // Remove profile picture
            user.ProfilePicture = null;
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
            _logger.LogError(ex, "Error removing profile picture for user {UserId}", userId);
            TempData["ErrorMessage"] = $"Error removing profile picture: {ex.Message}";
        }

        return RedirectToAction(nameof(Profile));
    }
}
