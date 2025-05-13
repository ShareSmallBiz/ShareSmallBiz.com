using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Caching.Memory;
using ShareSmallBiz.Portal.Data;
using ShareSmallBiz.Portal.Infrastructure.Services;
using WebSpark.HttpClientUtility.MemoryCache;
using WebSpark.HttpClientUtility.RequestResult;

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
                serviceProvider.GetRequiredService<IConfiguration>(),
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

    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        // Register application services
        services.AddScoped<UserProvider>();
        services.AddScoped<ProfileImageService>();
        
        return services;
    }
}
