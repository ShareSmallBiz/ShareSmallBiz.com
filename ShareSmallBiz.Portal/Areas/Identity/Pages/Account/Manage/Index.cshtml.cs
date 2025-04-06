﻿#nullable disable
using ShareSmallBiz.Portal.Data;
using ShareSmallBiz.Portal.Data.Entities;
using ShareSmallBiz.Portal.Infrastructure.Services;
using System.Linq;

namespace ShareSmallBiz.Portal.Areas.Identity.Pages.Account.Manage
{
    public class IndexModel(
        ShareSmallBizUserManager userManager,
        SignInManager<ShareSmallBizUser> signInManager) : PageModel
    {
        public string Username { get; set; }

        [TempData]
        public string StatusMessage { get; set; }

        [TempData]
        public string UserNameChangeLimitMessage { get; set; }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Required]
            [Display(Name = "First Name")]
            public string FirstName { get; set; }
            [Required]
            [Display(Name = "Last Name")]
            public string LastName { get; set; }

            [Required]
            [Display(Name = "Username")]
            public string Username { get; set; }

            [Phone]
            [Display(Name = "Phone number")]
            public string PhoneNumber { get; set; }

            // Profile picture url property
            [Display(Name = "Profile Picture URL")]
            public string ProfilePictureUrl { get; set; }

            [Display(Name = "Meta Description")]
            public string MetaDescription { get; set; }

            [Display(Name = "SEO Keywords (comma-separated)")]
            public string Keywords { get; set; }

            // Business Information
            [Display(Name = "Bio")]
            public string Bio { get; set; }

            [Display(Name = "Website")]
            public string WebsiteUrl { get; set; }

            // Social Links
            [Display(Name = "LinkedIn")]
            public string LinkedIn { get; set; }

            [Display(Name = "Facebook")]
            public string Facebook { get; set; }

            [Display(Name = "Instagram")]
            public string Instagram { get; set; }
        }

        private async Task LoadAsync(ShareSmallBizUser user, CancellationToken ct = default)
        {
            var userName = await userManager.GetUserNameAsync(user).ConfigureAwait(true);
            var phoneNumber = await userManager.GetPhoneNumberAsync(user).ConfigureAwait(true);

            Username = userName;

            Input = new InputModel
            {
                PhoneNumber = phoneNumber,
                Username = userName,
                FirstName = user.FirstName,
                LastName = user.LastName,
                ProfilePictureUrl = user.ProfilePictureUrl,
                MetaDescription = user.MetaDescription,
                Keywords = user.Keywords,
                Bio = user.Bio,
                WebsiteUrl = user.WebsiteUrl,
                LinkedIn = user.SocialLinks.FirstOrDefault(s => s.Platform == "LinkedIn")?.Url,
                Facebook = user.SocialLinks.FirstOrDefault(s => s.Platform == "Facebook")?.Url,
                Instagram = user.SocialLinks.FirstOrDefault(s => s.Platform == "Instagram")?.Url
            };
        }

        public async Task<IActionResult> OnGetAsync(CancellationToken ct = default)
        {
            var user = await userManager.GetFullUserAsync(User).ConfigureAwait(false);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{userManager.GetUserId(User)}'.");
            }
            await LoadAsync(user, ct).ConfigureAwait(false);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(CancellationToken ct = default)
        {
            var user = await userManager.GetFullUserAsync(User).ConfigureAwait(false);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{userManager.GetUserId(User)}'.");
            }

            if (!ModelState.IsValid)
            {
                await LoadAsync(user, ct).ConfigureAwait(false);
                return Page();
            }

            // Update phone number
            var phoneNumber = await userManager.GetPhoneNumberAsync(user);
            if (Input.PhoneNumber != phoneNumber)
            {
                var setPhoneResult = await userManager.SetPhoneNumberAsync(user, Input.PhoneNumber);
                if (!setPhoneResult.Succeeded)
                {
                    StatusMessage = "Unexpected error when trying to set phone number.";
                    return RedirectToPage();
                }
            }

            // Update user profile fields
            bool updated = false;

            if (Input.FirstName != user.FirstName)
            {
                user.FirstName = Input.FirstName;
                updated = true;
            }
            if (Input.LastName != user.LastName)
            {
                user.LastName = Input.LastName;
                updated = true;
            }
            if (Input.Bio != user.Bio)
            {
                user.Bio = Input.Bio;
                updated = true;
            }
            if (Input.WebsiteUrl != user.WebsiteUrl)
            {
                user.WebsiteUrl = Input.WebsiteUrl;
                updated = true;
            }

            // Update SEO Fields
            if (Input.Username != user.UserName)
            {
                // Check if username is already taken
                var existingUser = await userManager.FindByNameAsync(Input.Username);
                if (existingUser != null && existingUser.Id != user.Id)
                {
                    UserNameChangeLimitMessage = "Username is already taken.";
                    return RedirectToPage();
                }

                user.Slug = Input.Username;
                user.DisplayName = Input.Username;
                user.UserName = Input.Username;
                updated = true;
            }
            if (string.IsNullOrEmpty(user.UserName))
            {
                user.UserName = user.Email;
                updated = true;
            }
            if (string.IsNullOrEmpty(user.DisplayName))
            {
                user.DisplayName = Input.Username;
                updated = true;
            }

            if (Input.MetaDescription != user.MetaDescription)
            {
                user.MetaDescription = Input.MetaDescription;
                updated = true;
            }
            if (Input.Keywords != user.Keywords)
            {
                user.Keywords = Input.Keywords;
                updated = true;
            }
            if ((Input.LinkedIn != null && user.SocialLinks.FirstOrDefault(s => s.Platform == "LinkedIn")?.Url != Input.LinkedIn) ||
                (Input.Facebook != null && user.SocialLinks.FirstOrDefault(s => s.Platform == "Facebook")?.Url != Input.Facebook) ||
                (Input.Instagram != null && user.SocialLinks.FirstOrDefault(s => s.Platform == "Instagram")?.Url != Input.Instagram))
            {
                user.SocialLinks = await userManager.GetUserSocialLinksAsync(user.Id).ConfigureAwait(false);
                UpdateSocialLink(user.SocialLinks, user.Id, "LinkedIn", Input.LinkedIn);
                UpdateSocialLink(user.SocialLinks, user.Id, "Facebook", Input.Facebook);
                UpdateSocialLink(user.SocialLinks, user.Id, "Instagram", Input.Instagram);

                updated = true;
            }

            if (updated)
            {
                await userManager.UpdateAsync(user);
            }

            await signInManager.RefreshSignInAsync(user);
            StatusMessage = "Your profile has been updated";
            return RedirectToPage();
        }

        private void UpdateSocialLink(ICollection<SocialLink> socialLinks, string userId, string platform, string newUrl)
        {
            var existingLink = socialLinks.FirstOrDefault(s => s.Platform == platform);
            if (!string.IsNullOrEmpty(newUrl))
            {
                if (existingLink == null)
                {
                    socialLinks.Add(new SocialLink { CreatedID = userId, Platform = platform, Url = newUrl });
                }
                else if (string.Compare(existingLink.Url, newUrl) != 0)
                {
                    existingLink.Url = newUrl;
                }
            }
            else if (existingLink != null)
            {
                socialLinks.Remove(existingLink);
            }
        }
    }
}