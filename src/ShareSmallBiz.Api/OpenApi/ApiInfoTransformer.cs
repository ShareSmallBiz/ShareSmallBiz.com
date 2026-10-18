using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace ShareSmallBiz.Api.OpenApi;

/// <summary>Title, version and a markdown walkthrough of the end-to-end flow, shown at the top of the docs.</summary>
internal sealed class ApiInfoTransformer : IOpenApiDocumentTransformer
{
    private const string Description = """
        The ShareSmallBiz API: accounts and member profiles for the ShareSmallBiz small-business community.

        ## Getting started
        1. **Register**: `POST /api/v1/auth/register`. The response is always `202` and a confirmation email is sent.
        2. **Confirm your email**: the email links to the client's `/confirm-email?userId=…&token=…` page.
           Send those two values to `POST /api/v1/auth/confirm-email`.
           *Running locally?* Read the email with `GET /dev/mailbox/latest?to=<your email>`, which is available in Development only.
        3. **Sign in**: `POST /api/v1/auth/login` returns an `accessToken` (15 minutes) and a `refreshToken` (14 days).
           Send `Authorization: Bearer <accessToken>` on secured calls; they are marked with a lock.
        4. **Stay signed in**: exchange the refresh token at `POST /api/v1/auth/refresh`. Each refresh token works **once**;
           replaying a spent one ends the whole session.
        5. **Your profile**: `GET /api/v1/me`, then `PUT /api/v1/me/profile`. Others see it at `GET /api/v1/profiles/{userName}`.

        ## Conventions
        - **Errors** are [RFC 9457 problem details](https://www.rfc-editor.org/rfc/rfc9457) (`application/problem+json`).
          Validation failures carry an `errors` object keyed by field.
        - **Lists** return `{ items, page, pageSize, totalCount, totalPages }`. `page` starts at 1, and `pageSize` is at most 50.
        - **Enums** are strings, e.g. `"Public"`.
        - **Privacy:** only `GET /api/v1/me` returns your email and other private fields. No other response does.
        - **Rate limits:** auth endpoints allow 10 requests per minute per IP and return `429` beyond that.
        """;

    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        document.Info.Title = "ShareSmallBiz API";
        document.Info.Version = "v1";
        document.Info.Description = Description;
        return Task.CompletedTask;
    }
}
