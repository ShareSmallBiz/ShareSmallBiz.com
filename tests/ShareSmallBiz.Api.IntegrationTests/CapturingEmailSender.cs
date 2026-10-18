using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.WebUtilities;
using ShareSmallBiz.Core.Email;

namespace ShareSmallBiz.Api.IntegrationTests;

/// <summary>Records outbound email so tests can follow confirmation and reset links.</summary>
public sealed partial class CapturingEmailSender : IEmailSender
{
    private readonly ConcurrentQueue<EmailMessage> _sent = new();

    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        _sent.Enqueue(message);
        return Task.CompletedTask;
    }

    public IReadOnlyList<EmailMessage> SentTo(string email) =>
        [.. _sent.Where(m => string.Equals(m.To, email, StringComparison.OrdinalIgnoreCase))];

    /// <summary>The query string of the client link in the most recent email to <paramref name="email"/>.</summary>
    public Dictionary<string, string> LastLinkQuery(string email, out Uri link)
    {
        var message = SentTo(email)[^1];
        link = new Uri(LinkRegex().Match(message.TextBody).Value);
        return QueryHelpers.ParseQuery(link.Query).ToDictionary(kv => kv.Key, kv => kv.Value.ToString());
    }

    [GeneratedRegex(@"https://\S+")]
    private static partial Regex LinkRegex();
}
