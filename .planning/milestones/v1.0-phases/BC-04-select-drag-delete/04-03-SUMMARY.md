---
phase: 04-select-drag-delete
plan: 03
subsystem: ui
tags: [blazor, svg, selection, drag, delete, pointer-events]

requires:
  - phase: 04-select-drag-delete
    provides: FigureStore UpdateAsync/DeleteAsync and FigureShape/Toolbar selection/delete hooks
provides:
  - Local figure selection with empty-canvas deselect and DOM-based topmost hit selection
  - Page-spanning drag state machine with 3px threshold, Movement.ClampMove edge clamping, and one UPDATE on drop
  - Delete button enablement and optimistic local delete using FigureStore.DeleteAsync
  - Grab/grabbing cursor affordance through CSS-isolated deep selectors
affects: [04-select-drag-delete, phase-05-live-sync]

tech-stack:
  added: []
  patterns:
    - Wrapper-level pointer handlers for drag termination without JavaScript
    - Press-time original box retained for future rollback
    - Optimistic local UI apply before persistence await

key-files:
  created:
    - .planning/phases/BC-04-select-drag-delete/04-03-SUMMARY.md
  modified:
    - src/BlazorCanvas/Components/Pages/Home.razor
    - src/BlazorCanvas/Components/Pages/Home.razor.css

key-decisions:
  - "Selection remains per-circuit UI state only; topmost selection relies on SVG DOM paint order rather than a C# hit-test."
  - "Drag uses Movement.ClampMove with the original press-time box and total delta, preserving Phase 5 rollback state."
  - "Delete ignores the affected-row count by design because D-10 only requires branching on zero-row UPDATE, not DELETE."

patterns-established:
  - "Home.razor now keeps drag state separate from draw state: SVG handlers own drawing, app-shell handlers own dragging."
  - "CommitDragAsync clears dragging and captures locals before the first await to avoid duplicate drop writes."

requirements-completed:
  - FIG-02
  - FIG-03
  - FIG-04

coverage:
  - id: D1
    description: "Pointer-tool figure presses select the clicked figure, empty-canvas presses clear selection, and no hit-test/z-order resolver is implemented."
    requirement: FIG-02
    verification:
      - kind: other
        ref: "source assertions: selectedId count 1, FigureShape Selected/Selectable/OnPointerDown counts 1 each, selectedId=null count 3, topmost resolver grep 0"
        status: pass
      - kind: other
        ref: "dotnet build BlazorCanvas.sln"
        status: pass
    human_judgment: true
    rationale: "Automated source checks prove wiring and absence of a resolver; actual topmost click behavior requires browser interaction with overlapping SVG figures."
  - id: D2
    description: "Drag starts from figure press, uses a 3px Euclidean threshold, clamps movement through Movement.ClampMove, and writes exactly once on drop."
    requirement: FIG-03
    verification:
      - kind: other
        ref: "source assertions: app-shell wrapper handlers 3, SVG draw handlers 4, Movement.ClampMove(dragOriginalBox, dx, dy) count 1, Figures.UpdateAsync(userId...) count 1, pointerout/OffsetX/OffsetY grep 0"
        status: pass
      - kind: integration
        ref: "dotnet test tests/BlazorCanvas.Tests/BlazorCanvas.Tests.csproj (395 passed)"
        status: pass
    human_judgment: true
    rationale: "Source and tests prove the wiring and existing geometry/store contracts; live drag feel, Alt-Tab/release-outside behavior, and exact browser event delivery need manual UI verification."
  - id: D3
    description: "Zero-row UPDATE silently removes the local figure and clears selection without messages, retries, broadcasts, or exception handling."
    requirement: FIG-03
    verification:
      - kind: other
        ref: "source assertions: affected == 0 count 1, selectedId=null count 3, try/catch grep 0, notifier/broadcast/retry grep 0"
        status: pass
    human_judgment: false
  - id: D4
    description: "Delete button is enabled only when selectedId exists and removes the selected figure through FigureStore.DeleteAsync with no keyboard or confirmation path."
    requirement: FIG-04
    verification:
      - kind: other
        ref: "source assertions: DeleteEnabled selectedId.HasValue count 1, OnDelete HandleDeleteAsync count 1, Figures.DeleteAsync(userId...) count 1, !selectedId.HasValue count 1, @onkeydown/tabindex/confirm/window.alert grep 0"
        status: pass
      - kind: other
        ref: "dotnet build BlazorCanvas.sln"
        status: pass
    human_judgment: true
    rationale: "Source checks prove callback wiring and forbidden paths; button disabled/enabled behavior and immediate disappearance need browser verification."
  - id: D5
    description: "Pointer-tool figures show grab/grabbing cursors while preserving Phase 3 crosshair and user-select rules."
    requirement: FIG-03
    verification:
      - kind: other
        ref: "source assertions: CSS ::deep count 2, is-dragging CSS count 3, cursor grab/grabbing/crosshair counts 1 each, user-select count 1, hex color count 2"
        status: pass
    human_judgment: true
    rationale: "CSS selector presence is automated; actual cursor rendering requires browser verification."

duration: 50min
completed: 2026-07-16
status: complete
---

# Phase 04 Plan 03: Selection, Drag and Delete State Machine Summary

**Local selection, page-spanning drag commits, clamped drop persistence, and immediate toolbar deletion on the canvas page**

## Performance

- **Duration:** 50 min
- **Started:** 2026-07-16T19:45:00+02:00
- **Completed:** 2026-07-16T20:35:00+02:00
- **Tasks:** 3
- **Files modified:** 2

## Accomplishments

- Wired `Home.razor` selection state into looped `FigureShape` instances, including primary-button-only empty-canvas deselect.
- Added the page-spanning `.app-shell` drag surface and wrapper pointer handlers while preserving the SVG draw handlers.
- Implemented drag press state, the 3px Euclidean threshold, live movement through `Movement.ClampMove`, zero-row UPDATE cleanup, and one `FigureStore.UpdateAsync` call on drop.
- Wired the Delete button to selected state and `HandleDeleteAsync`, with optimistic local removal and no keyboard/confirmation path.
- Added CSS-isolated grab/grabbing cursor rules using deep selectors while preserving Phase 3 cursor and color rules.

## Task Commits

Each task was committed atomically:

1. **Task 1: Selection — click to select, click empty canvas to deselect, topmost wins** - `46ed7a1` (feat)
2. **Task 2: The drag — page-spanning surface, threshold, edge clamp, one UPDATE on drop** - `c1fd46c` (feat)
3. **Task 3: Delete, and the grab/grabbing cursor affordance** - `8a36aea` (feat)

**Plan metadata:** committed after summary/state updates.

## Files Created/Modified

- `src/BlazorCanvas/Components/Pages/Home.razor` - Adds local selection, drag state, wrapper drag handlers, commit/delete methods, and toolbar/figure hook wiring.
- `src/BlazorCanvas/Components/Pages/Home.razor.css` - Adds `.app-shell` and grab/grabbing cursor rules with CSS isolation support.
- `.planning/phases/BC-04-select-drag-delete/04-03-SUMMARY.md` - Records this plan's completion and coverage metadata.

## Decisions Made

- Selection remains local UI state only; no schema, query, or broadcast was added.
- Drag movement is rendered from the existing `dragCurrentBox` only while `dragging`, and committed coordinates are copied into the in-memory figure before the update await.
- `HandleDeleteAsync` deliberately ignores the delete affected-row count because a ghost delete is idempotent under D-10.

## Deviations from Plan

None - plan executed exactly as written.

**Total deviations:** 0 auto-fixed.
**Impact on plan:** No scope change.

## Issues Encountered

- The Windows sandbox helper was missing, so several read/edit/assertion commands required approved escalated PowerShell execution. Changes remained scoped to the planned files.
- One attempted parallel build/test verification caused a transient file lock on `BlazorCanvas.dll`; rerunning the test sequentially passed.

## Verification

- `docker compose up -d` passed; `canvas-postgres` was already running.
- `dotnet build BlazorCanvas.sln` passed with 0 warnings and 0 errors after each task and at plan close.
- `dotnet test tests/BlazorCanvas.Tests/BlazorCanvas.Tests.csproj` passed: 395 total, 395 passed, 0 failed, 0 skipped.
- Task 1 source assertions passed: selected field/hook counts, preview unchanged, two primary-button guards, no hit-test/z-order resolver, `@key="f.Id"` unchanged.
- Task 2 source assertions passed: wrapper handlers count 3, SVG draw handlers count 4, `pointerout` count 0, `Movement.ClampMove(dragOriginalBox, dx, dy)` count 1, `Figures.UpdateAsync(userId...)` count 1, `affected == 0` count 1, `OffsetX`/`OffsetY` count 0.
- Task 3 source assertions passed: delete wiring counts, `selectedId = null` count 3, `is-dragging` Home count 1/CSS count 3, `::deep` count 2, cursor declarations and color counts unchanged, keyboard/confirmation/try/catch greps 0.

## Known Stubs

None.

## Threat Flags

None. This plan used the threat surfaces already registered in PLAN.md: pointer events into the circuit and owner-scoped update/delete calls into `FigureStore`.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

Ready for 04-04 human verification on a real screen. Browser UAT still needs to confirm overlapping-figure selection, drag feel at edges, release-outside/Alt-Tab behavior, Delete button state, and persistence after F5.

## Self-Check: PASSED

- Summary file exists at `.planning/phases/BC-04-select-drag-delete/04-03-SUMMARY.md`.
- Task commits exist: `46ed7a1`, `c1fd46c`, `8a36aea`.
- Modified source files exist: `src/BlazorCanvas/Components/Pages/Home.razor`, `src/BlazorCanvas/Components/Pages/Home.razor.css`.
- Final build, test, and source assertion gates passed as recorded above.

---
*Phase: 04-select-drag-delete*
*Completed: 2026-07-16*
