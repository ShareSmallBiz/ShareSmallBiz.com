using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ShareSmallBiz.Api.Development;

/// <summary>Development-only endpoints for reading captured email. Never mapped outside Development.</summary>
public static class DevMailboxEndpoints
{
    public const string Tag = "Development: Mailbox";

    public static IEndpointRouteBuilder MapDevMailboxEndpoints(this IEndpointRouteBuilder app)
    {
        var mailbox = app.MapGroup("/dev/mailbox").WithTags(Tag);

        mailbox.MapGet("/latest", GetLatest)
            .WithName("GetLatestDevMail")
            .WithSummary("Newest captured email, with its links parsed")
            .WithDescription("""
                **Development only.** Returns the most recent email the API sent, newest first.

                Typical use while testing sign-up: after `POST /api/v1/auth/register`, call this with
                `to=<your email>&subject=confirm`, then send `links[0].query.userId` and
                `links[0].query.token` to `POST /api/v1/auth/confirm-email`.
                For password reset use `subject=reset` and send `email` and `token` to `POST /api/v1/auth/reset-password`.
                """);

        mailbox.MapGet("", List)
            .WithName("ListDevMail")
            .WithSummary("Recent captured emails")
            .WithDescription($"**Development only.** Up to the last {DevMailbox.Capacity} emails, newest first, optionally filtered by recipient and subject text.");

        mailbox.MapDelete("", Clear)
            .WithName("ClearDevMail")
            .WithSummary("Empty the mailbox")
            .WithDescription("**Development only.** Removes every captured email.");

        return app;
    }

    private static Results<Ok<DevMailMessage>, NotFound> GetLatest(DevMailbox mailbox, string? to = null, string? subject = null) =>
        mailbox.Find(to, subject, take: 1) is [var latest] ? TypedResults.Ok(latest) : TypedResults.NotFound();

    private static Ok<IReadOnlyList<DevMailMessage>> List(
        DevMailbox mailbox, string? to = null, string? subject = null, [Range(1, DevMailbox.Capacity)] int take = 20) =>
        TypedResults.Ok(mailbox.Find(to, subject, take));

    private static NoContent Clear(DevMailbox mailbox)
    {
        mailbox.Clear();
        return TypedResults.NoContent();
    }
}
