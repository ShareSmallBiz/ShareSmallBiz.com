using Microsoft.Extensions.Logging;
using ShareSmallBiz.Core.Email;

namespace ShareSmallBiz.Infrastructure.Email;

/// <summary>Development-only sender: writes the whole message (including links) to the log.</summary>
internal sealed partial class LoggingEmailSender(ILogger<LoggingEmailSender> logger) : IEmailSender
{
    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        LogEmail(message.To, message.Subject, message.TextBody);
        return Task.CompletedTask;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Email to {To}: {Subject}\n{Body}")]
    private partial void LogEmail(string to, string subject, string body);
}
