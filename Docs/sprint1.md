# Sprint 1

Sprint 1 focuses on establishing a working foundation for Palette,
integrating core features, and introducing automated testing,
continuous integration, and API documentation.

## 1. Sprint Summary

During Sprint 1, Palette transitioned from initial planning to implementing and integrating its first core features.

The team established a local development environment using React Native with Expo, ASP.NET Core, and PostgreSQL. The frontend and backend can be started together using a unified development command, and database schema changes are managed through EF Core migrations.

The first implemented features include user authentication and movie search, allowing users to access the application, search for movies by title, and view movie details.

The team also worked on automated testing, continuous integration, and documentation to support future development.

## 2. Completed Work

Detailed development progress is tracked through our GitHub issues, pull requests, and sprint board.

| Feature | Sprint 1 Progress | Evidence |
|---|---|---|
| Authentication | Implemented user authentication, including sign-up, sign-in, and sign-out. | [Issue/PR](https://github.com/ArionKennedy/PaletteProject/pull/44) |
| Movie Search | Implemented movie title search and movie detail pages displaying information such as posters, genres, and runtime. | [Issue/PR](https://github.com/ArionKennedy/PaletteProject/pull/48) |

**Note:** The linked evidence is the most representive PR for the feature, bugs might be fixed in separate small PRs.
Other planned features remain under development and are tracked on the sprint board.

### Development Infrastructure

- Configured the Expo frontend and ASP.NET Core backend.
- Established local PostgreSQL development using Docker Compose.
- Added EF Core migrations for managing database schema changes.
- Introduced a unified command for starting the frontend and backend.
- Documented local development and database migration workflows.

See the [Sprint Board](https://github.com/users/ArionKennedy/projects/4) for detailed task tracking.

## 3. Testing

Our current testing includes database unit tests and integration tests for movie search.

- **Unit Tests:** Database unit tests using EF Core InMemory.
- **Integration Tests:** Not yet implemented.
- **Frontend Tests:** Not yet implemented.

See [Testing Documentation](testing.md) for testing tools, execution instructions, and current limitations.

Coverage evidence: [Link to testing coverage](https://github.com/ArionKennedy/PaletteProject/actions/runs/40025119304)

## 4. Continuous Integration

The project uses GitHub Actions for continuous integration.

- **Trigger:** Pull requests.
- **Automated Checks:** Runs backend tests using .NET 10.
- **Test Environment:** PostgreSQL 17 service is configured.
- **Current Limitations:** Frontend tests are not yet included in the CI pipeline.

See [CI Workflow](../.github/workflows/ci.yml).

## 5. API Documentation

The project maintains documentation for interfaces between
the frontend, backend, and supporting services.

See [API Documentation](api.md).

## 6. System Architecture

The architecture documentation reflects the system's current
implementation and major component relationships.

- [System Architecture](architecture.md)
- [Database Design](database.md)

## 7. Deployment and Local Setup

**Deployment Status:** Not yet deployed.

The application currently runs locally using the Expo frontend, ASP.NET Core backend, and PostgreSQL database. The frontend and backend can be started together using a unified development command.

Deployment to a publicly accessible environment is planned for a future sprint.

For local development, see:
- [Local Setup](Local%20Setup.md)
- [Database Migration Guide](db-migration.md)

## 8. Known Issues and Limitations

- **Incomplete Features:** Logging & Archiving, Recommendations, Sharing & Social, and Analytics are not yet integrated into the application.
- **Movie Search:** Currently supports searching by movie title only.
- **Frontend Testing:** Automated frontend tests have not yet been implemented.
- **Deployment:** The application is not yet publicly deployed and currently runs locally.
- **Authentication:** The applciaiton currently only creates a token from clerk on the frontend and makes a cookie for the user to login to the application with, however the user is not yet stored in the database alongside their authenticated token.
- **Movie Lookup:** The lookup response does not currently include mediaType, although the frontend type expects this field.

## 9. Sprint Reflection

### What are we proud of?

We established a clear database structure, set up a CI pipeline, and introduced a unified start command to simplify local development. We also designed a modular search structure that makes it easier to add new media types and integrate additional external APIs in the future.

### What could have been improved?

Team coordination and early planning could have been better. Initial setup and integration took longer than expected, leaving less time for feature development and testing. In future sprints, we should start earlier, collaborate more frequently, and clarify design decisions before implementation to reduce unnecessary changes and integration difficulties.

### How did testing fit into our workflow?

Testing was introduced during the sprint, but much of the initial effort focused on project setup and feature implementation. As a result, some tests were written later in the development process. In future sprints, we plan to start testing earlier and allocate more time for both unit and integration tests. PR reviews worked well and helped us identify issues before merging changes.
