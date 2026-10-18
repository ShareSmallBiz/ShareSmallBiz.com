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

## Validate locally (nothing here touches the live site)

```powershell
./scripts/api-local.ps1 -Reset    # terminal 1: fresh API on http://localhost:5280 (docs at /scalar/v1)
./scripts/api-smoke.ps1           # terminal 2: end-to-end acceptance checks, PASS/FAIL per check
dotnet test ShareSmallBiz.Api.slnx  # integration tests (also run by CI on every PR)
```

- `api-local.ps1` creates a random dev JWT key in user secrets on first run. Its data is local only:
  `src/ShareSmallBiz.Api/sharesmallbiz-dev.db`, with account emails written as text files to `src/ShareSmallBiz.Api/dev-mail/`.
- `api-smoke.ps1` walks the whole account lifecycle over real HTTP. It refuses any non-localhost URL.
- `src/ShareSmallBiz.Api/ShareSmallBiz.Api.http` has requests for manual exploring in VS Code or Visual Studio.

Nothing is deployed from this branch. CI builds and tests only. Production cutover happens only after
local validation and explicit approval.

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
