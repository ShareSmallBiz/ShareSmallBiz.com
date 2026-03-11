# ShareSmallBiz Web — API Gap Analysis

This document lists every capability the React web app requires that is not currently provided
by `api.sharesmallbiz.com`. Each missing endpoint is written in the same style as the
existing [api-developer-guide.md](api-developer-guide.md) so it can be used directly as a
specification for the API team.

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

---

### GET /api/stats `[Public]`

Return a lightweight snapshot of platform-wide activity. Cached server-side; does not
need to be real-time.

**Response `200 OK`:**

```json
{
  "totalMembers": 2847,
  "totalDiscussions": 4821,
  "totalKeywords": 42,
  "memberGrowthThisMonth": 83
}
```

---

## 2. Search

**Gap:** The navigation bar contains a full-width search input. There is no search endpoint.
The current implementation does nothing when the user types.

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
  "discussions": [
    {
      "id": 101,
      "title": "How to Market Your Small Business on LinkedIn",
      "slug": "how-to-market-...",
      "description": "A short excerpt...",
      "createdDate": "2026-01-15T12:00:00Z"
    }
  ],
  "profiles": [
    {
      "id": "3fa85f64-...",
      "displayName": "Jane Smith",
      "userName": "jane.smith",
      "profilePictureUrl": "/Media/42",
      "bio": "Small business owner from Austin, TX."
    }
  ],
  "keywords": [
    {
      "id": 7,
      "name": "Marketing",
      "postCount": 312
    }
  ]
}
```

**Error responses:**

| Status | Reason |
|---|---|
| `400` | `q` is missing or shorter than 2 characters |

---

## 3. User Preferences / Settings

**Gap:** The Settings page has tabs for notification preferences and privacy controls.
There is no endpoint to read or write these settings. The `UserModel` returned by
`GET /api/users/{userId}` does not include any preference fields.

---

### GET /api/users/{userId}/settings `[Authenticated]`

Return the caller's preference settings. The caller must be the account owner.

**Response `200 OK`:**

```json
{
  "notifications": {
    "emailOnComment": true,
    "emailOnLike": false,
    "emailOnFollow": true,
    "weeklySummary": false
  },
  "privacy": {
    "profileVisibility": "Public",
    "showEmail": false,
    "showWebsite": true
  }
}
```

`profileVisibility` values: `Public`, `Authenticated`, `Connections`, `Private`

---

### PUT /api/users/{userId}/settings `[Authenticated]`

Update preference settings. Only the provided fields are changed (partial update).

**Request body:**

```json
{
  "notifications": {
    "emailOnComment": false
  },
  "privacy": {
    "profileVisibility": "Connections"
  }
}
```

**Response `204 No Content`**

---

## 4. Notifications

**Gap:** The navigation header shows a bell icon with a badge count. There is no
notifications endpoint of any kind.

---

### GET /api/notifications `[Authenticated]`

Return the caller's notifications, newest first.

| Query param | Type | Default | Description |
|---|---|---|---|
| `unreadOnly` | `bool` | `false` | Return only unread notifications |
| `pageSize` | `int` | `20` | Results per page |
| `pageNumber` | `int` | `1` | 1-based page |

**Response `200 OK`:**

```json
{
  "items": [
    {
      "id": "a1b2c3...",
      "type": "comment",
      "message": "Jane Smith commented on your post.",
      "isRead": false,
      "createdDate": "2026-03-10T14:00:00Z",
      "targetId": 101,
      "targetType": "discussion"
    }
  ],
  "unreadCount": 3,
  "totalCount": 47,
  "pageNumber": 1,
  "pageSize": 20,
  "totalPages": 3
}
```

`type` values: `comment`, `like`, `follow`, `mention`

---

### POST /api/notifications/read-all `[Authenticated]`

Mark all notifications as read.

**Response `204 No Content`**

---

### PUT /api/notifications/{id}/read `[Authenticated]`

Mark a single notification as read.

**Response `204 No Content`**

---

## 5. Direct Messages

**Gap:** The navigation header shows a mail icon with a badge count. There is no
messaging endpoint.

---

### GET /api/messages/conversations `[Authenticated]`

List the caller's conversations, most-recently-active first.

**Response `200 OK`:**

```json
[
  {
    "conversationId": "conv_abc123",
    "otherUser": {
      "id": "3fa85f64-...",
      "displayName": "Jane Smith",
      "userName": "jane.smith",
      "profilePictureUrl": "/Media/42"
    },
    "lastMessage": "Sounds great, let's connect!",
    "lastMessageDate": "2026-03-09T18:22:00Z",
    "unreadCount": 2
  }
]
```

---

### GET /api/messages/conversations/{conversationId} `[Authenticated]`

Return messages in a conversation, newest first.

| Query param | Type | Default |
|---|---|---|
| `pageSize` | `int` | `30` |
| `pageNumber` | `int` | `1` |

**Response `200 OK`:** paginated list of message objects:

```json
{
  "items": [
    {
      "id": "msg_xyz",
      "senderId": "3fa85f64-...",
      "content": "Hi! I saw your post about marketing.",
      "sentDate": "2026-03-09T18:20:00Z",
      "isRead": true
    }
  ],
  "totalCount": 14,
  "pageNumber": 1,
  "pageSize": 30,
  "totalPages": 1
}
```

---

### POST /api/messages `[Authenticated]`

Send a direct message to another user.

**Request body:**

```json
{
  "recipientId": "3fa85f64-...",
  "content": "Hi Jane, would you be interested in a collaboration?"
}
```

**Response `201 Created`:**

```json
{
  "conversationId": "conv_abc123",
  "messageId": "msg_xyz"
}
```

**Error responses:**

| Status | Reason |
|---|---|
| `400` | `content` is empty |
| `404` | `recipientId` does not exist |

---

## 6. Follow Status on Profile Listings

**Gap:** `GET /api/profiles` returns a list of `UserModel` objects but does not indicate
whether the authenticated caller is already following each profile. Without this, the
"Follow" / "Unfollow" button cannot be initialised correctly in the Suggested Connections
widget.

---

### Change to existing: GET /api/profiles `[Public]`

When an `Authorization` header is present, augment each `UserModel` in the response with:

```json
{
  "...": "all existing UserModel fields",
  "isFollowedByMe": true
}
```

`isFollowedByMe` is `null` when the request is unauthenticated.

---

## 7. Post Interactions — Saves and Shares

**Gap:** The post card has Save (bookmark) and Share buttons. Neither has an API endpoint.

---

### POST /api/discussion/{id}/save `[Authenticated]`

Toggle a saved/bookmarked state on a discussion.

**Response `200 OK`:**

```json
{ "saved": true }
```

---

### GET /api/users/{userId}/saved `[Authenticated]`

Return the discussions saved by the caller.

**Response `200 OK`:** array of [DiscussionModel]

---

### POST /api/discussion/{id}/share `[Authenticated]`

Record that the caller shared a discussion (increments the share counter).

**Response `200 OK`:**

```json
{ "shareCount": 14 }
```

---

### Change to existing: DiscussionModel

Add two fields:

```json
{
  "...": "all existing fields",
  "shareCount": 14,
  "isSavedByMe": false
}
```

`isSavedByMe` is `null` when the request is unauthenticated.

---

## 8. Comment Likes

**Gap:** `PostCommentModel` includes a `likeCount` field, but there is no endpoint to
like or unlike a comment.

---

### POST /api/comments/{id}/like `[Authenticated]`

Toggle a like on a comment.

**Response `204 No Content`**

---

### Change to existing: PostCommentModel

Add:

```json
{
  "...": "all existing fields",
  "isLikedByMe": false
}
```

---

## 9. Events

**Gap:** The right sidebar displays upcoming local events. There is no events endpoint.

---

### GET /api/events `[Public]`

Return upcoming events, ordered by date ascending.

| Query param | Type | Description |
|---|---|---|
| `from` | `date` | Start date filter (ISO 8601). Defaults to today. |
| `count` | `int` | Max results. Default `10`. |

**Response `200 OK`:**

```json
[
  {
    "id": 1,
    "title": "Small Business Networking Night",
    "description": "Monthly meetup for local small business owners.",
    "location": "Portland Business Center",
    "isOnline": false,
    "startDate": "2026-03-15T18:00:00Z",
    "endDate": "2026-03-15T20:00:00Z",
    "registrationUrl": "https://eventbrite.com/..."
  }
]
```

---

### GET /api/events/{id} `[Public]`

**Response `200 OK`:** single event object as above.

---

## 10. Password Reset

**Gap:** There is no forgot-password or reset-password flow. Users who forget their
password have no self-service recovery path.

---

### POST /api/auth/forgot-password `[Public]`

Send a password reset email to the given address.

**Request body:**

```json
{ "email": "jane@example.com" }
```

**Response `200 OK`:**

```json
{ "message": "If that address is registered, a reset link has been sent." }
```

> Always returns 200 regardless of whether the email exists, to prevent account
> enumeration.

---

### POST /api/auth/reset-password `[Public]`

Complete the reset using the token from the email.

**Request body:**

```json
{
  "email": "jane@example.com",
  "token": "<token-from-email>",
  "newPassword": "NewSecurePassword1!"
}
```

**Response `200 OK`:**

```json
{ "message": "Password reset successfully. You can now sign in." }
```

**Error responses:**

| Status | Reason |
|---|---|
| `400` | Token expired, already used, or password fails validation |

---

## 11. AI Assistant

**Gap:** The AI Assistant component is currently a "coming soon" placeholder because the
API exposes no AI endpoint.

---

### POST /api/ai/chat `[Authenticated]`

Send a message to the AI business assistant and receive a response.

**Request body:**

```json
{
  "message": "What are the best marketing strategies for a local hardware store?",
  "context": "retail"
}
```

`context` (optional): a hint about the user's business type to improve relevance.

**Response `200 OK`:**

```json
{
  "response": "For a local hardware store, the most effective strategies are...",
  "suggestions": [
    "Run seasonal promotions tied to home improvement projects",
    "Partner with local contractors for referral programmes"
  ],
  "actionItems": [
    "Set up a Google Business Profile if you haven't already",
    "Create a loyalty card scheme for repeat customers"
  ]
}
```

---

## 12. Missing Fields on Existing Models

### UserModel

| Field | Type | Notes |
|---|---|---|
| `followerCount` | `int` | Currently only available via a separate `GET /api/profiles/{slug}/followers` call |
| `followingCount` | `int` | Currently only available via `GET /api/profiles/{slug}/following` |
| `isFollowedByMe` | `bool?` | `null` when unauthenticated; set when JWT present |

### DiscussionModel

| Field | Type | Notes |
|---|---|---|
| `shareCount` | `int` | Number of times this discussion has been shared |
| `isSavedByMe` | `bool?` | Whether the authenticated caller has saved/bookmarked this |

### PostCommentModel

| Field | Type | Notes |
|---|---|---|
| `isLikedByMe` | `bool?` | Whether the authenticated caller has liked this comment |

---

## 13. CMS / Public Articles

**Gap:** The platform needs a public-facing content layer — staff- or editor-authored articles that
are readable without a login and that search engines can index. This is distinct from
user-generated `DiscussionModel` posts:

| Dimension | CMS Article | Discussion |
|---|---|---|
| Authored by | Staff / editors | Any member |
| Login required to read | No | No (public discussions) |
| SEO / crawlable | Yes — canonical URL, meta description | Secondary |
| Comments | Optional (moderated) | Yes (full thread) |
| Purpose | Content marketing, evergreen guides | Community conversation |

There is currently no `/api/articles` surface at all.

---

### ArticleModel

```json
{
  "id": 1,
  "title": "5 Ways Small Businesses Can Leverage AI in 2026",
  "slug": "small-business-ai-2026",
  "description": "A concise meta description (≤ 160 chars) used in search previews.",
  "content": "<HTML or Markdown — full body>",
  "coverImageUrl": "/Media/77",
  "category": "Marketing",
  "tags": ["AI", "Productivity", "Tools"],
  "author": {
    "displayName": "ShareSmallBiz Editorial",
    "profilePictureUrl": "/Media/1"
  },
  "publishedDate": "2026-02-01T09:00:00Z",
  "modifiedDate": "2026-02-05T14:30:00Z",
  "readingTimeMinutes": 6,
  "viewCount": 1842,
  "isFeatured": true
}
```

---

### GET /api/articles `[Public]`

Return a paginated list of published articles, newest first.

| Query param | Type | Default | Description |
|---|---|---|---|
| `pageNumber` | `int` | `1` | 1-based page |
| `pageSize` | `int` | `10` | Results per page |
| `category` | `string` | — | Filter by category name |
| `tag` | `string` | — | Filter by tag |
| `featured` | `bool` | — | `true` to return only featured articles |

**Response `200 OK`:**

```json
{
  "items": [ { "...": "ArticleModel (without full content body)" } ],
  "totalCount": 42,
  "pageNumber": 1,
  "pageSize": 10,
  "totalPages": 5
}
```

> The `content` field is omitted in list responses; return only summary fields to keep
> the payload small. Deliver full `content` only via the detail endpoint below.

---

### GET /api/articles/{slug} `[Public]`

Return a single article by slug, including the full `content` body.

**Response `200 OK`:** full `ArticleModel` as above.

**Error responses:**

| Status | Reason |
|---|---|
| `404` | No published article matches the slug |

---

### GET /api/articles/featured `[Public]`

Return the _n_ most recent featured articles. Intended for the home-page hero carousel or
sidebar "Featured Reads" widget.

| Query param | Type | Default |
|---|---|---|
| `count` | `int` | `3` |

**Response `200 OK`:** array of `ArticleModel` (without `content`).

---

### GET /api/articles/categories `[Public]`

Return the distinct category names and article counts. Used to populate a category
navigation menu.

**Response `200 OK`:**

```json
[
  { "name": "Marketing", "articleCount": 14 },
  { "name": "Finance",   "articleCount": 9  },
  { "name": "Tech",      "articleCount": 7  }
]
```

---

### GET /api/articles/related/{slug} `[Public]`

Return up to _n_ articles in the same category or sharing tags with the given article.
Displayed in a "You might also like" section at the bottom of an article detail page.

| Query param | Type | Default |
|---|---|---|
| `count` | `int` | `4` |

**Response `200 OK`:** array of `ArticleModel` (without `content`).

---

### Change to existing: GET /api/stats

Add a `totalArticles` field to the community stats response:

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

## 14. Summary Table

| Gap | Endpoint(s) | Access |
|---|---|---|
| Community stats | `GET /api/stats` | Public |
| Search | `GET /api/search` | Public |
| User settings read | `GET /api/users/{userId}/settings` | Authenticated |
| User settings write | `PUT /api/users/{userId}/settings` | Authenticated |
| Notifications list | `GET /api/notifications` | Authenticated |
| Notifications mark read | `PUT /api/notifications/{id}/read`, `POST /api/notifications/read-all` | Authenticated |
| Direct messages — list | `GET /api/messages/conversations` | Authenticated |
| Direct messages — thread | `GET /api/messages/conversations/{id}` | Authenticated |
| Direct messages — send | `POST /api/messages` | Authenticated |
| Follow status on profile list | Augment `GET /api/profiles` response | Public (field null) / Authenticated |
| Save / bookmark discussion | `POST /api/discussion/{id}/save`, `GET /api/users/{userId}/saved` | Authenticated |
| Share discussion | `POST /api/discussion/{id}/share` | Authenticated |
| Like a comment | `POST /api/comments/{id}/like` | Authenticated |
| Events list | `GET /api/events` | Public |
| Events detail | `GET /api/events/{id}` | Public |
| Forgot password | `POST /api/auth/forgot-password` | Public |
| Reset password | `POST /api/auth/reset-password` | Public |
| AI chat | `POST /api/ai/chat` | Authenticated |
| `UserModel.followerCount` / `followingCount` / `isFollowedByMe` | Existing model change | — |
| `DiscussionModel.shareCount` / `isSavedByMe` | Existing model change | — |
| `PostCommentModel.isLikedByMe` | Existing model change | — |
| CMS articles list | `GET /api/articles` | Public |
| CMS article detail | `GET /api/articles/{slug}` | Public |
| CMS featured articles | `GET /api/articles/featured` | Public |
| CMS article categories | `GET /api/articles/categories` | Public |
| CMS related articles | `GET /api/articles/related/{slug}` | Public |
| `GET /api/stats` — add `totalArticles` | Existing endpoint change | Public |
