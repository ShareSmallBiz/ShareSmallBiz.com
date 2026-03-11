using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
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
    private readonly IEmailSender _emailSender;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
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
        if (model == null || string.IsNullOrWhiteSpace(model.Email) || string.IsNullOrWhiteSpace(model.Password))
        {
            return BadRequest(new { Message = "Email and password are required" });
        }

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null)
            return Unauthorized(new { Message = "Invalid credentials" });

        // lockoutOnFailure: true — increments failure count and locks out after threshold
        var result = await _signInManager.CheckPasswordSignInAsync(user, model.Password, lockoutOnFailure: true);
        if (result.IsLockedOut)
        {
            _logger.LogWarning("Account locked out for email: {Email}", model.Email);
            return StatusCode(StatusCodes.Status429TooManyRequests, new { Message = "Account is temporarily locked. Please try again later." });
        }

        if (!result.Succeeded)
            return Unauthorized(new { Message = "Invalid credentials" });

        var token = await GenerateJwtToken(user);
        return Ok(new { Token = token, UserId = user.Id, DisplayName = user.DisplayName });
    }

    [HttpPost("oauth/login")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> OAuthLogin([FromBody] LoginRequest model)
    {
        return await Login(model);
    }

    [HttpPost("register")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest model)
    {
        if (model == null
            || string.IsNullOrWhiteSpace(model.DisplayName)
            || string.IsNullOrWhiteSpace(model.Email)
            || string.IsNullOrWhiteSpace(model.Password))
        {
            return BadRequest(new { Message = "Display name, email and password are required" });
        }

        // Generate a unique slug from display name
        var baseSlug = model.DisplayName.ToLowerInvariant()
            .Replace(" ", "-")
            .Replace("'", string.Empty)
            .Replace("\"", string.Empty);

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

        // Send email confirmation — do not issue JWT until email is verified
        var confirmationToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        var confirmationLink = Url.Action(
            "ConfirmEmail",
            "Account",
            new { userId = user.Id, token = confirmationToken },
            Request.Scheme);

        await _emailSender.SendEmailAsync(
            user.Email!,
            "Confirm your ShareSmallBiz account",
            $"Please confirm your account by clicking <a href='{confirmationLink}'>here</a>.");

        _logger.LogInformation("New user registered: {Email}. Confirmation email sent.", user.Email);

        return Ok(new { Message = "Registration successful. Please check your email to confirm your account before signing in." });
    }

    [HttpGet("test")]
    public IActionResult TestToken()
    {
        try
        {
            var authHeader = Request.Headers["Authorization"].FirstOrDefault();
            if (string.IsNullOrWhiteSpace(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                return Unauthorized(new { Message = "Missing or invalid Authorization header." });
            }

            var tokenString = authHeader["Bearer ".Length..].Trim();

            var secret = _configuration["JwtSettings:Secret"];
            var issuer = _configuration["JwtSettings:Issuer"];
            var audience = _configuration["JwtSettings:Audience"];

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret!));
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

            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(tokenString, tokenValidationParams, out SecurityToken _);

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

        var secret = _configuration["JwtSettings:Secret"];
        if (string.IsNullOrWhiteSpace(secret))
            throw new InvalidOperationException("JWT secret not configured. Check your appsettings or user secrets.");

        var expirationHours = _configuration.GetValue<int>("JwtSettings:ExpirationInHours", 1);

        var userRoles = await _userManager.GetRolesAsync(user);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id ?? string.Empty),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new("displayName", user.DisplayName ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        foreach (var role in userRoles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _configuration["JwtSettings:Issuer"],
            audience: _configuration["JwtSettings:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(expirationHours),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
