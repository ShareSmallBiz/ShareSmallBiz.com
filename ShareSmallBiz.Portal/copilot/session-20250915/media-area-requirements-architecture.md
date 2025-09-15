# Media Area Requirements & Architecture

Version: 1.0  
Date: 2025-09-15  
Status: Draft  
Scope: Media Area (Library / User Media / Unsplash / YouTube integrations / File Upload & Storage Abstraction)

---

## 1. Overview

The Media Area enables authenticated users to ingest, manage, discover, and reuse media assets (images, videos, audio, documents, remote links via Unsplash & YouTube) within the ShareSmallBiz platform. It provides a unified abstraction over local storage and external sources, enforcing validation, attribution, and standardized metadata for downstream features (posts, comments, profiles).

### 1.1 Objectives

- Centralize all media CRUD and retrieval interactions.
- Provide safe, validated upload pipeline with extensible storage provider model.
- Allow external media referencing (Unsplash images, YouTube videos) with attribution & metadata capture.
- Enable thumbnail generation & media-type‑aware presentation.
- Support reuse: linking media to posts, comments, and user profiles without duplication.
- Lay foundation for future enhancements (CDN, AI tagging, transformations, multi-provider cloud storage).

### 1.2 Out of Scope (Current Iteration)

- Video transcoding / adaptive streaming.
- Image AI-based tagging or auto-captioning.
- DRM, signed URLs, or time‑limited access tokens.
- Bulk import/export tooling.
- Content moderation automation (manual only via future Admin tooling).

---

## 2. Stakeholders & Roles

| Role | Permissions (Media Context) |
|------|-----------------------------|
| Anonymous Visitor | View public media when embedded in public pages (indirect only). |
| Authenticated User | Upload (subject to size/type limits), list own media, search own media, attach media to owned posts/comments, add external Unsplash/YouTube references, delete own media. |
| Moderator/Admin | All user capabilities plus inspect/delete any media, audit metadata, enforce policy. |

Role enforcement integrates with existing ASP.NET Core Identity & authorization attributes; Admin tooling may evolve in `Areas/Admin`.

---

## 3. Functional Requirements

### 3.1 Media Types Supported

- Image (jpeg, png, gif, webp, svg*)
- Video (mp4, webm)
- Audio (mp3, wav, ogg subset)
- Document (pdf, doc/docx, xls/xlsx, txt)
- External / Link-based (Unsplash images, YouTube videos)
- Other (fallback classification)

### 3.2 Core Use Cases

| ID | Title | Description | Actors | Success Criteria |
|----|-------|-------------|--------|------------------|
| UC1 | Upload Local File | User uploads an image/video/etc. | Auth User | Stored on disk, metadata persisted; validation passes. |
| UC2 | Create External Media (Unsplash) | User searches Unsplash and selects image. | Auth User | Media record created with attribution + metadata, no file stored locally (unless future caching). |
| UC3 | Create External Media (YouTube) | User adds a YouTube URL (or via search). | Auth User | Media record created with video ID, embed URL derivable. |
| UC4 | List My Media | User views paginated list of own media items. | Auth User | Items returned ordered by CreatedDate desc. |
| UC5 | Search My Media | User searches filename/description/attribution. | Auth User | Filtered results returned in < 1s median. |
| UC6 | View Media Details | User/admin views metadata for a media item. | Auth / Admin | All stored metadata visible; permissions enforced. |
| UC7 | Attach Media to Post/Comment | User chooses existing media while authoring content. | Auth User | Post/Comment references mediaId; no duplication. |
| UC8 | Generate Thumbnail | System generates and serves thumbnail for images. | System | First request triggers generation; subsequent cached. |
| UC9 | Delete Media | User deletes own media (or Admin any). | Auth / Admin | File & thumbnails removed (local), DB row removed, external links unaffected. |
| UC10 | Profile Picture Upload | User uploads image used for profile. | Auth User | Resized/validated, saved to profiles path, media record updated. |
| UC11 | Validate File | System enforces allowed extensions and size. | System | Invalid uploads rejected with clear message. |
| UC12 | Audit External Attribution | Admin spot-checks Unsplash attribution. | Admin | Correct format persisted ("Photo by X on Unsplash"). |

### 3.3 CRUD + Query Requirements

- Create: Local upload or external link creation.
- Read: By Id (with ownership filter), by User, by Type, by Storage Provider, by search term.
- Update: Limited to mutable fields (FileName, Description, Attribution, associations) — binary content replacement not required v1.
- Delete: Hard delete (no soft delete yet) — future enhancement may introduce soft delete for retention.

### 3.4 Validation & Business Rules

- File size limit: from `MediaStorageOptions.MaxFileSize` (configurable).
- Allowed extensions: from configuration (`AllowedExtensions`).
- Forbidden: Executables / scripts not in whitelist.
- Unsplash: Must store photo ID and required attribution format.
- YouTube: Must store videoId; embed URL generated from ID (not stored separately unless beneficial for performance).
- Duplicate detection (Optional v1): Not enforced; future could hash file content.
- Thumbnails only for Image + LocalStorage.

### 3.5 Error Handling

- All service-layer operations log errors via `ILogger<T>`.
- User-facing messages generic ("Upload failed") while logs capture stack + context.
- Concurrency: Update uses EF Core; concurrency conflicts logged and return `false`.

### 3.6 Security & Authorization

- Only owner or admin can view raw file stream for private items.
- Media retrieval endpoint must validate ownership unless asset is referenced by a public post (future optimization: publish state check).
- Validate and sanitize filenames (already sanitized for Unsplash; add server-side override for uploads to avoid path traversal).
- Do not trust client-supplied MIME type alone—rely on server inference (`GetContentTypeFromFileName`) plus whitelist.

### 3.7 Performance

- Thumbnail generation lazy; cache on disk.
- DB queries use indexed columns (Id PK, UserId typical). Consider index on `(UserId, CreatedDate desc)` and `(UserId, MediaType)`.
- Pagination for large sets (future: add skip/take to listing methods; currently returns full list — improvement target).

### 3.8 Logging & Observability

- Log at Information: successful creation (Id, UserId, Type).
- Log at Warning: suspicious validation failures, repeated large rejects.
- Log at Error: external API failures (Unsplash/YouTube), storage IO errors, thumbnail generation failures.
- Correlate requests via existing logging pipeline (Serilog enrichment) — include `MediaId` property.

### 3.9 Internationalization

- Textual metadata stored Unicode (EF Core default). No special i18n transformations required v1.

### 3.10 Accessibility

- Encourage population of Description / AltDescription for images; future: enforce required alt text for images embedded in posts.

---

## 4. Non-Functional Requirements

| Category | Requirement |
|----------|------------|
| Reliability | 99% success rate for valid uploads under normal disk availability. |
| Availability | Same as site availability (no separate cluster). |
| Performance | Thumbnail generation < 400ms for typical images (<2MB). |
| Scalability | Can scale vertically initially; abstraction allows cloud provider integration later. |
| Maintainability | Clear service boundaries; extension via new `IStorageStrategy` (future) or new external providers without refactoring core. |
| Security | Enforce extension + size whitelist; no execution of uploaded content. |
| Privacy | Only owner/admin can enumerate user-specific media. |
| Auditability | Attribution & metadata stored JSON string for external sources. |
| Portability | Storage root configurable via `MediaStorage:RootPath`. |
| Testability | Service methods injectable; external APIs via `IHttpClientFactory`. |

---

## 5. Domain & Data Model

### 5.1 Core Entity (Inferred)

`Media` (DB):

- Id (int, PK)
- FileName (string)
- MediaType (enum: Image, Video, Audio, Document, Other)
- StorageProvider (enum: LocalStorage, External, YouTube, (future: AzureBlob, S3))
- Url (string) – full path (local) or external URL.
- ContentType (string) – server inferred or explicit for external.
- FileSize (long?) – null for external references.
- Description (string?)
- StorageMetadata (string) – JSON blob for provider-specific data (Unsplash IDs, YouTube metadata, etc.).
- Attribution (string?) – required for Unsplash Photos.
- UserId (string) – FK to AspNetUsers.
- PostId (int?, optional association)
- CommentId (int?, optional association)
- CreatedDate / ModifiedDate (tracked by EF override)

### 5.2 View Models

- `MediaModel` (service-level) – mirrors entity + Created/Modified + link associations.
- `Unsplash*ViewModel` (search, user, photo) – shapes remote API payload for UI.
- `YouTube*ViewModel` (search results, video details, channel details) – shapes remote API payload.
- Composite screens: `MediaIndexViewModel`, `LibraryMediaViewModel`, `ProfileMediaViewModel`, etc.

### 5.3 Enumerations

- `MediaType` — classification for UI decisions (icons, thumbnail logic).
- `StorageProviderNames` — route logic for Upload, Retrieval, Delete.

### 5.4 Relationships

- Media optionally linked to Post or Comment (one-to-many from Post/Comment perspective).
- User has many Media.
- Future: Many-to-many tagging (Media <-> Keywords) using join table (follow existing pattern). Potential `MediaTags` table.

---

## 6. Service Responsibilities

| Service | Responsibility | Notes |
|---------|----------------|-------|
| `MediaService` | CRUD + queries over `Media` entity; mapping to `MediaModel`; deletion orchestrates storage deletion. | Concurrency safe; improve pagination later. |
| `StorageProviderService` | Physical file handling (local), thumbnail generation, profile picture storage, content type inference, YouTube URL helpers (partial duplication w/ `YouTubeService`). | Consider extracting shared YouTube ID logic to single service to avoid duplication. |
| `UnsplashService` | External API search, user, photo retrieval; constructing attribution + metadata; creating `MediaModel` via `FileUploadService`. | Respects Unsplash attribution guidelines. |
| `YouTubeService` | YouTube API search, channel/video detail retrieval, metadata shaping, formatting durations/views, ID extraction. | Embed URL builder. |
| `FileUploadService` | (Not shown here) likely orchestrates creating DB record for upload or external link (via `MediaService`). | Ensure validation pipeline centralization. |
| `MediaFactoryService` | (Not reviewed) likely for building models or mass-transformations. | Evaluate relevance; keep Single Responsibility. |

### 6.1 Cross-Cutting Concerns

- Logging: Each service logs context-rich errors.
- Configuration: `MediaStorageOptions` drives validation.
- DI Registration: Centralized via `MediaServiceExtensions` following repo pattern.

---

## 7. Controllers & UI Layer (Inferred)

Controllers in `Areas/Media/Controllers/`:

- `MediaController` – core CRUD + streaming endpoints.
- `UserMediaController` – user-focused listing & profile integration.
- `LibraryController` – possibly curated or shared repository view.
- `UnsplashController` / `UnsplashMediaController` – search/select/import flows.
- `YouTubeController` – search & display YouTube content.

Views follow area folder: strongly typed Razor pages using corresponding view models; `_ViewImports` sets namespaces.

### 7.1 Routing Pattern

Area route: `/Media/{controller}/{action}/{id?}` (implicitly). Keep consistent with global area routing in `Program.cs`.

### 7.2 UX Considerations

- Tabbed interface for: My Uploads | Unsplash | YouTube | Library (shared) | Upload New.
- Inline validation errors for file uploads (size, extension) shown before server round-trip using JS mirror of config (optional enhancement).
- Lazy-load thumbnails with fallback icons.

---

## 8. External Integrations

### 8.1 Unsplash

- Auth via Access Key (env/config). Rate limiting must be handled gracefully (log + friendly error).
- Required attribution persisted.
- Register downloads per API spec (already triggers download endpoint before actual asset retrieval in `DownloadImageAsync`).
- Potential caching of JSON responses (MemoryCache) for repeated queries — future improvement.

### 8.2 YouTube Data API

- Key via `GOOGLE_YOUTUBE_API_KEY` env/config.
- Search, channel, video detail, related videos endpoints consumed.
- Duration parsing (ISO 8601) converted to user-friendly format.
- View count formatting supports large numbers abbreviations.

---

## 9. File Storage & Paths

- Root: `MediaStorage:RootPath` (default `c:/websites/sharesmallbiz/media`).
- Subdirectories: `/uploads`, `/thumbnails`, `/profiles`.
- File naming: sanitized user-supplied or provider-derived; consider adding unique suffix (GUID/short hash) to prevent collision — planned enhancement.
- Thumbnails: `thumb_{width}x{height}_{OriginalFileName}`.

### 9.1 Future Cloud / CDN Strategy

- Introduce abstraction interface: `IMediaStorageStrategy` with implementations: Local, AzureBlob, S3.
- Use DI to inject appropriate strategy based on config flag.
- Add CDN base URL rewriting layer for public delivery.

---

## 10. Security & Compliance

| Aspect | Measure |
|--------|--------|
| Path Traversal | Enforce server-generated final file path; ignore directory hints in uploaded filename. |
| MIME Sniffing | Provide server-side `Content-Type` header; consider `X-Content-Type-Options: nosniff` globally. |
| Virus Scanning | (Future) Hook to scanning service for documents/videos. |
| Rate Limiting | (Future) Limit uploads per user per minute to mitigate abuse. |
| Secrets | Keys stored in configuration providers (User Secrets/dev; env vars/prod). |
| PII | Media metadata expected non-sensitive; still treat internal paths as non-public. |

---

## 11. Performance & Scaling Considerations

| Concern | Current Approach | Improvement Path |
|---------|------------------|------------------|
| Listing Large Media Sets | Full list queries | Introduce pagination parameters & indexes |
| Thumbnail Generation | On-demand, synchronous | Background job + pre-generation for popular images |
| External API Calls | Direct, no caching | Add in-memory / distributed cache w/ short TTL |
| File Serving | Local disk FileStream | Move to CDN-backed blob store |
| Search | SQL LIKE | Implement full-text index or tagging service |

---

## 12. Error Handling & Logging Strategy

- Wrap external API calls in try/catch; log structured context (Query, Username, PhotoId, VideoId).
- Propagate user-safe exceptions upward; convert to problem details or user-friendly message.
- Consider introducing custom exception types (`MediaValidationException`, `ExternalMediaException`) for clarity.

---

## 13. Extensibility Patterns

| Extension Point | Strategy |
|-----------------|----------|
| New storage provider | Add new `StorageProviderNames` enum value + implement new strategy behind interface (future refactor). |
| New external source (e.g., Vimeo) | Mirror `UnsplashService` pattern: API client + metadata creation + attribution rules. |
| Media transformations | Introduce `IMediaProcessor` pipeline (resize, watermark). |
| Tagging / AI | Add `MediaTag` entity + background enrichment service. |
| Soft Delete / Retention | Add `IsDeleted`, filter queries, background purge. |

---

## 14. Implementation Plan (Greenfield New Site)

### Phase 0: Foundations

1. Create `Media` entity + enums in Data layer (if not present) inheriting `BaseEntity` for timestamps.
2. Add EF migration: `YYYYMMDDHHMMSS_AddMedia` with indexes on `(UserId, CreatedDate)` and `(UserId, MediaType)`.
3. Add `MediaStorageOptions` strongly typed config + register via `Configure<MediaStorageOptions>()` in an extension method.

### Phase 1: Services

1. Implement `MediaService` (CRUD, queries) — ensure async, mapping helpers.
2. Implement `StorageProviderService` (local disk only initially) — directory bootstrap, validation, thumbnail creation.
3. Implement `FileUploadService` orchestrating create (local vs external) calling `StorageProviderService` + `MediaService`.
4. Implement `UnsplashService` and `YouTubeService` (HTTP client + DTO models) with resilient logging.
5. Register all via `MediaServiceExtensions.AddMediaServices(this IServiceCollection)`.

### Phase 2: Controllers & Views

1. Scaffold area `Areas/Media` structure (Controllers, Models, Views) using consistent pattern.
2. Implement `MediaController` endpoints:
   - GET Index (paged)
   - GET Details/{id}
   - GET Thumbnail/{id}
   - POST Upload (multipart/form-data)
   - POST External (Unsplash/YouTube)
   - DELETE / POST Delete
3. Implement specialized controllers: `UnsplashController`, `YouTubeController`, `UserMediaController`.
4. Razor views: reuse partials for media cards, thumbnail tag helper (optional).

### Phase 3: Integration

1. Embed media selection modal into Post/Comment authoring (JS fetch from `/Media/UserMedia/List`).
2. Add profile image upload hooking into `StorageProviderService.SaveProfilePictureAsync`.
3. Provide helper method for generating display URLs (`MediaService.GetMediaUrl`).

### Phase 4: Hardening

1. Add unit tests: services (validation, mapping, YouTube ID extraction, Unsplash attribution).
2. Add integration tests: upload workflow, external link creation.
3. Add logging enrichment (Serilog `MediaId`).
4. Add config validation on startup (fail fast if keys missing in Production when features enabled).

### Phase 5: Enhancements (Post-MVP)

- Pagination parameters for listing.
- Caching of Unsplash/YouTube search responses (MemoryCache with 60s TTL).
- Unique file naming strategy (GUID + sanitized slug).
- Soft delete column + Admin restore.
- Abstraction for multi-provider storage (Azure Blob, S3) with feature flag.
- CDN integration & signed thumbnail URLs.

### 14.1 Acceptance Criteria Summary

- Users can upload allowed file types within size limit; invalid attempts rejected.
- Unsplash search returns results and selected photo creates media record with proper attribution.
- YouTube video addition stores metadata and renders via embed URL.
- Thumbnails accessible at predictable `/Media/Thumbnail/{id}` route for local images.
- Deleting media removes file and thumbnails (local) and DB record.
- All service methods covered by baseline unit tests (>=70% statement coverage for Media & Storage).

---

## 15. Risks & Mitigations

| Risk | Impact | Mitigation |
|------|--------|-----------|
| Large file uploads exhaust disk | High | Enforce size limit; monitor disk usage; future: move to scalable object store. |
| External API rate limit | Medium | Implement minimal caching + exponential backoff logging. |
| Filename collisions | Medium | Append unique suffix (future). Current risk low if user varied names. |
| Security - malicious file disguised | Medium | Rely on extension + content type, add magic-byte sniffing (future). |
| Thumbnail generation failure | Low | Fallback to original or placeholder icon. |
| API key leakage | High | Store only in secure configuration provider; never log keys. |
| Performance degradation with large media sets | Medium | Add pagination + indexes early. |
| Data inconsistency (deleted file but DB row remains) | Low | Reverse order deletion (file after DB) or add compensating job; currently delete file first but handle failures with try/catch. |

---

## 16. Future Enhancements (Backlog)

1. Multi-tenancy media isolation (directory partitioning per tenant).
2. Configurable image transformations (webp conversion, quality setting).
3. AI-powered tagging / OCR extraction of text from images.
4. Video transcoding + adaptive streaming (HLS) via background workers.
5. Media usage analytics (views, embed counts) with counters table.
6. Content moderation pipeline (NSFW detection + quarantine state).
7. Bulk upload & drag-and-drop multi-file queue with progress UI.
8. Pre-signed URLs for direct-to-cloud uploads (bypass web server load).
9. Encrypted at rest storage for sensitive documents.
10. Webhook ingestion for external provider events (e.g., YouTube channel updates).

---

## 17. Open Questions

| Topic | Question | Proposed Resolution |
|-------|----------|---------------------|
| Public Access Rules | Are all user media private until attached to public content? | Implement `IsPublic` flag later; for now assume private to owner unless embedded. |
| Soft Delete | Required for compliance? | Post-MVP, add `IsDeleted`. |
| Versioning | Overwrite vs new record on re-upload? | Current: update metadata only; new binary => new record (later). |
| External Caching | Cache Unsplash/YouTube responses? | Acceptable short-term improvement; low complexity. |

---

## 18. Alignment with Repository Conventions

- Services registered through a dedicated extension class (e.g., `MediaServiceExtensions`).
- Controllers thin; business logic in providers/services (pattern followed).
- Async service calls consistent with existing style (`ConfigureAwait(false)` optional—match file style when adding).
- Entities use EF Core with timestamp override logic (no manual ModifiedDate setting outside service mapping except where legitimate).
- Logging uses structured messages with identifiers.

---

## 19. Glossary

| Term | Definition |
|------|------------|
| Attribution | Required credit line for third-party asset source (e.g., Unsplash). |
| External Media | Media not stored locally but referenced via URL and metadata. |
| Storage Provider | Logical bucket determining how media is stored/served (Local, External, YouTube, etc.). |
| Thumbnail | Reduced-size derivative of an image for quick display. |

---

## 20. References

- Unsplash API Guidelines: <https://help.unsplash.com/>
- YouTube Data API: <https://developers.google.com/youtube/v3>
- ASP.NET Core File Upload Security Guidance (Microsoft Docs)

---
End of Document.
