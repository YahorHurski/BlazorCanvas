---
phase: 04-select-drag-delete
plan: 01
subsystem: database
tags: [ef-core, postgres, figure-store, idor, tests]

requires:
  - phase: 03-the-canvas-drawing
    provides: FigureStore LoadAsync/InsertAsync, live database test fixture, and figure geometry contracts
provides:
  - Owner-filtered FigureStore.UpdateAsync returning affected-row count
  - Owner-filtered FigureStore.DeleteAsync returning affected-row count
  - Database tests proving move/delete affected counts, zero-row behavior, circle movement, and cross-user isolation
affects: [04-select-drag-delete, phase-05-live-sync]

tech-stack:
  added: []
  patterns:
    - EF Core ExecuteUpdateAsync/ExecuteDeleteAsync with id + user owner filter
    - Database-backed IDOR proof tests using the existing DatabaseFixture

key-files:
  created:
    - .planning/phases/BC-04-select-drag-delete/04-01-SUMMARY.md
  modified:
    - src/BlazorCanvas/Data/FigureStore.cs
    - tests/BlazorCanvas.Tests/Data/FigureStoreTests.cs

key-decisions:
  - "UpdateAsync and DeleteAsync use ExecuteUpdateAsync/ExecuteDeleteAsync instead of tracked saves so zero-row writes return 0 without throwing, per D-10."
  - "Both write methods filter on figure id and user id in the database query, making ownership enforcement part of the SQL WHERE clause."

patterns-established:
  - "Owner-scoped writes return affected-row counts; callers branch on 0 for staleness instead of catching exceptions."
  - "IDOR guard tests mutate real rows through the public store and assert the foreign-user write affects 0 rows and leaves data untouched."

requirements-completed:
  - FIG-03
  - FIG-04

coverage:
  - id: D1
    description: "FigureStore.UpdateAsync issues one owner-filtered coordinate UPDATE and returns the affected-row count."
    requirement: FIG-03
    verification:
      - kind: integration
        ref: "tests/BlazorCanvas.Tests/Data/FigureStoreTests.cs#UpdateAsync_MovesFigure_ReturnsOneAffectedRow"
        status: pass
      - kind: other
        ref: "dotnet build BlazorCanvas.sln"
        status: pass
    human_judgment: false
  - id: D2
    description: "UpdateAsync returns 0 without exception for missing rows and never updates another user's figure."
    requirement: FIG-03
    verification:
      - kind: integration
        ref: "tests/BlazorCanvas.Tests/Data/FigureStoreTests.cs#UpdateAsync_ForMissingFigure_ReturnsZeroAndThrowsNothing"
        status: pass
      - kind: integration
        ref: "tests/BlazorCanvas.Tests/Data/FigureStoreTests.cs#UpdateAsync_NeverTouchesAnotherUsersFigure"
        status: pass
      - kind: other
        ref: "mutation check: removing UpdateAsync owner filter makes UpdateAsync_NeverTouchesAnotherUsersFigure fail with affected=1"
        status: pass
    human_judgment: false
  - id: D3
    description: "Moving a circle preserves its inscribed square/radius while translating its centre."
    requirement: FIG-03
    verification:
      - kind: integration
        ref: "tests/BlazorCanvas.Tests/Data/FigureStoreTests.cs#UpdateAsync_Circle_TranslationPreservesTheInscribedSquare"
        status: pass
    human_judgment: false
  - id: D4
    description: "FigureStore.DeleteAsync issues one owner-filtered DELETE, returns the affected-row count, and leaves unrelated rows present."
    requirement: FIG-04
    verification:
      - kind: integration
        ref: "tests/BlazorCanvas.Tests/Data/FigureStoreTests.cs#DeleteAsync_RemovesFigure_ReturnsOneAffectedRow"
        status: pass
      - kind: integration
        ref: "tests/BlazorCanvas.Tests/Data/FigureStoreTests.cs#DeleteAsync_ForMissingFigure_ReturnsZeroAndThrowsNothing"
        status: pass
      - kind: integration
        ref: "tests/BlazorCanvas.Tests/Data/FigureStoreTests.cs#DeleteAsync_NeverDeletesAnotherUsersFigure"
        status: pass
    human_judgment: false

duration: 45min
completed: 2026-07-16
status: complete
---

# Phase 04 Plan 01: FigureStore Write Methods Summary

**Owner-filtered EF Core update/delete paths with database tests proving affected-row counts and cross-user isolation**

## Performance

- **Duration:** 45 min
- **Started:** 2026-07-16T18:34:00+02:00
- **Completed:** 2026-07-16T19:19:23+02:00
- **Tasks:** 2
- **Files modified:** 2

## Accomplishments

- Added `FigureStore.UpdateAsync(int userId, int figureId, Box box)` using `ExecuteUpdateAsync` against `Id` and `UserId`, returning the affected-row count.
- Added `FigureStore.DeleteAsync(int userId, int figureId)` using `ExecuteDeleteAsync` against `Id` and `UserId`, returning the affected-row count.
- Added seven live-database tests covering successful writes, zero-row missing rows, IDOR isolation, and circle movement preserving its inscribed square.
- Proved the `UpdateAsync` IDOR test fails when the owner filter is temporarily removed, then restored the filter.

## Task Commits

Each task was committed atomically:

1. **Task 1: UpdateAsync and DeleteAsync owner-filtered writes** - `2bb7b1a` (feat)
2. **Task 2: Affected-row and ownership tests** - `774b772` (test)

## Files Created/Modified

- `src/BlazorCanvas/Data/FigureStore.cs` - Adds the two write APIs and updates the class documentation to cover load/insert/update/delete.
- `tests/BlazorCanvas.Tests/Data/FigureStoreTests.cs` - Adds seven database-backed tests for update/delete behavior and ownership guards.
- `.planning/phases/BC-04-select-drag-delete/04-01-SUMMARY.md` - Records this plan's completion.

## Decisions Made

- None beyond the plan. The implementation followed the locked D-10 and T-04-01 design: affected-row counts are the staleness signal, and the owner filter is part of each write query.

## Deviations from Plan

None - plan executed exactly as written.

**Total deviations:** 0 auto-fixed.
**Impact on plan:** No scope change.

## Issues Encountered

- `apply_patch` and several read-only shell commands hit a missing Windows sandbox helper. I used narrowly scoped PowerShell rewrites and reran diffs/assertions after each edit.
- The plan's literal `SaveChangesAsync` source assertion required rewording an existing XML doc comment so the literal appears only in the existing `InsertAsync` call.

## Verification

- `dotnet build BlazorCanvas.sln` passed with 0 warnings and 0 errors.
- `docker compose up -d` confirmed the PostgreSQL container was running.
- `dotnet test tests/BlazorCanvas.Tests/BlazorCanvas.Tests.csproj` passed: 395 total, 395 passed, 0 failed, 0 skipped.
- Source assertions passed: owner filter count 2, `ExecuteUpdateAsync` count 1, `ExecuteDeleteAsync` count 1, `SaveChangesAsync` count 1, `SetProperty` count 4, statement-position `try`/`catch` count 0, `[Fact]` count 13, exception-shaped assertion count 0.
- Mutation check passed: temporarily removing the `UpdateAsync` owner filter made `UpdateAsync_NeverTouchesAnotherUsersFigure` fail with expected 0 / actual 1; restoring the filter made the focused test pass.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

Plan 04-02 can now build on the proven write store contract. Later UI wiring can call `UpdateAsync`/`DeleteAsync` and branch on the affected-row count without adding ownership checks or exception handling around zero-row staleness.

---
*Phase: 04-select-drag-delete*
*Completed: 2026-07-16*