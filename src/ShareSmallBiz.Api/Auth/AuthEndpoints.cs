using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using ShareSmallBiz.Core.Users;

namespace ShareSmallBiz.Api.Auth;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var auth = app.MapGroup("/api/v1/auth")
            .WithTags("Auth")
            .RequireRateLimiting(RateLimitingOptions.AuthPolicy);

        auth.MapPost("/register", Register)
            .WithSummary("Create an account; a confirmation email is sent. Always 202 for a well-formed request so emails cannot be enumerated.");
        auth.MapPost("/confirm-email", ConfirmEmail)
            .WithSummary("Confirm an email address with the userId and token from the confirmation link.");
        auth.MapPost("/resend-confirmation", ResendConfirmation)
            .WithSummary("Send another confirmation email if the account exists and is unconfirmed. Always 202.");
        auth.MapPost("/login", Login)
            .WithSummary("Exchange email and password for an access token and refresh token.")
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);
        auth.MapPost("/refresh", Refresh)
            .WithSummary("Exchange a refresh token for a new token pair. Each refresh token works once.")
            .ProducesProblem(StatusCodes.Status401Unauthorized);
        auth.MapPost("/logout", Logout)
            .WithSummary("End the session the refresh token belongs to.");
        auth.MapPost("/forgot-password", ForgotPassword)
            .WithSummary("Send a password reset email if a confirmed account exists. Always 202.");
        auth.MapPost("/reset-password", ResetPassword)
            .WithSummary("Set a new password with the token from the reset email; signs out every session.");

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
