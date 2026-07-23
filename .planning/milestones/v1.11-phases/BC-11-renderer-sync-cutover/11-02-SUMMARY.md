---
phase: BC-11-renderer-sync-cutover
plan: 02
subsystem: renderer-sync-circuit
tags: [blazor, svg, postgresql, sync, uuid, xunit]
requires:
  - phase: BC-11-renderer-sync-cutover
    plan: 01
    provides: owner-scoped v11 canvas and registered persistence graph
provides:
  - Local-frame SVG rendering for persisted v11 geometry and selection traces
  - UUID-only position sync and local-bbox movement clamps
  - Testable Home circuit coordinator preserving draw, drag, rollback, and remote protocol rules
affects: [BC-11 plan 03 cutover]
key-files:
  created:
    - src/BlazorCanvas/Geometry/V11Movement.cs
    - src/BlazorCanvas/Components/Pages/CanvasInteractionCoordinator.cs
    - tests/BlazorCanvas.Tests/Geometry/V11MovementTests.cs
    - tests/BlazorCanvas.Tests/Components/V11RenderContractTests.cs
    - tests/BlazorCanvas.Tests/Components/CanvasInteractionCoordinatorTests.cs
  modified:
    - src/BlazorCanvas/Components/Canvas/FigureShape.razor
    - src/BlazorCanvas/Components/Canvas/SelectionTrace.razor
    - src/BlazorCanvas/Components/Pages/Home.razor
    - src/BlazorCanvas/Sync/SyncMessage.cs
requirements-completed: [RENDER-01, SYNC-02, SYNC-03]
completed: 2026-07-22
status: complete
---

# Phase BC-11 Plan 02: Renderer, UUID Sync and Circuit Summary

The live canvas now renders v1.11 rows in local SVG frames, moves them by UUID x/y state, and drives the Home pointer lifecycle through a deterministic circuit coordinator.

## Accomplishments

- Replaced legacy box rendering with registry-parsed local geometry under invariant-culture `translate(x, y) rotate(rotation)` groups. Committed style, preview opacity, z order, selection appearance, pointer behavior, and the 48px page mapping are retained.
- Added `V11Movement.ClampPosition`, which clamps only decimal position against cached local bbox extents; geometry and cache values never change during drag.
- Replaced sync payloads with canonical-row draw plus UUID-and-position-only move/rollback/delete messages.
- Switched Home to owner-resolved v11 rows, validated gateway draw input, and v11 repositories. `CanvasInteractionCoordinator` supplies 50ms move throttling with a final trailing update, zero-row stale deletion, rollback on persistence failure, reload snapshots, echo filtering, update-only remote moves, and blanket mid-drag remote discard.

## Task Commits

1. **Task 1: Render v11 geometry locally and retain selection appearance** — `78ec50e` (feat)
2. **Task 2: Replace box movement and sync payloads with UUID position state** — `cf9ba4f` (feat)
3. **Task 3: Wire Home draw, drag, delete, rollback, and remote apply to v11** — `1dced08` (feat)

## Verification

- `dotnet test BlazorCanvas.sln --nologo --filter "FullyQualifiedName~V11MovementTests|FullyQualifiedName~CanvasSyncNotifierTests|FullyQualifiedName~V11RenderContractTests|FullyQualifiedName~CanvasInteractionCoordinatorTests"` — 14 passed.
- `dotnet build BlazorCanvas.sln --nologo -v q` — passed with 0 warnings and 0 errors.
- `dotnet test BlazorCanvas.sln --nologo` — 1,308 passed, 0 failed, 0 skipped.

## Deviations

None. The render contract is source-level because this test project intentionally has no bUnit dependency; compilation plus the contract checks cover the Razor output shape without broadening the test stack.

## Self-check

- All three plan tasks are implemented and committed independently.
- Only plan-scoped source, tests, and this summary are staged; existing `.planning/config.json` and the untracked PDF remain untouched.
- Wave 3 may remove legacy paths now that Home consumes only the v11 runtime graph.
