using Microsoft.AspNetCore.Mvc;
using ShareSmallBiz.Portal.Data;
using ShareSmallBiz.Portal.Infrastructure.Services;

namespace ShareSmallBiz.Portal.Areas.Admin.Controllers;

public class YouTubeController(
    ShareSmallBizUserContext _context,
    ShareSmallBizUserManager _userManager,
    IYouTubeService _youTubeService,
    RoleManager<IdentityRole> _roleManager) : AdminBaseController(_context, _userManager, _roleManager)
{
    public async Task<IActionResult> Index(string channelId = "UCWy4-89rNbDI_HGUCB8pkBA", int maxResults = 20)
    {
        var videos = await _youTubeService.GetChannelVideosAsync(channelId, maxResults);
        return View(videos);
    }

    public async Task<IActionResult> ChannelVideos(string channelId = "UCWy4-89rNbDI_HGUCB8pkBA", int maxResults = 20)
    {
        var videos = await _youTubeService.GetChannelVideosAsync(channelId, maxResults);
        return View("Index",videos);
    }
    public async Task<IActionResult> VideoDetails(string videoId, string channelId = "UCWy4-89rNbDI_HGUCB8pkBA")
    {
        if (string.IsNullOrEmpty(videoId))
        {
            return RedirectToAction("ChannelVideos", new { channelId });
        }

        // Get all videos to find the current one and related videos
        var allVideos = await _youTubeService.GetChannelVideosAsync(channelId, 20);

        // Find the current video
        var currentVideo = allVideos.Find(v => v.VideoId == videoId);

        if (currentVideo == null)
        {
            return NotFound();
        }

        // Create view model with current video and related videos
        var viewModel = new VideoDetailsViewModel
        {
            CurrentVideo = currentVideo,
            RelatedVideos = allVideos.FindAll(v => v.VideoId != videoId).Take(5).ToList()
        };

        return View(viewModel);
    }
}







public class VideoDetailsViewModel
{
    public YouTubeVideo CurrentVideo { get; set; }
    public List<YouTubeVideo> RelatedVideos { get; set; } = new List<YouTubeVideo>();
}