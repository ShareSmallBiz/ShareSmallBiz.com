namespace ShareSmallBiz.Core.Email;

/// <summary>An outbound email. Bodies are plain text; links point at the thin client.</summary>
public sealed record EmailMessage(string To, string Subject, string TextBody);

/// <summary>Sends transactional email (confirmation, password reset).</summary>
public interface IEmailSender
{
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
}
