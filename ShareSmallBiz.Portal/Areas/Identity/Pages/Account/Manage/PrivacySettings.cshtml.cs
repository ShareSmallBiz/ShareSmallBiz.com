using System;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ShareSmallBiz.Portal.Data.Entities;
using ShareSmallBiz.Portal.Data.Enums;
using ShareSmallBiz.Portal.Infrastructure.Services;

namespace ShareSmallBiz.Portal.Areas.Identity.Pages.Account.Manage
{
    public class PrivacySettingsModel : PageModel
    {
        private readonly UserManager<ShareSmallBizUser> _userManager;
        private readonly SignInManager<ShareSmallBizUser> _signInManager;
        private readonly UserProvider _userProvider;

        public PrivacySettingsModel(
            UserManager<ShareSmallBizUser> userManager,
            SignInManager<ShareSmallBizUser> signInManager,
            UserProvider userProvider)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _userProvider = userProvider;
        }

        /// <summary>
        /// Gets or sets the status message.
        /// </summary>
        [TempData]
        public string StatusMessage { get; set; }

        /// <summary>
        /// Gets or sets the user display name.
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// Gets or sets the username.
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// Gets or sets the profile view count.
        /// </summary>
        public int ProfileViewCount { get; set; }

        /// <summary>
        /// Gets or sets the Input model containing form data.
        /// </summary>
        [BindProperty]
        public InputModel Input { get; set; }

        /// <summary>
        /// Input model for privacy settings form.
        /// </summary>
        public class InputModel
        {
            [Display(Name = "Profile Visibility")]
            public ProfileVisibility ProfileVisibility { get; set; }

            [Display(Name = "Custom Profile URL")]
            [RegularExpression(@"^[a-zA-Z0-9\-_]+$", ErrorMessage = "Custom URL can only contain letters, numbers, hyphens, and underscores")]
            [StringLength(50, ErrorMessage = "Custom URL cannot exceed 50 characters")]
            public string CustomProfileUrl { get; set; }

            [Display(Name = "Show Profile View Count")]
            public bool ShowProfileViewCount { get; set; }
        }

        private async Task LoadAsync(ShareSmallBizUser user)
        {
            var userName = await _userManager.GetUserNameAsync(user);

            UserName = userName;
            DisplayName = user.DisplayName;
            ProfileViewCount = user.ProfileViewCount;

            Input = new InputModel
            {
                ProfileVisibility = user.ProfileVisibility,
                CustomProfileUrl = user.CustomProfileUrl,
                ShowProfileViewCount = true // This would be stored in user settings if implemented
            };
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            await LoadAsync(user);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            if (!ModelState.IsValid)
            {
                await LoadAsync(user);
                return Page();
            }

            // Handle custom profile URL
            if (!string.IsNullOrWhiteSpace(Input.CustomProfileUrl) && Input.CustomProfileUrl != user.CustomProfileUrl)
            {
                var urlResult = await _userProvider.UpdateCustomProfileUrlAsync(user.Id, Input.CustomProfileUrl);
                if (!urlResult.Success)
                {
                    ModelState.AddModelError("Input.CustomProfileUrl", urlResult.ErrorMessage);
                    await LoadAsync(user);
                    return Page();
                }
            }

            // Update profile visibility
            if (Input.ProfileVisibility != user.ProfileVisibility)
            {
                await _userProvider.UpdateProfileVisibilityAsync(user.Id, Input.ProfileVisibility);
            }

            // Calculate profile completeness score (a good time to do this is when users are actively managing their profile)
            await _userProvider.UpdateProfileCompletenessScoreAsync(user.Id);

            // Success message
            StatusMessage = "Your privacy settings have been updated";
            return RedirectToPage();
        }
    }
}