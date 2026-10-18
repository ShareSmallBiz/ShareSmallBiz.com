using System.Collections.Concurrent;
using System.ComponentModel;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.WebUtilities;
using ShareSmallBiz.Core.Email;

namespace ShareSmallBiz.Api.Development;

[Description("A link found in a captured email, with its query string parsed.")]
public sealed record DevMailLink(
    [property: Description("The full link as it appears in the email.")] string Url,
    [property: Description("Client route, e.g. `/confirm-email` or `/reset-password`.")] string Path,
    [property: Description("Query parameters, e.g. `userId` and `token`: paste these into the matching auth request.")] IReadOnlyDictionary<string, string> Query);

[Description("An email the API sent while running in Development.")]
public sealed record DevMailMessage(
    Guid Id,
    string To,
    string Subject,
    string Body,
    DateTimeOffset SentAt,
    IReadOnlyList<DevMailLink> Links);

/// <summary>Development-only, in-memory record of the most recent outbound emails.</summary>
public sealed partial class DevMailbox(TimeProvider time)
{
    public const int Capacity = 200;

    private readonly ConcurrentQueue<DevMailMessage> _messages = new();

    public void Add(EmailMessage message)
    {
        _messages.Enqueue(new DevMailMessage(
            Guid.NewGuid(), message.To, message.Subject, message.TextBody, time.GetUtcNow(), ParseLinks(message.TextBody)));
        while (_messages.Count > Capacity)
            _messages.TryDequeue(out _);
    }

    /// <summary>Newest first, optionally filtered by recipient and subject text (both case-insensitive).</summary>
    public IReadOnlyList<DevMailMessage> Find(string? to, string? subject, int take) =>
        [.. _messages.ToArray()
            .Reverse()
            .Where(m => to is null || string.Equals(m.To, to, StringComparison.OrdinalIgnoreCase))
            .Where(m => subject is null || m.Subject.Contains(subject, StringComparison.OrdinalIgnoreCase))
            .Take(take)];

    public void Clear() => _messages.Clear();

    private static List<DevMailLink> ParseLinks(string body) =>
        [.. LinkRegex().Matches(body).Select(match =>
        {
            var uri = new Uri(match.Value);
            var query = QueryHelpers.ParseQuery(uri.Query).ToDictionary(kv => kv.Key, kv => kv.Value.ToString());
            return new DevMailLink(match.Value, uri.AbsolutePath, query);
        })];

    [GeneratedRegex(@"https?://\S+")]
    private static partial Regex LinkRegex();
}

/// <summary>Records every message in the <see cref="DevMailbox"/>, then hands it to the configured sender.</summary>
internal sealed class DevMailboxEmailSender(DevMailbox mailbox, IEmailSender inner) : IEmailSender
{
    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        mailbox.Add(message);
        return inner.SendAsync(message, cancellationToken);
    }
}

public static class DevMailboxServiceCollectionExtensions
{
    /// <summary>Wraps the registered <see cref="IEmailSender"/> so Development emails are also captured in memory.</summary>
    public static IServiceCollection AddDevMailbox(this IServiceCollection services)
    {
        services.AddSingleton<DevMailbox>();

        var inner = services.Last(d => d.ServiceType == typeof(IEmailSender));
        services.Remove(inner);
        services.AddSingleton<IEmailSender>(sp => new DevMailboxEmailSender(sp.GetRequiredService<DevMailbox>(), Resolve(sp, inner)));
        return services;
    }

    private static IEmailSender Resolve(IServiceProvider services, ServiceDescriptor descriptor) =>
        (IEmailSender)(descriptor.ImplementationInstance
            ?? descriptor.ImplementationFactory?.Invoke(services)
            ?? ActivatorUtilities.CreateInstance(services, descriptor.ImplementationType!));
}
