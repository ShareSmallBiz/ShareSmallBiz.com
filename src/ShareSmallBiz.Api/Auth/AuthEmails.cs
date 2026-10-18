using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using ShareSmallBiz.Core.Email;
using ShareSmallBiz.Core.Users;

namespace ShareSmallBiz.Api.Auth;

/// <summary>Composes account emails whose links open pages in the thin client.</summary>
public sealed class AuthEmails(UserManager<AppUser> users, IEmailSender sender, IOptions<ClientOptions> client)
{
    public async Task SendConfirmationAsync(AppUser user, CancellationToken cancellationToken)
    {
        var token = EncodeToken(await users.GenerateEmailConfirmationTokenAsync(user));
        var link = ClientLink("confirm-email", ("userId", user.Id), ("token", token));
        await sender.SendAsync(new EmailMessage(
            user.Email!,
            "Confirm your ShareSmallBiz account",
            $"Hi {user.DisplayName},\n\nConfirm your email address to finish creating your account:\n{link}\n"),
            cancellationToken);
    }

    /// <summary>Sent instead of an error when someone registers with an existing email, so the API never reveals which emails have accounts.</summary>
    public Task SendAccountExistsAsync(AppUser user, CancellationToken cancellationToken)
    {
        var link = ClientLink("forgot-password");
        return sender.SendAsync(new EmailMessage(
            user.Email!,
            "You already have a ShareSmallBiz account",
            $"Hi {user.DisplayName},\n\nSomeone tried to register with this email address, but you already have an account.\nIf you forgot your password you can reset it here:\n{link}\n"),
            cancellationToken);
    }

    public async Task SendPasswordResetAsync(AppUser user, CancellationToken cancellationToken)
    {
        var token = EncodeToken(await users.GeneratePasswordResetTokenAsync(user));
        var link = ClientLink("reset-password", ("email", user.Email!), ("token", token));
        await sender.SendAsync(new EmailMessage(
            user.Email!,
            "Reset your ShareSmallBiz password",
            $"Hi {user.DisplayName},\n\nUse this link to choose a new password:\n{link}\n\nIf you did not ask for this, you can ignore this email.\n"),
            cancellationToken);
    }

    /// <summary>Decodes a token received back from the client; returns null when it is malformed.</summary>
    public static string? DecodeToken(string token)
    {
        try
        {
            return Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token));
        }
        catch (FormatException)
        {
            return null;
        }
    }

    private static string EncodeToken(string token) => WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

    private string ClientLink(string path, params (string Key, string Value)[] query)
    {
        var url = new Uri(client.Value.BaseUrl, path).ToString();
        return QueryHelpers.AddQueryString(url, query.Select(q => KeyValuePair.Create(q.Key, (string?)q.Value)));
    }
}
