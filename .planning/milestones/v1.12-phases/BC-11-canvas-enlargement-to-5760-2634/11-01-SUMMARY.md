---
phase: BC-11-canvas-enlargement-to-5760-2634
plan: 01
subsystem: ui
tags: [blazor, css, svg, geometry, testing, dotnet]

# Dependency graph
requires:
  - phase: BC-10
    provides: anchor+geometry storage model, per-operation drag/draw/delete
provides:
  - CanvasBounds grown to 5760x2634 (single size source bound by Home.razor)
  - Fixed toolbar (position: fixed) with 48px canvas top offset preserving PageY-48 mapping
  - Test suite re-pinned to the enlarged surface bounds
affects: [11-02, canvas-enlargement, future zoom-pan camera work]

# Tech tracking
tech-stack:
  added: []
  patterns: ["Single-size-source constant (CanvasBounds) drives SVG dimensions with zero call-site duplication", "Fixed toolbar + compensating content padding-top to preserve a coordinate invariant across a layout change"]

key-files:
  created: []
  modified:
    - src/BlazorCanvas/Geometry/CanvasBounds.cs
    - src/BlazorCanvas/Components/Canvas/Toolbar.razor.css
    - src/BlazorCanvas/Components/Pages/Home.razor.css
    - tests/BlazorCanvas.Tests/Geometry/NormalisationTests.cs
    - tests/BlazorCanvas.Tests/Geometry/CanvasCoordinatesTests.cs
    - tests/BlazorCanvas.Tests/Geometry/DrawGestureTests.cs
    - tests/BlazorCanvas.Tests/Geometry/GeometryCodecTests.cs
    - tests/BlazorCanvas.Tests/Data/FigureStoreTests.cs

key-decisions:
  - "z-index: 10 chosen for the fixed .toolbar rule — comfortably below the existing save-failed modal/backdrop layers (100/101), keeping the modal always on top while the toolbar stays above scrolling canvas content."
  - "GeometryCodecTests off-canvas RoundTripCases and FigureStoreTests off-canvas InlineData rows bumped to values clearly past 5760/2634 (not merely past the old 1472/828) so the off-canvas assertions remain genuinely off-canvas under the new bounds."

requirements-completed: [CANV-04, CANV-05, TEST-03]

coverage:
  - id: D1
    description: "CanvasBounds.Width = 5760, CanvasBounds.Height = 2634 (grow-only, D-19); Home.razor unchanged (still binds width/height to the constants)"
    requirement: CANV-04
    verification:
      - kind: unit
        ref: "tests/BlazorCanvas.Tests/Geometry/NormalisationTests.cs#CanvasBoundsTests.Bounds_AreTheFixed5760x2634Canvas"
        status: pass
      - kind: other
        ref: "dotnet build BlazorCanvas.sln (0 warnings, 0 errors)"
        status: pass
    human_judgment: false
  - id: D2
    description: ".toolbar is position: fixed (top:0, left:0, z-index:10, below the 100/101 save-failed layers); .canvas-area carries padding-top: 48px so the PageY-48 mapping is preserved; CanvasCoordinates.FromPage untouched"
    requirement: CANV-05
    verification:
      - kind: unit
        ref: "tests/BlazorCanvas.Tests/Geometry/CanvasCoordinatesTests.cs#FromPage_InclusiveFarCorner_MapsToWidthHeight (FromPage(5760,2682) -> (5760,2634))"
        status: pass
      - kind: other
        ref: "grep -rc viewBox src/BlazorCanvas/Components/Pages/Home.razor == 0"
        status: pass
    human_judgment: true
    rationale: "Fixed-toolbar visual behavior during native scroll and figure-under-pointer pixel accuracy at scroll offsets require live browser verification — gated to plan 11-02's human-verify checkpoint per this plan's own success_criteria."
  - id: D3
    description: "Test suite re-pinned to the enlarged 5760x2634 surface: renamed CanvasBoundsTests fact, re-pinned far-corner and off-canvas cases across five files; no stale 1472/828 or 1467/823 literals remain; fixtures untouched; full suite green"
    requirement: TEST-03
    verification:
      - kind: unit
        ref: "dotnet test BlazorCanvas.sln (416/416 passing)"
        status: pass
      - kind: other
        ref: "grep -nE \"1472|828\" across the five updated test files returns no lines; grep -nE \"1467|823\" DrawGestureTests.cs returns no lines"
        status: pass
    human_judgment: false

duration: 12min
completed: 2026-07-27
status: complete
---

# Phase BC-11 Plan 01: Grow canvas, fix toolbar, re-pin tests Summary

**Grew the fixed SVG canvas from 1472x828 to 5760x2634 via two constants, made the toolbar `position: fixed` with a compensating 48px canvas top offset to preserve the PageY-48 coordinate mapping, and re-pinned five test files' hard-coded bounds/comments to the enlarged surface — build clean, 416/416 tests green.**

## Performance

- **Duration:** 12 min
- **Started:** 2026-07-27T12:55:00Z (approx, first task commit)
- **Completed:** 2026-07-27T12:58:17Z
- **Tasks:** 3
- **Files modified:** 8

## Accomplishments
- `CanvasBounds.Width`/`Height` grown from 1472/828 to 5760/2634 — pure growth (D-19), Home.razor unchanged since it already binds the SVG to the constants
- `.toolbar` taken out of document flow (`position: fixed; top: 0; left: 0; z-index: 10`) while `.canvas-area` gains `padding-top: 48px`, keeping `canvasY = PageY - 48` correct at any scroll position with zero JS interop
- Five test files re-pinned to the new 5760x2634 bounds (renamed `CanvasBoundsTests` fact, far-corner `FromPage` case, `DrawGestureTests` far-corner + grid points, `GeometryCodecTests` off-canvas round-trip rows, `FigureStoreTests` off-canvas move rows) with no stale 1472/828 literals remaining and fixtures untouched

## Task Commits

Each task was committed atomically:

1. **Task 1: Grow CanvasBounds to 5760x2634 (CANV-04)** - `2362c79` (feat)
2. **Task 2: Fix the toolbar in place and offset the canvas 48px (CANV-05)** - `bdee93b` (feat)
3. **Task 3: Re-pin the test suite to the enlarged 5760x2634 surface (TEST-03)** - `3372a5f` (test)

**Plan metadata:** committed separately after this SUMMARY (docs: complete plan)

## Files Created/Modified
- `src/BlazorCanvas/Geometry/CanvasBounds.cs` - Width 1472->5760, Height 828->2634
- `src/BlazorCanvas/Components/Canvas/Toolbar.razor.css` - `.toolbar` gains `position: fixed; top: 0; left: 0; z-index: 10`
- `src/BlazorCanvas/Components/Pages/Home.razor.css` - `.canvas-area` gains `padding-top: 48px`
- `tests/BlazorCanvas.Tests/Geometry/NormalisationTests.cs` - renamed/re-pinned `CanvasBoundsTests` fact
- `tests/BlazorCanvas.Tests/Geometry/CanvasCoordinatesTests.cs` - far-corner `FromPage` re-pinned to (5760, 2682) -> (5760, 2634)
- `tests/BlazorCanvas.Tests/Geometry/DrawGestureTests.cs` - far-corner comment/values and `GridPoints` boundary points re-pinned
- `tests/BlazorCanvas.Tests/Geometry/GeometryCodecTests.cs` - off-canvas `RoundTripCases` anchors bumped past new bounds
- `tests/BlazorCanvas.Tests/Data/FigureStoreTests.cs` - off-canvas `MoveAsync` InlineData rows bumped past new bounds

## Decisions Made
- Chose `z-index: 10` for the fixed toolbar — well below the existing `save-failed-backdrop`/`save-failed-modal` layers (100/101), so the toolbar sits above scrolling canvas content but never obscures the save-failure modal.
- Bumped GeometryCodecTests/FigureStoreTests off-canvas fixture values to anchors genuinely past the new 5760/2634 edge (not just past the old 1472/828 edge), so those "off-canvas" assertions stay true under the enlarged bounds.

## Deviations from Plan

None - plan executed exactly as written. The only environment action beyond the plan's file edits was starting the already-configured `canvas-postgres` Docker container (Docker Desktop was not running) so `dotnet test` could reach PostgreSQL for the database/migration test suites — this is routine environment setup, not a code deviation.

## Issues Encountered
- `dotnet test` initially failed 54/416 tests with `Failed to connect to 127.0.0.1:5433` because Docker Desktop was not running. Started Docker Desktop, then `docker compose up -d` brought up the existing `canvas-postgres` container (host 5433 -> container 5432, per D-27/D-58); re-ran the full suite afterward with all 416 tests passing.

## User Setup Required

None - no external service configuration required. (Docker Compose Postgres must be running locally for `dotnet test`, per existing project constraints D-01/D-27 — no new setup introduced by this plan.)

## Next Phase Readiness
- CANV-04 and TEST-03 are fully satisfied and automatically verified (build clean, 416/416 tests green).
- CANV-05's code-level requirements (fixed toolbar CSS, preserved PageY-48 mapping, no JS interop) are in place and unit-verified via the re-pinned `FromPage` far-corner test; the remaining visual/interactive confirmation (toolbar staying visible during scroll, figure-under-pointer accuracy at scroll offsets) is explicitly gated to plan 11-02's human-verify checkpoint, per this plan's `success_criteria`.
- No blockers for 11-02.

---
*Phase: BC-11-canvas-enlargement-to-5760-2634*
*Completed: 2026-07-27*

## Self-Check: PASSED

All created/modified files verified present on disk; all four commits (2362c79, bdee93b, 3372a5f, d6c9cc4) verified in git log.
