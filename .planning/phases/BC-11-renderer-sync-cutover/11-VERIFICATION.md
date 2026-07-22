---
phase: BC-11-renderer-sync-cutover
verified: 2026-07-22T16:00:00+02:00
status: gaps_found
score: 3/7 must-haves fully verified
behavior_unverified: 4
overrides_applied: 0
---

# Phase BC-11: Renderer, Sync & Cutover Verification Report

**Phase goal:** The app draws, drags, deletes, and synchronises as v1.1 while all runtime pixels and messages use the new storage model; the old runtime model and its obsolete tests are removed.

**Verified:** 2026-07-22
**Status:** gaps_found

## Evidence actually executed

| Check | Result |
| --- | --- |
| `dotnet build BlazorCanvas.sln --nologo -v q` | PASS — 0 warnings, 0 errors |
| `dotnet test BlazorCanvas.sln --nologo --no-build` | PASS — 276 passed, 0 failed, 0 skipped |
| Focused renderer/coordinator/movement/sync/cutover filter | PASS — 16 passed, 0 failed |
| Final-runtime source inventory excluding Transition and `V11Cutover` | PASS — no active `FigureStore`, `Figure`, `Box`, or `FigureType` model path; transition SQL is isolated as intended |

## Observable truths

| # | Truth | Status | Evidence |
| --- | --- | --- | --- |
| 1 | The authenticated owner resolves a deterministic final-public canvas before `Home` loads its `FigureRow` list. | VERIFIED (static) | `Program.cs` performs `V11Cutover.EnsureAsync` before `MapRazorComponents`; `Home.razor` calls `CanvasRepository.EnsureForOwnerAsync` before constructing the coordinator; `CanvasRepository` is public-schema-only and owner-scoped. There is no remaining dedicated canvas repository test after cutover. |
| 2 | Committed figures and the selection trace render local geometry under one invariant `translate(x, y) rotate(rotation)` wrapper, retaining appearance and the 48px mapping. | VERIFIED | `FigureShape.razor` and `SelectionTrace.razor` use local geometry inside their `<g>` transform, invariant formatting, and the canonical white/blue dashed trace. `V11RenderContractTests` covers all four geometry cases structurally and `CanvasCoordinates` remains 48px. Visual equivalence remains Phase 12's human gate. |
| 3 | Drag uses bbox-based local-frame clamping, UUID position-only messages, a 50ms throttle plus forced trailing edge, D-40 update-only receive, D-53 kinds/echo filtering, and D-54 coordinator-level discard. | PARTIAL | `V11Movement`, `SyncMessage`, and `CanvasInteractionCoordinator` contain the stated logic; coordinator tests cover trailing publication, unknown move/rollback, echo filtering, and direct D-54 discard. The suite has no browser/circuit integration proof that queued `Home.HandleRemoteMessage` deliveries received mid-drag are discarded before a pointer-up can commit. |
| 4 | Draw, delete, and drag persist through final public v1.11 tables and propagate across tabs. | NOT VERIFIED | Runtime wiring is present (`Home` delegates to `FigureRepository`; coordinator publishes canonical draw/delete/UUID move messages), but the only cross-circuit test uses in-memory delegates and a `List<FigureRow>`. No final-public database test demonstrates persisted draw/delete followed by a second circuit/tab receiving the event. |
| 5 | A zero-row move removes the stale row everywhere; a save failure rolls all peers back and the reload path converges to the persisted snapshot. | PARTIAL | Coordinator code handles zero affected rows by removing and publishing delete, and its failure test observes a rollback on two in-memory coordinators. It does not execute `ReloadAsync`, nor verify a database-backed failure/reload convergence. |
| 6 | Guarded cutover is transaction-safe for legacy-only, additive, fresh-users-only, completed, and invalid catalogs. Upgrade and fresh installation finish with only the public v1.11 schema. | NOT VERIFIED | Static inspection finds one transaction, `pg_advisory_xact_lock`, explicit catalog states, migration before promotion, and promotion/drop in that transaction. But `V11CutoverTests.cs` now has only two tests: completed-state restart and Program ordering. It supplies no fresh install, legacy migration, additive-state, invalid-state, or injected failure/rollback proof required by `11-03-PLAN.md`. |
| 7 | Final runtime paths and standing tests no longer carry the old Figure/Box/FigureStore/type-specific storage model or obsolete circle/normalisation/CHECK-mirror scaffolding. | VERIFIED | The old entity/store/geometry classes and their listed tests are deleted. The scoped `rg` inventory is clean apart from the explicitly retained legacy SQL fixture and `Data/V11/Transition/**`/`V11Cutover.cs`. The final suite is green at 276 tests. |

## Requirement traceability

| Requirement | Status | Evidence / gap |
| --- | --- | --- |
| RENDER-01 | SATISFIED | Local SVG geometry, transform, selection trace, fixed style/preview opacity, and 48px mapping are implemented and covered by the renderer contract test. The visual indistinguishability acceptance remains correctly deferred to REG-01. |
| SYNC-02 | GAPS FOUND | UUID message shape, throttled movement, unknown-id update-only handling, and direct coordinator D-54 tests exist. Missing final-public persistence plus two-circuit/circuit-scheduling proof prevents verifying the claimed end-to-end cross-tab outcome. |
| SYNC-03 | GAPS FOUND | Code and in-memory coordinator coverage show zero-row removal and rollback publication, but there is no database-backed stale-row test or reload-convergence test. |
| TEST-02 | SATISFIED | The retired old-model production code and obsolete test families are absent, with only the expressly isolated legacy migration boundary/fixture retained. |

`REQUIREMENTS.md` still displays all four Phase 11 requirements as unchecked/Pending, and `STATE.md` still says Phase 11 is ready/not started. Those planning-state entries do not invalidate the source evidence above, but must be updated only after the outstanding verification gaps are closed.

## Required gap closure

1. Expand `V11CutoverTests` with guarded scratch-database coverage for legacy-only migration/promotion, additive rerun, fresh-users-only install, invalid catalog rejection, and failures during seed/migration/promotion that prove full rollback. Assert final public columns and absence of both `v11` and legacy coordinate columns.
2. Add final-public persistence integration coverage for draw/delete/move across two coordinators/circuits (or the closest runnable Blazor circuit seam), including D-40, D-47, D-53, and D-54. Ensure the production `InvokeAsync` delivery boundary cannot admit a message received during drag after the drag commits.
3. Add database-backed zero-row move and failed-save → modal reload → peer convergence tests.

## Gaps summary

The implementation is materially present and compiles cleanly, but Phase 11's plan explicitly requires executable proof of all cutover states/atomic rollback and of persisted cross-tab behaviour under the final public schema. The current focused evidence is only 16 tests, of which `V11CutoverTests` contributes two completed-state/order checks and the coordinator tests use in-memory delegates. Those tests cannot establish the fresh/upgrade/rollback and persisted multi-tab claims. Therefore the phase cannot be marked `passed` yet.

---

_Verifier: generic-agent workaround acting as independent GSD verifier_
