using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ShareSmallBiz.Portal.Areas.Media.Services;
using ShareSmallBiz.Portal.Data;
using ShareSmallBiz.Portal.Data.Enums;
using System;
using System.Linq;
using System.Security.Claims;

namespace ShareSmallBiz.Portal.Areas.Admin.Controllers
{
    [Authorize(Roles = "Admin")]
    [Area("Admin")]
    public class ProfileImageFixController : Controller
    {
        private readonly ILogger<ProfileImageFixController> _logger;
        private readonly ShareSmallBizUserContext _context;
        private readonly FileUploadService _fileUploadService;

        public ProfileImageFixController(
            ILogger<ProfileImageFixController> logger,
            ShareSmallBizUserContext context,
            FileUploadService fileUploadService)
        {
            _logger = logger;
            _context = context;
            _fileUploadService = fileUploadService;
        }

        // GET: Admin/ProfileImageFix
        public async Task<IActionResult> Index()
        {
            _logger.LogInformation("Profile Image Fix page accessed at {Time}", DateTime.UtcNow);

            // Get all users for dropdown
            var users = await _context.Users
                .OrderBy(u => u.UserName)
                .Select(u => new
                {
                    Id = u.Id,
                    DisplayName = !string.IsNullOrEmpty(u.DisplayName) ? u.DisplayName : $"{u.FirstName} {u.LastName}",
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

        // GET: Admin/ProfileImageFix/Upload/5
        public async Task<IActionResult> Upload(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                _logger.LogWarning("No user ID provided to Upload action");
                TempData["ErrorMessage"] = "Please select a user";
                return RedirectToAction(nameof(Index));
            }

            _logger.LogInformation("Upload page accessed for user {UserId}", id);

            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                _logger.LogWarning("User not found: {UserId}", id);
                TempData["ErrorMessage"] = "User not found";
                return RedirectToAction(nameof(Index));
            }

            var model = new ProfileImageUploadViewModel
            {
                UserId = user.Id,
                UserName = user.UserName,
                DisplayName = !string.IsNullOrEmpty(user.DisplayName) ? user.DisplayName : $"{user.FirstName} {user.LastName}",
                Email = user.Email,
                HasProfilePicture = !string.IsNullOrEmpty(user.ProfilePictureUrl)
            };

            if (!string.IsNullOrEmpty(user.ProfilePictureUrl))
            {
                model.CurrentProfileImageType = "url";
                model.ProfilePicturePreview = user.ProfilePictureUrl;
            }

            _logger.LogInformation("User {UserId} loaded successfully for upload. Has profile picture: {HasProfilePicture}",
                id, model.HasProfilePicture);

            return View(model);
        }

        // POST: Admin/ProfileImageFix/ProcessUpload
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(10 * 1024 * 1024)] // 10 MB
        public async Task<IActionResult> ProcessUpload(ProfileImageUploadViewModel model)
        {
            _logger.LogInformation("ProcessUpload action started for user {UserId}", model.UserId);

            if (string.IsNullOrEmpty(model.UserId))
            {
                _logger.LogWarning("No user ID provided to ProcessUpload action");
                TempData["ErrorMessage"] = "User ID is required";
                return RedirectToAction(nameof(Index));
            }

            if (model.ProfilePictureFile == null || model.ProfilePictureFile.Length == 0)
            {
                _logger.LogWarning("No file was uploaded for user {UserId}", model.UserId);
                TempData["ErrorMessage"] = "Please select an image file";
                return RedirectToAction(nameof(Upload), new { id = model.UserId });
            }

            try
            {
                // Find the user
                var user = await _context.Users.FindAsync(model.UserId);
                if (user == null)
                {
                    _logger.LogWarning("User not found: {UserId}", model.UserId);
                    TempData["ErrorMessage"] = "User not found";
                    return RedirectToAction(nameof(Index));
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
                    // Update user's profile picture URL to point to the media
                    user.ProfilePictureUrl = $"/Media/{media.Id}";
                    user.LastModified = DateTime.UtcNow;

                    // Save changes
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Profile picture updated successfully for user {UserId}", model.UserId);
                    TempData["SuccessMessage"] = "Profile picture updated successfully";
                    return RedirectToAction(nameof(Success), new { id = model.UserId });
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to upload the profile picture";
                    return RedirectToAction(nameof(Upload), new { id = model.UserId });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing image upload for user {UserId}", model.UserId);
                TempData["ErrorMessage"] = "An error occurred: " + ex.Message;
                return RedirectToAction(nameof(Upload), new { id = model.UserId });
            }
        }

        // GET: Admin/ProfileImageFix/Success/5
        public async Task<IActionResult> Success(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                _logger.LogWarning("No user ID provided to Success action");
                return RedirectToAction(nameof(Index));
            }

            _logger.LogInformation("Success page accessed for user {UserId}", id);

            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                _logger.LogWarning("User not found: {UserId}", id);
                TempData["ErrorMessage"] = "User not found";
                return RedirectToAction(nameof(Index));
            }

            var model = new ProfileImageUploadViewModel
            {
                UserId = user.Id,
                UserName = user.UserName,
                DisplayName = !string.IsNullOrEmpty(user.DisplayName) ? user.DisplayName : $"{user.FirstName} {user.LastName}",
                Email = user.Email,
                HasProfilePicture = !string.IsNullOrEmpty(user.ProfilePictureUrl)
            };

            if (!string.IsNullOrEmpty(user.ProfilePictureUrl))
            {
                model.CurrentProfileImageType = "url";
                model.ProfilePicturePreview = user.ProfilePictureUrl;
            }

            return View(model);
        }

        // POST: Admin/ProfileImageFix/RemoveProfilePicture
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveProfilePicture(string userId)
        {
            _logger.LogInformation("RemoveProfilePicture action started for user {UserId}", userId);

            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("No user ID provided to RemoveProfilePicture action");
                TempData["ErrorMessage"] = "User ID is required";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                // Find the user
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("User not found: {UserId}", userId);
                    TempData["ErrorMessage"] = "User not found";
                    return RedirectToAction(nameof(Index));
                }

                // If user has a ProfilePictureUrl, extract the media ID to delete the associated media
                if (!string.IsNullOrEmpty(user.ProfilePictureUrl))
                {
                    var urlParts = user.ProfilePictureUrl.Split('/');
                    if (urlParts.Length > 0 && int.TryParse(urlParts[urlParts.Length - 1], out int mediaId))
                    {
                        // Find the media to delete it
                        var media = await _context.Media.FindAsync(mediaId);
                        if (media != null)
                        {
                            _context.Media.Remove(media);
                        }
                    }
                }

                // Clear profile picture URL
                user.ProfilePictureUrl = null;
                user.LastModified = DateTime.UtcNow;

                _logger.LogInformation("Removing profile picture for user {UserId}", userId);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Profile picture removed successfully for user {UserId}", userId);
                TempData["SuccessMessage"] = "Profile picture removed successfully";
                return RedirectToAction(nameof(Success), new { id = userId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing profile picture for user {UserId}", userId);
                TempData["ErrorMessage"] = "An error occurred: " + ex.Message;
                return RedirectToAction(nameof(Upload), new { id = userId });
            }
        }

        // POST: Admin/ProfileImageFix/SetProfilePictureUrl
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetProfilePictureUrl(string userId, string pictureUrl)
        {
            _logger.LogInformation("SetProfilePictureUrl action started for user {UserId}", userId);

            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("No user ID provided to SetProfilePictureUrl action");
                TempData["ErrorMessage"] = "User ID is required";
                return RedirectToAction(nameof(Index));
            }

            if (string.IsNullOrEmpty(pictureUrl))
            {
                _logger.LogWarning("No URL provided for user {UserId}", userId);
                TempData["ErrorMessage"] = "Picture URL is required";
                return RedirectToAction(nameof(Upload), new { id = userId });
            }

            try
            {
                // Find the user
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("User not found: {UserId}", userId);
                    TempData["ErrorMessage"] = "User not found";
                    return RedirectToAction(nameof(Index));
                }

                // Set profile picture URL
                user.ProfilePictureUrl = pictureUrl;
                user.LastModified = DateTime.UtcNow;

                _logger.LogInformation("Setting profile picture URL for user {UserId}", userId);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Profile picture URL set successfully for user {UserId}", userId);
                TempData["SuccessMessage"] = "Profile picture URL set successfully";
                return RedirectToAction(nameof(Success), new { id = userId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting profile picture URL for user {UserId}", userId);
                TempData["ErrorMessage"] = "An error occurred: " + ex.Message;
                return RedirectToAction(nameof(Upload), new { id = userId });
            }
        }

        // API Endpoint: Admin/ProfileImageFix/UpdateProfilePicture
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfilePicture(string userId, IFormFile imageFile)
        {
            try
            {
                _logger.LogInformation("API: UpdateProfilePicture called for user {UserId}", userId);

                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("No user ID provided");
                    return BadRequest("User ID is required");
                }

                if (imageFile == null || imageFile.Length == 0)
                {
                    _logger.LogWarning("No file was uploaded");
                    return BadRequest("No file was uploaded");
                }

                // Find the user in the context
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("User not found: {UserId}", userId);
                    return NotFound("User not found");
                }

                // Upload the file as a media entity
                var media = await _fileUploadService.UploadFileAsync(
                    imageFile,
                    userId,
                    StorageProviderNames.LocalStorage,
                    "Profile picture",
                    "Personal"
                );

                if (media != null)
                {
                    // Update user's profile picture URL to point to the media
                    user.ProfilePictureUrl = $"/Media/{media.Id}";
                    user.LastModified = DateTime.UtcNow;

                    // Save changes
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Profile picture updated successfully");
                    return Ok(new { message = "Profile picture updated successfully" });
                }
                else
                {
                    return BadRequest("Failed to upload profile picture");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating profile picture");
                return StatusCode(500, "An error occurred: " + ex.Message);
            }
        }
    }

    public class ProfileImageUploadViewModel
    {
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string DisplayName { get; set; }
        public string Email { get; set; }
        public bool HasProfilePicture { get; set; }
        public string CurrentProfileImageType { get; set; } // "stored" or "url"
        public string ProfilePicturePreview { get; set; }
        public IFormFile ProfilePictureFile { get; set; }
        public string ProfilePictureUrl { get; set; }
    }
}