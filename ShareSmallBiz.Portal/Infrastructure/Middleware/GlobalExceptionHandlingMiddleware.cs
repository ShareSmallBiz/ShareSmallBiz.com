using System.Security.Claims;
using System.Text.Json;

namespace ShareSmallBiz.Portal.Infrastructure.Middleware;

public static class GlobalExceptionHandlingMiddlewareExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandling(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<GlobalExceptionHandlingMiddleware>();
    }
}

public class GlobalExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public GlobalExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionHandlingMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            // Get request details for logging
            var requestMethod = context.Request.Method;
            var path = context.Request.Path;
            var queryString = context.Request.QueryString;
            var userId = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "anonymous";

            // Log the exception with context details
            _logger.LogError(
                ex,
                "Unhandled exception for User: {CreatedID}, Method: {Method}, Path: {Path}, Query: {QueryString}",
                userId,
                requestMethod,
                path,
                queryString);

            // Don't attempt to handle the response if it has already started
            if (context.Response.HasStarted)
            {
                _logger.LogWarning("Response has already started, cannot handle exception gracefully");
                throw; // Re-throw so the host can handle it
            }

            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        // Determine if it's an API request based on Accept header or URL path
        bool isApiRequest = context.Request.Headers.Accept.Any(x => x.Contains("application/json")) ||
                           context.Request.Path.StartsWithSegments("/api");

        // Reset the response to ensure it's clean
        context.Response.Clear();

        if (isApiRequest)
        {
            await HandleApiExceptionAsync(context, exception);
        }
        else
        {
            await HandleWebExceptionAsync(context, exception);
        }
    }

    private async Task HandleApiExceptionAsync(HttpContext context, Exception exception)
    {
        var statusCode = DetermineStatusCode(exception);
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json; charset=utf-8";

        var response = new
        {
            status = statusCode,
            error = GetErrorMessage(exception),
            traceId = context.TraceIdentifier
        };

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = _environment.IsDevelopment()
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response, options));
    }

    private async Task HandleWebExceptionAsync(HttpContext context, Exception exception)
    {
        // For web UI requests, redirect to an error page
        var statusCode = DetermineStatusCode(exception);
        var errorMessage = GetErrorMessage(exception);

        // Option 1: Redirect to error page (typically better for user experience)
        var errorPageUrl = $"/Error?statusCode={statusCode}&message={Uri.EscapeDataString(errorMessage)}&traceId={context.TraceIdentifier}";
        context.Response.Redirect(errorPageUrl);

        // Option 2: If you prefer to generate HTML directly (uncomment if needed)
        /*
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "text/html; charset=utf-8";
        
        var htmlContent = $@"
            <!DOCTYPE html>
            <html>
            <head>
                <title>Error - {statusCode}</title>
                <meta name='viewport' content='width=device-width' />
                <link rel='stylesheet' href='/css/site.css' />
            </head>
            <body>
                <div class='container text-center'>
                    <h1>Oops! Something went wrong.</h1>
                    <h2>{statusCode} - {GetStatusCodeDescription(statusCode)}</h2>
                    <div class='alert alert-danger'>{errorMessage}</div>
                    <a href='/' class='btn btn-primary mt-3'>Return to Home</a>
                    <p class='text-muted small mt-3'>Trace ID: {context.TraceIdentifier}</p>
                </div>
            </body>
            </html>";
            
        await context.Response.WriteAsync(htmlContent);
        */
    }

    private int DetermineStatusCode(Exception exception)
    {
        // Map common exception types to appropriate HTTP status codes
        return exception switch
        {
            ArgumentException or ArgumentNullException => StatusCodes.Status400BadRequest,
            UnauthorizedAccessException => StatusCodes.Status401Unauthorized,
            InvalidOperationException => StatusCodes.Status400BadRequest,
            KeyNotFoundException or FileNotFoundException => StatusCodes.Status404NotFound,
            NotImplementedException => StatusCodes.Status501NotImplemented,
            OperationCanceledException or TimeoutException => StatusCodes.Status408RequestTimeout,
            // Add more specific exception mappings as needed for your application
            _ => StatusCodes.Status500InternalServerError
        };
    }

    private string GetErrorMessage(Exception exception)
    {
        // Provide detailed errors in development, generic errors in production
        if (_environment.IsDevelopment())
        {
            return exception.Message;
        }

        // In production, return user-friendly messages based on exception type
        return exception switch
        {
            ArgumentException => "The request contains invalid parameters.",
            UnauthorizedAccessException => "You are not authorized to perform this action.",
            KeyNotFoundException => "The requested resource was not found.",
            _ => "An unexpected error occurred. Please try again later."
        };
    }

    private string GetStatusCodeDescription(int statusCode)
    {
        return statusCode switch
        {
            StatusCodes.Status400BadRequest => "Bad Request",
            StatusCodes.Status401Unauthorized => "Unauthorized",
            StatusCodes.Status403Forbidden => "Forbidden",
            StatusCodes.Status404NotFound => "Not Found",
            StatusCodes.Status408RequestTimeout => "Request Timeout",
            StatusCodes.Status500InternalServerError => "Internal Server Error",
            StatusCodes.Status501NotImplemented => "Not Implemented",
            _ => "Error"
        };
    }
}