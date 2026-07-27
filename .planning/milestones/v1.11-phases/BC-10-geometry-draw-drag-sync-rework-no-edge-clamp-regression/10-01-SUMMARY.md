---
phase: 10-geometry-draw-drag-sync-rework-no-edge-clamp-regression
plan: 01
subsystem: database
tags: [csharp, blazor, geometry, jsonb]

# Dependency graph
requires:
  - phase: 09-schema-entity-migration
    provides: anchor+geometry schema, Figure entity, GeometryCodec, data-preserving migration
provides:
  - Unclamped DrawGesture.Build — a draw gesture reproduces the pointer's press/cursor extent exactly, for all four figure types
  - CircleEncoding reduced to FromCentreRadius/ToCentreRadius; the circle draw-clamp is deleted
  - MinSizeGuard re-expressed on the exact {dx,dy}/{w,h}/{r} integers GeometryCodec serialises, rejecting only strictly-zero-extent gestures per type
  - CanvasCoordinates.FromPage made total over double (non-finite input maps to 0) — the T-10-01 mitigation for the removed clamp's incidental safety net
  - Reworked DrawGestureTests, CircleEncodingTests, MinSizeGuardTests, CanvasCoordinatesTests pinning the no-clamp contract
affects: [10-02-drag-and-sync-rework]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Degenerate-draw guard reads the exact serialisation primitives (GeometryCodec's {dx,dy}/{w,h}/{r}), not a re-derived shape predicate, so the guard and the codec can never disagree"

key-files:
  created: []
  modified:
    - src/BlazorCanvas/Geometry/DrawGesture.cs
    - src/BlazorCanvas/Geometry/CircleEncoding.cs
    - src/BlazorCanvas/Geometry/Box.cs
    - src/BlazorCanvas/Geometry/CanvasBounds.cs
    - src/BlazorCanvas/Geometry/MinSizeGuard.cs
    - src/BlazorCanvas/Geometry/CanvasCoordinates.cs
    - tests/BlazorCanvas.Tests/Geometry/DrawGestureTests.cs
    - tests/BlazorCanvas.Tests/Geometry/CircleEncodingTests.cs
    - tests/BlazorCanvas.Tests/Geometry/MinSizeGuardTests.cs
    - tests/BlazorCanvas.Tests/Geometry/CanvasCoordinatesTests.cs

key-decisions:
  - "PA-1/PA-7/PA-5 (planner assumptions, applied as written): Box retained as a transient extent (doc corrected, not renamed); MinSizeGuard's circle arm drops square-ness/even-side terms and reads the radius through CircleEncoding.ToCentreRadius; CanvasCoordinates.FromPage gains a non-finite guard that is an int-domain bound, not a reinstated canvas clamp."
  - "MinSizeGuard's line/rectangle/triangle arms rewritten from bounding-box comparisons (X2>=X1, X2>X1 && Y2>Y1) to Box.Width/Height reads, matching the {dx,dy}/{w,h} pairs GeometryCodec actually serialises, per D-59 item 7."

patterns-established:
  - "Degenerate-draw guard reads the exact serialisation primitives (GeometryCodec's {dx,dy}/{w,h}/{r}), not a re-derived shape predicate, so the guard and the codec can never disagree (T-10-02)."

requirements-completed: [STOR-03, STOR-04, TEST-02]

coverage:
  - id: D1
    description: "DrawGesture.Build no longer clamps any coordinate to the canvas for any of the four figure types; a gesture may extend wholly or partly past the canvas edge"
    requirement: "STOR-04"
    verification:
      - kind: unit
        ref: "tests/BlazorCanvas.Tests/Geometry/DrawGestureTests.cs#CornerToCorner_FarCorner_IsReproducedUnclamped, CornerToCorner_NegativeOrigin_IsReproducedUnclamped, Circle_NearLeftEdge_KeepsFullRadius_AndExtendsOffCanvas, EveryResult_ReproducesTheGesture_WithNoClamp"
        status: pass
    human_judgment: false
  - id: D2
    description: "MinSizeGuard's three arms read the exact integer primitives GeometryCodec will serialise ({dx,dy}/{w,h}/{r}) and reject only a strictly-zero-extent gesture per type; the circle arm drops the retired square-ness/even-side terms"
    requirement: "STOR-03"
    verification:
      - kind: unit
        ref: "tests/BlazorCanvas.Tests/Geometry/MinSizeGuardTests.cs#Circle_OddSide_IsAccepted, Circle_NotSquare_IsAccepted, Circle_WidthRoundsRadiusToZero_IsRejected, PerType_ExactlyZeroExtent_IsRejected_OneStepBeyond_IsAccepted"
        status: pass
    human_judgment: false
  - id: D3
    description: "CanvasCoordinates.FromPage is total over double — NaN and both infinities map to a defined (0,0) instead of reaching an undefined (int) cast; all existing finite-input results stay byte-identical"
    requirement: "TEST-02"
    verification:
      - kind: unit
        ref: "tests/BlazorCanvas.Tests/Geometry/CanvasCoordinatesTests.cs#FromPage_NonFiniteInput_MapsToZeroZero"
        status: pass
    human_judgment: false
  - id: D4
    description: "The geometry test suite is reworked to the no-clamp model: edge-clamp tests repurposed to unclamped-reproduction tests, the circle round-trip re-expressed as a geometry {r} storage assertion, and the whole geometry namespace passes clean"
    requirement: "TEST-02"
    verification:
      - kind: unit
        ref: "dotnet test BlazorCanvas.sln --nologo --filter \"FullyQualifiedName~BlazorCanvas.Tests.Geometry\" (319/319 pass)"
        status: pass
    human_judgment: false

duration: 8min
completed: 2026-07-24
status: complete
---

# Phase 10 Plan 01: Unclamp Draw & Re-express the Degenerate-Draw Guard Summary

**DrawGesture reproduces pointer coordinates exactly with no canvas-edge clamp, MinSizeGuard now reads the exact {dx,dy}/{w,h}/{r} primitives GeometryCodec serialises, and CanvasCoordinates.FromPage is total over every double.**

## Performance

- **Duration:** 8 min
- **Started:** 2026-07-24T03:11:19Z
- **Completed:** 2026-07-24T03:19:05Z
- **Tasks:** 3
- **Files modified:** 10

## Accomplishments
- Removed the four `Movement.ClampDelta` calls from `DrawGesture.Build` and the circle draw-clamp from `CircleEncoding` — a rectangle/triangle/line/circle gesture now reproduces the pointer's press/cursor extent exactly, including past the canvas edge and into negative coordinates (STOR-04)
- `CircleEncoding` reduced to its two encode/decode members (`FromCentreRadius`, `ToCentreRadius`); `ClampDrawRadius` deleted entirely
- `MinSizeGuard`'s three arms rewritten to read the exact primitives `GeometryCodec.Encode` serialises — the line's `{dx,dy}`, the rectangle/triangle's `{w,h}`, and the circle's `{r}` via `CircleEncoding.ToCentreRadius` — rejecting only a strictly-zero-extent gesture per type and closing the width-1-rounds-to-radius-0 poison-row hole (STOR-03, T-10-02)
- `CanvasCoordinates.FromPage` routed through a new non-finite-safe helper: NaN/±Infinity now map to a defined `(0, 0)` instead of reaching an undefined `(int)` cast (T-10-01), with every existing finite-input result staying byte-identical
- `Box`, `CanvasBounds`, and `CanvasCoordinates` doc comments corrected to stop describing a clamp or a bounding-box storage model, now naming D-59's anchor+geometry model
- All four geometry test files reworked to the no-clamp model per TEST-02: edge-clamp tests repurposed (not merely deleted) into unclamped-reproduction tests, the circle round-trip re-expressed as a `{r}` storage assertion via `GeometryCodec`, and a per-type zero-vs-one-step boundary theory added for `MinSizeGuard`

## Task Commits

Each task was committed atomically:

1. **Task 1: Unclamp the draw gesture and delete the circle draw-clamp** - `af98e5d` (feat)
2. **Task 2: Re-express MinSizeGuard on the geometry primitives, and make FromPage non-finite-safe** - `6f99f44` (feat)
3. **Task 3: Rework the geometry tests to the no-clamp model** - `e2b7a0d` (test)

**Plan metadata:** pending (this commit)

## Files Created/Modified
- `src/BlazorCanvas/Geometry/DrawGesture.cs` - Removed the four-coordinate clamp; circle arm computes radius directly from press-cursor distance
- `src/BlazorCanvas/Geometry/CircleEncoding.cs` - `ClampDrawRadius` deleted; class reduced to `FromCentreRadius`/`ToCentreRadius`
- `src/BlazorCanvas/Geometry/Box.cs` - Doc comment corrected: transient extent, not the storage model
- `src/BlazorCanvas/Geometry/CanvasBounds.cs` - Doc comment corrected: sizes the SVG surface only, no clamp domain
- `src/BlazorCanvas/Geometry/MinSizeGuard.cs` - Three arms rewritten to read `{dx,dy}`/`{w,h}`/`{r}` primitives; retired square-ness/even-side circle terms dropped
- `src/BlazorCanvas/Geometry/CanvasCoordinates.cs` - Added a private non-finite-safe int conversion helper; both axes of `FromPage` route through it
- `tests/BlazorCanvas.Tests/Geometry/DrawGestureTests.cs` - Clamp assertions repurposed to unclamped-reproduction assertions; invariant grid theory repurposed to a no-clamp check
- `tests/BlazorCanvas.Tests/Geometry/CircleEncodingTests.cs` - Seven `ClampDrawRadius` tests deleted; translation test no longer depends on `Movement`; adds a `{r}`-literal storage round-trip test
- `tests/BlazorCanvas.Tests/Geometry/MinSizeGuardTests.cs` - Circle odd-side/not-square tests flipped from rejected to accepted; adds a poison-row rejection test and a per-type zero-vs-one-step boundary theory
- `tests/BlazorCanvas.Tests/Geometry/CanvasCoordinatesTests.cs` - Adds NaN/Infinity cases per axis; corrects two stale comments

## Decisions Made
- Followed the plan's three planner assumptions (PA-1, PA-7, PA-5) as written — `Box` retained rather than renamed, `MinSizeGuard`'s circle arm reads the radius through `CircleEncoding.ToCentreRadius` rather than a naive width check, and `CanvasCoordinates`'s non-finite guard is documented explicitly as an int-domain bound, not a reinstated canvas clamp.
- `MinSizeGuard`'s line/rectangle/triangle arms were rewritten from raw `X1`/`X2`/`Y1`/`Y2` comparisons to `Box.Width`/`Box.Height` reads — behaviourally identical to the plan's stated rule, but expressed directly in terms of the `{dx,dy}`/`{w,h}` pairs the codec serialises, per the plan's own framing of D-59 item 7.

## Deviations from Plan

None — plan executed exactly as written. Two minor verification-tooling notes, not code deviations:

1. **Task 1's `grep -c 'public static' CircleEncoding.cs` acceptance criterion expects `2`; the actual result is `3`.** The grep pattern also matches the `public static class CircleEncoding` declaration line (which it did before this plan too — the original file's count was `4`, not `3`, for the same reason). The substantive criterion — `CircleEncoding` reduced to exactly two public members (`FromCentreRadius`, `ToCentreRadius`) — is met; this is a plan-authoring quirk in the grep pattern itself, not a code gap. No fix applied since making the class non-`public static` is not an option.
2. **Task 1 and Task 2's `dotnet build BlazorCanvas.sln` acceptance criterion cannot pass in isolation after Task 1 or Task 2** because `CircleEncodingTests.cs` still referenced the (now-deleted) `ClampDrawRadius` method until Task 3 reworked it — an inherent consequence of splitting geometry-core changes (Tasks 1-2) from test rework (Task 3) into separate commits. Verified Tasks 1 and 2 instead via `dotnet build src/BlazorCanvas/BlazorCanvas.csproj` (0 warnings, 0 errors) plus the grep-based structural checks; the full-solution build was confirmed clean only after Task 3, per that task's own `<verify>` block.

## Issues Encountered
None.

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- `Movement.ClampMove` (the drag clamp) is deliberately untouched — `Home.razor` still calls it. 10-02 deletes it together with the drag rework.
- `Normalisation.cs`, `GeometryCodec.cs`, `Movement.cs`, `Home.razor`, `FigureStore.cs`, and `SyncMessage.cs` are confirmed unchanged by this plan (verified via `git diff --stat`).
- Full solution builds clean (0 warnings, 0 errors); the geometry namespace test suite is 319/319 green. The 42 Database/Migration test failures observed in a full `dotnet test` run are pre-existing and environmental (Docker Compose Postgres not running locally at the time of this plan's execution) — unrelated to this plan's changes, and out of this plan's scope per its `<verification>` block (which is itself scoped to the geometry namespace filter).

---
*Phase: 10-geometry-draw-drag-sync-rework-no-edge-clamp-regression*
*Completed: 2026-07-24*

## Self-Check: PASSED

All 10 created/modified files verified present on disk. All 3 task commits (`af98e5d`, `6f99f44`, `e2b7a0d`) verified present in git history.
