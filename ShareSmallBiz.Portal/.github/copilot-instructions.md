## ShareSmallBiz.Portal – AI Agent Working Guide

Keep answers concrete and code-aware. Prefer referencing existing patterns over inventing new ones.

### 1. Azure Rule (Preserve)

- @azure Rule - Use Azure Best Practices: When generating code for Azure, running terminal commands for Azure, or performing operations related to Azure, invoke your `azure_development-get_best_practices` tool if available.

### 2. High-Level Architecture

- ASP.NET Core 9.0 MVC + Razor Pages + Areas (e.g., `Areas/Media`, `Areas/Admin`).
- Central composition root: `Program.cs` wires services exclusively via extension methods in `Infrastructure/Extensions/*Extensions.cs` (keep this pattern for new concerns).
- Domain/Data layer via EF Core Sqlite: `ShareSmallBizUserContext` (Identity + application entities) under `Data/` with rich relationship config and automatic date tracking override.
- Services layer: `Infrastructure/Services/*Provider|*Service` encapsulates business logic (e.g., `DiscussionProvider`, `MailerSendService`). Controllers stay thin.
- Logging via Serilog configured in `Infrastructure/Logging/ConfigureLogging.cs` invoked early in `Program.cs`.

### 3. Dependency Injection Conventions

- Add new service registrations by creating/augmenting a `*Extensions.cs` file; do NOT inline registrations in `Program.cs`.
- Naming: Use `AddXyzServices()` returning `IServiceCollection` (see `ApplicationServicesExtensions`).
- Lifetime guidelines inferred: cross-request state = scoped; stateless helpers = singleton (e.g., `ApplicationStatus`, `IStringConverter`). Follow existing lifetimes when adding related services.

### 4. Data & Entities

- All persisted entities inherit (directly or indirectly) from `BaseEntity` when timestamps are needed; timestamps auto-updated in overridden `SaveChanges*` methods—never set `ModifiedDate` manually.
- Migration naming pattern: `yyyyMMddHHmmss_Description` (see `Migrations/`). Keep this exact timestamp prefix (UTC implied).
- Relationship patterns: Many-to-many uses `.UsingEntity(j => j.ToTable("PostKeywords"));`. Follow that style (explicit join table naming) when adding new many-to-many sets.

### 5. Common Business Logic Patterns

- Slug creation: reuse `DiscussionProvider.GenerateSlug`—do not duplicate slug logic.
- Converting stored multiline text to HTML line breaks uses `DiscussionProvider.StringToHtml` before returning content.
- Pagination pattern: Accept `(pageNumber, pageSize, SortType)` and return a DTO with `Posts`, `CurrentPage`, `PageSize`, `TotalCount` (see `PaginatedPostResult`). Match this shape for new paged queries.
- Feature queries (`FeaturedPostsAsync`, `MostCommentedPostsAsync`, etc.) follow: filter public, include navigation (`Author`, `Comments.Author`), apply ordering, `Take(count)`, `AsNoTracking()`. Replicate ordering + projection style for new summaries.

### 6. Identity & Auth

- Identity configured via `AddIdentityServices` with enforced confirmed email + unique email; do not weaken these without explicit instruction.
- JWT settings pulled from `JwtSettings:*` in configuration; if adding endpoints requiring JWT, ensure `[Authorize]` and proper scheme alignment.
- DataProtection keys persisted to `C:\websites\ShareSmallBiz\keys`; keep path configurable if introducing multi-environment support.

### 7. HTTP / External Integrations

- Outbound email: `MailerSendService` uses `MAILER_SEND_API_KEY` and raw `HttpClient` JSON payload (`/v1/email`). Reuse this service; do not handcraft new mail calls.
- SMTP fallback / confirmation emails handled through `IEmailSender` implementation (`SmtpEmailSender`). Use interface injection, not concrete type.
- HttpClient helpers likely added via `AddHttpClientUtilities` (see extension usage). New HttpClients should go through that pattern.

### 8. Logging & Diagnostics

- Prefer `ILogger<T>` injection; avoid static logging except where already established (e.g., initial Serilog bootstrap).
- Noise reduction: Framework categories filtered to Error—log app-level info with moderation; avoid verbose debug unless behind conditional dev checks.
- When adding complex operations (e.g., batch updates), log intent + key identifiers (`{EntityId}`, `{UserId}`) consistent with `DiscussionProvider` style.

### 9. Front-End Asset Pipeline

- Node build auto-runs before .NET build via MSBuild `Target Name="EnsureNodeModules"` (updates packages with `ncu -u`, installs, builds). Keep deterministic build speed—avoid expensive extra commands here.
- Scripts in `package.json`: `build:scss`, `build:scripts`, `build:assets`. Add new asset steps by extending `scripts/` JS build helpers, then reference them in `build` script chain.
- Avoid adding large frontend bundlers unless justified—current approach is modular custom scripts.

### 10. Running & Workflows

- Build: `dotnet build ShareSmallBiz.Portal.csproj` (task: `build`).
- Run (hot reload): `dotnet watch run --project ShareSmallBiz.Portal.csproj` (task: `watch`).
- EF migrations: `dotnet ef migrations add <Timestamp_Description>` (timestamp prefix manually added) then `dotnet ef database update`.
- Swagger UI at `/swagger` configured explicitly with `RoutePrefix = "swagger"`.

### 11. Controllers & Routing

- Area routing pattern: `{area:exists}/{controller=Home}/{action=Index}/{id?}` plus default route. Keep fallbacks consistent (`MapFallbackToController("GetError","Home")`). New catch-all behavior should funnel through existing error action unless a distinct UX is specified.
- Controllers stay lean—delegate to `*Provider` or `*Service`. If logic grows > ~30 lines, migrate to a service.

### 12. Middleware & Order

- Order in `Program.cs` is intentional: `UseAuthentication` BEFORE `UseAuthorization`, session AFTER authorization, CORS named policy `AllowAllOrigins` applied after session setup. Preserve order when inserting new middleware (place custom diagnostic middleware early if it inspects auth state).

### 13. Coding Style / Patterns

- Favor async + `ConfigureAwait(false)` in deep service methods (pattern appears selectively—follow existing usage within a file for consistency).
- Entity fetch + mutate + save pattern: eager include nav properties when the result is rendered; use `AsNoTracking()` for read-only lists.
- Concurrency: Non-critical concurrency exceptions (e.g., view count) are caught and logged—mimic this where counters are best-effort.

### 14. Adding New Features (Example Flow)

1. Define entity (inherit `BaseEntity` if auditing needed) and relationships in `ShareSmallBizUserContext`.
2. Add migration with timestamped name.
3. Create service (`Infrastructure/Services/NewFeatureProvider.cs`) encapsulating logic.
4. Register via an existing or new extension method.
5. Inject service into controller or Razor Page; keep controller slim.
6. Add Razor views under appropriate `Views/` or `Areas/<AreaName>/Views/` path following naming conventions.

### 15. When Unsure

- Prefer extending established extension classes over creating ad-hoc bootstrapping code.
- If a new concern spans multiple layers (auth, caching, logging), propose placement referencing existing extension patterns.

Provide changes as minimal diffs and reference concrete file paths. Ask for clarification only when blocked by missing domain rules (e.g., new authorization policies).

### 16. Copilot Documentation Output Location

- All AI/Copilot generated documentation or exploratory notes (`*.md`) MUST be placed under `/copilot/session-{yyyyMMdd}/`.
- Never write generated markdown into code, domain, or config directories (`Data/`, `Infrastructure/`, `Areas/`, `Controllers/`, `Views/`, root) unless explicitly instructed.
- If the session folder for today does not exist, create it (e.g., `/copilot/session-20250915/`).
- Name files descriptively: `architecture-notes.md`, `migration-plan.md`, etc. Avoid overwriting prior session artifacts—new day, new folder.
- When referencing these docs in PRs, link relative to repository root (e.g., `copilot/session-20250915/architecture-notes.md`).

---

If any of these sections seem incomplete (e.g., caching policy details, media endpoint expectations), indicate which to expand.
