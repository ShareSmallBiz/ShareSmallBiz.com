// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using Microsoft.AspNetCore.Authentication;
using ShareSmallBiz.Portal.Data;
using ShareSmallBiz.Portal.Data.Entities;
using System;
using System.Linq;

namespace ShareSmallBiz.Portal.Areas.Identity.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly SignInManager<ShareSmallBizUser> _signInManager;
        private readonly ILogger<LoginModel> _logger;
        private readonly ShareSmallBizUserContext _context; // Add DbContext field

        public LoginModel(SignInManager<ShareSmallBizUser> signInManager, ILogger<LoginModel> logger, ShareSmallBizUserContext context) // Inject DbContext
        {
            _signInManager = signInManager;
            _logger = logger;
            _context = context; // Assign DbContext
        }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [BindProperty]
        public InputModel Input { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public string ReturnUrl { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [TempData]
        public string ErrorMessage { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public class InputModel
        {
            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Required]
            [EmailAddress]
            public string Email { get; set; }

            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Required]
            [DataType(DataType.Password)]
            public string Password { get; set; }

            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Display(Name = "Remember me?")]
            public bool RememberMe { get; set; }
        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                ModelState.AddModelError(string.Empty, ErrorMessage);
            }

            returnUrl ??= Url.Content("~/");

            // Clear the existing external cookie to ensure a clean login process
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            ReturnUrl = returnUrl;
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            _logger.LogInformation("Login attempt for email: {Email}", Input.Email);

            // --- Start Login History Tracking ---
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
            var userAgent = HttpContext.Request.Headers["User-Agent"].ToString() ?? "Unknown";
            var loginHistory = new LoginHistory
            {
                LoginTime = DateTime.UtcNow,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                Success = false // Default to false
            };
            // --- End Login History Tracking ---

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Login failed: Model state is invalid.");
                // --- Save Failed Login Attempt (Invalid Model) ---
                // We don't have a user ID yet, so we can't associate it
                _context.LoginHistories.Add(loginHistory);
                await _context.SaveChangesAsync();
                // --- End Save ---
                return Page();
            }

            var user = await _signInManager.UserManager.FindByEmailAsync(Input.Email);
            if (user == null)
            {
                _logger.LogWarning("Login failed: No user found with email {Email}", Input.Email);
                // --- Save Failed Login Attempt (User Not Found) ---
                // Still no user ID
                _context.LoginHistories.Add(loginHistory);
                await _context.SaveChangesAsync();
                // --- End Save ---
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                return Page();
            }

            // --- Associate User with Login History ---
            loginHistory.UserId = user.Id;
            // --- End Association ---

            _logger.LogWarning("User {UserName} found, attempting password sign-in.", user.UserName);

            var result = await _signInManager.PasswordSignInAsync(user.UserName, Input.Password, Input.RememberMe, lockoutOnFailure: false);

            // --- Update Login History Success Status ---
            loginHistory.Success = result.Succeeded;
            _context.LoginHistories.Add(loginHistory);
            await _context.SaveChangesAsync();
            // --- End Update and Save ---

            if (result.Succeeded)
            {
                _logger.LogWarning("User {UserName} successfully signed in.", user.UserName);

                // ✅ Explicitly set the authentication scheme
                var claimsPrincipal = await _signInManager.CreateUserPrincipalAsync(user);
                await HttpContext.SignInAsync(IdentityConstants.ApplicationScheme, claimsPrincipal);

                _logger.LogWarning("🔹 Forced Sign-In: User.Identity.IsAuthenticated = {IsAuthenticated}", User.Identity?.IsAuthenticated);

                // Check if the user has a profile picture URL.
                // If not, redirect them to profile management to set one up.
                if (string.IsNullOrEmpty(user.ProfilePictureUrl))
                {
                    _logger.LogWarning("User {UserName} does not have a profile picture. Redirecting to profile management.", user.UserName);
                    return Redirect("/Media/User/Profile");
                }

                return LocalRedirect(returnUrl);
            }
            // Removed redundant logging and saving for failed login here, as it's handled above
            else
            {
                _logger.LogWarning("Login failed: Invalid credentials for user {UserName}.", user.UserName);
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                return Page();
            }
        }
    }
}