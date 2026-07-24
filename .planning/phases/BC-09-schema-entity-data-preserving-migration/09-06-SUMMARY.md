---
phase: 09-schema-entity-data-preserving-migration
plan: 06
subsystem: testing
tags: [migration, postgres, fixture, jsonb, xunit]
requires:
  - phase: 09-02
    provides: Immutable fixture and manifest
  - phase: 09-04
    provides: AnchorGeometryRewrite migration
  - phase: 09-05
    provides: Green test suite on D-59 model
provides:
  - Automated MIG-02 round-trip proof
affects: [BC-09, migration-audit]
tech-stack:
  added: []
  patterns:
    - Disposable Postgres database migration proof from immutable SQL fixture
key-files:
  created:
    - tests/BlazorCanvas.Tests/Migrations/FixtureManifest.cs
    - tests/BlazorCanvas.Tests/Migrations/MigrationRoundTripTests.cs
  modified: []
key-decisions:
  - "Fixture seeding strips pg_dump backslash meta-commands and executes the SQL through Npgsql, then EF applies the real migration."
patterns-established:
  - "Migration proofs create and drop isolated canvas_migtest_* databases, leaving shared databases untouched."
requirements-completed: [MIG-02]
coverage:
  - id: D1
    description: "The real AnchorGeometryRewrite migration preserves every fixture figure's anchor, geometry, z, row count, type coverage, and uuid ids."
    requirement: MIG-02
    verification:
      - kind: integration
        ref: "dotnet test tests/BlazorCanvas.Tests/BlazorCanvas.Tests.csproj --filter FullyQualifiedName~MigrationRoundTrip"
        status: pass
    human_judgment: false
  - id: D2
    description: "The full suite remains green after adding the migration round-trip proof."
    verification:
      - kind: integration
        ref: "BLAZORCANVAS_TEST_CONNECTION=Host=localhost;Port=5433;Database=canvas_phase09;Username=postgres;Password=postgres dotnet test tests/BlazorCanvas.Tests/BlazorCanvas.Tests.csproj"
        status: pass
    human_judgment: false
duration: 14 min
completed: 2026-07-23
status: complete
---

# Phase 09 Plan 06: Migration Round-Trip Summary

**The real migration preserves all 795 immutable fixture figures against the D-59 manifest.**

## Performance

- **Duration:** 14 min
- **Started:** 2026-07-23T19:50:00Z
- **Completed:** 2026-07-23T20:04:00Z
- **Tasks:** 2
- **Files modified:** 2

## Accomplishments

- Added a disposable Postgres harness that creates `canvas_migtest_*`, seeds `v1.1-pre-rewrite.sql`, runs EF `MigrateAsync`, and drops the database in cleanup.
- Added a manifest parser for the 795-row expected-values table.
- Asserted row-count preservation, all-four-types coverage, per-row anchor/geometry/z equality, circle exactness, down-right line preservation, z-order, and uuid migrated ids.
- Verified no disposable migration-test databases remained after the run.

## Task Commits

1. **Task 1-2: Round-trip harness and MIG-02 assertions** - `a664932` (test)

**Plan metadata:** committed separately by GSD close-out.

## Files Created/Modified

- `tests/BlazorCanvas.Tests/Migrations/FixtureManifest.cs` - Parses the manifest expected values.
- `tests/BlazorCanvas.Tests/Migrations/MigrationRoundTripTests.cs` - Seeds fixture, runs migration, and asserts migrated rows.

## Decisions Made

- Used the actual EF migration path instead of re-implementing migration math in C#.

## Deviations from Plan

None - plan executed exactly as written.

**Total deviations:** 0 auto-fixed. **Impact on plan:** None.

## Issues Encountered

None.

## User Setup Required

None - Compose Postgres was already running.

## Next Phase Readiness

All Phase 9 plans are complete. Ready for phase verification and then Phase 10 planning/execution.

---
*Phase: 09-schema-entity-data-preserving-migration*
*Completed: 2026-07-23*
