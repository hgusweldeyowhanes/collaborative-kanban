# TaskFlow

A real-time collaborative Kanban board — built to practice senior-level full-stack patterns: Clean Architecture, CQRS, real-time sync, JWT auth with refresh tokens, and a deployable Docker setup.

## Stack

- **Backend**: ASP.NET Core 8, EF Core + PostgreSQL, MediatR (CQRS), FluentValidation, SignalR, JWT auth, Serilog
- **Frontend**: React + TypeScript (Vite), TanStack Query, Zustand, dnd-kit, SignalR client, Tailwind
- **Infra**: Docker Compose, GitHub Actions CI

## Architecture

The backend follows Clean Architecture with four layers, each only depending on the one "inward" of it:

```
TaskFlow.Domain          <- entities, no dependencies
TaskFlow.Application      <- CQRS commands/queries, depends only on Domain
                              (talks to persistence through IApplicationDbContext,
                               an interface it owns — never EF Core directly)
TaskFlow.Infrastructure    <- EF Core, JWT, implements Application's interfaces
TaskFlow.Presentation       <- controllers, SignalR hub, DI wiring, HTTP concerns
```

Why this matters: `TaskFlow.Application` has zero references to EF Core or ASP.NET Core. You can unit test every command/query handler with an in-memory database and mocked services — see `backend/tests/TaskFlow.Application.Tests`.

Every command/query goes through a MediatR pipeline behavior (`ValidationBehavior`) that automatically runs its FluentValidation validator before the handler executes, so handlers never have to remember to call `Validate()` manually.

## Real-time sync

`BoardHub` (SignalR) groups connected clients by board ID. When `MoveCardCommandHandler` persists a card move, it calls `IBoardNotifier.NotifyCardMovedAsync`, which broadcasts to everyone in that board's group. The React `useSignalR` hook joins the group on mount and updates local state on the `CardMoved` event — no polling, no manual refresh.

## Running locally

### Option A — Docker Compose (fastest)

```bash
docker compose up --build
```

- Frontend: http://localhost:5173
- API: http://localhost:5000 (Swagger at `/swagger`)
- Postgres: localhost:5433

> Note: `VITE_API_URL` is baked in at build time by Vite, so changing it in `docker-compose.yml` alone won't affect an already-built frontend image — rebuild with `docker compose up --build` after changing it, or switch to runtime env injection (a good exercise: read `window.__ENV__` set by an nginx `envsubst` entrypoint instead).

### Option B — Run each side natively

**Backend**
```bash
cd backend
dotnet user-secrets set "Jwt:Secret" "some-long-random-string-at-least-32-chars" --project src/TaskFlow.Presentation
# start a local Postgres, or run just the postgres service:
docker compose up postgres -d
dotnet run --project src/TaskFlow.Presentation
```

**Frontend**
```bash
cd frontend
npm install
npm run dev
```

## Running tests

```bash
cd backend
dotnet test
```

## What's included

Working features beyond the original skeleton:

1. **Card detail** — click a card to edit title/description, delete
2. **Assignments** — assign any board member
3. **Board members** — invite by registered email; mutations require Editor+; invites require Admin
4. **Due dates + labels** — board labels with colors; due date on cards
5. **Comments** — threaded discussion on each card
6. **Within-list reorder** — drag computes real `newOrder` (not always `0`)
7. **Realtime `BoardChanged`** — create/update/delete/invite refreshes other clients

Suggested next steps: optimistic concurrency (`RowVersion` on `Card`), deploy to Render/Vercel/Neon.

## Project layout

```
backend/
  src/
    TaskFlow.Domain/
    TaskFlow.Application/
    TaskFlow.Infrastructure/
    TaskFlow.Presentation/
  tests/
    TaskFlow.Application.Tests/
frontend/
  src/
    api/          axios client + endpoint functions
    components/   pages
    hooks/        useSignalR
    store/        zustand auth store
    types/
docker-compose.yml
.github/workflows/ci.yml
```
