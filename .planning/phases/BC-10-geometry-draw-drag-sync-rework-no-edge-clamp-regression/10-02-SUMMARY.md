---
phase: 10-geometry-draw-drag-sync-rework-no-edge-clamp-regression
plan: 02
subsystem: persistence, ui
tags: [csharp, blazor, ef-core, geometry, jsonb]

# Dependency graph
requires:
  - phase: 10-geometry-draw-drag-sync-rework-no-edge-clamp-regression
    plan: 01
    provides: unclamped DrawGesture, degenerate-draw guard re-expressed on GeometryCodec primitives, non-finite-safe CanvasCoordinates
provides:
  - "FigureStore.MoveAsync(userId, figureId, x, y) — anchor-only move; ExecuteUpdateAsync sets exactly f.X and f.Y, no type lookup, no GeometryCodec call"
  - "FigureStore.InsertAsync(userId, type, FigureGeometry) — takes an already-encoded anchor+geometry instead of a Box"
  - "Home.razor anchor-only drag state (dragFigure, dragAnchorX/Y, dragCurrentAnchorX/Y) replacing dragOriginalBox; ContinueDragAsync computes the current anchor as press-time anchor plus the raw pointer delta with no bounds arithmetic"
  - "dragCurrentBox as an explicit interim bridge, DERIVED from the current anchor plus the dragged figure's untouched geometry via GeometryCodec.DecodeToBox, for the still-box-shaped renderer and broadcast payload"
  - "Movement.cs and ClampTests.cs deleted — no move clamp exists anywhere in the solution"
  - "FigureStoreTests reworked: geometry-byte-identical-across-move (all four types), off-canvas anchor round-trip, move idempotency, identical-figure adjacency, post-mutation z ordering"
affects: [10-03-sync-payload-rework, 10-04-renderer-rework]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "A drag keeps a read-only reference to the dragged Figure (its Type and Geometry) for the whole gesture (PA-8), so the box the renderer/broadcast still need can be derived every pointer-move without a list lookup and without ever re-encoding — the geometry a move touches is exactly zero bytes of it."
    - "PostgreSQL's jsonb column canonicalises a JSON string's key order and spacing once, at INSERT time — not on every read. A 'byte-identical across N operations' proof must therefore capture its baseline via a reload, never the in-memory object InsertAsync/attribute-assignment produced, or the assertion silently compares two different canonicalization states instead of the property under test."

key-files:
  created: []
  modified:
    - src/BlazorCanvas/Data/FigureStore.cs
    - src/BlazorCanvas/Components/Pages/Home.razor
    - tests/BlazorCanvas.Tests/Data/FigureStoreTests.cs
  deleted:
    - src/BlazorCanvas/Geometry/Movement.cs
    - tests/BlazorCanvas.Tests/Geometry/ClampTests.cs

key-decisions:
  - "MoveAsync's four-parameter signature (userId, figureId, int x, int y) makes the anchor-only contract structural rather than a comment: there is no Box, FigureGeometry, or string parameter a caller could route geometry through, per planner assumption PA-3."
  - "dragCurrentBox is retained but re-scoped to an explicitly-commented interim bridge value, DERIVED every pointer-move from the anchor pair plus dragFigure.Geometry — never assigned by a clamp — because FigureShape and SyncMessage are still box-shaped until 10-04/10-03."
  - "CommitDragAsync's local apply and rollback both assign only figure.X/figure.Y directly, bypassing the pre-existing ApplyBox helper (which re-encodes via GeometryCodec.Encode) — ApplyBox stays in place for the remote-message path (ApplyMessage), which is out of this plan's scope and still box-shaped."

patterns-established:
  - "A drag keeps a read-only reference to the dragged Figure for the gesture's box-derivation bridge, rather than a repeated list lookup or a mutable copy (PA-8)."

requirements-completed: [STOR-02, STOR-04, STOR-05]

coverage:
  - id: D1
    description: "FigureStore.MoveAsync(userId, figureId, x, y) sets exactly f.X and f.Y under the user-scoped filter; it cannot express a geometry write because no parameter carries one"
    requirement: "STOR-02"
    verification:
      - kind: unit
        ref: "tests/BlazorCanvas.Tests/Data/FigureStoreTests.cs#MoveAsync_MovesFigure_ReturnsOneAffectedRow, MoveAsync_ForMissingFigure_ReturnsZeroAndThrowsNothing, MoveAsync_NeverTouchesAnotherUsersFigure"
        status: pass
    human_judgment: false
  - id: D2
    description: "A figure's stored Geometry string is byte-identical before and after a move, for all four figure types, proven against the reload PostgreSQL actually returns"
    requirement: "STOR-02"
    verification:
      - kind: unit
        ref: "tests/BlazorCanvas.Tests/Data/FigureStoreTests.cs#MoveAsync_GeometryStringIsByteIdenticalAcrossAMove(type: Line|Rectangle|Triangle|Circle)"
        status: pass
    human_judgment: false
  - id: D3
    description: "The drag path performs no canvas-bounds arithmetic — ContinueDragAsync computes the current anchor as the press-time anchor plus the raw pointer delta, with no min/max/width/height term — so a figure dragged toward the edge keeps going off-canvas"
    requirement: "STOR-04"
    verification:
      - kind: unit
        ref: "tests/BlazorCanvas.Tests/Data/FigureStoreTests.cs#MoveAsync_OffCanvasAnchor_PersistsUnchanged(-500,300 | 300,-500 | 2000,300 | 300,1200)"
        status: pass
    human_judgment: false
  - id: D4
    description: "Applying the same absolute anchor twice via MoveAsync leaves the figure in exactly the same place (a move carries an absolute anchor, never a delta)"
    requirement: "STOR-04"
    verification:
      - kind: unit
        ref: "tests/BlazorCanvas.Tests/Data/FigureStoreTests.cs#MoveAsync_AppliedTwiceWithSameAnchor_IsIdempotent"
        status: pass
    human_judgment: false
  - id: D5
    description: "Two figures with identical type, anchor and geometry remain two distinct rows with distinct ids and z; figures load in ascending z with id as tiebreak and that order survives a mixed sequence of draw, move and delete"
    requirement: "STOR-05"
    verification:
      - kind: unit
        ref: "tests/BlazorCanvas.Tests/Data/FigureStoreTests.cs#InsertAsync_TwoIdenticalFigures_RemainDistinctRows, LoadAsync_AfterMoveAndDelete_PreservesRelativeZOrderOfSurvivors"
        status: pass
    human_judgment: false
  - id: D6
    description: "Movement.cs (the move clamp) and ClampTests.cs no longer exist anywhere in the solution; the full solution builds clean with 0 warnings, 0 errors"
    requirement: "STOR-04"
    verification:
      - kind: unit
        ref: "dotnet build BlazorCanvas.sln --nologo (0 Warning(s), 0 Error(s)); test ! -f src/BlazorCanvas/Geometry/Movement.cs; test ! -f tests/BlazorCanvas.Tests/Geometry/ClampTests.cs"
        status: pass
    human_judgment: false
  - id: D7
    description: "The full test suite is green after the rework (regression check, not a plan-specific requirement but the plan's closing bar)"
    requirement: "STOR-02, STOR-04, STOR-05"
    verification:
      - kind: unit
        ref: "dotnet test BlazorCanvas.sln --nologo (367/367 pass)"
        status: pass
    human_judgment: false

duration: 20min
completed: 2026-07-24
status: complete
---

# Phase 10 Plan 02: Geometry Draw/Drag/Sync Rework — Anchor-Only Drag & Store Summary

**FigureStore.MoveAsync sets exactly x and y under the user-scoped filter (no geometry parameter exists to carry one), Home.razor's drag adds the raw pointer delta to the figure's anchor with zero bounds arithmetic, Movement.cs and its clamp tests are gone, and the store suite proves geometry survives byte-identical across any move for all four shapes.**

## Performance

- **Duration:** ~20 min
- **Started:** 2026-07-24 (session start)
- **Completed:** 2026-07-24T08:10:50Z
- **Tasks:** 3
- **Files modified:** 3 (+2 deleted)

## Accomplishments
- `FigureStore.UpdateAsync(userId, id, Box)` replaced by `MoveAsync(userId, id, int x, int y)`: a single `ExecuteUpdateAsync` setting exactly `f.X` and `f.Y`, with the preliminary type lookup and `GeometryCodec.Encode` call both deleted — the method's own signature makes "a move cannot write geometry" structural, not a comment (STOR-02, D-59, PA-3)
- `InsertAsync` now takes an already-encoded `FigureGeometry` instead of a `Box`; the internal `GeometryCodec.Encode` call moved to the one call site that needs it — `Home.razor`'s `CommitAsync`, keeping the guard-then-encode ordering (STOR-03) in exactly one place
- `Home.razor`'s drag state reworked to an integer anchor pair (`dragAnchorX/Y`, `dragCurrentAnchorX/Y`) plus a read-only `dragFigure` reference (PA-8), replacing `dragOriginalBox`; `ContinueDragAsync` computes the current anchor as the press-time anchor plus the raw pointer delta with no minimum, maximum, or canvas-dimension term anywhere on the path
- `dragCurrentBox` kept only as an explicitly-commented interim bridge — DERIVED every pointer-move from the current anchor and the dragged figure's untouched `Geometry` via `GeometryCodec.DecodeToBox` — because the renderer and `SyncMessage` are still box-shaped until 10-03/10-04
- `CommitDragAsync` assigns only `figure.X`/`figure.Y` locally (both on the success path and the D-52 rollback path) and calls `Figures.MoveAsync` with two integers; it never calls the pre-existing `ApplyBox` helper, which re-encodes geometry and would silently violate the "geometry never changes on a move" invariant
- `src/BlazorCanvas/Geometry/Movement.cs` and `tests/BlazorCanvas.Tests/Geometry/ClampTests.cs` deleted; the full-solution build is the proof no call site of the deleted class survives (STOR-04, D-24/D-29/D-36 dropped)
- `FigureStoreTests.cs` reworked to the anchor-only contract: renamed `MoveAsync_*` tests carry the IDOR proof forward; a new per-type theory proves the raw stored `Geometry` string is byte-identical across a move; new tests prove off-canvas anchors persist unchanged (one case per canvas side), moves are anchor-idempotent, identically-shaped figures stay distinct rows, and z-order survives a mixed draw/move/delete sequence

## Task Commits

Each task was committed atomically:

1. **Task 1: FigureStore writes the anchor only** - `81292b9` (feat)
2. **Task 2: Anchor-only drag in Home.razor, and delete the move clamp** - `79138cf` (feat)
3. **Task 3: Rework the FigureStore suite to the anchor-only contract** - `4a27e2d` (test)

**Plan metadata:** pending (this commit)

## Files Created/Modified
- `src/BlazorCanvas/Data/FigureStore.cs` - `UpdateAsync` → `MoveAsync(userId, id, int x, int y)`, an anchor-only single-`SetProperty`-pair update; `InsertAsync` now takes a `FigureGeometry`; class doc corrected
- `src/BlazorCanvas/Components/Pages/Home.razor` - Drag state reworked to an integer anchor pair plus a `dragFigure` reference; `ContinueDragAsync` drops `Movement.ClampMove` for raw-delta anchor arithmetic; `CommitDragAsync` writes/reads only the anchor; `CommitAsync` now encodes explicitly before calling the reshaped `InsertAsync`
- `tests/BlazorCanvas.Tests/Data/FigureStoreTests.cs` - Update tests renamed to the `MoveAsync` contract; new byte-identical-geometry, off-canvas, idempotency, adjacency and z-ordering tests added
- `src/BlazorCanvas/Geometry/Movement.cs` - Deleted
- `tests/BlazorCanvas.Tests/Geometry/ClampTests.cs` - Deleted

## Decisions Made
- Followed planner assumptions PA-3 (rename, not narrow — `MoveAsync`'s signature structurally excludes a geometry write) and PA-8 (the drag keeps a read-only `Figure` reference for the whole gesture, not a re-derived list lookup on every pointer-move) exactly as written.
- Discovered mid-Task-3 that PostgreSQL's `jsonb` column type canonicalises a stored JSON string's key order and whitespace once, at INSERT time — not on every read (e.g. `{"w":10,"h":10}` as written by `GeometryCodec.Encode` is returned by a subsequent `SELECT` as `{"h": 10, "w": 10}`). The plan's "byte-identical across a move" tests and the IDOR "unchanged victim" test both originally compared the in-memory `InsertAsync` result's `Geometry` (pre-canonicalization) against a post-move reload (post-canonicalization) and failed on that confound alone, with the move itself working correctly. Fixed by capturing every "before" baseline via an explicit reload, so both sides of every comparison are canonicalization-state-matched. This is a test-correctness fix (Rule 1 — auto-fixed bug in the plan's own worked test design), not a change to `FigureStore`, `Home.razor`, or the storage model; `MoveAsync` genuinely never touches `geometry`, which is exactly what these corrected tests now prove.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Fixed a canonicalization confound in the geometry-byte-identical and IDOR-unchanged tests**
- **Found during:** Task 3, first `dotnet test` run against the live database
- **Issue:** `MoveAsync_GeometryStringIsByteIdenticalAcrossAMove` and `MoveAsync_NeverTouchesAnotherUsersFigure` compared the in-memory `Geometry` string from `InsertAsync`'s return value against a value reloaded from PostgreSQL after a move/no-op. PostgreSQL's `jsonb` type reformats the JSON text once at INSERT time (canonical key order and spacing), so the two sides of the comparison were never guaranteed equal even when `MoveAsync` behaved correctly — 5 of 26 tests failed on the first run purely from this confound.
- **Fix:** Both tests now capture their "before" baseline via an explicit `store.LoadAsync` reload immediately after insert (or before the cross-user move attempt), so every comparison is reload-to-reload and the canonicalization state matches on both sides.
- **Files modified:** `tests/BlazorCanvas.Tests/Data/FigureStoreTests.cs`
- **Commit:** `4a27e2d` (folded into Task 3's single commit — discovered and fixed before the task's commit, per the shared auto-fix process)

**2. Verification-tooling note (not a code deviation), carried over from 10-01's pattern**
- Task 1's acceptance criterion `grep -c 'f.UserId == userId' src/BlazorCanvas/Data/FigureStore.cs` returns `3` expects `3` (move, delete, load). The actual count is `4`: `InsertAsync`'s pre-existing `z = max(z) + 1` scoping query also contains the term (unrelated to the IDOR guard, present before this plan too, and unchanged by Task 1). The substantive criterion — the IDOR term survives on `MoveAsync`, `DeleteAsync` and `LoadAsync` — is met.
- Task 2's acceptance criterion `grep -c 'CanvasBounds' src/BlazorCanvas/Components/Pages/Home.razor` returns `2` expects `2`; the actual `grep -c` (line-count) result is `1` because both `CanvasBounds.Width` and `CanvasBounds.Height` sit on the same `<svg>` line — `grep -o | wc -l` correctly counts `2` occurrences. No bounds term exists on any pointer path either way.

## Issues Encountered
None beyond the test-baseline confound documented above, which was resolved within Task 3 before its commit.

## User Setup Required
None — the Compose PostgreSQL container was already up and healthy per the environment note; no `BLAZORCANVAS_TEST_CONNECTION` override was set (the stale Phase-9 operational note was correctly disregarded).

## Next Phase Readiness
- `SyncMessage.cs`, `FigureShape.razor` and `SelectionTrace.razor` are confirmed unchanged by this plan (verified via the task-scoped file lists and `git status`).
- `dragCurrentBox` is explicitly marked in code comments as an interim bridge for 10-04 to remove once `FigureShape` takes the anchor and geometry directly; `Home.razor`'s render sites (`FigureBox`, `ApplyBox`, `ApplyMessage`, `HandleRemoteMessage`, `BroadcastReloadedSnapshot`, `HandleDeleteAsync`, `ReloadFromDatabaseAsync`) were left structurally untouched, per the plan's own scope boundary — 10-03 reworks the payload, 10-04 reworks the renderer.
- Full solution builds clean (0 warnings, 0 errors); full test suite is 367/367 green (down from the 378 baseline noted in the environment context by the 21 deleted `ClampTests.cs` cases, offset by 10 new `FigureStoreTests` cases net of the one replaced circle-translation test).

---
*Phase: 10-geometry-draw-drag-sync-rework-no-edge-clamp-regression*
*Completed: 2026-07-24*

## Self-Check: PASSED

All 3 modified files verified present on disk; both deleted files verified absent. All 3 task commits (`81292b9`, `79138cf`, `4a27e2d`) verified present in git history. Full solution build: 0 warnings, 0 errors. Full test suite: 367/367 passing.
