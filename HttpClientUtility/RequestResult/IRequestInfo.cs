namespace HttpClientUtility.RequestResult;

/// <summary>
/// Interface for entities that contain HTTP request information
/// </summary>
public interface IRequestInfo
{
    /// <summary>
    /// Gets or sets the iteration count of the request
    /// </summary>
    int Iteration { get; set; }

    /// <summary>
    /// Gets or sets the request path or URL
    /// </summary>
    string RequestPath { get; set; }

    /// <summary>
    /// Gets or sets the HTTP method (GET, POST, etc.)
    /// </summary>
    HttpMethod RequestMethod { get; set; }

    /// <summary>
    /// Gets or sets the request body content
    /// </summary>
    StringContent? RequestBody { get; set; }

    /// <summary>
    /// Gets or sets the HTTP headers for the request
    /// </summary>
    Dictionary<string, string>? RequestHeaders { get; set; }
}
