using ShareSmallBiz.Portal.Data;
using ShareSmallBiz.Portal.Infrastructure.Services;

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

        var result = await _userManager.UpdateAsync(user);
        if (result.Succeeded)
            return RedirectToAction("Index");

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var users = await _userManager.Users.ToListAsync();
        var userModels = await Task.WhenAll(users.Select(CreateUserModelAsync));
        return View(userModels.ToList());
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
}
