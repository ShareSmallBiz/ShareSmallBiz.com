using ShareSmallBiz.Portal.Infrastructure.Services;

namespace ShareSmallBiz.Portal.Controllers;

[Route("Profiles")]
public class ProfilesController : Controller
{
    private readonly ILogger<ProfilesController> logger;
    private readonly UserProvider userProvider;

    public ProfilesController(ILogger<ProfilesController> logger, UserProvider userProvider)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.userProvider = userProvider ?? throw new ArgumentNullException(nameof(userProvider));
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var users = await userProvider.GetAllPublicUsersAsync();
        return View(users);
    }
    [HttpGet("{id}")]
    public async Task<IActionResult> ViewProfile(string id)
    {
        var users = await userProvider.GetAllPublicUsersAsync();
        var userModel = await userProvider.GetUserByUsernameAsync(id).ConfigureAwait(false);
        if (userModel == null)
        {
            logger.LogError("Missing Profile:{id}", id);
            return RedirectToAction("Index");
        }

        // Check if the requested id exactly matches the canonical username
        if (!string.Equals(id, userModel.DisplayName, StringComparison.Ordinal))
        {
            // Permanent redirect (301) to the correct userModel URL
            return RedirectPermanent($"/Profiles/{userModel.DisplayName}");
        }
        var profileModel = new ProfileModel(userModel);
        profileModel.PublicUsers = users;
        return View(profileModel);
    }

}
