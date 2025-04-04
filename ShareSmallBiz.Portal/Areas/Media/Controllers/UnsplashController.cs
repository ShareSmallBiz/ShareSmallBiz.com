using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShareSmallBiz.Portal.Areas.Media.Models;
using ShareSmallBiz.Portal.Areas.Media.Services;
using ShareSmallBiz.Portal.Data;
using System.Security.Claims;

namespace ShareSmallBiz.Portal.Areas.Media.Controllers;

[Area("Media")]
[Authorize]
[Route("Media/Unsplash")]
public class UnsplashController : Controller
{
    private readonly UnsplashService _unsplashService;
    private readonly ILogger<UnsplashController> _logger;

    public UnsplashController(
        UnsplashService unsplashService,
        ILogger<UnsplashController> logger)
    {
        _unsplashService = unsplashService;
        _logger = logger;
    }

    // GET: /Media/Unsplash
    [HttpGet]
    public IActionResult Index()
    {
        var viewModel = new UnsplashSearchViewModel();
        viewModel.PopularCategories = new List<string>
        {
            "business",
            "marketing",
            "office",
            "technology",
            "nature",
            "people",
            "food",
            "travel",
            "minimal"
        };

        return View(viewModel);
    }

    // GET: /Media/Unsplash/Search
    [HttpGet("Search")]
    public async Task<IActionResult> Search(string? query = "", int page = 1, int perPage = 9)
    {
        var viewModel = new UnsplashSearchViewModel
        {
            Query = query,
            Page = page,
            PerPage = perPage
        };

        if (!string.IsNullOrEmpty(query))
        {
            try
            {
                var searchResponse = await _unsplashService.SearchImagesAsync(query, page, perPage);
                if (searchResponse != null)
                {
                    viewModel.Photos = searchResponse.Results;
                    viewModel.TotalPages = searchResponse.TotalPages;
                    viewModel.TotalResults = searchResponse.Total;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching Unsplash");
                ModelState.AddModelError(string.Empty, $"Error searching Unsplash: {ex.Message}");
            }
        }

        return View(viewModel);
    }

    // POST: /Media/Unsplash/Search
    [HttpPost("Search")]
    public async Task<IActionResult> Search(UnsplashSearchViewModel viewModel)
    {
        if (string.IsNullOrWhiteSpace(viewModel.Query))
        {
            ModelState.AddModelError("Query", "Please enter a search term");
            return View(viewModel);
        }

        try
        {
            var searchResponse = await _unsplashService.SearchImagesAsync(viewModel.Query, viewModel.Page, viewModel.PerPage);
            if (searchResponse != null)
            {
                viewModel.Photos = searchResponse.Results;
                viewModel.TotalPages = searchResponse.TotalPages;
                viewModel.TotalResults = searchResponse.Total;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching Unsplash");
            ModelState.AddModelError(string.Empty, $"Error searching Unsplash: {ex.Message}");
        }

        return View(viewModel);
    }

    // GET: /Media/Unsplash/UserProfile/{username}
    // Updated route to include page parameter
    [HttpGet("UserProfile/{username}/{page:int?}")]
    public async Task<IActionResult> UserProfile(string username, int page = 1, int perPage = 9)
    {
        try
        {
            // Get user profile information
            var userProfile = await _unsplashService.GetUserProfileAsync(username);
            if (userProfile == null)
            {
                return NotFound();
            }

            // Get user photos
            var userPhotosResponse = await _unsplashService.GetUserPhotosAsync(username, page, perPage);

            var viewModel = new UnsplashUserViewModel
            {
                UserProfile = userProfile,
                Photos = userPhotosResponse?.Photos ?? new List<UnsplashPhoto>(),
                Page = page,
                PerPage = perPage,
                TotalPages = userPhotosResponse?.TotalPages ?? 1,
                TotalPhotos = userPhotosResponse?.Total ?? 0
            };

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Unsplash user details for {Username}", username);
            TempData["ErrorMessage"] = $"Error getting user details: {ex.Message}";
            return RedirectToAction(nameof(Index));
        }
    }

    // GET: /Media/Unsplash/Details/{id}
    [HttpGet("Details/{id}")]
    public async Task<IActionResult> Details(string id)
    {
        try
        {
            var photo = await _unsplashService.GetPhotoAsync(id);
            if (photo == null)
            {
                return NotFound();
            }

            var viewModel = new UnsplashPhotoViewModel
            {
                Photo = photo
            };

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Unsplash photo details");
            TempData["ErrorMessage"] = $"Error getting photo details: {ex.Message}";
            return RedirectToAction(nameof(Index));
        }
    }

    // POST: /Media/Unsplash/Save
    [HttpPost("Save")]
    public async Task<IActionResult> Save(string photoId, string? returnUrl = null)
    {
        if (string.IsNullOrEmpty(photoId))
        {
            return BadRequest("Photo ID is required");
        }

        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var photo = await _unsplashService.GetPhotoAsync(photoId);

            if (photo == null)
            {
                return NotFound("Photo not found");
            }

            // Create the media entry as external link only
            var media = await _unsplashService.CreateUnsplashMediaAsync(photo, userId);

            TempData["SuccessMessage"] = "Unsplash photo added successfully to your library.";

            // Redirect back to referring page if provided
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Details", "Library", new { id = media.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving Unsplash photo");
            TempData["ErrorMessage"] = $"Error saving photo: {ex.Message}";
            return RedirectToAction("Search");
        }
    }

    // API endpoint for AJAX search
    [HttpGet("api/search")]
    public async Task<IActionResult> ApiSearch([FromQuery] string query, [FromQuery] int page = 1, [FromQuery] int perPage = 10)
    {
        try
        {
            var searchResponse = await _unsplashService.SearchImagesAsync(query, page, perPage);
            return Ok(searchResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Unsplash API search");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    // API endpoint for AJAX user photos
    [HttpGet("api/user/{username}")]
    public async Task<IActionResult> ApiUserPhotos([FromRoute] string username, [FromQuery] int page = 1, [FromQuery] int perPage = 10)
    {
        try
        {
            var userPhotosResponse = await _unsplashService.GetUserPhotosAsync(username, page, perPage);
            return Ok(userPhotosResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Unsplash API user photos request");
            return StatusCode(500, new { error = ex.Message });
        }
    }
}