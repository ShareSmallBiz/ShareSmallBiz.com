using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ShareSmallBiz.Portal.Data.Entities;

namespace ShareSmallBiz.Portal.Controllers.api;

/// <summary>
/// OAuth-compatible login endpoint. Delegates to the shared auth logic.
/// </summary>
[Route("api/oauth")]
[ApiController]
public class OAuthController : ControllerBase
{
    private readonly UserManager<ShareSmallBizUser> _userManager;
    private readonly SignInManager<ShareSmallBizUser> _signInManager;
    private readonly IEmailSender _emailSender;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthController> _logger;

    public OAuthController(
        UserManager<ShareSmallBizUser> userManager,
        SignInManager<ShareSmallBizUser> signInManager,
        IEmailSender emailSender,
        IConfiguration configuration,
        ILogger<AuthController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _emailSender = emailSender;
        _configuration = configuration;
        _logger = logger;
    }

    [HttpPost("login")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Login([FromBody] LoginRequest model)
    {
        // Delegate to AuthController via DI-constructed instance
        var authController = new AuthController(_userManager, _signInManager, _emailSender, _configuration, _logger);
        authController.ControllerContext = ControllerContext;
        return await authController.Login(model);
    }
}
