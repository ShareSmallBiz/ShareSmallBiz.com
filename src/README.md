# ShareSmallBiz API (greenfield)

Standalone REST API for `api.sharesmallbiz.com`, consumed by the thin client at `sharesmallbiz.com`.
The legacy `ShareSmallBiz.Portal` beside it is reference code only and will be removed.

| Project | Role |
|---|---|
| `src/ShareSmallBiz.Api` | Minimal-API endpoints, auth, OpenAPI, CORS, rate limiting |
| `src/ShareSmallBiz.Core` | Domain entities and abstractions (no EF, no ASP.NET) |
| `src/ShareSmallBiz.Infrastructure` | EF Core + SQLite, migrations, email |
| `tests/ShareSmallBiz.Api.IntegrationTests` | Real HTTP tests via `WebApplicationFactory` |
| `openapi/ShareSmallBiz.Api.json` | The API contract, regenerated on every build. Commit it; CI fails if it is stale |

## Run locally

```bash
dotnet user-secrets set "Jwt:SigningKey" "$(openssl rand -base64 48)" --project src/ShareSmallBiz.Api
dotnet run --project src/ShareSmallBiz.Api        # http://localhost:5280/scalar/v1
```

Development uses `sharesmallbiz-dev.db` (created and migrated on startup) and writes account emails,
including confirmation and reset links, to the console.

## Test

```bash
dotnet test ShareSmallBiz.Api.slnx
```

## Database migrations

```bash
dotnet tool restore
dotnet ef migrations add <Name> -p src/ShareSmallBiz.Infrastructure -s src/ShareSmallBiz.Infrastructure -o Data/Migrations
```

## Configuration

Secrets never go in `appsettings*.json`. On the server, use environment variables (`__` separates sections).
The app refuses to start when any required setting is missing or unsafe.

| Setting | Required | Notes |
|---|---|---|
| `Jwt__SigningKey` | yes | 32+ bytes, random |
| `ConnectionStrings__Default` | yes | e.g. `Data Source=C:\data\sharesmallbiz.db` |
| `Email__Provider` | yes | `Log` is Development-only; a real provider comes with deployment |
| `Client__BaseUrl` | no | Where email links point; default `https://sharesmallbiz.com` |
| `DataProtection__KeysPath` | recommended | Persists keys so email links survive restarts |
| `Cors__AllowedOrigins__0..n` | no | Default: `https://sharesmallbiz.com`, `https://www.sharesmallbiz.com` |

## Auth model

- Email + password login → 15-minute JWT access token + 14-day refresh token.
- Refresh tokens rotate on every use and are stored only as SHA-256 hashes. Replaying a spent token revokes that whole session.
- Accounts must confirm their email before signing in. Five failed logins lock the account for 15 minutes.
- Auth endpoints are rate-limited per IP. Registration and password-reset responses never reveal whether an email has an account.
- Only `CurrentUserResponse` (`GET /api/v1/me`) carries private fields. `ContractTests` fail the build if another schema exposes them.
