# Sprint 2

Sprint 2 focuses on expanding Palette's working features beyond Sprint 1, improving regression testing and code quality, introducing automated delivery, and making the application accessible for review.

This page serves as the landing page for Sprint 2 progress and milestone evidence.

## 1. Sprint Summary

During Sprint 2, Palette continued development of its core media tracking and discovery features while expanding automated testing, continuous integration, and deployment infrastructure.

- Added Archiving to allow users to save favourited lists of movies, shows, and books
- Added Searches for TV Shows and books
- Added UI Elements and back page buttons to improve the user experience
-
[Summarize the main integrated improvements made during Sprint 2, and explain how they add value beyond Sprint 1. Describe only functionality that has been tested and can be demonstrated.]

See the [Sprint Board](https://github.com/users/ArionKennedy/projects/4) for detailed task tracking.

## 2. Completed Work

Detailed development progress is tracked through our GitHub issues, pull requests, and sprint board. The features listed below should represent integrated, testable, and demonstrable work.

| Feature | Sprint 2 Progress | Evidence |
|---|---|---|
| [Feature name] | [Describe the working change and its user value.] | [Issue/PR](#) |
| [Feature name] | [Describe the working change and its user value.] | [Issue/PR](#) |

**Note:** Linked evidence should point to representative merged PRs or completed issues. Additional bug fixes may be linked separately.

### Development Improvements

- [Describe significant refactoring or improvements to code readability, modularity, and separation of responsibilities.]
- [Describe updated development or database workflows.]
- [Describe new functionality that can be demonstrated end-to-end.]

## 3. Regression Testing

Regression testing helps verify that changes do not break previously working functionality. The backend suite covers search and lookup, My Library, API contracts, authentication and user provisioning, and PostgreSQL persistence. Frontend tests cover selected library interactions and password-reset behaviour.

### Current Regression Tests

The full backend suite is run through `tests/Palette.Tests.csproj`, and the frontend suite uses Node.js tests under `tests/frontend/`. The tests were selected to protect user-facing flows, API contracts, database constraints, and important error-handling paths. See the linked testing documentation for individual cases.

| Test Suite | Behaviour Covered | Why It Was Selected |
|---|---|---|
| Backend unit tests | Search/lookup services, media providers, mapping, validation, and error handling | Protects shared business logic and response contracts. |
| Backend HTTP integration tests | Authenticated API requests, search/lookup, user provisioning, and My Library | Checks interactions across real application components. |
| PostgreSQL integration tests | Persistence, relationships, uniqueness, constraints, and cascade deletion | Verifies database behaviour that EF Core InMemory cannot prove. |
| Frontend Node.js tests | My Library interactions and password-reset flows | Protects selected user interactions and failure states. |

### Execution and Failure Handling

- **Trigger:** Pull requests, pushes to `develop` and `master`, and manual workflow dispatch.
- **Approximate Duration:** Approximately 2-3 minutes.
- **Failure Handling:** Failed test jobs are reported in GitHub Actions. The team investigates and reruns the affected tests and failed suite after fixing failures. An existing branch rule prevents the pull request from being merged until the tests have been resolved.
- **Recent Execution:** The latest `develop` CI run is reported as passing as of November 20, 2026 (team confirmation). [GitHub Actions workflow runs](https://github.com/ArionKennedy/PaletteProject/actions/workflows/ci.yml). [Add the direct run URL, commit SHA, and measured duration.]

### Test Coverage and Gaps

- **Coverage Evidence:** [November 19 local coverage screenshot](evidence/backend-test-coverage-11-19.png); [CI artifacts and runs](https://github.com/ArionKennedy/PaletteProject/actions/workflows/ci.yml) [verify latest run].
- **Coverage Summary:** `Docs/testing.md` reports 471 passing backend tests, 99.5% line coverage (1064/1069), and 93.4% branch coverage (172/184), last verified locally November 19, 2026.

Important weakly tested or untested behaviour includes:

- **Live authentication:** Controlled test identities do not verify real Clerk JWT validation, expiry, or production connectivity; perform deployed sign-in and authenticated API checks.
- **Device and deployment behaviour:** Mocked frontend tests do not cover physical-device layout, live password-reset email delivery, or deployed HTTPS; record representative manual checks.
- **Acceptance criteria:** Map important user-story acceptance criteria to automated tests or documented manual checks.

See [Testing Documentation](testing.md) for testing tools, execution instructions, and current limitations.

## 4. Continuous Integration and Delivery

The project uses GitHub Actions for continuous integration. The backend is hosted on Render and the PostgreSQL database on Supabase. Render deploys automatically from the master branch.

### Continuous Integration

- **Trigger:** Pull requests, pushes to `develop` or `master`, and manual dispatch.
- **Automated Checks:** Separate frontend Node.js tests and backend .NET 10 tests. The backend job collects Cobertura coverage and uploads test/coverage artifacts.
- **Failure Handling:** Failed checks are visible in GitHub Actions; Branch rules are set to run the tests whe nmerging into master as well as develop, Anytime a pull request to develop fails, the assigned team members must resolve the issue before it can be approved by a member that had not worked on it.

The latest `develop` run is reported as **passing** (November 20, 2026). This confirms the current CI status as reported by the team, but the exact run, commit, job durations, and uploaded artifacts still need to be linked and checked.

See [CI Workflow](../.github/workflows/ci.yml) and [CI Runs](https://github.com/ArionKennedy/PaletteProject/actions/workflows/ci.yml). [Link the specific passing run before submission.]

### Continuous Delivery / Deployment

- **Development Branch:** `develop`
- **Workflow:** [Confirm whether Render GitHub auto-deploy is enabled; link Render deployment evidence if available.]
- **Deployment Steps:** [Describe what the workflow builds, publishes, or deploys.]
- **Success Criteria:** [Explain how a successful deployment is verified.]
- **Failure Handling:** [Describe where failures are reported and what the team does next.]

## 5. API Documentation

The project maintains documentation for interfaces between the frontend, backend, and supporting services. The documentation should reflect the currently integrated endpoints, request and response models, and authentication requirements.

See [API Documentation](api.md).

[Summarize significant API or interface changes since Sprint 1.]

## 6. System Architecture

The architecture documentation reflects the system's current implementation and major component relationships, including the frontend, backend, database, authentication, and external services.

- [System Architecture](architecture.md)
- [Database Design](database.md)

The backend and database now have hosted environments (Render and Supabase), unlike the local-only deployment described in Sprint 1. **Documentation update needed:** `architecture.md` still describes PostgreSQL as local-only and cloud deployment as undecided; update its deployment description and distinguish proposed Redis caching from deployed components.

## 7. Deployment and Local Setup

**Deployment Status:** The ASP.NET Core backend is hosted on Render and PostgreSQL is hosted on Supabase. The Expo frontend is currently run locally; instructor-accessible frontend deployment has not been confirmed.

- **Application Access:** Please download the most recently updated apk file for the mobile development at [Add Link]
- **Backend API:** Render, [add backend service URL and authenticated/public endpoint details.]
- **Database:** Supabase-hosted PostgreSQL. [Confirm connection health and version; do not disclose credentials.]
- **Deployment Verification:** [Explain how the team checked that the deployed version is running and current.]
- **Configuration:** Environment-based settings are used for application and database connectivity. Document required variable *names* for Render, Supabase, and Clerk without exposing values. Client-side `EXPO_PUBLIC_` variables must not contain secrets.

For local development, see:
- [Local Setup](Local%20Setup.md)
- [Database Migration Guide](db-migration.md)

## 8. Cross-Team Testing and Feedback

### Black-Box Review

[Once completed, describe how the team reviewed its assigned project using externally observable behaviour without relying on implementation details.]

- **Assigned Team / Project:** Group 5 - Drove
- **Testing Evidence:** [Link to submitted review.](#)
- **Test Scenarios:**
    - Tested a user logging in and verifying their account, the email took 5 minutes, took too long.
    - Tested the search bar, saw the user being confused how to search, determiend the UI needed to be relocated.
    - Tested availabitliy for student body, drives arent available for rural students, rural students need to be included.
    - Tested document verification, No checks for empty documents (blank pngs), a check needs to be added asto not waste an admin's time.
    - Tested vehicle registration, Any 30 character string can be added, Maybe add a dropdown for verified automotive dealers.
    - Tested credit card registration, some inputs for cards worked, others didnt, the postal code isnt verified, when saving the browser shows a danger message, just need to make the card entry consistent and resolve the postal code and browser extension issue.

- **Recommendations:**
    - Login verification takes way too long to send an email
        - Thought it didn't send

    - The search address bar is in a confusing place for pickup area, thought I could click it
        - Maybe move it to be onto or include the pickup area as a search bar (similar to google maps or apple maps)

    - Ignores students that live in local municipalities
        - Include students that live at least as far as Steinbach or even portage la prairie

    - No existing check for empty documents
        - Atleast parse maybe a face or text to exist

    - No dropdown for existing vehicles, user can put in anything they would like

    - Have to wait for an admin to review it, no automated checks

    - Card payments
        - The card entry is confusing and doesn't accept inputs like 1234 1234 1234 1234, but accepts 4242 4242 4242 4242
        - Doesn't check postal codes
        - Google browser shows a dangerous message in the tab, may be off putting to users
    <img width="1545" height="41" alt="Screenshot 2026-11-20 130738" src="https://github.com/user-attachments/assets/d5db5201-5d6f-4a02-9775-3e70a5e308fc" />

    Overall, the project was really well done otherwise. There are a lot of checks an input locks that were really well done, and the website design looks fantastic!

### Feedback Received on Palette

[Summarize the external feedback the team received and explain how it was reviewed.]

| Feedback | Decision | Reason / Next Step | Evidence |
|---|---|---|---|
| [Feedback item] | [Accepted / Deferred / Rejected] | [Reason] | [Issue/PR or discussion](#) |
| [Feedback item] | [A different decision category] | [Reason] | [Issue/PR or discussion](#) |

**Note:** Include at least two responses across at least two categories: accepted, deferred, and rejected.

## 9. Known Issues and Final Iteration Risks

- **[Risk or limitation]:** [Explain its impact, priority, and proposed next step.]
- **[Risk or limitation]:** [Explain its impact, priority, and proposed next step.]
- **[Remaining feature or testing gap]:** [Explain what is not yet complete and how the team plans to address it.]

## 10. Sprint Reflection

Each team member should contribute a brief reflection on their Sprint 2 work, challenges, and lessons learned.

### Individual Reflections

- **[Team Member]:** [Brief reflection or link.]
- **[Team Member]:** [Brief reflection or link.]
- **[Add remaining team members]:** [Brief reflection or link.]

### What are we proud of?

[Summarize one or two strong pieces of Sprint 2 work. Link representative issues or pull requests and explain why they demonstrate quality.]

### What could have been improved?

[Describe important coordination, implementation, testing, or delivery challenges and how they will be addressed.]

### How did testing fit into our workflow?

[Explain how regression testing, code review, CI, and deployment validation affected development during Sprint 2.]

## 11. Sprint Demonstration

The team will demonstrate the currently integrated functionality and explain the main improvements since Sprint 1.

- **Application Access:** [Verified URL or access instructions.]
- **Demonstration Steps:** [Core flow, Sprint 2 features, and testing/deployment evidence.]
- **Known Demo Limitations:** [Any limitations or workarounds.]

## 12. Milestone Evidence

The links below provide a summary of the key Sprint 2 evidence.

| Requirement | Evidence |
|---|---|
| Sprint Board and Completed Work | [Sprint Board](https://github.com/users/ArionKennedy/projects/4), [PRs / Issues](#) |
| Regression Testing and Results | [Testing Documentation](testing.md), [CI Runs](https://github.com/ArionKennedy/PaletteProject/actions/workflows/ci.yml) [specific run pending] |
| Test Coverage and Testing Gaps | [Local Coverage Evidence](evidence/backend-test-coverage-11-19.png), [Section 3](#3-regression-testing) |
| CI/CD and Deployment | [CI Workflow](../.github/workflows/ci.yml), [Render deployment details pending], [Section 7](#7-deployment-and-local-setup) |
| API and Architecture | [API Documentation](api.md), [System Architecture](architecture.md), [Database Design](database.md) |
| Cross-Team Review and Feedback | [Section 8](#8-cross-team-testing-and-feedback) |
| Individual Reflections and Best Work | [Section 10](#10-sprint-reflection) |
| Final Iteration Risks and Demonstration | [Section 9](#9-known-issues-and-final-iteration-risks), [Section 11](#11-sprint-demonstration) |
