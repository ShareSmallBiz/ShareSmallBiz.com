using System.Net;

namespace HttpClientUtility.RequestResult;

/// <summary>
/// Interface for entities that contain HTTP response result information
/// </summary>
public interface IResultInfo
{
    /// <summary>
    /// Gets or sets the completion date and time of the request
    /// </summary>
    DateTime? CompletionDate { get; set; }

    /// <summary>
    /// Gets or sets the elapsed time in milliseconds for the request to complete
    /// </summary>
    long ElapsedMilliseconds { get; set; }

    /// <summary>
    /// Gets or sets the HTTP status code of the response
    /// </summary>
    HttpStatusCode StatusCode { get; set; }

    /// <summary>
    /// Gets the age of the result as a formatted string
    /// </summary>
    string ResultAge { get; }
}
