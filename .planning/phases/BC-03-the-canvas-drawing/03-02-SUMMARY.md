---
phase: 03-the-canvas-drawing
plan: 02
subsystem: database
tags: [efcore, postgres, dbcontextfactory, blazor-server, npgsql]

requires:
  - phase: BC-01-database-schema-geometry-core
    provides: CanvasDbContext, Figure entity, the three CHECK constraints, FigureTypeNames, Box
  - phase: BC-02-login-session-logout
    provides: Login.razor's scoped-CanvasDbContext create-or-compare flow, the user_id cookie claim
provides:
  - FigureStore.LoadAsync(userId) — the app's only canvas load, filtering on user_id, ordering by id
  - FigureStore.InsertAsync(userId, type, box) — returns the Figure with its database-assigned Id
  - Runtime IDbContextFactory<CanvasDbContext> registration replacing the scoped CanvasDbContext
  - Login.razor migrated to a short-lived context from the factory, behavior unchanged
affects: [BC-03 plan 03-04 (Home.razor canvas load/draw wiring), BC-04, BC-05 (cross-tab sync depends on the insert->id->broadcast ordering)]

tech-stack:
  added: []
  patterns:
    - "IDbContextFactory<T> with short-lived, per-call contexts for any Blazor Server InteractiveServer component or service (never a scoped DbContext in that render mode)"
    - "AsNoTracking on read paths whose entities outlive the context (view state held across a whole circuit)"
    - "Return-after-SaveChangesAsync so a database-assigned identity column is available to the caller"

key-files:
  created:
    - src/BlazorCanvas/Data/FigureStore.cs
    - tests/BlazorCanvas.Tests/Data/FigureStoreTests.cs
  modified:
    - src/BlazorCanvas/Program.cs
    - src/BlazorCanvas/Components/Pages/Login.razor

key-decisions:
  - "DbContext lifetime (IDbContextFactory vs scoped) was engineering discretion — docs/DECISIONS.md is silent on it — recorded here per the plan's own note"
  - "Test-side IDbContextFactory<CanvasDbContext> adapter is a hand-written nested class over DatabaseFixture.CreateContext(), not a DI/mocking package (D-49 test-project scope cap)"

patterns-established:
  - "Pattern: any future service touching CanvasDbContext from a long-lived circuit takes IDbContextFactory<CanvasDbContext>, never CanvasDbContext, and creates/disposes a context per call"

requirements-completed: [DATA-01]

coverage:
  - id: D1
    description: "FigureStore.LoadAsync filters on user_id and orders by id — the load query that IS the canvas, proven never to leak another user's figures"
    requirement: "DATA-01"
    verification:
      - kind: integration
        ref: "tests/BlazorCanvas.Tests/Data/FigureStoreTests.cs#LoadAsync_NeverReturnsAnotherUsersFigures"
        status: pass
      - kind: integration
        ref: "tests/BlazorCanvas.Tests/Data/FigureStoreTests.cs#LoadAsync_ReturnsFiguresInCreationOrder"
        status: pass
      - kind: integration
        ref: "tests/BlazorCanvas.Tests/Data/FigureStoreTests.cs#LoadAsync_ForUserWithNoFigures_ReturnsEmptyNonNullList"
        status: pass
    human_judgment: false
  - id: D2
    description: "FigureStore.InsertAsync returns the Figure with its database-assigned Id, populated only after SaveChangesAsync"
    requirement: "DATA-01"
    verification:
      - kind: integration
        ref: "tests/BlazorCanvas.Tests/Data/FigureStoreTests.cs#InsertAsync_ReturnsDatabaseAssignedId_PresentInSubsequentLoad"
        status: pass
      - kind: integration
        ref: "tests/BlazorCanvas.Tests/Data/FigureStoreTests.cs#InsertAsync_TypeLiteral_RoundTrips"
        status: pass
    human_judgment: false
  - id: D3
    description: "Runtime container exposes IDbContextFactory<CanvasDbContext> and no scoped CanvasDbContext; app boots, migrates, and Login.razor's create-or-compare/unique-violation/SignInAsync flow is unchanged"
    verification:
      - kind: manual_procedural
        ref: "dotnet run --project src/BlazorCanvas; curl GET /login then POST Input.Username/Input.Password with antiforgery token -> 302 to / with .AspNetCore.Cookies set"
        status: pass
      - kind: unit
        ref: "dotnet test --filter FullyQualifiedName~UsernameNormalizer (0 failed, 6 passed)"
        status: pass
    human_judgment: false

duration: 20min
completed: 2026-07-16
status: complete
---

# Phase BC-03 Plan 02: FigureStore Data Path Summary

**FigureStore built on IDbContextFactory<CanvasDbContext> — `WHERE user_id = @id ORDER BY id` load and an INSERT that returns the database-assigned id, with Login.razor migrated off the scoped context.**

## Performance

- **Duration:** ~20 min
- **Started:** 2026-07-16T12:03Z (approx, per STATE.md session start)
- **Completed:** 2026-07-16T12:16Z
- **Tasks:** 3
- **Files modified:** 4 (2 modified, 2 created)

## Accomplishments
- `Program.cs` registers `IDbContextFactory<CanvasDbContext>` instead of a scoped `CanvasDbContext`, so the long-lived InteractiveServer circuit can never accumulate tracked entities or hit the "second operation started on this context" failure
- Startup migration block resolves the factory as a root service and creates a short-lived context, preserving the 10-attempt/2s `NpgsqlException` retry loop unchanged
- `Login.razor` retargeted onto `IDbContextFactory<CanvasDbContext>` with one context spanning the whole `LoginAsync` method — the unique-violation detach-and-requery race handling is untouched
- `FigureStore.LoadAsync(userId)`: `AsNoTracking().Where(f => f.UserId == userId).OrderBy(f => f.Id)` — no join, no canvas entity, exactly the query the plan's `must_haves.truths` specify
- `FigureStore.InsertAsync(userId, type, box)`: builds the `Figure` via `FigureTypeNames.ToDbValue`, saves, and returns the entity *after* `SaveChangesAsync` so `Id` is database-assigned
- `FigureStoreTests.cs`: 9 tests proving cross-user isolation (the phase's one real security surface, T-03-01), empty-canvas behavior, creation-order/z-order, id-after-INSERT, and type-literal round-trip for all four `FigureType` values

## Task Commits

Each task was committed atomically:

1. **Task 1: Move the runtime DbContext to IDbContextFactory and migrate the login consumer** - `e68ee08` (feat)
2. **Task 2: FigureStore — the load query that IS the canvas, and the insert that returns an id** - `82d8a8e` (feat)
3. **Task 3: Prove cross-user isolation, creation order, and the id-after-INSERT contract** - `5a26883` (test)

**Plan metadata:** (pending — final metadata commit follows this SUMMARY)

## Files Created/Modified
- `src/BlazorCanvas/Program.cs` - `AddDbContextFactory<CanvasDbContext>` registration, `AddScoped<FigureStore>()`, startup migration reworked onto the factory
- `src/BlazorCanvas/Components/Pages/Login.razor` - `@inject IDbContextFactory<CanvasDbContext> DbFactory`, one short-lived `db` context spanning `LoginAsync`
- `src/BlazorCanvas/Data/FigureStore.cs` - new: `LoadAsync`, `InsertAsync`, no update/delete, no normalisation
- `tests/BlazorCanvas.Tests/Data/FigureStoreTests.cs` - new: 9 tests against the live database via `DatabaseFixture`

## Decisions Made
- DbContext lifetime pattern (`IDbContextFactory` with per-call short-lived contexts) is engineering discretion, since no locked decision (D-01…D-58) constrains it — recorded per the plan's explicit instruction to document this choice.
- The test-side `IDbContextFactory<CanvasDbContext>` adapter is a small hand-written nested class (`TestDbContextFactory`) wrapping `DatabaseFixture.CreateContext()`, not a DI or mocking package, keeping the test project's package count at D-49's cap.

## Deviations from Plan

None - plan executed exactly as written. All acceptance criteria (source-assertion greps, build, boot, and test counts) were verified and passed without needing any Rule 1-4 auto-fix.

## TDD Gate Compliance

Task 3 is tagged `tdd="true"`, but the plan itself is `type: execute`, not `type: tdd`, and the plan's own task ordering builds the implementation in Task 2 before the proving tests in Task 3 — the opposite of RED-before-implementation. Because `FigureStore` already existed when the tests were written, there was no meaningful RED (failing) phase to enforce: the tests passed immediately (9 passed, 0 failed) against the already-complete implementation. This is a deliberate task-ordering choice in the plan (implementation task, then a dedicated proving-tests task), not a gate skip — the plan-level RED/GREEN/REFACTOR gate in the executor instructions applies only to `type: tdd` plans, which this is not.

## Issues Encountered
None.

## User Setup Required
None - no external service configuration required. The PostgreSQL dev database was already running per the environment notes; it was never started, stopped, or otherwise touched.

## Next Phase Readiness
- `FigureStore` is ready for `Home.razor` (plan 03-04) to call `LoadAsync` on initial render and `InsertAsync` after a completed `DrawGesture`/`MinSizeGuard`-accepted drag.
- The `InsertAsync`-returns-after-`SaveChangesAsync` ordering is the exact contract Phase 5's `insert -> get id -> broadcast` sequence needs (D-39) — no further data-path work is required to unblock that phase.
- Login/session behavior (Phase 2) is unchanged and was proven end-to-end via a real HTTP login round-trip (curl, antiforgery token, 302 to `/`, `.AspNetCore.Cookies` set) during this plan's verification, not just by unit test.
- No blockers or concerns carried forward.

---
*Phase: 03-the-canvas-drawing*
*Completed: 2026-07-16*

## Self-Check: PASSED

All created/modified files verified present on disk; all three task commits (`e68ee08`, `82d8a8e`, `5a26883`) verified present in git log.
