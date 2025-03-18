using Microsoft.AspNetCore.Mvc.Rendering;
using ShareSmallBiz.Portal.Data;
using ShareSmallBiz.Portal.Infrastructure.Services;
using ShareSmallBiz.Portal.Services;
using System.Security.Claims;

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

                if (viewModel.IsExternalLink)
                {
                    // Create external link
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
                    // Upload file
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
        return RedirectToAction("Index");
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
            FileSize = media.FileSize
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

        // Repopulate dropdown lists
        viewModel.MediaTypes = Enum.GetValues(typeof(MediaType))
            .Cast<MediaType>()
            .Select(mt => new SelectListItem
            {
                Value = ((int)mt).ToString(),
                Text = mt.ToString()
            }).ToList();

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

        // External links cannot be downloaded directly
        if (media.StorageProvider == StorageProviderNames.External)
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
        ExternalUrl = media.Url;
        ContentType = media.ContentType;
        FileSize = media.FileSize;
        Url = media.Url;
    }

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
    public IFormFile File { get; set; }

    [Display(Name = "External URL")]
    [Url(ErrorMessage = "Please enter a valid URL")]
    public string? ExternalUrl { get; set; }
    public string? ContentType { get; set; } = string.Empty;
    public long? FileSize { get; set; }
    public string? Url { get; set; } = string.Empty;
    public int Id { get; set; }
}

