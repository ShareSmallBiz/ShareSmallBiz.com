using ShareSmallBiz.Portal.Infrastructure.Services;
using System.Text.Json;

namespace ShareSmallBiz.Aspire.ApiService.PostService
{
    public static class PostProviderExtensions
    {
        public static void RegisterPostProviderServices(this IServiceCollection services)
        {
            services.AddScoped<PostProvider>();
        }

        public static void MapPostProviderEndpoints(this IEndpointRouteBuilder app)
        {
            var logger = app.ServiceProvider.GetRequiredService<ILogger<PostProvider>>();

            app.MapPost("/postprovider/comment", async (HttpContext context, IServiceScopeFactory serviceScopeFactory) =>
            {
                using var scope = serviceScopeFactory.CreateScope();
                var provider = scope.ServiceProvider.GetRequiredService<PostProvider>();
                logger.LogInformation("API call received: /postprovider/comment");

                try
                {
                    var request = await JsonSerializer.DeserializeAsync<CommentRequest>(context.Request.Body);
                    if (request == null || request.PostId <= 0 || string.IsNullOrWhiteSpace(request.Comment))
                    {
                        logger.LogWarning("Invalid input received");
                        context.Response.StatusCode = StatusCodes.Status400BadRequest;
                        await context.Response.WriteAsync("Invalid input");
                        return;
                    }

                    var result = await provider.CommentPostAsync(request.PostId, request.Comment);
                    await context.Response.WriteAsJsonAsync(result);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error processing comment request");
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    await context.Response.WriteAsync("An error occurred while processing your request.");
                }
            }).WithName("CommentPostEndpoint");

            app.MapPost("/postprovider/createpost", async (HttpContext context, IServiceScopeFactory serviceScopeFactory) =>
            {
                using var scope = serviceScopeFactory.CreateScope();
                var provider = scope.ServiceProvider.GetRequiredService<PostProvider>();
                logger.LogInformation("API call received: /postprovider/createpost");

                try
                {
                    var request = await JsonSerializer.DeserializeAsync<ShareSmallBiz.Portal.Infrastructure.Services.PostModel>(context.Request.Body);
                    if (request == null || string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Content))
                    {
                        logger.LogWarning("Invalid input received");
                        context.Response.StatusCode = StatusCodes.Status400BadRequest;
                        await context.Response.WriteAsync("Invalid input");
                        return;
                    }

                    var userPrincipal = context.User;
                    var result = await provider.CreatePostAsync(request, userPrincipal);
                    await context.Response.WriteAsJsonAsync(result);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error processing create post request");
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    await context.Response.WriteAsync("An error occurred while processing your request.");
                }
            }).WithName("CreatePostEndpoint");

            app.MapGet("/postprovider/getpost/{postId}", async (HttpContext context, int postId, IServiceScopeFactory serviceScopeFactory) =>
            {
                using var scope = serviceScopeFactory.CreateScope();
                var provider = scope.ServiceProvider.GetRequiredService<PostProvider>();
                logger.LogInformation("API call received: /postprovider/getpost/{PostId}", postId);

                try
                {
                    if (postId <= 0)
                    {
                        logger.LogWarning("Invalid PostId received");
                        context.Response.StatusCode = StatusCodes.Status400BadRequest;
                        await context.Response.WriteAsync("Invalid PostId");
                        return;
                    }

                    var result = await provider.GetPostByIdAsync(postId);
                    await context.Response.WriteAsJsonAsync(result);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error processing get post request");
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    await context.Response.WriteAsync("An error occurred while processing your request.");
                }
            }).WithName("GetPostByIdEndpoint");

            app.MapDelete("/postprovider/deletepost/{postId}", async (HttpContext context, int postId, IServiceScopeFactory serviceScopeFactory) =>
            {
                using var scope = serviceScopeFactory.CreateScope();
                var provider = scope.ServiceProvider.GetRequiredService<PostProvider>();
                logger.LogInformation("API call received: /postprovider/deletepost/{PostId}", postId);

                try
                {
                    if (postId <= 0)
                    {
                        logger.LogWarning("Invalid PostId received");
                        context.Response.StatusCode = StatusCodes.Status400BadRequest;
                        await context.Response.WriteAsync("Invalid PostId");
                        return;
                    }

                    var result = await provider.DeletePostAsync(postId);
                    await context.Response.WriteAsJsonAsync(result);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error processing delete post request");
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    await context.Response.WriteAsync("An error occurred while processing your request.");
                }
            }).WithName("DeletePostEndpoint");

            app.MapGet("/postprovider/featuredposts/{count}", async (HttpContext context, int count, IServiceScopeFactory serviceScopeFactory) =>
            {
                using var scope = serviceScopeFactory.CreateScope();
                var provider = scope.ServiceProvider.GetRequiredService<PostProvider>();
                logger.LogInformation("API call received: /postprovider/featuredposts/{Count}", count);

                var result = await provider.FeaturedPostsAsync(count);
                await context.Response.WriteAsJsonAsync(result);
            }).WithName("FeaturedPostsEndpoint");

            app.MapGet("/postprovider/getposts/{perPage}/{pageNumber}", async (HttpContext context, int perPage, int pageNumber, IServiceScopeFactory serviceScopeFactory) =>
            {
                using var scope = serviceScopeFactory.CreateScope();
                var provider = scope.ServiceProvider.GetRequiredService<PostProvider>();
                logger.LogInformation("API call received: /postprovider/getposts/{PerPage}/{PageNumber}", perPage, pageNumber);

                var result = await provider.GetPostsAsync(perPage, pageNumber);
                await context.Response.WriteAsJsonAsync(result);
            }).WithName("GetPostsEndpoint");

            app.MapGet("/postprovider/mostcommentedposts/{count}", async (HttpContext context, int count, IServiceScopeFactory serviceScopeFactory) =>
            {
                using var scope = serviceScopeFactory.CreateScope();
                var provider = scope.ServiceProvider.GetRequiredService<PostProvider>();
                logger.LogInformation("API call received: /postprovider/mostcommentedposts/{Count}", count);

                var result = await provider.MostCommentedPostsAsync(count);
                await context.Response.WriteAsJsonAsync(result);
            }).WithName("MostCommentedPostsEndpoint");

            app.MapGet("/postprovider/mostpopularposts/{count}", async (HttpContext context, int count, IServiceScopeFactory serviceScopeFactory) =>
            {
                using var scope = serviceScopeFactory.CreateScope();
                var provider = scope.ServiceProvider.GetRequiredService<PostProvider>();
                logger.LogInformation("API call received: /postprovider/mostpopularposts/{Count}", count);

                var result = await provider.MostPopularPostsAsync(count);
                await context.Response.WriteAsJsonAsync(result);
            }).WithName("MostPopularPostsEndpoint");

            app.MapGet("/postprovider/mostrecentposts/{count}", async (HttpContext context, int count, IServiceScopeFactory serviceScopeFactory) =>
            {
                using var scope = serviceScopeFactory.CreateScope();
                var provider = scope.ServiceProvider.GetRequiredService<PostProvider>();
                logger.LogInformation("API call received: /postprovider/mostrecentposts/{Count}", count);

                var result = await provider.MostRecentPostsAsync(count);
                await context.Response.WriteAsJsonAsync(result);
            }).WithName("MostRecentPostsEndpoint");
        }

    }
    public record CommentRequest(int PostId, string Comment);
}
