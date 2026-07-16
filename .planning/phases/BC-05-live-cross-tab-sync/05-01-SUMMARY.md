---
phase: BC-05-live-cross-tab-sync
plan: 01
subsystem: sync
tags: [blazor-server, pubsub, sync, xunit]

requires:
  - phase: BC-04-select-drag-delete
    provides: Completed persisted draw, drag, and delete paths that later sync wiring will broadcast.
provides:
  - D-53 SyncMessage contract with four canonical message kinds.
  - User-keyed in-memory CanvasSyncNotifier with disposable subscriptions.
  - Ten tests proving cross-user isolation, unsubscribe behavior, publish/dispose safety, and payload shapes.
affects: [BC-05-live-cross-tab-sync, Home.razor sync wiring, Program.cs singleton registration]

tech-stack:
  added: []
  patterns:
    - ConcurrentDictionary-of-subscribers keyed by user_id.
    - Flat sealed record message contract with factory methods.
    - Pure in-memory xUnit tests without DI or mocking packages.

key-files:
  created:
    - src/BlazorCanvas/Sync/SyncMessage.cs
    - src/BlazorCanvas/Sync/CanvasSyncNotifier.cs
    - tests/BlazorCanvas.Tests/Sync/CanvasSyncNotifierTests.cs
  modified: []

key-decisions:
  - "SyncMessage implements D-53 exactly: draw, move, delete, rollback; no drop kind and no type on move/rollback."
  - "CanvasSyncNotifier isolation is entirely the user_id dictionary key; Publish never enumerates other users."
  - "The notifier owns subscription lifecycle and user-keyed delivery only; echo filtering, throttling, and mid-drag discard remain caller state for later plans."

patterns-established:
  - "Canvas sync primitives live under BlazorCanvas.Sync."
  - "Notifier subscriptions return IDisposable handles and double-dispose through Interlocked.Exchange."

requirements-completed: [SYNC-01]

coverage:
  - id: D1
    description: "D-53 SyncMessage contract with exactly four canonical payload shapes."
    requirement: SYNC-01
    verification:
      - kind: unit
        ref: "tests/BlazorCanvas.Tests/Sync/CanvasSyncNotifierTests.cs#Draw_CarriesTypeAndCoordinates"
        status: pass
      - kind: unit
        ref: "tests/BlazorCanvas.Tests/Sync/CanvasSyncNotifierTests.cs#Move_CarriesNoType_BecauseTypeNeverChanges"
        status: pass
      - kind: unit
        ref: "tests/BlazorCanvas.Tests/Sync/CanvasSyncNotifierTests.cs#Delete_CarriesOnlyTheId"
        status: pass
      - kind: unit
        ref: "tests/BlazorCanvas.Tests/Sync/CanvasSyncNotifierTests.cs#Rollback_CarriesTheOriginalCoordinates"
        status: pass
    human_judgment: false
  - id: D2
    description: "CanvasSyncNotifier publishes to subscribers of the same user and not to other users."
    requirement: SYNC-01
    verification:
      - kind: unit
        ref: "tests/BlazorCanvas.Tests/Sync/CanvasSyncNotifierTests.cs#Publish_ForDifferentUser_DoesNotInvokeHandler"
        status: pass
      - kind: unit
        ref: "tests/BlazorCanvas.Tests/Sync/CanvasSyncNotifierTests.cs#Publish_ToMultipleSubscribersOfSameUser_InvokesAll"
        status: pass
      - kind: other
        ref: "negative check: hard-coded Publish lookup key made Publish_ForDifferentUser_DoesNotInvokeHandler fail"
        status: pass
    human_judgment: false
  - id: D3
    description: "Subscription disposal is safe and publish/no-subscriber cases do not throw."
    requirement: SYNC-01
    verification:
      - kind: unit
        ref: "tests/BlazorCanvas.Tests/Sync/CanvasSyncNotifierTests.cs#Subscribe_ThenDispose_StopsReceivingMessages"
        status: pass
      - kind: unit
        ref: "tests/BlazorCanvas.Tests/Sync/CanvasSyncNotifierTests.cs#Dispose_CalledTwice_IsSafe"
        status: pass
      - kind: unit
        ref: "tests/BlazorCanvas.Tests/Sync/CanvasSyncNotifierTests.cs#Publish_WithNoSubscribers_DoesNotThrow"
        status: pass
      - kind: unit
        ref: "tests/BlazorCanvas.Tests/Sync/CanvasSyncNotifierTests.cs#Publish_WhenHandlerUnsubscribesDuringDelivery_DoesNotThrow"
        status: pass
    human_judgment: false

duration: 35min
completed: 2026-07-16
status: complete
---

# Phase BC-05 Plan 01: Sync Primitives Summary

**D-53 sync messages and a user-keyed in-memory notifier with tests proving isolation and subscription cleanup**

## Performance

- **Duration:** 35 min
- **Started:** 2026-07-16T21:25:00Z
- **Completed:** 2026-07-16T21:59:34Z
- **Tasks:** 3
- **Files modified:** 3

## Accomplishments

- Added `SyncMessage`, the flat D-53 record with `draw`, `move`, `delete`, and `rollback` factories.
- Added `CanvasSyncNotifier`, the in-memory pub/sub keyed only by `user_id` and returning disposable subscription handles.
- Added 10 xUnit tests proving cross-user isolation, multi-tab fanout, unsubscribe/double-dispose safety, publish/dispose race safety, and all four payload shapes.

## Task Commits

Each task was committed atomically:

1. **Task 1: SyncMessage — the D-53 broadcast contract as one flat record** - `9657f12` (feat)
2. **Task 2: CanvasSyncNotifier — the in-memory pub/sub, keyed by user_id** - `7776863` (feat)
3. **Task 3: Prove the cross-user isolation, the unsubscribe contract, and the four payload shapes** - `668a4b7` (test)

**Plan metadata:** pending final docs commit

## Files Created/Modified

- `src/BlazorCanvas/Sync/SyncMessage.cs` - D-53 broadcast contract and factory methods.
- `src/BlazorCanvas/Sync/CanvasSyncNotifier.cs` - User-keyed in-memory notifier and disposable subscription type.
- `tests/BlazorCanvas.Tests/Sync/CanvasSyncNotifierTests.cs` - Ten pure in-memory tests for notifier behavior and message payload shapes.

## Verification

- `dotnet build BlazorCanvas.sln` - passed, 0 warnings, 0 errors.
- `dotnet test BlazorCanvas.sln --filter "FullyQualifiedName~CanvasSyncNotifierTests"` - passed, 10 passed, 0 failed, 0 skipped.
- Negative isolation check - temporarily hard-coded `Publish` lookup to user id `1`; `Publish_ForDifferentUser_DoesNotInvokeHandler` failed because user 1 received user 2's message; restored the correct `userId` lookup.
- `dotnet test BlazorCanvas.sln` - passed, 405 passed, 0 failed, 0 skipped.
- `git diff --stat HEAD -- '*.js'` - no output.
- `git diff -- '*.csproj'` - no output.
- `src/BlazorCanvas/Sync` contains exactly `CanvasSyncNotifier.cs` and `SyncMessage.cs`.

## Decisions Made

- Followed D-53 exactly: no `drop` kind, no fifth factory, `move` and `rollback` carry coordinates with `Type = null`, and `delete` carries only the id.
- Kept `CanvasSyncNotifier` dependency-free so plan 05-02 can register it as a singleton and tests can instantiate it directly.
- Kept echo filtering, D-54 mid-drag discard, throttling, and receiver apply logic out of the notifier because those are per-circuit responsibilities in later plans.

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered

- The Windows sandbox helper intermittently failed to launch for read/write shell and patch operations. Required commands were rerun with explicit escalation; no scope or code behavior changed.
- A concurrent targeted test run collided with the full test run on the same build output DLL. The targeted filter was rerun by itself and passed.

## Known Stubs

None.

## Threat Flags

None.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

Ready for 05-02. The sync primitives exist and are tested; the next plan can register the notifier singleton and configure retry behavior without adding packages.

## Self-Check: PASSED

- Found `src/BlazorCanvas/Sync/SyncMessage.cs`.
- Found `src/BlazorCanvas/Sync/CanvasSyncNotifier.cs`.
- Found `tests/BlazorCanvas.Tests/Sync/CanvasSyncNotifierTests.cs`.
- Found task commits `9657f12`, `7776863`, and `668a4b7` in git history.

---
*Phase: BC-05-live-cross-tab-sync*
*Completed: 2026-07-16*
