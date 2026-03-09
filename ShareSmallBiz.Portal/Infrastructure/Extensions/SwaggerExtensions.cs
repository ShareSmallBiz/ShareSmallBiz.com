using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace ShareSmallBiz.Portal.Infrastructure.Extensions
{
    public static class OpenApiExtensions
    {
        public static IServiceCollection AddOpenApiDocumentation(this IServiceCollection services)
        {
            services.AddOpenApi("v1", options =>
            {
                options.AddDocumentTransformer((document, context, cancellationToken) =>
                {
                    document.Info = new OpenApiInfo
                    {
                        Version = "v1",
                        Title = "ShareSmallBiz API",
                        Description = "API documentation for ShareSmallBiz Portal",
                        Contact = new OpenApiContact
                        {
                            Name = "Support",
                            Email = "support@sharesmallbiz.com",
                            Url = new Uri("https://sharesmallbiz.com/")
                        }
                    };

                    // Only include routes that start with /api/
                    if (document.Paths != null)
                    {
                        var apiPaths = document.Paths
                            .Where(p => p.Key.StartsWith("/api/", StringComparison.OrdinalIgnoreCase))
                            .ToList();

                        document.Paths.Clear();
                        foreach (var path in apiPaths)
                            document.Paths.Add(path.Key, path.Value);
                    }

                    return Task.CompletedTask;
                });
            });

            return services;
        }
    }
}
