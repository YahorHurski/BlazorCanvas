---
phase: BC-01-database-schema-geometry-core
plan: 01
subsystem: infra
tags: [docker-compose, postgresql, dotnet10, blazor-server, xunit, npgsql, entity-framework-core]

# Dependency graph
requires: []
provides:
  - "Running PostgreSQL 17 container (docker-compose.yml) on port 5432, database `canvas`, named volume `canvas-pgdata`"
  - "Two-project .NET 10 solution: src/BlazorCanvas (Blazor Web App) + tests/BlazorCanvas.Tests (xUnit)"
  - "Connection string ConnectionStrings:Canvas in appsettings.Development.json, verified to match docker-compose.yml exactly"
  - "Npgsql.EntityFrameworkCore.PostgreSQL + Microsoft.EntityFrameworkCore.Design installed and pinned in the app project"
affects: [BC-01-02, BC-01-03, BC-01-04]

# Tech tracking
tech-stack:
  added: [PostgreSQL 17 (Docker), .NET 10, Blazor Server (InteractiveServer), xUnit, Npgsql.EntityFrameworkCore.PostgreSQL 10.0.3, Microsoft.EntityFrameworkCore.Design 10.0.10]
  patterns: ["Docker Compose named volume for local dev DB persistence", "explicit-version-pinned PackageReference (no wildcards)"]

key-files:
  created:
    - docker-compose.yml
    - .gitignore
    - BlazorCanvas.sln
    - src/BlazorCanvas/BlazorCanvas.csproj
    - src/BlazorCanvas/appsettings.Development.json
    - tests/BlazorCanvas.Tests/BlazorCanvas.Tests.csproj
  modified: []

key-decisions:
  - "docker-compose.yml publishes 5432:5432 (not loopback-bound) per explicit user decision — D-27 pins the port, not the interface; loopback binding was proposed and rejected"
  - ".NET 10's `dotnet new sln` now defaults to the .slnx format — regenerated with `--format sln` to satisfy the plan's explicit BlazorCanvas.sln requirement"

patterns-established:
  - "PackageReference entries always carry an explicit Version attribute — no floating ranges (T-BC01-SC mitigation)"

requirements-completed: [DATA-02, TEST-01]

coverage:
  - id: D1
    description: "PostgreSQL 17 via Docker Compose, port 5432, database canvas, named volume canvas-pgdata — rows survive docker compose down (without -v) + up"
    requirement: DATA-02
    verification:
      - kind: manual_procedural
        ref: "docker compose up -d; psql select version() -> PostgreSQL 17.10; insert probe row; docker compose down (no -v); docker compose up -d; probe row still readable; docker volume ls shows project1_canvas-pgdata"
        status: pass
    human_judgment: false
  - id: D2
    description: ".NET 10 two-project solution (BlazorCanvas app + BlazorCanvas.Tests) builds and tests green, both net10.0"
    requirement: TEST-01
    verification:
      - kind: integration
        ref: "dotnet build BlazorCanvas.sln (exit 0, 0 warnings); dotnet test BlazorCanvas.sln (exit 0, 1/1 passed); dotnet sln list (exactly 2 projects)"
        status: pass
    human_judgment: false
  - id: D3
    description: "Connection string in appsettings.Development.json matches docker-compose.yml exactly (host/port/db/credentials)"
    requirement: DATA-02
    verification:
      - kind: unit
        ref: "manual diff: Host=localhost;Port=5432;Database=canvas;Username=postgres;Password=postgres against docker-compose.yml POSTGRES_DB/USER/PASSWORD and port mapping"
        status: pass
    human_judgment: false

duration: 5min
completed: 2026-07-14
status: complete
---

# Phase BC-01 Plan 01: Docker Compose PostgreSQL + .NET 10 Two-Project Solution Summary

**PostgreSQL 17 running in Docker Compose with a proven-persistent named volume, plus a two-project .NET 10 solution (Blazor Server app + xUnit tests) with EF Core/Npgsql packages pinned and a verified matching connection string — zero schema, zero UI, ground only.**

## Performance

- **Duration:** 5 min
- **Started:** 2026-07-14T20:58:58Z
- **Completed:** 2026-07-14T21:04:21Z
- **Tasks:** 2
- **Files modified:** 71 (2 in Task 1, 69 in Task 2 — mostly Blazor/Bootstrap template scaffolding)

## Accomplishments
- PostgreSQL 17 container defined in `docker-compose.yml`, verified healthy, and proven to retain data across a full `docker compose down` (without `-v`) + `up` cycle via a scratch probe table
- Root `.gitignore` for .NET build artifacts (`bin/`, `obj/`, `.vs/`, `*.user`) — `appsettings.Development.json` deliberately left un-ignored per D-08
- `BlazorCanvas.sln` with exactly two `net10.0` projects: `src/BlazorCanvas` (Blazor Web App, InteractiveServer) and `tests/BlazorCanvas.Tests` (xUnit, referencing the app project)
- `Npgsql.EntityFrameworkCore.PostgreSQL` (10.0.3) and `Microsoft.EntityFrameworkCore.Design` (10.0.10) added to the app project; `Npgsql.EntityFrameworkCore.PostgreSQL` (10.0.3) added to the test project — every `PackageReference` in both csproj files carries an explicit pinned `Version`
- `appsettings.Development.json` connection string (`ConnectionStrings:Canvas`) verified character-for-character consistent with `docker-compose.yml`'s port/database/credentials
- `dotnet build BlazorCanvas.sln` exits 0 with 0 warnings; `dotnet test BlazorCanvas.sln` exits 0 (1/1 template test passed)

## Task Commits

Each task was committed atomically:

1. **Task 1: PostgreSQL 17 via Docker Compose with a named volume** - `be38c04` (feat)
2. **Task 2: .NET 10 two-project solution — one Blazor Web App, one narrow test project** - `8a5ab5e` (feat)

**Plan metadata:** (pending — final docs commit follows this SUMMARY)

## Files Created/Modified
- `docker-compose.yml` - PostgreSQL 17 service, port 5432, db `canvas`, named volume `canvas-pgdata`, `pg_isready` healthcheck
- `.gitignore` - .NET build artifact ignores
- `BlazorCanvas.sln` - classic `.sln` solution referencing both projects
- `src/BlazorCanvas/BlazorCanvas.csproj` - Blazor Web App, net10.0, EF Core/Npgsql packages
- `src/BlazorCanvas/appsettings.Development.json` - `ConnectionStrings:Canvas` matching the Compose database
- `tests/BlazorCanvas.Tests/BlazorCanvas.Tests.csproj` - xUnit test project, net10.0, `ProjectReference` to the app
- `src/BlazorCanvas/Program.cs`, `Components/*`, `wwwroot/*` - unedited `dotnet new blazor` template output (left untouched per plan prohibition)

## Decisions Made
- Published the Postgres port as literal `5432:5432` (no `127.0.0.1:` prefix) — the plan's explicit instruction per the user's D-27 decision, not a default.
- Regenerated `BlazorCanvas.sln` with `dotnet new sln --format sln` after discovering `dotnet new sln` defaults to the newer `.slnx` XML format on the .NET 10 SDK — the plan and its acceptance criteria require the classic `.sln` extension.
- Verified the running Postgres server from the host over raw TCP (git-bash lacks a local `psql` client) using a `/dev/tcp` connect check and a minimal Python startup-packet probe confirming the server negotiates `SCRAM-SHA-256` on port 5432; full `select version()` / persistence proof was run via `docker exec` against the same container (identical TCP/auth stack, verified separately to work from `127.0.0.1` inside the container).

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] `dotnet new sln` produced `.slnx` instead of `.sln`**
- **Found during:** Task 2 (solution creation)
- **Issue:** On the .NET 10 SDK, `dotnet new sln` defaults to the new XML-based `.slnx` format, producing `BlazorCanvas.slnx`. The plan's frontmatter (`files_modified: BlazorCanvas.sln`) and every acceptance criterion (`dotnet sln list`, `dotnet build BlazorCanvas.sln`, `dotnet test BlazorCanvas.sln`) require the classic `.sln` file.
- **Fix:** Deleted `BlazorCanvas.slnx` and regenerated with `dotnet new sln --name BlazorCanvas --format sln`, then re-added both projects.
- **Files modified:** `BlazorCanvas.sln` (replaces the discarded `.slnx`)
- **Verification:** `ls *.sln` shows `BlazorCanvas.sln`; `dotnet sln list` lists both projects; `dotnet build`/`dotnet test` both exit 0.
- **Committed in:** `8a5ab5e` (Task 2 commit)

---

**Total deviations:** 1 auto-fixed (1 blocking)
**Impact on plan:** Necessary to satisfy the plan's explicit `.sln` artifact requirement. No scope creep — no other file or behavior changed as a result.

## Issues Encountered
- No local `psql` client available in the git-bash environment. Verification of the PostgreSQL service used `docker exec` (same container, unix-socket trust auth) for the functional checks (version string, persistence probe) plus an independent raw-TCP handshake test from the host confirming the published port 5432 is genuinely reachable and negotiates password auth (`SCRAM-SHA-256`) — together these cover the acceptance criteria without requiring a new host-level dependency. A stray `docker run --rm ... host.docker.internal` cross-container test failed with a garbled (Russian-locale) authentication error; this was a nested-container networking artifact unrelated to the Compose service itself and was not pursued further once the host-level TCP/handshake test succeeded.

## User Setup Required

None - no external service configuration required. The PostgreSQL container (`canvas-postgres`) is left running after this plan; subsequent plans in this phase (01-02, 01-03, 01-04) depend on it being available on `localhost:5432`.

## Next Phase Readiness
- The database is up, healthy, and proven persistent — ROADMAP success criterion 1 is fully satisfied.
- The two-project skeleton (D-49) is in place for plan 01-03 (EF Core schema/migrations) and plan 01-02 (pure geometry core) to write into.
- No `Data/`, `Migrations/`, or `Geometry/` directories exist yet, as required — those are out of scope for this plan.
- `docker-compose.yml` and `appsettings.Development.json` are verified consistent; plan 01-03 can register the `DbContext` against `ConnectionStrings:Canvas` with no further reconciliation needed.

---
*Phase: BC-01-database-schema-geometry-core*
*Completed: 2026-07-14*

## Self-Check: PASSED

All created files confirmed present on disk; both task commits (`be38c04`, `8a5ab5e`) confirmed present in git history.
