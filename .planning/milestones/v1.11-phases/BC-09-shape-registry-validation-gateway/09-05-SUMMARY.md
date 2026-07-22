---
phase: BC-09-shape-registry-validation-gateway
plan: 05
subsystem: shape-validation
tags: [csharp, xunit, shapes, geometry, regression]
requires:
  - phase: BC-09-04
    provides: Four shipped IShapeDefinition implementations and a fresh DefaultShapes registry
provides:
  - Exact v1.1 gesture-equivalence coverage over the complete 196-case boundary grid
  - A test-only fifth shape proving registry extensibility without production or schema changes
  - Point-list primacy guards for line and triangle geometry
affects: [BC-09-06, BC-10, BC-11, BC-12]
tech-stack:
  added: []
  patterns:
    - Exact legacy-versus-registry contract tests with no floating-point tolerance
    - Test-only shape implementations registered through IShapeDefinition
    - Same-bounds/different-JSON regression guards for point-list geometry
key-files:
  created:
    - tests/BlazorCanvas.Tests/Shapes/V11GestureEquivalenceTests.cs
    - tests/BlazorCanvas.Tests/Shapes/PentagonShape.cs
    - tests/BlazorCanvas.Tests/Shapes/ShapeRegistryExtensibilityTests.cs
    - tests/BlazorCanvas.Tests/Shapes/PointListPrimacyTests.cs
  modified: []
key-decisions:
  - "Legacy equivalence uses exact double equality; any arithmetic drift is a behavioural defect."
  - "The fifth shape remains test-only with its geometry record privately nested in the one shape class."
  - "Line and triangle same-bounds/different-JSON pairs are permanent guards against bbox-derived geometry."
patterns-established:
  - "Temporary cutover tests may reference v1.1 types but must state their planned Phase 11 removal."
  - "Shape extension tests resolve new definitions only through IShapeDefinition after registration."
requirements-completed: [SHAPE-01, SHAPE-02, SHAPE-03]
coverage:
  - id: D1
    description: "The registry exactly reproduces v1.1 bounds, drawability, endpoints, triangle vertices, and circle centre/radius across the invariant gesture grid."
    requirement: SHAPE-01
    verification:
      - kind: unit
        ref: "tests/BlazorCanvas.Tests/Shapes/V11GestureEquivalenceTests.cs; dotnet test --nologo"
        status: pass
    human_judgment: false
  - id: D2
    description: "Line and triangle retain ordered point-list geometry, including downward and sideways triangle data, rather than deriving it from bounds."
    requirement: SHAPE-02
    verification:
      - kind: unit
        ref: "tests/BlazorCanvas.Tests/Shapes/PointListPrimacyTests.cs"
        status: pass
    human_judgment: false
  - id: D3
    description: "A fifth unshipped shape round-trips through the registry interface without production, database, or schema changes."
    requirement: SHAPE-03
    verification:
      - kind: unit
        ref: "tests/BlazorCanvas.Tests/Shapes/ShapeRegistryExtensibilityTests.cs (PostgreSQL stopped)"
        status: pass
    human_judgment: false
duration: 25min
completed: 2026-07-22
status: complete
---

# Phase BC-09 Plan 05: Registry Contract Proofs Summary

**The shape registry is now mechanically proven equivalent to v1.1 gestures, extensible by one test-only shape class, and resistant to bounding-box-derived point-list regressions.**

## Performance

- **Duration:** 25 min
- **Tasks:** 3/3
- **Files modified:** 4
- **Verification:** `dotnet test --nologo` — 967 passed, 0 failed

## Accomplishments

- Added 392 exact assertions over the entire 196-case v1.1 gesture grid, covering bounds, drawability, line endpoints, triangle vertices, and circle centre/radius.
- Registered a test-only `PentagonShape` through the existing interface and proved it works while PostgreSQL is stopped; a fresh default registry remains at four shapes.
- Added round-trip and same-bounds/different-JSON guards that preserve ordered line and triangle point-list geometry, including downward and sideways triangles.

## Task Commits

1. **Task 1: The v1.1 gesture-equivalence grid** — `3c845eb` (test)
2. **Task 2: A fifth shape type the application does not ship** — `a7d978c` (test)
3. **Task 3: Point-list primacy invariant and triangle data proof** — `97cdb72` (test)

## Files Created

- `tests/BlazorCanvas.Tests/Shapes/V11GestureEquivalenceTests.cs` — temporary exact v1.1 cutover-equivalence contract tests.
- `tests/BlazorCanvas.Tests/Shapes/PentagonShape.cs` — self-contained, test-only fifth `IShapeDefinition`.
- `tests/BlazorCanvas.Tests/Shapes/ShapeRegistryExtensibilityTests.cs` — interface-only extension, isolation, and no-database proof.
- `tests/BlazorCanvas.Tests/Shapes/PointListPrimacyTests.cs` — generic round-trip, distinct-geometry, and alternate-triangle guards.

## Decisions Made

- The legacy gesture comparison is exact rather than epsilon-based because the new gesture arithmetic deliberately transcribes v1.1.
- `PentagonShape` keeps its point-list geometry private and nested so the extension proof adds no shared type or production composition entry.
- Point-list primacy is asserted by values with identical bounds but intentionally different JSON, protecting the actual behavioural invariant rather than current implementation structure.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Test correctness] Replaced a same-bounds triangle case in the pure-bounds test with distinct-extent cases.**
- **Found during:** Task 3
- **Issue:** One test vector was a downward triangle that intentionally shares the original triangle's bounds, contradicting Task 3 Part D's requirement to prove a changed extent.
- **Fix:** Retained the downward-triangle proof in Part C and used three different-extent geometries in Part D.
- **Files modified:** `tests/BlazorCanvas.Tests/Shapes/PointListPrimacyTests.cs`
- **Verification:** Focused primacy suite passed 12 tests.
- **Committed in:** `97cdb72`

---

**Total deviations:** 1 auto-fixed (Rule 1)
**Impact on plan:** Test-only correction that strengthens the specified pure-bounds proof; no scope change.

## Issues Encountered

None beyond the corrected test vector.

## User Setup Required

None - PostgreSQL was temporarily stopped and restarted automatically for the isolation check.

## Next Phase Readiness

The Phase 9 registry contract is ready for input-gateway validation in 09-06 and for Phase 11 cutover; Phase 11 must remove the temporary v1.1 equivalence test with the legacy geometry classes.

## Self-Check: PASSED

- All four planned test files exist and no production file was changed by this plan.
- Task commits `3c845eb`, `a7d978c`, and `97cdb72` exist in git history.
