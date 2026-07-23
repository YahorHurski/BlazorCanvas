---
phase: BC-10-storage-schema-migration-persistence-layer
plan: "04"
subsystem: database
tags: [postgresql, npgsql, migration, replay, jsonb, xunit]
requires:
  - phase: BC-10-storage-schema-migration-persistence-layer
    provides: v11 schema, deterministic legacy conversion, shape validation gateway, and FigureRepository insert path
provides:
  - Transactional v1.1-to-v1.11 migration with deterministic canvas and figure identifiers
  - Guarded per-run scratch-database replay of the immutable v1.1 fixture
  - Whole-population losslessness proof for 708 users, 795 figures, geometry, stacking order, style, and bbox cache
affects: [BC-10-05, BC-11-renderer-sync-cutover]
tech-stack:
  added: []
  patterns: [single-transaction data migration, deterministic migration ids, checksum-verified scratch replay, jsonb equality assertions]
key-files:
  created:
    - src/BlazorCanvas/Data/V11/V11DataMigration.cs
    - tests/BlazorCanvas.Tests/Database/V11/V11MigrationReplayFixture.cs
    - tests/BlazorCanvas.Tests/Database/V11/V11MigrationReplayTests.cs
  modified:
    - src/BlazorCanvas/Data/V11/FigureRepository.cs
    - tests/BlazorCanvas.Tests/Database/V11/FigureRepositoryTests.cs
key-decisions:
  - "The migration applies schema and type seed, creates canvases, converts figures, then commits; the draft's destructive step 4 remains deferred to Phase 11."
  - "Migrated canvas and figure ids derive deterministically from legacy ids, making replay idempotent while preserving old id as z."
  - "The dump is replayed only in a GUID-named, checksum-verified scratch database guarded against the live canvas database."
  - "Migrated created_at is the migration timestamp, the single documented D-68 inaccuracy; Phase 11 must call the migration once before cutover."
requirements-completed: [MIGR-01, MIGR-02, MIGR-03, MODEL-06]
coverage:
  - id: D1
    description: Transactional migration aborts on an invalid legacy row rather than silently losing or defaulting data.
    requirement: MIGR-01
    verification:
      - kind: integration
        ref: tests/BlazorCanvas.Tests/Database/V11/V11MigrationReplayTests.cs#InvalidLegacyFigure_AbortsAndRollsBackAllData
        status: pass
    human_judgment: false
  - id: D2
    description: Every fixture user receives one 1472 by 828 canvas, including 173 users with no figures.
    requirement: MIGR-02
    verification:
      - kind: integration
        ref: tests/BlazorCanvas.Tests/Database/V11/V11MigrationReplayTests.cs#FigurelessUsers_StillReceiveTheir173Canvases
        status: pass
    human_judgment: false
  - id: D3
    description: All 795 fixture figures retain converted geometry, absolute rendered vertices, old-id stacking order, and idempotent replay behaviour.
    requirement: MIGR-03
    verification:
      - kind: integration
        ref: tests/BlazorCanvas.Tests/Database/V11/V11MigrationReplayTests.cs#EveryLegacyFigure_MatchesAnIndependentConversion
        status: pass
      - kind: integration
        ref: tests/BlazorCanvas.Tests/Database/V11/V11MigrationReplayTests.cs#SecondRun_IsIdempotentAndChangesNoCounts
        status: pass
    human_judgment: false
  - id: D4
    description: Every migrated figure receives the validated fixed style and a bbox cache recomputed from stored geometry.
    requirement: MODEL-06
    verification:
      - kind: integration
        ref: tests/BlazorCanvas.Tests/Database/V11/V11MigrationReplayTests.cs#EveryFigure_UsesTheFixedStyleComparedAsJsonb
        status: pass
      - kind: integration
        ref: tests/BlazorCanvas.Tests/Database/V11/V11MigrationReplayTests.cs#EveryFigure_BboxEqualsAFreshGeometryRecompute
        status: pass
    human_judgment: false
duration: 5min
completed: 2026-07-22
status: complete
---

# Phase BC-10 Plan 04: Migration Replay Summary

**A guarded scratch-database replay now proves the v1.1 fixture migrates all 708 users and 795 figures losslessly into v11, with deterministic canvases, preserved layers, fixed style, and cached bounds.**

## Performance

- **Duration:** 5 min implementation window; manual close-out completed after the executor disconnected.
- **Started:** 2026-07-22T02:04:23Z
- **Completed:** 2026-07-22T02:08:58Z
- **Tasks:** 3/3
- **Files created:** 3
- **Files modified:** 2 supporting files
- **Verification:** 27 focused replay tests and 1,236 full-suite tests passed with zero failures.

## Accomplishments

- Added `V11DataMigration`, which applies and seeds the v11 schema, creates one deterministic 1472 by 828 canvas per legacy user, converts every legacy figure through the validation gateway and repository, and commits only after the whole run succeeds.
- Preserved the v1.1 visual model for all 795 figures: converted geometry and literal vertices, old-id `z` ordering, fixed JSONB style, zero rotation, and bbox values from reparsed geometry.
- Added a checksum-verified replay fixture which only restores into GUID-named `canvas_v11_replay_` scratch databases, rejects `canvas`, and drops its databases with `WITH (FORCE)`.

## Migration Rules

- The order is schema/type seed, transaction, canvases in ascending `users.id`, figures in ascending `figures.id`, then commit.
- The draft's destructive step 4 (dropping the old table) is deliberately deferred to Phase 11. `public.users` and `public.figures` are read-only here.
- Fixture results are exactly **708 canvases**, **795 figures**, and **173 empty canvases**. Figure ids and canvas ids are deterministic, while `z` is the old globally unique figure id.
- `created_at` on migrated figures is the migration timestamp, not an original creation time. This is D-68's single documented inaccuracy.
- Phase 11 must invoke this migration once before switching the application to the v11 persistence path.

## Task Commits

1. **Task 1: V11DataMigration — one canvas per user, every figure converted, one transaction** — `9216841` (feat)
2. **Task 2: The scratch-database replay harness** — `46fb95d` (test)
3. **Task 3: The replay proof — every figure, every vertex, every layer** — `065dc0b` (test)

## Files Created/Modified

- `src/BlazorCanvas/Data/V11/V11DataMigration.cs` — transactional migration and report.
- `tests/BlazorCanvas.Tests/Database/V11/V11MigrationReplayFixture.cs` — guarded fixture restore, scratch lifecycle, and fresh migration helper.
- `tests/BlazorCanvas.Tests/Database/V11/V11MigrationReplayTests.cs` — 27 whole-population replay and rollback assertions.
- `src/BlazorCanvas/Data/V11/FigureRepository.cs` — internal transaction-aware migration insert that retains the existing single bbox-writing SQL path.
- `tests/BlazorCanvas.Tests/Database/V11/FigureRepositoryTests.cs` — shared JSONB assertion overload for a scratch `NpgsqlDataSource`.

## Decisions Made

- Keep figure writes on `FigureRepository.InsertWithIdAndZAsync`; a migration-owned `INSERT INTO v11.figures` would create a second bbox-writing path.
- Make migration idempotency a property of deterministic legacy-id mappings plus `ON CONFLICT (id) DO NOTHING`, rather than tracking a separate migration marker.
- Use `jsonb` equality for object values; textual key order is not data equality.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 2 - Missing supporting capability] Added a transaction-aware repository insert overload.**

- **Found during:** Task 1.
- **Issue:** The existing repository surface could not execute its one bbox-writing insert inside the migration's already-open transaction.
- **Fix:** Added an internal `InsertWithIdAndZAsync` overload using the supplied connection and transaction, reusing the existing insert SQL and parameter binding.
- **Files modified:** `src/BlazorCanvas/Data/V11/FigureRepository.cs`
- **Verification:** All 27 replay tests and the full 1,236-test suite passed.
- **Committed in:** `9216841`

**2. [Rule 3 - Blocking test reuse] Extended the shared JSONB helper for the scratch data source.**

- **Found during:** Task 3.
- **Issue:** The plan required reuse of the existing JSONB comparison helper, but it accepted only the live `DatabaseFixture`, not the replay fixture's isolated data source.
- **Fix:** Added a data-source overload that delegates to the same guarded connection-level comparison.
- **Files modified:** `tests/BlazorCanvas.Tests/Database/V11/FigureRepositoryTests.cs`
- **Verification:** All 27 replay tests and the full 1,236-test suite passed.
- **Committed in:** `065dc0b`

**Total deviations:** 2 auto-fixed supporting changes. No application cutover, schema scope, or migration semantics changed.

## Issues Encountered

The original executor disconnected after creating the three task commits. This manual close-out verified the committed implementation and added the missing planning records without reimplementing or reverting task work.

## User Setup Required

None - the existing PostgreSQL test environment is used; no packages or external configuration were added.

## Next Phase Readiness

Plan 10-05 can build its remaining TEST-03 guards on the v11 schema and validation boundary. Phase 11 has a proven migration routine to call once before its renderer and persistence cutover, while retaining the old tables until that phase.

## Self-Check: PASSED

- All three planned artifacts and both minimal supporting files exist.
- Task commits `9216841`, `46fb95d`, and `065dc0b` exist in history.
- Focused replay verification passed 27/27; the full solution passed 1,236/1,236.
- Docker PostgreSQL reports zero surviving `canvas_v11_replay_%` databases after the test run.
- Stub scan found only intentional C# non-null initialization (`null!`) and raw SQL string declarations; no unresolved placeholder or UI data stub was introduced.

---
*Phase: BC-10-storage-schema-migration-persistence-layer*
*Completed: 2026-07-22*
