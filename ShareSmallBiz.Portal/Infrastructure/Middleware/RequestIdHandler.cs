namespace ShareSmallBiz.Portal.Infrastructure.Middleware;

/// <summary>
/// Adds a unique X-Request-ID header to every outgoing HTTP request for correlation tracing.
/// </summary>
public class RequestIdHandler : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        request.Headers.TryAddWithoutValidation("X-Request-ID", Guid.NewGuid().ToString());
        return base.SendAsync(request, cancellationToken);
    }
}
