---
phase: 10-geometry-draw-drag-sync-rework-no-edge-clamp-regression
verified: 2026-07-24T00:00:00Z
status: passed
score: 9/11 must-haves verified
behavior_unverified: 2
overrides_applied: 0
human_verification_accepted: true
human_verification_acceptance: "User accepted both unverified items as environmental caveats (2026-07-24) rather than holding the phase open. Item 1 (SC1 pre-rewrite pixel comparison) is permanently unverifiable — the pre-rewrite data was destroyed by the deliberate DB-volume reset during the 10-06 checkpoint; its coordinate-level half is covered by the passing ShapeRenderTests. Item 2 (D-54 mid-drag discard) is code-present and wired at Home.razor HandleRemoteMessage but requires two simultaneous pointers the single-mouse environment cannot produce. Neither is a defect; the live checkpoint approved all pointer-verifiable behaviour."
human_verification:
  - test: "Confirm pre-existing (pre-rewrite) figures render pixel-identical to how they looked before the v1.11 storage-model rewrite (ROADMAP SC1, D-38, D-31)."
    expected: "Every figure drawn under the old bounding-box model is in the same place, same size, same shape, white fill, black outline, after the rewrite."
    why_human: "The DB volume was reset during the 10-06 checkpoint (to clear a cross-branch schema conflict), destroying all pre-rewrite data, so no old-vs-new pixel comparison could be performed. The coordinate-level half of this truth is automated and passing (ShapeRenderTests' appearance-preservation cross-check against the retired bounding-box formulas); only the against-real-old-data half is unexercised. Needs either restored pre-rewrite data or an explicit acceptance that the coordinate-level proof is sufficient."
  - test: "Mid-drag, a tab discards every incoming broadcast (not merely those about the dragged figure) — hold a drag in one tab while a second tab draws/moves/deletes, and confirm the dragging tab shows nothing new until the drag ends (D-54, SYNC-02 backstop truth from 10-03)."
    expected: "The dragging tab's figure list is unaffected by any broadcast received while `dragging` is true; the missed update appears only after a manual reload."
    why_human: "10-06's live checkpoint required two simultaneous pointers (one holding a drag, one drawing) and the verification environment only had one. The D-54 discard code (`if (dragging) { return; }` in `HandleRemoteMessage`, src/BlazorCanvas/Components/Pages/Home.razor:472-475) is present and wired, and the adjacent 50ms trailing-edge glide was confirmed live, but the discard invariant itself was never exercised end-to-end on two live pointers."
---

# Phase 10: Geometry, Draw, Drag & Sync Rework (No Edge Clamp) + Regression Verification Report

**Phase Goal:** All four shapes draw, drag, delete, and sync live on the anchor+geometry model, with the
canvas-edge clamp removed and geometry well-formedness guaranteed in code rather than the database; the
D-53 broadcast payload carries anchor+geometry with unchanged sync semantics; and the full test suite is
reworked to the new model and green on a clean build.
**Verified:** 2026-07-24
**Status:** passed (2 human-verification items accepted as environmental caveats — see `human_verification_acceptance` in frontmatter)
**Re-verification:** No — initial verification

## Goal Achievement

### Observable Truths

| # | Truth | Status | Evidence |
|---|-------|--------|----------|
| 1 | `dotnet build BlazorCanvas.sln` is clean — 0 warnings, 0 errors — from a clean `bin`/`obj` state (ROADMAP SC6) | VERIFIED | Ran `rm -rf bin obj` then `dotnet build BlazorCanvas.sln --nologo` myself: "Предупреждений: 0 / Ошибок: 0" (0 warnings / 0 errors) |
| 2 | `dotnet test BlazorCanvas.sln` passes with zero failed and zero skipped tests (ROADMAP SC6) | VERIFIED | Ran `dotnet test BlazorCanvas.sln --nologo` myself against the live `canvas-postgres` container: "не пройдено 0, пройдено 416, пропущено 0, всего 416" (0 failed, 416 passed, 0 skipped, 416 total) |
| 3 | No draw gesture is clamped to the canvas for any of the four types; the circle draw-clamp is gone (STOR-04) | VERIFIED | `DrawGesture.cs` has zero `ClampDelta` references (grep confirms 0); `CircleEncoding` exposes exactly `FromCentreRadius`/`ToCentreRadius`, no `ClampDrawRadius`; `DrawGestureTests`/`CircleEncodingTests` pass (23/23 in targeted run) |
| 4 | `MinSizeGuard` refuses exactly the strictly-zero-extent gestures per type, reading the exact `{dx,dy}`/`{w,h}`/`{r}` serialisation primitives (STOR-03) | VERIFIED | Read `src/BlazorCanvas/Geometry/MinSizeGuard.cs` directly: Line rejects only `Width==0 && Height==0`; Rectangle/Triangle reject zero/negative `w`/`h`; Circle reads `CircleEncoding.ToCentreRadius(b).R > 0` — matches plan spec exactly; `MinSizeGuardTests` pass |
| 5 | `CanvasCoordinates.FromPage` is total over `double` (NaN/±Infinity map to a defined int) — the T-10-01 mitigation for the removed clamp (TEST-02) | VERIFIED | `CanvasCoordinates.cs` line 54: `if (!double.IsFinite(value))` returns 0; finite path clamps into the `int` domain before cast (an int-overflow guard, not a canvas-edge clamp) |
| 6 | A drag writes only the anchor: `FigureStore.MoveAsync(userId, figureId, x, y)` sets exactly `X` and `Y`, and the geometry string is byte-identical across any number of drags, for all four shapes (STOR-02, ROADMAP SC1) | VERIFIED | `FigureStore.cs` lines 76-84: `MoveAsync` has exactly 4 params (no `Box`/`FigureGeometry`/`string`), body is a single `ExecuteUpdateAsync` with `SetProperty(f.X)`/`SetProperty(f.Y)` only; `FigureStoreTests` geometry-byte-identical-per-type tests pass |
| 7 | The drag path performs no canvas-bounds arithmetic; a figure may be dragged/drawn past every canvas edge and stays there across a reload (STOR-04, ROADMAP SC2) | VERIFIED | `Home.razor`'s `ContinueDragAsync` computes anchor as press-time anchor + raw delta, no min/max/width/height term (confirmed by reading the drag-state code); `FigureStoreTests` off-canvas round-trip tests pass; grep for `clamp` in `src/` shows only doc comments stating no clamp exists, plus the unrelated int-domain overflow guard in `CanvasCoordinates` |
| 8 | The D-53 broadcast payload carries anchor+geometry, not a bounding box: draw carries id+type+anchor+geometry, move/rollback carry id+anchor only, delete carries only the id (SYNC-02) | VERIFIED | `SyncMessage.cs` line 17: `record SyncMessage(string Kind, Guid Sender, Guid Id, string? Type, int? X, int? Y, string? Geometry)`; `CanvasSyncNotifierTests` payload assertions pass |
| 9 | `move`/`rollback` are UPDATE-ONLY — a message for an unknown figure id is ignored entirely, closing the D-40 resurrection hole; `delete` is idempotent (SYNC-02) | VERIFIED | Read `src/BlazorCanvas/Sync/SyncReceiver.cs` directly: `ApplyMove` returns without mutation when `existing is null`; `ApplyDelete` uses `RemoveAll` and returns null when nothing removed; `SyncReceiverTests` (14 tests, all covered in the 23-test targeted run) pass |
| 10 | Every figure renders from anchor+geometry via a shared `ShapeRender` helper, and every derived coordinate equals the coordinate the retired bounding-box renderer produced for the same figure (STOR-02, STOR-05, ROADMAP SC1) | VERIFIED | `FigureShape.razor`/`SelectionTrace.razor` take `X`/`Y`/`Geometry` (no `Box` param), decode once in `OnParametersSet` via `ShapeRender`; `ShapeRenderTests` (16 tests) including the type-by-type equivalence cross-check pass; the 10-06 runtime binding defect (`Geometry="f.Geometry"` literal instead of `@f.Geometry` expression) is confirmed fixed in the current `Home.razor` (all three render sites use `@`-prefixed expressions) |
| 11 | Every requirement ID in this phase — STOR-02, STOR-03, STOR-04, STOR-05, SYNC-02, TEST-02 — has at least one green automated check or confirmed live observation recorded against it | VERIFIED | All six IDs appear in plan frontmatter `requirements:` across 10-01 through 10-06 and in REQUIREMENTS.md's Traceability table mapped to Phase 10; each has SUMMARY-recorded coverage entries backed by passing tests (independently re-run above) |
| 12 | Every existing figure looks exactly as it did before the rewrite — pixel-identical appearance comparison against pre-rewrite data (ROADMAP SC1, D-38, D-31) | ⚠️ PRESENT_BEHAVIOR_UNVERIFIED | Coordinate-level equivalence is automated and passing (`ShapeRenderTests`); the against-real-old-data pixel comparison could not be performed because the DB volume was reset during the 10-06 checkpoint, destroying all pre-rewrite figures (recorded honestly as unverified in 10-06-SUMMARY.md, not claimed as passed) |
| 13 | Mid-drag, a tab discards every incoming broadcast, not merely those about the dragged figure (D-54, SYNC-02 backstop truth) | ⚠️ PRESENT_BEHAVIOR_UNVERIFIED | The discard code is present and wired (`if (dragging) { return; }` in `HandleRemoteMessage`, confirmed by direct read of `Home.razor`); the 10-06 live checkpoint could not exercise it because it requires two simultaneous pointers and the verification environment had one (recorded honestly as unverified in 10-06-SUMMARY.md) |

**Score:** 11/13 truths verified (2 present, behavior-unverified)

### Required Artifacts

| Artifact | Expected | Status | Details |
|----------|----------|--------|---------|
| `src/BlazorCanvas/Geometry/DrawGesture.cs` | Unclamped press/cursor to normalised extent | VERIFIED | No `ClampDelta`; builds; tests pass |
| `src/BlazorCanvas/Geometry/CircleEncoding.cs` | Reduced to two encode/decode members | VERIFIED | `FromCentreRadius`/`ToCentreRadius` only, `ClampDrawRadius` deleted |
| `src/BlazorCanvas/Geometry/MinSizeGuard.cs` | Per-type zero-extent guard reading `{dx,dy}`/`{w,h}`/`{r}` | VERIFIED | Confirmed by direct read, matches spec |
| `src/BlazorCanvas/Geometry/CanvasCoordinates.cs` | Non-finite-safe page-to-canvas mapping | VERIFIED | `double.IsFinite` guard present |
| `src/BlazorCanvas/Data/FigureStore.cs` | `MoveAsync` (anchor-only); `InsertAsync` taking encoded anchor+geometry | VERIFIED | Signatures match spec exactly |
| `src/BlazorCanvas/Sync/SyncMessage.cs` | Anchor+geometry record, four factories | VERIFIED | 7-field record confirmed |
| `src/BlazorCanvas/Sync/SyncReceiver.cs` | UI-free receiver rules | VERIFIED | Static class, draw/move/rollback/delete arms match D-40/D-53 |
| `src/BlazorCanvas/Geometry/ShapeRender.cs` | Anchor+geometry to SVG coordinates | VERIFIED | Exists, used by both components, tests pass |
| `src/BlazorCanvas/Components/Canvas/FigureShape.razor` | Renders from Type, X, Y, Geometry | VERIFIED | 7 `[Parameter]`-attributed members (incl. combined `[Parameter, EditorRequired]`), no `Box` param |
| `src/BlazorCanvas/Components/Canvas/SelectionTrace.razor` | Same parameters, same helper | VERIFIED | Confirmed via grep, no duplicated formatter |
| `src/BlazorCanvas/Components/Pages/Home.razor` | Anchor-only render/drag/broadcast sites | VERIFIED | All three `Geometry` bindings are `@`-prefixed expressions (10-06 defect fix present); D-54 discard code present |
| All 10 test files (Draw/CircleEncoding/MinSizeGuard/CanvasCoordinates/ShapeRender/FigureStore/CanvasSyncNotifier/SyncReceiver/CheckConstraint/SchemaShape/TypeWhitelistAndPersistence) | Reworked to anchor+geometry model | VERIFIED | All present on disk; full suite 416/416 green in an independent re-run |
| Deleted: `Movement.cs`, `ClampTests.cs`, `GuardMirrorsChecksTests.cs`, `UnitTest1.cs` | Absent | VERIFIED | Confirmed absent on disk; zero remaining `Movement.` references anywhere in `src/`/`tests/` |

### Key Link Verification

| From | To | Via | Status | Details |
|------|-----|-----|--------|---------|
| `DrawGesture.Build` | `CircleEncoding.FromCentreRadius` | Direct call, no clamp | VERIFIED | Confirmed by reading `DrawGesture.cs` |
| `MinSizeGuard` circle arm | `GeometryCodec.Encode`'s `{r}` | `CircleEncoding.ToCentreRadius` | VERIFIED | Same read-path, cannot disagree — confirmed |
| `Home.razor`'s drag state | `FigureStore.MoveAsync` | Anchor pair passed as two ints | VERIFIED | `CommitDragAsync` reads two anchor ints, no `Box` |
| `SyncMessage.Draw` | `Figure` entity | Reads `f.X`/`f.Y`/`f.Geometry` directly, no decode | VERIFIED | Confirmed in `SyncMessage.cs` |
| `Home.razor`'s `ApplyMessage` | `SyncReceiver.Apply` | Delegates list mutation | VERIFIED | Confirmed; selection cleared only on returned removed-id |
| `FigureShape`/`SelectionTrace` | `ShapeRender` | Shared triangle-points formatter, decoded once in `OnParametersSet` | VERIFIED | No duplicated formatter in either file |
| `Home.razor` render sites | `FigureShape`/`SelectionTrace`'s `Geometry` param | `@`-prefixed expression binding | VERIFIED | This is the exact 10-06 runtime defect and fix; confirmed fixed in current source |

### Behavioral Spot-Checks

| Behavior | Command | Result | Status |
|----------|---------|--------|--------|
| Clean build is 0 warnings/0 errors | `rm -rf bin obj && dotnet build BlazorCanvas.sln --nologo` | "Предупреждений: 0 / Ошибок: 0" | PASS |
| Full suite is green, zero skipped | `dotnet test BlazorCanvas.sln --nologo` (DB container up) | 416 passed, 0 failed, 0 skipped, 416 total | PASS |
| Landmine + off-canvas + SyncReceiver tests pass | `dotnet test --filter "FullyQualifiedName~Landmine\|OffCanvas\|SyncReceiverTests"` | 23 passed, 0 failed | PASS |
| Schema/constraint/persistence tests pass against live catalog | `dotnet test --filter "FullyQualifiedName~SchemaShapeTests\|CheckConstraintTests\|TypeWhitelistAndPersistenceTests"` | 27 passed, 0 failed | PASS |
| No lingering clamp logic in `src/` | `grep -rn -i clamp src/BlazorCanvas/` | Only doc comments stating no clamp, plus the unrelated int-domain overflow safety clamp in `CanvasCoordinates.FromPage` | PASS |
| No stray `Movement.` references | `grep -rn "Movement\." src/ tests/` | No matches | PASS |
| No debt markers in phase-touched files | `grep -n -E "TBD\|FIXME\|XXX\|TODO\|HACK\|PLACEHOLDER"` across 11 key source files | No matches | PASS |

### Requirements Coverage

| Requirement | Source Plan | Description | Status | Evidence |
|-------------|-------------|--------------|--------|----------|
| STOR-02 | 10-02, 10-04, 10-05 | Geometry lives relative to anchor; move touches only x,y | SATISFIED | `FigureStore.MoveAsync`, `FigureStoreTests`, `ShapeRenderTests` all confirmed |
| STOR-03 | 10-01, 10-05 | Well-formedness guaranteed by server code, not the DB | SATISFIED | `MinSizeGuard` confirmed; `SchemaShapeTests` confirms no CHECK on `geometry` |
| STOR-04 | 10-01, 10-02, 10-06 | Canvas-edge clamp removed | SATISFIED | No clamp calls anywhere; off-canvas round-trip tests pass; live checkpoint step D approved |
| STOR-05 | 10-02, 10-03, 10-04 | All four shapes draw/drag/delete, persist per-op | SATISFIED | Store, receiver, render tests pass; live checkpoint step B approved |
| SYNC-02 | 10-03, 10-06 | D-53 payload carries anchor+geometry, semantics unchanged | SATISFIED (with 1 flagged item) | `SyncMessage`/`SyncReceiver` confirmed and tested; live checkpoint step E draw/glide/delete/resurrection approved; the D-54 mid-drag discard specifically was not live-exercised (see Human Verification #2) |
| TEST-02 | 10-01, 10-05, 10-06 | Suite reworked to new model, clean build, full green | SATISFIED | Independently re-verified: 0 warnings, 416/416 tests, 0 skipped |

No orphaned requirements — REQUIREMENTS.md maps exactly STOR-02, STOR-03, STOR-04, STOR-05, SYNC-02, TEST-02 to Phase 10, and every plan in the phase declares at least one of these in its frontmatter `requirements:` field. All six are accounted for.

### Anti-Patterns Found

None. Scanned all 11 phase-modified core source files (`DrawGesture.cs`, `CircleEncoding.cs`, `MinSizeGuard.cs`, `CanvasCoordinates.cs`, `ShapeRender.cs`, `FigureStore.cs`, `SyncMessage.cs`, `SyncReceiver.cs`, `Home.razor`, `FigureShape.razor`, `SelectionTrace.razor`) for `TBD`/`FIXME`/`XXX`/`TODO`/`HACK`/`PLACEHOLDER` and similar markers — zero matches.

### Human Verification Required

### 1. Pre-rewrite appearance comparison (ROADMAP SC1)

**Test:** With a canvas that has figures drawn under the OLD (pre-v1.11) bounding-box model, compare every figure's on-screen position, size, shape, fill and outline against how it looked before the rewrite.
**Expected:** Pixel-identical — no figure moved, resized, or changed shape as a side effect of the storage-model rewrite.
**Why human:** The DB volume was reset during the 10-06 checkpoint to resolve a cross-branch schema conflict, destroying all pre-rewrite figures — there is currently no old data left to compare against. The coordinate-level half of this claim is proven automatically by `ShapeRenderTests`' cross-check against the retired bounding-box formulas; only the against-real-old-data visual half remains open. This is an environmental gap, not a code defect — flagged per the environment note's instruction to treat it as honestly unverified rather than a gap to reopen.

### 2. Mid-drag blanket broadcast discard (D-54, SYNC-02 backstop truth)

**Test:** Hold a drag in one browser tab while a second tab (same user, second window) draws, moves, or deletes a figure. Confirm the dragging tab shows nothing new until the drag ends and the tab is refreshed.
**Expected:** Every incoming broadcast is discarded while `dragging` is true in the receiving tab, not just broadcasts about the figure being dragged.
**Why human:** Requires two simultaneous pointer devices/windows acting at once; the 10-06 checkpoint environment had only one. The discard code (`if (dragging) { return; }` in `Home.razor`'s `HandleRemoteMessage`) is present and wired, and the adjacent 50ms trailing-edge glide behavior was confirmed live — only the discard invariant itself is unexercised.

### Gaps Summary

No gaps. All must-have artifacts exist, are substantive, and are wired correctly; the build is clean; the
full 416-test suite is green (independently re-verified, not just trusted from SUMMARY.md); every phase
requirement ID has passing automated coverage; and the one runtime defect found during the 10-06 human
checkpoint (the `Geometry="f.Geometry"` literal-vs-expression binding bug) is confirmed fixed in the
current codebase. The two open items are pre-existing environmental limitations honestly recorded as
unverified in 10-06-SUMMARY.md (not claimed as passed, not silently dropped) — a destroyed pre-rewrite
dataset and a single-pointer verification environment — and are carried forward here as human-verification
items rather than blockers, per the environment note's explicit guidance.

---

*Verified: 2026-07-24*
*Verifier: Claude (gsd-verifier)*
