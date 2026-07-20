---
phase: BC-01-database-schema-geometry-core
plan: 02
subsystem: geometry
tags: [csharp, dotnet10, xunit, pure-functions, geometry]

# Dependency graph
requires:
  - phase: BC-01-database-schema-geometry-core (plan 01)
    provides: "Two-project .NET 10 solution (src/BlazorCanvas app project, tests/BlazorCanvas.Tests xUnit project)"
provides:
  - "BlazorCanvas.Geometry namespace: FigureType, FigureTypeNames, Box, CanvasBounds"
  - "Normalisation.Normalise — per-type canonical order on write (D-41), including the line whole-point-pair swap"
  - "MinSizeGuard.IsDrawable — per-type draw rejection mirroring the three CHECK constraints exactly (D-50)"
  - "Movement.ClampDelta / ClampMove — the move clamp: clamp delta, translate uniformly, inclusive bounds (D-36)"
  - "CircleEncoding.FromCentreRadius / ToCentreRadius / ClampDrawRadius — inscribed-square circle encoding and the circle draw-clamp (D-22, D-13, D-24, D-29)"
  - "All three mandated TEST-01 tests passing (clamp maths, circle round-trip, line normalisation)"
affects: [BC-01-04, phase-3-drawing, phase-4-drag, phase-5-sync]

# Tech tracking
tech-stack:
  added: []
  patterns: ["Pure static classes with no I/O for domain maths (D-49)", "readonly record struct for value-type geometry (Box)", "Per-type switch expressions instead of shared/generic rules where the domain requires type-specific behavior (D-50)"]

key-files:
  created:
    - src/BlazorCanvas/Geometry/FigureType.cs
    - src/BlazorCanvas/Geometry/FigureTypeNames.cs
    - src/BlazorCanvas/Geometry/Box.cs
    - src/BlazorCanvas/Geometry/CanvasBounds.cs
    - src/BlazorCanvas/Geometry/Normalisation.cs
    - src/BlazorCanvas/Geometry/MinSizeGuard.cs
    - src/BlazorCanvas/Geometry/Movement.cs
    - src/BlazorCanvas/Geometry/CircleEncoding.cs
    - tests/BlazorCanvas.Tests/Geometry/NormalisationTests.cs
    - tests/BlazorCanvas.Tests/Geometry/MinSizeGuardTests.cs
    - tests/BlazorCanvas.Tests/Geometry/ClampTests.cs
    - tests/BlazorCanvas.Tests/Geometry/CircleEncodingTests.cs
  modified: []

key-decisions:
  - "ClampDrawRadius rounds the input distance with MidpointRounding.AwayFromZero (10.5 -> 11) since the plan specified 'rounds the distance' without pinning a rounding mode and away-from-zero is the intuitive human reading"
  - "FigureTypeNames/CanvasBounds tests were placed inside NormalisationTests.cs (as separate test classes) rather than a new file, since the plan's files_modified list did not include a dedicated file for them"

patterns-established:
  - "Every Geometry/*.cs file carries zero non-System using directives — purity is enforced by a grep gate, not just convention"

requirements-completed: [TEST-01]

coverage:
  - id: D1
    description: "Line normalisation swaps the whole point pair — an up-and-right diagonal does not become the opposite diagonal (D-41, mandated test 3)"
    requirement: TEST-01
    verification:
      - kind: unit
        ref: "tests/BlazorCanvas.Tests/Geometry/NormalisationTests.cs#Line_UpAndRightDiagonal_IsNotFlippedToOppositeDiagonal"
        status: pass
    human_judgment: false
  - id: D2
    description: "Rectangle/triangle/circle normalisation sorts axes independently (D-41)"
    requirement: TEST-01
    verification:
      - kind: unit
        ref: "tests/BlazorCanvas.Tests/Geometry/NormalisationTests.cs#Rectangle_OppositeDiagonal_GetsAxisSort"
        status: pass
    human_judgment: false
  - id: D3
    description: "The move clamp clamps the delta then translates uniformly; per-axis independence holds (D-36, mandated test 1)"
    requirement: TEST-01
    verification:
      - kind: unit
        ref: "tests/BlazorCanvas.Tests/Geometry/ClampTests.cs#FlushRightEdge_XClippedToZero_YPassesThroughAtFullDelta"
        status: pass
    human_judgment: false
  - id: D4
    description: "Bounds are inclusive: 0..1280 x 0..720"
    requirement: TEST-01
    verification:
      - kind: unit
        ref: "tests/BlazorCanvas.Tests/Geometry/ClampTests.cs#LargeDownwardDelta_LandsExactlyAtY2Equals720"
        status: pass
    human_judgment: false
  - id: D5
    description: "Circle stored as inscribed square; centre/radius exact after encode+decode and after N translations (D-22, mandated test 2)"
    requirement: TEST-01
    verification:
      - kind: unit
        ref: "tests/BlazorCanvas.Tests/Geometry/CircleEncodingTests.cs#Radius_SurvivesTenSuccessiveTranslations_IncludingTwoEdgeClipped"
        status: pass
    human_judgment: false
  - id: D6
    description: "Per-type min-size guard mirrors the three CHECK constraints exactly (D-50); horizontal/vertical lines accepted, zero-height rectangle rejected"
    requirement: TEST-01
    verification:
      - kind: unit
        ref: "tests/BlazorCanvas.Tests/Geometry/MinSizeGuardTests.cs#PerTypeGuard_HorizontalLineLegal_ButZeroHeightRectangleIllegal"
        status: pass
    human_judgment: false
  - id: D7
    description: "Circle draw-clamp caps radius at the nearest edge (D-24, D-29), including the accepted tiny-circle-near-edge consequence"
    requirement: TEST-01
    verification:
      - kind: unit
        ref: "tests/BlazorCanvas.Tests/Geometry/CircleEncodingTests.cs#ClampDrawRadius_NearLeftEdge_ForcesATinyCircle"
        status: pass
    human_judgment: false
  - id: D8
    description: "The geometry core is pure C# — no Blazor, no EF Core, no I/O (D-49)"
    requirement: TEST-01
    verification:
      - kind: unit
        ref: "grep -h '^using' src/BlazorCanvas/Geometry/*.cs | grep -v '^using System' | wc -l -> 0"
        status: pass
    human_judgment: false

duration: 7min
completed: 2026-07-14
status: complete
---

# Phase BC-01 Plan 02: Pure C# Geometry Core Summary

**Pure C# geometry core in `BlazorCanvas.Geometry` — per-type normalisation, per-type min-size guard, the delta-clamp move formula, and circle-as-inscribed-square encoding — proven by all three TEST-01 mandated tests (line landmine, clamp per-axis independence, circle round-trip under translation), 77/77 tests green.**

## Performance

- **Duration:** 7 min
- **Started:** 2026-07-14T21:11:15Z
- **Completed:** 2026-07-14T21:18:12Z
- **Tasks:** 3
- **Files modified:** 12 (8 implementation, 4 test files)

## Accomplishments
- `FigureType`, `FigureTypeNames` (exact lowercase DB literals per D-46), `Box` (always the bounding box, D-20/D-22), `CanvasBounds` (1280x720, D-19)
- `Normalisation.Normalise` — rectangle/triangle/circle sort axes independently; a line swaps the whole point pair, never its axes independently (D-41 landmine test 3 passes: the up-and-right diagonal does NOT become the opposite diagonal)
- `MinSizeGuard.IsDrawable` — per-type rejection, a literal transcription of `line_is_a_line`/`box_is_a_box`/`circle_is_a_circle` (D-50); proves D-23's retracted shared guard is not what was built (horizontal line legal, zero-height rectangle illegal)
- `Movement.ClampDelta`/`ClampMove` — clamps the movement delta then translates all four coordinates uniformly, inclusive `0..1280 x 0..720` bounds, and recomputes the min/max bounding box so a normalised line with `Y1 > Y2` still clamps correctly (D-36 mandated test 1 passes)
- `CircleEncoding.FromCentreRadius`/`ToCentreRadius`/`ClampDrawRadius` — exact integer round-trip, radius proven identical across 10 successive translations including 2 edge-clipped ones, and the circle draw-clamp `r = min(round(distance), cx, cy, W-cx, H-cy)` with the accepted tiny-circle-near-the-edge consequence asserted, not "fixed" (D-22 mandated test 2 passes)
- `dotnet test BlazorCanvas.sln` exits 0 with 77/77 tests passing; `dotnet build BlazorCanvas.sln` exits 0 with 0 warnings
- Purity gate: `grep -h '^using' src/BlazorCanvas/Geometry/*.cs | grep -v '^using System' | wc -l` prints `0` — the core carries no Blazor and no EF dependency (D-49)

## Task Commits

Each task followed the RED -> GREEN TDD cycle and was committed atomically:

1. **Task 1: Core types, per-type normalisation, per-type min-size guard**
   - `8e9822d` (test) — failing NormalisationTests.cs + MinSizeGuardTests.cs
   - `4f0dcf2` (feat) — FigureType, FigureTypeNames, Box, CanvasBounds, Normalisation, MinSizeGuard
2. **Task 2: The move clamp**
   - `9219fa9` (test) — failing ClampTests.cs
   - `8af7d06` (feat) — Movement.ClampDelta / ClampMove
3. **Task 3: Circle inscribed-square encoding and draw-clamp**
   - `4fdb77f` (test) — failing CircleEncodingTests.cs
   - `1488ddf` (feat) — CircleEncoding.FromCentreRadius / ToCentreRadius / ClampDrawRadius

**Plan metadata:** (pending — final docs commit follows this SUMMARY)

## Files Created/Modified
- `src/BlazorCanvas/Geometry/FigureType.cs` - the four figure types enum (D-12)
- `src/BlazorCanvas/Geometry/FigureTypeNames.cs` - enum <-> exact lowercase DB literal mapping (D-46)
- `src/BlazorCanvas/Geometry/Box.cs` - `readonly record struct Box(X1,Y1,X2,Y2)` with Width/Height
- `src/BlazorCanvas/Geometry/CanvasBounds.cs` - `const int Width = 1280, Height = 720` (D-19)
- `src/BlazorCanvas/Geometry/Normalisation.cs` - per-type canonical order on write (D-41)
- `src/BlazorCanvas/Geometry/MinSizeGuard.cs` - per-type draw rejection mirroring the CHECKs (D-50)
- `src/BlazorCanvas/Geometry/Movement.cs` - the delta clamp, inclusive bounds (D-36)
- `src/BlazorCanvas/Geometry/CircleEncoding.cs` - circle <-> inscribed square, plus the draw-clamp (D-22, D-13, D-24, D-29)
- `tests/BlazorCanvas.Tests/Geometry/NormalisationTests.cs` - normalisation tests + FigureTypeNames/CanvasBounds tests
- `tests/BlazorCanvas.Tests/Geometry/MinSizeGuardTests.cs` - min-size guard tests
- `tests/BlazorCanvas.Tests/Geometry/ClampTests.cs` - mandated test 1 of TEST-01
- `tests/BlazorCanvas.Tests/Geometry/CircleEncodingTests.cs` - mandated test 2 of TEST-01

## Decisions Made
- `ClampDrawRadius` rounds the input `distance` with `MidpointRounding.AwayFromZero` (so `10.5` rounds to `11`). The plan specified "rounds the distance before capping" without pinning a rounding mode; away-from-zero was chosen as the more intuitive reading and is asserted by a dedicated test.
- `FigureTypeNames` and `CanvasBounds` tests were added as additional test classes inside `NormalisationTests.cs` rather than new files, since the plan's `files_modified` list for Task 1 named only `NormalisationTests.cs` and `MinSizeGuardTests.cs` as test outputs.

## Deviations from Plan

None - plan executed exactly as written. All three mandated TEST-01 tests pass; all acceptance criteria for all three tasks were met on the first implementation pass with no auto-fixes required.

## Issues Encountered

None.

## User Setup Required

None - no external service configuration required. This plan touches only `src/BlazorCanvas/Geometry/` and `tests/BlazorCanvas.Tests/Geometry/`; no database or Docker interaction.

## Next Phase Readiness
- ROADMAP success criterion 4 is fully satisfied: the three mandated TEST-01 tests pass.
- The geometry core (`BlazorCanvas.Geometry`) is pure C#, carries no Blazor/EF dependency, and is ready for Phases 3-5 to call for drawing, dragging, and sync.
- `MinSizeGuard`'s three predicates are a literal transcription of the three CHECK constraints plan 01-03 will implement — plan 01-04 must prove they agree against the live database (this plan only proves the C#-side half).
- No `Data/`, `Migrations/`, or `Program.cs` changes were made — those remain untouched for the sibling plan 01-03, which runs next in this wave.

---
*Phase: BC-01-database-schema-geometry-core*
*Completed: 2026-07-14*

## Self-Check: PASSED

All 8 created implementation files and 4 created test files confirmed present on disk; all 6 task commits (`8e9822d`, `4f0dcf2`, `9219fa9`, `8af7d06`, `4fdb77f`, `1488ddf`) and the summary commit (`f0c0498`) confirmed present in git history.
