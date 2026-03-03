# K1.Idea Backend Design

**Date:** 2026-03-03
**Status:** Approved
**Spec version:** PROJECTSPEC.md v1.1.0

---

## Architecture

4-layer Clean Architecture (.NET 9 solution):

```
K1Idea.sln
├── src/
│   ├── K1Idea.Domain         — entities, enums, interfaces (no external deps)
│   ├── K1Idea.Application    — MediatR CQRS, FluentValidation, DTOs, pagination
│   ├── K1Idea.Infrastructure — Dapper repos, UoW, JwtService, DbUp migrations
│   └── K1Idea.API            — Hot Chocolate v14, middleware, Program.cs
└── tests/
    ├── K1Idea.Domain.Tests
    ├── K1Idea.Application.Tests
    └── K1Idea.API.IntegrationTests
```

---

## Domain Layer

Pure C# — `Npgsql` only for `IUnitOfWork` contract types.

**Entities:** `Tenant`, `Organization`, `BusinessUnit`, `User`, `OrgUser`, `Ticket`, `TicketBusinessUnit`, `Comment`, `RefreshToken`

**Enums:** `UserRole` (Admin/Member/Viewer), `TicketType` (Idea/Initiative/Project/Task), `TicketStatus` (Backlog/InProgress/InReview/Done/Cancelled), `TicketPriority` (Low/Medium/High/Critical)

**Interfaces:**
- `IUnitOfWork` — `NpgsqlConnection Connection`, `NpgsqlTransaction? Transaction`, `BeginAsync`, `CommitAsync`, `RollbackAsync`
- `ITicketRepository`, `ICommentRepository`, `IUserRepository`, `IOrgRepository`, `IBusinessUnitRepository`
- `IClock` — abstracts `DateTimeOffset.UtcNow` for testability

---

## Application Layer

MediatR CQRS + FluentValidation pipeline behavior (`ValidationBehavior`).

| Feature | Commands | Queries |
|---|---|---|
| Auth | Register, Login, RefreshToken, SelectOrganization | — |
| Orgs | — | ListOrganizations, ListBusinessUnits |
| Tickets | CreateTicket, UpdateTicket, DeleteTicket, ShareTicket | GetTicketById, ListTickets |
| Comments | AddComment, DeleteComment | ListComments |

All command handlers run inside a `IUnitOfWork` transaction. Cursor pagination: `base64("{created_at:o}|{id}")`.

---

## Infrastructure Layer

- **`NpgsqlConnectionFactory`** — creates `NpgsqlConnection` from env-var connection string
- **`UnitOfWork`** — lazy connection open, `BeginAsync` starts transaction, safe dispose
- **Repositories** — Dapper over `IUnitOfWork.Connection`, all async with `CancellationToken`, `ConfigureAwait(false)`
- **`TicketSqlBuilder`** — parameterized SELECT/COUNT for filter/sort/cursor-paging; column whitelist enforced; no string concat with user input
- **`JwtService`** — HS256 access tokens (15 min) with `sub`/`tenant_id`/`org_id` claims; refresh token rotation (7 days)
- **`PasswordHasher`** — bcrypt cost 12 via `BCrypt.Net-Next`
- **DbUp** — SQL scripts under `Migrations/Scripts/` in lexical order, run on startup in Development

---

## API Layer

Hot Chocolate v14 code-first GraphQL on `/graphql`.

- **Resolvers** are single-line `mediator.Send(...)` calls — no business logic
- **`CurrentUserMiddleware`** — populates `GlobalState("CurrentUser")` from JWT
- **`TenantOrgContextMiddleware`** — sets scoped `TenantContext`/`OrgContext` from JWT claims; rejects missing claims except on auth mutations
- **DataLoaders** — source-generation `[DataLoader]` on `UserByIdDataLoader`, `BusinessUnitByIdDataLoader`, `TicketByIdDataLoader` — batch load to prevent N+1
- **Subscriptions** — in-memory `ITopicEventSender`; `commentAdded(ticketId)` fires when `AddComment` succeeds
- **`GraphQLErrorFilter`** — maps `NotFoundException` → `NOT_FOUND`, `UnauthorizedException` → `AUTH_NOT_AUTHORIZED`, `ValidationException` → `VALIDATION_ERROR`
- **Banana Cake Pop** — Development only

---

## Database

PostgreSQL 16 with the prescribed DDL (see PROJECTSPEC.md §5.2):
- `gen_random_uuid()` for all PKs
- `enforce_ticket_hierarchy()` trigger for hierarchy rules
- `ticket_business_units` row inserted for owner BU on every ticket creation
- All timestamps `timestamptz` UTC; soft-delete via `deleted_at`
- Prescribed indexes

---

## Implementation Order

1. Solution scaffold (`.sln`, `.csproj`, `Directory.Build.props`, docker-compose)
2. Domain entities + interfaces + enums → Domain.Tests
3. Infrastructure: DbUp + DDL migration scripts + UoW + repositories
4. Application — Auth feature + validators → Application.Tests
5. API — Auth mutations + middleware + JWT setup → Integration tests
6. Application — Tickets (CRUD + Share + List) + validators → Application.Tests
7. API — Ticket queries/mutations + DataLoaders → Integration tests
8. Application + API — Comments + subscription → Integration tests

---

## Key Decisions

| Decision | Choice | Reason |
|---|---|---|
| DataLoader style | Source-gen `[DataLoader]` | Modern HC14 approach, less boilerplate |
| Subscription transport | In-memory | Zero extra deps, correct for single-node v1 |
| Test strategy | Alongside production code | TDD discipline, spec requirement |
| ORM | Dapper (prescribed) | No EF Core anywhere |
| Migration tool | DbUp (prescribed) | SQL scripts in lexical order |
