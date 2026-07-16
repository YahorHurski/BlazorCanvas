---
phase: 04-select-drag-delete
plan: 02
subsystem: ui
tags: [blazor, svg, selection, toolbar, pointer-events]

requires:
  - phase: 03-the-canvas-drawing
    provides: FigureShape renderer, Toolbar buttons, DeleteEnabled disabled state, and canvas draw handlers
provides:
  - Optional FigureShape Selected/Selectable/OnPointerDown presentation hooks
  - Selected figure stroke color switch using the inherited #B91C1C token
  - Toolbar OnDelete callback wired to the existing native-disabled Delete button
affects: [04-select-drag-delete]

tech-stack:
  added: []
  patterns:
    - Bound Blazor stopPropagation modifier for conditional figure press routing
    - Dumb presentation components receiving state from Home and reporting events upward

key-files:
  created:
    - .planning/phases/BC-04-select-drag-delete/04-02-SUMMARY.md
  modified:
    - src/BlazorCanvas/Components/Canvas/FigureShape.razor
    - src/BlazorCanvas/Components/Canvas/Toolbar.razor

key-decisions:
  - "FigureShape keeps selection as presentation only: Selected changes stroke color, while Selectable controls both event propagation and callback dispatch."
  - "Toolbar keeps the existing native disabled Delete button and only adds the OnDelete callback; no CSS, confirmation, or keyboard deletion path was added."

patterns-established:
  - "Child SVG shapes can bind @onpointerdown:stopPropagation to a bool parameter so pointer-tool presses are claimed while shape-tool presses fall through to the parent SVG."
  - "Toolbar destructive actions stay native-disabled until the parent supplies enabled state."

requirements-completed:
  - FIG-02
  - FIG-04

coverage:
  - id: D1
    description: "FigureShape renders selected figures with #B91C1C and unselected figures with #000000 at the same 2px stroke width."
    requirement: FIG-02
    verification:
      - kind: other
        ref: "source assertions: stroke=@StrokeColor count 4, #B91C1C count 1, #000000 count 1, stroke-width=2 count 4"
        status: pass
      - kind: other
        ref: "dotnet build BlazorCanvas.sln"
        status: pass
    human_judgment: false
  - id: D2
    description: "FigureShape claims pointer presses only when Selectable is true and otherwise lets presses fall through to the canvas."
    requirement: FIG-02
    verification:
      - kind: other
        ref: "source assertions: @onpointerdown HandlePointerDown count 4, stopPropagation=Selectable count 4, hardcoded stopPropagation count 0"
        status: pass
    human_judgment: false
  - id: D3
    description: "Toolbar Delete button raises OnDelete when enabled while preserving the native disabled attribute."
    requirement: FIG-04
    verification:
      - kind: other
        ref: "source assertions: OnDelete parameter count 1, OnDelete.InvokeAsync count 1, disabled=@(!DeleteEnabled) count 1"
        status: pass
      - kind: other
        ref: "dotnet test tests/BlazorCanvas.Tests/BlazorCanvas.Tests.csproj"
        status: pass
    human_judgment: false
  - id: D4
    description: "No Delete-key path, focus behavior, confirmation dialog, or Toolbar CSS change was introduced."
    requirement: FIG-04
    verification:
      - kind: other
        ref: "source assertions: @onkeydown/tabindex/Delete-key/confirm count 0, button count 7, Toolbar.razor.css diff empty"
        status: pass
    human_judgment: false

duration: 20min
completed: 2026-07-16
status: complete
---

# Phase 04 Plan 02: Presentation Hooks Summary

**Selectable SVG figures and a live Delete button callback without changing layout or CSS**

## Performance

- **Duration:** 20 min
- **Started:** 2026-07-16T19:20:00+02:00
- **Completed:** 2026-07-16T19:40:00+02:00
- **Tasks:** 2
- **Files modified:** 2

## Accomplishments

- Added optional `Selected`, `Selectable`, and `OnPointerDown` parameters to `FigureShape`.
- Replaced all four hardcoded black SVG strokes with `StrokeColor`, preserving 2px stroke width and fill/opacity behavior.
- Added bound `@onpointerdown:stopPropagation="Selectable"` and a guarded handler on every figure branch.
- Added optional `Toolbar.OnDelete` and wired the existing Delete button's click while preserving the native disabled attribute.

## Task Commits

Each task was committed atomically:

1. **Task 1: FigureShape selected outline and conditional press claim** - `5a17894` (feat)
2. **Task 2: Toolbar Delete button callback** - `b583cc7` (feat)

## Files Created/Modified

- `src/BlazorCanvas/Components/Canvas/FigureShape.razor` - Adds selected outline state and optional pointer press routing.
- `src/BlazorCanvas/Components/Canvas/Toolbar.razor` - Adds `OnDelete` and wires it to the existing Delete button.
- `.planning/phases/BC-04-select-drag-delete/04-02-SUMMARY.md` - Records this plan's completion.

## Decisions Made

None beyond the plan. The implementation followed the UI-SPEC: no new colors, no CSS changes, no confirmation dialog, and no keyboard deletion path.

## Deviations from Plan

None - plan executed exactly as written.

**Total deviations:** 0 auto-fixed.
**Impact on plan:** No scope change.

## Issues Encountered

None in implementation. The same Windows sandbox helper issue affected some read/assertion commands, so required assertions were rerun outside the sandbox.

## Verification

- `dotnet build BlazorCanvas.sln` passed with 0 warnings and 0 errors after each task.
- `dotnet test tests/BlazorCanvas.Tests/BlazorCanvas.Tests.csproj` passed: 395 total, 395 passed, 0 failed, 0 skipped.
- FigureShape assertions passed: `stroke="@StrokeColor"` count 4, `stroke="#000000"` count 0, `#000000` count 1, `#B91C1C` count 1, `stroke-width="2"` count 4, `@onpointerdown="HandlePointerDown"` count 4, `@onpointerdown:stopPropagation="Selectable"` count 4, hardcoded stopPropagation count 0, forbidden focus/click/key paths count 0, new parameter counts 1 each, `EditorRequired` count 2.
- Toolbar assertions passed: `OnDelete` parameter count 1, `OnDelete.InvokeAsync()` count 1, native disabled attribute count 1, `DeleteEnabled` parameter count 1, forbidden keyboard/confirmation terms count 0, button count 7, Toolbar CSS unchanged.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

Plan 04-03 can now wire `Home.razor` to these presentation hooks and the 04-01 write methods: figures can report pointer presses, selected figures can render red, and the toolbar can raise Delete.

---
*Phase: 04-select-drag-delete*
*Completed: 2026-07-16*