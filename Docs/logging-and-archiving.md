# Logging and archiving

## Sprint 1 scope

My Shows displays only titles the user has reviewed. Watched status alone is not enough.
REVIEWS joined to MEDIA is the source of truth. Each title appears once, newest review first.
History queries do not create media, reviews, or duplicate USER_MEDIA_LOGS records.
The shared authentication middleware provisions a missing profile before the query runs.
There is no demo account, sample picker, review editor, or watched-save action.

## Authenticated integration contract

Logging uses develop's existing Clerk JWT validation, user service, and profile-provisioning middleware.
Sign-in screens and the app layout come from develop.

POST /api/me/shows/query accepts { "page": 1 } and requires a Clerk bearer token.
POST is used only to carry a JSON query body; the operation is read-only.
The endpoint gets the verified Clerk identity from UserService, maps it through USERS.ClerkId,
and filters reviews using that user's database UUID. Missing authentication returns 401.
A missing profile after shared provisioning returns 404.
The response contains items, page, and hasMore (up to 50 titles).
Items contain mediaId, title, mediaType (develop's enum name), and lastReviewedAt.

The frontend uses the shared fetchWithAuth helper to send the token. It does not submit userId.
An extra userId in the JSON body cannot override the authenticated identity.
Logging adds no separate authentication scheme or Clerk configuration.

## Frontend behavior

The list refreshes on focus, account/session changes, and manual refresh, with pagination.
Only primitive identity values drive the focus callback; changing Clerk function references
does not restart it. Responses from a previous screen lifecycle are ignored.
Loading, empty, and failure states are displayed.
Deleting the last review removes that title on the next refresh.

## Verification

Use develop's normal local setup. No additional profile-sync service or backend startup script is needed.
The database must contain reviews from the team's review feature to show a non-empty history.
Actual sign-in testing requires local Clerk configuration.

Run from frontend/: npx tsc --noEmit and npx expo lint --no-cache.
Run from the repository root: node --test tests/frontend/my-shows.test.cjs.
For backend tests, run dotnet test tests/Palette.Tests.csproj (the same project used by CI).
The existing PostgreSQL fixture starts an isolated container, or accepts the documented
PALETTE_TEST_DB_CONNECTION override for a local palette_integration_test database.
Tests create isolated test records and verify request validation, filtering, deduplication,
ordering, pagination, no writes on reads, and removal after review deletion.
Authentication tests use the shared test-only identity handler while keeping production
authorization and profile provisioning enabled. They verify anonymous rejection and that
supplying another user's ID cannot expose that user's history. Real Clerk sign-in still needs device testing.
