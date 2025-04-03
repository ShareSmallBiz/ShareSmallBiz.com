namespace ShareSmallBiz.Portal.Areas.Media.Controllers;

[Area("Media")]
[Route("unsplash")]
[ApiController]
public class UnsplashMediaController(IHttpClientFactory httpClientFactory, IConfiguration configuration) : ControllerBase
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient();
    private readonly string _unsplashKey = configuration["Unsplash:AccessKey"];

    [HttpGet("search")]
    public async Task<IActionResult> SearchImages([FromQuery] string query)
    {
        if (string.IsNullOrEmpty(_unsplashKey))
        {
            return BadRequest("Unsplash API key is not configured.");
        }

        var requestUrl = $"https://api.unsplash.com/search/photos?query={query}&per_page=8&client_id={_unsplashKey}";

        var response = await _httpClient.GetAsync(requestUrl);
        if (!response.IsSuccessStatusCode)
        {
            return StatusCode((int)response.StatusCode, "Error fetching images from Unsplash.");
        }

        var jsonResponse = await response.Content.ReadAsStringAsync();
        return Content(jsonResponse, "application/json");
    }
}
