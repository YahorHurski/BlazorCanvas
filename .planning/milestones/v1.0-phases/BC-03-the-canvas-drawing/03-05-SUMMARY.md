---
phase: 03-the-canvas-drawing
plan: 05
subsystem: ui
tags: [blazor, canvas, svg, drawing, persistence, postgres]

requires:
  - phase: 03-01
    provides: DrawGesture and CanvasCoordinates for clamped page-to-canvas drawing
  - phase: 03-02
    provides: FigureStore.InsertAsync and ordered per-user figure loading
  - phase: 03-03
    provides: Toolbar tool mapping and FigureShape preview rendering
  - phase: 03-04
    provides: Home canvas assembly and authenticated user figure loading
provides:
  - Live draw gesture preview for all shape tools
  - Release/leave commit path that inserts and appends database-assigned figures
  - Silent MinSizeGuard rejection for degenerate draws
affects: [phase-04-selection-and-moving, phase-05-realtime-and-failure-handling]

tech-stack:
  added: []
  patterns:
    - Blazor SVG pointer handlers route PageX/PageY through CanvasCoordinates.FromPage
    - DrawGesture owns all clamp and geometry construction
    - CommitAsync sets drawing=false before awaiting persistence

key-files:
  created:
    - .planning/phases/BC-03-the-canvas-drawing/03-05-SUMMARY.md
  modified:
    - src/BlazorCanvas/Components/Pages/Home.razor
    - src/BlazorCanvas/Components/Pages/Home.razor.css

key-decisions:
  - "CommitAsync captures preview type and box after setting drawing=false, before the first await, to prevent duplicate inserts from release/leave races."
  - "Degenerate draw rejection remains silent via MinSizeGuard; no UI feedback or catch path was added."

patterns-established:
  - "Home.razor is a coordinator only: CanvasCoordinates.FromPage -> DrawGesture.Build -> MinSizeGuard.IsDrawable -> FigureStore.InsertAsync -> figures.Add."
  - "Live previews render as the final SVG child with FigureShape Preview=true so they paint above existing figures without entering persisted state."

requirements-completed:
  - FIG-01
  - DATA-01

coverage:
  - id: D1
    description: "Shape tools draw with a live preview using page-relative coordinates routed through DrawGesture."
    requirement: FIG-01
    verification:
      - kind: other
        ref: "source assertions: handlers=4, FromPage=2, DrawGesture.Build=2, pointerout/OffsetX/OffsetY/manual geometry=0"
        status: pass
      - kind: other
        ref: "dotnet build BlazorCanvas.sln"
        status: pass
    human_judgment: true
    rationale: "Visual preview position, opacity, cursor feel, and shape appearance require screen confirmation. Approved by human checkpoint."
  - id: D2
    description: "Draw release and canvas leave commit persisted figures through InsertAsync and append the database-assigned entity."
    requirement: FIG-01
    verification:
      - kind: other
        ref: "source assertions: MinSizeGuard.IsDrawable=1, Figures.InsertAsync(userId)=1, await CommitAsync()=3, StateHasChanged=0"
        status: pass
      - kind: other
        ref: "dotnet test tests/BlazorCanvas.Tests/BlazorCanvas.Tests.csproj"
        status: pass
    human_judgment: true
    rationale: "The real-screen commit-on-leave and visible persistence flow was verified through the human checkpoint."
  - id: D3
    description: "Reload preserves persisted drawing order and cross-user isolation remains intact."
    requirement: DATA-01
    verification:
      - kind: other
        ref: "dotnet test tests/BlazorCanvas.Tests/BlazorCanvas.Tests.csproj"
        status: pass
    human_judgment: true
    rationale: "Overlap/occlusion after F5 and second-user empty canvas require browser verification. Approved by human checkpoint."

# Metrics
duration: 45 min
completed: 2026-07-16
status: complete
---

# Phase 03: The Canvas Drawing Plan 05 Summary

**Blazor SVG drawing now previews live, commits on release or canvas leave, and persists database-assigned figures immediately.**

## Performance

- **Duration:** 45 min
- **Started:** 2026-07-16T16:10:00+02:00
- **Completed:** 2026-07-16T16:56:01+02:00
- **Tasks:** 3
- **Files modified:** 2

## Accomplishments

- Wired the canvas pointer release and leave handlers into a single commit path for active draw gestures.
- Added silent per-type minimum-size rejection before persistence, preserving legal horizontal and vertical lines while rejecting degenerate rectangles, circles, and triangles.
- Inserted completed figures through `FigureStore.InsertAsync(userId, type, box)` and appended the returned entity only after the database assigned its id.
- Completed the required real-screen human verification for drawing, clamping, circle shape, drawing over existing figures, reload order, and user isolation.

## Task Commits

Each task was committed atomically:

1. **Task 1: The draw gesture state machine and the live preview** - `6f8a36f` (feat)
2. **Task 2: The commit path - silent rejection, INSERT, then the database-assigned id** - `ba94536` (feat)
3. **Task 3: Human verification - the phase's five success criteria on a real screen** - approved by checkpoint response

**Plan metadata:** pending docs commit

## Files Created/Modified

- `src/BlazorCanvas/Components/Pages/Home.razor` - Active draw gestures now commit through MinSizeGuard and FigureStore, then append the saved figure.
- `src/BlazorCanvas/Components/Pages/Home.razor.css` - Canvas surface carries `user-select: none` from the preview task.
- `.planning/phases/BC-03-the-canvas-drawing/03-05-SUMMARY.md` - Plan execution summary and coverage metadata.

## Decisions Made

- `drawing = false` stays at the top of `CommitAsync`, before any await, so pointerup/pointerleave cannot double-insert the same gesture.
- Rejected draws remain silent; this follows D-50 and D-32 and keeps database-failure handling for Phase 5.

## Deviations from Plan

None - plan executed exactly as written.

**Total deviations:** 0 auto-fixed.
**Impact on plan:** No scope creep. The only non-plan artifact touched by the interrupted subagent was restored before commit.

## Issues Encountered

- The first `gsd-executor` subagent was interrupted after partial work and did not write `03-05-SUMMARY.md`. The orchestrator closed it, inspected the partial patch, restored import scope, finished the plan inline, and committed the remaining production change.
- The source assertion `grep -c "try\|catch"` is overly broad in this file because `Geometry` contains the substring `try`. Exact inspection found no `try` or `catch` statement.

## Verification

- `dotnet build BlazorCanvas.sln` - passed, 0 warnings, 0 errors.
- `dotnet test tests/BlazorCanvas.Tests/BlazorCanvas.Tests.csproj` - passed, 388 passed, 0 failed, 0 skipped.
- Source assertions - passed for pointer handlers, page-relative coordinates, DrawGesture usage, no pointerout/OffsetX/OffsetY, no component geometry reimplementation, no StateHasChanged, MinSizeGuard gate, InsertAsync call, and commit call count.
- Human verification checkpoint - approved.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

Phase BC-03 now has summaries for all five plans and is ready for phase-level verification. Phase 4 can build selection and movement on top of the committed draw path.

---
*Phase: 03-the-canvas-drawing*
*Completed: 2026-07-16*