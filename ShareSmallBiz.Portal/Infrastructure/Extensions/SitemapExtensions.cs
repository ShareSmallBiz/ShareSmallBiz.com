using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using ShareSmallBiz.Portal.Data;
using ShareSmallBiz.Portal.Infrastructure.Services;
using System;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
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

            logger.LogInformation("Generating new sitemap.xml");

            var request = httpContextAccessor.HttpContext?.Request;
            var baseUrl = new UriBuilder
            {
                Scheme = request?.Scheme ?? "https",
                Host = request?.Host.Host ?? "localhost",
                Port = request?.Host.Port ?? -1
            }.Uri.ToString().TrimEnd('/');

            var urlElements = new List<XElement>();

            // Add static routes - with improved filtering
            urlElements.AddRange(GetStaticRoutes(endpointDataSource, baseUrl));

            // Add business user profiles
            await foreach (var profileUrl in GetBusinessProfileUrlsAsync(dbContext, baseUrl))
            {
                urlElements.Add(profileUrl);
            }

            // Add discussions
            var discussionUrls = await GetDiscussionUrlsAsync(discussionProvider, baseUrl);
            urlElements.AddRange(discussionUrls);

            // Add homepage if not already included
            if (!urlElements.Any(x => x.Element("loc")?.Value == baseUrl))
            {
                urlElements.Insert(0, new XElement("url",
                    new XElement("loc", baseUrl),
                    new XElement("changefreq", "daily"),
                    new XElement("priority", "1.0")));
            }

            var sitemap = new XDocument(
                new XDeclaration("1.0", "utf-8", "yes"),
                new XElement(XName.Get("urlset", "http://www.sitemaps.org/schemas/sitemap/0.9"), urlElements)
            );

            sitemapXml = sitemap.ToString();
            cache.Set(cacheKey, sitemapXml, new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromHours(1))
                .SetPriority(CacheItemPriority.High));

            return Results.Content(sitemapXml, "application/xml");
        });

        return endpoints;
    }

    private static IEnumerable<XElement> GetStaticRoutes(EndpointDataSource endpointDataSource, string baseUrl)
    {
        var endpointsList = endpointDataSource.Endpoints.OfType<RouteEndpoint>();

        // Regular expression to detect routes with parameters
        var paramRegex = new Regex(@"\{[^}]+\}");

        // Get only valid static routes
        var staticRoutes = endpointsList
            .Where(e =>
                !string.IsNullOrEmpty(e.RoutePattern.RawText) &&

                // Exclude API routes
                !e.RoutePattern.RawText.TrimStart('/').StartsWith("api", StringComparison.OrdinalIgnoreCase) &&

                // Exclude Identity routes
                !e.RoutePattern.RawText.TrimStart('/').StartsWith("Identity", StringComparison.OrdinalIgnoreCase) &&
                !e.RoutePattern.RawText.Contains("/Account/", StringComparison.OrdinalIgnoreCase) &&
                !e.RoutePattern.RawText.Contains("/Manage/", StringComparison.OrdinalIgnoreCase) &&

                // Exclude sitemap.xml itself
                e.RoutePattern.RawText.TrimStart('/') != "sitemap.xml" &&
                !e.RoutePattern.RawText.Contains("error", StringComparison.CurrentCultureIgnoreCase) &&

                // Exclude routes with parameters
                !paramRegex.IsMatch(e.RoutePattern.RawText) &&

                // Only include anonymous accessible endpoints
                IsAnonymousAccessible(e))
            .Select(e => e.RoutePattern.RawText.TrimStart('/'))
            .Distinct();

        return staticRoutes.Select(route => new XElement("url",
            new XElement("loc", $"{baseUrl}/{route}"),
            new XElement("changefreq", "weekly"),
            new XElement("priority", "0.5")));
    }

    private static bool IsAnonymousAccessible(RouteEndpoint endpoint)
    {
        // Check if the endpoint has any authorization metadata
        var authorizeData = endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>();
        if (authorizeData.Any())
        {
            // If any authorization requirements exist, it's not anonymous
            return false;
        }

        // Check if this is a controller action
        var controllerActionDescriptor = endpoint.Metadata.GetMetadata<ControllerActionDescriptor>();
        if (controllerActionDescriptor != null)
        {
            // Check controller-level attributes
            var controllerType = controllerActionDescriptor.ControllerTypeInfo;
            if (controllerType.GetCustomAttributes<AuthorizeAttribute>(true).Any())
            {
                // If controller has [Authorize], check if action has [AllowAnonymous]
                var  controllerActionMethod = controllerActionDescriptor.MethodInfo;
                return controllerActionMethod.GetCustomAttributes<AllowAnonymousAttribute>(true).Any();
            }

            // Check action-level attributes
            var actionMethod = controllerActionDescriptor.MethodInfo;
            return !actionMethod.GetCustomAttributes<AuthorizeAttribute>(true).Any();
        }

        // By default, assume it's accessible
        return true;
    }

    private static async IAsyncEnumerable<XElement> GetBusinessProfileUrlsAsync(ShareSmallBizUserContext dbContext, string baseUrl)
    {
        var businessRoleId = await dbContext.Roles
            .Where(r => r.Name == "Business")
            .Select(r => r.Id)
            .AsNoTracking()
            .FirstOrDefaultAsync();

        if (string.IsNullOrEmpty(businessRoleId))
        {
            yield break;
        }

        var businessUsers = dbContext.Users
            .Where(u => dbContext.UserRoles.Any(ur => ur.UserId == u.Id && ur.RoleId == businessRoleId))
            .Select(u => new
            {
                u.UserName,
                u.Email,
                u.LastModified
            })
            .AsNoTracking()
            .AsAsyncEnumerable();

        await foreach (var user in businessUsers)
        {
            if (!string.IsNullOrWhiteSpace(user.UserName) && user.UserName.ToLower() != user.Email.ToLower())
            {
                yield return new XElement("url",
                    new XElement("loc", $"{baseUrl}/profiles/{user.UserName}"),
                    new XElement("lastmod", user.LastModified.ToString("yyyy-MM-dd")),
                    new XElement("changefreq", "monthly"),
                    new XElement("priority", "0.7"));
            }
        }
    }

    private static async Task<IEnumerable<XElement>> GetDiscussionUrlsAsync(DiscussionProvider discussionProvider, string baseUrl)
    {
        var discussions = await discussionProvider.GetAllDiscussionsAsync();

        return discussions.Select(discussion =>
            new XElement("url",
                new XElement("loc", $"{baseUrl}/discussions/{discussion.Id}/{discussion.Slug}"),
                new XElement("lastmod", discussion.ModifiedDate.ToString("yyyy-MM-dd") ?? DateTime.Now.ToString("yyyy-MM-dd")),
                new XElement("changefreq", "daily"),
                new XElement("priority", "0.8"))
        );
    }
}