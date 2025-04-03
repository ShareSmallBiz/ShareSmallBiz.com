using ShareSmallBiz.Portal.Areas.Media.Services;

namespace ShareSmallBiz.Portal.Areas.Media;

/// <summary>
/// Media Area Registration
/// </summary>
public static class MediaAreaRegistration
{
    /// <summary>
    /// Register services required for the Media area
    /// </summary>
    public static IServiceCollection AddMediaServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Register MediaStorageOptions from configuration
        services.Configure<Infrastructure.Configuration.MediaStorageOptions>(
            configuration.GetSection(Infrastructure.Configuration.MediaStorageOptions.MediaStorage));

        // Register MediaService
        services.AddScoped<MediaService>();

        // Register YouTubeService
        services.AddScoped<Services.YouTubeService>();

        // Register HttpClient for YouTube API
        services.AddHttpClient();

        return services;
    }

    /// <summary>
    /// Register controllers and endpoints for the Media area
    /// </summary>
    public static IEndpointRouteBuilder MapMediaEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapControllerRoute(
            name: "media_area",
            pattern: "{area:exists}/{controller=Media}/{action=Index}/{id?}");

        endpoints.MapControllerRoute(
            name: "media",
            pattern: "Media/{id?}",
            defaults: new { controller = "Media", action = "Index" });
        return endpoints;


    }
}