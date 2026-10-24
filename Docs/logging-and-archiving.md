# Sprint 1: Logging and Archiving

## What this branch implements

A viewer can mark a TV show as watched, save it to PostgreSQL, and retrieve their history.
The My Shows screen displays real saved records, with loading, empty, error, refresh, and load-more states.
Repeated saves return the existing entry. Reviews remain separate and are not required.

Scope: TV shows, watched status, one log per user/show through this endpoint.
Collections, review forms, editing, deleting, and rewatch events are not implemented.

The frontend uses the team's Clerk sign-in flow. Backend Clerk token validation,
profile provisioning, and media search are still team dependencies. The local logging
demo uses one explicit shared demo profile and five sample TV choices, even when
different Clerk accounts sign in. These are not each Clerk user's personal logs.
The picker is local testing UI, not a replacement for Search & Discovery.
Nothing is inserted into MEDIA just by browsing it.

## Files

- `backend/Logging/ShowLogService.cs`: save and read database queries.
- `backend/Logging/LoggingAuthentication.cs`: two endpoints, current-user lookup,
  and temporary local-only authentication handler.
- `backend/Program.cs`: registers the service, authentication, and endpoints.
- `frontend/src/app/(app)/(tabs)/my-shows.tsx`: screen state and API calls.
- `frontend/src/components/show-list.tsx`: list display and status messages.
- `frontend/src/services/show-logs.ts`: HTTP requests, timeouts, and errors.
- `frontend/src/types/show-log.ts`: frontend request/response types.
- `frontend/src/constants/logging.ts`: demo picker and API settings.
- `tests/Palette.Api.Tests/ShowLogTests.cs`: PostgreSQL-backed integration tests.

The existing `sample-shows.ts` file is preserved but no longer used by the screen.
Login, signup, protected routes, and logout behavior are kept from `develop`; this
feature does not restore the deleted mock session helper or logout route. Logging UI
uses the shared Reusables `Text` component and theme constants from `lib/theme`.
The remaining removed-component references in `ui/collapsible.tsx` are migrated too.
Navigation files add My Shows. Dependencies and their lockfile match `develop`.
The parent database test project excludes the separate logging integration project
so both suites can build independently.

## Run the local demo

After switching branches, run `npm ci` inside `frontend` to install the locked dependencies.
Create or update the ignored `frontend/.env` with the team's Clerk **publishable** key:

```dotenv
EXPO_PUBLIC_CLERK_PUBLISHABLE_KEY=your_team_clerk_publishable_key
```

Get the actual publishable key from the authentication teammate. Do not put a Clerk
secret key in an `EXPO_PUBLIC_` variable or commit your `.env`. The standard Clerk
sign-in is required to reach My Shows, including when the logging demo is enabled.

The verification container is named `palette-logging-check-20261020`.
It uses a separate database, `palette_logging_tests`, bound only to local port 55435.
The password below is only for that disposable local test container, not a team credential.
Its data remains in the container until that container is removed.

Ensure Docker Desktop is running. To start the existing verification container:

```sh
docker start palette-logging-check-20261020
```

If that container has not been created on this computer:

```sh
docker run --detach --name palette-logging-check-20261020 \
  --publish 127.0.0.1:55435:5432 \
  --env POSTGRES_PASSWORD=palette-local-check \
  --env POSTGRES_DB=palette_logging_tests postgres:17
```

In a terminal at the repository root, start the backend:

The latest `develop` also registers the team's movie search provider. Configure its
`Tmdb:ReadAccessToken` using the team's local setup instructions/user secrets before
starting the app; provider configuration is validated at startup even on the logging
screen. Do not put that private token in frontend environment variables or Git.

```sh
ASPNETCORE_ENVIRONMENT=Development \
LoggingDemo__Enabled=true \
ConnectionStrings__DefaultConnection='Host=127.0.0.1;Port=55435;Database=palette_logging_tests;Username=postgres;Password=palette-local-check' \
dotnet run --project backend/Palette.Api.csproj --no-launch-profile --urls http://127.0.0.1:5251
```

In another terminal, from `frontend`:

```sh
EXPO_PUBLIC_API_URL=http://127.0.0.1:5251 \
EXPO_PUBLIC_LOGGING_DEMO=true \
npm run web -- --port 8081
```

Open http://localhost:8081, sign in through the team's Clerk screen, then open My Shows.
Mark a sample show as watched, reload, and reopen My Shows: the entry persists.
The screen labels this as shared local demo history, not the signed-in Clerk user's history.
A backend/front-end process must be stopped before starting another on the same port.
Use Ctrl+C in the server terminal to stop it. Stop the container with
`docker stop palette-logging-check-20261020` when it is no longer needed.

The local demo handler accepts only the explicit demo token from a loopback connection,
with Development environment and LoggingDemo:Enabled=true. It fails closed otherwise.
Never enable this mode for shared or production deployments. It does not provide real login.
It authenticates only the local logging API's shared demo profile; it does not bypass Clerk's
frontend route protection. A release frontend hides the test picker and never selects the
demo token even if the environment variable was set.

## Endpoint contract

Both endpoints require an authenticated subject. Neither accepts a user ID from the client.

- `GET /api/me/shows?page=1`: returns `{ items, page, hasMore }`, at most 50 items,
  ordered by logged date and log ID descending. Items include logId, mediaId, tmdbTvId,
  title, status, and loggedDate.
- `POST /api/me/shows`: accepts `{ tmdbTvId, title }`. Saves watched status and returns
  the entry. Validates a positive ID and a nonempty title of at most 300 characters.
  Returns the existing media/log when already present. Logged dates currently use UTC.

Saving uses one database transaction and a PostgreSQL transaction advisory lock per TV ID,
so concurrent requests through this service do not produce duplicate media/log entries.
This does not add a database-wide unique constraint to logs; future writers must reuse
this service or coordinate their own constraints. Existing data is not deleted or deduplicated.
The API checks the ID format but does not contact TMDB to verify metadata; future search
integration supplies the selected show's provider ID and title.

## Remaining backend authentication and search integration

With logging demo mode disabled, My Shows requests a current Clerk session token with
`getToken()` for each API operation. It sends that token unchanged and never falls back
to the demo account if authentication fails. The current backend does not yet validate
Clerk tokens, so normal-mode logging requests receive an authorization error until that
integration is completed. Frontend sign-in alone does not make logging production-ready.

The authentication owner must replace the temporary backend authentication registration
with a validated Clerk token scheme and provision the corresponding Palette user profile.
Preserve the authorized endpoints. The existing user lookup column is still named
`USERS.Auth0Id`; agree on the Clerk subject mapping or a schema migration with the team.
Do not trust a user ID supplied by the frontend. Once real authentication is configured,
an unknown profile receives 404 rather than being silently created.

Search & Discovery should call saveWatchedShow with the selected show's TMDB TV ID and title.
Remove the local picker when that integration is complete.

## Verification

From the repository root:

```sh
dotnet test tests/Palette.Tests.csproj

PALETTE_TEST_DATABASE='Host=127.0.0.1;Port=55435;Database=palette_logging_tests;Username=postgres;Password=palette-local-check' \
dotnet test tests/Palette.Api.Tests/Palette.Api.Tests.csproj
```

The suite accepts only a local database named palette_logging_tests.
It uses a test-only TMDB placeholder and a loopback provider address; it does not
require real TMDB credentials or make external provider requests.
Tests create synthetic profiles/shows in that database and never clear a team database.
They cover persistence, separate user histories, unauthorized requests, concurrent duplicate
saves, invalid inputs, shared media records, pagination, and unknown profiles.

From frontend: `npx tsc --noEmit`, `npx expo lint`, and `npx expo export --platform web`.
The web build requires the Clerk publishable key from `frontend/.env`.
The existing UseStorageState.ts has two hook-dependency lint warnings; other upstream
warnings may also appear. The earlier mock-login version was checked for saving and
reloading entries; retest the updated Clerk flow with a team test account before merging.
Check signed-out redirects, the shared-demo label, save/refresh/pagination, and that
leaving the screen or switching accounts does not apply an older request's result.
Native device testing and real backend authentication/search integration remain outstanding.

## Review workflow

Work belongs on `Israel_logging/archiving_task`, with review against `develop`.
Keep the team's authentication screens unchanged and resolve integration conflicts on
the feature branch. After testing, review and commit the fixes, then push the branch
to update its existing pull request. Wait for teammate approval before merging the PR.
Do not commit .env files, node_modules, bin/obj, or build output.
