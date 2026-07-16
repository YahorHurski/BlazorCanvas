---
phase: BC-05-live-cross-tab-sync
plan: 04
subsystem: ui-sync
tags: [blazor-server, cross-tab-sync, rollback, modal, css-isolation]
requires:
  - phase: BC-05 plan 03
    provides: Home.razor sync wiring, SyncMessage.Rollback, CanvasSyncNotifier, optimistic glide broadcasts
provides:
  - Save-failure modal with locked DATA-04 copy and OK-triggered database reload
  - Failed drag rollback broadcast using the retained original coordinates
  - Failed draw, drag, and delete write handling that keeps the circuit alive
affects: [BC-05-live-cross-tab-sync, DATA-04, SYNC-01]
tech-stack:
  added: []
  patterns:
    - Scoped write-path catch blocks after provider retry exhaustion
    - Native dialog rendered outside app-shell with fixed overlay CSS
key-files:
  created:
    - .planning/phases/BC-05-live-cross-tab-sync/05-04-SUMMARY.md
  modified:
    - src/BlazorCanvas/Components/Pages/Home.razor
    - src/BlazorCanvas/Components/Pages/Home.razor.css
key-decisions:
  - "Failed drag rollback uses dragOriginalBox as the forced D-52 payload; the failed final box is never broadcast as rollback truth."
  - "Draw and delete failures show the same locked modal but do not broadcast rollback because neither announces cross-tab state before its write succeeds."
patterns-established:
  - "Save failure UI is the only visible error surface for DATA-04 and is rendered outside .app-shell to preserve D-43 geometry."
requirements-completed: [DATA-04]
coverage:
  - id: D1
    description: "Failed drag restores the acting tab locally and broadcasts rollback with the retained original box."
    requirement: DATA-04
    verification:
      - kind: other
        ref: "Select-String SyncMessage.Rollback(figureId.Value, dragOriginalBox, _sender) count = 1"
        status: pass
      - kind: other
        ref: "dotnet test BlazorCanvas.sln (405 passed)"
        status: pass
    human_judgment: true
    rationale: "The two-tab stopped-database rollback behavior is intentionally verified in plan 05-05 on real screens."
  - id: D2
    description: "Failed draw, drag, and delete writes show exactly one locked modal string and keep the circuit alive."
    requirement: DATA-04
    verification:
      - kind: other
        ref: "showSaveFailedModal = true count = 3; catch count = 4; locked string count = 1"
        status: pass
      - kind: other
        ref: "dotnet build BlazorCanvas.sln (0 warnings, 0 errors)"
        status: pass
    human_judgment: true
    rationale: "Actual database-outage UI behavior and circuit survival require interactive verification in 05-05."
  - id: D3
    description: "The modal is out of document flow, has only the OK click path, and uses login-card tokens with no motion properties."
    requirement: DATA-04
    verification:
      - kind: other
        ref: "modal @onclick count = 1; position: fixed count = 2; transition/animation count = 0"
        status: pass
    human_judgment: true
    rationale: "Layout position, autofocus, Escape/backdrop behavior, and visual comparison are reserved for the human verification checkpoint."
duration: 35min
completed: 2026-07-17
status: complete
---

# Phase BC-05 Plan 04: Save-Failure Rollback and Modal Summary

**DATA-04 save failures now restore cross-tab truth with rollback and a forced database reload modal.**

## Performance

- **Duration:** 35 min
- **Started:** 2026-07-17T00:00:00Z
- **Completed:** 2026-07-17T00:35:00Z
- **Tasks:** 3
- **Files modified:** 2

## Accomplishments

- Added the locked save-failure modal outside `.app-shell`, with the exact required copy and a single OK button that reloads figures from PostgreSQL and clears selection.
- Wrapped all three write paths so final write failures show the modal and keep the Blazor circuit alive.
- Added forced failed-drag recovery: restore the acting tab to `dragOriginalBox` and publish `SyncMessage.Rollback(figureId.Value, dragOriginalBox, _sender)` for other tabs.
- Appended fixed-position modal CSS using the login card and CTA tokens, with no CSS motion properties.

## Task Commits

1. **Task 1-3: modal, rollback write paths, and modal CSS** - `50895cc` (feat)

**Plan metadata:** pending docs commit

## Files Created/Modified

- `src/BlazorCanvas/Components/Pages/Home.razor` - Adds `showSaveFailedModal`, modal markup, `ReloadFromDatabaseAsync`, and scoped write failure handling for draw, drag, and delete.
- `src/BlazorCanvas/Components/Pages/Home.razor.css` - Adds fixed overlay/modal styles sourced from `Login.razor.css` tokens.
- `.planning/phases/BC-05-live-cross-tab-sync/05-04-SUMMARY.md` - This execution summary.

## Verification

- `dotnet build BlazorCanvas.sln` - PASS, 0 warnings, 0 errors.
- `dotnet test BlazorCanvas.sln` - PASS, 405 passed, 0 failed, 0 skipped.
- Locked string count - PASS, exactly 1.
- Superseded D-45 string absence - PASS, count 0.
- Dialog and backdrop source assertions - PASS, each count 1.
- Modal click handler count - PASS, exactly 1 `@onclick` in the modal block.
- `Notifier.Publish` count - PASS, exactly 6.
- Rollback payload assertion - PASS, exactly one `SyncMessage.Rollback(figureId.Value, dragOriginalBox, _sender)`.
- Modal trigger count - PASS, exactly 3 `showSaveFailedModal = true;` assignments.
- Catch count - PASS, exactly 4 catch blocks including the existing remote-message disposal guard.
- Zero-row branch modal count - PASS, 0.
- No hand-rolled retry - PASS, count 0 for retry-loop patterns.
- No JS/csproj diffs - PASS, both diff-stat checks produced no output.
- Out-of-scope files untouched - PASS, no diffs in `FigureStore.cs`, `app.css`, toolbar CSS, login CSS, or reconnect modal CSS.

## Decisions Made

- None new. Followed locked D-52/UI-05-01/UI-05-03/UI-05-04 decisions exactly.

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered

- The Windows sandbox helper executable was missing, so `apply_patch` and some read-only shell assertions failed before execution. I reran the scoped edits and verification commands with approved escalation. No project behavior changed because of this tooling issue.

## Known Stubs

None.

## Threat Flags

None.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

Ready for `05-05-PLAN.md`, the human verification checkpoint. The remaining checks are intentionally visual/interactive: two-tab rollback behavior, modal autofocus, Escape/backdrop dismissal behavior, and layout confirmation while the modal is open.

## Self-Check: PASSED

- Summary file exists.
- Production commit `50895cc` exists.
- No unexpected tracked file deletions were introduced.

---
*Phase: BC-05-live-cross-tab-sync*
*Completed: 2026-07-17*