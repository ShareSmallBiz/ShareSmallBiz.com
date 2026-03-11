# ShareSmallBiz Web — API Gap Analysis

> **Status as of 2026-03-11: ALL 14 GAPS RESOLVED ✅**
>
> This document was originally written as a specification for missing API capabilities.
> All identified gaps have been implemented across three phases (Phase A, B, C).
> The [api-developer-guide.md](api-developer-guide.md) (sections 19–26) is the canonical
> reference for these endpoints going forward.
>
> This file is retained as a historical record of what was identified, specified, and delivered.

Access level key:

| Symbol | Meaning |
|---|---|
| `[Public]` | No `Authorization` header required |
| `[Authenticated]` | Valid JWT required |
| `[Admin]` | Valid JWT + Admin role required |

---

## Table of Contents

1. [Community Stats](#1-community-stats)
2. [Search](#2-search)
3. [User Preferences / Settings](#3-user-preferences--settings)
4. [Notifications](#4-notifications)
5. [Direct Messages](#5-direct-messages)
6. [Follow Status on Profile Listings](#6-follow-status-on-profile-listings)
7. [Post Interactions — Saves and Shares](#7-post-interactions--saves-and-shares)
8. [Comment Likes](#8-comment-likes)
9. [Events](#9-events)
10. [Password Reset](#10-password-reset)
11. [AI Assistant](#11-ai-assistant)
12. [Missing Fields on Existing Models](#12-missing-fields-on-existing-models)
13. [CMS / Public Articles](#13-cms--public-articles)
14. [Summary Table](#14-summary-table)

---

## 1. Community Stats

**Gap:** The home page hero and right sidebar display platform-wide figures (total members,
total discussions, partnerships formed). There is no public endpoint that returns these
aggregate numbers. `GET /api/admin/dashboard` contains this data but requires the Admin role.

**Status: ✅ IMPLEMENTED — Phase A**

- `StatsService` + `StatsController` added
- Results cached 5 minutes via `IMemoryCache`
- See [api-developer-guide.md §19](api-developer-guide.md#19-community-stats)

---

### GET /api/stats `[Public]`

Return a lightweight snapshot of platform-wide activity. Cached server-side; does not
need to be real-time.

**Response `200 OK`:**

```json
{
  "totalMembers": 2847,
  "totalDiscussions": 4821,
  "totalArticles": 42,
  "totalKeywords": 42,
  "memberGrowthThisMonth": 83
}
```

---

## 2. Search

**Gap:** The navigation bar contains a full-width search input. There is no search endpoint.
The current implementation does nothing when the user types.

**Status: ✅ IMPLEMENTED — Phase A**

- `SearchService` + `SearchController` added
- Case-insensitive full-text search across Posts (title/description/content), Users (displayName/bio), Keywords
- `SearchResultModel` collections default to empty lists (not null) when filtered by type
- See [api-developer-guide.md §20](api-developer-guide.md#20-search)

---

### GET /api/search `[Public]`

Full-text search across discussions, profiles, and keywords. Returns grouped results.

| Query param | Type | Required | Description |
|---|---|---|---|
| `q` | `string` | yes | Search term (min 2 characters) |
| `type` | `string` | no | Filter to `discussions`, `profiles`, or `keywords`. Omit for all. |
| `pageSize` | `int` | no | Max results per group. Default `5`. |

**Response `200 OK`:**

```json
{
  "discussions": [ { "id": 101, "title": "...", "slug": "...", "description": "...", "createdDate": "..." } ],
  "profiles":    [ { "id": "3fa85f64-...", "displayName": "Jane Smith", "userName": "jane.smith", "profilePictureUrl": "..." } ],
  "keywords":    [ { "id": 7, "name": "Marketing", "postCount": 312 } ]
}
```

**Error responses:**

| Status | Reason |
|---|---|
| `400` | `q` is missing or shorter than 2 characters |

---

## 3. User Preferences / Settings

**Gap:** The Settings page has tabs for notification preferences and privacy controls.
There is no endpoint to read or write these settings.

**Status: ✅ IMPLEMENTED — Phase A**

- `UserPreference` entity added (migration `20260311083125_PhaseA_DatabaseSchema`)
- `UserSettingsController` at route `api/users/{userId}/settings`
- Fields: email notifications, per-event toggles (follower/comment/like/DM), privacy settings
- See [api-developer-guide.md §21](api-developer-guide.md#21-user-settings)

---

### GET /api/users/{userId}/settings `[Authenticated]`

Return the caller's preference settings. The caller must be the account owner.

### PUT /api/users/{userId}/settings `[Authenticated]`

Update preference settings.

---

## 4. Notifications

**Gap:** The navigation header shows a bell icon with a badge count. There is no
notifications endpoint of any kind.

**Status: ✅ IMPLEMENTED — Phase B**

- `Notification` entity + migration `20260311131707_PhaseB_NewTables`
- `NotificationService` + `NotificationsController`
- Self-notifications are skipped automatically
- Notifications created on follow, comment, like, and direct message events
- See [api-developer-guide.md §22](api-developer-guide.md#22-notifications)

---

### GET /api/notifications `[Authenticated]`

Return the caller's notifications, newest first.

| Query param | Type | Default |
|---|---|---|
| `unreadOnly` | `bool` | `false` |
| `pageSize` | `int` | `20` |
| `pageNumber` | `int` | `1` |

### PUT /api/notifications/{id}/read `[Authenticated]`

Mark a single notification as read. Returns `403` if not owned by caller.

### POST /api/notifications/read-all `[Authenticated]`

Mark all notifications as read. Returns `{ "markedCount": N }`.

---

## 5. Direct Messages

**Gap:** The navigation header shows a mail icon with a badge count. There is no
messaging endpoint.

**Status: ✅ IMPLEMENTED — Phase B**

- `DirectMessage` entity + migration `20260311131707_PhaseB_NewTables`
- `MessageService` + `DirectMessagesController`
- Deterministic `ConversationId` = `min(userA,userB)_max(userA,userB)` ensures both directions share same thread
- Sending a message auto-creates a notification to the recipient
- Getting messages auto-marks incoming unread messages as read
- See [api-developer-guide.md §23](api-developer-guide.md#23-direct-messages)

---

### GET /api/messages/conversations `[Authenticated]`

List the caller's conversations, most-recently-active first.

### GET /api/messages/conversations/{conversationId} `[Authenticated]`

Return messages in a conversation (paginated). Marks incoming messages as read on fetch.

### POST /api/messages `[Authenticated]`

Send a direct message. Returns `MessageModel` on `201 Created`.

---

## 6. Follow Status on Profile Listings

**Gap:** `GET /api/profiles` returns a list of `UserModel` objects but does not indicate
whether the authenticated caller is already following each profile.

**Status: ✅ IMPLEMENTED — Phase A**

- `ProfilesApiController.GetAll` and `GetBySlug` augmented with `isFollowedByMe`
- `followerCount` and `followingCount` added as denormalized fields to `ShareSmallBizUser` (migration Phase A)
- See [api-developer-guide.md §7](api-developer-guide.md#7-profiles) and [§27 UserModel](api-developer-guide.md#usermodel)

---

### Change to existing: GET /api/profiles `[Public]`

When an `Authorization` header is present, each `UserModel` now includes:

```json
{
  "followerCount": 128,
  "followingCount": 34,
  "isFollowedByMe": true
}
```

`isFollowedByMe` is `null` when the request is unauthenticated.

---

## 7. Post Interactions — Saves and Shares

**Gap:** The post card has Save (bookmark) and Share buttons. Neither has an API endpoint.

**Status: ✅ IMPLEMENTED — Phase B**

- `PostSave` entity + migration `20260311131707_PhaseB_NewTables` (unique index on UserId+PostId)
- `DiscussionProvider` extended: `SaveDiscussionAsync` (toggle), `GetSavedDiscussionsAsync`, `ShareDiscussionAsync`
- `DiscussionController` extended: `POST /{id}/save`, `GET /saved`, `POST /{id}/share`
- `Post.ShareCount` field added (migration Phase A)
- See [api-developer-guide.md §8](api-developer-guide.md#8-discussions)

---

### POST /api/discussion/{id}/save `[Authenticated]`

Toggle save. Returns `{ "saved": true|false }`.

### GET /api/discussion/saved `[Authenticated]`

Return saved discussions for the authenticated user.

### POST /api/discussion/{id}/share `[Authenticated]`

Increment share counter. Returns `{ "shareCount": N }`.

---

## 8. Comment Likes

**Gap:** `PostCommentModel` includes a `likeCount` field, but there is no endpoint to
like or unlike a comment.

**Status: ✅ IMPLEMENTED — Phase A**

- `CommentProvider.LikeCommentAsync` added (toggle)
- `CommentsController` extended with `POST /{id}/like`
- Returns `{ "liked": bool, "likeCount": int }`
- See [api-developer-guide.md §9](api-developer-guide.md#9-comments)

---

### POST /api/comments/{id}/like `[Authenticated]`

Toggle a like on a comment. Returns `{ "liked": true, "likeCount": 5 }`.

---

## 9. Events

**Gap:** The right sidebar displays upcoming local events. There is no events endpoint.

**Status: ✅ IMPLEMENTED — Phase C**

- `Event` entity + migration `20260311135517_PhaseC_NewTables`
- `EventService` (5-min cache keyed by date+count) + `EventsController`
- Organizer FK uses `OnDelete(DeleteBehavior.SetNull)` — events survive organizer account deletion
- See [api-developer-guide.md §25](api-developer-guide.md#25-events)

---

### GET /api/events `[Public]`

Upcoming events. `from` defaults to today (UTC). Cached 5 minutes.

### GET /api/events/{id} `[Public]`

Single event detail.

---

## 10. Password Reset

**Gap:** There is no forgot-password or reset-password flow. Users who forget their
password have no self-service recovery path.

**Status: ✅ IMPLEMENTED — Phase C**

- `POST /api/auth/forgot-password` and `POST /api/auth/reset-password` added to `AuthController`
- Uses ASP.NET Identity `UserManager.GeneratePasswordResetTokenAsync` / `ResetPasswordAsync` — no custom token table needed
- Always returns `200` on forgot-password to prevent account enumeration
- Reset link uses `AppSettings:FrontendUrl` config value
- `ForgotPasswordRequest` and `ResetPasswordRequest` DTOs added to `AuthModels.cs`
- See [api-developer-guide.md §6](api-developer-guide.md#6-auth-endpoints)

---

### POST /api/auth/forgot-password `[Public]`

Sends reset email. Always `200 OK`.

### POST /api/auth/reset-password `[Public]`

Completes reset using ASP.NET Identity token. `400` if token invalid/expired.

---

## 11. AI Assistant

**Gap:** The AI Assistant component is currently a "coming soon" placeholder because the
API exposes no AI endpoint.

**Status: ✅ IMPLEMENTED (Phase 1 placeholder) — Phase C**

- `AiController` added with `POST /api/ai/chat`
- Phase 1: context-aware curated responses for `retail`, `restaurant`, `services`, `ecommerce`, and a general default
- Response includes `response` (string), `suggestions[]`, and `actionItems[]`
- Phase 2 (future): replace with real LLM integration (Claude/OpenAI)
- See [api-developer-guide.md §26](api-developer-guide.md#26-ai-assistant)

---

### POST /api/ai/chat `[Authenticated]`

Returns structured advice with suggestions and action items. No database I/O in Phase 1.

---

## 12. Missing Fields on Existing Models

**Status: ✅ ALL FIELDS ADDED — Phase A**

### UserModel

| Field | Type | Status |
|---|---|---|
| `followerCount` | `int` | ✅ Added — denormalized on `ShareSmallBizUser` |
| `followingCount` | `int` | ✅ Added — denormalized on `ShareSmallBizUser` |
| `isFollowedByMe` | `bool?` | ✅ Added — computed in `ProfilesApiController` |

### DiscussionModel

| Field | Type | Status |
|---|---|---|
| `shareCount` | `int` | ✅ Added — `Post.ShareCount` column (Phase A migration) |
| `isSavedByMe` | `bool?` | ✅ Added — computed from `PostSave` table (Phase B) |

### PostCommentModel

| Field | Type | Status |
|---|---|---|
| `isLikedByMe` | `bool?` | ✅ Added — computed in `CommentsController` |

---

## 13. CMS / Public Articles

**Gap:** The platform needs a public-facing content layer — staff- or editor-authored
articles that are readable without a login and that search engines can index.

**Status: ✅ IMPLEMENTED — Phase B**

**Design decision:** Articles reuse the existing `Post` entity (`IsPublic=true`) with the
`Category` field (added Phase A migration) rather than a separate Article entity. This
avoids schema duplication while enabling all required filtering.

- `ArticleService` (15-min category cache, 5-min featured cache) + `ArticlesController`
- `GetBySlugAsync` increments `PostViews` on retrieval
- List responses omit `content` for payload efficiency; detail response includes it
- See [api-developer-guide.md §24](api-developer-guide.md#24-articles--cms)

---

### GET /api/articles `[Public]`

Paginated list. Filters: `category`, `tag`, `featured`.

### GET /api/articles/{slug} `[Public]`

Full article with content. Increments view count.

### GET /api/articles/featured `[Public]`

Top N featured articles.

### GET /api/articles/categories `[Public]`

Distinct categories with article counts (cached 15 min).

### GET /api/articles/related/{slug} `[Public]`

Same-category articles, excluding the specified slug.

---

## 14. Summary Table

| Gap | Endpoint(s) | Access | Status |
|---|---|---|---|
| Community stats | `GET /api/stats` | Public | ✅ Phase A |
| Search | `GET /api/search` | Public | ✅ Phase A |
| User settings read | `GET /api/users/{userId}/settings` | Authenticated | ✅ Phase A |
| User settings write | `PUT /api/users/{userId}/settings` | Authenticated | ✅ Phase A |
| Notifications list | `GET /api/notifications` | Authenticated | ✅ Phase B |
| Notifications mark read | `PUT /api/notifications/{id}/read`, `POST /api/notifications/read-all` | Authenticated | ✅ Phase B |
| Direct messages — list | `GET /api/messages/conversations` | Authenticated | ✅ Phase B |
| Direct messages — thread | `GET /api/messages/conversations/{id}` | Authenticated | ✅ Phase B |
| Direct messages — send | `POST /api/messages` | Authenticated | ✅ Phase B |
| Follow status on profile list | Augment `GET /api/profiles` response | Public (field null) / Authenticated | ✅ Phase A |
| Save / bookmark discussion | `POST /api/discussion/{id}/save`, `GET /api/discussion/saved` | Authenticated | ✅ Phase B |
| Share discussion | `POST /api/discussion/{id}/share` | Authenticated | ✅ Phase B |
| Like a comment | `POST /api/comments/{id}/like` | Authenticated | ✅ Phase A |
| Events list | `GET /api/events` | Public | ✅ Phase C |
| Events detail | `GET /api/events/{id}` | Public | ✅ Phase C |
| Forgot password | `POST /api/auth/forgot-password` | Public | ✅ Phase C |
| Reset password | `POST /api/auth/reset-password` | Public | ✅ Phase C |
| AI chat | `POST /api/ai/chat` | Authenticated | ✅ Phase C (placeholder) |
| `UserModel.followerCount` / `followingCount` / `isFollowedByMe` | Existing model change | — | ✅ Phase A |
| `DiscussionModel.shareCount` / `isSavedByMe` | Existing model change | — | ✅ Phase A/B |
| `PostCommentModel.isLikedByMe` | Existing model change | — | ✅ Phase A |
| CMS articles list | `GET /api/articles` | Public | ✅ Phase B |
| CMS article detail | `GET /api/articles/{slug}` | Public | ✅ Phase B |
| CMS featured articles | `GET /api/articles/featured` | Public | ✅ Phase B |
| CMS article categories | `GET /api/articles/categories` | Public | ✅ Phase B |
| CMS related articles | `GET /api/articles/related/{slug}` | Public | ✅ Phase B |
| `GET /api/stats` — add `totalArticles` | Existing endpoint change | Public | ✅ Phase A/B |

**All 14 gaps resolved. 26 endpoints implemented. 3 model changes delivered.**
