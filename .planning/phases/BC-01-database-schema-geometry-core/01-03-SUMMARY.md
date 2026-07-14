---
phase: BC-01-database-schema-geometry-core
plan: 03
subsystem: database
tags: [efcore, postgres, npgsql, migrations, docker]

# Dependency graph
requires:
  - phase: BC-01 (plan 01-01)
    provides: BlazorCanvas project skeleton, docker-compose.yml, appsettings.Development.json scaffolding
provides:
  - "CanvasDbContext with User and Figure entities, explicitly configured CHECK constraints, index, and table comment"
  - "Initial EF Core migration (InitialSchema), applied automatically at app startup"
  - "Live-database-verified schema: users, figures, four named CHECK constraints, ix_figures_user_id, table comment"
affects: [BC-01-04 (constraint-rejection tests), Phase 2 (login/auth writes to users table), Phase 3 (figure CRUD write path)]

# Tech tracking
tech-stack:
  added: [Microsoft.EntityFrameworkCore.Design (design-time tooling), Npgsql.EntityFrameworkCore.PostgreSQL (already referenced from 01-01)]
  patterns: ["Explicit OnModelCreating configuration — EF Core infers none of the CHECKs, index, or comment", "IDesignTimeDbContextFactory for dotnet-ef tooling before DI registration exists", "Bounded retry loop around Database.Migrate() at startup for container-not-ready races"]

key-files:
  created:
    - src/BlazorCanvas/Data/User.cs
    - src/BlazorCanvas/Data/Figure.cs
    - src/BlazorCanvas/Data/CanvasDbContext.cs
    - src/BlazorCanvas/Data/CanvasDbContextFactory.cs
    - src/BlazorCanvas/Migrations/20260714212457_InitialSchema.cs
    - src/BlazorCanvas/Migrations/20260714212457_InitialSchema.Designer.cs
    - src/BlazorCanvas/Migrations/CanvasDbContextModelSnapshot.cs
  modified:
    - src/BlazorCanvas/Program.cs
    - docker-compose.yml
    - src/BlazorCanvas/appsettings.Development.json

key-decisions:
  - "D-27 port deviation (user-approved): Docker container host-published port moved from 5432 to 5433 because a pre-existing native postgresql-x64-18 Windows service permanently occupies 5432 on this machine"
  - "Figure.Type is a plain string/text column, never a C# enum or HasConversion — preserves lowercase literal matching in the CHECK predicates (D-46)"
  - "IDesignTimeDbContextFactory added ahead of schedule (Task 2) so dotnet ef tooling works before Program.cs DI registration exists in Task 3"

patterns-established:
  - "Every structural DB guarantee (CHECKs, index, comment) is verified against pg_constraint/pg_indexes/information_schema in the live database — a green build is never accepted as proof"

requirements-completed: [DATA-02]

coverage:
  - id: D1
    description: "CanvasDbContext explicitly configures 4 named CHECK constraints, ix_figures_user_id index, table comment, cascade FK — all present in the live database"
    requirement: "DATA-02"
    verification:
      - kind: manual_procedural
        ref: "psql -h localhost -p 5433 -U postgres -d canvas -c \"select conname from pg_constraint where conrelid='figures'::regclass and contype='c'\" -> box_is_a_box, circle_is_a_circle, figures_type_is_known, line_is_a_line"
        status: pass
      - kind: manual_procedural
        ref: "psql ... -c \"select indexname from pg_indexes where tablename='figures'\" -> ix_figures_user_id"
        status: pass
      - kind: manual_procedural
        ref: "psql ... -c \"select obj_description('figures'::regclass)\" -> contains 'inscribed in'"
        status: pass
    human_judgment: false
  - id: D2
    description: "Running the app applies the InitialSchema migration automatically at startup, creating exactly users, figures, __EFMigrationsHistory (no canvases table)"
    requirement: "DATA-02"
    verification:
      - kind: manual_procedural
        ref: "dotnet run --project src/BlazorCanvas (first run) -> log shows 'Applying migration 20260714212457_InitialSchema'; psql information_schema.tables -> exactly users, figures, __EFMigrationsHistory"
        status: pass
      - kind: manual_procedural
        ref: "dotnet run --project src/BlazorCanvas (second run) -> log shows 'No migrations were applied. The database is already up to date.'"
        status: pass
    human_judgment: false
  - id: D3
    description: "figures.type column is text (never a PostgreSQL enum or int-mapped C# enum) — 7 columns exactly on figures, 3 on users"
    requirement: "DATA-02"
    verification:
      - kind: manual_procedural
        ref: "psql ... -c \"select data_type from information_schema.columns where table_name='figures' and column_name='type'\" -> text; select count(*) from pg_type where typtype='e' -> 0"
        status: pass
    human_judgment: false
  - id: D4
    description: "Docker container host port moved 5432->5433 to avoid a pre-existing native PostgreSQL 18 service; app confirmed talking to Postgres 17 (container), not 18 (native)"
    requirement: ""
    verification:
      - kind: manual_procedural
        ref: "psql -h localhost -p 5433 -U postgres -d canvas -c 'select version()' -> PostgreSQL 17.10 (Debian ...)"
        status: pass
    human_judgment: true
    rationale: "This is a deliberate, user-approved deviation from a locked decision (D-27). A human must be aware the deviation exists and that docs/DECISIONS.md D-27 text was not amended — flagged as an open follow-up requiring a conscious decision, not something automation should silently resolve."

# Metrics
duration: 15min
completed: 2026-07-14
status: complete
---

# Phase BC-01 Plan 03: Database Schema & Migrations Summary

**EF Core migration wires CanvasDbContext (User, Figure entities) to a live PostgreSQL 17 container, with all four CHECK constraints, the user_id index, and the inscribed-square table comment verified present via pg_constraint — not merely assumed from a green build.**

## Performance

- **Duration:** ~15 min (across this continuation session; Tasks 1–2 completed by a prior session)
- **Started:** 2026-07-14T21:23:57Z (Task 1 commit)
- **Completed:** 2026-07-14T21:38:04Z (Task 3 commit)
- **Tasks:** 3
- **Files modified:** 10 (7 created, 3 modified)

## Accomplishments
- `User` and `Figure` entities created with `Figure.Type` as a plain `string`/`text` column, deliberately avoiding EF's `Enum.ToString()` PascalCase pitfall (D-46)
- `CanvasDbContext.OnModelCreating` explicitly configures all four named CHECK constraints (`circle_is_a_circle`, `box_is_a_box`, `line_is_a_line`, `figures_type_is_known`), the `ix_figures_user_id` index, the cascade FK, and the inscribed-square table comment — none of which EF Core would emit on its own
- `InitialSchema` migration generated and verified line-by-line against the authoritative `CONSTRAINT-schema` DDL
- `Program.cs` registers `CanvasDbContext` against `ConnectionStrings:Canvas` and applies the migration automatically at startup with a bounded retry loop (10 attempts, 2s delay) for container-not-ready races
- Full schema presence proven against the LIVE containerized database via `pg_constraint`, `pg_indexes`, and `information_schema` — exactly the three expected tables, seven columns on `figures`, three on `users`, zero PostgreSQL enum types, identity PK on `figures.id`
- Confirmed the migration is idempotent — a second `dotnet run` logs "No migrations were applied. The database is already up to date."

## Task Commits

Each task was committed atomically:

1. **Task 1: Entities and CanvasDbContext — the three CHECKs, index, comment** - `81f88bc` (feat)
2. **Task 2: Generate initial migration; verify SQL against CONSTRAINT-schema** - `cffc906` (feat)
3. **Task 3: Register DbContext and apply migrations at startup** - `9298264` (feat)

**Plan metadata:** (this commit, docs: complete plan)

## Files Created/Modified
- `src/BlazorCanvas/Data/User.cs` - `Id`, `Username` (unique), `Password` (plaintext, D-08) — 3 columns
- `src/BlazorCanvas/Data/Figure.cs` - `Id`, `UserId`, `Type` (string), `X1`,`Y1`,`X2`,`Y2` — 7 columns
- `src/BlazorCanvas/Data/CanvasDbContext.cs` - `OnModelCreating` with 4 `HasCheckConstraint` calls, `HasIndex("ix_figures_user_id")`, `HasComment`, cascade FK
- `src/BlazorCanvas/Data/CanvasDbContextFactory.cs` - `IDesignTimeDbContextFactory` for `dotnet ef` tooling ahead of DI registration
- `src/BlazorCanvas/Migrations/20260714212457_InitialSchema.cs` + `.Designer.cs` + `CanvasDbContextModelSnapshot.cs` - the initial migration
- `src/BlazorCanvas/Program.cs` - registers `CanvasDbContext`, applies `Database.Migrate()` at startup with retry loop
- `docker-compose.yml` - host port changed `5432:5432` → `5433:5432` (see deviation below)
- `src/BlazorCanvas/appsettings.Development.json` - connection string `Port=5432` → `Port=5433` (see deviation below)

## Decisions Made
- `Figure.Type` mapped as plain `text`, no `HasConversion`, no C# enum in the entity — the geometry core's `FigureTypeNames.ToDbValue` (plan 01-02) is the only source of the four lowercase literals
- `IDesignTimeDbContextFactory` added in Task 2 (not scheduled explicitly in the plan, but necessary) so `dotnet ef` commands work before Program.cs gains DI registration in Task 3
- Host port for the Postgres container moved from 5432 to 5433 — see Deviations below

## Deviations from Plan

### User-Approved Architectural Deviation (Rule 4 — checkpoint, not auto-fixed)

**1. [Rule 4 - Checkpoint/User Decision] Docker container host port moved from 5432 to 5433**

- **Found during:** Task 3 (register DbContext, apply migrations at startup)
- **Issue:** `docs/DECISIONS.md` D-27 locks the Postgres connection at host port **5432**. On this machine, a pre-existing native Windows service `postgresql-x64-18` (PostgreSQL 18, `Automatic` start, `Running`) permanently occupies port 5432. Connections to `localhost:5432` were silently landing on the native PostgreSQL 18 instance instead of the project's Docker container (`canvas-postgres`, Postgres 17) — surfacing as Npgsql auth error `28P01` because the native instance doesn't have the `postgres`/`postgres` role configured the way the container does. This is a host-machine conflict outside the plan's control, not a bug in the code.
- **STOP condition triggered:** this is a locked decision (D-27) requiring structural modification (which server the whole application talks to) — Rule 4 applied, execution paused at a `checkpoint:human-action`, and the user was asked to choose between: (a) move the container's host-published port, (b) stop/reconfigure the native service, or (c) something else.
- **User's decision:** move the Docker container to host port 5433. The native `postgresql-x64-18` service was explicitly left untouched — still `Running`/`Automatic`, nothing under `C:\Program Files\PostgreSQL\` was modified.
- **Fix applied:**
  - `docker-compose.yml`: `ports: ["5432:5432"]` → `ports: ["5433:5432"]` (container still listens on 5432 *internally*; only the host-published port changed)
  - `src/BlazorCanvas/appsettings.Development.json`: connection string `Port=5432` → `Port=5433`
  - Container recreated via `docker compose up -d` (image, env, healthcheck, and the named volume `canvas-pgdata` all unchanged and preserved — `docker compose down -v` was never run)
- **Verification:** `psql -h localhost -p 5433 -U postgres -d canvas -c "select version()"` returned `PostgreSQL 17.10 (Debian 17.10-1.pgdg13+1)...` — confirming the app now talks to the intended containerized Postgres 17, not the native Postgres 18 instance.
- **Files modified:** `docker-compose.yml`, `src/BlazorCanvas/appsettings.Development.json`
- **Committed in:** `9298264` (Task 3 commit)

**Everything else in D-27 remains literal and unchanged:** Postgres **17**, database `canvas`, user/password `postgres`/`postgres`, and the named volume `canvas-pgdata` for persistence.

**⚠ OPEN FOLLOW-UP FOR THE USER:** `docs/DECISIONS.md` D-27's text still says host port **5432**. This summary documents the approved runtime deviation, but the ADR document itself was **not** edited — amending a locked decision is the user's call, not the executor's. If this deviation should be permanent (e.g., committed to the team's dev setup), D-27 needs an explicit amendment by the user. If it's a local-machine-only workaround, no amendment is needed, but the discrepancy between D-27's text and this machine's `docker-compose.yml` should stay visible until resolved.

---

**Total deviations:** 1 (Rule 4 — user-approved architectural/decision-level change, not auto-fixed)
**Impact on plan:** No scope creep. The application-level schema (tables, CHECKs, index, comment) exactly matches the plan and `CONSTRAINT-schema`. Only the host-machine port binding changed, and only because of an environmental conflict that had no code-level fix.

## Issues Encountered
- `psql` was not on `PATH` in the Bash environment; located and used the native PostgreSQL 18 install's `psql.exe` client (`C:\Program Files\PostgreSQL\18\bin\psql.exe`) to connect to the container on port 5433. The client version connecting does not affect which server it talks to — this is purely a CLI-tooling convenience, not a server-side change.

## User Setup Required

None - no external service configuration required beyond what plan 01-01 already established (Docker Desktop running). The port deviation is already applied and verified in this repository; no further action needed unless the user decides to amend D-27's documented port.

## Next Phase Readiness
- Schema is live, migrated, and verified against `pg_constraint` — ready for plan 01-04's constraint-rejection tests (which will assert the CHECKs actually reject illegal rows, not just that they exist)
- `Figure.Type`'s string/text contract is ready for the geometry core (plan 01-02, already complete) to hand off `FigureTypeNames.ToDbValue` literals in Phase 3's write path
- The `users` table exists with the unique username constraint and plaintext password column, ready for Phase 2's login flow (no login `.razor` component or auth service was added here — correctly out of scope per this plan's prohibitions)
- **Carry-forward for whoever next touches `docker-compose.yml` or onboarding docs:** the host port is 5433, not the D-27-documented 5432, on this development machine. Any README/setup instructions for other machines should either state the port is machine-dependent or the user should decide whether to make 5433 the new project-wide standard via a D-27 amendment.

---
*Phase: BC-01-database-schema-geometry-core*
*Completed: 2026-07-14*

## Self-Check: PASSED

All 8 created/modified files confirmed present on disk; all 3 task commits (`81f88bc`, `cffc906`, `9298264`) confirmed present in git history.
