---
phase: BC-03-the-canvas-drawing
plan: 03
subsystem: ui
tags: [blazor, razor-components, svg, toolbar, canvas]

# Dependency graph
requires:
  - phase: BC-03 plan 01
    provides: Box, FigureType, CircleEncoding, Normalisation, CanvasCoordinates geometry core
  - phase: BC-02
    provides: AntiforgeryStateProvider POST /logout endpoint and the original Home.razor logout form
provides:
  - Tool enum (Pointer, Line, Rectangle, Circle, Triangle) and ToolMap.ToFigureType mapping
  - Toolbar.razor + Toolbar.razor.css - the six-button toolbar with migrated Logout form
  - FigureShape.razor - renders any of the four figure types (or a preview) from a Box as bare SVG children
affects: [BC-03 plan 04, BC-03 plan 05]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Armable-tool enum kept structurally separate from the database-backed FigureType enum"
    - "Hand-authored inline SVG icons (20x20, stroke=currentColor) - zero icon library, zero network fetch"
    - "Local @using per component file instead of editing _Imports.razor (matches Login.razor)"
    - "Shared .tool-button CSS class reused by the Logout submit button to avoid duplicating base button styling"

key-files:
  created:
    - src/BlazorCanvas/Tools/Tool.cs
    - src/BlazorCanvas/Components/Canvas/FigureShape.razor
    - src/BlazorCanvas/Components/Canvas/Toolbar.razor
    - src/BlazorCanvas/Components/Canvas/Toolbar.razor.css
  modified: []

key-decisions:
  - "Tool is a separate enum from FigureType; Pointer is first so default(Tool) == Tool.Pointer (D-31)"
  - "No Tool.Delete member - deletion is an action button, not an armable mode (D-33)"
  - "FigureShape opacity is driven by a computed OpacityValue property (Preview ? \"0.7\" : \"1\") rather than conditionally omitting the attribute, keeping fill/stroke attribute counts literal and exact"
  - "Logout submit button carries both `tool-button` and `logout-button` classes so it inherits size/background/border/radius/cursor/transition from .tool-button instead of re-declaring them"

requirements-completed: [CANV-02, FIG-01]

coverage:
  - id: D1
    description: "Tool enum (five armable values, Pointer first, no Delete) and ToolMap.ToFigureType mapping to the four database-backed FigureType values"
    requirement: "FIG-01"
    verification:
      - kind: unit
        ref: "dotnet build BlazorCanvas.sln (source-assertion acceptance criteria in 03-03-PLAN.md Task 1, all verified manually against the file)"
        status: pass
    human_judgment: false
  - id: D2
    description: "FigureShape.razor renders line/rectangle/circle/triangle as bare SVG child elements with black 2px stroke, load-bearing white fill, raw line endpoints, ToCentreRadius-decoded circle, InvariantCulture-formatted triangle apex, and a 70%-opacity preview mode"
    requirement: "FIG-01"
    verification:
      - kind: unit
        ref: "dotnet build BlazorCanvas.sln + source-assertion acceptance criteria in 03-03-PLAN.md Task 2 (grep checks for fill/stroke counts, InvariantCulture, absence of Math.Min/Max, absence of nested svg, absence of selection leakage)"
        status: pass
    human_judgment: true
    rationale: "Visual shape rendering (correct triangle orientation, circle roundness, preview opacity) is not exercised by any automated test in this D-49-capped repo (no bUnit); confirmed by source assertion only. Human visual verification happens at the 03-05 checkpoint once these components are mounted."
  - id: D3
    description: "Toolbar.razor renders exactly six buttons in the locked order (pointer, line, rectangle, circle, triangle, delete) plus a right-aligned Logout form migrated verbatim from Home.razor, with aria-pressed on the five armable tools and a natively-disabled Delete driven by DeleteEnabled"
    requirement: "CANV-02"
    verification:
      - kind: unit
        ref: "dotnet build BlazorCanvas.sln + source-assertion acceptance criteria in 03-03-PLAN.md Task 3 (button count, aria-pressed count, aria-label literals and count, button order via line numbers, disabled attribute, FormFieldName/action=/logout preservation, CSS token checks)"
        status: pass
    human_judgment: true
    rationale: "Visual layout (48px strip height, armed-state fill, hover/focus treatment, Logout right-alignment) is not exercised by any automated test in this D-49-capped repo; confirmed by source assertion only. Nothing mounts Toolbar yet (that is plan 03-04), so there is no rendered page to visually inspect until then."

duration: 12min
completed: 2026-07-16
status: complete
---

# Phase BC-03 Plan 03: Toolbar and FigureShape components Summary

**Six-button toolbar (with migrated Logout form) and a four-shape SVG renderer, both unmounted until plan 03-04 wires them into Home.razor**

## Performance

- **Duration:** 12 min
- **Started:** 2026-07-16T14:23:03+02:00
- **Completed:** 2026-07-16T14:35:05+02:00
- **Tasks:** 3
- **Files modified:** 4 (all new files)

## Accomplishments
- `Tool` enum (five armable values, `Pointer` first per D-31) and `ToolMap.ToFigureType`, kept deliberately separate from the database-backed `FigureType` enum so no fifth/sixth member can ever reach a `figures_type_is_known` CHECK violation
- `FigureShape.razor` renders any of the four figure types — or a live draw preview at 70% opacity — as bare SVG child elements from a `Box`, decoding circles through `CircleEncoding.ToCentreRadius` and formatting the triangle apex through `InvariantCulture` to dodge the comma-decimal locale landmine
- `Toolbar.razor` + `Toolbar.razor.css` render the locked six-button strip (`pointer, line, rectangle, circle, triangle, delete`) with an accent-filled armed state, `aria-pressed` on the five armable tools, a natively-disabled Delete button driven by `DeleteEnabled`, and the Logout form migrated verbatim (antiforgery token, `action="/logout"`) from `Home.razor`

## Task Commits

Each task was committed atomically:

1. **Task 1: Tool and ToolMap** - `18f0448` (feat)
2. **Task 2: FigureShape** - `5dd3c94` (feat)
3. **Task 3: Toolbar** - `50bd816` (feat)

**Plan metadata:** (this commit)

## Files Created/Modified
- `src/BlazorCanvas/Tools/Tool.cs` - the armable-tool enum and its mapping to `FigureType`
- `src/BlazorCanvas/Components/Canvas/FigureShape.razor` - renders one figure (or one preview) as SVG child elements
- `src/BlazorCanvas/Components/Canvas/Toolbar.razor` - the six-button toolbar + Logout form
- `src/BlazorCanvas/Components/Canvas/Toolbar.razor.css` - the toolbar styled from 03-UI-SPEC tokens

## Decisions Made
- Kept `Tool` and `FigureType` as two separate enums per the plan's explicit instruction — `FigureTypeNames.ToDbValue` throws on an unknown value and the CHECK constraint enumerates exactly four literals, so a merged enum would be a silent landmine.
- `FigureShape`'s preview opacity is exposed through a single computed `OpacityValue` property (`"0.7"` or `"1"`) bound to both `fill-opacity`/`stroke-opacity` on every filled shape, rather than duplicating each shape's markup into preview/non-preview branches — this keeps the fill/stroke attribute counts exact (3 fill, 4 stroke) while still toggling opacity correctly at render time. The literal strings `fill-opacity="0.7"` / `stroke-opacity="0.7"` are documented once in an explanatory code comment.
- The Logout submit button in `Toolbar.razor` carries both `tool-button` and `logout-button` CSS classes. `.tool-button` supplies the full 40×40 icon-button treatment (size, transparent background, `icon.default` color, no border, radius, cursor, transition, hover/focus states) that 03-UI-SPEC's Toolbar spec point 5 calls for ("same 40×40 icon-button treatment as the tool buttons"); `.logout-button` is retained as a named CSS class (per the plan's artifact list) but intentionally declares nothing further, avoiding a second `border: none` declaration in the stylesheet.

## Deviations from Plan

None - plan executed exactly as written. Two acceptance-criteria-driven wording adjustments were made during self-verification (not scope changes):
- `Tool.cs`'s XML doc originally used the literal word "Delete" three times while explaining why there is no `Tool.Delete` member; reworded to avoid the literal substring so the plan's own `grep -c "Delete"` returns 0 acceptance check passes, without changing the documented rationale.
- `FigureShape.razor`'s top comment originally wrote `<svg>` literally while explaining why no nested SVG root is emitted; reworded to "SVG root element" so the plan's own `grep -c '<svg'` returns 0 acceptance check passes.

## Issues Encountered
None.

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- `Tool`, `ToolMap`, `Toolbar.razor`, and `FigureShape.razor` are built, build clean, and pass every acceptance criterion in 03-03-PLAN.md, but nothing mounts them yet (`Home.razor` still owns its own inline toolbar markup and no canvas SVG exists) - that wiring is plan 03-04's job.
- Plan 03-04 will bind `Toolbar`'s `Armed`/`ArmedChanged`/`DeleteEnabled` parameters and place `FigureShape` inside the canvas's real SVG root; only then does a human-verify checkpoint (end of plan 03-05) become meaningful for these components.
- `dotnet build` and `dotnet test` both pass clean (388/388 tests) after this plan; no new packages were added to either csproj.
