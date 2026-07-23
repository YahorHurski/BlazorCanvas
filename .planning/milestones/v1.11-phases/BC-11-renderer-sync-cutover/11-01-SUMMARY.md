---
phase: BC-11-renderer-sync-cutover
plan: 01
subsystem: v11-runtime-bootstrap
tags: [csharp, npgsql, postgresql, dependency-injection, migration, xunit]
requires:
  - phase: BC-10-storage-schema-migration-persistence-layer
    provides: additive v11 schema, deterministic ids, validated migration, and FigureRepository
provides:
  - Owner-derived, deterministic v11 canvas resolution for authenticated circuits
  - Additive startup coordinator that prepares and verifies v11 before component routes are mapped
  - Pooled v11 DI graph preserving FigureInputGateway's validated-write boundary
affects: [BC-11 plan 02 renderer-and-sync, BC-11 plan 03 cutover]
tech-stack:
  added: []
  patterns:
    - Deterministic owner-to-canvas resolution with parameterized idempotent insert
    - Bounded startup bootstrap after EF migration and before interactive route mapping
    - Source and integration tests for DI composition and legacy safety boundary
key-files:
  created:
    - src/BlazorCanvas/Data/V11/CanvasRepository.cs
    - src/BlazorCanvas/Data/V11/V11RuntimeBootstrap.cs
    - tests/BlazorCanvas.Tests/Database/V11/CanvasRepositoryTests.cs
    - tests/BlazorCanvas.Tests/Database/V11/V11RuntimeBootstrapTests.cs
  modified:
    - src/BlazorCanvas/Program.cs
key-decisions:
  - "CanvasRepository derives canvas UUIDs solely from the authenticated owner id and never accepts a canvas id from callers."
  - "Startup retains public.figures, replays Phase 10's deterministic migration when it exists, and verifies the v11 catalog before routes map."
  - "One singleton pooled NpgsqlDataSource and singleton default registry compose scoped gateway and repository services without adding a raw JSON write path."
requirements-completed: [SYNC-03]
coverage:
  - id: D1
    description: "An existing or new authenticated owner resolves exactly one deterministic, owner-scoped canvas with the documented defaults."
    requirement: SYNC-03
    verification:
      - kind: integration
        ref: tests/BlazorCanvas.Tests/Database/V11/CanvasRepositoryTests.cs
        status: pass
    human_judgment: false
  - id: D2
    description: "The v11 bootstrap migrates a guarded legacy database, preserves public.figures, is restart-idempotent, and propagates invalid catalog failures."
    requirement: SYNC-03
    verification:
      - kind: integration
        ref: tests/BlazorCanvas.Tests/Database/V11/V11RuntimeBootstrapTests.cs
        status: pass
    human_judgment: false
  - id: D3
    description: "Program composition bootstraps after EF migration and before component routes while retaining validated-input-only FigureRepository writes."
    requirement: SYNC-03
    verification:
      - kind: source_and_reflection
        ref: tests/BlazorCanvas.Tests/Database/V11/V11RuntimeBootstrapTests.cs#Program_BootstrapsThePooledV11GraphBeforeComponentRoutes
        status: pass
    human_judgment: false
duration: 11min
completed: 2026-07-22
status: complete
---

# Phase BC-11 Plan 01: Runtime Bootstrap and Canvas Ownership Summary

**The application now prepares the additive v1.11 store before any interactive circuit, and each authenticated owner can lazily and idempotently resolve only their deterministic 1472×828 canvas.**

## Accomplishments

- Added `CanvasRepository`, which derives a canvas identity from the owner id, inserts only the canonical default row, and reads it back with both id and owner predicates.
- Added `V11RuntimeBootstrap`, which requires the EF users schema, invokes the established Phase 10 migration while legacy `public.figures` exists, otherwise applies and seeds v11, then verifies its required catalog.
- Registered one pooled `NpgsqlDataSource`, the canonical default registry, validation gateway, v11 figure repository, and canvas repository. Bootstrap executes after EF migration and before Razor components map.
- Added live database and guarded scratch-database coverage for defaults, migration, idempotent reruns, figure-less owners, invalid catalogs, missing EF prerequisites, DI ordering, and validated-write API shape.

## Task Commits

1. **Task 1: Add owner-scoped v11 canvas resolution** — `4780b58` (feat)
2. **Task 2: Add explicit pre-cutover startup bootstrap and DI composition** — `bce0134` (feat)
3. **Task 3: Prove composition preserves validation and legacy safety boundaries** — `af34c2e` (test)

## Verification

- `dotnet test BlazorCanvas.sln --nologo --filter "FullyQualifiedName~CanvasRepositoryTests"` — 5 passed.
- `dotnet test BlazorCanvas.sln --nologo --filter "FullyQualifiedName~V11RuntimeBootstrapTests"` — 4 passed before final composition assertions; final full-suite run below includes all 6 bootstrap tests.
- `dotnet build BlazorCanvas.sln --nologo -v q` — passed with 0 warnings and 0 errors.
- `dotnet test BlazorCanvas.sln --nologo` — 1,304 passed, 0 failed, 0 skipped.

## Deviations

None. The invalid-catalog test uses an intentionally incomplete registry against a valid legacy rectangle row, which exercises bootstrap failure propagation without weakening the legacy database's own type constraints.

## Next Phase Readiness

Wave 2 can resolve the authenticated owner's canvas id through `CanvasRepository` and use the registered validated v11 persistence graph. `public.figures` remains intact until the explicit Wave 3 cutover.
