---
phase: 10-geometry-draw-drag-sync-rework-no-edge-clamp-regression
plan: 03
subsystem: sync
tags: [csharp, blazor, signalr-free-di-notifier, sync-protocol]

# Dependency graph
requires:
  - phase: 10-geometry-draw-drag-sync-rework-no-edge-clamp-regression
    plan: 02
    provides: "FigureStore.MoveAsync anchor-only contract; Home.razor anchor-based drag state (dragAnchorX/Y, dragCurrentAnchorX/Y, dragFigure); dragCurrentBox as an interim box-shaped bridge"
provides:
  - "SyncMessage reshaped to (Kind, Sender, Id, Type, X, Y, Geometry) — draw carries type+anchor+geometry, move/rollback carry only the anchor, delete carries only the id (D-53 amended by D-59)"
  - "SyncReceiver — a new static, UI-free class owning the D-40/D-53 receiver rules: draw-creates-only, move/rollback-update-only (unknown id ignored, never inserted), idempotent delete, anchor-only mutation on move — unit-tested for the first time without a component-test harness"
  - "Home.razor's ApplyMessage delegates to SyncReceiver.Apply and clears selectedId only when the returned removed-id matches the current selection"
affects: [10-04-renderer-rework, 10-06-verification-checkpoint]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "A UI-free receiver returns the id it removed (or null) rather than mutating component selection state directly — the component reacts to that return value for its own per-circuit selection, keeping SyncReceiver ignorant of UI concerns entirely (PA-4)."
    - "The wire payload for draw is the figure's own stored X/Y/Geometry with zero re-derivation — SyncMessage.Draw reads three fields once instead of decoding-then-reading four coordinates (PA-9)."

key-files:
  created:
    - src/BlazorCanvas/Sync/SyncReceiver.cs
    - tests/BlazorCanvas.Tests/Sync/SyncReceiverTests.cs
  modified:
    - src/BlazorCanvas/Sync/SyncMessage.cs
    - src/BlazorCanvas/Components/Pages/Home.razor
    - tests/BlazorCanvas.Tests/Sync/CanvasSyncNotifierTests.cs

key-decisions:
  - "Task 1's own acceptance criterion (clean build) forced touching Home.razor's receive path one plan-task earlier than the plan's stated scope boundary — see Deviations."
  - "SyncReceiver.Apply returns Guid? (the removed figure id) rather than mutating selection state, so the receiver has zero UI dependencies and Home.razor's four-statement ApplyMessage is the only place that reacts to it (PA-4)."
  - "SyncReceiverTests seeds the draw-idempotency case without the target id present (so the first application is a genuine insert) while move/rollback/delete cases seed with the target id present — a single seed shape would have let the draw idempotency case degenerate into a no-op-of-a-no-op instead of proving insert-then-no-duplicate."

requirements-completed: [SYNC-02, STOR-05]

coverage:
  - id: D1
    description: "SyncMessage carries anchor+geometry: draw reads Figure.X/Y/Geometry with no decoding; move and rollback take (id, x, y) and set Type/Geometry null; delete sets everything but Kind/Sender/Id null"
    requirement: "SYNC-02"
    verification:
      - kind: unit
        ref: "tests/BlazorCanvas.Tests/Sync/CanvasSyncNotifierTests.cs#Draw_CarriesTypeAndCoordinates, Move_CarriesNoType_BecauseTypeNeverChanges, Delete_CarriesOnlyTheId, Rollback_CarriesTheOriginalCoordinates"
        status: pass
    human_judgment: false
  - id: D2
    description: "SyncReceiver: draw is the only kind that may create a figure; a draw for an id already present does not duplicate"
    requirement: "SYNC-02"
    verification:
      - kind: unit
        ref: "tests/BlazorCanvas.Tests/Sync/SyncReceiverTests.cs#Draw_ForUnknownId_InsertsWithAppendedZ, Draw_ForKnownId_DoesNotDuplicate"
        status: pass
    human_judgment: false
  - id: D3
    description: "move and rollback are UPDATE-ONLY: an unknown figure id is ignored entirely, never inserted (D-40 resurrection guard closed)"
    requirement: "SYNC-02"
    verification:
      - kind: unit
        ref: "tests/BlazorCanvas.Tests/Sync/SyncReceiverTests.cs#Move_ForUnknownId_LeavesListUnchanged_AndCreatesNothing, Rollback_ForUnknownId_LeavesListUnchanged_AndCreatesNothing"
        status: pass
    human_judgment: false
  - id: D4
    description: "A move on a known figure changes only the anchor and leaves the Geometry string byte-identical, for all four figure types"
    requirement: "STOR-05"
    verification:
      - kind: unit
        ref: "tests/BlazorCanvas.Tests/Sync/SyncReceiverTests.cs#Move_OnKnownFigure_ChangesAnchor_LeavesGeometryByteIdentical(type: Line|Rectangle|Circle|Triangle)"
        status: pass
    human_judgment: false
  - id: D5
    description: "delete is idempotent: removes and returns the id when present, returns null and throws nothing when absent; applying any of the four kinds twice equals applying it once"
    requirement: "SYNC-02"
    verification:
      - kind: unit
        ref: "tests/BlazorCanvas.Tests/Sync/SyncReceiverTests.cs#Delete_RemovesAndReturnsTheId, Delete_OfUnknownId_ReturnsNull_AndThrowsNothing, Apply_IsIdempotent_ForEveryKind(kind: draw|move|rollback|delete)"
        status: pass
    human_judgment: false
  - id: D6
    description: "Home.razor's receive path delegates to SyncReceiver; the echo filter, mid-drag discard, and disposed-circuit catch stay component-side; full solution builds clean and the full suite is green"
    requirement: "SYNC-02"
    verification:
      - kind: unit
        ref: "dotnet build BlazorCanvas.sln --nologo (0 Warning(s), 0 Error(s)); dotnet test BlazorCanvas.sln --nologo (381/381 pass)"
        status: pass
    human_judgment: false
  - id: D7
    description: "The live two-tab behaviours this plan cannot unit-test — real-time glide, the mid-drag discard, the trailing edge, and the save-failure rollback — are carried to the 10-06 verification checkpoint"
    requirement: "SYNC-02"
    verification: []
    human_judgment: true
    rationale: "No component-test harness exists (D-49); these behaviours require live two-tab observation, deferred by design to the 10-06 checkpoint."

duration: 15min
completed: 2026-07-24
status: complete
---

# Phase 10 Plan 03: Geometry Draw/Drag/Sync Rework — Sync Payload Rework Summary

**SyncMessage reshaped from a box to anchor+geometry (D-53 amended by D-59); the D-40 receiver rules — draw-creates-only, move/rollback-update-only, idempotent delete — extracted into a new UI-free SyncReceiver and unit-tested for the first time; Home.razor's receive path now delegates entirely to it.**

## Performance

- **Duration:** ~15 min
- **Started:** 2026-07-24 (session start)
- **Completed:** 2026-07-24T08:22:54Z
- **Tasks:** 3
- **Files modified:** 5 (3 modified, 2 created)

## Accomplishments
- `SyncMessage` reshaped to `(Kind, Sender, Id, Type, X, Y, Geometry)`: `draw` carries the type, the anchor and the stored geometry JSON read directly off the `Figure` entity (no decoding); `move` and `rollback` carry the anchor alone with `Type`/`Geometry` null; `delete` carries only the id (SYNC-02, D-53 as amended by D-59)
- Every publish site in `Home.razor` updated to the new factory signatures: the glide publish in `ContinueDragAsync`, the drop publish and rollback publish in `CommitDragAsync`, and both passes of `BroadcastReloadedSnapshot` — all pass the anchor pair directly, with D-47's 50 ms trailing-edge throttle and the unconditional final-move-before-write ordering left untouched
- New `SyncReceiver` (`src/BlazorCanvas/Sync/SyncReceiver.cs`): a static, UI-free class with one entry point, `Apply(figures, message, userId) -> Guid?`, implementing draw-creates-only (new figure appended with `z` = local max + 1, or 1 when the list is empty), move/rollback-update-only (an unknown id is silently ignored — D-40's resurrection hole stays closed), and idempotent delete (returns the removed id or null)
- `SyncReceiverTests` (14 new tests): pure unit tests with no `Database` collection and no fixture, covering insertion, no-duplicate draw, unknown-id move/rollback no-ops, a per-`FigureType` theory proving `Geometry` is byte-identical across a move, delete plus unknown-delete, and an idempotency theory across all four kinds
- `Home.razor`'s `ApplyMessage` now delegates entirely to `SyncReceiver.Apply`, clearing `selectedId` only when the returned removed-id matches the current selection; the now-superseded `ApplyBox`, `MessageBox` and `NextLocalZ` helpers are deleted; `HandleRemoteMessage`'s echo filter (D-11), blanket mid-drag discard (D-54), `StateHasChanged` call and `ObjectDisposedException` catch are byte-for-byte unchanged
- Full solution builds clean (0 warnings, 0 errors); full test suite is 381/381 green (up from the 367 baseline by the 14 new `SyncReceiverTests`)

## Task Commits

Each task was committed atomically:

1. **Task 1: The anchor+geometry broadcast record, and every publish site** - `861afbe` (feat)
2. **Task 2: SyncReceiver — the D-40 / D-53 receiver rules, made testable** - `b2a9dd3` (feat)
3. **Task 3: Home.razor's receive path delegates to SyncReceiver** - `a96b532` (refactor)

**Plan metadata:** pending (this commit)

## Files Created/Modified
- `src/BlazorCanvas/Sync/SyncMessage.cs` - Record reshaped to `(Kind, Sender, Id, Type, X, Y, Geometry)`; `Draw` reads `f.X`/`f.Y`/`f.Geometry` directly; `Move`/`Rollback` take `(id, x, y, sender)`; class doc updated to the amended D-53
- `src/BlazorCanvas/Components/Pages/Home.razor` - Every publish site updated to the anchor-only signatures; `ApplyMessage` delegates to `SyncReceiver`; `ApplyBox`/`MessageBox`/`NextLocalZ` deleted
- `tests/BlazorCanvas.Tests/Sync/CanvasSyncNotifierTests.cs` - The four payload-assertion tests reworked to the anchor+geometry shape; the six notifier cross-user-isolation tests (T-10-08) left untouched
- `src/BlazorCanvas/Sync/SyncReceiver.cs` - New; the D-40/D-53 receiver rules, static and UI-free
- `tests/BlazorCanvas.Tests/Sync/SyncReceiverTests.cs` - New; 14 unit tests proving every receiver rule without a database or component harness

## Decisions Made
- Followed planner assumptions PA-4 (`SyncReceiver` extracted with no UI dependency, returning the removed id rather than owning selection state), PA-9 (geometry travels as the raw stored JSON string, never re-derived), and PA-10 (a remotely drawn figure gets `z` = local max + 1, mirroring `FigureStore.InsertAsync`'s append rule) exactly as written.
- `SyncReceiverTests`'s idempotency theory seeds the `draw` case without the target id present (so the first application is a genuine insert, proving the second is a real no-duplicate) while `move`/`rollback`/`delete` seed with the target id present (needed to exercise their update/removal paths) — a single shared seed across all four kinds would have let the `draw` case degenerate into a no-op-of-a-no-op instead of proving anything about insertion.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] Home.razor's receive path updated one task earlier than the plan's stated scope**
- **Found during:** Task 1, first `dotnet build` after reshaping `SyncMessage`
- **Issue:** Task 1's action explicitly said "Leave the receive path (`ApplyMessage` and its helpers) alone in this task; Task 3 rewrites it" — but Task 1's own acceptance criterion requires `dotnet build BlazorCanvas.sln` to exit 0 with 0 errors. `ApplyMessage`'s helpers `MessageBox` (built a `Box` from `msg.X1/Y1/X2/Y2`) and `ApplyBox` (re-encoded via `GeometryCodec`) referenced record members that no longer exist once `SyncMessage` drops the four box coordinates for an anchor pair — the solution would not compile after Task 1's own change.
- **Fix:** Updated `ApplyMessage`'s draw/move/rollback arms inline to the new anchor+geometry payload — a functionally correct interim fix, not a stub — and deleted `MessageBox`/`ApplyBox` since their logic no longer applied. This is real, correct behavior for the receive path; Task 3 then cleanly replaced the whole method body with a delegation to `SyncReceiver` (built in Task 2), which is what the plan intended all along — Task 3's diff is a genuine refactor onto `SyncReceiver`, not a discovery of new work.
- **Files modified:** `src/BlazorCanvas/Components/Pages/Home.razor`
- **Verification:** `dotnet build BlazorCanvas.sln` exits 0 (0 warnings, 0 errors) after Task 1; `dotnet test BlazorCanvas.sln --nologo --filter "FullyQualifiedName~CanvasSyncNotifierTests"` — 10/10 pass.
- **Committed in:** `861afbe` (Task 1 commit)

---

**Total deviations:** 1 auto-fixed (1 blocking)
**Impact on plan:** Necessary to satisfy Task 1's own build-clean acceptance criterion; no scope creep — Task 3 still performed its planned `SyncReceiver` delegation and deleted the same three helper names (`ApplyBox`, `MessageBox`, `NextLocalZ`) the plan named, just with `NextLocalZ` surviving one task longer than `ApplyBox`/`MessageBox` because the draw arm still needed it until `SyncReceiver` existed.

## Issues Encountered
None beyond the build-ordering deviation documented above, resolved within Task 1 before its commit.

## User Setup Required
None — the Compose PostgreSQL container was already up and healthy; no `BLAZORCANVAS_TEST_CONNECTION` override was set (the stale Phase-9 operational note was correctly disregarded, per the environment note).

## Next Phase Readiness
- `SyncMessage`, `SyncReceiver`, and `Home.razor`'s receive/publish paths are all on the anchor+geometry contract; `FigureShape.razor` and `SelectionTrace.razor` are confirmed unchanged by this plan (verified via `git diff --stat` across the plan's three commits).
- `dragCurrentBox` remains Home.razor's explicitly-commented interim box-shaped bridge for the renderer, unchanged by this plan — 10-04 is next to remove it once `FigureShape` takes the anchor and geometry directly.
- Full solution builds clean (0 warnings, 0 errors); full test suite is 381/381 green (up from the 367 baseline noted at 10-02's close, by the 14 new `SyncReceiverTests`).
- The live two-tab behaviours this plan cannot unit-test — real-time glide, the mid-drag discard, the trailing edge, and the save-failure rollback — are unchanged in logic (only the wire payload shape changed) and remain carried to the 10-06 verification checkpoint, per the plan's own verification section.

---
*Phase: 10-geometry-draw-drag-sync-rework-no-edge-clamp-regression*
*Completed: 2026-07-24*

## Self-Check: PASSED

All 5 created/modified source and test files verified present on disk. SUMMARY.md itself verified present. All 3 task commits (`861afbe`, `b2a9dd3`, `a96b532`) verified present in git history. Full solution build: 0 warnings, 0 errors. Full test suite: 381/381 passing.
