---
phase: BC-01-database-schema-geometry-core
plan: 05
subsystem: database
tags: [geometry, clamp, csharp, xunit, gap-closure]

# Dependency graph
requires:
  - phase: BC-01-database-schema-geometry-core (plans 01-04)
    provides: the geometry core (Movement, CircleEncoding, MinSizeGuard, Normalisation), the Postgres schema with its CHECK constraints, and the original 145-test TEST-01 suite this plan hardens
provides:
  - A ClampDelta that returns 0 instead of silently inverting when lo > hi (closes CR-02)
  - A ClampDrawRadius that clamps its centre into the canvas and floors the result at 0, so it can never return a negative radius (closes CR-01)
  - Five new named regression tests proving both fixes, each RED on the pre-fix code
affects: [BC-02-drawing-interaction, any phase consuming Movement.ClampMove or CircleEncoding.ClampDrawRadius with raw pointer input]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Degenerate-range guard: when a clamp's lo/hi bounds can themselves invert (lo > hi), guard that case explicitly rather than trusting Math.Min/Math.Max ordering to fail safe."
    - "Compose clamp primitives: ClampDrawRadius now calls Movement.ClampDelta to clamp its centre, rather than duplicating clamp logic — one hardened primitive, reused."

key-files:
  created: []
  modified:
    - src/BlazorCanvas/Geometry/Movement.cs
    - src/BlazorCanvas/Geometry/CircleEncoding.cs
    - tests/BlazorCanvas.Tests/Geometry/ClampTests.cs
    - tests/BlazorCanvas.Tests/Geometry/CircleEncodingTests.cs

key-decisions:
  - "Fix lives entirely in C# clamp maths (Movement.ClampDelta, CircleEncoding.ClampDrawRadius) — no canvas-bounds CHECK constraint was added, per locked D-36 and the plan's scope fence."
  - "CircleEncoding.ClampDrawRadius clamps its centre by calling Movement.ClampDelta(cx, 0, Width) / (cy, 0, Height) rather than duplicating clamp logic — reuses the same hardened primitive Task 1 fixed."

patterns-established:
  - "A clamp function whose lo/hi bounds are themselves derived from untrusted input (box size, off-canvas centre) must guard the lo > hi degenerate case explicitly; Math.Min(Math.Max(v, lo), hi) alone silently inverts."

requirements-completed: [TEST-01]

coverage:
  - id: D1
    description: "ClampDelta returns 0 (not an inverted, sign-flipped value) when lo > hi, so ClampMove(oversizedBox, 0, 0) is the identity — closes CR-02"
    requirement: "TEST-01"
    verification:
      - kind: unit
        ref: "tests/BlazorCanvas.Tests/Geometry/ClampTests.cs#ClampMove_OversizedWidthBox_ZeroDelta_IsIdentity"
        status: pass
      - kind: unit
        ref: "tests/BlazorCanvas.Tests/Geometry/ClampTests.cs#ClampMove_OversizedHeightBox_ZeroDelta_IsIdentity"
        status: pass
      - kind: unit
        ref: "tests/BlazorCanvas.Tests/Geometry/ClampTests.cs#ClampDelta_WhenLoGreaterThanHi_ReturnsZero"
        status: pass
    human_judgment: false
  - id: D2
    description: "ClampDrawRadius clamps the press centre into the canvas and floors the result at 0, so an off-canvas centre can never produce a negative radius or a guard-accepted off-canvas circle — closes CR-01"
    requirement: "TEST-01"
    verification:
      - kind: unit
        ref: "tests/BlazorCanvas.Tests/Geometry/CircleEncodingTests.cs#ClampDrawRadius_OffCanvasCentre_IsNeverNegative"
        status: pass
      - kind: unit
        ref: "tests/BlazorCanvas.Tests/Geometry/CircleEncodingTests.cs#ClampDrawRadius_OffCanvasCentre_ProducesNoLegalCircle"
        status: pass
    human_judgment: false

duration: 15min
completed: 2026-07-15
status: complete
---

# Phase BC-01 Plan 05: Close CR-01/CR-02 Geometry Clamp Defects Summary

**Hardened `Movement.ClampDelta` (0 instead of inverting when lo > hi) and `CircleEncoding.ClampDrawRadius` (centre-clamp + zero-floor), each proven by a named regression test that reproduces the exact live database repro from 01-VERIFICATION.md and was RED on the pre-fix code.**

## Performance

- **Duration:** ~15 min
- **Completed:** 2026-07-15T07:51:00Z
- **Tasks:** 2
- **Files modified:** 4

## Accomplishments

- `Movement.ClampDelta` now returns 0 whenever `lo > hi` (the degenerate case reached exactly when a box is wider than 1280 or taller than 720). `ClampMove(oversizedBox, 0, 0)` is now the identity for every box, closing CR-02.
- `CircleEncoding.ClampDrawRadius` now clamps the press centre into the canvas (via `Movement.ClampDelta`) before computing the four edge-distance terms, and floors the final capped value at 0 with `Math.Max(0, ...)`. It can no longer return a negative radius, closing CR-01.
- Five new named regression tests added across `ClampTests.cs` and `CircleEncodingTests.cs`, each targeting the exact input region (oversized/out-of-canvas boxes; off-canvas centres) the original 145-test suite never probed. `ClampDrawRadius_OffCanvasCentre_ProducesNoLegalCircle` reproduces the exact live-database repro from `01-VERIFICATION.md` (`ClampDrawRadius(-5, 360, 50)` → now 0, not -5; the resulting normalised box is now rejected by `MinSizeGuard.IsDrawable`).
- Confirmed all 145 pre-existing tests still pass unchanged; full suite is now 153/153 green (145 + 8 new: 3 in ClampTests, 5 in CircleEncodingTests — 4 of which come from a `[Theory]` over the 4 off-canvas directions), 0 build warnings.
- Confirmed no non-`System` `using` directive was introduced in `src/BlazorCanvas/Geometry/*.cs` (purity check: `grep` count is 0) and no file outside the plan's four target files was touched — no CHECK constraint, migration, or schema change (D-36 honoured).

## Task Commits

Each task was committed atomically:

1. **Task 1: Guard ClampDelta against the inverted lo > hi case (CR-02)** - `93e485e` (fix)
2. **Task 2: Floor ClampDrawRadius at zero and clamp the centre into the canvas (CR-01)** - `481ed64` (fix)

**Plan metadata:** committed separately after this SUMMARY (docs: complete plan)

## Files Created/Modified

- `src/BlazorCanvas/Geometry/Movement.cs` - `ClampDelta` now returns 0 when `lo > hi`, else the existing `Math.Min(Math.Max(v, lo), hi)`
- `src/BlazorCanvas/Geometry/CircleEncoding.cs` - `ClampDrawRadius` clamps its centre via `Movement.ClampDelta` before capping, and floors the result at 0 via `Math.Max(0, ...)`
- `tests/BlazorCanvas.Tests/Geometry/ClampTests.cs` - added `ClampMove_OversizedWidthBox_ZeroDelta_IsIdentity`, `ClampMove_OversizedHeightBox_ZeroDelta_IsIdentity`, `ClampDelta_WhenLoGreaterThanHi_ReturnsZero`
- `tests/BlazorCanvas.Tests/Geometry/CircleEncodingTests.cs` - added `ClampDrawRadius_OffCanvasCentre_IsNeverNegative` (4-case theory), `ClampDrawRadius_OffCanvasCentre_ProducesNoLegalCircle`

## Decisions Made

- The fix stays entirely inside the two C# clamp functions; no canvas-bounds CHECK constraint was added to the schema, per locked D-36 and the plan's explicit scope fence.
- `ClampDrawRadius`'s centre clamp reuses `Movement.ClampDelta` rather than a bespoke clamp — one hardened primitive, composed, matching the plan's `key_links` guidance.

## Deviations from Plan

None - plan executed exactly as written. Both tasks matched the drafted fixes in `01-REVIEW.md` CR-01/CR-02 verbatim, including the worked examples used as regression-test fixtures.

One environmental note (not a deviation from the plan's own scope): the local Docker Desktop daemon and the `canvas-postgres` container were not running at the start of this session. Since the plan's `<verification>` block requires `dotnet test BlazorCanvas.sln` to exit 0 for the *full* solution (which includes the separate `Database` test category from prior plans, requiring a live Postgres connection), Docker Desktop was started and the container's health check was awaited before the final full-suite run, to get a genuine 153/153 green result rather than relying on the Geometry-only filtered runs alone. No files outside the plan's scope were touched to do this.

## Issues Encountered

None. Both fixes matched their `01-REVIEW.md` drafts exactly; both regression-test groups failed on the pre-fix code and passed after the fix, as designed.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

- CR-01 and CR-02 are closed: the clamp/circle-encoding maths is now robust against off-canvas and oversized input, which is exactly the input region BC-02's raw-pointer drag handler will produce.
- CR-03 (`CanvasDbContextFactory`'s hardcoded wrong-server fallback) remains open — it was explicitly out of this plan's scope (files_modified did not include `src/BlazorCanvas/Data/CanvasDbContextFactory.cs`) and is tracked separately in `01-REVIEW.md`/`01-VERIFICATION.md`.
- The full test suite (153 tests) is green with 0 build warnings; BC-02 can now build on top of a geometry core that has no known silent off-canvas escape.

---
*Phase: BC-01-database-schema-geometry-core*
*Completed: 2026-07-15*

## Self-Check: PASSED

All 4 modified files confirmed present on disk; all 2 task commits (`93e485e`, `481ed64`) confirmed in git log.
