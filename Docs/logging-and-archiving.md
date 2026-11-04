# Logging and archiving

## Confirmed Sprint 1 scope

Display only titles the signed-in user has reviewed. A watched status alone is not enough.
Reviews are the source of truth: the list joins REVIEWS to MEDIA, covers any reviewed media type,
and displays each title once, ordered by its most recent review.
No extra log record, demo account, sample picker, review editor, or watched-save action is needed.
Creating and editing reviews belongs to the team's review feature.

## Current integration

The working branch targets develop and retains link-develop's shared Clerk authentication.
GET /api/me/shows?page=1 returns items, page, and hasMore (up to 50 titles per page).
Each item contains mediaId, title, mediaType, and lastReviewedAt.
The verified Clerk subject maps to USERS.Auth0Id (the existing column name).
The shared /api/users/me flow synchronizes the Palette profile before displaying tabs.
Caller-supplied user IDs do not select whose history is returned.
POST /api/me/shows has been removed: it saved watched TV shows, which is outside the confirmed scope.
No database schema changes are required.

My Shows refreshes on focus, account changes, and manual refresh; pagination is available.
Old requests are ignored after leaving the screen or changing accounts.
Empty, loading, and failure states are displayed.
Removing a user's last review for a title removes it from their history on the next refresh.

## Local verification

Follow Local Setup.md for the team's actual Clerk, TMDB, and database configuration.
Real sign-in requires frontend/.env configuration; do not commit credentials.

Frontend checks from frontend/: npx tsc --noEmit and npx expo lint --no-cache.
Refresh-loop regression from the repository root: node --test tests/frontend/my-shows.test.cjs.

Database integration tests: set PALETTE_TEST_DATABASE to a dedicated local PostgreSQL database
named palette_logging_tests, unset POSTGRES_DB/POSTGRES_USER/POSTGRES_PASSWORD overrides,
and run dotnet test tests/Palette.Api.Tests/Palette.Api.Tests.csproj.
The tests use test-only authenticated identities, not real Clerk sign-in, and create test records.
They verify reviewed-only filtering, account isolation, deduplication, ordering, pagination,
no writes during history reads, removal after review deletion, and authentication failures.

Before merging, test the app with real Clerk accounts and reviews created by the team's review flow.
