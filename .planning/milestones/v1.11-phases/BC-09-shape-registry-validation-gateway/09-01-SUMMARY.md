---
phase: BC-09-shape-registry-validation-gateway
plan: 01
subsystem: shape-model
tags: [csharp, system-text-json, xunit, geometry, validation]
requires: []
provides:
  - Typed local geometry records and shape-definition contract
  - Case-sensitive, registration-ordered shape registry
  - Finite JSON geometry parsing and invariant serialisation helpers
affects: [BC-09 plans 02-06, shape registry, validation gateway, persistence]
tech-stack:
  added: []
  patterns:
    - Typed IFigureGeometry records separate local shape data from canvas placement
    - Utf8JsonWriter-only geometry output is invariant and byte-stable
key-files:
  created:
    - src/BlazorCanvas/Shapes/IShapeDefinition.cs
    - src/BlazorCanvas/Shapes/ShapeRegistry.cs
    - src/BlazorCanvas/Shapes/GeometryJson.cs
    - tests/BlazorCanvas.Tests/Shapes/GeometryModelTests.cs
    - tests/BlazorCanvas.Tests/Shapes/ShapeRegistryTests.cs
  modified: []
key-decisions:
  - "Line and triangle retain ordered point lists; bounds remain derived."
  - "Registry lookup is ordinal and case-sensitive with no default shape."
  - "Geometry JSON is parsed from JsonElement and written only with Utf8JsonWriter."
patterns-established:
  - "Geometry implementations validate through GeometryJson and re-serialise typed records."
requirements-completed: [SHAPE-01, SHAPE-02]
coverage:
  - id: D1
    description: "Typed geometry model and the IShapeDefinition registry contract."
    requirement: SHAPE-01
    verification:
      - kind: unit
        ref: "tests/BlazorCanvas.Tests/Shapes/ShapeRegistryTests.cs"
        status: pass
    human_judgment: false
  - id: D2
    description: "Ordered point-list geometry plus finite, culture-invariant geometry JSON primitives."
    requirement: SHAPE-02
    verification:
      - kind: unit
        ref: "tests/BlazorCanvas.Tests/Shapes/GeometryModelTests.cs"
        status: pass
    human_judgment: false
---

# Phase BC-09 Plan 01: Shape Model Foundation Summary

**Typed local geometry, an ordinal shape registry, and finite culture-invariant JSON primitives establish the pure-C# foundation for the v1.11 shape model.**

## Performance

- **Duration:** 6 min
- **Started:** 2026-07-21T21:23:14Z
- **Completed:** 2026-07-21T21:29:29Z
- **Tasks:** 3/3
- **Files modified:** 14
- **Test baseline:** 405 passing
- **Final full suite:** 441 passing (36 tests added)

## Accomplishments

- Added distinct local and canvas point types, typed shape geometry records, derived bounds, and a placement record without touching the v1.1 geometry model.
- Defined the six-member `IShapeDefinition` contract and a non-static, case-sensitive registry that preserves registration order and rejects duplicates or fallback lookups.
- Added JSON primitives that reject malformed/non-finite geometry and emit minified invariant JSON, with focused tests for culture, structure, ordering, and registry behavior.

## Task Commits

1. **Task 1: Typed geometry value model** - `59910f4` (feat)
2. **Task 2: IShapeDefinition contract and ShapeRegistry** - `47cb6ed` (feat)
3. **Task 3: GeometryJson helpers and tests** - `8b1eded` (feat)

## Files Created/Modified

- `src/BlazorCanvas/Shapes/` - Typed geometry vocabulary, registry, contract, and JSON helpers.
- `tests/BlazorCanvas.Tests/Shapes/GeometryModelTests.cs` - Geometry JSON parsing, serialisation, and culture guards.
- `tests/BlazorCanvas.Tests/Shapes/ShapeRegistryTests.cs` - Registry safety and ordering coverage.

## Decisions Made

- Kept line and triangle point-list equality reference-based as mandated; tests compare point sequences.
- Stored registry lookup and registration order separately, so dictionary ordering cannot become a Phase 10 seed-order dependency.
- Used `Utf8JsonWriter` exclusively for number output to guard against comma-decimal server cultures.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Compile correctness] Initialise the finite-number helper's out parameter**
- **Found during:** Task 3 (GeometryJson helpers and tests)
- **Issue:** C# requires an `out` parameter to be assigned even when the JSON value is not a number.
- **Fix:** Initialised `number` before the short-circuiting validation expression.
- **Files modified:** `src/BlazorCanvas/Shapes/GeometryJson.cs`
- **Verification:** Focused Shape tests passed 36/36; full suite passed 441/441.
- **Committed in:** `8b1eded`

---

**Total deviations:** 1 auto-fixed (Rule 1 compile correctness).
**Impact on plan:** Required for compilation only; no scope expansion.

## Issues Encountered

- The pre-plan test baseline initially hit the expected MSB3021 apphost lock from a local `BlazorCanvas` development server. The plan explicitly directs stopping that server; after doing so, the baseline ran successfully with 405 passing tests.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

- Plans 09-02 through 09-06 can build on the typed records, registry, and JSON primitives.
- No blockers. Existing v1.1 code and tests remain untouched.

## Verification

- `dotnet build src/BlazorCanvas/BlazorCanvas.csproj --nologo -v q` — passed with 0 warnings and 0 errors.
- `dotnet test --nologo --filter "FullyQualifiedName~BlazorCanvas.Tests.Shapes"` — passed 36/36.
- `dotnet test --nologo` — passed 441/441 (405 baseline + 36 added).
- Source and diff scope checks — passed; no legacy geometry, components, data, migrations, or existing test directories changed.

## Self-Check: PASSED

---
*Phase: BC-09-shape-registry-validation-gateway*
*Completed: 2026-07-21*
