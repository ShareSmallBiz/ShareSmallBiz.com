using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ShareSmallBiz.Core.Email;

namespace ShareSmallBiz.Infrastructure.Email;

/// <summary>
/// Development-only sender: writes each message to a text file in <see cref="EmailOptions.PickupDirectory"/>
/// so developers (and the local smoke script) can open confirmation and reset links.
/// </summary>
internal sealed partial class PickupDirectoryEmailSender(
    IOptions<EmailOptions> options, TimeProvider time, ILogger<PickupDirectoryEmailSender> logger) : IEmailSender
{
    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        var directory = Path.GetFullPath(options.Value.PickupDirectory);
        Directory.CreateDirectory(directory);

        var fileName = $"{time.GetUtcNow():yyyyMMdd-HHmmss-fffffff}-{Guid.NewGuid():N}.txt";
        var path = Path.Combine(directory, fileName);
        await File.WriteAllTextAsync(path, $"To: {message.To}\nSubject: {message.Subject}\n\n{message.TextBody}", cancellationToken);

        LogWritten(message.To, message.Subject, path);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Email to {To} ({Subject}) written to {Path}")]
    private partial void LogWritten(string to, string subject, string path);
}
