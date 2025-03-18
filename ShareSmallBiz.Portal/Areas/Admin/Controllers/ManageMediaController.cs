using Microsoft.AspNetCore.Mvc.Rendering;
using ShareSmallBiz.Portal.Data;
using ShareSmallBiz.Portal.Infrastructure.Services;
using ShareSmallBiz.Portal.Services;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace ShareSmallBiz.Portal.Areas.Admin.Controllers;

public class ManageMediaController(
    ShareSmallBizUserContext _context,
    ShareSmallBizUserManager userManager,
    RoleManager<IdentityRole> _roleManager,
    ILogger<AdminDiscussionController> _logger,
    StorageProviderService _storageProviderService
    ) : AdminBaseController(_context, userManager, _roleManager)
{
    // GET: ManageMedia
    public async Task<IActionResult> Index(string searchString, int? mediaTypeFilter, int? storageProviderFilter)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var user = await _userManager.FindByIdAsync(userId);

        if (user == null)
        {
            return NotFound();
        }

        var mediaQuery = _context.Media
            .Include(m => m.User)
            .Where(m => m.UserId == userId)
            .AsQueryable();

        // Apply search filter
        if (!string.IsNullOrEmpty(searchString))
        {
            mediaQuery = mediaQuery.Where(m =>
                m.FileName.Contains(searchString) ||
                m.Description.Contains(searchString) ||
                m.Attribution.Contains(searchString));
        }

        // Apply media type filter
        if (mediaTypeFilter.HasValue)
        {
            var mediaType = (MediaType)mediaTypeFilter.Value;
            mediaQuery = mediaQuery.Where(m => m.MediaType == mediaType);
        }

        // Apply storage provider filter
        if (storageProviderFilter.HasValue)
        {
            var storageProvider = (Data.StorageProviderNames)storageProviderFilter.Value;
            mediaQuery = mediaQuery.Where(m => m.StorageProvider == storageProvider);
        }

        var viewModel = new MediaIndexViewModel
        {
            Media = await mediaQuery.OrderByDescending(m => m.CreatedDate).ToListAsync(),
            SearchString = searchString,
            MediaTypeFilter = mediaTypeFilter,
            StorageProviderFilter = storageProviderFilter,
            MediaTypes = Enum.GetValues(typeof(MediaType))
                .Cast<MediaType>()
                .Select(mt => new SelectListItem
                {
                    Value = ((int)mt).ToString(),
                    Text = mt.ToString()
                }).ToList(),
            StorageProviders = Enum.GetValues(typeof(Data.StorageProviderNames))
                .Cast<Data.StorageProviderNames>()
                .Select(sp => new SelectListItem
                {
                    Value = ((int)sp).ToString(),
                    Text = sp.ToString()
                }).ToList()
        };

        return View(viewModel);
    }

    // GET: ManageMedia/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var media = await _context.Media
            .Include(m => m.User)
            .FirstOrDefaultAsync(m => m.Id == id && m.UserId == userId);

        if (media == null)
        {
            return NotFound();
        }

        // Get public URL
        var publicUrl = await _storageProviderService.GetPublicUrlAsync(media);
        ViewBag.PublicUrl = publicUrl;

        // Set YouTube embed URL if applicable
        if (media.StorageProvider == StorageProviderNames.YouTube)
        {
            ViewBag.YouTubeEmbedUrl = GetYouTubeEmbedUrl(media.Url);
        }

        var vm = new ManageMediaViewModel(media);

        return View(vm);
    }

    // GET: ManageMedia/Create
    public IActionResult Create()
    {
        var viewModel = new ManageMediaViewModel
        {
            MediaTypes = Enum.GetValues(typeof(MediaType))
                .Cast<MediaType>()
                .Select(mt => new SelectListItem
                {
                    Value = ((int)mt).ToString(),
                    Text = mt.ToString()
                }).ToList(),
            StorageProviders = Enum.GetValues(typeof(StorageProviderNames))
                .Cast<StorageProviderNames>()
                .Select(sp => new SelectListItem
                {
                    Value = ((int)sp).ToString(),
                    Text = sp.ToString()
                }).ToList()
        };

        return View(viewModel);
    }

    // POST: ManageMedia/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ManageMediaViewModel viewModel)
    {
        if (ModelState.IsValid)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            try
            {
                Media media;

                if (IsValidYouTubeUrl(viewModel.ExternalUrl))
                {
                    viewModel.IsExternalLink = true;
                    viewModel.IsYouTube = true;
                    viewModel.YouTubeUrl = viewModel.ExternalUrl;
                }


                if (viewModel.IsYouTube)
                {
                    // Handle YouTube video
                    if (string.IsNullOrEmpty(viewModel.YouTubeUrl))
                    {
                        ModelState.AddModelError("YouTubeUrl", "Please enter a YouTube URL.");
                        PrepareCreateViewModel(viewModel);
                        return View(viewModel);
                    }

                    media = await _storageProviderService.CreateExternalLinkAsync(
                        viewModel.YouTubeUrl,
                        viewModel.FileName,
                        MediaType.Video, // Force video type for YouTube
                        userId,
                        viewModel.Attribution,
                        viewModel.Description
                    );
                }
                else if (viewModel.IsExternalLink)
                {
                    // Create regular external link
                    media = await _storageProviderService.CreateExternalLinkAsync(
                        viewModel.ExternalUrl,
                        viewModel.FileName,
                        (MediaType)viewModel.MediaType,
                        userId,
                        viewModel.Attribution,
                        viewModel.Description
                    );
                }
                else
                {
                    // Upload file (unchanged)
                    if (viewModel.File == null || viewModel.File.Length == 0)
                    {
                        ModelState.AddModelError("File", "Please select a file to upload.");
                        PrepareCreateViewModel(viewModel);
                        return View(viewModel);
                    }

                    media = await _storageProviderService.UploadFileAsync(
                        viewModel.File,
                        userId,
                        (StorageProviderNames)viewModel.StorageProvider,
                        viewModel.Description
                    );

                    // Update additional properties
                    media.Attribution = viewModel.Attribution;
                    await _context.SaveChangesAsync();
                }

                TempData["SuccessMessage"] = "Media uploaded successfully.";
                return RedirectToAction(nameof(Details), new { id = media.Id });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error creating media: {ex.Message}");
                PrepareCreateViewModel(viewModel);
                return View(viewModel);
            }
        }
        PrepareCreateViewModel(viewModel);
        return View(viewModel);
    }
    // GET: ManageMedia/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var media = await _context.Media
            .FirstOrDefaultAsync(m => m.Id == id && m.UserId == userId);

        if (media == null)
        {
            return NotFound();
        }

        var viewModel = new ManageMediaViewModel
        {
            Id = media.Id,
            FileName = media.FileName,
            Description = media.Description,
            Attribution = media.Attribution,
            MediaType = (int)media.MediaType,
            StorageProvider = (int)media.StorageProvider,
            Url = media.Url,
            MediaTypes = Enum.GetValues(typeof(MediaType))
                .Cast<MediaType>()
                .Select(mt => new SelectListItem
                {
                    Value = ((int)mt).ToString(),
                    Text = mt.ToString()
                }).ToList(),
            ContentType = media.ContentType,
            FileSize = media.FileSize,
            IsExternalLink = media.StorageProvider == StorageProviderNames.External || media.StorageProvider == StorageProviderNames.YouTube,
            ExternalUrl = media.Url
        };

        return View(viewModel);
    }

    // POST: ManageMedia/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ManageMediaViewModel viewModel)
    {
        if (id != viewModel.Id)
        {
            return NotFound();
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var media = await _context.Media
            .FirstOrDefaultAsync(m => m.Id == id && m.UserId == userId);

        if (media == null)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                // Update editable properties
                media.FileName = viewModel.FileName;
                media.Description = viewModel.Description;
                media.Attribution = viewModel.Attribution;
                media.MediaType = (MediaType)viewModel.MediaType;
                media.ModifiedDate = DateTime.UtcNow;

                // Update URL if external or YouTube
                if (viewModel.IsExternalLink && !string.IsNullOrEmpty(viewModel.ExternalUrl))
                {
                    // If changing to YouTube or updating YouTube URL
                    if ((StorageProviderNames)viewModel.StorageProvider == StorageProviderNames.YouTube)
                    {
                        if (!IsValidYouTubeUrl(viewModel.ExternalUrl))
                        {
                            ModelState.AddModelError("ExternalUrl", "Please enter a valid YouTube URL.");
                            PrepareEditViewModel(viewModel);
                            return View(viewModel);
                        }

                        media.Url = viewModel.ExternalUrl;
                        media.StorageProvider = StorageProviderNames.YouTube;
                        media.MediaType = MediaType.Video;
                    }
                    else if ((StorageProviderNames)viewModel.StorageProvider == StorageProviderNames.External)
                    {
                        media.Url = viewModel.ExternalUrl;
                    }
                }

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Media updated successfully.";
                return RedirectToAction(nameof(Details), new { id = media.Id });
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!MediaExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
        }

        PrepareEditViewModel(viewModel);
        return View(viewModel);
    }

    // GET: ManageMedia/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var media = await _context.Media
            .Include(m => m.User)
            .FirstOrDefaultAsync(m => m.Id == id && m.UserId == userId);

        if (media == null)
        {
            return NotFound();
        }

        return View(media);
    }

    // POST: ManageMedia/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var media = await _context.Media
            .FirstOrDefaultAsync(m => m.Id == id && m.UserId == userId);

        if (media == null)
        {
            return NotFound();
        }

        try
        {
            await _storageProviderService.DeleteFileAsync(media);
            TempData["SuccessMessage"] = "Media deleted successfully.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Error deleting media: {ex.Message}";
            return RedirectToAction(nameof(Delete), new { id = media.Id });
        }
    }

    // GET: ManageMedia/Download/5
    public async Task<IActionResult> Download(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var media = await _context.Media
            .FirstOrDefaultAsync(m => m.Id == id && m.UserId == userId);

        if (media == null)
        {
            return NotFound();
        }

        // External links and YouTube videos cannot be downloaded directly
        if (media.StorageProvider == StorageProviderNames.External ||
            media.StorageProvider == StorageProviderNames.YouTube)
        {
            return Redirect(media.Url);
        }

        try
        {
            var stream = await _storageProviderService.GetFileStreamAsync(media);
            return File(stream, media.ContentType, media.FileName);
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Error downloading media: {ex.Message}";
            return RedirectToAction(nameof(Details), new { id = media.Id });
        }
    }

    private bool MediaExists(int id)
    {
        return _context.Media.Any(e => e.Id == id);
    }

    private void PrepareCreateViewModel(ManageMediaViewModel viewModel)
    {
        viewModel.MediaTypes = Enum.GetValues(typeof(MediaType))
            .Cast<MediaType>()
            .Select(mt => new SelectListItem
            {
                Value = ((int)mt).ToString(),
                Text = mt.ToString(),
                Selected = viewModel.MediaType == (int)mt
            }).ToList();

        viewModel.StorageProviders = Enum.GetValues(typeof(StorageProviderNames))
            .Cast<StorageProviderNames>()
            .Select(sp => new SelectListItem
            {
                Value = ((int)sp).ToString(),
                Text = sp.ToString(),
                Selected = viewModel.StorageProvider == (int)sp
            }).ToList();
    }

    private void PrepareEditViewModel(ManageMediaViewModel viewModel)
    {
        PrepareCreateViewModel(viewModel);
    }

    private bool IsValidYouTubeUrl(string? url)
    {
        if (string.IsNullOrEmpty(url))
        {
            return false;
        }

        // Match standard YouTube URLs
        // Handles formats like:
        // - https://www.youtube.com/watch?v=VIDEO_ID
        // - https://youtu.be/VIDEO_ID
        // - https://youtube.com/watch?v=VIDEO_ID
        // - https://www.youtube.com/embed/VIDEO_ID
        var regex = new Regex(@"(?:https?:\/\/)?(?:www\.)?(?:youtube\.com\/(?:watch\?v=|embed\/)|youtu\.be\/)([a-zA-Z0-9_-]{11})");

        return regex.IsMatch(url);
    }

    private string GetYouTubeEmbedUrl(string url)
    {
        if (string.IsNullOrEmpty(url))
        {
            return string.Empty;
        }

        var regex = new Regex(@"(?:https?:\/\/)?(?:www\.)?(?:youtube\.com\/(?:watch\?v=|embed\/)|youtu\.be\/)([a-zA-Z0-9_-]{11})");
        var match = regex.Match(url);

        if (match.Success && match.Groups.Count > 1)
        {
            var videoId = match.Groups[1].Value;
            return $"https://www.youtube.com/embed/{videoId}";
        }

        return string.Empty;
    }
}


public class MediaIndexViewModel
{
    public IEnumerable<Media> Media { get; set; }
    public string SearchString { get; set; }
    public int? MediaTypeFilter { get; set; }
    public int? StorageProviderFilter { get; set; }
    public List<SelectListItem> MediaTypes { get; set; }
    public List<SelectListItem> StorageProviders { get; set; }
}

public class ManageMediaViewModel
{
    public ManageMediaViewModel()
    {
    }
    public ManageMediaViewModel(Media media)
    {
        Id = media.Id;
        FileName = media.FileName;
        MediaType = (int)media.MediaType;
        StorageProvider = (int)media.StorageProvider;
        Description = media.Description;
        Attribution = media.Attribution;
        IsExternalLink = media.StorageProvider == StorageProviderNames.External;
        IsYouTube = media.StorageProvider == StorageProviderNames.YouTube;
        ExternalUrl = media.StorageProvider == StorageProviderNames.External ? media.Url : string.Empty;
        YouTubeUrl = media.StorageProvider == StorageProviderNames.YouTube ? media.Url : string.Empty;
        ContentType = media.ContentType; FileSize = media.FileSize;
        Url = media.Url;
    }
    [Display(Name = "Is YouTube Video")]
    public bool IsYouTube { get; set; }

    [Display(Name = "YouTube URL")]
    [Url(ErrorMessage = "Please enter a valid YouTube URL")]
    public string? YouTubeUrl { get; set; }

    [Required(ErrorMessage = "File name is required")]
    [StringLength(255, ErrorMessage = "File name cannot exceed 255 characters")]
    [Display(Name = "File Name")]
    public string FileName { get; set; }

    [Required(ErrorMessage = "Media type is required")]
    [Display(Name = "Media Type")]
    public int MediaType { get; set; }

    public List<SelectListItem>? MediaTypes { get; set; } = [];

    [Display(Name = "Storage Provider")]
    public int StorageProvider { get; set; }

    public List<SelectListItem>? StorageProviders { get; set; }

    [StringLength(512, ErrorMessage = "Description cannot exceed 512 characters")]
    public string Description { get; set; }

    [StringLength(255, ErrorMessage = "Attribution cannot exceed 255 characters")]
    [Display(Name = "Attribution (if applicable)")]
    public string Attribution { get; set; }

    [Display(Name = "Is External Link")]
    public bool IsExternalLink { get; set; }

    [Display(Name = "File")]
    public IFormFile? File { get; set; }

    [Display(Name = "External URL")]
    [Url(ErrorMessage = "Please enter a valid URL")]
    public string? ExternalUrl { get; set; }
    public string? ContentType { get; set; } = string.Empty;
    public long? FileSize { get; set; }
    public string? Url { get; set; } = string.Empty;
    public int Id { get; set; }
}