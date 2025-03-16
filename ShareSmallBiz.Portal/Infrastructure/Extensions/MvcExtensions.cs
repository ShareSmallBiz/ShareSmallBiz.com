namespace ShareSmallBiz.Portal.Infrastructure.Extensions;

public static class MvcExtensions
{
    public static IServiceCollection AddMvcServices(this IServiceCollection services)
    {
        services.AddControllersWithViews(options =>
        {
        });

        services.AddRazorPages();

        return services;
    }

    public static IServiceCollection AddCorsPolicy(this IServiceCollection services, IConfiguration configuration)
    {
        // Ideally get allowed origins from configuration
        var allowedOrigins = configuration.GetSection("AllowedOrigins").Get<string[]>()
            ?? new[] { "https://yourdomain.com" };

        services.AddCors(options =>
        {
            options.AddPolicy("ApiCorsPolicy", builder =>
            {
                builder.WithOrigins(allowedOrigins)
                       .AllowAnyMethod()
                       .AllowAnyHeader()
                       .AllowCredentials();
            });
        });

        return services;
    }

    public static IServiceCollection AddRealTimeServices(this IServiceCollection services)
    {
        services.AddSignalR().AddJsonProtocol(options =>
        {
            // Use camelCase by default (JSON standard)
            // options.PayloadSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;

            // Or keep Pascal case if required by your client applications
            options.PayloadSerializerOptions.PropertyNamingPolicy = null;
        });

        return services;
    }
}