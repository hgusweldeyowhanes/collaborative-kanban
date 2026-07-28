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

## What's scaffolded vs. what to build next

This repo is a working skeleton, not a finished app — intentionally, so building it out is the actual learning exercise. Already wired: auth, board creation/listing, card-move persistence + broadcast, CI, Docker.

Suggested build order for the rest of the week:
1. **`GetBoardDetailQuery`** — return a board's lists + cards (follow the `GetBoardsQuery` pattern exactly). Wire it into `BoardViewPage` with `useQuery` instead of the placeholder local state.
2. **`CreateListCommand` / `CreateCardCommand`** — same CQRS pattern again. By the third vertical slice this should feel mechanical — that repetition is the point.
3. **Board membership & authorization** — a `[Authorize]` policy or MediatR behavior that checks the current user is a member of the board being mutated, not just any authenticated user.
4. **Optimistic concurrency** — add a `RowVersion` column to `Card`, handle `DbUpdateConcurrencyException` when two users move the same card at once.
5. **Deploy for real** — API to Render or Fly.io, frontend to Vercel/Netlify, Postgres to Neon. Point CI at it with a deploy job.

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
