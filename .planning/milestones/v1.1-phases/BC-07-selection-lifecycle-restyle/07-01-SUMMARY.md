---
phase: BC-07-selection-lifecycle-restyle
plan: "01"
subsystem: ui
tags: [blazor, svg, selection, razor]
requires:
  - phase: BC-06-canvas-resize
    provides: 1472x828 canvas bounds and SVG surface
provides:
  - Local one-at-a-time selection lifecycle with draw auto-select and toolbar deselection
  - Topmost blue-over-white dashed SVG selection trace for every figure type
affects: [BC-07-02-human-verification, selection-ui]
tech-stack:
  added: []
  patterns: [topmost SVG overlay via document order, local selection state]
key-files:
  created: [src/BlazorCanvas/Components/Canvas/SelectionTrace.razor]
  modified: [src/BlazorCanvas/Components/Canvas/FigureShape.razor, src/BlazorCanvas/Components/Canvas/Toolbar.razor, src/BlazorCanvas/Components/Pages/Home.razor]
key-decisions:
  - "Selection trace is a final SVG child so SVG document order guarantees topmost paint."
  - "Selection remains local UI state and is never persisted or synchronized."
patterns-established:
  - "Selection overlays reproduce each figure geometry and ignore pointer events."
requirements-completed: [SEL-01, SEL-02]
coverage:
  - id: D1
    description: Local selection lifecycle keeps the tool armed, selects a completed draw, and clears through the required routes.
    requirement: SEL-01
    verification:
      - kind: other
        ref: dotnet test BlazorCanvas.sln --nologo
        status: pass
    human_judgment: true
    rationale: Interactive pointer and toolbar behavior requires the dedicated 07-02 browser verification.
  - id: D2
    description: A topmost blue-and-white dashed trace follows the selected figure geometry.
    requirement: SEL-02
    verification:
      - kind: other
        ref: dotnet build BlazorCanvas.sln
        status: pass
    human_judgment: true
    rationale: Visual paint order and multi-tab behavior require the dedicated 07-02 browser verification.
duration: 15min
completed: 2026-07-21
status: complete
---

# Phase BC-07 Plan 01: Selection Lifecycle & Restyle Summary

**Local draw selection and a topmost blue-and-white dashed SVG trace replace the previous red selection outline.**

## Performance

- **Duration:** 15 min
- **Started:** 2026-07-20T23:42:00Z
- **Completed:** 2026-07-20T23:57:26Z
- **Tasks:** 3
- **Files modified:** 4

## Accomplishments

- Drawing now selects the persisted figure without disarming its active tool; toolbar clicks clear selection except Delete.
- `SelectionTrace` overlays the selected line, rectangle, circle, or triangle as the final inert SVG child.
- Figure outlines remain black; the dashed overlay provides the calm selection affordance.

## Task Commits

Each implementation task was committed atomically:

1. **Task 1: SEL-01 selection lifecycle** - `664063c` (feat)
2. **Task 2: SEL-02 restyle** - `e8c6c91` (feat)
3. **Task 3: Prove the gates** - verification only; no file changes

## Files Created/Modified

- `src/BlazorCanvas/Components/Canvas/SelectionTrace.razor` - inert, geometry-matched dual-stroke selection overlay.
- `src/BlazorCanvas/Components/Canvas/FigureShape.razor` - always renders committed figure outlines in black.
- `src/BlazorCanvas/Components/Canvas/Toolbar.razor` - exposes toolbar deselection while preserving Delete propagation behavior.
- `src/BlazorCanvas/Components/Pages/Home.razor` - owns local selection lifecycle and renders the final SVG trace layer.

## Decisions Made

- Used SVG document order, rather than CSS stacking, to ensure the selection trace paints over all figures.
- Kept selection local-only; no database schema or sync contract changes were introduced.

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered

The workspace patch helper could not launch because of a local sandbox setup failure. The identical scoped diff was applied with `git apply`; build and all gates verified the resulting files.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

Automated coverage is complete. Plan 07-02 remains ready for browser-based human verification of the interactive selection lifecycle, topmost paint order, and remote-delete edge case.

## Self-Check: PASSED

---
*Phase: BC-07-selection-lifecycle-restyle*
*Completed: 2026-07-21*
