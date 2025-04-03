using ShareSmallBiz.Portal.Infrastructure.Services;

namespace ShareSmallBiz.Portal.Controllers;

public class HomeController(DiscussionProvider postProvider, ILogger<HomeController> logger) : Controller
{
    public async Task<IActionResult> Index()
    {
        return View(await postProvider.FeaturedPostsAsync(3));
    }
    public IActionResult ContentModerationPolicy()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }
    public IActionResult Terms()
    {
        return View();
    }
    public IActionResult KitchenSink()
    {
        return View();
    }

    [HttpGet("error/{*slug}")]
    public IActionResult GetError(string? slug)
    {
        var attemptedPath = HttpContext.Request.Path.Value;
        var queryString = HttpContext.Request.QueryString.Value;
        logger.LogError("Catch All: Attempted path: {AttemptedPath}{queryString}", attemptedPath, queryString);
        return RedirectToAction("Index", "Home");
    }


    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
