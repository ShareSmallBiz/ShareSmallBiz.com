// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity.UI.Services;
using ShareSmallBiz.Portal.Data.Entities;
using System;
using System.Linq;
using System.Text.Encodings.Web;

namespace ShareSmallBiz.Portal.Areas.Identity.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly SignInManager<ShareSmallBizUser> _signInManager;
        private readonly UserManager<ShareSmallBizUser> _userManager;
        private readonly IUserStore<ShareSmallBizUser> _userStore;
        private readonly IUserEmailStore<ShareSmallBizUser> _emailStore;
        private readonly ILogger<RegisterModel> _logger;
        private readonly IEmailSender _emailSender;

        public RegisterModel(
            UserManager<ShareSmallBizUser> userManager,
            IUserStore<ShareSmallBizUser> userStore,
            SignInManager<ShareSmallBizUser> signInManager,
            ILogger<RegisterModel> logger,
            IEmailSender emailSender)
        {
            _userManager = userManager;
            _userStore = userStore;
            _emailStore = GetEmailStore();
            _signInManager = signInManager;
            _logger = logger;
            _emailSender = emailSender;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string ReturnUrl { get; set; }

        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        public class InputModel
        {
            [Required]
            [Display(Name = "User Name")]
            public string UserName { get; set; }

            [Required]
            [EmailAddress]
            [Display(Name = "Email")]
            public string Email { get; set; }

            [Required]
            [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Password")]
            public string Password { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Confirm password")]
            [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
            public string ConfirmPassword { get; set; }

            // New property for the custom captcha answer.
            [Required]
            [Display(Name = "Captcha Answer")]
            public string CaptchaAnswer { get; set; }
        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            ReturnUrl = returnUrl;
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Captcha validation.
            var sessionCaptcha = HttpContext.Session.GetInt32("CaptchaAnswer");
            if (sessionCaptcha == null)
            {
                _logger.LogWarning("Captcha session expired or not found.");
                ModelState.AddModelError("Input.CaptchaAnswer", "Captcha has expired. Please refresh and try again.");
                return Page();
            }
            if (!int.TryParse(Input.CaptchaAnswer, out int userCaptcha) || userCaptcha != sessionCaptcha)
            {
                _logger.LogWarning("Captcha validation failed. User input: {UserCaptcha}, Expected: {SessionCaptcha}", Input.CaptchaAnswer, sessionCaptcha);
                ModelState.AddModelError("Input.CaptchaAnswer", "Incorrect captcha answer. Please try again.");
                return Page();
            }

            try
            {
                // Optional: Pre-check for unique username and email to provide early error messages.
                var existingUserByName = await _userManager.FindByNameAsync(Input.UserName);
                if (existingUserByName != null)
                {
                    ModelState.AddModelError("Input.UserName", "Username already exists.");
                    return Page();
                }

                var existingUserByEmail = await _userManager.FindByEmailAsync(Input.Email);
                if (existingUserByEmail != null)
                {
                    ModelState.AddModelError("Input.Email", "Email already exists.");
                    return Page();
                }

                var user = CreateUser();
                user.FirstName = " ";
                user.LastName = " ";
                user.Slug = Input.UserName;
                user.DisplayName = Input.UserName;
                user.ProfilePicture = null;

                await _userStore.SetUserNameAsync(user, Input.UserName, CancellationToken.None);
                await _emailStore.SetEmailAsync(user, Input.Email, CancellationToken.None);

                var result = await _userManager.CreateAsync(user, Input.Password);
                if (result.Succeeded)
                {
                    _logger.LogInformation("User created a new account with password.");

                    var userId = await _userManager.GetUserIdAsync(user);
                    var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                    var callbackUrl = Url.Page(
                        "/Account/ConfirmEmail",
                        pageHandler: null,
                        values: new { area = "Identity", userId = userId, code = code, returnUrl = returnUrl },
                        protocol: Request.Scheme);

                    await _emailSender.SendEmailAsync(Input.Email, "Confirm your email",
                        $"Please confirm your account by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.");

                    if (_userManager.Options.SignIn.RequireConfirmedAccount)
                    {
                        return RedirectToPage("RegisterConfirmation", new { email = Input.Email, returnUrl = returnUrl });
                    }
                    else
                    {
                        await _signInManager.SignInAsync(user, isPersistent: false);
                        return LocalRedirect(returnUrl);
                    }
                }

                // Handle identity errors.
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while registering a new user.");
                // Display a generic error message.
                ModelState.AddModelError(string.Empty, "An unexpected error occurred. Please try again later.");
            }

            // If we got this far, something failed, redisplay form.
            return Page();
        }

        private ShareSmallBizUser CreateUser()
        {
            try
            {
                return Activator.CreateInstance<ShareSmallBizUser>();
            }
            catch
            {
                throw new InvalidOperationException($"Can't create an instance of '{nameof(ShareSmallBizUser)}'. " +
                    $"Ensure that '{nameof(ShareSmallBizUser)}' is not an abstract class and has a parameterless constructor, or alternatively " +
                    $"override the register page in /Areas/Identity/Pages/Account/Register.cshtml");
            }
        }

        private IUserEmailStore<ShareSmallBizUser> GetEmailStore()
        {
            if (!_userManager.SupportsUserEmail)
            {
                throw new NotSupportedException("The default UI requires a user store with email support.");
            }
            return (IUserEmailStore<ShareSmallBizUser>)_userStore;
        }
    }
}
