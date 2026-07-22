---
phase: BC-10-storage-schema-migration-persistence-layer
plan: "02"
subsystem: database
tags: [migration, geometry, uuid, testing]
requires:
  - phase: BC-09-shape-registry-validation-gateway
    provides: typed geometry records and canonical shape registry
provides:
  - lossless v1.1 row-to-local-geometry conversion
  - deterministic version-8 UUID derivation for migrated canvases and figures
  - database-free migration conversion and identifier proof suite
affects: [BC-10-03, BC-10-04, migration-replay]
tech-stack:
  added: []
  patterns:
    - pure conversion before the persistence gateway
    - deterministic UUID namespaces pinned by literal regression tests
key-files:
  created:
    - src/BlazorCanvas/Data/V11/LegacyFigureConversion.cs
    - src/BlazorCanvas/Data/V11/V11DeterministicId.cs
    - tests/BlazorCanvas.Tests/Migration/LegacyFigureConversionTests.cs
    - tests/BlazorCanvas.Tests/Migration/V11DeterministicIdTests.cs
  modified: []
key-decisions:
  - "D-60 conversion preserves legacy line point order; it never canonicalises a diagonal."
  - "D-62 legacy IDs map deterministically into frozen, namespace-separated version-8 UUID layouts."
patterns-established:
  - "Migration conversion returns typed Phase 9 geometry; JSON serialisation remains at FigureInputGateway."
  - "Migration proof tests use fixture literals rather than repeating conversion formulas."
requirements-completed: [MIGR-01]
coverage:
  - id: D1
    description: "Legacy rows convert losslessly to decimal placement and local typed geometry, including ordered and degenerate lines."
    requirement: MIGR-01
    verification:
      - kind: unit
        ref: "tests/BlazorCanvas.Tests/Migration/LegacyFigureConversionTests.cs#Convert_ManifestRows_MatchesPositionGeometryAndLocalBbox"
        status: pass
      - kind: unit
        ref: "dotnet test BlazorCanvas.sln --nologo --filter FullyQualifiedName~BlazorCanvas.Tests.Migration (PostgreSQL stopped)"
        status: pass
    human_judgment: false
  - id: D2
    description: "Migrated canvas and figure identifiers are deterministic, namespaced RFC 9562 version-8 UUIDs."
    requirement: MIGR-01
    verification:
      - kind: unit
        ref: "tests/BlazorCanvas.Tests/Migration/V11DeterministicIdTests.cs#Derivation_HasPinnedStableTextualForms"
        status: pass
    human_judgment: false
duration: 4min
completed: 2026-07-22
status: complete
---

# Phase BC-10 Plan 02: Pure Migration Conversion and IDs Summary

**Lossless v1.1 figure conversion and stable version-8 UUID mapping are proven independently of PostgreSQL.**

## Performance

- **Duration:** 4 min
- **Started:** 2026-07-22T01:44:52Z
- **Completed:** 2026-07-22T01:48:41Z
- **Tasks:** 3/3
- **Files modified:** 6

## Accomplishments

- Added pure conversion from legacy bounding-box rows: rectangles use `(x1,y1,w=x2-x1,h=y2-y1)`; circles use `(x1,y1,r=(x2-x1)/2)`; triangles use top-centre apex with a bottom-edge base; lines use `x1,min(y1,y2)` and retain the original endpoint order.
- Rejected unknown type literals and v1.1-impossible coordinate pairs without placing user coordinates in exception messages.
- Added deterministic, namespace-separated RFC 9562 version-8 UUIDs. `ForFigure(3860)` is pinned to `46494755-5245-8000-8000-000000000f14`; `ForCanvas(3561)` is pinned to `43414e56-4153-8000-8000-000000000de9`.
- Proved all eight fixture rows, legal degenerate lines, no-tidying edge cases, and 80 shape-mix cases with PostgreSQL stopped; the full solution suite also passes.

## Task Commits

1. **Task 1: The four conversion formulas** — `c9cc478` (feat)
2. **Task 2: Deterministic id derivation for migrated canvases and figures** — `44aca1d` (feat)
3. **Task 3: Exhaustive conversion and id tests, provable with the database stopped** — `0d1de05` (test)

## Files Created

- `src/BlazorCanvas/Data/V11/LegacyFigureRow.cs` — direct v1.1 figure-row transcription.
- `src/BlazorCanvas/Data/V11/ConvertedFigure.cs` — decimal placement plus typed local geometry.
- `src/BlazorCanvas/Data/V11/LegacyFigureConversion.cs` — exact, no-tidying conversion formulas and validation.
- `src/BlazorCanvas/Data/V11/V11DeterministicId.cs` — frozen-prefix deterministic UUID mapping.
- `tests/BlazorCanvas.Tests/Migration/LegacyFigureConversionTests.cs` — fixture contract and migration edge-case coverage.
- `tests/BlazorCanvas.Tests/Migration/V11DeterministicIdTests.cs` — identifier stability, format, separation, and collision coverage.

## Decisions Made

- Prefix constants are frozen: changing either would re-key every already-migrated canvas or figure.
- UUIDs are predictable by design and are not capability tokens; authorization must continue to filter by `canvas_id`.
- Conversion stays typed and database-free, leaving JSON serialisation and validation at the Phase 9 gateway.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Corrected incomplete manifest test-case arguments**
- **Found during:** Task 3
- **Issue:** Point-list fixture cases initially omitted their explicit local-bbox arguments and did not compile.
- **Fix:** Supplied the transcribed literal bbox values for every line and triangle case.
- **Files modified:** `tests/BlazorCanvas.Tests/Migration/LegacyFigureConversionTests.cs`
- **Verification:** Migration-only suite passed 117 tests with PostgreSQL stopped; full solution suite passed 1,179 tests.
- **Committed in:** `0d1de05`

**Total deviations:** 1 auto-fixed (1 Rule 1 bug)

## Issues Encountered

The first migration-only test compilation exposed the incomplete point-list fixture arguments; it was corrected inline and verified. PostgreSQL was restarted successfully before the full-suite run.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

Plan 10-04 can replay legacy rows using exact typed conversion results and the pinned deterministic UUIDs. No blockers remain.

## Self-Check: PASSED

- All six planned source and test files exist.
- Task commits `c9cc478`, `44aca1d`, and `0d1de05` exist.
- No stubs or new threat surfaces beyond the plan's documented migration boundaries were found.
