# COMP4350 Project - Palette

Palette is a media tracking and discovery platform that allows users to track, review, organize, and discover movies, books, games, music, and other forms of entertainment.

The platform is designed around different ways people engage with media. Some users enjoy reviewing and sharing their opinions, some are looking for recommendations, and others want to maintain a personal archive of what they have experienced.

Palette brings these activities together in one place and helps users build a picture of their own media taste, or their personal “palette.”

## Core Features

Palette is designed around six core features. Development is ongoing, with Authentication and Movie Search implemented during Sprint 1.

- **[Authentication](https://github.com/ArionKennedy/COMP4350Project/issues/25)**
  - User account creation, sign-in, and sign-out using Clerk
  - User information stored in PostgreSQL
  - Secure access to personal data (planned)

- **[Search & Discovery](https://github.com/ArionKennedy/COMP4350Project/issues/26)**
  - Search for movies by title using TMDB
  - View movie details, including posters, descriptions, and runtime
  - Modular search structure designed to support additional media types and external APIs
  - Support for books, games, music, and other media types (planned)

- **[Logging & Archiving](https://github.com/ArionKennedy/COMP4350Project/issues/27)** *(Planned)*
  - Save watched, read, played, or listened-to media
  - Maintain a personal history of media activity
  - Organize saved media into collections or records

- **[Recommendations](https://github.com/ArionKennedy/COMP4350Project/issues/28)** *(Planned)*
  - Recommend what users may want to watch, read, play, or listen to next
  - Use user activity, community data, and relationships between media to support recommendations
  - Support specialized recommendations such as canonical or release-order viewing

- **[Sharing & Social Features](https://github.com/ArionKennedy/COMP4350Project/issues/29)** *(Planned)*
  - Rate and review media
  - Participate in discussions
  - Share media activity, opinions, and recommendations with other users

- **[Analytics & Visualizations](https://github.com/ArionKennedy/COMP4350Project/issues/30)** *(Planned)*
  - View statistics about personal media consumption
  - Track information such as total activity time and unique directors, authors, or game studios
  - Generate graphical summaries and visualizations similar to Spotify Wrapped or Receiptify

## Technology Stack

### Frontend

- React Native
- Expo
- TypeScript
- Clerk for authentication

### Backend

- ASP.NET Core Web API (.NET 10)
- Entity Framework Core
- Docker

### Data

- PostgreSQL 17 as the primary database
- EF Core migrations for database schema management
- Docker Compose for local database development
- Redis for caching recent queries (planned)

For the database schema, ER diagram, relationships, and data storage strategy, see [Database Design](Docs/database.md).

### Authentication and External Services

- Clerk for user authentication and session management
- TMDB API for movie search and lookup
- Additional external media APIs planned for future media types

### Hosting

- Currently running locally
- Cloud hosting provider: TBD
- AWS is being considered

React Native with Expo is used for cross-platform development, while ASP.NET Core provides backend application logic and APIs. PostgreSQL serves as the primary database, with Entity Framework Core managing database operations and migrations. Clerk handles user authentication, and TMDB provides movie data through backend API integrations.

Docker Compose is currently used for local PostgreSQL development. Additional infrastructure, including caching and cloud deployment, may be introduced in future sprints.

For more details, see [System Architecture](Docs/architecture.md).

## Project Documentation

Important project documentation:

- [Team Working Agreement](Docs/Team%20Working%20Agreement.pdf)
- [Sprint 0](Docs/sprint0.md)
- [Sprint 1](Docs/sprint1.md)
- [System Architecture](Docs/architecture.md)
- [Database Design](Docs/database.md)
- [API Documentation](Docs/api.md)
- [Testing Documentation](Docs/testing.md)
- [Process and Protocols](Docs/Process%20and%20Protocols.md)
- [Local Setup](Docs/Local%20Setup.md)
- [Database Migrations](Docs/db_migration.md)

## Team

- Junyi Hu
- William Gore
- Arion Kennedy
- Israel Ijiekhuamen
- Alexandre Brakel
- Binal Chhodavadiya

## Project Status

Palette is currently in Sprint 1, with its initial development infrastructure and first core features established.

**Implemented:**
- User authentication using Clerk
- Movie search and movie detail lookup using TMDB
- PostgreSQL database structure and EF Core migrations
- Frontend and backend integration
- Unified development startup command
- Database unit tests using xUnit and EF Core InMemory
- GitHub Actions CI for running backend tests on pull requests

**Current Limitations:**
- Search and lookup currently support movies only.
- Other core features are still under development.
- Automated frontend and integration tests have not yet been implemented.
- The application is not yet publicly deployed.

Development progress is tracked through the [Sprint Board](https://github.com/users/ArionKennedy/projects/4).

## Development Setup

For complete setup instructions, including frontend, backend, Docker, PostgreSQL, environment variables, and database migrations, see the [Local Setup Guide](Docs/Local%20Setup.md).

### Prerequisites

- Node.js and npm
- .NET SDK 10
- Docker Desktop (or Docker Engine with Compose)
- Expo Go (optional, for testing on a physical mobile device)

### Quick Start

**1. Install dependencies**

From the project root:

```bash
npm install
cd frontend
npm ci
cd ..
dotnet restore backend/Palette.Api.csproj
```

**2. Configure the environment**

Copy the frontend environment template:

```bash
cp frontend/.env.example frontend/.env.local
```

Configure the following frontend environment variables:

```env
EXPO_PUBLIC_API_URL=http://localhost:5251
EXPO_PUBLIC_CLERK_PUBLISHABLE_KEY=<your_clerk_publishable_key>
```

Configure the PostgreSQL database, backend connection string, and TMDB token according to the [Local Setup Guide](Docs/Local%20Setup.md).

Make sure the database is running before starting the backend.

**3. Start the application**

From the project root:

```bash
npm start
```

This starts both the Expo frontend and ASP.NET Core backend.

To stop both services, press `Ctrl+C`.

### Running Services Separately

The frontend and backend can also be started independently in separate terminals.

**Frontend:**

```bash
cd frontend
npx expo start
```

**Backend:**

```bash
cd backend
dotnet run
```

### Running Tests

Backend unit tests use xUnit and EF Core InMemory.

From the project root:

```bash
dotnet test tests/Palette.Tests.csproj
```

GitHub Actions automatically runs the backend test project on pull requests.

Integration tests and automated frontend tests are not included in Sprint 1.

For testing details and coverage information, see [Testing Documentation](Docs/testing.md).

### Repository Structure

```text
COMP4350Project/
├── backend/             # ASP.NET Core API
├── frontend/            # React Native / Expo application
├── tests/               # Backend tests
├── Docs/                # Project documentation
├── .github/workflows/   # CI configuration
├── package.json         # Development startup scripts
├── package-lock.json
└── README.md
```

The frontend and backend are maintained as separate projects and can be run together using `npm start` from the repository root, or independently.

The structure may evolve as development continues.
