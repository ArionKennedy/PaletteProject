# API Documentation

This document provides an overview of the API contracts between the Palette frontend, ASP.NET Core backend, and external services.

## 1. Overview

**Local Base URL:** `http://localhost:5251`

**Response Format:** Successful API responses use JSON. API errors use `application/problem+json`.

**OpenAPI Specification:** `/openapi/v1.json` (Development environment only).

## 2. Search & Discovery

The backend provides two endpoints for searching media and retrieving item details.

| Method | Endpoint | Description |
|---|---|---|
| GET | `/api/search/{type}` | Search for media by title. |
| GET | `/api/lookup/{type}/{id}` | Retrieve details of a specific media item. |

Currently, only movies are supported through TMDB.

For request parameters, response formats, error handling, and examples, see [Search & Lookup API Contract](search-api.md).

## 3. Authentication

Clerk manages authentication for Palette and is the source of truth for user identity and profile data.

The frontend calls Clerks API to authenticate/create users, obtaining a Clerk session JWT. Requests to protected backend endpoints include that token as a bearer token in the `Authorization` header. In the frontend you can use `fetchWithAuth` to inject the token in your request. The backend validates the token using the configured Clerk Frontend API URL.

The Clerk session token is configured to include the user's `username` and `email` within the token as claims.

The frontend can read these claims from `sessionClaims`. For example:

```tsx
const { sessionClaims } = useAuth();
const username = sessionClaims?.username;
```

The backend reads these claims through `UserService`. Middleware uses these claims to create a new user record from the claims when an authenticated Clerk user has no record yet.

By default all backend endpoints require authentication, rejecting requests if not authorized. You can mark public endpoints with `[AllowAnonymous]` or `.AllowAnonymous()` to not require authenticaiton.
