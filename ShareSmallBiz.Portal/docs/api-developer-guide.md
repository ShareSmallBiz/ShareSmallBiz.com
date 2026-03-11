# ShareSmallBiz API — Developer Guide

**Base URL:** `https://api.sharesmallbiz.com`
**Interactive Docs:** `https://api.sharesmallbiz.com/scalar/v1` (non-production)
**OpenAPI Schema:** `https://api.sharesmallbiz.com/openapi/v1.json` (non-production)

---

## Table of Contents

1. [Overview](#1-overview)
2. [Authentication](#2-authentication)
3. [Common Conventions](#3-common-conventions)
4. [Rate Limiting](#4-rate-limiting)
5. [CORS](#5-cors)
6. [Auth Endpoints](#6-auth-endpoints)
7. [Profiles](#7-profiles)
8. [Discussions](#8-discussions)
9. [Comments](#9-comments)
10. [Keywords / Tags](#10-keywords--tags)
11. [Users](#11-users)
12. [Media Library](#12-media-library)
13. [Profile Picture](#13-profile-picture)
14. [Admin — Dashboard](#14-admin--dashboard)
15. [Admin — Users](#15-admin--users)
16. [Admin — Comments](#16-admin--comments)
17. [Admin — Roles](#17-admin--roles)
18. [Real-Time Chat (SignalR)](#18-real-time-chat-signalr)
19. [Community Stats](#19-community-stats)
20. [Search](#20-search)
21. [User Settings](#21-user-settings)
22. [Notifications](#22-notifications)
23. [Direct Messages](#23-direct-messages)
24. [Articles / CMS](#24-articles--cms)
25. [Events](#25-events)
26. [AI Assistant](#26-ai-assistant)
27. [Data Models Reference](#27-data-models-reference)
28. [Error Reference](#28-error-reference)
29. [Quick-Start Examples](#29-quick-start-examples)

---

## 1. Overview

The ShareSmallBiz REST API gives your React application full programmatic access to every feature on the platform. The MVC web app and this API share the same backend — you are not talking to a different service, only a different interface to the same data.

**Technology stack**
- ASP.NET Core 10 on .NET 10
- SQLite via Entity Framework Core
- JWT Bearer tokens for API authentication
- Cookie-based sessions for the MVC web app (separate from the API)
- SignalR for real-time chat

**Everything is JSON.** All requests that send a body use `Content-Type: application/json` unless the endpoint accepts file uploads, in which case `multipart/form-data` is specified explicitly.

---

## 2. Authentication

The API uses **JWT Bearer tokens**. Cookies are not accepted for API requests.

### 2.1 Obtain a token

```http
POST /api/auth/login
Content-Type: application/json

{
  "email": "you@example.com",
  "password": "YourPassword1!"
}
```

**Success response `200 OK`:**

```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "displayName": "Jane Smith"
}
```

Tokens expire after the number of hours configured in `JwtSettings:ExpirationInHours` (default: 1 hour). There is currently no refresh-token endpoint; request a new token by logging in again.

### 2.2 Send the token

Include the token in every authenticated request using the `Authorization` header:

```http
GET /api/media
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

### 2.3 Public vs authenticated endpoints

| Access level | What it means |
|---|---|
| **Public** | No `Authorization` header needed |
| **Authenticated** | Valid JWT required |
| **Admin** | Valid JWT **and** the account must be in the `Admin` role |

Endpoints that are public are marked `[AllowAnonymous]` in the codebase and noted in this guide.

### 2.4 Token claims

The JWT payload contains:

| Claim | Value |
|---|---|
| `sub` | User ID (GUID string) |
| `email` | Email address |
| `displayName` | Display name |
| `jti` | Unique token ID |
| `http://schemas.microsoft.com/ws/2008/06/identity/claims/role` | One entry per role the user holds |

---

## 3. Common Conventions

### 3.1 HTTP methods

| Method | Usage |
|---|---|
| `GET` | Retrieve — never modifies state |
| `POST` | Create a resource |
| `PUT` | Replace a resource (full update) |
| `DELETE` | Remove a resource |

### 3.2 Status codes

| Code | Meaning |
|---|---|
| `200 OK` | Request succeeded, body contains data |
| `201 Created` | Resource created; `Location` header points to it |
| `204 No Content` | Request succeeded, no body |
| `400 Bad Request` | Validation failed or malformed request |
| `401 Unauthorized` | Missing or invalid token |
| `403 Forbidden` | Authenticated but not permitted (wrong role or not owner) |
| `404 Not Found` | Resource does not exist |
| `409 Conflict` | Unique constraint violation (e.g., duplicate role name) |
| `429 Too Many Requests` | Rate limit exceeded |
| `500 Internal Server Error` | Unexpected server error |

### 3.3 Error body

All error responses return a JSON body:

```json
{
  "message": "Human-readable description of the problem."
}
```

Identity errors (e.g., password too short) return an array:

```json
[
  { "code": "PasswordTooShort", "description": "Passwords must be at least 6 characters." }
]
```

### 3.4 Pagination

Discussions support cursor-based paging via query parameters:

| Parameter | Type | Description |
|---|---|---|
| `pageNumber` | `int` | 1-based page number |
| `pageSize` | `int` | Records per page |
| `sortType` | `SortType` enum | `0` = Recent, `1` = Popular, `2` = MostCommented |

**Paginated response shape:**

```json
{
  "items": [ ... ],
  "totalCount": 142,
  "pageNumber": 1,
  "pageSize": 20,
  "totalPages": 8
}
```

### 3.5 DateTime format

All dates are returned as ISO 8601 UTC strings:

```
"2026-03-10T14:32:00Z"
```

---

## 4. Rate Limiting

The auth endpoints are protected by a fixed-window rate limiter:

| Policy | Applied to | Limit |
|---|---|---|
| `auth` | `POST /api/auth/login`, `POST /api/auth/register`, `POST /api/auth/oauth/login` | 10 requests per minute per client IP |
| `auth` | `POST /api/auth/forgot-password`, `POST /api/auth/reset-password` | 10 requests per minute per client IP |

When the limit is exceeded the API returns:

```http
HTTP/1.1 429 Too Many Requests
```

```json
{ "message": "Too many requests. Please wait before trying again." }
```

Additionally, accounts that repeatedly fail login have their **ASP.NET Identity lockout** triggered. After exceeding the threshold the login endpoint returns:

```http
HTTP/1.1 429 Too Many Requests

{ "message": "Account is temporarily locked. Please try again later." }
```

---

## 5. CORS

The following origins are permitted in production:

- `https://sharesmallbiz.com`
- `https://www.sharesmallbiz.com`

In development/staging, add your dev server origin to `appsettings.Development.json`:

```json
{
  "AllowedOrigins": [
    "http://localhost:3000",
    "http://localhost:5173"
  ]
}
```

All HTTP methods and headers are allowed. Credentials (cookies) are allowed for cross-origin requests, though the API exclusively uses Bearer tokens.

---

## 6. Auth Endpoints

### POST /api/auth/login

Authenticate an existing user and receive a JWT.

**Request body:**

```json
{
  "email": "jane@example.com",
  "password": "SecurePassword1!"
}
```

**Response `200 OK`:**

```json
{
  "token": "<jwt>",
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "displayName": "Jane Smith"
}
```

**Error responses:**

| Status | Reason |
|---|---|
| `400` | Email or password missing |
| `401` | Invalid credentials |
| `429` | Account locked or rate limit exceeded |

---

### POST /api/auth/register

Register a new account. A confirmation email is sent automatically. **No token is returned** — the user must confirm their email before they can log in.

**Request body:**

```json
{
  "email": "jane@example.com",
  "password": "SecurePassword1!",
  "displayName": "Jane Smith",
  "firstName": "Jane",
  "lastName": "Smith"
}
```

**Response `200 OK`:**

```json
{
  "message": "Registration successful. Please check your email to confirm your account before signing in."
}
```

---

### POST /api/auth/forgot-password `[Public]`

Initiate a password reset. Always returns `200 OK` regardless of whether the email exists (prevents user enumeration). If the account exists, an email with a reset link is sent.

**Request body:**

```json
{
  "email": "jane@example.com"
}
```

**Response `200 OK`:**

```json
{
  "message": "If an account with that email exists, a password reset link has been sent."
}
```

---

### POST /api/auth/reset-password `[Public]`

Complete a password reset using the token from the email link.

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
{
  "message": "Password has been reset successfully."
}
```

**Error responses:**

| Status | Reason |
|---|---|
| `400` | Token invalid, expired, or password does not meet requirements |

> Tokens are single-use and expire as configured by ASP.NET Identity's data-protection settings (default: 1 day).

---

### GET /api/auth/test

Validate a token. Useful during development to inspect the claims it carries.

**Headers:** `Authorization: Bearer <token>`

**Response `200 OK`:**

```json
{
  "message": "Token is valid.",
  "claims": [
    { "type": "sub", "value": "3fa85f64-..." },
    { "type": "email", "value": "jane@example.com" },
    { "type": "displayName", "value": "Jane Smith" }
  ]
}
```

---

## 7. Profiles

Profiles are the public-facing user pages. Access is controlled by each user's `profileVisibility` setting (`Public`, `Authenticated`, `Connections`, `Private`).

### GET /api/profiles `[Public]`

List all public user profiles.

**Response `200 OK`:** array of [UserModel](#usermodel)

---

### GET /api/profiles/{slug} `[Public]`

Get a single profile by username or custom profile URL slug. Increments the profile view counter for non-owners. If the caller is the profile owner, analytics data is populated.

**Response `200 OK`:** [ProfileModel](#profilemodel)

---

### GET /api/profiles/{slug}/followers `[Public]`

**Response `200 OK`:** array of [UserModel](#usermodel)

---

### GET /api/profiles/{slug}/following `[Public]`

**Response `200 OK`:** array of [UserModel](#usermodel)

---

### POST /api/profiles/{slug}/follow `[Authenticated]`

Follow the specified user. Returns `400` if you try to follow yourself.

**Response `204 No Content`**

---

### POST /api/profiles/{slug}/unfollow `[Authenticated]`

**Response `204 No Content`**

---

## 8. Discussions

Discussions are the primary content type — posts, articles, and forum threads.

### GET /api/discussion `[Public]`

Return all discussions. By default only public ones are returned.

| Query param | Type | Default | Description |
|---|---|---|---|
| `onlyPublic` | `bool` | `true` | Set to `false` (requires auth) to include private |

**Response `200 OK`:** array of [DiscussionModel](#discussionmodel)

---

### GET /api/discussion/{id} `[Public]`

**Response `200 OK`:** [DiscussionModel](#discussionmodel)

---

### GET /api/discussion/paged `[Public]`

Paginated list with sort control.

| Query param | Type | Default |
|---|---|---|
| `pageNumber` | `int` | `1` |
| `pageSize` | `int` | `20` |
| `sortType` | `int` | `0` (Recent) |

**Response `200 OK`:** paginated result (see [Pagination](#34-pagination))

---

### GET /api/discussion/featured/{count} `[Public]`

**Response `200 OK`:** array of [DiscussionModel](#discussionmodel) (up to `count`)

---

### GET /api/discussion/most-commented/{count} `[Public]`

**Response `200 OK`:** array of [DiscussionModel](#discussionmodel)

---

### GET /api/discussion/most-popular/{count} `[Public]`

**Response `200 OK`:** array of [DiscussionModel](#discussionmodel)

---

### GET /api/discussion/most-recent/{count} `[Public]`

**Response `200 OK`:** array of [DiscussionModel](#discussionmodel)

---

### POST /api/discussion `[Authenticated]`

Create a new discussion.

**Request body:** [DiscussionModel](#discussionmodel) (omit `id`)

**Response `201 Created`:** [DiscussionModel](#discussionmodel) with generated `id`

---

### PUT /api/discussion/{id} `[Authenticated]`

Replace a discussion. The caller must be the original author.

**Request body:** [DiscussionModel](#discussionmodel) (must include matching `id`)

**Response `204 No Content`**

---

### DELETE /api/discussion/{id} `[Authenticated]`

**Response `204 No Content`**

---

### POST /api/discussion/{id}/comment `[Authenticated]`

Add a comment to a discussion (quick-add, no separate comment model).

**Request body:** plain string in JSON

```json
"This is a great post!"
```

**Response `204 No Content`**

---

### POST /api/discussion/{id}/like `[Authenticated]`

Toggle a like on a discussion.

**Response `204 No Content`**

---

### POST /api/discussion/{id}/save `[Authenticated]`

Toggle save/unsave a discussion. Saved discussions can be retrieved via `GET /api/discussion/saved`.

**Response `200 OK`:**

```json
{ "saved": true }
```

Returns `saved: false` when the discussion was unsaved.

---

### GET /api/discussion/saved `[Authenticated]`

Get all discussions the authenticated user has saved.

**Response `200 OK`:** array of [DiscussionModel](#discussionmodel)

---

### POST /api/discussion/{id}/share `[Authenticated]`

Record a share event. Increments the discussion's `shareCount` counter.

**Response `200 OK`:**

```json
{ "shareCount": 14 }
```

---

## 9. Comments

Full CRUD for post comments with owner-enforced access.

### GET /api/comments `[Public]`

Get all comments for a post.

| Query param | Required | Description |
|---|---|---|
| `postId` | yes | ID of the discussion |

**Response `200 OK`:** array of [PostCommentModel](#postcommentmodel)

---

### GET /api/comments/{id} `[Public]`

**Response `200 OK`:** [PostCommentModel](#postcommentmodel)

---

### POST /api/comments `[Authenticated]`

Add a comment.

**Request body:**

```json
{
  "postId": 42,
  "content": "Really useful article, thank you!"
}
```

**Response `200 OK`:**

```json
{ "message": "Comment added." }
```

---

### PUT /api/comments/{id} `[Authenticated]`

Edit your own comment. Returns `403 Forbidden` if the caller does not own the comment.

**Request body:**

```json
{
  "content": "Updated comment text."
}
```

**Response `204 No Content`**

---

### DELETE /api/comments/{id} `[Authenticated]`

Delete your own comment. Returns `403 Forbidden` if the caller does not own the comment.

**Response `204 No Content`**

---

### POST /api/comments/{id}/like `[Authenticated]`

Toggle a like on a comment.

**Response `200 OK`:**

```json
{ "liked": true, "likeCount": 5 }
```

---

## 10. Keywords / Tags

Keywords are the tagging/categorisation system for discussions.

### GET /api/keywords `[Public]`

**Response `200 OK`:** array of [KeywordModel](#keywordmodel)

---

### GET /api/keywords/{id} `[Public]`

**Response `200 OK`:** [KeywordModel](#keywordmodel)

---

### POST /api/keywords `[Admin]`

Create a new keyword. Duplicate names are silently de-duplicated — the existing keyword is returned.

**Request body:**

```json
{
  "name": "Social Media",
  "description": "Topics about social media marketing"
}
```

**Response `201 Created`:** [KeywordModel](#keywordmodel)

---

### PUT /api/keywords/{id} `[Admin]`

**Request body:**

```json
{
  "name": "Social Media Marketing",
  "description": "Updated description"
}
```

**Response `200 OK`:** [KeywordModel](#keywordmodel)

---

### DELETE /api/keywords/{id} `[Admin]`

**Response `204 No Content`**

---

## 11. Users

Direct user account operations requiring a JWT. For public profile browsing use the [Profiles endpoints](#7-profiles) instead.

All endpoints require `Authorization: Bearer <token>`.

### GET /api/users/all

**Response `200 OK`:** array of [UserModel](#usermodel) (public fields only)

---

### GET /api/users/{userId}

**Response `200 OK`:** [UserModel](#usermodel)

---

### GET /api/users/username/{username}

**Response `200 OK`:** [UserModel](#usermodel)

---

### PUT /api/users/{userId}

Update user profile. The caller should be the account owner (server validates via JWT claims).

**Request body:** [UserModel](#usermodel)

**Response `200 OK`:** `"User updated successfully"`

---

### DELETE /api/users/{userId}

Delete an account.

**Response `200 OK`:** `"User deleted successfully"`

---

### POST /api/users/{followerId}/follow/{followingId}

**Response `200 OK`:** `"User followed successfully"`

---

### POST /api/users/{followerId}/unfollow/{followingId}

**Response `200 OK`:** `"User unfollowed successfully"`

---

### GET /api/users/{userId}/followers

**Response `200 OK`:** array of [UserModel](#usermodel)

---

### GET /api/users/{userId}/following

**Response `200 OK`:** array of [UserModel](#usermodel)

---

## 12. Media Library

Manage the authenticated user's personal media library. Supports local file uploads and external link storage.

All endpoints require `Authorization: Bearer <token>`.

Max upload size: **50 MB**.

### GET /api/media

Return the caller's media library with optional filters.

| Query param | Type | Description |
|---|---|---|
| `search` | `string` | Search filename or description |
| `mediaType` | `int` | `0` = Image, `1` = Video, `2` = Audio, `3` = Document |
| `storageProvider` | `int` | `0` = LocalStorage, `1` = External, `2` = YouTube, `3` = Unsplash |

**Response `200 OK`:** array of [MediaModel](#mediamodel)

---

### GET /api/media/{id}

Get a single media item (must belong to the caller).

**Response `200 OK`:** [MediaModel](#mediamodel)

---

### POST /api/media/upload

Upload a file. Use `multipart/form-data`.

| Field | Type | Required | Description |
|---|---|---|---|
| `file` | file | yes | The file to upload |
| `description` | string | no | Human-readable description |
| `attribution` | string | no | Credit line (e.g. photographer name) |

**Example (JavaScript):**

```javascript
const formData = new FormData();
formData.append('file', fileInput.files[0]);
formData.append('description', 'Company logo');

const res = await fetch('https://api.sharesmallbiz.com/api/media/upload', {
  method: 'POST',
  headers: { Authorization: `Bearer ${token}` },
  body: formData,
});
const media = await res.json();
```

**Response `201 Created`:** [MediaModel](#mediamodel)

---

### PUT /api/media/{id}

Update metadata only (filename, description, attribution). File content cannot be replaced; delete and re-upload instead.

**Request body:**

```json
{
  "fileName": "company-logo-v2.png",
  "description": "Updated description",
  "attribution": "Design team"
}
```

**Response `204 No Content`**

---

### DELETE /api/media/{id}

**Response `204 No Content`**

---

### POST /api/media/external

Register an external URL (e.g. an image hosted on your own CDN) as a media item.

**Request body:**

```json
{
  "url": "https://cdn.example.com/images/banner.jpg",
  "description": "Homepage banner",
  "attribution": "Acme Photography",
  "mediaType": 0
}
```

**Response `201 Created`:** [MediaModel](#mediamodel)

---

## 13. Profile Picture

Manage the authenticated user's profile picture. All endpoints require `Authorization: Bearer <token>`.

### GET /api/media/profile

Get current profile picture status.

**Response `200 OK`:**

```json
{
  "hasProfilePicture": true,
  "profilePictureUrl": "/Media/142"
}
```

---

### POST /api/media/profile/upload

Upload a new profile picture (JPEG, PNG, WebP). The image is stored in local storage and the user's `profilePictureUrl` is updated automatically. Use `multipart/form-data`.

| Field | Type | Required |
|---|---|---|
| `file` | file | yes |

Max size: **10 MB**

**Response `200 OK`:**

```json
{
  "profilePictureUrl": "/Media/143",
  "mediaId": 143
}
```

---

### POST /api/media/profile/external

Set a public external image URL as the profile picture.

**Request body:**

```json
{
  "url": "https://example.com/my-photo.jpg",
  "description": "My headshot"
}
```

**Response `200 OK`:**

```json
{
  "profilePictureUrl": "/Media/144",
  "mediaId": 144
}
```

---

### POST /api/media/profile/unsplash

Set an Unsplash photo as the profile picture. The photo is saved to the media library with proper attribution.

**Request body:**

```json
{
  "photoId": "abc123xyz"
}
```

**Response `200 OK`:**

```json
{
  "profilePictureUrl": "/Media/145",
  "mediaId": 145
}
```

---

### DELETE /api/media/profile

Remove the profile picture. The associated media item is also deleted.

**Response `204 No Content`**

---

## 14. Admin — Dashboard

All admin endpoints require `Authorization: Bearer <token>` and the `Admin` role.

### GET /api/admin/dashboard

Returns the full dashboard snapshot in one call: user stats, discussion stats, comment stats, recent activity.

**Response `200 OK`:**

```json
{
  "users": {
    "totalUsers": 1240,
    "verifiedUsers": 1090,
    "businessUsers": 342,
    "recentRegistrations": {
      "Oct 2025": 48,
      "Nov 2025": 61,
      "Dec 2025": 55,
      "Jan 2026": 72,
      "Feb 2026": 83,
      "Mar 2026": 21
    }
  },
  "discussions": {
    "totalDiscussions": 4821,
    "publicDiscussions": 4200,
    "featuredDiscussions": 12,
    "popularDiscussions": { "How to Start a Business": 8200 },
    "monthlyDiscussions": { ... }
  },
  "comments": {
    "totalComments": 18043,
    "mostCommentedDiscussions": { ... },
    "monthlyComments": { ... }
  },
  "recentUsers": [ ... ],
  "recentDiscussions": [ ... ],
  "recentComments": [ ... ]
}
```

---

### GET /api/admin/dashboard/users

User statistics only.

### GET /api/admin/dashboard/discussions

Discussion statistics only.

### GET /api/admin/dashboard/comments

Comment statistics only.

---

## 15. Admin — Users

### GET /api/admin/users `[Admin]`

List all users with roles and login history.

| Query param | Type | Description |
|---|---|---|
| `emailConfirmed` | `bool` | Filter by email confirmation status |
| `role` | `string` | Filter by role name (e.g. `Admin`, `Business`) |

**Response `200 OK`:** array of [UserModel](#usermodel) (includes `roles`, `isLockedOut`, `lastLogin`, `loginCount`)

---

### GET /api/admin/users/{userId} `[Admin]`

**Response `200 OK`:** [UserModel](#usermodel)

---

### PUT /api/admin/users/{userId} `[Admin]`

Update any user's information.

**Request body:**

```json
{
  "email": "newemail@example.com",
  "firstName": "Jane",
  "lastName": "Smith",
  "displayName": "Jane Smith",
  "bio": "Small business owner",
  "websiteUrl": "https://janesmith.com"
}
```

All fields are optional — only provided fields are updated.

**Response `204 No Content`**

---

### DELETE /api/admin/users/{userId} `[Admin]`

Permanently delete a user account.

**Response `204 No Content`**

---

### POST /api/admin/users/{userId}/lock `[Admin]`

Toggle the lockout state. Locks the account for 100 years if currently unlocked; removes the lockout if currently locked.

**Response `200 OK`:**

```json
{ "locked": true }
```

---

### GET /api/admin/users/{userId}/roles `[Admin]`

**Response `200 OK`:**

```json
{
  "currentRoles": ["User", "Business"],
  "availableRoles": ["Admin", "Business", "User"]
}
```

---

### PUT /api/admin/users/{userId}/roles `[Admin]`

Replace the user's complete role set. Send an empty array to remove all roles.

**Request body:**

```json
{
  "roles": ["Business", "User"]
}
```

**Response `204 No Content`**

---

### POST /api/admin/users/business `[Admin]`

Create a pre-confirmed business user account with an auto-generated password.

**Request body:**

```json
{
  "email": "partner@theirbusiness.com",
  "firstName": "Alice",
  "lastName": "Jones",
  "bio": "Owner of ABC Plumbing",
  "websiteUrl": "https://abcplumbing.com"
}
```

**Response `200 OK`:**

```json
{
  "userId": "3fa85f64-...",
  "email": "partner@theirbusiness.com",
  "temporaryPassword": "xK9!mR2@qP7#nL4$"
}
```

> **Security note:** Display this password to the admin immediately. It is not stored and cannot be retrieved again. Advise the admin to pass it to the new user securely and require them to change it on first login.

---

## 16. Admin — Comments

### GET /api/admin/comments `[Admin]`

All comments on the platform, ordered newest first.

**Response `200 OK`:** array of [PostCommentModel](#postcommentmodel)

---

### GET /api/admin/comments/{id} `[Admin]`

**Response `200 OK`:** [PostCommentModel](#postcommentmodel)

---

### PUT /api/admin/comments/{id} `[Admin]`

Edit any comment regardless of ownership (moderation).

**Request body:**

```json
{
  "content": "Edited by moderator."
}
```

**Response `204 No Content`**

---

### DELETE /api/admin/comments/{id} `[Admin]`

Delete any comment regardless of ownership.

**Response `204 No Content`**

---

## 17. Admin — Roles

### GET /api/admin/roles `[Admin]`

**Response `200 OK`:**

```json
[
  { "id": "1a2b3c...", "name": "Admin" },
  { "id": "4d5e6f...", "name": "Business" },
  { "id": "7g8h9i...", "name": "User" }
]
```

---

### POST /api/admin/roles `[Admin]`

Create a new role.

**Request body:**

```json
{ "name": "Moderator" }
```

**Response `200 OK`:**

```json
{ "message": "Role 'Moderator' created." }
```

---

### DELETE /api/admin/roles/{roleId} `[Admin]`

**Response `204 No Content`**

---

## 18. Real-Time Chat (SignalR)

The chat feature uses **SignalR** over WebSockets. The hub URL is configured per environment.

### Connection

Install the client library:

```bash
npm install @microsoft/signalr
```

Connect to the hub:

```javascript
import * as signalR from '@microsoft/signalr';

const connection = new signalR.HubConnectionBuilder()
  .withUrl('https://api.sharesmallbiz.com/chatHub', {
    accessTokenFactory: () => localStorage.getItem('token'),
  })
  .withAutomaticReconnect()
  .build();

await connection.start();
```

### Sending a message

```javascript
await connection.invoke('SendMessage', {
  user: displayName,
  message: 'Hello everyone!',
});
```

### Receiving messages

```javascript
connection.on('ReceiveMessage', (user, message) => {
  console.log(`${user}: ${message}`);
});
```

### Connection status

```javascript
connection.onreconnecting(() => setStatus('Reconnecting...'));
connection.onreconnected(() => setStatus('Connected'));
connection.onclose(() => setStatus('Disconnected'));
```

---

## 19. Community Stats

### GET /api/stats `[Public]`

Returns aggregate community statistics. Results are cached for 5 minutes.

**Response `200 OK`:**

```json
{
  "totalMembers": 1240,
  "totalDiscussions": 4821,
  "totalArticles": 1102,
  "totalKeywords": 318,
  "memberGrowthThisMonth": 83
}
```

---

## 20. Search

### GET /api/search `[Public]`

Unified full-text search across discussions, profiles, and keywords.

| Query param | Type | Default | Description |
|---|---|---|---|
| `q` | `string` | — | **Required.** Minimum 2 characters. |
| `type` | `string` | `null` | Filter to `discussions`, `profiles`, or `keywords`. If omitted, all types are returned. |
| `pageSize` | `int` | `5` | Results per type (max 50). |

**Response `200 OK`:** [SearchResultModel](#searchresultmodel)

**Error responses:**

| Status | Reason |
|---|---|
| `400` | `q` missing or fewer than 2 characters |

**Example:**

```http
GET /api/search?q=marketing&type=discussions&pageSize=10
```

---

## 21. User Settings

Manage per-user notification and privacy preferences. All endpoints require `Authorization: Bearer <token>`.

### GET /api/users/{userId}/settings `[Authenticated]`

Get the authenticated user's preference settings.

**Response `200 OK`:** [UserSettingsModel](#usersettingsmodel)

---

### PUT /api/users/{userId}/settings `[Authenticated]`

Update preference settings. Only the authenticated user can update their own settings.

**Request body:** [UserSettingsModel](#usersettingsmodel)

**Response `200 OK`:**

```json
{ "message": "Settings updated successfully." }
```

---

## 22. Notifications

Manage in-app notifications for the authenticated user. All endpoints require `Authorization: Bearer <token>`.

### GET /api/notifications `[Authenticated]`

Get paginated notifications for the authenticated user.

| Query param | Type | Default | Description |
|---|---|---|---|
| `unreadOnly` | `bool` | `false` | Return only unread notifications |
| `pageSize` | `int` | `20` | Per page (max 50) |
| `pageNumber` | `int` | `1` | 1-based page number |

**Response `200 OK`:** array of [NotificationModel](#notificationmodel)

---

### PUT /api/notifications/{id}/read `[Authenticated]`

Mark a single notification as read. Returns `403 Forbidden` if the notification does not belong to the authenticated user.

**Response `200 OK`:**

```json
{ "message": "Notification marked as read." }
```

---

### POST /api/notifications/read-all `[Authenticated]`

Mark all unread notifications for the authenticated user as read.

**Response `200 OK`:**

```json
{ "markedCount": 7 }
```

---

## 23. Direct Messages

REST-based private messaging between users. All endpoints require `Authorization: Bearer <token>`.

### GET /api/messages/conversations `[Authenticated]`

Get all conversations for the authenticated user, ordered by most-recent message.

**Response `200 OK`:** array of [ConversationModel](#conversationmodel)

---

### GET /api/messages/conversations/{conversationId} `[Authenticated]`

Get paginated messages for a specific conversation. Also marks incoming unread messages as read.

| Query param | Type | Default |
|---|---|---|
| `pageSize` | `int` | `30` |
| `pageNumber` | `int` | `1` |

**Response `200 OK`:** array of [MessageModel](#messagemodel)

**Error responses:**

| Status | Reason |
|---|---|
| `403` | User is not a participant in this conversation |

---

### POST /api/messages `[Authenticated]`

Send a private message.

**Request body:**

```json
{
  "recipientId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "content": "Hello, I saw your post about marketing — great tips!"
}
```

Content is trimmed of leading/trailing whitespace and must not be empty.

**Response `201 Created`:** [MessageModel](#messagemodel)

**Error responses:**

| Status | Reason |
|---|---|
| `400` | Content is empty or whitespace |
| `404` | Recipient user not found |

---

## 24. Articles / CMS

Articles are public posts (`isPublic=true`) — they reuse the `Post` entity with an optional `category` field. All endpoints are public (`[AllowAnonymous]`).

### GET /api/articles `[Public]`

Paginated list of articles (content field omitted for performance).

| Query param | Type | Default | Description |
|---|---|---|---|
| `pageNumber` | `int` | `1` | 1-based page |
| `pageSize` | `int` | `10` | Per page (max 50) |
| `category` | `string` | `null` | Filter by category slug |
| `tag` | `string` | `null` | Filter by keyword/tag name |
| `featured` | `bool` | `null` | `true` = featured only |

**Response `200 OK`:** [ArticleListResult](#articlelistresult)

---

### GET /api/articles/featured `[Public]`

Get featured articles.

| Query param | Type | Default |
|---|---|---|
| `count` | `int` | `6` |

**Response `200 OK`:** array of [ArticleModel](#articlemodel)

---

### GET /api/articles/categories `[Public]`

Get all article categories with post counts. Cached for 15 minutes.

**Response `200 OK`:**

```json
[
  { "name": "Marketing", "slug": "marketing", "articleCount": 42 },
  { "name": "Finance",   "slug": "finance",   "articleCount": 28 }
]
```

---

### GET /api/articles/related/{slug} `[Public]`

Get related articles (same category, excluding the specified article).

| Query param | Type | Default |
|---|---|---|
| `count` | `int` | `4` |

**Response `200 OK`:** array of [ArticleModel](#articlemodel)

---

### GET /api/articles/{slug} `[Public]`

Get a single article with full content. Increments the view counter.

**Response `200 OK`:** [ArticleModel](#articlemodel) (with `content` field populated)

**Error responses:**

| Status | Reason |
|---|---|
| `404` | No public article with that slug |

---

## 25. Events

Community events listing. All endpoints are public (`[AllowAnonymous]`).

### GET /api/events `[Public]`

Get upcoming events. Results are cached for 5 minutes per `from`+`count` combination.

| Query param | Type | Default | Description |
|---|---|---|---|
| `from` | `DateTime` | today (UTC) | Start date filter (inclusive) |
| `count` | `int` | `10` | Max events to return |

**Response `200 OK`:** array of [EventModel](#eventmodel)

---

### GET /api/events/{id} `[Public]`

Get a single event by ID.

**Response `200 OK`:** [EventModel](#eventmodel)

**Error responses:**

| Status | Reason |
|---|---|
| `404` | Event not found |

---

## 26. AI Assistant

Context-aware AI business advice (Phase 1 — curated responses; Phase 2 will integrate an LLM).

### POST /api/ai/chat `[Authenticated]`

Submit a question and receive structured business advice with suggestions and action items.

**Request body:**

```json
{
  "message": "How do I attract more repeat customers?",
  "context": "retail"
}
```

`context` is optional. Supported values: `retail`, `restaurant`, `services`, `ecommerce`. Any other value (or omitting the field) returns general small-business advice.

**Response `200 OK`:**

```json
{
  "response": "Great question about retail! ...\n\nRegarding your question...",
  "suggestions": [
    "Focus on customer experience over price",
    "Build a loyalty program",
    "Leverage social media for local awareness",
    "Partner with complementary local businesses"
  ],
  "actionItems": [
    "Audit your current customer touchpoints",
    "Set up a simple loyalty rewards program",
    "Create a content calendar for social media",
    "Identify 3 local businesses to collaborate with"
  ]
}
```

---

## 27. Data Models Reference

### UserModel

```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "email": "jane@example.com",
  "userName": "jane.smith",
  "displayName": "Jane Smith",
  "firstName": "Jane",
  "lastName": "Smith",
  "bio": "Small business owner from Austin, TX.",
  "websiteUrl": "https://janesmith.com",
  "profilePictureUrl": "/Media/42",
  "profileViewCount": 1234,
  "likeCount": 87,
  "postCount": 14,
  "followerCount": 128,
  "followingCount": 34,
  "isFollowedByMe": false,
  "isEmailConfirmed": true,
  "isLockedOut": false,
  "roles": ["User", "Business"],
  "lastLogin": "2026-03-09T18:22:00Z",
  "loginCount": 47,
  "posts": []
}
```

`followerCount`, `followingCount`, and `isFollowedByMe` are populated in profile endpoints. `isFollowedByMe` is `null` when the request is unauthenticated.

### ProfileModel

Extends `UserModel` with:

```json
{
  "analytics": {
    "totalViews": 3892,
    "recentViews": {},
    "geoDistribution": {},
    "engagement": {
      "followerCount": 128,
      "newFollowers": 0,
      "totalLikes": 87,
      "recentLikes": 0
    }
  },
  "publicUsers": []
}
```

### DiscussionModel

```json
{
  "id": 101,
  "title": "How to Market Your Small Business on LinkedIn",
  "slug": "how-to-market-your-small-business-on-linkedin",
  "description": "A short excerpt shown in listings.",
  "content": "<p>Full HTML content...</p>",
  "cover": "https://images.unsplash.com/photo-abc123",
  "isPublic": true,
  "isFeatured": false,
  "postType": 0,
  "postViews": 4821,
  "shareCount": 14,
  "isSavedByMe": false,
  "rating": 4.5,
  "published": "2026-01-15T12:00:00Z",
  "createdDate": "2026-01-14T09:30:00Z",
  "modifiedDate": "2026-01-15T11:00:00Z",
  "tags": ["Marketing", "LinkedIn"],
  "creator": { "...": "UserModel fields" },
  "comments": [],
  "likes": [],
  "media": []
}
```

`isSavedByMe` is `null` when unauthenticated.

### PostCommentModel

```json
{
  "id": 55,
  "postId": 101,
  "content": "Really useful, thanks for sharing!",
  "likeCount": 3,
  "isLikedByMe": true,
  "createdDate": "2026-02-01T10:15:00Z",
  "modifiedDate": null,
  "author": {
    "id": "3fa85f64-...",
    "userName": "jane.smith",
    "displayName": "Jane Smith",
    "profilePictureUrl": "/Media/42"
  },
  "likes": []
}
```

`isLikedByMe` is `null` when unauthenticated.

### KeywordModel

```json
{
  "id": 7,
  "name": "Marketing",
  "description": "Topics related to business marketing strategies.",
  "postCount": 312
}
```

### MediaModel

```json
{
  "id": 200,
  "fileName": "company-logo.png",
  "description": "Main company logo",
  "attribution": "Design team",
  "url": "/uploads/abc123-company-logo.png",
  "contentType": "image/png",
  "fileSize": 48200,
  "mediaType": 0,
  "storageProvider": 0,
  "storageMetadata": "{}",
  "userId": "3fa85f64-...",
  "postId": null,
  "commentId": null,
  "createdDate": "2026-02-20T09:00:00Z",
  "modifiedDate": "2026-02-20T09:00:00Z"
}
```

#### `mediaType` values

| Value | Meaning |
|---|---|
| `0` | Image |
| `1` | Video |
| `2` | Audio |
| `3` | Document |

#### `storageProvider` values

| Value | Meaning |
|---|---|
| `0` | LocalStorage (uploaded file) |
| `1` | External (URL link) |
| `2` | YouTube |
| `3` | Unsplash |

---

### StatsModel

```json
{
  "totalMembers": 1240,
  "totalDiscussions": 4821,
  "totalArticles": 1102,
  "totalKeywords": 318,
  "memberGrowthThisMonth": 83
}
```

### SearchResultModel

```json
{
  "discussions": [ /* array of DiscussionModel */ ],
  "profiles":    [ /* array of UserModel */ ],
  "keywords":    [ /* array of KeywordModel */ ]
}
```

When a `type` filter is specified, the other two arrays are returned as empty (not null).

### UserSettingsModel

```json
{
  "emailNotificationsEnabled": true,
  "notifyOnFollower": true,
  "notifyOnComment": true,
  "notifyOnLike": false,
  "notifyOnDirectMessage": true,
  "profileVisibility": "Public",
  "showEmailOnProfile": false,
  "allowDirectMessages": true
}
```

### NotificationModel

```json
{
  "id": 88,
  "type": "NewFollower",
  "message": "Jane Smith started following you.",
  "isRead": false,
  "targetId": null,
  "targetType": null,
  "actorId": "3fa85f64-...",
  "actorDisplayName": "Jane Smith",
  "actorProfilePictureUrl": "/Media/42",
  "createdDate": "2026-03-10T14:00:00Z"
}
```

Common `type` values: `NewFollower`, `NewComment`, `NewLike`, `NewDirectMessage`.

### ConversationModel

```json
{
  "conversationId": "aaa-user-id_bbb-user-id",
  "otherUserId": "bbb-user-id",
  "otherUserDisplayName": "Bob Builder",
  "otherUserProfilePictureUrl": "/Media/55",
  "otherUserSlug": "bob-builder",
  "lastMessage": "Sounds great, let's connect!",
  "lastMessageDate": "2026-03-11T08:30:00Z",
  "unreadCount": 2
}
```

Conversation IDs are deterministic: `min(userIdA, userIdB)_max(userIdA, userIdB)`, ensuring bidirectional messages share the same key.

### MessageModel

```json
{
  "id": 201,
  "senderId": "aaa-user-id",
  "content": "Hello, great to connect!",
  "sentDate": "2026-03-11T08:29:00Z",
  "isRead": true
}
```

### ArticleModel

```json
{
  "id": 101,
  "title": "5 Marketing Strategies for Small Retailers",
  "slug": "5-marketing-strategies-for-small-retailers",
  "description": "A quick summary of the article.",
  "content": "<p>Full HTML body (only in detail response)...</p>",
  "cover": "https://images.unsplash.com/photo-xyz",
  "category": "marketing",
  "isFeatured": true,
  "postViews": 3200,
  "published": "2026-02-14T09:00:00Z",
  "tags": ["Marketing", "Retail"],
  "author": { "...": "partial UserModel" }
}
```

`content` is omitted (null/empty) in list responses and populated in `GET /api/articles/{slug}`.

### ArticleListResult

```json
{
  "articles": [ /* array of ArticleModel (no content) */ ],
  "totalCount": 128,
  "pageNumber": 1,
  "pageSize": 10
}
```

### ArticleCategoryModel

```json
{
  "name": "Marketing",
  "slug": "marketing",
  "articleCount": 42
}
```

### EventModel

```json
{
  "id": 5,
  "title": "Small Business Networking Night",
  "description": "Monthly meetup for local small business owners.",
  "location": "123 Main St, Austin TX",
  "isOnline": false,
  "startDate": "2026-04-15T18:00:00Z",
  "endDate": "2026-04-15T20:00:00Z",
  "registrationUrl": "https://example.com/register",
  "organizerId": "3fa85f64-...",
  "organizerName": "Jane Smith"
}
```

### AiChatResponse

```json
{
  "response": "Here are some general strategies...\n\nRegarding your question...",
  "suggestions": [
    "Build genuine relationships with your customers",
    "Focus on solving one problem exceptionally well",
    "Use data to understand what's working",
    "Invest in your online presence"
  ],
  "actionItems": [
    "Define your ideal customer in detail",
    "Ask your best customers what they love most about your business",
    "Set one specific growth goal for this quarter",
    "Review your online reviews and respond to all of them"
  ]
}
```

---

## 28. Error Reference

| Scenario | Status | Body |
|---|---|---|
| Missing/expired token | `401` | `{ "message": "..." }` |
| Token valid but wrong role | `403` | `{ "message": "..." }` |
| Editing another user's comment | `403` | (empty Forbid) |
| Resource not found | `404` | `{ "message": "..." }` |
| Duplicate role name | `409` | `{ "message": "Role 'X' already exists." }` |
| Rate limit exceeded | `429` | `{ "message": "..." }` |
| Identity validation error | `400` | array of `{ "code", "description" }` |
| Unexpected server error | `500` | `{ "message": "..." }` (detail hidden in production) |

---

## 29. Quick-Start Examples

### JavaScript / React fetch utility

```javascript
// api.js
const BASE = 'https://api.sharesmallbiz.com';

function getToken() {
  return localStorage.getItem('ssb_token');
}

async function apiFetch(path, options = {}) {
  const token = getToken();
  const res = await fetch(`${BASE}${path}`, {
    ...options,
    headers: {
      'Content-Type': 'application/json',
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
      ...options.headers,
    },
  });

  if (!res.ok) {
    const err = await res.json().catch(() => ({ message: res.statusText }));
    throw Object.assign(new Error(err.message ?? 'API error'), { status: res.status });
  }

  return res.status === 204 ? null : res.json();
}

export const api = {
  // Auth
  login: (email, password) =>
    apiFetch('/api/auth/login', { method: 'POST', body: JSON.stringify({ email, password }) }),

  // Profiles
  getProfiles: () => apiFetch('/api/profiles'),
  getProfile: (slug) => apiFetch(`/api/profiles/${slug}`),
  followProfile: (slug) => apiFetch(`/api/profiles/${slug}/follow`, { method: 'POST' }),
  unfollowProfile: (slug) => apiFetch(`/api/profiles/${slug}/unfollow`, { method: 'POST' }),

  // Discussions
  getDiscussions: (onlyPublic = true) =>
    apiFetch(`/api/discussion?onlyPublic=${onlyPublic}`),
  getDiscussion: (id) => apiFetch(`/api/discussion/${id}`),
  getPagedDiscussions: (pageNumber = 1, pageSize = 20, sortType = 0) =>
    apiFetch(`/api/discussion/paged?pageNumber=${pageNumber}&pageSize=${pageSize}&sortType=${sortType}`),
  createDiscussion: (model) =>
    apiFetch('/api/discussion', { method: 'POST', body: JSON.stringify(model) }),
  updateDiscussion: (id, model) =>
    apiFetch(`/api/discussion/${id}`, { method: 'PUT', body: JSON.stringify(model) }),
  deleteDiscussion: (id) =>
    apiFetch(`/api/discussion/${id}`, { method: 'DELETE' }),
  likeDiscussion: (id) =>
    apiFetch(`/api/discussion/${id}/like`, { method: 'POST' }),

  // Comments
  getComments: (postId) => apiFetch(`/api/comments?postId=${postId}`),
  addComment: (postId, content) =>
    apiFetch('/api/comments', { method: 'POST', body: JSON.stringify({ postId, content }) }),
  editComment: (id, content) =>
    apiFetch(`/api/comments/${id}`, { method: 'PUT', body: JSON.stringify({ content }) }),
  deleteComment: (id) =>
    apiFetch(`/api/comments/${id}`, { method: 'DELETE' }),

  // Keywords
  getKeywords: () => apiFetch('/api/keywords'),

  // Media
  getMedia: (params = {}) => {
    const q = new URLSearchParams(params).toString();
    return apiFetch(`/api/media${q ? '?' + q : ''}`);
  },
  uploadMedia: (file, description = '', attribution = '') => {
    const form = new FormData();
    form.append('file', file);
    form.append('description', description);
    form.append('attribution', attribution);
    return apiFetch('/api/media/upload', {
      method: 'POST',
      headers: { Authorization: `Bearer ${getToken()}` }, // no Content-Type for FormData
      body: form,
    });
  },
  deleteMedia: (id) => apiFetch(`/api/media/${id}`, { method: 'DELETE' }),

  // Profile picture
  uploadProfilePicture: (file) => {
    const form = new FormData();
    form.append('file', file);
    return apiFetch('/api/media/profile/upload', {
      method: 'POST',
      headers: { Authorization: `Bearer ${getToken()}` },
      body: form,
    });
  },
  removeProfilePicture: () => apiFetch('/api/media/profile', { method: 'DELETE' }),

  // Community stats & search
  getStats: () => apiFetch('/api/stats'),
  search: (q, type = null, pageSize = 5) => {
    const params = new URLSearchParams({ q, pageSize });
    if (type) params.set('type', type);
    return apiFetch(`/api/search?${params}`);
  },

  // User settings
  getUserSettings: (userId) => apiFetch(`/api/users/${userId}/settings`),
  updateUserSettings: (userId, settings) =>
    apiFetch(`/api/users/${userId}/settings`, { method: 'PUT', body: JSON.stringify(settings) }),

  // Notifications
  getNotifications: (unreadOnly = false, pageSize = 20) =>
    apiFetch(`/api/notifications?unreadOnly=${unreadOnly}&pageSize=${pageSize}`),
  markNotificationRead: (id) =>
    apiFetch(`/api/notifications/${id}/read`, { method: 'PUT' }),
  markAllNotificationsRead: () =>
    apiFetch('/api/notifications/read-all', { method: 'POST' }),

  // Direct messages
  getConversations: () => apiFetch('/api/messages/conversations'),
  getMessages: (conversationId, pageSize = 30) =>
    apiFetch(`/api/messages/conversations/${conversationId}?pageSize=${pageSize}`),
  sendMessage: (recipientId, content) =>
    apiFetch('/api/messages', { method: 'POST', body: JSON.stringify({ recipientId, content }) }),

  // Discussion interactions
  saveDiscussion: (id) => apiFetch(`/api/discussion/${id}/save`, { method: 'POST' }),
  shareDiscussion: (id) => apiFetch(`/api/discussion/${id}/share`, { method: 'POST' }),
  getSavedDiscussions: () => apiFetch('/api/discussion/saved'),
  likeComment: (id) => apiFetch(`/api/comments/${id}/like`, { method: 'POST' }),

  // Articles
  getArticles: (params = {}) => {
    const q = new URLSearchParams(params).toString();
    return apiFetch(`/api/articles${q ? '?' + q : ''}`);
  },
  getFeaturedArticles: (count = 6) => apiFetch(`/api/articles/featured?count=${count}`),
  getArticleCategories: () => apiFetch('/api/articles/categories'),
  getArticle: (slug) => apiFetch(`/api/articles/${slug}`),
  getRelatedArticles: (slug, count = 4) =>
    apiFetch(`/api/articles/related/${slug}?count=${count}`),

  // Events
  getUpcomingEvents: (count = 10) => apiFetch(`/api/events?count=${count}`),
  getEvent: (id) => apiFetch(`/api/events/${id}`),

  // AI assistant
  askAi: (message, context = null) =>
    apiFetch('/api/ai/chat', { method: 'POST', body: JSON.stringify({ message, context }) }),

  // Password reset
  forgotPassword: (email) =>
    apiFetch('/api/auth/forgot-password', { method: 'POST', body: JSON.stringify({ email }) }),
  resetPassword: (email, token, newPassword) =>
    apiFetch('/api/auth/reset-password', { method: 'POST', body: JSON.stringify({ email, token, newPassword }) }),
};
```

---

### React login hook

```javascript
import { useState } from 'react';
import { api } from './api';

export function useAuth() {
  const [user, setUser] = useState(null);
  const [error, setError] = useState(null);

  async function login(email, password) {
    try {
      const data = await api.login(email, password);
      localStorage.setItem('ssb_token', data.token);
      setUser({ id: data.userId, displayName: data.displayName });
    } catch (err) {
      setError(err.message);
    }
  }

  function logout() {
    localStorage.removeItem('ssb_token');
    setUser(null);
  }

  return { user, error, login, logout };
}
```

---

### curl examples

**Login:**
```bash
curl -X POST https://api.sharesmallbiz.com/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"jane@example.com","password":"Secret1!"}'
```

**List recent discussions (public):**
```bash
curl https://api.sharesmallbiz.com/api/discussion/most-recent/5
```

**Create a discussion (authenticated):**
```bash
curl -X POST https://api.sharesmallbiz.com/api/discussion \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{
    "title": "My First Post",
    "content": "<p>Hello world</p>",
    "description": "A brief intro post",
    "isPublic": true,
    "tags": ["Introductions"]
  }'
```

**Upload a file:**
```bash
curl -X POST https://api.sharesmallbiz.com/api/media/upload \
  -H "Authorization: Bearer $TOKEN" \
  -F "file=@/path/to/image.png" \
  -F "description=My logo"
```

**Admin — get dashboard:**
```bash
curl https://api.sharesmallbiz.com/api/admin/dashboard \
  -H "Authorization: Bearer $ADMIN_TOKEN"
```

---

*This document describes the API as deployed at `api.sharesmallbiz.com`. The interactive Scalar UI at `/scalar/v1` reflects the live schema and can be used to test endpoints directly in non-production environments.*
