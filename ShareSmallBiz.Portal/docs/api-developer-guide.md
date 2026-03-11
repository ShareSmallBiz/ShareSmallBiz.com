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
19. [Data Models Reference](#19-data-models-reference)
20. [Error Reference](#20-error-reference)
21. [Quick-Start Examples](#21-quick-start-examples)

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

## 19. Data Models Reference

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
  "isEmailConfirmed": true,
  "isLockedOut": false,
  "roles": ["User", "Business"],
  "lastLogin": "2026-03-09T18:22:00Z",
  "loginCount": 47,
  "posts": []
}
```

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

### PostCommentModel

```json
{
  "id": 55,
  "postId": 101,
  "content": "Really useful, thanks for sharing!",
  "likeCount": 3,
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

## 20. Error Reference

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

## 21. Quick-Start Examples

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
