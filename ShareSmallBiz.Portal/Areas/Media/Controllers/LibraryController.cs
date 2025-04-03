using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Options;
using ShareSmallBiz.Portal.Areas.Media.Services;
using ShareSmallBiz.Portal.Data;
using ShareSmallBiz.Portal.Infrastructure.Configuration;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace ShareSmallBiz.Portal.Areas.Media.Controllers;

[Area("Media")]
[Authorize]
[Route("Media/Library")]
public class LibraryController : Controller
{
    private readonly ShareSmallBizUserContext _context;
    private readonly MediaService _mediaService;
    private readonly StorageProviderService _storageProviderService;
    private readonly ILogger<LibraryController> _logger;
    private readonly MediaStorageOptions _mediaOptions;

    public LibraryController(
        ShareSmallBizUserContext context,
        MediaService mediaService,
        StorageProviderService storageProviderService,
        ILogger<LibraryController> logger,
        IOptions<MediaStorageOptions> mediaOptions)
    {
        _context = context;
        _mediaService = mediaService;
        _storageProviderService = storageProviderService;
        _logger = logger;
        _mediaOptions = mediaOptions.Value;
    }

    // GET: /Media/Library
    [HttpGet]
    public async Task<IActionResult> Index(string? searchString, int? mediaTypeFilter, int? storageProviderFilter)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

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
            var storageProvider = (StorageProviderNames)storageProviderFilter.Value;
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

    // GET: /Media/Library/Details/5
    [HttpGet("Details/{id:int}")]
    public async Task<IActionResult> Details(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var media = await _mediaService.GetUserMediaByIdAsync(id, userId);

        if (media == null)
        {
            return NotFound();
        }

        // Get public URL
        var publicUrl = await _mediaService.GetMediaUrlAsync(media);
        ViewBag.PublicUrl = publicUrl;

        // Set YouTube embed URL if applicable
        if (media.StorageProvider == StorageProviderNames.YouTube)
        {
            ViewBag.YouTubeEmbedUrl = GetYouTubeEmbedUrl(media.Url);
        }

        var vm = new LibraryMediaViewModel(media);
        return View(vm);
    }

    // GET: /Media/Library/Create
    [HttpGet("Create")]
    public IActionResult Create()
    {
        var viewModel = new LibraryMediaViewModel
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

    // POST: /Media/Library/Create
    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(LibraryMediaViewModel viewModel)
    {
        if (ModelState.IsValid)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            try
            {
                ShareSmallBiz.Portal.Data.Media media;
                var videoId = IsValidYouTubeUrl(viewModel.YouTubeUrl);

                if (!string.IsNullOrEmpty(videoId))
                {
                    viewModel.YouTubeVideoId = videoId;
                    viewModel.IsExternalLink = true;
                    viewModel.IsYouTube = true;
                }
                else
                {
                    viewModel.IsYouTube = false;
                    viewModel.YouTubeUrl = string.Empty;
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
                    // Validate external URL
                    if (string.IsNullOrEmpty(viewModel.ExternalUrl))
                    {
                        ModelState.AddModelError("ExternalUrl", "Please enter a URL.");
                        PrepareCreateViewModel(viewModel);
                        return View(viewModel);
                    }

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
                    // Upload file
                    if (viewModel.File == null || viewModel.File.Length == 0)
                    {
                        ModelState.AddModelError("File", "Please select a file to upload.");
                        PrepareCreateViewModel(viewModel);
                        return View(viewModel);
                    }

                    // Check file size
                    if (viewModel.File.Length > _mediaOptions.MaxFileSize)
                    {
                        ModelState.AddModelError("File", $"File size exceeds the maximum allowed size of {_mediaOptions.MaxFileSize / (1024 * 1024)}MB.");
                        PrepareCreateViewModel(viewModel);
                        return View(viewModel);
                    }

                    // Check file extension
                    var extension = Path.GetExtension(viewModel.File.FileName).ToLowerInvariant();
                    if (!_mediaOptions.AllowedExtensions.Contains(extension))
                    {
                        ModelState.AddModelError("File", $"File type {extension} is not allowed.");
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
                ModelState.AddModelError(string.Empty, $"Error creating media: {ex.Message}");
                PrepareCreateViewModel(viewModel);
                return View(viewModel);
            }
        }

        PrepareCreateViewModel(viewModel);
        return View(viewModel);
    }

    // GET: /Media/Library/Edit/5
    [HttpGet("Edit/{id:int}")]
    public async Task<IActionResult> Edit(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var media = await _mediaService.GetUserMediaByIdAsync(id, userId);

        if (media == null)
        {
            return NotFound();
        }

        var viewModel = new LibraryMediaViewModel
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
            IsYouTube = media.StorageProvider == StorageProviderNames.YouTube,
            ExternalUrl = media.Url,
            YouTubeUrl = media.StorageProvider == StorageProviderNames.YouTube ? media.Url : string.Empty
        };

        return View(viewModel);
    }

    // POST: /Media/Library/Edit/5
    [HttpPost("Edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, LibraryMediaViewModel viewModel)
    {
        if (id != viewModel.Id)
        {
            return NotFound();
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var media = await _mediaService.GetUserMediaByIdAsync(id, userId);

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
                    if (viewModel.IsYouTube)
                    {
                        var videoId = IsValidYouTubeUrl(viewModel.YouTubeUrl);
                        if (!string.IsNullOrEmpty(videoId))
                        {
                            ModelState.AddModelError("ExternalUrl", "Please enter a valid YouTube URL.");
                            PrepareEditViewModel(viewModel);
                            return View(viewModel);
                        }

                        media.Url = viewModel.ExternalUrl;
                        media.StorageProvider = StorageProviderNames.YouTube;
                        media.MediaType = MediaType.Video;
                    }
                    else
                    {
                        media.Url = viewModel.ExternalUrl;
                        media.StorageProvider = StorageProviderNames.External;
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

    // GET: /Media/Library/Delete/5
    [HttpGet("Delete/{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var media = await _mediaService.GetUserMediaByIdAsync(id, userId);

        if (media == null)
        {
            return NotFound();
        }

        return View(media);
    }

    // POST: /Media/Library/Delete/5
    [HttpPost("Delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var media = await _mediaService.GetUserMediaByIdAsync(id, userId);

        if (media == null)
        {
            return NotFound();
        }

        try
        {
            await _mediaService.DeleteMediaAsync(media);
            TempData["SuccessMessage"] = "Media deleted successfully.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Error deleting media: {ex.Message}";
            return RedirectToAction(nameof(Delete), new { id = media.Id });
        }
    }

    // GET: /Media/Library/Download/5
    [HttpGet("Download/{id:int}")]
    public async Task<IActionResult> Download(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var media = await _mediaService.GetUserMediaByIdAsync(id, userId);

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
            var stream = await _mediaService.GetFileStreamAsync(media);
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

    private void PrepareCreateViewModel(LibraryMediaViewModel viewModel)
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

    private void PrepareEditViewModel(LibraryMediaViewModel viewModel)
    {
        PrepareCreateViewModel(viewModel);
    }

    private string? IsValidYouTubeUrl(string? url)
    {
        if (string.IsNullOrEmpty(url))
        {
            return null;
        }

        // Match standard YouTube URLs
        // Handles formats like:
        // - https://www.youtube.com/watch?v=VIDEO_ID
        // - https://youtu.be/VIDEO_ID
        // - https://youtube.com/watch?v=VIDEO_ID
        // - https://www.youtube.com/embed/VIDEO_ID
        var regex = new Regex(@"(?:https?:\/\/)?(?:www\.)?(?:youtube\.com\/(?:watch\?v=|embed\/)|youtu\.be\/)([a-zA-Z0-9_-]{11})");

        var match = regex.Match(url);
        if (match.Success && match.Groups.Count > 1)
        {
            return match.Groups[1].Value; // Return the VIDEO_ID
        }

        return null; // Return null if not a valid YouTube URL
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
    public IEnumerable<ShareSmallBiz.Portal.Data.Media> Media { get; set; } = [];
    public string? SearchString { get; set; }
    public int? MediaTypeFilter { get; set; }
    public int? StorageProviderFilter { get; set; }
    public List<SelectListItem> MediaTypes { get; set; } = [];
    public List<SelectListItem> StorageProviders { get; set; } = [];
}

public class LibraryMediaViewModel
{
    public LibraryMediaViewModel()
    {
    }

    public LibraryMediaViewModel(ShareSmallBiz.Portal.Data.Media media)
    {
        Id = media.Id;
        FileName = media.FileName;
        MediaType = (int)media.MediaType;
        StorageProvider = (int)media.StorageProvider;
        Description = media.Description;
        Attribution = media.Attribution;
        IsExternalLink = media.StorageProvider == StorageProviderNames.External || media.StorageProvider == StorageProviderNames.YouTube;
        IsYouTube = media.StorageProvider == StorageProviderNames.YouTube;
        ExternalUrl = media.StorageProvider == StorageProviderNames.External ? media.Url : string.Empty;
        YouTubeUrl = media.StorageProvider == StorageProviderNames.YouTube ? media.Url : string.Empty;
        ContentType = media.ContentType;
        FileSize = media.FileSize;
        Url = media.Url;
    }

    public int Id { get; set; }

    [Display(Name = "Is YouTube Video")]
    public bool IsYouTube { get; set; }

    [Display(Name = "YouTube URL")]
    [Url(ErrorMessage = "Please enter a valid YouTube URL")]
    public string? YouTubeUrl { get; set; }

    [Required(ErrorMessage = "File name is required")]
    [StringLength(255, ErrorMessage = "File name cannot exceed 255 characters")]
    [Display(Name = "File Name")]
    public string FileName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Media type is required")]
    [Display(Name = "Media Type")]
    public int MediaType { get; set; }

    public List<SelectListItem>? MediaTypes { get; set; } = [];

    [Display(Name = "Storage Provider")]
    public int StorageProvider { get; set; }

    public List<SelectListItem>? StorageProviders { get; set; } = [];

    [StringLength(512, ErrorMessage = "Description cannot exceed 512 characters")]
    public string Description { get; set; } = string.Empty;

    [StringLength(255, ErrorMessage = "Attribution cannot exceed 255 characters")]
    [Display(Name = "Attribution (if applicable)")]
    public string Attribution { get; set; } = string.Empty;

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
    public string? YouTubeVideoId { get; set; } = string.Empty;
}