using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Options;
using ShareSmallBiz.Portal.Areas.Media.Models;
using ShareSmallBiz.Portal.Areas.Media.Services;
using ShareSmallBiz.Portal.Data;
using ShareSmallBiz.Portal.Data.Enums;
using ShareSmallBiz.Portal.Infrastructure.Configuration;
using System.Security.Claims;

namespace ShareSmallBiz.Portal.Areas.Media.Controllers;

[Area("Media")]
[Authorize]
[Route("Media/Library")]
public class LibraryController : Controller
{
    private readonly ShareSmallBizUserContext _context;
    private readonly MediaService _mediaService;
    private readonly MediaFactoryService _mediaFactoryService;
    private readonly StorageProviderService _storageProviderService;
    private readonly ILogger<LibraryController> _logger;
    private readonly MediaStorageOptions _mediaOptions;

    public LibraryController(
        ShareSmallBizUserContext context,
        MediaService mediaService,
        MediaFactoryService mediaFactoryService,
        StorageProviderService storageProviderService,
        ILogger<LibraryController> logger,
        IOptions<MediaStorageOptions> mediaOptions)
    {
        _context = context;
        _mediaService = mediaService;
        _mediaFactoryService = mediaFactoryService;
        _storageProviderService = storageProviderService;
        _logger = logger;
        _mediaOptions = mediaOptions.Value;
    }

    // GET: /Media/Library
    [HttpGet]
    public async Task<IActionResult> Index(string? searchString, int? mediaTypeFilter, int? storageProviderFilter)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        IEnumerable<MediaModel> mediaItems;

        // Get base collection
        mediaItems = await _mediaService.GetUserMediaAsync(userId);

        // Apply search filter
        if (!string.IsNullOrEmpty(searchString))
        {
            mediaItems = await _mediaService.SearchMediaAsync(userId, searchString);
        }

        // Apply media type filter
        if (mediaTypeFilter.HasValue)
        {
            var mediaType = (MediaType)mediaTypeFilter.Value;
            mediaItems = mediaItems.Where(m => m.MediaType == mediaType);
        }

        // Apply storage provider filter
        if (storageProviderFilter.HasValue)
        {
            var storageProvider = (StorageProviderNames)storageProviderFilter.Value;
            mediaItems = mediaItems.Where(m => m.StorageProvider == storageProvider);
        }

        var viewModel = new MediaIndexViewModel
        {
            Media = mediaItems.OrderByDescending(m => m.CreatedDate).ToList(),
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
        var publicUrl = await _mediaFactoryService.GetMediaUrlAsync(media);
        ViewBag.PublicUrl = publicUrl;

        // Set YouTube embed URL if applicable
        if (media.StorageProvider == StorageProviderNames.YouTube)
        {
            string youtubeUrl = media.Url;
            ViewBag.YouTubeEmbedUrl = _storageProviderService.GetYouTubeEmbedUrlFromVideoUrl(youtubeUrl);
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
                // Validate file upload
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

                // Force storage provider to LocalStorage
                viewModel.StorageProvider = (int)StorageProviderNames.LocalStorage;
                viewModel.IsExternalLink = false;
                viewModel.IsYouTube = false;

                // If no filename was provided, use the original file name
                if (string.IsNullOrEmpty(viewModel.FileName))
                {
                    viewModel.FileName = viewModel.File.FileName;
                }

                // Create media using the file upload service directly
                var fileUploadService = HttpContext.RequestServices.GetRequiredService<FileUploadService>();
                var media = await fileUploadService.UploadFileAsync(
                    viewModel.File,
                    userId,
                    StorageProviderNames.LocalStorage,
                    viewModel.Description,
                    viewModel.Attribution);

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
            ExternalUrl = media.StorageProvider == StorageProviderNames.External ? media.Url : string.Empty,
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

        // Verify media exists and belongs to the user
        var mediaExists = await _mediaService.GetUserMediaByIdAsync(id, userId);
        if (mediaExists == null)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                // Update media using the factory service
                var success = await _mediaFactoryService.UpdateMediaAsync(id, viewModel, userId);

                if (success)
                {
                    TempData["SuccessMessage"] = "Media updated successfully.";
                    return RedirectToAction(nameof(Details), new { id });
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to update media.";
                    PrepareEditViewModel(viewModel);
                    return View(viewModel);
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"Error updating media: {ex.Message}");
                PrepareEditViewModel(viewModel);
                return View(viewModel);
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

        try
        {
            await _mediaFactoryService.DeleteMediaAsync(id, userId);
            TempData["SuccessMessage"] = "Media deleted successfully.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Error deleting media: {ex.Message}";
            return RedirectToAction(nameof(Delete), new { id });
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
            var stream = await _mediaFactoryService.GetFileStreamAsync(media);
            return File(stream, media.ContentType, media.FileName);
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Error downloading media: {ex.Message}";
            return RedirectToAction(nameof(Details), new { id = media.Id });
        }
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

        viewModel.StorageProviders =
        [
            new SelectListItem
            {
                Value = ((int)StorageProviderNames.LocalStorage).ToString(),
                Text = StorageProviderNames.LocalStorage.ToString(),
                Selected = viewModel.StorageProvider == (int)StorageProviderNames.LocalStorage
            }
        ];
    }

    private void PrepareEditViewModel(LibraryMediaViewModel viewModel)
    {
        PrepareCreateViewModel(viewModel);
    }
}
