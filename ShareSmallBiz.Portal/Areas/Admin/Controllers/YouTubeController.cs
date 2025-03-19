using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Polly;
using ShareSmallBiz.Portal.Data;
using ShareSmallBiz.Portal.Infrastructure.Services;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
namespace ShareSmallBiz.Portal.Areas.Admin.Controllers;

[Route("admin/[controller]")]
public class YouTubeController(
    ShareSmallBizUserContext _context,
    ShareSmallBizUserManager _userManager,
    IYouTubeService _youTubeService,
    IMemoryCache _memoryCache,
    ILogger<YouTubeController> _logger,
    RoleManager<IdentityRole> _roleManager) : AdminBaseController(_context, _userManager, _roleManager)
{
    private const string DefaultChannelId = "UCWy4-89rNbDI_HGUCB8pkBA";
    private const string CacheKeyPrefix = "VideoDetailsViewModel_";
    private const int DefaultCacheExpirationMinutes = 30;
    private string CurrentChannelId = DefaultChannelId;

    private async Task<VideoDetailsViewModel> GetVideoDetailsViewModel(string channelId = DefaultChannelId, int maxResults = 20)
    {
        CurrentChannelId = channelId ??= DefaultChannelId;

        var cacheKey = $"{CacheKeyPrefix}{channelId}_{maxResults}";

        if (!_memoryCache.TryGetValue(cacheKey, out VideoDetailsViewModel returnModel))
        {
            returnModel = new VideoDetailsViewModel
            {
                CurrentChannel = await _youTubeService.GetChannelDetailsAsync(CurrentChannelId),
                RelatedVideos = await _youTubeService.GetChannelVideosAsync(CurrentChannelId, maxResults)
            };

            var cacheOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(DefaultCacheExpirationMinutes));

            _memoryCache.Set(cacheKey, returnModel, cacheOptions);
        }
        return returnModel;
    }

    public async Task<IActionResult> Index(string channelId = DefaultChannelId, int maxResults = 20)
    {
        return View("Index", await GetVideoDetailsViewModel(channelId, maxResults));
    }
    [HttpGet]
    [Route("channel/{channelId}")]
    public async Task<IActionResult> ChannelVideos(string channelId = DefaultChannelId, int maxResults = 20)
    {
        return View("Index", await GetVideoDetailsViewModel(channelId, maxResults));
    }

    [HttpGet]
    [Route("channel/{channelId}/{videoId}")]
    public async Task<IActionResult> VideoDetails(string channelId, string videoId)
    {
        if (string.IsNullOrEmpty(videoId))
        {
            return RedirectToAction("ChannelVideos", new { channelId });
        }
        var channel = await GetVideoDetailsViewModel(channelId, 20);
        channel.CurrentVideo = channel.RelatedVideos.Find(v => v.VideoId == videoId);
        if (channel.CurrentVideo == null)
        {
            _logger.LogError($"Video with ID {videoId} not found in channel {channelId}");
            return RedirectToAction("ChannelVideos", new { channelId });
        }
        return View(channel);
    }
}

public class VideoDetailsViewModel
{
    public YouTubeChannel CurrentChannel { get; set; }
    public YouTubeVideo CurrentVideo { get; set; }
    public List<YouTubeVideo> RelatedVideos { get; set; } = [];
}