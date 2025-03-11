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
            if (cache.TryGetValue(cacheKey, out string sitemapXml))
            {
                logger.LogInformation("Serving cached sitemap.xml");
                return Results.Content(sitemapXml, "application/xml");
            }

            logger.LogWarning("Cache miss! Regenerating sitemap.xml");

            var request = httpContextAccessor.HttpContext?.Request;
            var baseUrl = new UriBuilder
            {
                Scheme = request?.Scheme ?? "https",
                Host = request?.Host.Host ?? "localhost"
            }.Uri.ToString().TrimEnd('/');

            var endpointsList = endpointDataSource.Endpoints.OfType<RouteEndpoint>();
            var staticRoutes = endpointsList
                .Where(e => !string.IsNullOrEmpty(e.RoutePattern.RawText) &&
                            !e.RoutePattern.RawText.TrimStart('/').StartsWith("api", StringComparison.OrdinalIgnoreCase))
                .Select(e => e.RoutePattern.RawText.TrimStart('/'))
                .Distinct();

            var urlElements = new List<XElement>();

            foreach (var route in staticRoutes)
            {
                var fullUrl = $"{baseUrl}/{route}";
                urlElements.Add(new XElement("url",
                    new XElement("loc", fullUrl),
                    new XElement("changefreq", "weekly"),
                    new XElement("priority", "0.5")));
            }

            await foreach (var user in dbContext.Users.AsAsyncEnumerable())
            {
                if (!string.IsNullOrWhiteSpace(user.UserName) && user.UserName.ToLower() != user.Email.ToLower())
                {
                    urlElements.Add(new XElement("url",
                        new XElement("loc", $"{baseUrl}/profiles/{user.UserName}"),
                        new XElement("lastmod", DateTime.Now.ToString("yyyy-MM-dd")),
                        new XElement("changefreq", "monthly"),
                        new XElement("priority", "0.7")));
                }
            }

            var discussions = await discussionProvider.GetAllDiscussionsAsync();
            foreach (var discussion in discussions)
            {
                var discussionUrl = $"{baseUrl}/discussions/{discussion.Id}/{discussion.Slug}";
                urlElements.Add(new XElement("url",
                    new XElement("loc", discussionUrl),
                    new XElement("changefreq", "daily"),
                    new XElement("priority", "0.8")));
            }

            var sitemap = new XDocument(
                new XDeclaration("1.0", "utf-8", "yes"),
                new XElement(XName.Get("urlset", "http://www.sitemaps.org/schemas/sitemap/0.9"), urlElements)
            );

            sitemapXml = sitemap.ToString();
            cache.Set(cacheKey, sitemapXml, new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(30)));

            return Results.Content(sitemapXml, "application/xml");
        });

        return endpoints;
    }
}
