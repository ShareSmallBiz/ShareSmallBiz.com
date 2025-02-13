#nullable disable

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ShareSmallBiz.Portal.Data;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ShareSmallBiz.Portal.Areas.Identity.Pages.Account.Manage
{
    public class IndexModel : PageModel
    {
        private readonly UserManager<ShareSmallBizUser> _userManager;
        private readonly SignInManager<ShareSmallBizUser> _signInManager;

        public IndexModel(
            UserManager<ShareSmallBizUser> userManager,
            SignInManager<ShareSmallBizUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

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

            [Display(Name = "Profile Picture")]
            public byte[] ProfilePicture { get; set; }

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

        private async Task LoadAsync(ShareSmallBizUser user)
        {
            var userName = await _userManager.GetUserNameAsync(user);
            var phoneNumber = await _userManager.GetPhoneNumberAsync(user);

            Username = userName;

            Input = new InputModel
            {
                PhoneNumber = phoneNumber,
                Username = userName,
                FirstName = user.FirstName,
                LastName = user.LastName,
                ProfilePicture = user.ProfilePicture,
                MetaDescription = user.MetaDescription,
                Keywords = user.Keywords,
                Bio = user.Bio,
                WebsiteUrl = user.WebsiteUrl,
                LinkedIn = user.SocialLinks.FirstOrDefault(s => s.Platform == "LinkedIn")?.Url,
                Facebook = user.SocialLinks.FirstOrDefault(s => s.Platform == "Facebook")?.Url,
                Instagram = user.SocialLinks.FirstOrDefault(s => s.Platform == "Instagram")?.Url
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

        public async Task<IActionResult> OnPostAsync(CancellationToken ct = default)
        {
            var user = await _userManager.GetUserAsync(User).ConfigureAwait(false);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            if (!ModelState.IsValid)
            {
                await LoadAsync(user).ConfigureAwait(false);
                return Page();
            }

            // Update phone number
            var phoneNumber = await _userManager.GetPhoneNumberAsync(user);
            if (Input.PhoneNumber != phoneNumber)
            {
                var setPhoneResult = await _userManager.SetPhoneNumberAsync(user, Input.PhoneNumber);
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



            // Update Social Links
            var socialLinks = user.SocialLinks.ToList();

            UpdateSocialLink(socialLinks, user.Id, "LinkedIn", Input.LinkedIn);
            UpdateSocialLink(socialLinks, user.Id, "Facebook", Input.Facebook);
            UpdateSocialLink(socialLinks, user.Id, "Instagram", Input.Instagram);

            user.SocialLinks = socialLinks;

            if (updated)
            {
                await _userManager.UpdateAsync(user);
            }

            // Handle profile picture upload
            if (Request.Form.Files.Count > 0)
            {
                IFormFile file = Request.Form.Files.FirstOrDefault();
                using (var dataStream = new MemoryStream())
                {
                    await file.CopyToAsync(dataStream);
                    user.ProfilePicture = dataStream.ToArray();
                }
                await _userManager.UpdateAsync(user);
            }

            await _signInManager.RefreshSignInAsync(user);
            StatusMessage = "Your profile has been updated";
            return RedirectToPage();
        }

        private void UpdateSocialLink(List<SocialLink> socialLinks, string userId, string platform, string newUrl)
        {
            var existingLink = socialLinks.FirstOrDefault(s => s.Platform == platform);
            if (!string.IsNullOrEmpty(newUrl))
            {
                if (existingLink == null)
                {
                    socialLinks.Add(new SocialLink { UserId = userId, Platform = platform, Url = newUrl });
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
