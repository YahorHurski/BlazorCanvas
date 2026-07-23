---
phase: BC-13-star-shape-core
plan: 01
subsystem: shapes
tags: [dotnet, blazor, geometry-json, xunit, star5]
requires: []
provides:
  - Star5Geometry stores authoritative ten-point star geometry plus innerRatio metadata.
  - Star5Shape implements IShapeDefinition in isolation without DefaultShapes registration.
  - Direct unit tests cover gesture geometry, point-derived bounds, canonical JSON, malformed rejection, drawability, and the Phase 13 registry fence.
affects: [BC-14-catalog-seed-toolbar-decisions, BC-15-draw-preview-render-persist-star, BC-16-interaction-sync-test-guards]
tech-stack:
  added: []
  patterns:
    - Point-list geometry remains authoritative; bbox scans points only.
    - Canonical shape JSON is emitted through GeometryJson.Serialise with explicit property order.
    - Browser gesture coordinates are rounded away from zero and clamped to CanvasBounds before shape math.
key-files:
  created:
    - src/BlazorCanvas/Shapes/Star5Geometry.cs
    - src/BlazorCanvas/Shapes/Star5Shape.cs
    - tests/BlazorCanvas.Tests/Shapes/Star5ShapeTests.cs
  modified: []
key-decisions:
  - "Star5Shape remains unregistered in DefaultShapes during Phase 13; Phase 14 owns registry/catalog exposure."
  - "Star5Geometry.InnerRatio is required and preserved, but bounds remain a pure function of Points."
patterns-established:
  - "Star5 gesture generation uses ten alternating polar vertices inside the normalized drag box, starting at -pi/2."
  - "Star5 JSON canonical order is points first, then innerRatio."
requirements-completed: [SHAPE-04, SHAPE-05, SHAPE-06]
coverage:
  - id: D1
    description: "Star5Shape.FromGesture creates a point-up, stretchable ten-point star with default inner ratio 0.382 from normal, reversed, and clamped corner-to-corner gestures."
    requirement: SHAPE-04
    verification:
      - kind: unit
        ref: "tests/BlazorCanvas.Tests/Shapes/Star5ShapeTests.cs#FromGesture_*"
        status: pass
      - kind: other
        ref: "dotnet test tests/BlazorCanvas.Tests/BlazorCanvas.Tests.csproj --no-restore --filter FullyQualifiedName~Star5ShapeTests"
        status: pass
    human_judgment: false
  - id: D2
    description: "Star5Shape.BoundsOf derives bounds only from the stored point list; changing only InnerRatio does not change bounds."
    requirement: SHAPE-05
    verification:
      - kind: unit
        ref: "tests/BlazorCanvas.Tests/Shapes/Star5ShapeTests.cs#BoundsOf_UsesPointListOnly"
        status: pass
    human_judgment: false
  - id: D3
    description: "Star5Shape.TryParseGeometry and ToJson round-trip canonical JSON byte-identically and reject missing, malformed, non-finite, zero, or negative innerRatio/points payloads."
    requirement: SHAPE-06
    verification:
      - kind: unit
        ref: "tests/BlazorCanvas.Tests/Shapes/Star5ShapeTests.cs#TryParseGeometry_* and ToJson_ValidGeometry_RoundTripsCanonicalJsonByteForByte"
        status: pass
    human_judgment: false
  - id: D4
    description: "DefaultShapes.CreateRegistry remains unchanged at line, rectangle, circle, triangle."
    verification:
      - kind: unit
        ref: "tests/BlazorCanvas.Tests/Shapes/Star5ShapeTests.cs#DefaultRegistry_DoesNotContainStar5DuringPhase13"
        status: pass
      - kind: other
        ref: "git diff --exit-code -- src/BlazorCanvas/Shapes/DefaultShapes.cs"
        status: pass
    human_judgment: false
duration: 5min
completed: 2026-07-22
status: complete
---

# Phase BC-13 Plan 01: Star Shape Core Summary

**Unregistered Star5 shape core with authoritative ten-point geometry, required innerRatio metadata, and direct contract tests**

## Performance

- **Duration:** 5 min
- **Started:** 2026-07-22T19:04:41Z
- **Completed:** 2026-07-22T19:09:25Z
- **Tasks:** 2
- **Files modified:** 3

## Accomplishments

- Added `Star5Geometry` as a sealed geometry record with ordered `Points` plus preserved `InnerRatio`.
- Added `Star5Shape` implementing `IShapeDefinition` for gesture generation, parse, canonical JSON serialization, drawability, and bounds.
- Added direct xUnit coverage for SHAPE-04, SHAPE-05, SHAPE-06 and a Phase 13 no-registration fence.
- Verified `DefaultShapes.cs` has no diff and the full solution test suite remains green.

## Task Commits

1. **Task 1: Specify Star5 shape behavior (RED)** - `02a8f28` (test)
2. **Task 1: Implement Star5 shape behavior (GREEN)** - `279286e` (feat)
3. **Task 2: Verify additive phase boundary and full suite** - no source commit; verification-only task

## Files Created/Modified

- `src/BlazorCanvas/Shapes/Star5Geometry.cs` - Stores the star's authoritative point list and descriptive inner ratio.
- `src/BlazorCanvas/Shapes/Star5Shape.cs` - Implements the unregistered `star5` shape definition.
- `tests/BlazorCanvas.Tests/Shapes/Star5ShapeTests.cs` - Covers gesture geometry, JSON, bounds, drawability, malformed rejection, and registry boundary.

## Decisions Made

- Star5 remains unregistered in `DefaultShapes.CreateRegistry()` for Phase 13, matching the roadmap boundary and leaving catalog seeding to Phase 14.
- `InnerRatio` is required, finite, positive, and preserved during parse/serialize, but `BoundsOf` ignores it and scans `Points` only.

## Verification

- `dotnet test tests/BlazorCanvas.Tests/BlazorCanvas.Tests.csproj --no-restore --filter FullyQualifiedName~Star5ShapeTests` - passed, 23/23.
- `dotnet test tests/BlazorCanvas.Tests/BlazorCanvas.Tests.csproj --no-restore --filter "FullyQualifiedName~Star5ShapeTests|FullyQualifiedName~DefaultShapesTests|FullyQualifiedName~ShapeRegistryExtensibilityTests|FullyQualifiedName~PointListPrimacyTests"` - passed, 46/46.
- `git diff --exit-code -- src/BlazorCanvas/Shapes/DefaultShapes.cs` - passed, no diff.
- `dotnet test BlazorCanvas.sln --no-restore` - passed, 523/523.

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered

- Initial Star5-only red-gate test run was blocked by `D:\Project1\src\BlazorCanvas\bin\Debug\net10.0\BlazorCanvas.exe` being locked by process `BlazorCanvas (17748)`. The process had exited before inspection, so no stop was needed; rerunning the same command produced the expected missing-type failure, then the green run passed.

## Known Stubs

None.

## Threat Flags

None. The only new trust-boundary code is the planned `Star5Shape.TryParseGeometry` parser, which requires object JSON, exactly ten finite points, and a finite positive `innerRatio`.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

Phase 14 can register `Star5Shape`, seed `star5`, and expose the toolbar button. The core shape contract is already covered and the additive boundary is verified.

## Self-Check: PASSED

- Found `src/BlazorCanvas/Shapes/Star5Geometry.cs`.
- Found `src/BlazorCanvas/Shapes/Star5Shape.cs`.
- Found `tests/BlazorCanvas.Tests/Shapes/Star5ShapeTests.cs`.
- Found task commits `02a8f28` and `279286e`.

---
*Phase: BC-13-star-shape-core*
*Completed: 2026-07-22*
