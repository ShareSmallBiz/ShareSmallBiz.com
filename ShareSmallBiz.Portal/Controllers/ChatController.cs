using Microsoft.AspNetCore.Mvc;
using ShareSmallBiz.Portal.Infrastructure.Services;

namespace ShareSmallBiz.Portal.Controllers;




[Route("chat")]
[Authorize]
[Authorize(Roles = "Admin,User")]
public class ChatController : Controller
{
    private readonly ILogger<ProfilesController> logger;
    private readonly UserProvider userProvider;
    public ChatController(ILogger<ProfilesController> logger, UserProvider userProvider)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.userProvider = userProvider ?? throw new ArgumentNullException(nameof(userProvider));
    }
    public async Task<IActionResult> Index()
    {
        // Get the UserModel for the current user
        var userModel = await userProvider.GetUserByUsernameAsync(User.Identity.Name).ConfigureAwait(true);
        return View(userModel);
    }
}
