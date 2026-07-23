---
phase: 09-schema-entity-data-preserving-migration
plan: 01
subsystem: geometry
tags: [geometry, jsonb, storage, migration]
requires: []
provides:
  - GeometryCodec bidirectional Box to anchor+geometry JSON conversion
affects: [BC-09, BC-10, migrations, figure-store]
tech-stack:
  added: []
  patterns:
    - Storage boundary conversion through GeometryCodec
key-files:
  created:
    - src/BlazorCanvas/Geometry/GeometryCodec.cs
    - tests/BlazorCanvas.Tests/Geometry/GeometryCodecTests.cs
  modified: []
key-decisions:
  - "Geometry is represented as compact JSON strings at the boundary so EF can map it directly to jsonb later."
patterns-established:
  - "All Box <-> anchor+geometry conversions use GeometryCodec instead of re-derived formulas."
requirements-completed: [STOR-01]
coverage:
  - id: D1
    description: "GeometryCodec encodes and decodes all four figure types with integer-exact anchor and geometry values."
    requirement: STOR-01
    verification:
      - kind: unit
        ref: "dotnet test tests/BlazorCanvas.Tests/BlazorCanvas.Tests.csproj --filter FullyQualifiedName~GeometryCodec"
        status: pass
    human_judgment: false
duration: 10 min
completed: 2026-07-23
status: complete
---

# Phase 09 Plan 01: GeometryCodec Summary

**Box values now round-trip through D-59 anchor coordinates and compact per-type geometry JSON.**

## Performance

- **Duration:** 10 min
- **Started:** 2026-07-23T18:30:00Z
- **Completed:** 2026-07-23T18:40:00Z
- **Tasks:** 1
- **Files modified:** 2

## Accomplishments

- Added `GeometryCodec` for rectangle/triangle `{w,h}`, circle centre-anchor `{r}`, and line `{dx,dy}` conversion.
- Reused `CircleEncoding` for the circle path.
- Added unit coverage for all four types, including horizontal/vertical lines and the signed diagonal line landmine.

## Task Commits

1. **Task 1: GeometryCodec - Box <-> (anchor, geometry-JSON) per type, test-first** - `9c2d6fd` (feat)

**Plan metadata:** committed separately by GSD close-out.

## Files Created/Modified

- `src/BlazorCanvas/Geometry/GeometryCodec.cs` - Bidirectional storage-boundary codec.
- `tests/BlazorCanvas.Tests/Geometry/GeometryCodecTests.cs` - Round-trip and per-type JSON assertions.

## Decisions Made

- Geometry JSON is stored as compact strings with explicit lowercase member names.

## Deviations from Plan

None - plan executed exactly as written.

**Total deviations:** 0 auto-fixed. **Impact on plan:** None.

## Issues Encountered

None.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

Ready for 09-03 and 09-04 to reuse the same formulas in production persistence and migration SQL.

---
*Phase: 09-schema-entity-data-preserving-migration*
*Completed: 2026-07-23*
