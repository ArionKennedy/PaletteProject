# Logging and archiving

## My Library

Signed-in users can add or remove movies, TV shows, and books from their detail pages.
My Library opens a separate list for each type. My Library shows a horizontal cover preview for each type and has a manual refresh button. Each complete list refreshes on focus and supports pagination.
Lists and membership come from the signed-in user's records in `USER_MEDIA_LOGS`.

- `GET /api/me/library/previews` returns up to 12 saved covers per media type.
- `GET /api/me/library` returns counts by media type.
- `GET /api/me/library/{type}?page=1` returns saved media using the search result shape, including the provider ID used by detail routes.
- `GET /api/me/library/{type}/{externalId}` returns `{ "saved": true }` or `{ "saved": false }`.
- `GET /api/lookup/{type}/{externalId}?includeLibrary=true` includes the current user’s saved state with the media details.
- `PUT` and `DELETE` on the same item route add and remove membership and return the resulting state. Repeating either request is safe.

## Authentication

The frontend sends a Clerk bearer token using the shared `fetchWithAuth` helper.
The backend resolves the verified Clerk identity to a Palette user and scopes library operations to that user.
Missing authentication returns 401. A missing profile after shared provisioning returns 404.

## Verification

Use the normal local setup with Clerk and the provider credentials. Save an item from search, open it from its library list, then remove it and return to the list.

Run from frontend/: npx tsc --noEmit and npx expo lint --no-cache.
Run from the repository root: node --test tests/frontend/my-library.test.cjs.
For backend tests, run dotnet test tests/Palette.Tests.csproj (the same project used by CI).
The existing PostgreSQL fixture starts an isolated container, or accepts the documented
PALETTE_TEST_DB_CONNECTION override for a local palette_integration_test database.
Library integration tests cover account isolation, repeated saves and removals, category previews,
cover backfills, and the saved state returned with media details.
Authentication tests use the shared test-only identity handler while keeping production
authorization and profile provisioning enabled. Real Clerk sign-in still needs device testing.
