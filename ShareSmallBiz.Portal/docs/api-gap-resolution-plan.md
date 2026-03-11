# API Gap Resolution Plan

**Document Date:** 2026-03-11
**Status:** Phase A Complete ✅ | Phase B Queued
**Target Completion:** Phase C end of cycle

---

## Executive Summary

This document consolidates the comprehensive plan to address all 14 major API gaps identified in `api-gap-analysis.md`. The React web app requires significant API enhancements to provide a complete product experience. Implementation is organized into 3 prioritized phases, with Phase A focusing on high-impact, foundational features.

**Key Design Decision:** Articles are implemented as public posts (IsPublic=true) rather than a separate entity, reusing the Post infrastructure with a Category field.

---

## Completed Items ✅

### Migration & Database Schema
- **File:** `Migrations/20260311083125_PhaseA_DatabaseSchema.cs`
- **Status:** Created and ready to apply
- **Changes:**
  - **UserPreference table** (new): Stores notification preferences (EmailOnComment, EmailOnLike, EmailOnFollow, WeeklySummary) and privacy settings (ProfileVisibility, ShowEmail, ShowWebsite)
  - **Post table enhancements:** Added `Category` (string, 100 chars max) and `ShareCount` (int, default 0)
  - **ShareSmallBizUser table enhancements:** Added `FollowerCount` and `FollowingCount` (denormalized for performance)

### API Endpoints - Stats (Phase A)
- **Endpoint:** `GET /api/stats` [Public]
- **Status:** ✅ Implemented and integrated
- **Files:**
  - `Controllers/api/StatsController.cs` (controller)
  - `Controllers/api/StatsModel.cs` (DTO)
  - `Infrastructure/Services/StatsService.cs` (service)
- **Features:**
  - Returns: `totalMembers`, `totalDiscussions`, `totalArticles`, `totalKeywords`, `memberGrowthThisMonth`
  - 5-minute cache via IMemoryCache to minimize database hits
  - Exception handling and logging
  - `ClearCache()` method for manual invalidation
- **Registered:** In `Infrastructure/Extensions/ApplicationServicesExtensions.cs`

### API Endpoints - Search (Phase A)
- **Endpoint:** `GET /api/search?q={query}&type={discussions|profiles|keywords}&pageSize=5` [Public]
- **Status:** ✅ Implemented and integrated
- **Files:**
  - `Controllers/api/SearchController.cs` (controller with SearchResultModel DTO)
  - `Infrastructure/Services/SearchService.cs` (service)
- **Features:**
  - Full-text case-insensitive search across Post.Title/Content/Description, ShareSmallBizUser.DisplayName/UserName/Bio, Keyword.Name
  - Query validation (minimum 2 characters)
  - Grouped results: discussions, profiles, keywords (per-result-type pagination)
  - Type filtering (optional; if omitted returns all types)
  - Public profiles only (ProfileVisibility=Public), public posts only (IsPublic=true)
- **Registered:** In `Infrastructure/Extensions/ApplicationServicesExtensions.cs`

### API Endpoints - User Settings (Phase A)
- **Endpoints:**
  - `GET /api/users/{userId}/settings` [Authenticated]
  - `PUT /api/users/{userId}/settings` [Authenticated]
- **Status:** ✅ Implemented and integrated
- **Files:**
  - `Controllers/api/UserSettingsController.cs` (controller with UserSettingModel & UpdateUserSettingRequest DTOs)
  - Extended `Infrastructure/Services/UserProvider.cs` (GetUserSettingsAsync, UpdateUserSettingsAsync)
- **Features:**
  - Notification preferences: EmailOnComment, EmailOnLike, EmailOnFollow, WeeklySummary
  - Privacy preferences: ProfileVisibility, ShowEmail, ShowWebsite
  - User-only access (verifies caller is account owner)
  - Auto-creates default UserPreference if missing
  - Upsert pattern for atomic updates
- **Authorization:** Returns 403 Forbid if userId doesn't match authenticated user

### API Endpoints - Comment Likes (Phase A)
- **Endpoint:** `POST /api/comments/{id}/like` [Authenticated]
- **Status:** ✅ Implemented and integrated
- **Files:**
  - Extended `Infrastructure/Services/CommentProvider.cs` (LikeCommentAsync method)
  - Extended `Controllers/api/CommentsController.cs` (Like endpoint)
  - Extended `Infrastructure/Models/PostCommentModel.cs` (IsLikedByMe field + SetIsLikedByMe method)
- **Features:**
  - Toggle like/unlike (idempotent: liking twice = unlike)
  - Uses existing PostCommentLike table with CreatedID FK
  - Comments API now returns IsLikedByMe (null for unauthenticated, bool for authenticated)
  - Proper logging of like/unlike actions

### API Endpoints - Profile Enhancements (Phase A)
- **Endpoint Changes:**
  - `GET /api/profiles` now includes followerCount, followingCount, isFollowedByMe per user
  - `GET /api/profiles/{slug}` now includes followerCount, followingCount, isFollowedByMe
- **Status:** ✅ Implemented and integrated
- **Files:**
  - Extended `Infrastructure/Services/UserProvider.cs` (GetFollowerCountAsync, GetFollowingCountAsync, IsFollowedByMeAsync)
  - Extended `Controllers/api/ProfilesApiController.cs` (populate counts in GetAll & GetBySlug)
  - Extended `Infrastructure/Models/UserModel.cs` (FollowerCount, FollowingCount, IsFollowedByMe fields)
- **Features:**
  - Denormalized counts on ShareSmallBizUser for fast reads
  - isFollowedByMe: null for unauthenticated, bool for authenticated (null when viewing own profile)
  - Reuses existing UserFollow relationship
  - Works with existing follow/unfollow endpoints

### Build Status
- **dotnet build:** ✅ Success (0 errors, 257 warnings)
- Build includes Phase A endpoints (Stats, Search, Settings, Comment Likes, Profile Enhancements)
- All npm assets compiled successfully
- Warnings are pre-existing (platform-specific Windows APIs)
- Ready for Phase B implementation

---

## Remaining Implementation Plan

### Phase B: Medium Priority (Estimated 3-4 days)

These features are important but more complex; infrastructure is required first.

#### 5. Notifications System
- **Tables:** Create new `Notification` table
- **Endpoints:**
  - `GET /api/notifications?unreadOnly=false&pageSize=20&pageNumber=1` [Authenticated]
  - `PUT /api/notifications/{id}/read` [Authenticated]
  - `POST /api/notifications/read-all` [Authenticated]
- **File:** `Controllers/api/NotificationsController.cs`
- **Service:** `Infrastructure/Services/NotificationService.cs`
- **DTOs:** `NotificationModel` (id, type, message, isRead, createdDate, targetId, targetType)
- **Notification Types:** comment, like, follow, mention
- **Integration Points:**
  - After `CommentProvider.AddAsync()` → notify post author
  - After `PostLike` creation → notify post author (rate-limit: once/day per user)
  - After `UserFollow` creation → notify followed user
  - On `@mention` in comments/discussions → notify mentioned user
- **Caching:** Not recommended for notifications (real-time preferred)

#### 6. Direct Messages
- **Tables:** Create new `DirectMessage` table
- **Endpoints:**
  - `GET /api/messages/conversations` [Authenticated] — most recent first, with unread counts
  - `GET /api/messages/conversations/{conversationId}?pageSize=30&pageNumber=1` [Authenticated]
  - `POST /api/messages` [Authenticated] — send message
- **File:** `Controllers/api/DirectMessagesController.cs`
- **Service:** `Infrastructure/Services/MessageService.cs`
- **Conversation ID:** Generate as `min(senderId, recipientId)_max(senderId, recipientId)` for bidirectional lookup
- **DTOs:**
  - `MessageModel` (id, senderId, content, sentDate, isRead)
  - `ConversationModel` (conversationId, otherUser, lastMessage, lastMessageDate, unreadCount)
- **Integration:** Trigger notification on message send

#### 7. Post Interactions (Save & Share)
- **Tables:** Create `PostSave` and `PostShare` tables
- **Endpoints:**
  - `POST /api/discussion/{id}/save` [Authenticated] — toggle save/bookmark
  - `GET /api/users/{userId}/saved` [Authenticated] — get saved discussions
  - `POST /api/discussion/{id}/share` [Authenticated] — record share (increments counter)
- **File Changes:** Extend `Controllers/api/DiscussionController.cs`
- **Service Enhancements:** Extend `DiscussionProvider` with:
  - `SaveDiscussionAsync(postId, userId)` (toggle)
  - `GetSavedDiscussionsAsync(userId)`
  - `ShareDiscussionAsync(postId, userId)` (increment ShareCount)
- **Model Changes:** Add `shareCount: int`, `isSavedByMe?: bool` to `DiscussionModel`

#### 8. Articles/CMS Endpoints
- **Design:** Reuse Post entity where `IsPublic=true`
- **Endpoints:**
  - `GET /api/articles?pageNumber=1&pageSize=10&category={name}&tag={name}&featured={bool}` [Public]
  - `GET /api/articles/{slug}` [Public] (includes full content)
  - `GET /api/articles/featured?count=3` [Public]
  - `GET /api/articles/categories` [Public] — distinct categories with article counts
  - `GET /api/articles/related/{slug}?count=4` [Public] — same category or shared keywords
- **File:** `Controllers/api/ArticlesController.cs`
- **Service:** `Infrastructure/Services/ArticleService.cs`
- **DTOs:**
  - `ArticleModel` (extends Post) with id, title, slug, description, content, coverImageUrl, category, tags, author, publishedDate, modifiedDate, readingTimeMinutes, viewCount, isFeatured
  - List views omit `content` field; detail views include it
- **Impact:** Updates `GET /api/stats` to add `totalArticles` count

---

### Phase C: Lower Priority (Estimated 2-3 days)

These features are important but don't block core functionality; can be implemented after Phases A & B.

#### 9. Password Reset Flow
- **Endpoints:**
  - `POST /api/auth/forgot-password` [Public] (always returns 200 to prevent enumeration)
  - `POST /api/auth/reset-password` [Public]
- **File:** `Controllers/api/PasswordResetController.cs` or extend `AuthController.cs`
- **Service:** Use `UserManager.GeneratePasswordResetTokenAsync()` and `ResetPasswordAsync()`
- **Table:** `PasswordResetToken` (UserId, Token, ExpiryDate, Used)
- **Email:** Use `MailerSendService` to send reset link: `https://app.sharesmallbiz.com/reset-password?token={token}&email={email}`
- **Security:** Validate token before allowing change; mark token as used after reset

#### 10. Events Endpoints
- **Tables:** Create new `Event` table (Id, Title, Description, Location, IsOnline, StartDate, EndDate, RegistrationUrl, CreatedDate)
- **Endpoints:**
  - `GET /api/events?from={date}&count=10` [Public] (default today, ordered by StartDate)
  - `GET /api/events/{id}` [Public]
- **File:** `Controllers/api/EventsController.cs`
- **Service:** `Infrastructure/Services/EventService.cs`
- **DTOs:** `EventModel` matching spec
- **Admin:** CRUD endpoints can be added later if needed

#### 11. AI Assistant (Placeholder)
- **Endpoint:**
  - `POST /api/ai/chat` [Authenticated]
- **File:** `Controllers/api/AiController.cs`
- **Service:** `Infrastructure/Services/AiAssistantService.cs`
- **Implementation:**
  - Phase 1 (Current): Return hardcoded suggestions (no LLM integration)
  - Phase 2 (Future): Integrate with OpenAI, Claude, or other LLM provider
  - Optional rate limiting: 5 requests/minute per user
- **Request:** `{ "message": "...", "context": "retail" }` (context optional)
- **Response:** `{ "response": "...", "suggestions": [...], "actionItems": [...] }`

---

## Model & DTO Updates

### UserModel Changes
Add to existing model:
```json
{
  "...": "all existing fields",
  "followerCount": 128,
  "followingCount": 45,
  "isFollowedByMe": null  // null when unauthenticated
}
```

### DiscussionModel Changes
Add to existing model:
```json
{
  "...": "all existing fields",
  "shareCount": 14,
  "isSavedByMe": null  // null when unauthenticated
}
```

### PostCommentModel Changes
Add to existing model:
```json
{
  "...": "all existing fields",
  "isLikedByMe": null  // null when unauthenticated
}
```

### StatsModel (Updated for Articles)
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

## Integration Points

### Notification Triggers
When implemented, integrate `NotificationService.CreateNotificationAsync()` into:
1. **CommentProvider.AddAsync()** — notify post author when someone comments
2. **PostLike operations** — notify post author when liked (rate-limit: once per day per user)
3. **UserFollow operations** — notify followed user when followed
4. **Comment/Discussion parsing** — detect @username and notify mentioned user

### Cache Invalidation
- **Stats:** Invalidate when user registered or post published
- **Articles/Categories:** Invalidate on article publish/update (15-min TTL)
- **Events:** Invalidate on event create/update (5-min TTL)

### Follow Count Synchronization
When user follows/unfollows:
1. Create/delete `UserFollow` record
2. Increment/decrement `ShareSmallBizUser.FollowingCount` for follower
3. Increment/decrement `ShareSmallBizUser.FollowerCount` for followed user

---

## Service Layer Pattern

All new services follow this established pattern:

```csharp
public class MyService(
    ShareSmallBizUserContext context,
    ILogger<MyService> logger,
    UserManager<ShareSmallBizUser> userManager,
    IMemoryCache? memoryCache = null)
{
    public async Task<MyModel> GetAsync(int id)
    {
        try
        {
            var entity = await context.MyEntities.FindAsync(id);
            if (entity is null)
            {
                logger.LogWarning("Entity {Id} not found", id);
                return null;
            }
            return MapToModel(entity);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving entity {Id}", id);
            throw;
        }
    }
}
```

**Registration:** Add to `ApplicationServicesExtensions.cs`:
```csharp
services.AddScoped<MyService, MyService>();
```

---

## Controller Pattern

All API controllers inherit from `ApiControllerBase` (JWT enforced):

```csharp
[Route("api/endpoint")]
public class MyController(
    MyService service,
    ILogger<MyController> logger) : ApiControllerBase
{
    [HttpGet]
    [AllowAnonymous]  // if public
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var items = await service.GetAllAsync();
            return Ok(items);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving items");
            return StatusCode(500, new { message = "Error retrieving items" });
        }
    }
}
```

---

## Testing Strategy

### Unit Tests (Per Service)
- `StatsSessionTests` - cache hits/misses, count accuracy
- `SearchServiceTests` - query parsing, filtering, pagination
- `NotificationServiceTests` - creation, marking read, filtering
- `ArticleServiceTests` - category filtering, related article logic

### Integration Tests (Per Controller)
- Test each endpoint with valid/invalid JWT
- Test authorization (authenticated vs. public vs. admin)
- Test pagination and filtering
- Test response shapes match spec
- Test error handling (400, 401, 403, 404, 500)

### Manual Testing via Scalar UI
- `/scalar/v1` reflects live OpenAPI schema
- Test each endpoint with sample data
- Verify error responses
- Test edge cases (empty results, invalid IDs, own-user operations)

---

## Documentation Updates

### api-developer-guide.md Changes
Add new sections for each endpoint group:
1. **Community Stats (Section 1)** — GET /api/stats
2. **Search (Section 2)** — GET /api/search
3. **User Preferences (Section 3)** — GET/PUT /api/users/{id}/settings
4. **Notifications (Section 4)** — GET/PUT/POST notifications
5. **Direct Messages (Section 5)** — GET/POST messages
6. **Post Interactions (Section 6)** — Save & share endpoints
7. **Articles/CMS (Section 7)** — GET articles endpoints
8. **Events (Section 8)** — GET events endpoints
9. **Password Reset (Section 9)** — forgot/reset endpoints
10. **AI Assistant (Section 10)** — POST /api/ai/chat

Update existing model sections with new fields (followerCount, followingCount, shareCount, etc.)

---

## Critical Files

| Component | File Path |
|-----------|-----------|
| **Base API Controller** | `Controllers/api/ApiControllerBase.cs` |
| **Service Registration** | `Infrastructure/Extensions/ApplicationServicesExtensions.cs` |
| **Database Context** | `Data/ShareSmallBizUserContext.cs` |
| **Program Entry** | `Program.cs` |
| **API Guide** | `docs/api-developer-guide.md` |
| **Migrations** | `Migrations/20260311083125_PhaseA_DatabaseSchema.cs` |

---

## Implementation Checklist

### Phase A ✅ (COMPLETE - 2026-03-11)
- [x] Create migration for UserPreference & denormalized counts
- [x] Implement StatsController & StatsService
- [x] Implement SearchController & SearchService
- [x] Implement UserSettingsController & settings methods
- [x] Extend CommentProvider.LikeCommentAsync
- [x] Extend ProfilesApiController with follow data
- [x] Register SearchService in ApplicationServicesExtensions
- [x] Verify build succeeds with 0 errors
- [ ] Unit test Phase A services (queued for Phase B planning)
- [ ] Integration test Phase A endpoints (queued for Phase B planning)
- [ ] Update api-developer-guide.md with Phase A endpoints (queued for Phase B planning)

### Phase B (Queued)
- [ ] Create migration for Notification, DirectMessage, PostSave, PostShare tables
- [ ] Implement NotificationService & NotificationsController
- [ ] Implement MessageService & DirectMessagesController
- [ ] Extend DiscussionProvider (save/share methods)
- [ ] Implement ArticlesController & ArticleService
- [ ] Integrate notification triggers into CommentProvider, UserFollow, etc.
- [ ] Unit test Phase B services
- [ ] Integration test Phase B endpoints
- [ ] Update api-developer-guide.md with Phase B endpoints

### Phase C (Queued)
- [ ] Create migration for Event & PasswordResetToken tables
- [ ] Implement EventsController & EventService
- [ ] Implement PasswordResetController with email integration
- [ ] Implement AiController & AiAssistantService (placeholder)
- [ ] Unit test Phase C services
- [ ] Integration test Phase C endpoints
- [ ] Update api-developer-guide.md with Phase C endpoints

### Final Steps
- [ ] Unit & Integration tests for all Phase A endpoints
- [x] `dotnet build` — 0 errors ✅ (2026-03-11 05:20 UTC)
- [ ] Database migrations apply cleanly (Phase B/C when needed)
- [ ] Scalar UI (`/scalar/v1`) reflects all new Phase A endpoints
- [ ] api-developer-guide.md updated with Phase A docs
- [ ] Create pull request with Phase A changes
- [ ] Code review & merge Phase A
- [ ] Repeat for Phase B & C

---

## Success Criteria

✅ **All 14 API gaps closed**
✅ **Zero breaking changes to existing API**
✅ **Full test coverage for new services**
✅ **Documentation complete & accurate**
✅ **Build succeeds with 0 errors**
✅ **All endpoints follow established patterns**
✅ **Proper error handling & logging**
✅ **CORS, JWT, and authorization working**

---

## Notes

- **Design Choice:** Articles leverage existing Post entity with IsPublic=true and Category field rather than creating a separate Article entity. This avoids duplication and reuses all existing infrastructure.
- **Denormalized Counts:** FollowerCount and FollowingCount are denormalized on ShareSmallBizUser for profile listing performance. Keep in sync with UserFollow table.
- **Caching Strategy:** Stats (5 min), Articles/Categories (15 min), Events (5 min). Notifications & Messages should not be cached.
- **Future Enhancements:** Consider adding real-time notifications via SignalR, webhook support for external integrations, and full-text search engine (Lucene/Elasticsearch) in later phases.

---

**Last Updated:** 2026-03-11 05:20 UTC
**Next Review:** Phase B planning & implementation
