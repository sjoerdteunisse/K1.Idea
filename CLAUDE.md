# CLAUDE.md — K1.Idea Backend

## Project Overview

K1.Idea is an ASP.NET Core GraphQL backend for idea and ticket management. It uses a 4-layer Clean Architecture with CQRS, PostgreSQL, and Hot Chocolate v14. The API is fully GraphQL — there are no REST endpoints.

---

## Technology Stack

| Concern | Choice |
|---|---|
| Language / Runtime | C# / .NET 10.0 |
| API protocol | GraphQL via Hot Chocolate v14 |
| CQRS dispatcher | MediatR v12 |
| Validation | FluentValidation v11 |
| ORM | Dapper v2 (no Entity Framework anywhere) |
| Database | PostgreSQL 16 |
| DB driver | Npgsql v8 |
| Migrations | DbUp v5 (SQL scripts, lexical order) |
| Authentication | JWT Bearer (HS256) + BCrypt password hashing |
| Testing | xUnit v2, NSubstitute v5, FluentAssertions v6, Testcontainers |

---

## Solution Structure

```
K1.Idea/
├── Directory.Build.props          # Global MSBuild config (net10.0, nullable, warnings-as-errors)
├── Directory.Packages.props       # Centralized NuGet version management
├── K1Idea.slnx                    # Solution file
├── docker-compose.yml             # Local dev: PostgreSQL 16 + API
├── docs/plans/                    # Architecture design documents
└── src/
    ├── K1Idea.Domain/             # Entities, enums, repository interfaces (no external deps)
    ├── K1Idea.Application/        # MediatR handlers, validators, DTOs, pagination
    ├── K1Idea.Infrastructure/     # Dapper repos, UnitOfWork, JWT, DbUp migrations
    └── K1Idea.API/                # Hot Chocolate GraphQL, middleware, Program.cs
tests/
    ├── K1Idea.Domain.Tests/
    ├── K1Idea.Application.Tests/
    └── K1Idea.API.IntegrationTests/
```

---

## Architecture — Clean Architecture (4 Layers)

### Dependency direction

```
API → Application → Domain
Infrastructure → Domain
API → Infrastructure (DI registration only)
```

### Domain (`K1Idea.Domain/`)

- Pure C# with no external NuGet dependencies (Npgsql types only for `IUnitOfWork` contract).
- Contains: entities, enums, repository interfaces, `IUnitOfWork`, `IClock`.
- **Never add business logic outside this layer.**

Key entities: `Tenant`, `Organization`, `BusinessUnit`, `User`, `OrgUser`, `Ticket`, `TicketBusinessUnit`, `Comment`, `RefreshToken`

Key enums:
- `UserRole`: Admin / Member / Viewer
- `TicketType`: Idea / Initiative / Project / Task
- `TicketStatus`: Backlog / InProgress / InReview / Done / Cancelled
- `TicketPriority`: Low / Medium / High / Critical

### Application (`K1Idea.Application/`)

- MediatR CQRS: one class per command/query, one handler per command/query.
- `ValidationBehavior` pipeline runs FluentValidation automatically before every handler.
- All command handlers run inside a `IUnitOfWork` transaction.
- Features:

| Feature | Commands | Queries |
|---|---|---|
| Auth | Register, Login, RefreshToken, SelectOrganization | — |
| Orgs | — | ListOrganizations, ListBusinessUnits |
| Tickets | CreateTicket, UpdateTicket, DeleteTicket, ShareTicket | GetTicketById, ListTickets |
| Comments | AddComment, DeleteComment | ListComments |

- Cursor pagination encoding: `base64("{created_at:o}|{id}")`
- Custom exceptions: `NotFoundException`, `UnauthorizedException`, `ForbiddenException`

### Infrastructure (`K1Idea.Infrastructure/`)

- `NpgsqlConnectionFactory` — creates connections from the `ConnectionStrings__Default` environment variable.
- `UnitOfWork` — lazy connection open; `BeginAsync` starts a transaction.
- Repositories — Dapper queries, all async with `CancellationToken` and `ConfigureAwait(false)`.
- `TicketSqlBuilder` — parameterized SELECT/COUNT for filter/sort/cursor-paging; column whitelist enforced; **never concatenate user input into SQL strings**.
- `JwtService` — HS256 tokens; access tokens expire in 15 minutes, refresh tokens in 7 days; claims: `sub`, `tenant_id`, `org_id`.
- `PasswordHasher` — BCrypt cost 12.
- `DbUpRunner` — runs embedded SQL migration scripts from `Migrations/Scripts/` in lexical order on startup (Development only).

### API (`K1Idea.API/`)

- Hot Chocolate v14 code-first GraphQL, endpoint `/graphql`.
- **Resolvers must be single-line `mediator.Send(...)` calls — zero business logic in resolvers.**
- `CurrentUserMiddleware` — extracts JWT claims into `GlobalState("CurrentUser")`.
- `TenantOrgContextMiddleware` — populates scoped `TenantContext`/`OrgContext`; rejects requests missing claims except on auth mutations.
- DataLoaders use source-generation `[DataLoader]` attribute: `UserByIdDataLoader`, `BusinessUnitByIdDataLoader`, `TicketByIdDataLoader`.
- Subscriptions: in-memory `ITopicEventSender`; `commentAdded(ticketId)` fires on successful `AddComment`.
- `GraphQLErrorFilter` maps domain exceptions to GraphQL error codes:
  - `NotFoundException` → `NOT_FOUND`
  - `UnauthorizedException` → `AUTH_NOT_AUTHORIZED`
  - `ForbiddenException` → `FORBIDDEN`
  - `ValidationException` → `VALIDATION_ERROR`
- Banana Cake Pop playground available in Development only.

---

## Local Development

### Prerequisites

- Docker + Docker Compose
- .NET 10 SDK

### Start the stack

```bash
docker compose up
```

This starts PostgreSQL 16 (port `5432`) and the API (port `8080`). The API waits for a healthy Postgres before starting, then runs DbUp migrations automatically.

### Connection details (local dev)

| Setting | Value |
|---|---|
| Database | `k1idea` |
| User | `k1idea` |
| Password | `k1idea_dev` |
| API URL | `http://localhost:8080/graphql` |

### Run without Docker (API only)

```bash
# Start Postgres first via Docker, then:
cd src/K1Idea.API
dotnet run
# HTTP: http://localhost:5107  HTTPS: https://localhost:7013
```

Environment variables needed when running outside Docker:
```
ConnectionStrings__Default=Host=localhost;Port=5432;Database=k1idea;Username=k1idea;Password=k1idea_dev
JWT__Secret=<min-256-bit-key>
JWT__Issuer=k1idea-dev
JWT__Audience=k1idea-client
```

---

## Build

```bash
# Build entire solution
dotnet build

# Build specific project
dotnet build src/K1Idea.API
```

All projects share global settings from `Directory.Build.props`:
- Target: `net10.0`
- Nullable reference types: **enabled** — all warnings are treated as errors.
- Code style analysis enforced in build.

NuGet versions are centrally managed in `Directory.Packages.props` — do not specify versions inside individual `.csproj` files.

---

## Testing

```bash
# Run all tests
dotnet test

# Run specific project
dotnet test tests/K1Idea.Application.Tests

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

### Test conventions

- Unit tests use NSubstitute for mocking and FluentAssertions for assertions.
- Integration tests use `Testcontainers.PostgreSql` — a real PostgreSQL container spins up per test collection.
- Test files end with `Tests.cs`.
- Global usings are defined per test project to keep test files DRY.
- Test class names follow `{Subject}Tests` pattern (e.g., `RegisterCommandHandlerTests`).

---

## Code Conventions

### C# style

- All classes are `sealed` unless inheritance is required.
- Use `record` types for DTOs, commands, and queries.
- Use `init`-only properties for entities.
- `async`/`await` throughout — always pass and forward `CancellationToken`.
- Always call `ConfigureAwait(false)` on every awaited call in library/infrastructure code.
- Warnings are errors — fix them, don't suppress them.

### Naming patterns

| Artifact | Pattern | Example |
|---|---|---|
| Command | `{Resource}{Action}Command` | `CreateTicketCommand` |
| Query | `{Action}{Resource}Query` | `ListTicketsQuery` |
| Handler | `{CommandOrQuery}Handler` | `CreateTicketCommandHandler` |
| Validator | `{CommandOrQuery}Validator` | `CreateTicketCommandValidator` |
| Repository interface | `I{Resource}Repository` | `ITicketRepository` |
| Repository impl | `{Resource}Repository` | `TicketRepository` |
| Middleware | `{Feature}Middleware` | `CurrentUserMiddleware` |

### Database conventions

- Column names use `snake_case`.
- All primary keys use `gen_random_uuid()`.
- All timestamps use `TIMESTAMPTZ` (UTC-stored).
- Soft deletes use `deleted_at` column (nullable).
- Foreign key cascades on delete where appropriate.
- Enum values enforced via `CHECK` constraints.
- The `enforce_ticket_hierarchy()` trigger enforces parent-child rules:
  - Ideas — no parent
  - Initiatives — parent must be an Idea
  - Projects — parent must be an Initiative
  - Tasks — parent must be a Project
- Indexes exist on `tenant_id`, `org_id`, and join columns for performance.

### GraphQL conventions

- Use type extensions for entity fields resolved via DataLoader.
- Use `Input` suffix for mutation input types.
- Use cursor-based pagination (`Connection<T>`) — never offset pagination.
- Always batch related entity lookups through DataLoaders to prevent N+1 queries.

### Security rules

- **Never concatenate user input into SQL** — use parameterized queries or the `TicketSqlBuilder` column whitelist.
- **Never log JWT secrets or passwords.**
- All data access must filter by `tenant_id` and `org_id` — no cross-tenant data leaks.
- Scope enforcement uses `visible_business_units` on ticket queries.

---

## Multi-Tenancy

The system is multi-tenant. Every authenticated request carries `tenant_id` and `org_id` in the JWT and in the scoped `TenantContext`/`OrgContext` services. All repository queries must include these in their `WHERE` clauses. The middleware rejects requests with missing claims (except auth mutations).

---

## Adding New Features

Follow this pattern for a new feature (e.g., "Labels"):

1. **Domain** — add entity, enum, and repository interface.
2. **Infrastructure** — add repository implementation and a new DbUp SQL migration script (lexical filename, e.g., `0005_add_labels.sql`).
3. **Application** — add command/query records, handlers, and FluentValidation validators.
4. **API** — add Hot Chocolate type, resolver (single `mediator.Send` call), and wire up in `Program.cs`.
5. **Tests** — add unit tests for handler and validator; add integration test if a new GraphQL operation is exposed.

---

## Key Design Decisions

| Decision | Choice | Reason |
|---|---|---|
| ORM | Dapper only | Prescribed; no Entity Framework |
| Migration tool | DbUp | SQL scripts in lexical order |
| GraphQL library | Hot Chocolate v14 | Code-first, source-gen DataLoaders |
| Subscription transport | In-memory | Zero extra deps, correct for single-node |
| DataLoader style | Source-gen `[DataLoader]` | Modern HC14, less boilerplate |
| Test database | Testcontainers | Real Postgres, no mocks for SQL |
| Pagination | Cursor-based | Consistent results under concurrent writes |
