# Testing Documentation

This document describes Palette’s backend testing strategy, test infrastructure, execution instructions, coverage evidence, and known testing gaps.

## 1. Testing Strategy

Palette uses unit tests and integration tests to verify application behaviour and interactions between components.

The backend regression suite covers:

- Normal search and lookup workflows for movies and TV shows.
- Input validation, boundary values, missing data, and failure handling.
- External-provider request construction and response processing.
- HTTP response contracts and application middleware.
- PostgreSQL persistence, constraints, relationships, and cascade deletion.

The complete backend suite is run through `tests/Palette.Tests.csproj`. Running the full suite provides regression checks across shared components when the application changes.

Coverage percentages help identify untested code, but do not establish that every requirement or user workflow has been verified. Test selection also considers behaviour, component interactions, failure conditions, and user-story acceptance criteria.

## 2. Unit Testing

### Tools and Frameworks

- **xUnit:** Test execution and assertions.
- **EF Core InMemory:** Existing lightweight database-model tests.
- **Custom test doubles:** Controlled provider responses and recorded calls.
- **Stub HTTP handlers:** Simulated TMDB responses without external requests.

### Tested Components

| Component | Tested behaviour |
| --- | --- |
| SearchService | Missing queries, query-length boundaries, trimming, provider selection, result-limit forwarding, empty results, errors, and cancellation |
| LookupService | Missing IDs, trimming, provider selection, cancellation-token forwarding, and error propagation |
| MediaProviderResolver | Case-insensitive resolution, unknown types, duplicate registrations, and independent provider selection |
| TmdbMapper | Movie and TV mapping, dates, poster URLs, descriptions, runtimes, season counts, and missing optional values |
| ProviderCallGuard | Successful calls, HTTP and JSON failures, upstream cancellation, caller cancellation, and unexpected exceptions |
| TmdbClient | Request paths, query encoding, response deserialization, HTTP failures, invalid responses, and cancellation |
| Movie and TV providers | ID validation, result limits, mapping, missing items, provider failures, and cancellation |
| ApiExceptionHandler | ProblemDetails responses, trace IDs, logging levels, internal-detail protection, and unhandled exceptions |
| TMDB configuration | Required settings, URL validation, timeout boundaries, defaults, client configuration, and provider registration |

EF Core InMemory tests are not used as evidence that PostgreSQL constraints or database-side cascade deletion work correctly. Those behaviours are verified separately against PostgreSQL.

## 3. Integration Testing

### HTTP API Integration Tests

HTTP integration tests use ASP.NET Core TestServer and the real endpoint, service, provider, mapper, and error-handling implementations.

Only the external TMDB HTTP transport is replaced.

Tests cover:

- Movie and TV search results, empty results, and result limits.
- Movie and TV lookup responses, including media-specific fields.
- Missing optional data.
- Invalid requests and unsupported media types.
- HTTP 400, 404, and 502 responses.
- ProblemDetails content types, response fields, and trace IDs.
- Protection against leaking upstream error details.

These tests do not require a real TMDB account or API token.

### Application Startup Tests

WebApplicationFactory starts the application’s actual entry point using an isolated `Testing` configuration.

Startup tests verify:

- Health and backend connection endpoints.
- Movie and TV search registration.
- CORS behaviour for allowed and other origins.
- CORS preflight handling.
- Application exception-handling middleware.

These tests do not exercise the Development-only startup branch.

### PostgreSQL Integration Tests

Database integration tests use PostgreSQL 17 and the application’s EF Core migrations.

The shared `PostgresFixture` uses Testcontainers to manage a temporary database when no explicit test connection is supplied.

Database tests cover:

- Persistence and retrieval.
- Movie, TV, Book, and Game relationships with Media.
- Unique external identifiers.
- User email and external-identity uniqueness.
- Required database fields.
- Review and UserMediaLog foreign keys.
- Collection and CollectionItem relationships.
- CollectionItem composite-key uniqueness.
- Database-side cascade deletion.
- Preservation of unrelated records when deleting related entities.

Tests clear EF tracking where necessary so that assertions verify database reads and PostgreSQL behaviour rather than only the tracked object graph.

Database test data is isolated using transactions that are rolled back after each test.

## 4. Mocking and Test Utilities

### StubMediaProvider

`StubMediaProvider` implements the existing `IMediaProvider` interface.

Service tests configure its responses and inspect recorded calls. Unexpected, unconfigured calls fail rather than silently returning successful results.

### Stub HTTP Handlers

Stub handlers provide deterministic HTTP responses to the real `TmdbClient`.

They simulate successful responses, missing items, invalid JSON, network errors, and cancellation. Automated tests do not depend on live TMDB data or availability.

### Testcontainers and PostgresFixture

Without `PALETTE_TEST_DB_CONNECTION`, the fixture:

1. Creates a temporary PostgreSQL 17 container.
2. Uses an available host port.
3. Waits for the database to start.
4. Applies EF Core migrations.
5. Shares the database across the PostgreSQL test collection.
6. Disposes of the temporary container after testing.

If `PALETTE_TEST_DB_CONNECTION` is supplied, the fixture uses that connection instead. The override is restricted to the designated local test database to reduce the risk of running tests against development data.

Testcontainers manages only the containers it creates. An externally supplied database remains managed by its owner.

## 5. Running Tests

### Prerequisites

- .NET 10 SDK.
- Docker Desktop or a compatible Docker environment.
- A running Docker engine.

The first execution may need network access to restore NuGet packages and download container images.

### Run All Backend Tests

From the repository root:

```bash
dotnet test tests/Palette.Tests.csproj
```

With the default Testcontainers setup, no manual database creation, connection-string export, or local environment file is required.

If the terminal already contains `PALETTE_TEST_DB_CONNECTION`, that setting overrides automatic database creation. The configured database must then be available.

### Run Only Unit Tests

```bash
dotnet test tests/Palette.Tests.csproj \
  --filter "FullyQualifiedName~Palette.Tests.Unit"
```

Unit tests do not require PostgreSQL.

### Run HTTP Integration Tests

```bash
dotnet test tests/Palette.Tests.csproj \
  --filter "FullyQualifiedName~Palette.Tests.Integration.Api"
```

The current HTTP tests use isolated application settings and simulated TMDB responses.

### Run PostgreSQL Integration Tests

```bash
dotnet test tests/Palette.Tests.csproj \
  --filter "FullyQualifiedName~Palette.Tests.Integration.Database"
```

Docker must be running unless an available external test database is explicitly configured.

### Run One Test Class

For example:

```bash
dotnet test tests/Palette.Tests.csproj \
  --filter "FullyQualifiedName~MediaSubtypeDatabaseTests"
```

## 6. Test Coverage and Regression Evidence

### Collect Coverage

The test project includes Coverlet. Coverage settings are defined in `tests/coverage.runsettings`.

Run the following commands from the repository root in the same terminal:

```bash
coverage_run="TestResults/coverage-$(date +%Y%m%d-%H%M%S)"

dotnet test tests/Palette.Tests.csproj \
  --collect:"XPlat Code Coverage" \
  --settings tests/coverage.runsettings \
  --results-directory "$coverage_run" \
  --logger "trx;LogFileName=backend-tests.trx"
```

This produces:

- A TRX test execution report.
- A Cobertura XML coverage report.

Each execution uses a separate results directory to avoid combining reports from different runs unintentionally.

### Generate an HTML Report

Install ReportGenerator once:

```bash
dotnet tool install --global \
  dotnet-reportgenerator-globaltool \
  --version 5.6.0
```

Generate the report:

```bash
"$HOME/.dotnet/tools/reportgenerator" \
  "-reports:$coverage_run/**/coverage.cobertura.xml" \
  "-targetdir:$coverage_run/html" \
  "-reporttypes:Html"
```

On macOS, open it with:

```bash
open "$coverage_run/html/index.html"
```

On other platforms, open the generated `index.html` in a browser.

### Coverage Scope

The report measures the backend application assembly and excludes:

- EF Core migration classes and model snapshots.
- OpenAPI-generated code.
- Compiler support types.

These exclusions separate generated infrastructure from application code. Database migrations are still used to initialize test databases.

`Program.cs`, application services, providers, mappings, error handling, the DbContext, and entity models remain included.

Exclusion rules should be kept consistent between local and CI reports and documented whenever they change.

### Recorded Local Evidence

A local report captured on November 13, 2026 showed:

- **Line coverage: 98.3% — 587 of 597 lines.**
- **Branch coverage: 98.1% — 53 of 54 branches.**
- **Program.cs: 75% line coverage and 50% branch coverage.**

The remaining Program.cs gap corresponds to Development-only startup behaviour. Current startup tests run in the `Testing` environment.

These figures describe that recorded local run. They are not a claim that CI has already produced the same result or that all acceptance criteria are covered.

For milestone evidence, retain a report or screenshot together with its tested commit, test execution result, and coverage configuration.

### Continuous Integration

The backend workflow is located at `.github/workflows/ci.yml`.

The coverage-enabled workflow update is pending verification through a pull request. After it runs successfully, milestone documentation should link to:

- The actual workflow execution.
- The test result and coverage artifacts.
- The coverage summary.
- The tested commit.

A green test execution alone is not a coverage report.

If a regression test fails, investigate whether the implementation regressed, the test is unreliable, or the intended contract changed. Fix the cause and rerun the affected tests and full suite before considering the change ready.

## 7. Current Limitations and Follow-Up Work

- **Development startup:** Automatic migrations, OpenAPI exposure, and HTTPS redirection inside the Development-only Program.cs branch are not covered by the current startup tests.
- **Frontend automation:** This backend suite does not measure frontend coverage or replace frontend component, navigation, and browser tests.
- **Real external services:** Simulated TMDB responses verify application behaviour but do not prove that live TMDB credentials or deployed connectivity are correct.
- **Acceptance criteria:** User-story acceptance criteria still need an explicit mapping to automated tests or recorded manual checks.
- **CI evidence:** The updated coverage collection and artifact publication must be verified after the pull request is created.
- **Deployed workflows:** Local automated tests do not replace representative checks of the deployed application.
- **Future features:** Authentication and history/logging functionality require their own tests when integrated. Existing coverage percentages must not be interpreted as coverage of unmerged features.
- **Custom migrations:** Future migrations containing hand-written data transformations should receive dedicated upgrade/data-preservation tests even though migration classes are excluded from the application coverage metric.

Testing is a shared team responsibility. Feature changes should include relevant regression tests, and the testing documentation should be updated as functionality and known gaps change.