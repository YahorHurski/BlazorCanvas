---
phase: BC-15-draw-preview-render-persist-a-star
plan: 01
subsystem: testing
tags: [dotnet, blazor-server, postgres, star5, canvas-sync]
requires:
  - phase: BC-13-star-shape-core
    provides: Star5Shape geometry, parsing, canonical JSON, and bbox calculation.
  - phase: BC-14-catalog-seed-toolbar-decisions
    provides: Default registry star5 exposure, figure_types startup seed convergence, and Tool.Star mapping.
provides:
  - Coordinator-level proof that Tool.Star reaches DrawAsync as star5 through FigureInputGateway.
  - Edge clamp, zero extent rejection, positive sliver acceptance, selection, and committed draw publication tests for star5.
  - Final public PostgreSQL proof that star5 persists immediately and reloads unchanged.
  - Corrected Star5Shape gesture normalization so persisted bbox matches the dragged local box.
affects: [BC-15, BC-16, star5, FigureInputGateway, FigureRepository, CanvasSyncNotifier]
tech-stack:
  added: []
  patterns:
    - Registry-driven draw validation remains the only star draw path.
    - Final-public persistence tests use SyncHarness with real FigureRepository instances.
key-files:
  created:
    - .planning/phases/BC-15-draw-preview-render-persist-a-star/15-01-SUMMARY.md
  modified:
    - src/BlazorCanvas/Shapes/Star5Shape.cs
    - tests/BlazorCanvas.Tests/Components/CanvasInteractionCoordinatorTests.cs
    - tests/BlazorCanvas.Tests/Database/V11/FinalPublicCanvasSyncIntegrationTests.cs
    - tests/BlazorCanvas.Tests/Shapes/Star5ShapeTests.cs
key-decisions:
  - "Star5 gesture output is normalized so BoundsOf exactly matches the clamped drag box; this preserves the D-70 stretch-to-fill contract and keeps bbox_* authoritative."
  - "No star-specific coordinator or repository branch was introduced; DrawAsync still uses FigureInputGateway and FigureRepository.InsertAsync."
patterns-established:
  - "Coordinator star draw tests assert ToolMap.ToShapeName(Tool.Star) at the draw boundary."
  - "Final-public star persistence tests compare the committed row against a fresh FigureRepository.LoadAsync result."
requirements-completed: [FIG-05, FIG-07, DATA-05]
coverage:
  - id: D1
    description: Tool.Star/star5 draws through the shared coordinator path, selects the committed row, clamps to canvas bounds, rejects zero extents, accepts positive slivers, and publishes only committed draws.
    requirement: FIG-05
    verification:
      - kind: unit
        ref: "dotnet test tests/BlazorCanvas.Tests/BlazorCanvas.Tests.csproj --no-restore --filter \"FullyQualifiedName~CanvasInteractionCoordinatorTests\""
        status: pass
    human_judgment: false
  - id: D2
    description: Star draw edge behavior clamps to CanvasBounds, rejects zero width or zero height silently, and accepts a one-unit positive sliver.
    requirement: FIG-07
    verification:
      - kind: unit
        ref: "tests/BlazorCanvas.Tests/Components/CanvasInteractionCoordinatorTests.cs#StarToolDraw_D70D71D29D36_CommitsSelectedStar5ThroughRegistryGatewayAndClampsToCanvas"
        status: pass
      - kind: unit
        ref: "tests/BlazorCanvas.Tests/Components/CanvasInteractionCoordinatorTests.cs#StarDraw_D57D67_RejectsZeroExtentSilentlyWithoutRowsOrPublications"
        status: pass
      - kind: unit
        ref: "tests/BlazorCanvas.Tests/Components/CanvasInteractionCoordinatorTests.cs#StarDraw_D32_AcceptsPositiveOneCanvasUnitSliver"
        status: pass
    human_judgment: false
  - id: D3
    description: A star drawn through the final public repository persists immediately, reaches a second circuit as a committed draw, and reloads unchanged from public.figures.
    requirement: DATA-05
    verification:
      - kind: integration
        ref: "dotnet test tests/BlazorCanvas.Tests/BlazorCanvas.Tests.csproj --no-restore --filter \"FullyQualifiedName~FinalPublicCanvasSyncIntegrationTests\""
        status: pass
      - kind: integration
        ref: "dotnet test BlazorCanvas.sln --no-restore"
        status: pass
    human_judgment: false
duration: 25min
completed: 2026-07-22
status: complete
---

# Phase 15 Plan 01: Draw, Preview, Render & Persist a Star Summary

**Registry-driven star draw and final-public persistence proof, with gesture bounds corrected to fill the dragged box**

## Performance

- **Duration:** 25min
- **Started:** 2026-07-22T20:57:00Z
- **Completed:** 2026-07-22T21:22:15Z
- **Tasks:** 2
- **Files modified:** 4

## Accomplishments

- Added coordinator tests proving Tool.Star maps to star5 and reaches DrawAsync through FigureInputGateway, with immediate local selection and committed draw publication.
- Covered star edge behavior: out-of-canvas gestures clamp to CanvasBounds, zero-width and zero-height gestures are silently rejected, and positive one-unit slivers are accepted.
- Added a final-public PostgreSQL integration test proving star5 inserts immediately, reaches a second circuit as a draw message, and reloads unchanged through a fresh repository.
- Fixed Star5Shape.FromGesture so generated points normalize to the requested local bounds, making bbox_* match the dragged box as required by D-70/D-71.

## Task Commits

1. **Task 1: Specify registry-driven star draw behavior at the coordinator boundary** - `80d9aa6` (test RED), `a35a97a` (fix GREEN), `abc3cb5` (test alignment)
2. **Task 2: Prove final-public star persistence and reload** - `4e28f53` (test)

## Files Created/Modified

- `src/BlazorCanvas/Shapes/Star5Shape.cs` - Normalizes gesture-generated star points so BoundsOf spans the dragged local box.
- `tests/BlazorCanvas.Tests/Components/CanvasInteractionCoordinatorTests.cs` - Adds Tool.Star/star5 draw, selection, clamp, rejection, sliver, and publication coverage.
- `tests/BlazorCanvas.Tests/Database/V11/FinalPublicCanvasSyncIntegrationTests.cs` - Adds final-public star persistence/reload and committed-draw relay proof.
- `tests/BlazorCanvas.Tests/Shapes/Star5ShapeTests.cs` - Aligns star core expectations with the filled-bounds gesture contract.

## Decisions Made

- Star5 gesture output is normalized after raw point generation so the authoritative point list fills the clamped drag box; this is the minimal correction for the D-70 stretch-to-fill contract.
- No production coordinator, sync, repository, schema, migration, package, Save button, flush method, or star-specific persistence path was added.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Fixed star gesture bbox inset**
- **Found during:** Task 1 (Specify registry-driven star draw behavior at the coordinator boundary)
- **Issue:** The raw point-up star was inscribed in the drag box, so BoundsOf produced an inset x range and a width smaller than the clamped gesture width. This broke the plan truth that out-of-canvas star draws persist local bbox width/height equal to CanvasBounds.
- **Fix:** Normalized Star5Shape.FromGesture output points from their raw bounds into the requested local width/height, preserving ten points and innerRatio.
- **Files modified:** `src/BlazorCanvas/Shapes/Star5Shape.cs`, `tests/BlazorCanvas.Tests/Shapes/Star5ShapeTests.cs`
- **Verification:** `dotnet test tests/BlazorCanvas.Tests/BlazorCanvas.Tests.csproj --no-restore --filter "FullyQualifiedName~CanvasInteractionCoordinatorTests"`; `dotnet test tests/BlazorCanvas.Tests/BlazorCanvas.Tests.csproj --no-restore --filter "FullyQualifiedName~Star5ShapeTests"`; `dotnet test BlazorCanvas.sln --no-restore`
- **Committed in:** `a35a97a`, `abc3cb5`

---

**Total deviations:** 1 auto-fixed (Rule 1 bug)
**Impact on plan:** The fix was required for correctness and stayed within the star gesture contract. No new architecture or persistence branch was introduced.

## Issues Encountered

- A compile-only test wiring failure occurred before the RED assertion because the new coordinator tests needed existing `ToolMap` and `CanvasBounds` namespaces.
- A parallel focused test run caused a transient build artifact file lock; rerunning serially passed.

## User Setup Required

None - no external service configuration required beyond the existing PostgreSQL test fixture environment.

## Verification

- `dotnet test tests/BlazorCanvas.Tests/BlazorCanvas.Tests.csproj --no-restore --filter "FullyQualifiedName~CanvasInteractionCoordinatorTests"` - pass, 15/15.
- `dotnet test tests/BlazorCanvas.Tests/BlazorCanvas.Tests.csproj --no-restore --filter "FullyQualifiedName~FinalPublicCanvasSyncIntegrationTests"` - pass, 5/5.
- `dotnet test tests/BlazorCanvas.Tests/BlazorCanvas.Tests.csproj --no-restore --filter "FullyQualifiedName~Star5ShapeTests"` - pass, 23/23.
- `dotnet test BlazorCanvas.sln --no-restore` - pass, 535/535.

## Known Stubs

None.

## Threat Flags

None. No new endpoint, auth path, file access path, schema boundary, or package dependency was introduced.

## Next Phase Readiness

Plan 15-02 can build on a durable star5 row and the corrected local geometry bounds. Rendering proof remains pending for RENDER-02, and live preview parity remains pending for FIG-06 as planned.

## Self-Check: PASSED

- Summary file created at `.planning/phases/BC-15-draw-preview-render-persist-a-star/15-01-SUMMARY.md`.
- Task commits recorded: `80d9aa6`, `a35a97a`, `abc3cb5`, `4e28f53`.
- Required verification commands passed after final code/test changes.

---
*Phase: BC-15-draw-preview-render-persist-a-star*
*Completed: 2026-07-22*
