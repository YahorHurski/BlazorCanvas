---
phase: BC-15-draw-preview-render-persist-a-star
plan: 03
subsystem: preview
tags: [dotnet, blazor-server, star5, preview, source-contract]
requires:
  - phase: BC-15-draw-preview-render-persist-a-star
    plan: 01
    provides: Star draw, clamp, degenerate rejection, sliver acceptance, and immediate persistence/reload.
  - phase: BC-15-draw-preview-render-persist-a-star
    plan: 02
    provides: FigureShape renderer proof for committed Star5Geometry points.
provides:
  - FIG-06 proof that active star previews use DrawingPreviewSession registry placement.
  - Home.razor preview wiring that renders through FigureShape as the final SVG child.
  - Geometry-free Home.razor.js helper preserving pointer capture and stale-preview cleanup.
affects: [BC-15, FIG-06, DrawingPreviewSession, FigureShape, Home.razor, Home.razor.js]
tech-stack:
  added: []
  patterns:
    - Local previews are rendered as ShapePlacement data through the shared FigureShape renderer.
    - Source-contract tests pin JS as lifecycle-only, not visible shape geometry owner.
key-files:
  created:
    - tests/BlazorCanvas.Tests/Components/HomePreviewSourceTests.cs
    - .planning/phases/BC-15-draw-preview-render-persist-a-star/15-03-SUMMARY.md
  modified:
    - src/BlazorCanvas/Components/Pages/Home.razor
    - src/BlazorCanvas/Components/Pages/Home.razor.js
    - tests/BlazorCanvas.Tests/Components/DrawingPreviewSessionTests.cs
    - tests/BlazorCanvas.Tests/Components/CanvasInteractionCoordinatorTests.cs
key-decisions:
  - "Visible drawing preview geometry now belongs to DrawingPreviewSession plus FigureShape; Home.razor.js is lifecycle-only."
  - "The preview remains circuit-local and is never published through CanvasSyncNotifier."
requirements-completed: [FIG-06]
coverage:
  - id: P1
    description: DrawingPreviewSession star5 placement JSON equals DefaultShapes.CreateRegistry().Get("star5").FromGesture for active cursor updates and clamp edge cases.
    requirement: FIG-06
    verification:
      - kind: unit
        ref: "dotnet test tests/BlazorCanvas.Tests/BlazorCanvas.Tests.csproj --no-restore --filter \"FullyQualifiedName~DrawingPreviewSessionTests|FullyQualifiedName~HomePreviewSourceTests|FullyQualifiedName~CanvasInteractionCoordinatorTests|FullyQualifiedName~V11RenderContractTests\""
        status: pass
    human_judgment: false
  - id: P2
    description: Home.razor renders active previews as a final FigureShape child with PreviewPlacement and PreviewType after persisted figures and SelectionTrace.
    requirement: FIG-06
    verification:
      - kind: source-contract
        ref: "tests/BlazorCanvas.Tests/Components/HomePreviewSourceTests.cs"
        status: pass
    human_judgment: false
  - id: P3
    description: Home.razor.js keeps pointer capture and stale-preview cleanup while containing no shape-specific SVG creation, fallback polygon, star5 branch, or trigonometric star formula.
    requirement: FIG-06
    verification:
      - kind: source-contract
        ref: "tests/BlazorCanvas.Tests/Components/HomePreviewSourceTests.cs"
        status: pass
      - kind: unit
        ref: "dotnet test BlazorCanvas.sln --no-restore"
        status: pass
    human_judgment: false
duration: 4min
completed: 2026-07-22
status: complete
---

# Phase 15 Plan 03: Draw, Preview, Render & Persist a Star Summary

**Live star previews now render from the same registry placement and FigureShape renderer as committed stars**

## Performance

- **Duration:** 4min
- **Started:** 2026-07-22T21:29:50Z
- **Completed:** 2026-07-22T21:33:17Z
- **Tasks:** 2
- **Files modified:** 5

## Accomplishments

- Extended `DrawingPreviewSessionTests` so `star5` participates in registry-derived preview placement and canvas-edge clamp coverage.
- Added `HomePreviewSourceTests` proving the active preview is a final SVG child rendered through `FigureShape` with `PreviewPlacement` and `PreviewType`.
- Updated `Home.razor` to render the active `DrawingPreviewSession` placement through `FigureShape`, after persisted figures and after `SelectionTrace`.
- Simplified `Home.razor.js` to remove client-owned line/rect/circle/polygon geometry, the unknown-shape triangle fallback, and any star formula path while preserving pointer capture and stale-preview cleanup.
- Aligned the older coordinator preview source contract with the new ownership boundary.

## Task Commits

1. **Task 1: Specify star preview parity and source wiring** - `8196825` (test RED)
2. **Task 2: Render the active preview through FigureShape and remove JS shape formulas** - `99ef7cf` (feature GREEN)

## Files Created/Modified

- `src/BlazorCanvas/Components/Pages/Home.razor` - Adds the active local preview `FigureShape` after persisted figures and selection trace.
- `src/BlazorCanvas/Components/Pages/Home.razor.js` - Removes all visible preview geometry formulas and keeps attach/detach pointer capture cleanup.
- `tests/BlazorCanvas.Tests/Components/DrawingPreviewSessionTests.cs` - Adds star5 registry-placement and clamp parity coverage.
- `tests/BlazorCanvas.Tests/Components/HomePreviewSourceTests.cs` - Adds Razor/JS source contracts for FIG-06.
- `tests/BlazorCanvas.Tests/Components/CanvasInteractionCoordinatorTests.cs` - Updates the existing preview source assertion to require Razor preview ownership and JS non-ownership.

## Decisions Made

- Visible drawing preview geometry now belongs to `DrawingPreviewSession` plus `FigureShape`; `Home.razor.js` is lifecycle-only.
- The preview remains circuit-local and is never published through `CanvasSyncNotifier`.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking issue] Updated obsolete preview source assertion**
- **Found during:** Task 2
- **Issue:** `CanvasInteractionCoordinatorTests.HomeDrawingPreview_IsCircuitLocalAndUsesCompletedGestureForCommit` still asserted that the JS script contained preview SVG styling, which directly contradicted this plan's required removal of JS-owned geometry.
- **Fix:** Replaced that assertion with checks for `FigureShape` preview wiring in `Home.razor` and negative checks for JS SVG geometry creation.
- **Files modified:** `tests/BlazorCanvas.Tests/Components/CanvasInteractionCoordinatorTests.cs`
- **Verification:** Focused preview/coordinator/render test filter and full solution test both passed.
- **Committed in:** `99ef7cf`

**Total deviations:** 1 auto-fixed (Rule 3 blocking test-contract alignment)
**Impact on plan:** The deviation was required to let the new source of truth replace the old JS preview contract. No architecture, dependency, schema, persistence, or sync boundary changed.

## TDD Gate Compliance

- RED gate: `8196825` added failing tests. The focused filter failed because `Home.razor` had no `FigureShape` preview block and `Home.razor.js` still created preview SVG geometry.
- GREEN gate: `99ef7cf` implemented the Razor preview and JS cleanup-only helper. Focused and full solution tests passed.

## Auth Gates

None.

## Issues Encountered

- The first focused GREEN run exposed two source-contract mismatches: a self-closing Razor tag extraction in the new test and an older test expecting JS preview styling. Both were corrected before the Task 2 commit.

## User Setup Required

None.

## Verification

- `dotnet test tests/BlazorCanvas.Tests/BlazorCanvas.Tests.csproj --no-restore --filter "FullyQualifiedName~DrawingPreviewSessionTests|FullyQualifiedName~HomePreviewSourceTests"` - RED failed before implementation as expected, 2 failing source-contract assertions.
- `dotnet test tests/BlazorCanvas.Tests/BlazorCanvas.Tests.csproj --no-restore --filter "FullyQualifiedName~DrawingPreviewSessionTests|FullyQualifiedName~HomePreviewSourceTests|FullyQualifiedName~CanvasInteractionCoordinatorTests|FullyQualifiedName~V11RenderContractTests"` - pass, 27/27.
- `dotnet test BlazorCanvas.sln --no-restore` - pass, 540/540.

## Known Stubs

None.

## Threat Flags

None. No endpoint, auth path, file access path, schema boundary, package dependency, or sync protocol surface was introduced.

## Next Phase Readiness

Phase 15 is now complete: draw, preview, render, and persistence for `star5` are covered. Phase 16 can build on a committed star that previews locally through registry-derived geometry and is no longer duplicated in JS.

## Self-Check: PASSED

- Summary file created at `.planning/phases/BC-15-draw-preview-render-persist-a-star/15-03-SUMMARY.md`.
- Task commits recorded: `8196825`, `99ef7cf`.
- Required verification commands passed after final code/test changes.

---
*Phase: BC-15-draw-preview-render-persist-a-star*
*Completed: 2026-07-22*
