using ShareSmallBiz.Portal.Data;
using ShareSmallBiz.Portal.Infrastructure.Services;

namespace ShareSmallBiz.Portal.Areas.Admin.Controllers;

public class EmailSendController(
    ShareSmallBizUserContext _context,
    ShareSmallBizUserManager _userManager,
    RoleManager<IdentityRole> _roleManager, MailerSendService _mailerSendService
    ) : AdminBaseController(_context, _userManager, _roleManager)
{

    // GET: Display the email form.
    [HttpGet]
    public IActionResult Index()
    {
        // Return the view with an empty EmailRequest model.
        return View(new EmailRequest());
    }

    // POST: Process the email form submission.
    [HttpPost]
    public async Task<IActionResult> Index(EmailRequest model)
    {
        if (!ModelState.IsValid)
        {
            // Validation failed; re-display the form.
            return View(model);
        }

        try
        {
            // Send the email via MailerSend API.
            var response = await _mailerSendService.SendEmailAsync(
                model.FromEmail,
                model.ToEmail,
                model.Subject,
                model.HtmlContent,
                model.TextContent
            );

            // Pass a success message to the view.
            ViewBag.Message = "Email sent successfully!";
        }
        catch (Exception ex)
        {
            // Add a model error if sending fails.
            ModelState.AddModelError(string.Empty, $"Error sending email: {ex.Message}");
        }

        // Return the view with the model (and potential error messages).
        return View(model);
    }

    // DTO for capturing email data from the form.
    public class EmailRequest
    {
        [Required(ErrorMessage = "From Email is required.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        public string? FromEmail { get; set; }

        [Required(ErrorMessage = "To Email is required.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        public string? ToEmail { get; set; }

        [Required(ErrorMessage = "Subject is required.")]
        public string? Subject { get; set; }

        [Required(ErrorMessage = "HTML content is required.")]
        public string? HtmlContent { get; set; }

        [Required(ErrorMessage = "Text content is required.")]
        public string? TextContent { get; set; }
    }
}

