---
phase: BC-16-interaction-sync-test-guards
plan: 04
subsystem: testing
tags: [bunit, blazor, components, preview, star5, regression]
requires:
  - phase: BC-15-draw-preview-render-persist-a-star
    provides: Registry-backed DrawingPreviewSession and FigureShape preview rendering for star5.
provides:
  - bUnit render-level smoke guard for the active star drawing preview.
  - Negative-control coverage for the G-15-1 unbound literal PreviewType regression class.
  - Pinned test-only bunit dependency for component rendering tests.
affects: [BC-16, TEST-04, preview-rendering, FigureShape]
tech-stack:
  added: [bunit 2.7.2]
  patterns:
    - Render FigureShape directly for component-level preview smoke tests when Home requires auth and database scaffolding.
    - Use registry-known and registry-unknown PreviewType inputs to prove the FigureShape preview gate discriminates failures.
key-files:
  created:
    - tests/BlazorCanvas.Tests/Components/PreviewRenderSmokeTests.cs
  modified:
    - tests/BlazorCanvas.Tests/BlazorCanvas.Tests.csproj
key-decisions:
  - "Used bUnit 2.7.2's current BunitContext.Render API because RenderComponent is error-level obsolete in bUnit 2.x."
  - "Kept the smoke test at FigureShape instead of mounting Home because Home requires auth cascade and PostgreSQL-backed repositories, while FigureShape owns the Registry.Contains(PreviewType) gate that caused G-15-1."
patterns-established:
  - "bUnit component smoke tests should use BunitContext with JSInterop loose mode when JavaScript is irrelevant to server-side SVG output."
requirements-completed: [TEST-04]
coverage:
  - id: D1
    description: "A bUnit render-level smoke test renders an active star preview through FigureShape and asserts a polygon with non-empty points before commit."
    requirement: TEST-04
    verification:
      - kind: unit
        ref: "dotnet test BlazorCanvas.sln --filter 'FullyQualifiedName~PreviewRenderSmokeTests' (2/2 passed)"
        status: pass
      - kind: unit
        ref: "dotnet test BlazorCanvas.sln --nologo (559/559 passed)"
        status: pass
    human_judgment: false
  - id: D2
    description: "A negative-control test renders the same preview placement with a registry-unknown literal PreviewType and asserts no polygon is emitted."
    requirement: TEST-04
    verification:
      - kind: unit
        ref: "tests/BlazorCanvas.Tests/Components/PreviewRenderSmokeTests.cs#RegistryUnknownPreviewType_EmitsNoPolygonForG15LiteralBindingRegression"
        status: pass
    human_judgment: false
duration: 4 min
completed: 2026-07-22
status: complete
---

# Phase BC-16 Plan 04: Preview Render Smoke Guard Summary

**bUnit render guard for live star previews through FigureShape, with a G-15-1 literal-binding negative control.**

## Performance

- **Duration:** 4 min
- **Started:** 2026-07-22T23:33:00Z
- **Completed:** 2026-07-22T23:37:14Z
- **Tasks:** 2 completed
- **Files modified:** 2

## Accomplishments

- Added a pinned, test-only `bunit` 2.7.2 package reference to the test project.
- Added `PreviewRenderSmokeTests` to render an active `DrawingPreviewSession` star preview through `FigureShape`.
- Proved the guard bites for the G-15-1 class by asserting a registry-unknown literal `PreviewType` emits no polygon.

## Task Commits

Each task was committed atomically:

1. **Task 1: Add a pinned, test-only bUnit package reference compatible with net10.0** - `5b58094` (chore)
2. **Task 2: Render the live star preview through FigureShape and assert a polygon is emitted before commit** - `c352c05` (test)

**Plan metadata:** pending final docs commit

## Files Created/Modified

- `tests/BlazorCanvas.Tests/BlazorCanvas.Tests.csproj` - Adds exactly one direct bUnit package reference, pinned to 2.7.2.
- `tests/BlazorCanvas.Tests/Components/PreviewRenderSmokeTests.cs` - Adds the positive preview polygon render smoke test and the G-15-1 literal-binding negative control.

## Decisions Made

- Used bUnit 2.7.2's `BunitContext.Render<TComponent>` API after the initial `RenderComponent<TComponent>` attempt failed compilation because bUnit 2.x marks it error-level obsolete.
- Rendered `FigureShape` directly rather than `Home` because the failure surface is `FigureShape`'s `Registry.Contains(PreviewType)` preview gate; mounting `Home` would require unrelated auth and PostgreSQL repository scaffolding.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] Updated bUnit render API for 2.7.2**
- **Found during:** Task 2 (Render the live star preview through FigureShape and assert a polygon is emitted before commit)
- **Issue:** `RenderComponent<TComponent>` is error-level obsolete in bUnit 2.7.2, so the focused test command failed at compile time.
- **Fix:** Switched the test to `BunitContext.Render<TComponent>` while keeping JS interop loose mode.
- **Files modified:** `tests/BlazorCanvas.Tests/Components/PreviewRenderSmokeTests.cs`
- **Verification:** `dotnet test BlazorCanvas.sln --nologo --filter 'FullyQualifiedName~PreviewRenderSmokeTests'` passed 2/2.
- **Committed in:** `c352c05`

---

**Total deviations:** 1 auto-fixed (1 blocking)
**Impact on plan:** The change follows the selected bUnit version's current API and does not expand the plan scope.

## Issues Encountered

- `dotnet add package bunit` and subsequent restore/build/test commands report NuGet warning `NU1902` for transitive `AngleSharp` 1.4.0 advisory `GHSA-pgww-w46g-26qg` with medium severity. The dependency is test-only, introduced by the plan-approved bUnit package, and no production project references bUnit.
- The task was marked `tdd="true"`, but this plan guards existing Phase 15 behavior and required no production implementation. No RED/GREEN production cycle was possible without fabricating a failure; the committed outcome is a passing regression guard.

## User Setup Required

None - no external service configuration required.

## Verification

- `powershell -NoProfile -Command "Get-Process BlazorCanvas -ErrorAction SilentlyContinue | Stop-Process -Force; dotnet restore tests/BlazorCanvas.Tests/BlazorCanvas.Tests.csproj; dotnet build tests/BlazorCanvas.Tests/BlazorCanvas.Tests.csproj --no-restore --nologo"` - PASS, build succeeded with NU1902 warning for transitive AngleSharp.
- `powershell -NoProfile -Command "Get-Process BlazorCanvas -ErrorAction SilentlyContinue | Stop-Process -Force; dotnet test BlazorCanvas.sln --nologo --filter 'FullyQualifiedName~PreviewRenderSmokeTests'"` - PASS, 2/2 tests passed.
- `powershell -NoProfile -Command "Get-Process BlazorCanvas -ErrorAction SilentlyContinue | Stop-Process -Force; dotnet test BlazorCanvas.sln --nologo"` - PASS, 559/559 tests passed.

## Known Stubs

None.

## Threat Flags

None - the only new trust surface is the planned test-only bUnit dependency already covered by the plan threat model.

## Next Phase Readiness

Plan 16-04 is complete. Phase 16 can use this guard alongside the other TEST-04 and interaction/sync guard plans.

## Self-Check: PASSED

- Found `tests/BlazorCanvas.Tests/BlazorCanvas.Tests.csproj`.
- Found `tests/BlazorCanvas.Tests/Components/PreviewRenderSmokeTests.cs`.
- Found task commit `5b58094`.
- Found task commit `c352c05`.
- Summary status is `complete`.

---
*Phase: BC-16-interaction-sync-test-guards*
*Completed: 2026-07-22*
