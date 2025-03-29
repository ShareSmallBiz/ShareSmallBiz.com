using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ShareSmallBiz.Portal.Data;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace ShareSmallBiz.Portal.Areas.Admin.Controllers
{
    [Authorize(Roles = "Admin")]
    [Area("Admin")]
    public class ProfileImageFixController : Controller
    {
        private readonly ILogger<ProfileImageFixController> _logger;
        private readonly ShareSmallBizUserContext _context;

        public ProfileImageFixController(
            ILogger<ProfileImageFixController> logger,
            ShareSmallBizUserContext context)
        {
            _logger = logger;
            _context = context;
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

                // Process image with ImageSharp
                byte[] optimizedImage;
                using (var memoryStream = new MemoryStream())
                {
                    await model.ProfilePictureFile.CopyToAsync(memoryStream);
                    _logger.LogInformation("Image uploaded. Size: {Size} bytes", memoryStream.Length);

                    optimizedImage = await OptimizeImageWithImageSharp(memoryStream.ToArray());
                }

                if (optimizedImage == null)
                {
                    _logger.LogError("Failed to optimize image for user {UserId}", model.UserId);
                    TempData["ErrorMessage"] = "Failed to process the image";
                    return RedirectToAction(nameof(Upload), new { id = model.UserId });
                }

                // Just update the profile picture and related fields
                user.ProfilePicture = optimizedImage;
                user.LastModified = DateTime.UtcNow;
                user.ProfilePictureUrl = null; // Clear URL if using uploaded image

                _logger.LogInformation("Saving profile picture changes to database for user {UserId}", model.UserId);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Profile picture updated successfully for user {UserId}", model.UserId);
                TempData["SuccessMessage"] = "Profile picture updated successfully";
                return RedirectToAction(nameof(Success), new { id = model.UserId });
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

                // Clear profile picture data
                user.ProfilePicture = null;
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
                user.ProfilePicture = null; // Clear stored image if using URL
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

                // Process the image with ImageSharp
                byte[] optimizedImage;
                using (var memoryStream = new MemoryStream())
                {
                    await imageFile.CopyToAsync(memoryStream);
                    _logger.LogInformation("Image uploaded. Size: {Size} bytes", memoryStream.Length);

                    optimizedImage = await OptimizeImageWithImageSharp(memoryStream.ToArray());
                }

                if (optimizedImage == null)
                {
                    _logger.LogError("Failed to optimize image");
                    return BadRequest("Failed to process the image");
                }

                // Just update the profile picture and last modified date
                user.ProfilePicture = optimizedImage;
                user.LastModified = DateTime.UtcNow;
                user.ProfilePictureUrl = null; // Clear URL if using uploaded image

                _logger.LogInformation("Saving changes to database");
                await _context.SaveChangesAsync();

                _logger.LogInformation("Profile picture updated successfully");
                return Ok(new { message = "Profile picture updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating profile picture");
                return StatusCode(500, "An error occurred: " + ex.Message);
            }
        }

        /// <summary>
        /// Optimizes an image using ImageSharp - resizes and compresses
        /// </summary>
        private async Task<byte[]> OptimizeImageWithImageSharp(byte[] originalImage)
        {
            _logger.LogInformation("Beginning ImageSharp optimization of {Size} byte image", originalImage.Length);

            try
            {
                using var inputStream = new MemoryStream(originalImage);
                using var outputStream = new MemoryStream();

                using var image = await Image.LoadAsync(inputStream);

                _logger.LogInformation("Original image dimensions: {Width}x{Height}", image.Width, image.Height);

                // Calculate new dimensions while preserving aspect ratio
                const int maxSize = 250; // Maximum dimension (width or height)
                int width, height;

                if (image.Width > image.Height)
                {
                    width = maxSize;
                    height = (int)(image.Height * ((float)maxSize / image.Width));
                }
                else
                {
                    height = maxSize;
                    width = (int)(image.Width * ((float)maxSize / image.Height));
                }

                _logger.LogInformation("Calculated new dimensions: {Width}x{Height}", width, height);

                // Resize the image
                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Size = new Size(width, height),
                    Mode = ResizeMode.Max
                }));

                // Save as JPEG with quality setting
                await image.SaveAsJpegAsync(outputStream, new JpegEncoder
                {
                    Quality = 80
                });

                var result = outputStream.ToArray();
                _logger.LogInformation("Image optimization complete. Result size: {Size} bytes", result.Length);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ImageSharp processing: {ErrorMessage}", ex.Message);
                return null;
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