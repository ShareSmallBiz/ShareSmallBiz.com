using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;
using ShareSmallBiz.Portal.Data;
using ShareSmallBiz.Portal.Infrastructure.Services;
using System.Data;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;

namespace ShareSmallBiz.Portal.Areas.Admin.Controllers;

public class UserRolesController(
    ShareSmallBizUserContext _context,
    ShareSmallBizUserManager _userManager,
    RoleManager<IdentityRole> _roleManager,
    IWebHostEnvironment _webHostEnvironment,
    ILogger<UserRolesController> _logger) :
    AdminBaseController(_context, _userManager, _roleManager)
{
    private async Task<List<string>> GetUserRoles(ShareSmallBizUser user) =>
        new List<string>(await _userManager.GetRolesAsync(user));

    private async Task<UserModel> CreateUserModelAsync(ShareSmallBizUser user)
    {
        var userModel = new UserModel(user);

        // Add roles to the model
        userModel.Roles = await GetUserRoles(user);

        // Check lock status
        userModel.IsLockedOut = await _userManager.IsLockedOutAsync(user);

        // Initialize the profile picture properties
        if (user.ProfilePicture != null)
        {
            userModel.HasProfilePicture = true;
            userModel.ProfilePicturePreview = $"data:image/jpeg;base64,{Convert.ToBase64String(user.ProfilePicture)}";
        }
        else if (!string.IsNullOrEmpty(user.ProfilePictureUrl))
        {
            userModel.HasProfilePicture = true;
            userModel.ProfilePicturePreview = user.ProfilePictureUrl;
        }

        return userModel;
    }

    [HttpPost]
    public async Task<IActionResult> Delete(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return NotFound();

        var result = await _userManager.DeleteAsync(user);
        if (result.Succeeded)
        {
            TempData["SuccessMessage"] = "User deleted successfully.";
            return RedirectToAction("Index");
        }

        TempData["ErrorMessage"] = "Could not delete user. " + string.Join(", ", result.Errors.Select(e => e.Description));
        return RedirectToAction("Edit", new { userId });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return NotFound();

        var model = await CreateUserModelAsync(user);

        // Get available roles for the role dropdown
        ViewBag.AvailableRoles = _roleManager.Roles
            .Select(r => new SelectListItem { Value = r.Name, Text = r.Name })
            .OrderBy(r => r.Text)
            .ToList();

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(UserModel model)
    {
        var user = await _userManager.FindByIdAsync(model.Id);
        if (user == null)
            return NotFound();

        // Update basic user properties
        bool needsUpdate = false;

        if (user.Email != model.Email)
        {
            user.Email = model.Email;
            user.NormalizedEmail = model.Email.ToUpper();
            needsUpdate = true;
        }

        if (user.FirstName != model.FirstName)
        {
            user.FirstName = model.FirstName;
            needsUpdate = true;
        }

        if (user.LastName != model.LastName)
        {
            user.LastName = model.LastName;
            needsUpdate = true;
        }

        if (user.UserName != model.UserName)
        {
            user.UserName = model.UserName;
            user.NormalizedUserName = model.UserName.ToUpper();
            needsUpdate = true;
        }

        if (user.DisplayName != model.DisplayName)
        {
            user.DisplayName = model.DisplayName;
            needsUpdate = true;
        }

        if (user.Bio != model.Bio)
        {
            user.Bio = model.Bio;
            needsUpdate = true;
        }

        if (user.WebsiteUrl != model.WebsiteUrl)
        {
            user.WebsiteUrl = model.WebsiteUrl;
            needsUpdate = true;
        }

        // Handle profile picture update
        if (model.ProfilePictureOption == "upload" && model.ProfilePictureFile != null)
        {
            // Process and optimize the uploaded image
            using var memoryStream = new MemoryStream();
            await model.ProfilePictureFile.CopyToAsync(memoryStream);

            // Resize and optimize the image
            var optimizedImage = await OptimizeProfileImageAsync(memoryStream.ToArray());

            user.ProfilePicture = optimizedImage;
            user.ProfilePictureUrl = null;
            needsUpdate = true;
        }
        else if (model.ProfilePictureOption == "url" && !string.IsNullOrEmpty(model.ProfilePictureUrl))
        {
            user.ProfilePictureUrl = model.ProfilePictureUrl;
            user.ProfilePicture = null;
            needsUpdate = true;
        }
        else if (model.ProfilePictureOption == "remove")
        {
            user.ProfilePicture = null;
            user.ProfilePictureUrl = null;
            needsUpdate = true;
        }

        // Update user if changes were made
        if (needsUpdate)
        {
            user.LastModified = DateTime.UtcNow;
            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                // Get available roles for the role dropdown
                ViewBag.AvailableRoles = _roleManager.Roles
                    .Select(r => new SelectListItem { Value = r.Name, Text = r.Name })
                    .OrderBy(r => r.Text)
                    .ToList();

                return View(model);
            }

            TempData["SuccessMessage"] = "User updated successfully.";
        }

        return RedirectToAction("Index");
    }

    [HttpGet]
    [Route("Admin/UserRoles")]
    public async Task<IActionResult> Index(string emailConfirmed = "true", string role = "")
    {
        var users = await _userManager.Users
            .OrderByDescending(u => u.LastModified)
            .ToListAsync();

        var userModels = (await Task.WhenAll(users.Select(CreateUserModelAsync))).ToList();

        if (!string.IsNullOrEmpty(emailConfirmed))
        {
            bool isConfirmed = bool.Parse(emailConfirmed);
            userModels = userModels.Where(u => u.IsEmailConfirmed == isConfirmed).ToList();
        }

        if (!string.IsNullOrEmpty(role))
        {
            userModels = userModels.Where(u => u.Roles.Contains(role)).ToList();
        }

        // Get all available roles for the filter dropdown
        ViewBag.Roles = await _roleManager.Roles
            .Select(r => r.Name)
            .OrderBy(r => r)
            .ToListAsync();

        // Set the current filter values for the view
        ViewBag.CurrentEmailConfirmed = emailConfirmed;
        ViewBag.CurrentRole = role;

        // Add dashboard summary stats for users
        ViewBag.TotalUsers = await _userManager.Users.CountAsync();
        ViewBag.VerifiedUsers = await _userManager.Users.CountAsync(u => u.EmailConfirmed);
        ViewBag.UnverifiedUsers = ViewBag.TotalUsers - ViewBag.VerifiedUsers;

        return View(userModels);
    }

    [HttpPost]
    public async Task<IActionResult> LockUnlock(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return NotFound();

        if (await _userManager.IsLockedOutAsync(user))
            await _userManager.SetLockoutEndDateAsync(user, null);
        else
            await _userManager.SetLockoutEndDateAsync(user, DateTime.UtcNow.AddYears(100));

        TempData["SuccessMessage"] = await _userManager.IsLockedOutAsync(user)
            ? "User locked successfully."
            : "User unlocked successfully.";

        return RedirectToAction("Index");
    }

    [HttpGet]
    public async Task<IActionResult> Manage(string userId)
    {
        ViewBag.userId = userId;
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            ViewBag.ErrorMessage = $"User with Id = {userId} cannot be found";
            return View("NotFound");
        }
        ViewBag.UserName = user.UserName;

        var model = new List<ManageUserRolesVM>();
        foreach (var role in _roleManager.Roles.OrderBy(r => r.Name))
        {
            var roleVm = new ManageUserRolesVM
            {
                RoleId = role.Id,
                RoleName = role.Name,
                Selected = await _userManager.IsInRoleAsync(user, role.Name)
            };
            model.Add(roleVm);
        }
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Manage(List<ManageUserRolesVM> model, string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return View();

        var roles = await _userManager.GetRolesAsync(user);
        var removeResult = await _userManager.RemoveFromRolesAsync(user, roles);
        if (!removeResult.Succeeded)
        {
            ModelState.AddModelError(string.Empty, "Cannot remove user existing roles");
            return View(model);
        }

        var addResult = await _userManager.AddToRolesAsync(user,
            model.Where(x => x.Selected).Select(y => y.RoleName));
        if (!addResult.Succeeded)
        {
            ModelState.AddModelError(string.Empty, "Cannot add selected roles to user");
            return View(model);
        }

        TempData["SuccessMessage"] = "User roles updated successfully.";
        return RedirectToAction("Edit", new { userId });
    }

    [HttpGet]
    public IActionResult CreateBusinessUser()
    {
        // Return view for creating a business user
        return View(new CreateBusinessUserModel());
    }

    [HttpPost]
    public async Task<IActionResult> CreateBusinessUser(CreateBusinessUserModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        // Create the user
        var user = new ShareSmallBizUser
        {
            UserName = model.Email,
            Email = model.Email,
            FirstName = model.FirstName,
            LastName = model.LastName,
            DisplayName = $"{model.FirstName} {model.LastName}",
            Slug = model.Email.ToLower().Replace("@", "-at-"),
            EmailConfirmed = true, // Auto-confirm email to bypass verification
            Bio = model.Bio,
            WebsiteUrl = model.WebsiteUrl,
            LastModified = DateTime.UtcNow
        };

        // Handle profile picture upload if provided
        if (model.ProfilePictureFile != null && model.ProfilePictureFile.Length > 0)
        {
            using var memoryStream = new MemoryStream();
            await model.ProfilePictureFile.CopyToAsync(memoryStream);

            // Check if the image is not too large (e.g., 2MB limit)
            if (memoryStream.Length < 2097152) // 2MB
            {
                // Optimize the image before storing
                user.ProfilePicture = await OptimizeProfileImageAsync(memoryStream.ToArray());
            }
            else
            {
                ModelState.AddModelError("ProfilePictureFile", "The profile picture must be less than 2MB.");
                return View(model);
            }
        }

        // Create user with generated password
        var password = GenerateSecurePassword();
        var result = await _userManager.CreateAsync(user, password);

        if (result.Succeeded)
        {
            // Add user to Business role
            await _userManager.AddToRoleAsync(user, "Business");

            // Save temporary password to show to admin
            TempData["NewUserPassword"] = password;
            TempData["NewUserEmail"] = user.Email;

            return RedirectToAction("BusinessUserCreated");
        }

        // If we got this far, something failed
        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        return View(model);
    }

    [HttpGet]
    public IActionResult BusinessUserCreated()
    {
        // Display the created user information and password
        ViewBag.Password = TempData["NewUserPassword"];
        ViewBag.Email = TempData["NewUserEmail"];
        return View();
    }

    // Method to resize and optimize profile image
    private async Task<byte[]> OptimizeProfileImageAsync(byte[] originalImage)
    {
        try
        {
            // Load image
            using var memoryStream = new MemoryStream(originalImage);
            using var image = Image.FromStream(memoryStream);

            // Calculate dimensions while maintaining aspect ratio
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

            // Create a new bitmap with the calculated dimensions
            using var resizedImage = new Bitmap(width, height);
            using var graphics = Graphics.FromImage(resizedImage);

            // Set high quality scaling
            graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
            graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;

            // Draw the image to the new size
            graphics.DrawImage(image, 0, 0, width, height);

            // Save the resized image with reduced quality to optimize size
            using var outputStream = new MemoryStream();

            // Use JPEG encoder with quality setting
            var jpegEncoder = GetEncoder(ImageFormat.Jpeg);
            var encoderParameters = new EncoderParameters(1);
            // Important: Use System.Drawing.Imaging.Encoder to avoid ambiguity
            encoderParameters.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 80L);

            resizedImage.Save(outputStream, jpegEncoder, encoderParameters);
            return outputStream.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error optimizing profile image");
            return originalImage; // Return original if optimization fails
        }
    }

    private static ImageCodecInfo GetEncoder(ImageFormat format)
    {
        var codecs = ImageCodecInfo.GetImageEncoders();
        return codecs.FirstOrDefault(codec => codec.FormatID == format.Guid);
    }

    // Helper method to generate a secure random password
    private string GenerateSecurePassword()
    {
        // Generate a secure random password (16 characters)
        const string allowedChars = "abcdefghijkmnopqrstuvwxyzABCDEFGHJKLMNOPQRSTUVWXYZ0123456789!@$?_-";
        var random = new Random();
        var chars = new char[16];

        for (int i = 0; i < 16; i++)
        {
            chars[i] = allowedChars[random.Next(0, allowedChars.Length)];
        }

        return new string(chars);
    }
}