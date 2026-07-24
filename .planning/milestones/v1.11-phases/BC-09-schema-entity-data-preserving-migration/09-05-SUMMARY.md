---
phase: 09-schema-entity-data-preserving-migration
plan: 05
subsystem: testing
tags: [xunit, postgres, schema, guid, jsonb]
requires:
  - phase: 09-04
    provides: Migrated D-59 database schema
provides:
  - Existing test suite adapted to Guid anchor+geometry model
affects: [BC-09, regression, migration-round-trip]
tech-stack:
  added: []
  patterns:
    - Tests decode stored geometry through GeometryCodec when asserting Box behavior
key-files:
  created: []
  modified:
    - tests/BlazorCanvas.Tests/Database/DatabaseFixture.cs
    - tests/BlazorCanvas.Tests/Data/FigureStoreTests.cs
    - tests/BlazorCanvas.Tests/Sync/CanvasSyncNotifierTests.cs
    - tests/BlazorCanvas.Tests/Database/SchemaShapeTests.cs
    - tests/BlazorCanvas.Tests/Database/CheckConstraintTests.cs
    - tests/BlazorCanvas.Tests/Database/GuardMirrorsChecksTests.cs
key-decisions:
  - "DB geometry CHECK mirror tests were trimmed because D-59 deliberately removes geometry CHECKs; Phase 10 owns code-side guard re-expression."
patterns-established:
  - "DB-backed tests run against the migrated canvas_phase09 database in this workspace to avoid the unrelated future-schema canvas database."
requirements-completed: [STOR-01]
coverage:
  - id: D1
    description: "The existing test suite compiles and passes on Guid ids and anchor+geometry storage."
    requirement: STOR-01
    verification:
      - kind: integration
        ref: "BLAZORCANVAS_TEST_CONNECTION=Host=localhost;Port=5433;Database=canvas_phase09;Username=postgres;Password=postgres dotnet test tests/BlazorCanvas.Tests/BlazorCanvas.Tests.csproj"
        status: pass
    human_judgment: false
duration: 18 min
completed: 2026-07-23
status: complete
---

# Phase 09 Plan 05: Test Suite Adaptation Summary

**The existing xUnit suite is green on the migrated Guid anchor+geometry schema.**

## Performance

- **Duration:** 18 min
- **Started:** 2026-07-23T19:32:00Z
- **Completed:** 2026-07-23T19:50:00Z
- **Tasks:** 2
- **Files modified:** 6

## Accomplishments

- Adapted test fixture raw inserts to write `x`, `y`, `geometry`, and `z`.
- Retyped FigureStore and sync tests to Guid ids while preserving IDOR, empty-load, ordering, persistence, and box-payload assertions.
- Updated live schema tests to assert D-59: uuid id default, integer anchor, jsonb geometry, numeric z, one type CHECK, `(user_id,z)` index, and no `created_at`.
- Trimmed removed geometry CHECK and guard-mirror tests with Phase 10 forward comments.

## Task Commits

1. **Task 1: Adapt compile-surface tests** - `a760f0f` (test)
2. **Task 2: Re-point schema tests and trim removed-CHECK tests** - `62e202f` (test)

**Plan metadata:** committed separately by GSD close-out.

## Files Created/Modified

- `tests/BlazorCanvas.Tests/Database/DatabaseFixture.cs` - New-model raw insert helper.
- `tests/BlazorCanvas.Tests/Data/FigureStoreTests.cs` - Guid/z/GeometryCodec assertions.
- `tests/BlazorCanvas.Tests/Sync/CanvasSyncNotifierTests.cs` - Guid sync ids.
- `tests/BlazorCanvas.Tests/Database/SchemaShapeTests.cs` - D-59 schema assertions.
- `tests/BlazorCanvas.Tests/Database/CheckConstraintTests.cs` - Retained type-whitelist coverage.
- `tests/BlazorCanvas.Tests/Database/GuardMirrorsChecksTests.cs` - Retained type literal and persistence coverage.

## Decisions Made

- Continued running DB tests against `canvas_phase09` for this workspace because `canvas` is an unrelated future-schema database.

## Deviations from Plan

None - plan executed exactly as written.

**Total deviations:** 0 auto-fixed. **Impact on plan:** None.

## Issues Encountered

None.

## User Setup Required

None - Compose Postgres was already running. Test command used `BLAZORCANVAS_TEST_CONNECTION` to target `canvas_phase09`.

## Next Phase Readiness

Ready for 09-06 to add the fixture round-trip proof against the real migration.

---
*Phase: 09-schema-entity-data-preserving-migration*
*Completed: 2026-07-23*
