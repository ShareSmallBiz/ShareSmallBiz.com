using HttpClientUtility.MemoryCache;
using HttpClientUtility.RequestResult;
using HttpClientUtility.StringConverter;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using ShareSmallBiz.Portal.Data;
using ShareSmallBiz.Portal.Infrastructure.Services;
using ShareSmallBiz.Portal.Infrastructure.Utilities;

namespace ShareSmallBiz.Portal.Infrastructure.Extensions;


public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddHttpClientUtilities(this IServiceCollection services, IConfiguration configuration)
    {
        // Register the HttpClient service.
        services.AddHttpClient("HttpClientService", client =>
        {
            client.Timeout = TimeSpan.FromMilliseconds(90000);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            client.DefaultRequestHeaders.Add("User-Agent", "HttpClientService");
            client.DefaultRequestHeaders.Add("X-Request-ID", Guid.NewGuid().ToString());
            client.DefaultRequestHeaders.Add("X-Request-Source", "HttpClientService");
        });

        // Register the HTTP Send Service using the Decorator Pattern.
        services.AddSingleton(serviceProvider =>
        {
            IHttpRequestResultService baseService = new HttpRequestResultService(
                serviceProvider.GetRequiredService<ILogger<HttpRequestResultService>>(),
                serviceProvider.GetRequiredService<IHttpClientFactory>().CreateClient("HttpClientDecorator"));

            var retryOptions = configuration.GetSection("HttpRequestResultPollyOptions").Get<HttpRequestResultPollyOptions>();

            IHttpRequestResultService pollyService = new HttpRequestResultServicePolly(
                serviceProvider.GetRequiredService<ILogger<HttpRequestResultServicePolly>>(),
                baseService,
                retryOptions);

            IHttpRequestResultService telemetryService = new HttpRequestResultServiceTelemetry(
                serviceProvider.GetRequiredService<ILogger<HttpRequestResultServiceTelemetry>>(),
                pollyService);

            IHttpRequestResultService cacheService = new HttpRequestResultServiceCache(
                telemetryService,
                serviceProvider.GetRequiredService<ILogger<HttpRequestResultServiceCache>>(),
                serviceProvider.GetRequiredService<IMemoryCache>());

            return cacheService;
        });

        // Register the YouTubeApi service.
        services.Configure<YouTubeApiOptions>(configuration.GetSection("YouTubeApi"));
        services.AddHttpClient("YouTubeApi", client =>
        {
            var options = configuration.GetSection("YouTubeApi").Get<YouTubeApiOptions>();
            client.BaseAddress = new Uri(options.BaseUrl);
        });
        services.AddScoped<IYouTubeService, YouTubeService>();


        return services;
    }

    public static IServiceCollection AddCachingServices(this IServiceCollection services)
    {
        services.AddDistributedMemoryCache();
        services.AddSingleton<IMemoryCacheManager, MemoryCacheManager>();
        return services;
    }
    public static IServiceCollection AddDatabaseContexts(this IServiceCollection services, IConfiguration configuration)
    {
        var adminConnectionString = configuration.GetValue<string>("ShareSmallBizUserContext")
                                    ?? "Data Source=c:\\websites\\ShareSmallBiz\\ShareSmallBizUser.db";
        services.AddDbContext<ShareSmallBizUserContext>(options =>
        {
            options.UseSqlite(adminConnectionString, dbOptions =>
            {
                dbOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
            });
            options.ConfigureWarnings(warnings =>
                warnings.Throw(RelationalEventId.MultipleCollectionIncludeWarning));
        });
        return services;
    }
}
