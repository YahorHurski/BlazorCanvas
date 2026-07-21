---
phase: BC-05-live-cross-tab-sync
plan: 05
subsystem: verification
tags: [manual-uat, blazor-server, cross-tab-sync, postgres, modal]
requires:
  - phase: BC-05 plan 01
    provides: SyncMessage contract and CanvasSyncNotifier isolation proof
  - phase: BC-05 plan 02
    provides: singleton notifier registration and bounded database retries
  - phase: BC-05 plan 03
    provides: Home.razor live sync wiring
  - phase: BC-05 plan 04
    provides: save-failure rollback modal and reload path
provides:
  - Human approval of all Phase 5 success criteria on real browser tabs
  - Measured one UPDATE per completed drag against PostgreSQL logs
  - Recorded option-a decision for UI-05-04 autofocus behavior
affects: [BC-05-live-cross-tab-sync, milestone-v1.0, UAT, DATA-04, SYNC-01]
tech-stack:
  added: []
  patterns:
    - Human checkpoint records measured browser behavior that automated tests cannot observe
    - UI contract amended when browser behavior differs from markup-only focus expectation
key-files:
  created:
    - .planning/phases/BC-05-live-cross-tab-sync/05-05-SUMMARY.md
  modified:
    - .planning/phases/BC-05-live-cross-tab-sync/05-UI-SPEC.md
    - src/BlazorCanvas/Components/Pages/Home.razor
key-decisions:
  - "Human verification chose option-a for UI-05-04: accept no guaranteed autofocus and keep the modal markup-only/no-JavaScript."
  - "The failed first checkpoint exposed a real throttle bug: the first mid-drag publish was suppressed by TickCount64 arithmetic from long.MinValue."
patterns-established:
  - "Set drag broadcast throttle state at drag start; never use sentinel values that can overflow elapsed-time checks."
  - "After forced database reload, publish the canonical reloaded snapshot so peer tabs converge too."
requirements-completed: [SYNC-01, DATA-03, DATA-04]
coverage:
  - id: D1
    description: "All five Phase 5 ROADMAP success criteria were verified on real same-user tabs."
    requirement: SYNC-01
    verification:
      - kind: manual_procedural
        ref: "User checkpoint response 2026-07-17: approved after retest of steps 3, 12, and 13"
        status: pass
    human_judgment: true
    rationale: "Two-tab visual glide, rollback, and recovery behavior cannot be proven by repository tests alone."
  - id: D2
    description: "PostgreSQL sees exactly one UPDATE for a whole drag."
    requirement: SYNC-01
    verification:
      - kind: manual_procedural
        ref: "Step 5/12 UPDATE count reported by user: 1"
        status: pass
    human_judgment: true
    rationale: "The required proof is a live database log measurement."
  - id: D3
    description: "Cross-tab failed-save rollback and OK reload recovery leave all screens agreeing with the database."
    requirement: DATA-04
    verification:
      - kind: manual_procedural
        ref: "User checkpoint response 2026-07-17: approved after fixes cd3d5ce and 08cd7b8"
        status: pass
    human_judgment: true
    rationale: "The claim is visual agreement across two live browser tabs during database outage and recovery."
  - id: D4
    description: "UI-05-04 autofocus clause resolved by explicit human decision."
    requirement: DATA-04
    verification:
      - kind: manual_procedural
        ref: "Step 14 failed: Enter did not activate OK; user selected option-a"
        status: pass
    human_judgment: true
    rationale: "The contract change is a human product/design decision, not an automated correctness question."
duration: 70min
completed: 2026-07-17
status: complete
---

# Phase BC-05 Plan 05: Human Verification Summary

**Real two-tab verification approved live cross-tab sync, failed-save recovery, one-UPDATE drag persistence, and the markup-only autofocus tradeoff.**

## Performance

- **Duration:** 70 min
- **Started:** 2026-07-17T00:45:00+02:00
- **Completed:** 2026-07-17T02:05:00+02:00
- **Tasks:** 2
- **Files modified:** 2

## Accomplishments

- Confirmed Phase 5's visual contract on real browser tabs after fixing the checkpoint failures: drag now glides in the second tab during the drag, failed-save rollback restores the second tab, and OK reload recovery synchronizes peers.
- Measured PostgreSQL statement logging for a multi-second drag: `UPDATE figures` count was exactly `1`.
- Recorded optional D-54 blanket mid-drag discard as skipped; the source gate remains the primary proof for that rule.
- Resolved UI-05-04's autofocus mismatch through explicit human choice: `option-a`, accept no guaranteed autofocus and amend `05-UI-SPEC.md` rather than add browser-focus code.

## Task Commits

1. **Gap fix: close live sync verification gaps** - `cd3d5ce` (fix)
2. **Gap fix: publish first drag movement** - `08cd7b8` (fix)
3. **Task 2: amend UI-05-04 and record human verdict** - pending docs commit

## Files Created/Modified

- `src/BlazorCanvas/Components/Pages/Home.razor` - Fixed drag publish throttling, routed SVG drag events through the drag handler, and broadcast the reloaded database snapshot after OK.
- `.planning/phases/BC-05-live-cross-tab-sync/05-UI-SPEC.md` - Amended UI-05-04 to option-a: autofocus is best-effort only, with no JavaScript or FocusAsync implementation.
- `.planning/phases/BC-05-live-cross-tab-sync/05-05-SUMMARY.md` - Records the human checkpoint verdict and measured outcomes.

## Human Verification Record

Initial checkpoint verdict: not approved.

Failed checks reported:

- Step 3: real-time drag synchronization failed; the second tab moved only after mouse release.
- Step 12: failed-save rollback was incorrect in the second tab; it remained at the dropped position.
- Step 13: recovery after database restart and OK was local-only; the peer tab did not update.
- Step 14: OK appeared focused/highlighted, but Enter did not activate it.

Measured outcomes:

- Step 5 / UPDATE count: `1`.
- Optional Step 10 / D-54 second-device check: skipped.
- Step 14 autofocus outcome: Enter did not activate OK.

Final checkpoint verdict after fixes `cd3d5ce` and `08cd7b8`: approved.

## Decisions Made

- Chose `option-a` for UI-05-04: accept no guaranteed autofocus and amend the UI spec. This keeps the modal markup-only and preserves the locked no-application-authored-JavaScript constraint.
- Left the native `autofocus` attribute as best-effort behavior; the contract no longer claims Enter works immediately.

## Deviations from Plan

### Auto-fixed Issues

**1. Live drag did not broadcast during movement**
- **Found during:** Human checkpoint Step 3.
- **Issue:** The second tab saw only the final move after mouse release.
- **Fix:** Routed SVG pointer move/up/leave through active drag handling so drag events are handled at the event target as well as the wrapper.
- **Files modified:** `src/BlazorCanvas/Components/Pages/Home.razor`
- **Verification:** `dotnet build BlazorCanvas.sln`, `dotnet test BlazorCanvas.sln`, and human retest approval.
- **Committed in:** `cd3d5ce`

**2. Peer tabs did not converge after failed-save recovery**
- **Found during:** Human checkpoint Steps 12 and 13.
- **Issue:** Peer tab could remain at an optimistic dropped position and did not update after OK reload.
- **Fix:** After database reload, publish deletes for removed figures and draw/move messages for the canonical reloaded snapshot.
- **Files modified:** `src/BlazorCanvas/Components/Pages/Home.razor`
- **Verification:** `dotnet build BlazorCanvas.sln`, `dotnet test BlazorCanvas.sln`, and human retest approval.
- **Committed in:** `cd3d5ce`

**3. First mid-drag publish was suppressed by throttle overflow**
- **Found during:** Second human checkpoint retest after `cd3d5ce`.
- **Issue:** `_lastBroadcastTicks` started at `long.MinValue`, so `now - _lastBroadcastTicks` overflowed and the first throttled movement never published.
- **Fix:** Reset `_lastBroadcastTicks` at drag start and allow the first moved event to publish immediately.
- **Files modified:** `src/BlazorCanvas/Components/Pages/Home.razor`
- **Verification:** `dotnet build BlazorCanvas.sln`, `dotnet test BlazorCanvas.sln`, and human retest approval.
- **Committed in:** `08cd7b8`

---

**Total deviations:** 3 auto-fixed.
**Impact on plan:** All fixes were necessary to satisfy the human-observable Phase 5 success criteria. The UPDATE-count invariant remained intact at `1`.

## Issues Encountered

- `request_user_input` was unavailable in Default mode, so the checkpoint used text-mode handling and waited for the user's verdict.
- The Windows sandbox helper was missing, so scoped file edits and some reads used approved unsandboxed PowerShell commands.
- The verification app locked `BlazorCanvas.exe` during rebuild attempts; stopping the app process released the file and the gates passed.

## Verification

- `docker compose up -d` - container running before checkpoint.
- `dotnet build BlazorCanvas.sln` - passed, 0 warnings, 0 errors after fixes.
- `dotnet test BlazorCanvas.sln` - passed, 405/405 after fixes.
- Human checkpoint - approved.
- `git diff --stat HEAD -- src/ tests/` - clean after fix commits.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

Phase BC-05 is ready for verifier review and phase completion. The milestone definition of done has been human-approved, with the autofocus caveat explicitly recorded in the UI contract.

## Self-Check: PASSED

- Human approval recorded.
- UPDATE count recorded as `1`.
- Optional D-54 check recorded as skipped.
- Autofocus outcome and option-a decision recorded.
- All checkpoint deviations were fixed or documented.

---
*Phase: BC-05-live-cross-tab-sync*
*Completed: 2026-07-17*