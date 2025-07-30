using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ShareSmallBiz.Portal.Data.Entities;

namespace ShareSmallBiz.Portal.Controllers.api;

[Route("api/oauth")]
[ApiController]
public class OAuthController : ControllerBase
{
    private readonly AuthController _authController;

    public OAuthController(UserManager<ShareSmallBizUser> userManager,
                          SignInManager<ShareSmallBizUser> signInManager,
                          IConfiguration configuration)
    {
        _authController = new AuthController(userManager, signInManager, configuration);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest model)
    {
        // Delegate to the existing auth controller
        return await _authController.Login(model);
    }
}
