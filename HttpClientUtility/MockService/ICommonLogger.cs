namespace HttpClientUtility.MockService
{
    /// <summary>
    /// Interface for common logging operations
    /// </summary>
    public interface ICommonLogger
    {
        /// <summary>
        /// Tracks an event with the specified message
        /// </summary>
        /// <param name="message">The message to log</param>
        void TrackEvent(string message);

        /// <summary>
        /// Tracks an exception with the specified message
        /// </summary>
        /// <param name="exception">The exception to track</param>
        /// <param name="message">Additional context message for the exception</param>
        void TrackException(Exception exception, string message);
    }
}

