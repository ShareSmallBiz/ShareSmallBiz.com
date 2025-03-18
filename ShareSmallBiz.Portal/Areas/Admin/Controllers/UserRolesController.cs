using Microsoft.AspNetCore.Identity;
using ShareSmallBiz.Portal.Data;
using ShareSmallBiz.Portal.Infrastructure.Services;
using System.Data;

namespace ShareSmallBiz.Portal.Areas.Admin.Controllers;

public class UserRolesController(
    ShareSmallBizUserContext _context,
    ShareSmallBizUserManager _userManager,
    RoleManager<IdentityRole> _roleManager) :
    AdminBaseController(_context, _userManager, _roleManager)
{
    private async Task<List<string>> GetUserRoles(ShareSmallBizUser user) =>
        new List<string>(await _userManager.GetRolesAsync(user));

    private async Task<UserModel> CreateUserModelAsync(ShareSmallBizUser user) =>
        new UserModel
        {
            Id = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Roles = await GetUserRoles(user),
            UserName = user.UserName,
            IsEmailConfirmed = user.EmailConfirmed,
            IsLockedOut = await _userManager.IsLockedOutAsync(user)
        };

    [HttpPost]
    public async Task<IActionResult> Delete(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return NotFound();

        var result = await _userManager.DeleteAsync(user);
        if (result.Succeeded)
            return RedirectToAction("Index");

        return BadRequest("Could not delete user");
    }

    [HttpGet]
    public async Task<IActionResult> Edit(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return NotFound();

        var model = await CreateUserModelAsync(user);
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(UserModel model)
    {
        var user = await _userManager.FindByIdAsync(model.Id);
        if (user == null)
            return NotFound();

        user.Email = model.Email;
        user.FirstName = model.FirstName;
        user.LastName = model.LastName;
        user.UserName = model.UserName;
        user.DisplayName = model.UserName;
        user.Slug = user.UserName;
        user.Bio = model.Bio;
        user.WebsiteUrl = model.WebsiteUrl;


        // Handle profile picture upload
        if (Request.Form.Files.Count > 0)
        {
            IFormFile file = Request.Form.Files.FirstOrDefault();
            using (var dataStream = new MemoryStream())
            {
                await file.CopyToAsync(dataStream);
                user.ProfilePicture = dataStream.ToArray();
            }
        }
        var result = await _userManager.UpdateAsync(user);
        if (result.Succeeded)
            return RedirectToAction("Index");

        return View(model);
    }

    [HttpGet]
    [Route("Admin/UserRoles")]
    public async Task<IActionResult> Index(string emailConfirmed = "", string role = "")
    {
        var users = await _userManager.Users.ToListAsync();
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

        ViewBag.Roles = userModels.SelectMany(u => u.Roles).Distinct().ToList();
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

        return RedirectToAction("Index");
    }

    [HttpGet]
    public async Task<IActionResult> Manage(string userId)
    {
        ViewBag.userId = userId;
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            ViewBag.ErrorMessage = $"User with ServiceDefinitionId = {userId} cannot be found";
            return View("NotFound");
        }
        ViewBag.UserName = user.UserName;

        var model = new List<ManageUserRolesVM>();
        foreach (var role in _roleManager.Roles)
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

        return RedirectToAction("Index");
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
            WebsiteUrl = model.WebsiteUrl
        };

        // Handle profile picture upload if provided
        if (model.ProfilePicture != null && model.ProfilePicture.Length > 0)
        {
            using (var memoryStream = new MemoryStream())
            {
                await model.ProfilePicture.CopyToAsync(memoryStream);
                // Check if the image is not too large (e.g., 2MB limit)
                if (memoryStream.Length < 2097152) // 2MB
                {
                    user.ProfilePicture = memoryStream.ToArray();
                }
                else
                {
                    ModelState.AddModelError("ProfilePicture", "The profile picture must be less than 2MB.");
                    return View(model);
                }
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
public class CreateBusinessUserModel
{
    [Required]
    [EmailAddress]
    [Display(Name = "Email")]
    public string Email { get; set; }

    [Display(Name = "First Name")]
    public string? FirstName { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Last Name")]
    public string LastName { get; set; } = string.Empty;

    // Add any additional business-specific fields here
    [Display(Name = "Company Name")]
    public string CompanyName { get; set; } = string.Empty;

    [Display(Name = "Business Phone")]
    public string? BusinessPhone { get; set; } = string.Empty;

    [Display(Name = "Biography")]
    [DataType(DataType.MultilineText)]
    public string? Bio { get; set; } = string.Empty;

    [Display(Name = "Website URL")]
    [Url]
    public string WebsiteUrl { get; set; }

    [Display(Name = "Profile Picture")]
    public IFormFile ProfilePicture { get; set; }
}