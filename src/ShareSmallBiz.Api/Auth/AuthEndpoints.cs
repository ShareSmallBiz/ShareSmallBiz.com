using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using ShareSmallBiz.Core.Users;

namespace ShareSmallBiz.Api.Auth;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        // Every auth endpoint validates its body and is rate-limited per IP
        var auth = app.MapGroup("/api/v1/auth")
            .WithTags("Account: Authentication")
            .RequireRateLimiting(RateLimitingOptions.AuthPolicy)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status429TooManyRequests);

        auth.MapPost("/register", Register)
            .WithName("Register")
            .WithSummary("Create an account")
            .WithDescription("""
                Creates the account and emails a confirmation link. Sign-in is blocked until the email is confirmed.

                Always returns **202** for a well-formed request, even when the email already has an account
                (that owner is emailed instead), so this endpoint cannot be used to discover who is a member.
                Returns **400** when the username is taken or a field is invalid.
                """);
        auth.MapPost("/confirm-email", ConfirmEmail)
            .WithName("ConfirmEmail")
            .WithSummary("Confirm an email address")
            .WithDescription("Send the `userId` and `token` from the confirmation link. **204** when confirmed (or already confirmed); **400** when the link is invalid or expired.");
        auth.MapPost("/resend-confirmation", ResendConfirmation)
            .WithName("ResendConfirmation")
            .WithSummary("Resend the confirmation email")
            .WithDescription("Sends a new confirmation link if the account exists and is unconfirmed. Always **202**.");
        auth.MapPost("/login", Login)
            .WithName("Login")
            .WithSummary("Sign in")
            .WithDescription("""
                Exchanges email and password for tokens. Send `accessToken` as `Authorization: Bearer <accessToken>`;
                keep `refreshToken` for `POST /api/v1/auth/refresh`.

                - **401**: wrong email or password, or the account is locked (5 failures lock it for 15 minutes). The response is identical in every case.
                - **403** with `code: email_not_confirmed`: the email is not confirmed yet.
                """)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);
        auth.MapPost("/refresh", Refresh)
            .WithName("RefreshTokens")
            .WithSummary("Refresh the tokens")
            .WithDescription("""
                Exchanges a refresh token for a new access token **and** a new refresh token. Each refresh token works once:
                presenting one that was already used is treated as theft and ends that whole session (**401**).
                """)
            .ProducesProblem(StatusCodes.Status401Unauthorized);
        auth.MapPost("/logout", Logout)
            .WithName("Logout")
            .WithSummary("Sign out of this session")
            .WithDescription("Revokes the session the refresh token belongs to; other sessions stay signed in. Always **204**.");
        auth.MapPost("/forgot-password", ForgotPassword)
            .WithName("ForgotPassword")
            .WithSummary("Request a password reset email")
            .WithDescription("Emails a reset link if a confirmed account exists. Always **202**, so it reveals nothing about who is a member.");
        auth.MapPost("/reset-password", ResetPassword)
            .WithName("ResetPassword")
            .WithSummary("Set a new password")
            .WithDescription("Send the `email` and `token` from the reset link with the new password. On success (**204**) every session is signed out.");

        return app;
    }

    private static async Task<Results<Accepted, ValidationProblem>> Register(
        RegisterRequest request, UserManager<AppUser> users, AuthEmails emails, TimeProvider time, CancellationToken cancellationToken)
    {
        var existing = await users.FindByEmailAsync(request.Email);
        if (existing is not null)
        {
            await emails.SendAccountExistsAsync(existing, cancellationToken);
            return TypedResults.Accepted((string?)null);
        }

        var user = new AppUser
        {
            Email = request.Email,
            UserName = request.UserName,
            DisplayName = request.DisplayName,
            FirstName = request.FirstName,
            LastName = request.LastName,
            CreatedAt = time.GetUtcNow(),
        };
        var result = await users.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            return IdentityValidationProblem(result);

        await emails.SendConfirmationAsync(user, cancellationToken);
        return TypedResults.Accepted((string?)null);
    }

    private static async Task<Results<NoContent, ValidationProblem>> ConfirmEmail(
        ConfirmEmailRequest request, UserManager<AppUser> users)
    {
        var user = await users.FindByIdAsync(request.UserId);
        var token = AuthEmails.DecodeToken(request.Token);
        if (user is null || token is null)
            return InvalidToken();
        if (user.EmailConfirmed)
            return TypedResults.NoContent();

        var result = await users.ConfirmEmailAsync(user, token);
        return result.Succeeded ? TypedResults.NoContent() : InvalidToken();
    }

    private static async Task<Accepted> ResendConfirmation(
        EmailRequest request, UserManager<AppUser> users, AuthEmails emails, CancellationToken cancellationToken)
    {
        var user = await users.FindByEmailAsync(request.Email);
        if (user is { EmailConfirmed: false })
            await emails.SendConfirmationAsync(user, cancellationToken);
        return TypedResults.Accepted((string?)null);
    }

    private static async Task<Results<Ok<AuthTokensResponse>, ProblemHttpResult>> Login(
        LoginRequest request, UserManager<AppUser> users, TokenService tokens, CancellationToken cancellationToken)
    {
        var user = await users.FindByEmailAsync(request.Email);
        if (user is null || await users.IsLockedOutAsync(user))
            return InvalidCredentials();

        if (!await users.CheckPasswordAsync(user, request.Password))
        {
            await users.AccessFailedAsync(user);
            return InvalidCredentials();
        }

        if (!user.EmailConfirmed)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: "Email address not confirmed.",
                extensions: new Dictionary<string, object?> { ["code"] = "email_not_confirmed" });
        }

        await users.ResetAccessFailedCountAsync(user);
        return TypedResults.Ok(await tokens.IssueAsync(user, cancellationToken: cancellationToken));
    }

    private static async Task<Results<Ok<AuthTokensResponse>, ProblemHttpResult>> Refresh(
        RefreshRequest request, TokenService tokens, CancellationToken cancellationToken)
    {
        var refreshed = await tokens.RefreshAsync(request.RefreshToken, cancellationToken);
        return refreshed is null
            ? TypedResults.Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Refresh token is invalid or expired.")
            : TypedResults.Ok(refreshed);
    }

    private static async Task<NoContent> Logout(RefreshRequest request, TokenService tokens, CancellationToken cancellationToken)
    {
        await tokens.RevokeSessionAsync(request.RefreshToken, cancellationToken);
        return TypedResults.NoContent();
    }

    private static async Task<Accepted> ForgotPassword(
        EmailRequest request, UserManager<AppUser> users, AuthEmails emails, CancellationToken cancellationToken)
    {
        var user = await users.FindByEmailAsync(request.Email);
        if (user is { EmailConfirmed: true })
            await emails.SendPasswordResetAsync(user, cancellationToken);
        return TypedResults.Accepted((string?)null);
    }

    private static async Task<Results<NoContent, ValidationProblem>> ResetPassword(
        ResetPasswordRequest request, UserManager<AppUser> users, TokenService tokens, CancellationToken cancellationToken)
    {
        var user = await users.FindByEmailAsync(request.Email);
        var token = AuthEmails.DecodeToken(request.Token);
        if (user is null || token is null)
            return InvalidToken();

        var result = await users.ResetPasswordAsync(user, token, request.NewPassword);
        if (!result.Succeeded)
        {
            return result.Errors.Any(e => e.Code == nameof(IdentityErrorDescriber.InvalidToken))
                ? InvalidToken()
                : IdentityValidationProblem(result);
        }

        await users.ResetAccessFailedCountAsync(user);
        await tokens.RevokeAllAsync(user.Id, cancellationToken);
        return TypedResults.NoContent();
    }

    private static ProblemHttpResult InvalidCredentials() =>
        TypedResults.Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Invalid email or password.");

    private static ValidationProblem InvalidToken() =>
        TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["token"] = ["The link is invalid or has expired."] });

    private static ValidationProblem IdentityValidationProblem(IdentityResult result) =>
        TypedResults.ValidationProblem(result.Errors
            .GroupBy(e => e.Code.Contains("Password", StringComparison.Ordinal) ? "password"
                : e.Code.Contains("UserName", StringComparison.Ordinal) ? "userName"
                : e.Code.Contains("Email", StringComparison.Ordinal) ? "email"
                : string.Empty)
            .ToDictionary(g => g.Key, g => g.Select(e => e.Description).ToArray()));
}
