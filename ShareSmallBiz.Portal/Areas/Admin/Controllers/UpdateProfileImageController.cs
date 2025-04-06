using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ShareSmallBiz.Portal.Areas.Media.Services;
using ShareSmallBiz.Portal.Data;
using ShareSmallBiz.Portal.Data.Enums;
using ShareSmallBiz.Portal.Infrastructure.Services;
using System;
using System.Linq;
using System.Security.Claims;

namespace ShareSmallBiz.Portal.Areas.Admin.Controllers
{
    [Authorize(Roles = "Admin")]
    [Area("Admin")]
    public class UpdateProfileImageController : Controller
    {
        private readonly ILogger<UpdateProfileImageController> _logger;
        private readonly ShareSmallBizUserContext _context;
        private readonly ShareSmallBizUserManager _userManager;
        private readonly FileUploadService _fileUploadService;
        private readonly MediaService _mediaService;

        public UpdateProfileImageController(
            ILogger<UpdateProfileImageController> logger,
            ShareSmallBizUserContext context,
            ShareSmallBizUserManager userManager,
            FileUploadService fileUploadService,
            MediaService mediaService)
        {
            _logger = logger;
            _context = context;
            _userManager = userManager;
            _fileUploadService = fileUploadService;
            _mediaService = mediaService;
        }

        public async Task<IActionResult> Index()
        {
            _logger.LogInformation("Profile Image Update page accessed at {Time}", DateTime.UtcNow);

            // Get all users for dropdown
            var users = await _userManager.Users
                .OrderBy(u => u.UserName)
                .Select(u => new
                {
                    Id = u.Id,
                    DisplayName = u.DisplayName ?? $"{u.FirstName} {u.LastName}",
                    Email = u.Email
                })
                .ToListAsync();

            ViewBag.Users = users.Select(u => new SelectListItem
            {
                Value = u.Id,
                Text = $"{u.DisplayName} ({u.Email})"
            }).ToList();

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> SelectUser(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("No user ID provided");
                TempData["ErrorMessage"] = "Please select a user";
                return RedirectToAction("Index");
            }

            _logger.LogInformation("Loading user {CreatedID} for profile image update", userId);

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("User not found: {CreatedID}", userId);
                TempData["ErrorMessage"] = "User not found";
                return RedirectToAction("Index");
            }

            var model = new ProfileImageUpdateModel
            {
                UserId = user.Id,
                UserName = user.UserName,
                DisplayName = user.DisplayName ?? $"{user.FirstName} {user.LastName}",
                Email = user.Email,
                HasProfilePicture = !string.IsNullOrEmpty(user.ProfilePictureUrl)
            };

            if (!string.IsNullOrEmpty(user.ProfilePictureUrl))
            {
                model.CurrentProfileImageType = "url";
                model.ProfilePicturePreview = user.ProfilePictureUrl;
                model.ProfilePictureUrl = user.ProfilePictureUrl;
            }

            _logger.LogInformation("User {CreatedID} loaded successfully. Has profile picture: {HasProfilePicture}",
                userId, model.HasProfilePicture);

            return View(model);
        }

        [HttpPost]
        [RequestSizeLimit(10 * 1024 * 1024)] // 10 MB
        [RequestFormLimits(MultipartBodyLengthLimit = 10 * 1024 * 1024)]
        public async Task<IActionResult> UpdateImage(ProfileImageUpdateModel model)
        {
            _logger.LogInformation("UpdateImage POST started for user {CreatedID}", model.UserId);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model state");
                return View("SelectUser", model);
            }

            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null)
            {
                _logger.LogWarning("User not found: {CreatedID}", model.UserId);
                TempData["ErrorMessage"] = "User not found";
                return RedirectToAction("Index");
            }

            bool profileUpdated = false;

            try
            {
                // Handle profile picture update based on selected option
                if (model.ProfilePictureOption == "upload" && model.ProfilePictureFile != null)
                {
                    _logger.LogInformation("Processing profile picture upload for user {CreatedID}", model.UserId);

                    if (model.ProfilePictureFile.Length > 2097152) // 2MB
                    {
                        _logger.LogWarning("File too large: {Size} bytes", model.ProfilePictureFile.Length);
                        ModelState.AddModelError("ProfilePictureFile", "The profile picture must be less than 2MB.");
                        return View("SelectUser", model);
                    }

                    // Remove existing profile picture media if it exists
                    if (!string.IsNullOrEmpty(user.ProfilePictureUrl))
                    {
                        await RemoveExistingProfilePictureMedia(user);
                    }

                    // Upload the file as a media entity
                    var media = await _fileUploadService.UploadFileAsync(
                        model.ProfilePictureFile,
                        model.UserId,
                        StorageProviderNames.LocalStorage,
                        "Profile picture",
                        "Personal"
                    );

                    if (media != null)
                    {
                        // Add profile metadata to the media
                        media.StorageMetadata = $"{{\"type\":\"profile\",\"userId\":\"{model.UserId}\"}}";
                        await _mediaService.UpdateMediaAsync(media);

                        // Update user's profile picture URL
                        user.ProfilePictureUrl = $"/MediaEntity/{media.Id}";
                        profileUpdated = true;
                    }
                }
                else if (model.ProfilePictureOption == "url" && !string.IsNullOrEmpty(model.ProfilePictureUrl))
                {
                    _logger.LogInformation("Setting profile picture URL to: {Url}", model.ProfilePictureUrl);

                    // Remove existing profile picture media if it exists
                    if (!string.IsNullOrEmpty(user.ProfilePictureUrl))
                    {
                        await RemoveExistingProfilePictureMedia(user);
                    }

                    user.ProfilePictureUrl = model.ProfilePictureUrl;
                    profileUpdated = true;
                }
                else if (model.ProfilePictureOption == "remove")
                {
                    _logger.LogInformation("Removing profile picture for user {CreatedID}", model.UserId);

                    // Remove existing profile picture media if it exists
                    if (!string.IsNullOrEmpty(user.ProfilePictureUrl))
                    {
                        await RemoveExistingProfilePictureMedia(user);
                    }

                    user.ProfilePictureUrl = null;
                    profileUpdated = true;
                }

                if (profileUpdated)
                {
                    user.LastModified = DateTime.UtcNow;
                    var result = await _userManager.UpdateAsync(user);

                    if (result.Succeeded)
                    {
                        _logger.LogInformation("Profile picture updated successfully for user {CreatedID}", model.UserId);
                        TempData["SuccessMessage"] = "Profile picture updated successfully.";
                        return RedirectToAction("Success", new { userId = model.UserId });
                    }
                    else
                    {
                        _logger.LogWarning("Failed to update user. Errors: {Errors}",
                            string.Join(", ", result.Errors.Select(e => e.Description)));

                        foreach (var error in result.Errors)
                        {
                            ModelState.AddModelError(string.Empty, error.Description);
                        }

                        return View("SelectUser", model);
                    }
                }
                else
                {
                    _logger.LogInformation("No profile picture changes were made");
                    TempData["InfoMessage"] = "No changes were made to the profile picture.";
                    return RedirectToAction("SelectUser", new { userId = model.UserId });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating profile picture for user {CreatedID}", model.UserId);
                ModelState.AddModelError(string.Empty, "An error occurred: " + ex.Message);
                return View("SelectUser", model);
            }
        }

        public async Task<IActionResult> Success(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Index");
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found";
                return RedirectToAction("Index");
            }

            var model = new ProfileImageUpdateModel
            {
                UserId = user.Id,
                UserName = user.UserName,
                DisplayName = user.DisplayName ?? $"{user.FirstName} {user.LastName}",
                Email = user.Email,
                HasProfilePicture = !string.IsNullOrEmpty(user.ProfilePictureUrl)
            };

            if (!string.IsNullOrEmpty(user.ProfilePictureUrl))
            {
                model.CurrentProfileImageType = "url";
                model.ProfilePicturePreview = user.ProfilePictureUrl;
                model.ProfilePictureUrl = user.ProfilePictureUrl;
            }

            return View(model);
        }

        private async Task RemoveExistingProfilePictureMedia(ShareSmallBiz.Portal.Data.Entities.ShareSmallBizUser user)
        {
            if (!string.IsNullOrEmpty(user.ProfilePictureUrl))
            {
                var urlParts = user.ProfilePictureUrl.Split('/');
                if (urlParts.Length > 0 && int.TryParse(urlParts[urlParts.Length - 1], out int mediaId))
                {
                    // Find the media in context
                    var media = await _context.Media.FindAsync(mediaId);
                    if (media != null)
                    {
                        // Delete the media entity 
                        _context.Media.Remove(media);
                        await _context.SaveChangesAsync();
                    }
                }
            }
        }
    }

    public class ProfileImageUpdateModel
    {
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string DisplayName { get; set; }
        public string Email { get; set; }
        public bool HasProfilePicture { get; set; }
        public string CurrentProfileImageType { get; set; } // "stored" or "url"
        public string ProfilePicturePreview { get; set; }
        public string ProfilePictureOption { get; set; } // "keep", "upload", "url", "remove"
        public IFormFile ProfilePictureFile { get; set; }
        public string ProfilePictureUrl { get; set; }
    }
}