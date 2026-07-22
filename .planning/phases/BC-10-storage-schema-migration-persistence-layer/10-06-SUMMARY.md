---
phase: BC-10-storage-schema-migration-persistence-layer
plan: "06"
subsystem: database
tags: [postgresql, npgsql, migration, transaction, xunit]
requires:
  - phase: BC-10-storage-schema-migration-persistence-layer
    provides: v11 schema helpers, migration replay fixture, and transaction-aware figure insertion
provides:
  - One PostgreSQL transaction from v11 DDL and type seeding through canvas and figure migration
  - Catalog-backed proof that rejected legacy data leaves a legacy-only scratch database unchanged
affects: [BC-11-renderer-sync-cutover, BC-10-verification]
tech-stack:
  added: []
  patterns: [transaction-aware schema helpers, transactional PostgreSQL DDL]
key-files:
  created: []
  modified:
    - src/BlazorCanvas/Data/V11/V11Schema.cs
    - src/BlazorCanvas/Data/V11/V11DataMigration.cs
    - tests/BlazorCanvas.Tests/Database/V11/V11MigrationReplayTests.cs
key-decisions:
  - "Optional transaction parameters preserve existing standalone schema setup while allowing migrations to share one rollback boundary."
  - "The invalid-row regression queries PostgreSQL catalog resolution, so absence of figure-type seeds follows from absence of the v11 schema."
requirements-completed: [MIGR-01]
coverage:
  - id: D1
    description: Invalid legacy migration input rolls back v11 schema creation, type seeds, canvases, and figures while retaining the exact legacy records and redacting coordinates.
    requirement: MIGR-01
    verification:
      - kind: integration
        ref: tests/BlazorCanvas.Tests/Database/V11/V11MigrationReplayTests.cs#InvalidLegacyFigure_AbortsAndRollsBackAllData
        status: pass
      - kind: integration
        ref: dotnet test BlazorCanvas.sln --no-restore --nologo -v minimal
        status: pass
    human_judgment: false
duration: 3min
completed: 2026-07-22
status: complete
---

# Phase BC-10 Plan 06: Migration Atomicity Gap Closure Summary

**The v11 migration now rolls back its schema, seeded registry, canvases, and figures as one PostgreSQL transaction when legacy conversion rejects a row.**

## Performance

- **Duration:** 3 min
- **Completed:** 2026-07-22T14:30:12+02:00
- **Tasks:** 2/2
- **Files modified:** 3
- **Verification:** focused rollback test passed; replay suite passed 27/27; full suite passed 1,293/1,293.

## Accomplishments

- Added a fail-first regression that uses the guarded scratch fixture, verifies exception redaction and exact legacy-row retention, then proves `v11` and `v11.figure_types` are absent through PostgreSQL catalog resolution.
- Made `V11Schema.ApplyAsync` and `SeedFigureTypesAsync` transaction-aware without changing existing non-transactional callers.
- Began the migration transaction before DDL and seeding, retaining the same transaction for legacy reads, canvas inserts, repository writes, report validation, and commit.

## Task Commits

1. **Task 1: Specify no-residue rollback for an invalid legacy row** — `aa1a48d` (test)
2. **Task 2: Enclose schema, seed, and migrated data in one Npgsql transaction** — `40e6d24` (fix)

## Files Modified

- `tests/BlazorCanvas.Tests/Database/V11/V11MigrationReplayTests.cs` — catalog-backed legacy-only rollback regression.
- `src/BlazorCanvas/Data/V11/V11Schema.cs` — optional transaction binding for DDL and parameterised type seeding.
- `src/BlazorCanvas/Data/V11/V11DataMigration.cs` — single transaction envelope starting before schema application.

## Decisions Made

- Retained the connection-based helpers' default non-transactional behavior; callers opt into the ambient migration transaction without duplicate schema APIs.
- Used `to_regnamespace` and `to_regclass` after the expected failure because an absent namespace proves no v11 tables or seeded lookup rows persisted.

## Deviations from Plan

None — plan executed exactly as written.

## Issues Encountered

The new regression failed as intended before the transaction-boundary fix because the `v11` namespace persisted. It passed after the fix.

## User Setup Required

None.

## Next Phase Readiness

Phase 10's execution gap is closed and is ready for re-verification. Phase 11 remains the only phase authorised to cut over or mutate the legacy tables.

## Self-Check: PASSED

- Both task commits exist and contain only their planned code or test changes.
- Focused rollback test, all 27 migration replay tests, and the full 1,293-test suite passed.
- No Phase 11 cutover work, legacy-table mutation, package installation, or unrelated user files were included.

---
*Phase: BC-10-storage-schema-migration-persistence-layer*
*Completed: 2026-07-22*
