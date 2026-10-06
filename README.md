# SuperApp — Backend Service

The backend Web API for **SuperApp**, built with **.NET 8** and **C# 12** following a layered **Clean Architecture** (Controller → Service → Repository). It powers the SuperApp frontend with REST endpoints, real-time sync over SignalR, JWT/Google OAuth authentication, and Entity Framework Core data access over SQL Server. It also integrates Google Drive (file storage), a Claude-backed AI service (K module), and Git-based sync via LibGit2Sharp.

## Tech Stack

| Component | Technology |
| --- | --- |
| Framework | .NET 8.0 / ASP.NET Core Web API |
| Language | C# 12 |
| Architecture | Layered Clean Architecture (Controller → Service → Repository) |
| ORM / data access | Entity Framework Core 8 (SQL Server), plus a few stored procedures for bulk delete/move |
| Real-time | SignalR (`KSyncHub`, `KViewerTracker`) |
| Auth | JWT Bearer + Google OAuth; passwords hashed with BCrypt |
| Validation | FluentValidation |
| Mapping | AutoMapper |
| Logging | Serilog (Console + File sinks) |
| API docs | Swagger / Swashbuckle (Development only) |
| AI | OpenAI SDK client pointed at Claude (`claude-sonnet-4-5`) — used by the K module for grading/generation |
| File storage | Google Drive API (`Google.Apis.Drive.v3`) |
| Git integration | LibGit2Sharp (K repo sync) |
| Testing | xUnit |
| Config | DotNetEnv (`.env`) + `appsettings.*.json` + user-secrets |

## Solution Structure

The solution (`SuperApp-Service.sln`) is organized into Clean Architecture layers. Controllers depend on service interfaces (e.g. `INoteService`), which delegate to repositories — a straightforward layered flow, not CQRS/MediatR.

```
Timeline/
├── SuperAppAPI/                  # Presentation layer — ASP.NET Core Web API host
│   ├── Controllers/              #   REST controllers (Auth, Projects, K, Workspaces, LifeLog,
│   │                             #   DailyLog, File, Flow, Wiki, Keyword, Profile, Registry, …)
│   ├── Hubs/                     #   SignalR hubs (KSyncHub, KViewerTracker)
│   ├── BackgroundServices/       #   Hosted services (KRepoSyncBackgroundService, KRepoSyncDaemon)
│   ├── Middlewares/              #   Global error handling, etc.
│   ├── Extensions/ · Helpers/    #   DI wiring & helpers
│   ├── Program.cs · Startup.cs   #   Composition root
│   └── appsettings*.json
├── SuperAppServices/             # Application layer — business logic
│   ├── Services/ · Interfaces/   #   Use cases / service contracts
│   ├── Validators/               #   FluentValidation validators
│   └── Mappings/                 #   AutoMapper profiles
├── SuperAppDataRepositories/     # Infrastructure layer — data access
│   ├── Data/                     #   EF Core DbContext(s)
│   ├── Repositories/             #   Repositories (EF Core + stored procedures)
│   └── Migrations/               #   EF Core migrations
├── SuperAppModels/               # Domain layer — entities, DTOs, shared models
├── SuperAppServices.Tests/       # Unit / integration tests
├── docs/                         # Full documentation set (see below)
└── migrations/                   # SQL migration scripts
```

**Dependency direction:** `SuperAppAPI → SuperAppServices → SuperAppDataRepositories → SuperAppModels`.

## Getting Started

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- SQL Server (the app targets a `SuperApp-dev` / `SuperApp-pro` database)

### 1. Configure environment

Copy `.env.example` to `.env`. Secrets are **not** stored in `.env`: it only holds
`vault://<file>/<key>` references into tung-vault (sops),
resolved at startup by `scripts/run-dev.ps1` (`secret run`). `Program.cs` loads `.env` with
`NoClobber`, so real environment variables always win, and it refuses to start while any
`vault://` value is still unresolved.

```bash
cp .env.example .env
```

| Variable | Description |
| --- | --- |
| `ASPNETCORE_ENVIRONMENT` | `Development` or `Production` |
| `DB_SERVER` / `DB_PORT` / `DB_USER` / `DB_PASSWORD` | Database connection parts |
| `DB_NAME_DEV` / `DB_NAME_PRO` | Database name per environment |
| `ConnectionStrings__SuperAppConnection` | Main DB connection string |
| `ConnectionStrings__UserProfileConnection` | User-profile DB connection string |
| `Jwt__Key` / `Jwt__Issuer` / `Jwt__Audience` / `Jwt__ExpirationMinutes` | JWT settings |
| `OAuth__Google__ClientSecret` | Google OAuth client secret |

The dev connection strings carry no `Password=`; `run-dev.ps1` injects `DB_PASSWORD` inside the
process. The DB is reached through an SSH tunnel `127.0.0.1:14330 -> VPS:1433` (public 1433 is
closed); the script opens it when missing.

### 2. Run

```bash
dotnet restore
dotnet build

powershell -ExecutionPolicy Bypass -File scripts/run-dev.ps1                          # run the API
powershell -ExecutionPolicy Bypass -File scripts/run-dev.ps1 -Watch                   # hot reload
powershell -ExecutionPolicy Bypass -File scripts/run-dev.ps1 -Database SuperApp-test  # other DB
```

Swagger UI is available at the API root (`/swagger`) in Development.

### 3. Test

```bash
dotnet test
```

## Database

Data access is primarily through **Entity Framework Core 8** — a single `ApplicationDbContext` (in `SuperAppDataRepositories/Data/`) exposing ~36 `DbSet`s, accessed via repositories under `SuperAppDataRepositories/Repositories/`.

A small number of **stored procedures** are used for bulk operations (e.g. `sp_DeleteNodes`, `sp_DeleteWorkspace`, `sp_DeleteWorkspaceItems`, `sp_MoveWorkspaceItems`) in the K and Workspace repositories.

EF Core migrations live in `SuperAppDataRepositories/Migrations/`; ad-hoc SQL scripts live in `migrations/` and the repo root (e.g. `standardize_column_naming.sql`, `migration_remove_ktest.sql`).

## Documentation

Detailed guidelines live in [`.github/copilot-instructions.md`](.github/copilot-instructions.md) and the [`docs/`](docs/) folder:

- `PROJECT_OVERVIEW.md`, `ARCHITECTURE.md`, `CODING_STANDARDS.md`
- `DATABASE_ACCESS.md`, `EF_CORE_GUIDE.md`, `API_DESIGN.md`
- `AUTHENTICATION.md`, `ERROR_HANDLING.md`, `VALIDATION.md`
- `TESTING.md`, `SECURITY.md`, `DEPLOYMENT.md`, `TROUBLESHOOTING.md`

See also [`CLAUDE.md`](CLAUDE.md) for a quick command reference.
