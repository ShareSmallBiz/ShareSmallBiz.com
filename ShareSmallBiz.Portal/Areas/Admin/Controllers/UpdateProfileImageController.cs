using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using ShareSmallBiz.Portal.Data;
using ShareSmallBiz.Portal.Infrastructure.Services;
using System.Linq;

namespace ShareSmallBiz.Portal.Areas.Admin.Controllers
{
    [Authorize(Roles = "Admin")]
    [Area("Admin")]
    public class UpdateProfileImageController : Controller
    {
        private readonly ILogger<UpdateProfileImageController> _logger;
        private readonly ShareSmallBizUserContext _context;
        private readonly ShareSmallBizUserManager _userManager;

        public UpdateProfileImageController(
            ILogger<UpdateProfileImageController> logger,
            ShareSmallBizUserContext context,
            ShareSmallBizUserManager userManager)
        {
            _logger = logger;
            _context = context;
            _userManager = userManager;
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

            _logger.LogInformation("Loading user {UserId} for profile image update", userId);

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("User not found: {UserId}", userId);
                TempData["ErrorMessage"] = "User not found";
                return RedirectToAction("Index");
            }

            var model = new ProfileImageUpdateModel
            {
                UserId = user.Id,
                UserName = user.UserName,
                DisplayName = user.DisplayName ?? $"{user.FirstName} {user.LastName}",
                Email = user.Email,
                HasProfilePicture = user.ProfilePicture != null || !string.IsNullOrEmpty(user.ProfilePictureUrl)
            };

            if (user.ProfilePicture != null)
            {
                model.CurrentProfileImageType = "stored";
                model.ProfilePicturePreview = $"data:image/jpeg;base64,{Convert.ToBase64String(user.ProfilePicture)}";
            }
            else if (!string.IsNullOrEmpty(user.ProfilePictureUrl))
            {
                model.CurrentProfileImageType = "url";
                model.ProfilePicturePreview = user.ProfilePictureUrl;
                model.ProfilePictureUrl = user.ProfilePictureUrl;
            }

            _logger.LogInformation("User {UserId} loaded successfully. Has profile picture: {HasProfilePicture}",
                userId, model.HasProfilePicture);

            return View(model);
        }

        [HttpPost]
        [RequestSizeLimit(10 * 1024 * 1024)] // 10 MB
        [RequestFormLimits(MultipartBodyLengthLimit = 10 * 1024 * 1024)]
        public async Task<IActionResult> UpdateImage(ProfileImageUpdateModel model)
        {
            _logger.LogInformation("UpdateImage POST started for user {UserId}", model.UserId);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model state");
                return View("SelectUser", model);
            }

            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null)
            {
                _logger.LogWarning("User not found: {UserId}", model.UserId);
                TempData["ErrorMessage"] = "User not found";
                return RedirectToAction("Index");
            }

            bool profileUpdated = false;

            try
            {
                // Handle profile picture update based on selected option
                if (model.ProfilePictureOption == "upload" && model.ProfilePictureFile != null)
                {
                    _logger.LogInformation("Processing profile picture upload for user {UserId}", model.UserId);

                    if (model.ProfilePictureFile.Length > 2097152) // 2MB
                    {
                        _logger.LogWarning("File too large: {Size} bytes", model.ProfilePictureFile.Length);
                        ModelState.AddModelError("ProfilePictureFile", "The profile picture must be less than 2MB.");
                        return View("SelectUser", model);
                    }

                    // Process and optimize the uploaded image
                    using var memoryStream = new MemoryStream();
                    await model.ProfilePictureFile.CopyToAsync(memoryStream);

                    _logger.LogDebug("File copied to memory stream, size: {Size} bytes", memoryStream.Length);

                    // Optimize the image with ImageSharp
                    byte[] fileBytes = memoryStream.ToArray();
                }
                else if (model.ProfilePictureOption == "url" && !string.IsNullOrEmpty(model.ProfilePictureUrl))
                {
                    _logger.LogInformation("Setting profile picture URL to: {Url}", model.ProfilePictureUrl);
                    user.ProfilePictureUrl = model.ProfilePictureUrl;
                    user.ProfilePicture = null;
                    profileUpdated = true;
                }
                else if (model.ProfilePictureOption == "remove")
                {
                    _logger.LogInformation("Removing profile picture for user {UserId}", model.UserId);
                    user.ProfilePicture = null;
                    user.ProfilePictureUrl = null;
                    profileUpdated = true;
                }

                if (profileUpdated)
                {
                    user.LastModified = DateTime.UtcNow;
                    var result = await _userManager.UpdateAsync(user);

                    if (result.Succeeded)
                    {
                        _logger.LogInformation("Profile picture updated successfully for user {UserId}", model.UserId);
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
                _logger.LogError(ex, "Error updating profile picture for user {UserId}", model.UserId);
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
                HasProfilePicture = user.ProfilePicture != null || !string.IsNullOrEmpty(user.ProfilePictureUrl)
            };

            if (user.ProfilePicture != null)
            {
                model.CurrentProfileImageType = "stored";
                model.ProfilePicturePreview = $"data:image/jpeg;base64,{Convert.ToBase64String(user.ProfilePicture)}";
            }
            else if (!string.IsNullOrEmpty(user.ProfilePictureUrl))
            {
                model.CurrentProfileImageType = "url";
                model.ProfilePicturePreview = user.ProfilePictureUrl;
                model.ProfilePictureUrl = user.ProfilePictureUrl;
            }

            return View(model);
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