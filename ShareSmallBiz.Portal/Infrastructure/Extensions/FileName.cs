using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using ShareSmallBiz.Portal.Data;
using ShareSmallBiz.Portal.Infrastructure.Services;
using System;
using System.Linq;
using System.Xml.Linq;

namespace ShareSmallBiz.Portal.Infrastructure.Extensions;

public static class SitemapExtensions
{
    /// <summary>
    /// Maps an endpoint to serve the sitemap.xml.
    /// Excludes API and Swagger endpoints from the sitemap and adds dynamic user and discussion routes.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <returns>The endpoint route builder.</returns>
    public static IEndpointRouteBuilder MapSitemap(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/sitemap.xml", async (
            EndpointDataSource endpointDataSource,
            IHttpContextAccessor httpContextAccessor,
            IMemoryCache cache,
            ILogger<Program> logger,
            ShareSmallBizUserContext dbContext,
            DiscussionProvider discussionProvider) =>
        {
            const string cacheKey = "sitemap_xml";
            if (!cache.TryGetValue(cacheKey, out string sitemapXml))
            {
                // Build the base URL from the current request context.
                var request = httpContextAccessor.HttpContext?.Request;
                var host = request?.Host.Value ?? "localhost";
                var scheme = request?.Scheme ?? "https";
                var baseUrl = $"{scheme}://{host}";

                // Retrieve static endpoints that are RouteEndpoints.
                var endpointsList = endpointDataSource.Endpoints.OfType<RouteEndpoint>();

                // Filter endpoints to exclude those that:
                // - Are null or empty.
                // - Start with "swagger" (Swagger UI endpoints).
                // - Start with "api" (API endpoints).
                var staticRoutes = endpointsList
                    .Where(e =>
                    {
                        var template = e.RoutePattern.RawText;
                        if (string.IsNullOrEmpty(template))
                            return false;

                        // Trim any leading slash for a consistent check.
                        template = template.TrimStart('/');
                        return !template.StartsWith("swagger", StringComparison.OrdinalIgnoreCase) &&
                               !template.Contains("{") &&
                               !template.StartsWith("api", StringComparison.OrdinalIgnoreCase) &&
                               !template.StartsWith("Comments", StringComparison.OrdinalIgnoreCase) &&
                               !template.StartsWith("Discussions", StringComparison.OrdinalIgnoreCase) &&
                               !template.StartsWith("Error", StringComparison.OrdinalIgnoreCase) &&
                               !template.StartsWith("Forum", StringComparison.OrdinalIgnoreCase) &&
                               !template.StartsWith("Profiles", StringComparison.OrdinalIgnoreCase) &&
                               !template.StartsWith("identity", StringComparison.OrdinalIgnoreCase);

                    })
                    .Select(e => e.RoutePattern.RawText.TrimStart('/'))
                    .Distinct();

                // Create XML elements for each static route.
                var urlElements = new List<XElement>();
                foreach (var route in staticRoutes)
                {
                    var fullUrl = $"{baseUrl}/{route}";
                    urlElements.Add(new XElement("url",
                        new XElement("loc", fullUrl),
                        new XElement("changefreq", "weekly"),
                        new XElement("priority", "0.5")));
                }

                // Dynamic routes for profiles: /profiles/{username}
                var users = await dbContext.Users.ToListAsync();
                foreach (var user in users)
                {
                    // Assuming user.UserName holds the username.
                    if (!string.IsNullOrWhiteSpace(user.UserName))
                    {
                        if (user.UserName.ToLower() != user.Email.ToLower())
                        {
                            var profileUrl = $"{baseUrl}/profiles/{user.UserName}";
                            urlElements.Add(new XElement("url",
                                new XElement("loc", profileUrl),
                                new XElement("changefreq", "monthly"),
                                new XElement("priority", "0.7")));
                        }
                    }
                }

                // Dynamic routes for discussions: /discussions/{postID}/{post-slug}
                // Assuming DiscussionProvider.GetDiscussionsAsync() returns discussions with PostID and Slug.
                var discussions = await discussionProvider.GetAllDiscussionsAsync();
                foreach (var discussion in discussions)
                {
                    // Adjust property names as necessary.
                    var discussionUrl = $"{baseUrl}/discussions/{discussion.Id}/{discussion.Slug}";
                    urlElements.Add(new XElement("url",
                        new XElement("loc", discussionUrl),
                        new XElement("changefreq", "daily"),
                        new XElement("priority", "0.8")));
                }

                // Build the sitemap XML document.
                var sitemap = new XDocument(
                    new XDeclaration("1.0", "utf-8", "yes"),
                    new XElement("urlset", urlElements)
                );

                sitemapXml = sitemap.ToString();

                // Cache the generated sitemap for 1 hour.
                cache.Set(cacheKey, sitemapXml, new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromHours(1)));
            }

            return Results.Content(sitemapXml, "application/xml");
        });

        return endpoints;
    }
}


