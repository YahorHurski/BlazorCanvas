---
phase: BC-01-database-schema-geometry-core
plan: 04
subsystem: database
tags: [xunit, postgres, npgsql, efcore, integration-testing, docker]

# Dependency graph
requires:
  - phase: BC-01-database-schema-geometry-core (plan 02)
    provides: "Pure C# geometry core: FigureType, FigureTypeNames, Box, MinSizeGuard.IsDrawable"
  - phase: BC-01-database-schema-geometry-core (plan 03)
    provides: "CanvasDbContext with the four named CHECK constraints, ix_figures_user_id, table comment, live migrated schema on the Compose container (port 5433)"
provides:
  - "Live-database-verified proof that PostgreSQL itself refuses an illegal row (circle_is_a_circle, box_is_a_box, line_is_a_line, figures_type_is_known each reject their target case with SqlState 23514 and the correct ConstraintName)"
  - "Live-database-verified proof that MinSizeGuard.IsDrawable and the CHECK constraints agree exactly, in both directions, over a 32-case matrix (D-50)"
  - "Live-database-verified proof that the live schema's information_schema/pg_catalog shape matches CONSTRAINT-schema exactly"
  - "Live-database-verified proof that figures written via EF Core survive a real 'docker compose down' (no -v) + 'up -d' container teardown, with identical ids and ORDER BY id sequence"
affects: [phase-3-drawing, phase-4-drag, phase-5-sync]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Every illegal-row / matrix test wraps its INSERT attempt in its own transaction that is deliberately never committed — proves the DB verdict without ever leaving a row behind, regardless of pass/fail"
    - "DatabaseFixture fails loudly (throws from IAsyncLifetime.InitializeAsync) when PostgreSQL is unreachable, rather than silently skipping the suite"
    - "The volume-persistence proof shells out to the real 'docker compose down'/'up -d --wait' from inside a live xUnit test (Process.Start), rather than as an external manual script — this keeps the proof repeatable via a single 'dotnet test BlazorCanvas.sln' invocation"

key-files:
  created:
    - tests/BlazorCanvas.Tests/Database/DatabaseFixture.cs
    - tests/BlazorCanvas.Tests/Database/SchemaShapeTests.cs
    - tests/BlazorCanvas.Tests/Database/CheckConstraintTests.cs
    - tests/BlazorCanvas.Tests/Database/GuardMirrorsChecksTests.cs
  modified:
    - tests/BlazorCanvas.Tests/BlazorCanvas.Tests.csproj

key-decisions:
  - "Aligned the test project's EF Core package references (Microsoft.EntityFrameworkCore, Microsoft.EntityFrameworkCore.Relational) to 10.0.10 to match the app project's Microsoft.EntityFrameworkCore.Design version — direct use of DbContextOptionsBuilder in test code turned the pre-existing MSB3277 warning-only version conflict into a hard CS1705 compile error"
  - "The volume-persistence proof (ROADMAP success criterion 1, re-proven with real data) is implemented as a real, permanent xUnit Fact that shells out to 'docker compose down'/'up -d --wait' via Process.Start, rather than as an external manual/bash procedure — this makes the proof repeatable by any future 'dotnet test BlazorCanvas.sln' run, matching the plan's own automated verify command for Task 3"
  - "All illegal-row and matrix-agreement tests use a transaction-per-attempt pattern (begin, insert, never commit) so the suite is fully self-cleaning regardless of outcome; the one exception is the volume-persistence test, which commits for real (that's the point) and then explicitly deletes its own user (cascading to its figures) once the persistence proof is captured"
  - "DatabaseFixture connects on port 5433, not the D-27-documented 5432 — this repository's docker-compose.yml was already moved to 5433 in plan 01-03 as a user-approved deviation (native postgresql-x64-18 Windows service occupies 5432 on this machine); this plan does not touch docker-compose.yml or appsettings, only reuses the already-deviated port"

patterns-established:
  - "Live-database schema assertions always query information_schema/pg_catalog directly, never the EF model — the EF model is the thing under test, not the oracle"

requirements-completed: [DATA-02, TEST-01]

coverage:
  - id: D1
    description: "The database itself refuses an illegal row: a non-square or odd-sided circle, a zero-area rectangle or triangle, and a zero-length line are each rejected by a named CHECK constraint (SqlState 23514), with MinSizeGuard and Normalisation bypassed entirely (ROADMAP success criterion 3)"
    requirement: TEST-01
    verification:
      - kind: integration
        ref: "tests/BlazorCanvas.Tests/Database/CheckConstraintTests.cs#IllegalRow_IsRejectedByNamedConstraint (13 cases, Theory)"
        status: pass
    human_judgment: false
  - id: D2
    description: "A horizontal line, a vertical line, and an up-and-right diagonal line are all ACCEPTED by the database — they are legal figures, and rejecting them would misread D-50"
    requirement: TEST-01
    verification:
      - kind: integration
        ref: "tests/BlazorCanvas.Tests/Database/CheckConstraintTests.cs#LegalLine_IsAccepted (3 cases, Theory)"
        status: pass
    human_judgment: false
  - id: D3
    description: "figures_type_is_known rejects an unknown type literal and a PascalCase literal (the string an accidental Enum.ToString() would produce), while all four FigureTypeNames.ToDbValue literals round-trip successfully"
    requirement: TEST-01
    verification:
      - kind: integration
        ref: "tests/BlazorCanvas.Tests/Database/CheckConstraintTests.cs#IllegalRow_IsRejectedByNamedConstraint ('oval'/'Circle' cases); tests/BlazorCanvas.Tests/Database/GuardMirrorsChecksTests.cs#TypeLiteral_RoundTrips_AgainstFiguresTypeIsKnown"
        status: pass
    human_judgment: false
  - id: D4
    description: "The per-type min-size guard (MinSizeGuard.IsDrawable) and the three CHECK constraints agree exactly, in both directions, over a 32-case matrix (4 FigureType members x 8 boundary-probing boxes) — the decisive proof of D-50"
    requirement: TEST-01
    verification:
      - kind: integration
        ref: "tests/BlazorCanvas.Tests/Database/GuardMirrorsChecksTests.cs#GuardVerdict_MatchesDatabaseVerdict (32 cases, Theory); #HorizontalLine_AndZeroHeightRectangle_SameBoxOppositeVerdicts"
        status: pass
    human_judgment: false
  - id: D5
    description: "The live schema — queried from information_schema/pg_catalog, not the EF model — is exactly two tables (users, figures, plus __EFMigrationsHistory), with figures' 7 columns in order, users' 3 columns, type as text with zero Postgres enum types, 4 named CHECK constraints, ix_figures_user_id, identity id columns, the inscribed-square table comment, a unique index on username, and a CASCADE delete rule on the user_id FK"
    requirement: DATA-02
    verification:
      - kind: integration
        ref: "tests/BlazorCanvas.Tests/Database/SchemaShapeTests.cs (10 Facts)"
        status: pass
    human_judgment: false
  - id: D6
    description: "With PostgreSQL unreachable, the suite fails with an actionable message telling the developer to run 'docker compose up -d' — it does not silently skip"
    requirement: TEST-01
    verification:
      - kind: manual_procedural
        ref: "docker compose stop db && dotnet test tests/BlazorCanvas.Tests --filter FullyQualifiedName~SchemaShapeTests.Users_HasExactlyThreeColumns -> FAIL with InvalidOperationException naming 'docker compose up -d'; docker compose up -d --wait restored the container"
        status: pass
    human_judgment: false
  - id: D7
    description: "Figures written via EF Core survive a full container teardown of the named volume: a real 'docker compose down' (no -v) followed by 'docker compose up -d' — the same figure ids come back in the same ORDER BY id sequence (ROADMAP success criterion 1, re-proven with real EF-written data)"
    requirement: DATA-02
    verification:
      - kind: integration
        ref: "tests/BlazorCanvas.Tests/Database/GuardMirrorsChecksTests.cs#FiguresWrittenViaEfCore_SurviveContainerTeardown"
        status: pass
    human_judgment: false
  - id: D8
    description: "The entire phase's test suite is green in one command, geometry and database together"
    requirement: TEST-01
    verification:
      - kind: integration
        ref: "dotnet test BlazorCanvas.sln -> 145/145 passing (77 geometry, 68 database)"
        status: pass
    human_judgment: false

duration: ~30min
completed: 2026-07-14
status: complete
---

# Phase BC-01 Plan 04: Database Refuses an Illegal Row Summary

**68 new xUnit tests against the live PostgreSQL container prove ROADMAP success criterion 3 (the database itself rejects a non-square circle, zero-area box, and zero-length line via named CHECK constraints) and the decisive D-50 claim (MinSizeGuard.IsDrawable agrees with the live database across a 32-case matrix, in both directions) — including a real `docker compose down`/`up -d` container-teardown test proving the named volume holds EF-written figures.**

## Performance

- **Duration:** ~30 min
- **Completed:** 2026-07-14
- **Tasks:** 3
- **Files modified:** 5 (4 created, 1 modified)

## Accomplishments

- `DatabaseFixture` — an `IAsyncLifetime` xUnit fixture that connects to the live Compose container (port 5433 per the plan 01-03 D-27 deviation), migrates once, and **fails loudly** (not silently skips) when PostgreSQL is unreachable; confirmed live by stopping the container and observing the actual `InvalidOperationException` naming `docker compose up -d`
- `SchemaShapeTests` — 10 facts querying `information_schema`/`pg_catalog` directly (never the EF model): exactly 3 public tables and no `canvases`, `figures`' 7 columns in order, `users`' 3 columns, `type` is `text` with zero Postgres enum types, identity `id` columns, exactly 4 named CHECK constraints, `ix_figures_user_id`, the inscribed-square table comment, a unique index on `username`, and `CASCADE` on the `user_id` FK
- `CheckConstraintTests` — 21 tests (13 rejection cases + 3 legal-line acceptance cases + 3 well-formed-figure acceptance cases + implicit rows) constructing `Figure` entities with literal coordinates and calling `SaveChanges` directly, **bypassing `MinSizeGuard`/`Normalisation` entirely**; each rejection asserts the unwrapped `PostgresException.SqlState == "23514"` **and** the exact `ConstraintName` — proving ROADMAP success criterion 3 is a PostgreSQL fact, not a C# one
- `GuardMirrorsChecksTests` — the decisive test of the phase: a 32-case matrix (4 `FigureType` members × 8 boundary-probing boxes) asserting `MinSizeGuard.IsDrawable(type, box)` **if and only if** the live INSERT succeeds, plus the isolated horizontal-line/zero-height-rectangle pair that proves the guard is per-type (D-50) not shared (D-23, retracted), plus all four `FigureTypeNames.ToDbValue` literals round-tripping against `figures_type_is_known`, plus a real container-teardown test that inserts figures via the DbContext, shells out to `docker compose down` (no `-v`) and `docker compose up -d --wait`, and asserts the same ids survive in the same order
- `dotnet test BlazorCanvas.sln` → **145/145 passing** (77 pre-existing geometry tests + 68 new database tests), `dotnet build BlazorCanvas.sln` → 0 errors, 0 warnings
- Manually confirmed (with the container stopped and restarted) that the suite fails loudly rather than skipping, and that after restart the app is still talking to the containerized PostgreSQL 17.10 — not the native PostgreSQL 18 service on this machine

## Task Commits

Each task was committed atomically:

1. **Task 1: Database test fixture and live schema-shape assertions** - `2030565` (test)
2. **Task 2: The database refuses an illegal row — ROADMAP success criterion 3** - `03fcb9b` (test)
3. **Task 3: The guard and the CHECKs agree — D-50's two halves, plus real-data volume persistence** - `bed992c` (test)

**Plan metadata:** (this commit, docs: complete plan)

## Files Created/Modified

- `tests/BlazorCanvas.Tests/Database/DatabaseFixture.cs` - `IAsyncLifetime` fixture: connection, one-time migrate, fail-loud-not-skip, transaction-scoped raw-INSERT helper bypassing the geometry core
- `tests/BlazorCanvas.Tests/Database/SchemaShapeTests.cs` - 10 facts asserting the live `information_schema`/`pg_catalog` shape matches `CONSTRAINT-schema` exactly
- `tests/BlazorCanvas.Tests/Database/CheckConstraintTests.cs` - proves ROADMAP success criterion 3: illegal rows rejected by name, legal lines and well-formed figures accepted
- `tests/BlazorCanvas.Tests/Database/GuardMirrorsChecksTests.cs` - the 32-case guard/CHECK agreement matrix, the D-46 literal round-trip, and the real container-teardown persistence proof
- `tests/BlazorCanvas.Tests/BlazorCanvas.Tests.csproj` - added explicit `Microsoft.EntityFrameworkCore`/`Microsoft.EntityFrameworkCore.Relational` 10.0.10 references (see Deviations)

## Decisions Made

- The volume-persistence proof (ROADMAP success criterion 1, re-proven with real data) is a real, permanent xUnit `Fact` that shells out to `docker compose down`/`up -d --wait` via `Process.Start`, rather than an external manual bash procedure. This makes the proof repeatable by any future `dotnet test BlazorCanvas.sln` run rather than a one-time claim recorded only in this summary, and matches the plan's own `<verify><automated>` command for Task 3, which already runs the whole solution's tests as the acceptance check.
- All illegal-row and matrix-agreement tests wrap their INSERT attempt in a transaction that is deliberately never committed, so the suite never accumulates rows in `users`/`figures` regardless of pass or fail. The persistence test is the one deliberate exception — it commits for real (that's the property under test) and cleans up its own user (cascading to its figures) immediately after the proof is captured.
- `DatabaseFixture` connects on port 5433 (not the D-27-documented 5432), reusing the deviation already applied to `docker-compose.yml`/`appsettings.Development.json` in plan 01-03. This plan does not touch either file.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] Aligned test project's EF Core package versions to fix a hard compile error**

- **Found during:** Task 1 (Database test fixture and live schema-shape assertions)
- **Issue:** The test project referenced `Npgsql.EntityFrameworkCore.PostgreSQL 10.0.3`, which transitively pulls `Microsoft.EntityFrameworkCore(.Relational/.Abstractions) 10.0.4`. The app project references `Microsoft.EntityFrameworkCore.Design 10.0.10`, which pulls the matching `10.0.10` EF Core assemblies. Prior plans (01-02, 01-03) never triggered a compile failure from this mismatch because no test code called EF Core APIs directly — it only ever showed up as the 3 pre-existing MSB3277 *warnings* noted in this plan's environment notes. The moment `DatabaseFixture.cs` used `DbContextOptionsBuilder<CanvasDbContext>` directly, the compiler needed a single consistent version and failed with `CS1705` ("BlazorCanvas ... uses ... EntityFrameworkCore, Version=10.0.10.0 ... with a higher version than ... 10.0.4.0 ... referenced").
- **Fix:** Added explicit `<PackageReference Include="Microsoft.EntityFrameworkCore" Version="10.0.10" />` and `<PackageReference Include="Microsoft.EntityFrameworkCore.Relational" Version="10.0.10" />` to `tests/BlazorCanvas.Tests/BlazorCanvas.Tests.csproj`, forcing NuGet to resolve the same 10.0.10 version the app project already uses. Both packages were already present in the local NuGet cache (pulled transitively by `Microsoft.EntityFrameworkCore.Design`), so no new package was installed — this is a version-pin, not a new dependency.
- **Files modified:** `tests/BlazorCanvas.Tests/BlazorCanvas.Tests.csproj`
- **Verification:** `dotnet build tests/BlazorCanvas.Tests/BlazorCanvas.Tests.csproj` went from 1 error (CS1705) + 3 MSB3277 warnings to 0 errors, 0 warnings. `dotnet build BlazorCanvas.sln` and `dotnet test BlazorCanvas.sln` both confirmed clean afterward.
- **Committed in:** `2030565` (Task 1 commit)

---

**Total deviations:** 1 auto-fixed (1 blocking — package version alignment, no new install)
**Impact on plan:** No scope creep, no `src/` files touched. The fix only pins versions of packages already transitively present via the app project's existing `Microsoft.EntityFrameworkCore.Design` reference (plan 01-03). It also incidentally cleared the 3 pre-existing MSB3277 warnings the environment notes said not to chase — a side effect of the correct fix, not a separate refactoring detour.

## Issues Encountered

None beyond the deviation above. The one manual verification step (stopping/restarting the container to prove fail-loud-not-skip behavior) completed cleanly: the container returned to `healthy` status via `docker compose up -d --wait`, and the full 145-test suite passed immediately afterward.

## User Setup Required

None. This plan authors only files under `tests/BlazorCanvas.Tests/` (plus the one `.csproj` version pin) and touches no `src/` file, `docker-compose.yml`, or `appsettings.*.json`. `git diff --name-only` for this plan's three task commits confirms this — every changed path is under `tests/`.

## Next Phase Readiness

- **ROADMAP success criterion 3 is fully satisfied and proven from the database's own error codes**, not from application logic: `CheckConstraintTests` shows a non-square/odd-sided circle, a zero-area rectangle/triangle, and a zero-length line are each rejected with `SqlState 23514` and the exact `ConstraintName`, with `MinSizeGuard`/`Normalisation` bypassed entirely.
- **D-50's two halves are proven to agree exactly, in both directions**, over a 32-case matrix — this is the property Phases 3–5 depend on: the drawing UI can trust that anything `MinSizeGuard.IsDrawable` accepts will also be accepted by the database, and vice versa, so a "successful" draw in the UI can never silently fail to persist.
- **ROADMAP success criterion 1 is re-proven with real EF-written data**: the named volume `canvas-pgdata` survives a full `docker compose down`/`up -d` cycle with actual `figures` rows, not just the plan 01-01 scratch table.
- **Phase BC-01 is now complete**: all four ROADMAP success criteria hold (schema enforcement, volume persistence with real data, database-level illegal-row rejection, and the three mandated TEST-01 geometry tests from plan 01-02). No `.razor` component, login flow, sync/broadcast code, or runtime CRUD service was added — those remain correctly deferred to Phases 2–5.
- No production code (`src/`) was modified by this plan. If any future phase finds a guard/CHECK disagreement, `CONSTRAINT-schema` (the DDL) is authoritative, and the fix belongs in plan 01-02's `MinSizeGuard` or plan 01-03's `CanvasDbContext`, never here.

---
*Phase: BC-01-database-schema-geometry-core*
*Completed: 2026-07-14*
