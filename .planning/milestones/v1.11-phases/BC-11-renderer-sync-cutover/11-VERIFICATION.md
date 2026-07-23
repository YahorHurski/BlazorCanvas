---
phase: BC-11-renderer-sync-cutover
verified: 2026-07-22T16:35:00+02:00
status: passed
score: 7/7 must-haves verified
behavior_unverified: 0
overrides_applied: 0
---

# Phase BC-11: Renderer, Sync & Cutover Verification Report

**Phase goal:** The app draws, drags, deletes, and synchronises as v1.1 while all runtime pixels and messages use the v1.11 storage model; the obsolete runtime model and its dead tests are removed.

**Verified:** 2026-07-22
**Status:** passed

## Evidence executed

| Check | Result |
| --- | --- |
| `dotnet build BlazorCanvas.sln --nologo -v q` | PASS — 0 warnings, 0 errors |
| Focused `V11CutoverTests`, `FinalPublicCanvasSyncIntegrationTests`, and `CanvasInteractionCoordinatorTests` | PASS — 25 passed, 0 failed, 0 skipped |
| `dotnet test BlazorCanvas.sln --nologo` | PASS — 296 passed, 0 failed, 0 skipped |

## Observable truths

| # | Truth | Status | Direct evidence |
| --- | --- | --- | --- |
| 1 | `Home` resolves the authenticated owner's deterministic final-public canvas before loading rows. | VERIFIED | `Program.cs` invokes `V11Cutover.EnsureAsync` before component routes; `Home.razor` calls `CanvasRepository.EnsureForOwnerAsync` then creates a coordinator whose delegates exclusively call public v1.11 repositories. |
| 2 | Figures and selection traces use local geometry inside one invariant `translate(x, y) rotate(rotation)` SVG group, retaining selection styling and the 48px page-to-canvas mapping. | VERIFIED | `FigureShape.razor`, `SelectionTrace.razor`, `V11RenderContractTests`, and `CanvasCoordinates.ToolbarHeight == 48`. Visual indistinguishability remains Phase 12's explicit human acceptance scope. |
| 3 | Sync is UUID-keyed and update-only: D-53 draw/delete/move/rollback shapes, echo filtering, D-40 unknown-move/rollback non-insertion, D-47's 50ms throttle plus trailing coordinate, and D-54 receipt-time blanket mid-drag discard all execute at the production seam. | VERIFIED | `CanvasInteractionCoordinator` implements the protocol; `Home.HandleRemoteMessage` authorizes before `InvokeAsync`; coordinator tests cover queued receipt-before-pointer-up behavior; final-public integration tests cover UUID messages, unknown IDs, throttle/trailing persistence, and canonical relay. |
| 4 | Two independent circuits persist and converge through `public.figures` for draw, move, and delete, without duplicate insertion. | VERIFIED | `FinalPublicCanvasSyncIntegrationTests.FinalPublicRows_PersistAndRelayCanonicalDrawMoveDeleteWithoutDuplicateInsertion` uses separate repositories/coordinators and a shared notifier, asserts canonical UUID/geometry/style/z persistence, replay suppression, and synchronized delete. |
| 5 | A zero-row final-public move removes stale state without resurrection; a failed save rolls peers back, displays the reload modal, and reload converges both circuits to the authoritative database snapshot. | VERIFIED | `ZeroRowMove_RemovesStaleFigureForEveryCircuitWithoutResurrection` deletes directly through the second real repository before the stale move. `FailedMove_RollsBackPeerThenReloadsBothCircuitsToAuthoritativePublicSnapshot` proves rollback, modal state, reload, and equality with the final-public repository. |
| 6 | Cutover safely handles legacy-only, additive, fresh-users-only, completed, and invalid catalog states; injected failures leave a byte-stable pre-cutover catalog and a retry succeeds. | VERIFIED | Scratch-database `V11CutoverTests` cover each named state and failures after schema apply, type seed, migration, legacy drop, and first promotion. Final catalog assertions require public UUID/storage columns, no `v11` schema, no legacy coordinate columns, and no legacy constraint. |
| 7 | The active runtime and standing tests no longer retain the old figure store/entity/box model or the retired circle round-trip, line-normalisation landmine, and guard-vs-CHECK scaffolding. | VERIFIED | Current source inventory contains only the intentionally isolated `Data/V11/Transition` legacy conversion boundary; the obsolete production classes and test families are absent. The rebased suite is green at 296 tests. |

## Requirement traceability

| Requirement | Status | Evidence |
| --- | --- | --- |
| RENDER-01 | SATISFIED | Local SVG wrapper and local geometry, selection trace, and 48px mapping have source-contract coverage. The remaining visual side-by-side acceptance is deliberately Phase 12 / REG-01. |
| SYNC-02 | SATISFIED | Final-public two-circuit integration coverage proves draw, delete, move, duplicate suppression, D-40, D-47, D-53, and D-54's receipt-time bridge. |
| SYNC-03 | SATISFIED | Database-backed stale zero-row deletion and failed-save rollback/modal/reload/peer convergence are exercised. |
| TEST-02 | SATISFIED | Legacy runtime/test scaffolding is retired; only the narrow migration transition code remains intentionally. |

## Result

All Phase 11 requirements have executable evidence and the full rebased suite passes. Phase 12's live visual regression acceptance is intentionally outside this phase and does not block Phase 11 from passing.

---

_Verifier: generic-agent workaround acting as independent GSD verifier_
