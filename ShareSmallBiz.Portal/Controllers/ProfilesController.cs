using Markdig.Extensions.MediaLinks;
using Microsoft.AspNetCore.Mvc;
using ShareSmallBiz.Portal.Infrastructure.Services;

namespace ShareSmallBiz.Portal.Controllers;


[Route("Profiles")]
public class ProfilesController(
    UserProvider userProvider, 
    ILogger<ProfilesController> logger) : Controller
{
    public IActionResult Index()
    {
        return View();
    }
    [HttpGet("{id}")]
    public async Task<IActionResult> ViewProfile(string id)
    {
        var profile = await userProvider.GetUserByUsernameAsync(id).ConfigureAwait(false);
        if (profile == null)
        {
            return NotFound();
        }
        return View(profile);
    }
}
