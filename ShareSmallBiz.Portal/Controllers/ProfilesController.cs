using ShareSmallBiz.Portal.Infrastructure.Services;

namespace ShareSmallBiz.Portal.Controllers;

[Route("Profiles")]
public class ProfilesController(
    UserProvider userProvider,
    ILogger<ProfilesController> logger) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var users = await userProvider.GetAllPublicUsersAsync();
        return View(users);
    }
    [HttpGet("{id}")]
    public async Task<IActionResult> ViewProfile(string id)
    {
        var profile = await userProvider.GetUserByUsernameAsync(id).ConfigureAwait(false);
        if (profile == null)
        {
            return NotFound();
        }

        // Check if the requested id exactly matches the canonical username
        if (!string.Equals(id, profile.UserName, StringComparison.Ordinal))
        {
            // Permanent redirect (301) to the correct profile URL
            return RedirectPermanent($"/Profiles/{profile.UserName}");
        }

        return View(profile);
    }

}
