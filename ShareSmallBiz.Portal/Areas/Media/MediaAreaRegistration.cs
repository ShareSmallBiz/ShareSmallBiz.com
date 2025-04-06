using ShareSmallBiz.Portal.Areas.Media.Services;
using ShareSmallBiz.Portal.Infrastructure.Configuration;

namespace ShareSmallBiz.Portal.Areas.Media;

/// <summary>
/// MediaEntity Area Registration
/// </summary>
public static class MediaAreaRegistration
{
    /// <summary>
    /// Register services required for the MediaEntity area
    /// </summary>
    public static IServiceCollection AddMediaServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Register MediaStorageOptions from configuration
        services.Configure<MediaStorageOptions>(
            configuration.GetSection(MediaStorageOptions.MediaStorage));

        // Register core services
        services.AddScoped<MediaService>();
        services.AddScoped<StorageProviderService>();

        // Register specialized services
        services.AddScoped<FileUploadService>();
        services.AddScoped<YouTubeMediaService>();
        services.AddScoped<YouTubeService>();
        services.AddScoped<UnsplashService>();

        // Register factory service
        services.AddScoped<MediaFactoryService>();

        // Register HttpClient for external APIs
        services.AddHttpClient();

        return services;
    }

    /// <summary>
    /// Register controllers and endpoints for the MediaEntity area
    /// </summary>
    public static IEndpointRouteBuilder MapMediaEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapControllerRoute(
            name: "media_area",
            pattern: "{area:exists}/{controller=MediaEntity}/{action=Index}/{id?}");

        endpoints.MapControllerRoute(
            name: "media",
            pattern: "MediaEntity/{id?}",
            defaults: new { controller = "MediaEntity", action = "Index" });

        return endpoints;
    }
}