# Testing Documentation

This document describes the testing approach, tools, and current test coverage for Palette.

## 1. Testing Strategy

Our testing strategy combines unit tests and integration tests to validate individual components and interactions between system layers.

Tests are developed and maintained alongside the features they exercise.

## 2. Unit Testing

Unit tests currently focus on database operations and entity relationships.

### Tools and Frameworks

- xUnit
- EF Core InMemory database provider

### Tested Components

- Database operations and entity persistence.

## 3. Integration Testing

Integration tests verify interactions between system components.

### Tools and Frameworks

- xUnit

### Tested Components

- Movie search integration tests: Not implmented yet

## 4. Mocking and Test Utilities

- **EF Core InMemory:** Used for database unit tests without requiring a running PostgreSQL instance.
- **PostgreSQL 17:** Configured as a service in GitHub Actions for database-dependent tests.

## 5. Running Tests

Backend tests can be run from the repository root:

```bash
dotnet test tests/Palette.Tests.csproj
```

The same command is used in the GitHub Actions CI workflow.

## 6. Test Coverage

Current automated testing focuses on database operations, with movie search integration tests being added.

Coverage evidence: [Link to CI coverage report](https://github.com/ArionKennedy/PaletteProject/actions/runs/40026656302)

## 7. Current Limitations

- Automated frontend tests have not yet been implemented.
- Automated testing is currently focused on database operations and movie search.
- Testing should begin earlier in future sprints to allow more time for unit and integration tests.
