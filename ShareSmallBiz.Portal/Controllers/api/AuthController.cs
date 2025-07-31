using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ShareSmallBiz.Portal.Data.Entities;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
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

        var token = await GenerateJwtToken(user).ConfigureAwait(true);
        return Ok(new { Token = token, UserId = user.Id, DisplayName = user.DisplayName });
    }

    [HttpPost("oauth/login")]
    public async Task<IActionResult> OAuthLogin([FromBody] LoginRequest model)
    {
        // This creates the api/auth/oauth/login endpoint
        // You can either redirect to the main login or implement OAuth-specific logic here
        return await Login(model);
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest model)
    {
        // Validate required fields
        if (string.IsNullOrEmpty(model.DisplayName))
            return BadRequest(new { Message = "Display name is required" });

        // Generate a unique slug from display name
        var baseSlug = model.DisplayName.ToLowerInvariant()
            .Replace(" ", "-")
            .Replace("'", string.Empty)
            .Replace("\"", string.Empty);

        // Ensure slug is unique by checking if it already exists
        var uniqueSlug = baseSlug;
        var counter = 1;
        while (await _userManager.Users.AnyAsync(u => u.Slug == uniqueSlug))
        {
            uniqueSlug = $"{baseSlug}-{counter}";
            counter++;
        }

        var user = new ShareSmallBizUser
        {
            UserName = model.Email,
            Email = model.Email,
            DisplayName = model.DisplayName,
            FirstName = model.FirstName,
            LastName = model.LastName,
            Slug = uniqueSlug
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
            return BadRequest(result.Errors);

        var token = await GenerateJwtToken(user).ConfigureAwait(true);
        return Ok(new { Token = token, UserId = user.Id, DisplayName = user.DisplayName });
    }
    [HttpGet("test")]
    public IActionResult TestToken()
    {
        try
        {
            // 1) Retrieve the Authorization header
            var authHeader = Request.Headers["Authorization"].FirstOrDefault();
            if (string.IsNullOrWhiteSpace(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                return Unauthorized(new { Message = "Missing or invalid Authorization header." });
            }

            // 2) Extract the token (strip out "Bearer ")
            var tokenString = authHeader["Bearer ".Length..].Trim();

            // 3) Prepare validation parameters (matching GenerateJwtToken settings)
            var secret = _configuration["JwtSettings:Secret"];
            var issuer = _configuration["JwtSettings:Issuer"];
            var audience = _configuration["JwtSettings:Audience"];

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
            var tokenValidationParams = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateIssuer = true,
                ValidIssuer = issuer,
                ValidateAudience = true,
                ValidAudience = audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            // 4) Validate the token
            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(tokenString, tokenValidationParams, out SecurityToken validatedToken);

            // 5) If we get here without exception, the token is valid
            return Ok(new
            {
                Message = "Token is valid.",
                Claims = principal.Claims.Select(c => new { c.Type, c.Value }).ToList()
            });
        }
        catch (SecurityTokenExpiredException)
        {
            return Unauthorized(new { Message = "Token has expired." });
        }
        catch (SecurityTokenException ex)
        {
            return Unauthorized(new { Message = $"Token validation failed: {ex.Message}" });
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { Message = ex.Message });
        }
    }


    private async Task<string> GenerateJwtToken(ShareSmallBizUser user)
    {
        if (user == null)
            throw new ArgumentNullException(nameof(user), "User parameter is required.");

        // Retrieve secret from configuration
        var secret = _configuration["JwtSettings:Secret"];
        if (string.IsNullOrWhiteSpace(secret))
            throw new InvalidOperationException("JWT secret not configured or is empty. Check your appsettings or user secrets.");

        // Get user roles
        var userRoles = await _userManager.GetRolesAsync(user);

        // Build the claims
        var claims = new List<Claim>
    {
        new Claim(JwtRegisteredClaimNames.Sub, user.Id ?? string.Empty),
        new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
        new Claim("displayName", user.DisplayName ?? string.Empty),
        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
    };

        // Add roles as claims
        foreach (var role in userRoles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        // Generate the signing credentials
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // Construct the token
        var token = new JwtSecurityToken(
            issuer: _configuration["JwtSettings:Issuer"],
            audience: _configuration["JwtSettings:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds
        );

        // Return the serialized token
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

}

public class LoginRequest
{
    public string? Email { get; set; }
    public string? Password { get; set; }
}

public class RegisterRequest
{
    public string? Email { get; set; }
    public string? Password { get; set; }
    public string? DisplayName { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
}
