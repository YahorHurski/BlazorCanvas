---
phase: BC-14-catalog-seed-toolbar-decisions
plan: 01
subsystem: database
tags: [dotnet, blazor, postgres, npgsql, shape-registry, star5, xunit]
requires:
  - phase: BC-13-star-shape-core
    provides: Star5Shape and Star5Geometry core contract without registry exposure.
provides:
  - Star5Shape is registered in DefaultShapes.CreateRegistry() after triangle.
  - Registry-driven figure_types seeding targets v11 and public schemas through closed code-owned choices.
  - V11Cutover.EnsureAsync converges completed public catalogs to include star5 on startup.
  - Regression coverage proves completed-catalog convergence and repeated-startup idempotency.
affects: [BC-15-draw-preview-render-persist-star, BC-16-interaction-sync-test-guards]
tech-stack:
  added: []
  patterns:
    - Seed figure_types from DefaultShapes registry names using parameterized @name values.
    - Select SQL seed target schema only through code-owned v11/public enum values.
    - Keep completed-catalog startup mutation inside the existing advisory-lock transaction.
key-files:
  created: []
  modified:
    - src/BlazorCanvas/Shapes/DefaultShapes.cs
    - src/BlazorCanvas/Data/V11/Transition/V11Schema.cs
    - src/BlazorCanvas/Data/V11/V11Cutover.cs
    - tests/BlazorCanvas.Tests/Shapes/DefaultShapesTests.cs
    - tests/BlazorCanvas.Tests/Shapes/Star5ShapeTests.cs
    - tests/BlazorCanvas.Tests/Shapes/ShapeRegistryExtensibilityTests.cs
    - tests/BlazorCanvas.Tests/Database/V11/V11CutoverTests.cs
key-decisions:
  - "Star5Shape now participates in the default registry and figure_types seed order immediately after triangle."
  - "Completed public catalogs are no longer exact no-ops; startup may insert missing registry-owned figure_types rows idempotently."
patterns-established:
  - "V11Schema exposes a public-catalog seed helper while retaining the existing v11 seed helper for additive/fresh cutover paths."
  - "Completed CatalogState seeding commits rather than rolls back after taking the same advisory transaction lock."
requirements-completed: [MODEL-08]
coverage:
  - id: D1
    description: "DefaultShapes.CreateRegistry exposes star5 in canonical order after triangle."
    requirement: MODEL-08
    verification:
      - kind: unit
        ref: "tests/BlazorCanvas.Tests/Shapes/DefaultShapesTests.cs#CreateRegistry_RegistersCanonicalNamesInSeedOrder"
        status: pass
      - kind: unit
        ref: "tests/BlazorCanvas.Tests/Shapes/Star5ShapeTests.cs#DefaultRegistry_ContainsStar5AfterTriangle"
        status: pass
      - kind: other
        ref: "dotnet test tests/BlazorCanvas.Tests/BlazorCanvas.Tests.csproj --no-restore --filter \"FullyQualifiedName~DefaultShapesTests|FullyQualifiedName~Star5ShapeTests|FullyQualifiedName~V11CutoverTests\""
        status: pass
    human_judgment: false
  - id: D2
    description: "Completed public catalogs missing star5 gain it during startup with no migration or manual SQL."
    requirement: MODEL-08
    verification:
      - kind: integration
        ref: "tests/BlazorCanvas.Tests/Database/V11/V11CutoverTests.cs#CompletedPublicCatalog_MissingRegistryTypeConvergesIdempotently"
        status: pass
      - kind: other
        ref: "dotnet test BlazorCanvas.sln --no-restore"
        status: pass
    human_judgment: false
  - id: D3
    description: "Two consecutive V11Cutover.EnsureAsync calls leave exactly one star5 row."
    requirement: MODEL-08
    verification:
      - kind: integration
        ref: "tests/BlazorCanvas.Tests/Database/V11/V11CutoverTests.cs#CompletedPublicCatalog_MissingRegistryTypeConvergesIdempotently"
        status: pass
      - kind: other
        ref: "dotnet test BlazorCanvas.sln --no-restore"
        status: pass
    human_judgment: false
duration: 4min
completed: 2026-07-22
status: complete
---

# Phase BC-14 Plan 01: Catalog Seed Registration Summary

**Star5 registry exposure with idempotent startup seeding for completed public PostgreSQL catalogs**

## Performance

- **Duration:** 4 min
- **Started:** 2026-07-22T20:01:24Z
- **Completed:** 2026-07-22T20:05:27Z
- **Tasks:** 2
- **Files modified:** 7

## Accomplishments

- Registered `Star5Shape` after `TriangleShape` in the default registry, making `star5` part of canonical seed order.
- Generalized `V11Schema` seeding so only closed code-owned targets, `v11.figure_types` and `public.figure_types`, can be selected while all names remain bound through `@name`.
- Changed the completed-catalog startup path to seed `public.figure_types` under the existing advisory transaction lock, commit, and return.
- Added regression coverage for registry exposure, five-row cutover catalogs, completed public catalog convergence, and two-run idempotency.

## Task Commits

1. **Task 1: Specify star registry and completed-catalog seed convergence (RED)** - `a71fe7c` (test)
2. **Task 2: Register Star5Shape and seed closed schema targets (GREEN)** - `e455728` (feat)

## Files Created/Modified

- `src/BlazorCanvas/Shapes/DefaultShapes.cs` - Registers `Star5Shape` in the default registry after triangle.
- `src/BlazorCanvas/Data/V11/Transition/V11Schema.cs` - Adds closed v11/public seed target selection and a public seed helper.
- `src/BlazorCanvas/Data/V11/V11Cutover.cs` - Seeds completed public catalogs before returning.
- `tests/BlazorCanvas.Tests/Shapes/DefaultShapesTests.cs` - Expects `star5` in default registry order and round-trip coverage.
- `tests/BlazorCanvas.Tests/Shapes/Star5ShapeTests.cs` - Replaces the Phase 13 no-registration fence with a Phase 14 registration assertion.
- `tests/BlazorCanvas.Tests/Shapes/ShapeRegistryExtensibilityTests.cs` - Updates stale registry size/order expectations for the fifth shipped shape.
- `tests/BlazorCanvas.Tests/Database/V11/V11CutoverTests.cs` - Expects five figure types and proves completed-catalog convergence/idempotency.

## Decisions Made

- Completed catalogs are permitted to mutate only for registry-owned `figure_types` convergence; the old exact no-op invariant is narrowed to idempotent catalog convergence.
- Schema-qualified seeding uses an internal closed enum rather than accepting schema text from callers, configuration, tests, or registry data.

## Verification

- `dotnet test tests/BlazorCanvas.Tests/BlazorCanvas.Tests.csproj --no-restore --filter "FullyQualifiedName~DefaultShapesTests|FullyQualifiedName~Star5ShapeTests|FullyQualifiedName~V11CutoverTests"` - RED failed before production changes for missing registration, four-row catalogs, and missing completed-catalog `star5`; GREEN passed 40/40.
- `dotnet test tests/BlazorCanvas.Tests/BlazorCanvas.Tests.csproj --no-restore --filter "FullyQualifiedName~DefaultShapesTests|FullyQualifiedName~Star5ShapeTests|FullyQualifiedName~V11CutoverTests|FullyQualifiedName~ShapeRegistryExtensibilityTests"` - passed 46/46 after updating stale extensibility expectations.
- `dotnet test BlazorCanvas.sln --no-restore` - passed 524/524.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Allowed the locked `star5` identifier in registry-name tests**
- **Found during:** Task 2 (Register Star5Shape and seed closed schema targets)
- **Issue:** `DefaultShapesTests.CreateRegistry_DefinitionsHaveUniqueLowercaseAsciiNames` allowed only `a-z`, but the locked registry/database name is `star5`.
- **Fix:** Renamed the test and allowed lowercase ASCII letters or digits.
- **Files modified:** `tests/BlazorCanvas.Tests/Shapes/DefaultShapesTests.cs`
- **Verification:** Focused and full solution test commands passed.
- **Committed in:** `e455728`

**2. [Rule 1 - Bug] Updated stale test-only registry extensibility counts**
- **Found during:** Task 2 full solution verification
- **Issue:** `ShapeRegistryExtensibilityTests` still treated the shipped default registry as four shapes, so registering test-only pentagon expected five instead of six.
- **Fix:** Updated default and extended registry order assertions to include `star5`.
- **Files modified:** `tests/BlazorCanvas.Tests/Shapes/ShapeRegistryExtensibilityTests.cs`
- **Verification:** Focused and full solution test commands passed.
- **Committed in:** `e455728`

---

**Total deviations:** 2 auto-fixed (2 Rule 1 bugs)
**Impact on plan:** Both fixes were stale test expectations directly caused by the planned `star5` registration. No scope beyond MODEL-08 was added.

## Issues Encountered

- The unrelated staged PDF was accidentally included in two task commit attempts because it was already staged. Both commits were amended immediately to remove the PDF, and the PDF was restored to staged status afterward.

## Known Stubs

None.

## Threat Flags

None. The only trust-boundary change is the planned registry-to-database seed path; names remain parameterized and schema targets are code-owned.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

Plan 14-02 can add the toolbar star tool knowing `star5` is registered and writable on both fresh/additive cutovers and existing completed public catalogs.

## Self-Check: PASSED

- Found `.planning/phases/BC-14-catalog-seed-toolbar-decisions/14-01-SUMMARY.md`.
- Found all seven Plan 14-01 source/test files.
- Found task commits `a71fe7c` and `e455728`.

---
*Phase: BC-14-catalog-seed-toolbar-decisions*
*Completed: 2026-07-22*
