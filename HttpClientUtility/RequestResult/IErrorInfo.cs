namespace HttpClientUtility.RequestResult;

/// <summary>
/// Interface for entities that contain error information
/// </summary>
public interface IErrorInfo
{
    /// <summary>
    /// Gets or sets the list of errors that occurred during operations
    /// </summary>
    List<string> ErrorList { get; set; }
}
