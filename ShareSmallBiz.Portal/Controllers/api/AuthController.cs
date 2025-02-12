using Microsoft.IdentityModel.Tokens;
using ShareSmallBiz.Portal.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
namespace ShareSmallBiz.Portal.Controllers.api;


[Route("api/auth")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly UserManager<ShareSmallBizUser> _userManager;
    private readonly SignInManager<ShareSmallBizUser> _signInManager;
    private readonly IConfiguration _configuration;

    public AuthController(UserManager<ShareSmallBizUser> userManager, SignInManager<ShareSmallBizUser> signInManager, IConfiguration configuration)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _configuration = configuration;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest model)
    {
        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null)
            return Unauthorized(new { Message = "Invalid credentials" });

        var result = await _signInManager.CheckPasswordSignInAsync(user, model.Password, false);
        if (!result.Succeeded)
            return Unauthorized(new { Message = "Invalid credentials" });

        var token = GenerateJwtToken(user);
        return Ok(new { Token = token, UserId = user.Id, DisplayName = user.DisplayName });
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest model)
    {
        var user = new ShareSmallBizUser
        {
            UserName = model.Email,
            Email = model.Email,
            DisplayName = model.DisplayName,
            FirstName = model.FirstName,
            LastName = model.LastName
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
            return BadRequest(result.Errors);

        var token = GenerateJwtToken(user);
        return Ok(new { Token = token, UserId = user.Id, DisplayName = user.DisplayName });
    }

    private string GenerateJwtToken(ShareSmallBizUser user)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim("displayName", user.DisplayName),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            _configuration["Jwt:Issuer"],
            _configuration["Jwt:Audience"],
            claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

public class LoginRequest
{
    public string Email { get; set; }
    public string Password { get; set; }
}

public class RegisterRequest
{
    public string Email { get; set; }
    public string Password { get; set; }
    public string DisplayName { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
}
