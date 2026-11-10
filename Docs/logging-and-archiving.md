# Logging and archiving

## Sprint 1 scope

My Shows displays only titles the user has reviewed. Watched status alone is not enough.
REVIEWS joined to MEDIA is the source of truth. Each title appears once, newest review first.
History reads do not create media, reviews, profiles, or duplicate USER_MEDIA_LOGS records.
There is no demo account, sample picker, review editor, or watched-save action.

## Temporary integration contract

This branch is based on develop. Discarded link-develop authentication and profile-sync code
is not part of the implementation. Sign-in screens and the app layout come from develop.

POST /api/me/shows/query accepts { "userId": "<Clerk useAuth().userId>", "page": 1 }.
POST is used only to carry a JSON query body; the operation is read-only.
The service maps Clerk userId to the existing USERS.Auth0Id column and filters reviews
using that user's database UUID. A missing profile returns 404; logging never creates one.
The response contains items, page, and hasMore (up to 50 titles).
Items contain mediaId, title, mediaType (develop's enum name), and lastReviewedAt.

IMPORTANT: userId is caller-supplied and is not yet verified by the backend.
This is filtering, NOT access control. Changing userId can expose another user's history.
The authentication feature owner must verify it against the signed-in identity before release.
No bearer token, custom authentication scheme, JWT package, or Clerk issuer setting is added here.

## Frontend behavior

The list refreshes on focus, account/session changes, and manual refresh, with pagination.
Only primitive identity values drive the focus callback; changing Clerk function references
does not restart it. Responses from a previous screen lifecycle are ignored.
Loading, empty, and failure states are displayed.
Deleting the last review removes that title on the next refresh.

## Verification

Use develop's normal local setup. No additional profile-sync service or backend startup script is needed.
The database must already contain the user's Clerk ID and reviews from the team's review feature.
Actual sign-in testing requires local Clerk configuration.

Run from frontend/: npx tsc --noEmit and npx expo lint --no-cache.
Run from the repository root: node --test tests/frontend/my-shows.test.cjs.
For database tests, set PALETTE_TEST_DATABASE to a local PostgreSQL database named
palette_logging_tests and run dotnet test tests/Palette.Api.Tests/Palette.Api.Tests.csproj.
Tests create isolated test records and verify request validation, filtering, deduplication,
ordering, pagination, no writes on reads, and removal after review deletion.
They explicitly document the current lack of identity verification; they do not test real Clerk sign-in.
