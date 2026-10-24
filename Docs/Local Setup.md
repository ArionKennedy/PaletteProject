# Local Development Setup

This guide explains how to set up and run Palette locally, including the Expo frontend, ASP.NET Core backend, PostgreSQL database, and Docker.

## 1. Prerequisites

Install the following tools before starting:

- **Git** — for cloning the repository
- **Node.js and npm** — for the Expo frontend and development scripts
- **.NET SDK 10** — for the ASP.NET Core backend
- **Docker Desktop** (or Docker Engine with Compose) — for containerized services
- **Expo Go** (optional) — for testing on a physical mobile device

A PostgreSQL client such as pgAdmin or DBeaver is optional.

Verify your installations:

```bash
git --version
node --version
npm --version
dotnet --version
docker --version
docker compose version
```

Make sure Docker Desktop is running before starting Docker containers.

## 2. Clone the Repository

```bash
git clone https://github.com/ArionKennedy/COMP4350Project.git
cd COMP4350Project
```

All commands below assume you are in the project root unless otherwise specified.

## 3. Install Dependencies

Install the root-level development dependencies:

```bash
npm install
```

Install the frontend dependencies:

```bash
cd frontend
npm ci
cd ..
```

Restore the backend dependencies:

```bash
dotnet restore backend/Palette.Api.csproj
```

The root-level npm scripts use `concurrently` to run the frontend and backend together.

## 4. Configure the Frontend

### Environment Variables

Create a local environment file from the provided template:

```bash
cp frontend/.env.example frontend/.env.local
```

For local web development, configure:

```env
EXPO_PUBLIC_API_URL=http://localhost:5251
EXPO_PUBLIC_CLERK_PUBLISHABLE_KEY=<your_clerk_publishable_key>
```

- `EXPO_PUBLIC_API_URL` must match the address used by the backend.
- `EXPO_PUBLIC_CLERK_PUBLISHABLE_KEY` is required for Clerk authentication. Obtain the publishable key from the team's development configuration.

**Important:**

- `.env.local` contains machine-specific configuration and should not be committed to Git.
- `.env.example` is a template that can be committed.
- Never put passwords, Clerk secret keys, or private API tokens in variables prefixed with `EXPO_PUBLIC_`, since Expo exposes these values to client applications.

### Expo Go on Physical Devices

When testing on a physical device, `localhost` refers to the device itself, not your computer.

Replace `localhost` with your computer's local network IP address, for example:

```env
EXPO_PUBLIC_API_URL=http://192.168.x.x:5251
```

The computer and device must be able to communicate over the network. The backend must also be configured to listen on a network-accessible interface. Mobile operating system HTTP security restrictions may require additional configuration.

Local web development is recommended for the initial setup.

## 5. Configure Docker and PostgreSQL

Palette uses PostgreSQL 17, running in a Docker container for local development.

### Configure the Database Environment

Create a `.env` file in the same directory as the Docker Compose file:

```env
POSTGRES_PASSWORD=your_local_password
```

Replace `your_local_password` with a password of your choice. Make sure the `.env` file is ignored by Git.

The Docker Compose configuration uses the following settings:

| Setting | Value |
|---|---|
| Container | `palette-postgres` |
| Database | `palette_dev` |
| Username | `palette` |
| Password | Value of `POSTGRES_PASSWORD` |
| Port | `5432` |

### Start PostgreSQL

From the directory containing the Docker Compose file, run:

```bash
docker compose up -d
```

Check that the container is running:

```bash
docker compose ps
```

### Configure the Backend Connection

The backend must use the same PostgreSQL credentials configured above.

From the project root, run:

```bash
cd backend
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=palette_dev;Username=palette;Password=your_local_password"
cd ..
```

Replace `your_local_password` with the same password used in the Docker `.env` file.

.NET User Secrets stores the local connection string outside the repository, so database credentials do not need to be committed to Git.

If the backend project already has a `UserSecretsId`, you can skip `dotnet user-secrets init`.

### Configure the TMDB Token

The backend requires a TMDB API Read Access Token to access movie information.

Obtain the token from the team's shared development resources and store it using .NET User Secrets.

From the project root:

```bash
dotnet user-secrets set "Tmdb:ReadAccessToken" "<your_token>" --project backend
```

Replace `<your_token>` with the actual TMDB API Read Access Token.

Do not commit the token to the repository.

### Stop PostgreSQL

To stop the database container, run from the Docker Compose directory:

```bash
docker compose down
```

The database data is preserved in the `postgres_data` Docker volume.

Do not use `docker compose down -v` unless you intentionally want to delete the database volume.

## 6. Database Migrations

Palette uses Entity Framework Core to manage its PostgreSQL schema.

Pending migrations are automatically applied when the backend starts in the Development environment. Follow Section 7 to start the application.

When no additional migrations are pending, the backend may display:

```text
No migrations were applied.
The database is already up to date.
```

### Manual Migration Commands

When working on database schema changes, use the EF Core CLI.

From the `backend` directory, list existing migrations:

```bash
dotnet ef migrations list
```

After modifying entity models or the database context, create a new migration:

```bash
dotnet ef migrations add DescriptiveMigrationName
```

Apply pending migrations manually when needed:

```bash
dotnet ef database update
```

Do not create a new migration just to initialize an existing database if the repository already contains the required migrations.

For detailed instructions, see [Database Migrations](db-migration.md).

## 7. Run the Application

Make sure PostgreSQL is running and the backend connection string, TMDB token, and frontend environment variables have been configured.

### Option A: Start Frontend and Backend Together

From the project root:

```bash
npm start
```

This uses `concurrently` to start both the Expo frontend and ASP.NET Core backend.

The backend normally runs at:

```text
http://localhost:5251
```

Expo normally starts its local development server on port 8081, unless the port is changed or unavailable.

Follow the Expo terminal instructions to open the application in a web browser or Expo Go.

Press `Ctrl+C` to stop both development services.

**Note:** `npm start` does not start PostgreSQL. The Docker database container must already be running.

### Option B: Start Services Separately

The frontend and backend can also be started independently in separate terminals.

**Terminal 1 — Backend**

```bash
cd backend
dotnet run
```

**Terminal 2 — Frontend**

```bash
cd frontend
npx expo start
```

For browser testing, you can also use:

```bash
npx expo start --web
```

Running the services separately is useful when debugging frontend or backend issues.

### Verify Backend Connection

Once the backend is running, open:

```text
http://localhost:5251/api/test
```

Expected response:

```json
{
  "message": "Connected to backend!"
}
```

This verifies that the backend is running and responding to HTTP requests. It does not verify database connectivity or external API availability.

## 8. Running Tests

Palette includes automated backend tests and frontend development quality checks.

### Backend Tests

Backend tests use xUnit, including database unit tests with EF Core InMemory.

From the repository root:

```bash
dotnet test tests/Palette.Tests.csproj
```

The same command is used in the GitHub Actions CI workflow.

### Frontend Checks

Automated frontend tests have not yet been implemented.

The following commands can be used for static type checking and linting.

From the `frontend` directory:

```bash
npx tsc --noEmit
npx expo lint
```

These checks help identify TypeScript errors and code quality issues but do not replace automated frontend tests.

For more details about testing frameworks, integration tests, and coverage, see [Testing Documentation](testing.md).
