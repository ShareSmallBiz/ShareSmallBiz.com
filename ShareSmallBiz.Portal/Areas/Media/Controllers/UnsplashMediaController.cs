using Microsoft.AspNetCore.Mvc;
using ShareSmallBiz.Portal.Areas.Media.Services;

namespace ShareSmallBiz.Portal.Areas.Media.Controllers;

[Area("Media")]
[Route("unsplash/api")]
[ApiController]
public class UnsplashMediaController : ControllerBase
{
    private readonly UnsplashService _unsplashService;
    private readonly ILogger<UnsplashMediaController> _logger;

    public UnsplashMediaController(
        UnsplashService unsplashService,
        ILogger<UnsplashMediaController> logger)
    {
        _unsplashService = unsplashService;
        _logger = logger;
    }

    [HttpGet("search")]
    public async Task<IActionResult> SearchImages([FromQuery] string query, [FromQuery] int page = 1, [FromQuery] int perPage = 10)
    {
        try
        {
            var searchResponse = await _unsplashService.SearchImagesAsync(query, page, perPage);
            return Ok(searchResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching Unsplash images: {Message}", ex.Message);
            return StatusCode(500, new { error = "Error searching Unsplash images" });
        }
    }

    [HttpGet("photo/{id}")]
    public async Task<IActionResult> GetPhoto(string id)
    {
        try
        {
            var photo = await _unsplashService.GetPhotoAsync(id);
            if (photo == null)
            {
                return NotFound();
            }
            return Ok(photo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Unsplash photo details: {Message}", ex.Message);
            return StatusCode(500, new { error = "Error getting photo details" });
        }
    }
}