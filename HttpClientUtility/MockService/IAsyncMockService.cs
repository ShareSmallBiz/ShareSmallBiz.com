namespace HttpClientUtility.MockService;

/// <summary>
/// Interface for asynchronous mock service operations
/// </summary>
public interface IAsyncMockService
{
    /// <summary>
    /// Simulates a long-running operation
    /// </summary>
    /// <param name="loop">The number of iterations to perform</param>
    /// <returns>A task representing the operation with a decimal result</returns>
    Task<decimal> LongRunningOperation(int loop);

    /// <summary>
    /// Simulates a long-running operation that can be canceled
    /// </summary>
    /// <param name="loop">The number of iterations to perform</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests</param>
    /// <returns>A task representing the operation with a decimal result</returns>
    Task<decimal> LongRunningOperationWithCancellationTokenAsync(int loop, CancellationToken cancellationToken);
}
