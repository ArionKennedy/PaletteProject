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

These endpoints do not require user authentication.

For request parameters, response formats, error handling, and examples, see [Search & Lookup API Contract](search-api.md).

## 3. Authentication

Palette uses Clerk for user authentication and session management.

Authentication is managed through Clerk on the frontend.

**Clerk Configuration (Development):**

- **Frontend API:** `https://crisp-ocelot-4132.clerk.accounts.dev/`
- **Backend API:** `https://api.clerk.com/`
- **JWKS Endpoint:** `https://crisp-ocelot-4132.clerk.accounts.dev/.well-known/jwks.json`

The JWKS endpoint provides public keys that can be used to verify Clerk-issued JWTs.

The current ASP.NET Core endpoint configuration does not expose a dedicated authentication endpoint. Backend token verification and user synchronization have not yet been confirmed.
