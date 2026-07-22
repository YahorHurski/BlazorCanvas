---
phase: BC-09-shape-registry-validation-gateway
plan: 04
subsystem: shape-validation
tags: [csharp, shapes, geometry, json, xunit]
requires:
  - phase: BC-09-01
    provides: Typed geometry records, JSON helpers, and ShapeRegistry contracts
provides:
  - Four self-contained IShapeDefinition implementations for line, rectangle, circle, and triangle
  - Canonical DefaultShapes registry in v1.1 persistence-name order
  - Boundary, order-preservation, and gesture-equivalence unit coverage
affects: [BC-09-05, BC-09-06, BC-10, BC-11]
tech-stack:
  added: []
  patterns:
    - Shape-local parse, serialise, drawability, bounds, and gesture rules
    - Round-before-clamp handling for untrusted browser circuit coordinates
key-files:
  created:
    - src/BlazorCanvas/Shapes/LineShape.cs
    - src/BlazorCanvas/Shapes/TriangleShape.cs
    - src/BlazorCanvas/Shapes/RectangleShape.cs
    - src/BlazorCanvas/Shapes/CircleShape.cs
    - src/BlazorCanvas/Shapes/DefaultShapes.cs
    - tests/BlazorCanvas.Tests/Shapes/DefaultShapesTests.cs
  modified: []
key-decisions:
  - "Line and triangle preserve ordered local vertices instead of canonicalising them."
  - "Circle geometry uses the top-left of its bounding square as local origin; its centre is (R, R)."
  - "DefaultShapes returns a fresh registry to isolate test-only registrations."
patterns-established:
  - "Each shape definition exclusively owns its type-specific JSON, validation, bounds, and gesture arithmetic."
  - "Pointer inputs round away from zero before inclusive canvas clamping."
requirements-completed: [SHAPE-01, SHAPE-02, SHAPE-03]
coverage:
  - id: D1
    description: "Four typed shape definitions validate geometry, preserve point order, calculate bounds, and reproduce legacy gestures."
    requirement: SHAPE-01
    verification:
      - kind: unit
        ref: "dotnet test --nologo"
        status: pass
    human_judgment: false
  - id: D2
    description: "Line and triangle ordered-point semantics reject degenerate shapes without changing stored appearance."
    requirement: SHAPE-02
    verification:
      - kind: unit
        ref: "tests/BlazorCanvas.Tests/Shapes/LineShapeTests.cs; tests/BlazorCanvas.Tests/Shapes/TriangleShapeTests.cs"
        status: pass
    human_judgment: false
  - id: D3
    description: "Positive extent validation and exact bounds preserve the circle's top-left local origin."
    requirement: SHAPE-03
    verification:
      - kind: unit
        ref: "tests/BlazorCanvas.Tests/Shapes/RectangleShapeTests.cs; tests/BlazorCanvas.Tests/Shapes/CircleShapeTests.cs"
        status: pass
    human_judgment: false
duration: 25min
completed: 2026-07-22
status: complete
---

# Phase BC-09 Plan 04: Shape Definition Implementations Summary

**Four isolated shape definitions now preserve local geometry precisely, reject hostile or degenerate input, and reproduce the v1.1 drawing behaviour.**

## Performance

- **Duration:** 25 min
- **Tasks:** 3/3
- **Files modified:** 10
- **Verification:** `dotnet build --nologo` and `dotnet test --nologo` — 557 passed, 0 failed

## Accomplishments

- Added point-list line and triangle implementations that preserve vertex order, reject duplicate/collinear geometry, and retain fractional bounds.
- Added rectangle and circle implementations that reject non-positive dimensions, clamp untrusted pointer input, and preserve the circle bounding-square origin and edge cap.
- Added the canonical fresh `DefaultShapes` composition root, proven against the v1.1 literal mapping and registry isolation.

## Task Commits

1. **Task 1: LineShape and TriangleShape — the point-list types** — `3ec1b59` (feat)
2. **Task 2: RectangleShape and CircleShape** — `cf2fcdc` (feat)
3. **Task 3: DefaultShapes composition root and the name-parity guard** — `a9b13ac` (feat)

## Files Created

- `src/BlazorCanvas/Shapes/LineShape.cs` and `TriangleShape.cs` — ordered point-list shape rules.
- `src/BlazorCanvas/Shapes/RectangleShape.cs` and `CircleShape.cs` — positive-extent shape rules.
- `src/BlazorCanvas/Shapes/DefaultShapes.cs` — canonical registry composition.
- `tests/BlazorCanvas.Tests/Shapes/*ShapeTests.cs` — focused contract and regression coverage.

## Decisions Made

- Stored line endpoints remain in draw order because SVG renders either endpoint order identically while preserving the original diagonal data.
- Circle placement is offset from the press-centre by its radius because its local origin is the top-left of the inscribed bounding square.
- Registry creation is intentionally non-singleton so test-only registrations cannot affect other callers or future database seeding.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Test correctness] Compared point sequences rather than records containing reference-equal point lists.**
- **Found during:** Task 1
- **Issue:** `TriangleGeometry` record equality intentionally compares its point-list reference, so direct `ShapePlacement` equality made valid gesture tests fail.
- **Fix:** Asserted placement coordinates and ordered points explicitly.
- **Files modified:** `tests/BlazorCanvas.Tests/Shapes/TriangleShapeTests.cs`
- **Verification:** Focused line/triangle suite passed 36 tests.
- **Committed in:** `3ec1b59`

---

**Total deviations:** 1 auto-fixed (Rule 1)
**Impact on plan:** Test-only correctness fix; implementation scope and behaviour remain unchanged.

## Issues Encountered

None beyond the record/list equality assertion corrected during Task 1.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

The registry is ready for 09-05 equivalence tests, 09-06 gateway validation, Phase 10 type seeding, and Phase 11 rendering dispatch.

## Self-Check: PASSED

- All ten planned source and test files exist.
- Task commits `3ec1b59`, `cf2fcdc`, and `a9b13ac` exist in git history.
