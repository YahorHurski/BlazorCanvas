---
phase: BC-16-interaction-sync-test-guards
plan: 03
subsystem: testing
tags: [dotnet, xunit, blazor-server, star5, bbox, validation]
requires:
  - phase: BC-15-draw-preview-render-persist-a-star
    provides: Registry-backed star drawing, preview, rendering, persistence, and Home.razor.js lifecycle-only helper.
provides:
  - TEST-04 drift guard proving star preview geometry and the inner-ratio literal stay out of Home.razor.js.
  - TEST-04 bbox agreement guard covering persisted star5 rows with exact BoundsOf recomputation and corruption detection.
  - TEST-04 gateway and unit guards rejecting malformed and degenerate star5 geometry while accepting a positive sliver.
affects: [BC-16, TEST-04, HomePreviewSourceTests, BboxCacheAgreementTests, FigureInputGatewayTests, Star5ShapeTests]
tech-stack:
  added: []
  patterns:
    - Source-contract tests pin Home.razor.js as lifecycle-only while Star5Shape owns star preview geometry constants.
    - Whole-table bbox agreement remains registry-driven and type-neutral, with star5 covered by seeded rows rather than a forked scanner.
    - Gateway and unit tests pin the same zero-extent boundary for star5 geometry.
key-files:
  created:
    - .planning/phases/BC-16-interaction-sync-test-guards/16-03-SUMMARY.md
  modified:
    - tests/BlazorCanvas.Tests/Components/HomePreviewSourceTests.cs
    - tests/BlazorCanvas.Tests/Database/V11/BboxCacheAgreementTests.cs
    - tests/BlazorCanvas.Tests/Shapes/FigureInputGatewayTests.cs
    - tests/BlazorCanvas.Tests/Shapes/Star5ShapeTests.cs
key-decisions:
  - "TEST-04 guards are test-only: production star geometry, gateway, repository, schema, and Home.razor.js behavior did not change."
  - "The star bbox agreement proof reuses the existing registry-driven whole-table scan instead of introducing a star-specific scanner."
patterns-established:
  - "Silent star failure modes are pinned by focused source, persistence, gateway, and unit boundary tests."
requirements-completed: [TEST-04]
coverage:
  - id: D1
    description: Home.razor.js contains lifecycle behavior only and no star5 branch, SVG geometry creation, trigonometric preview math, points writing, innerRatio text, or Star5Shape.DefaultInnerRatio literal.
    requirement: TEST-04
    verification:
      - kind: unit
        ref: "dotnet test tests/BlazorCanvas.Tests/BlazorCanvas.Tests.csproj --no-restore --filter \"FullyQualifiedName~HomePreviewSourceTests\""
        status: pass
    human_judgment: false
  - id: D2
    description: A seeded persisted star5 row participates in the whole-table bbox agreement scan, exactly matches fresh Star5Shape.BoundsOf over stored point doubles, and is caught by deliberate bbox_h corruption.
    requirement: TEST-04
    verification:
      - kind: integration
        ref: "dotnet test tests/BlazorCanvas.Tests/BlazorCanvas.Tests.csproj --no-restore --filter \"FullyQualifiedName~BboxCacheAgreementTests\""
        status: pass
    human_judgment: false
  - id: D3
    description: FigureInputGateway and Star5Shape reject malformed, non-finite, non-positive-ratio, wrong-point-count, and zero-extent star5 geometry while accepting valid stars and a one-unit positive sliver.
    requirement: TEST-04
    verification:
      - kind: unit
        ref: "dotnet test tests/BlazorCanvas.Tests/BlazorCanvas.Tests.csproj --no-restore --filter \"FullyQualifiedName~FigureInputGatewayTests|FullyQualifiedName~Star5ShapeTests\""
        status: pass
      - kind: unit
        ref: "dotnet test BlazorCanvas.sln --no-restore"
        status: pass
    human_judgment: false
duration: 8min
completed: 2026-07-22
status: complete
---

# Phase 16 Plan 03: Interaction, Sync & Test Guards Summary

**TEST-04 star guards now pin preview ownership, bbox cache agreement, and malformed/degenerate geometry rejection**

## Performance

- **Duration:** 8min
- **Started:** 2026-07-22T23:32:54Z
- **Completed:** 2026-07-22T23:41:18Z
- **Tasks:** 3
- **Files modified:** 4

## Accomplishments

- Extended `HomePreviewSourceTests` so `Home.razor.js` fails if star preview geometry, point-list writing, trigonometric math, `innerRatio`, or the star inner-ratio literal drifts back into JavaScript.
- Folded a seeded `star5` row into `BboxCacheAgreementTests`, asserting exact stored `bbox_*` equality against a fresh `Star5Shape.BoundsOf` recompute and proving a deliberate star-row cache corruption is detected.
- Extended `FigureInputGatewayTests` with malformed star payloads, valid star payload coverage, star gesture parity, and the zero-extent versus one-unit sliver boundary.
- Added an explicit `Star5ShapeTests` unit-boundary assertion for the same zero-extent threshold.

## Task Commits

1. **Task 1: Extend the JS-vs-C# drift guard so star preview geometry cannot return to Home.razor.js** - `921bd1a` (test)
2. **Task 2: Fold a star5 row into the whole-table bbox agreement scan** - `398a163` (test)
3. **Task 3: Reject degenerate and malformed star geometry at the gateway and unit boundary** - `6838a84` (test)

## Files Created/Modified

- `tests/BlazorCanvas.Tests/Components/HomePreviewSourceTests.cs` - Adds TEST-04/D-70/D-71 source-contract checks for JS preview non-ownership and the C# inner-ratio source.
- `tests/BlazorCanvas.Tests/Database/V11/BboxCacheAgreementTests.cs` - Seeds a persisted star row into the existing whole-table scan and adds exact bbox recompute plus corruption coverage.
- `tests/BlazorCanvas.Tests/Shapes/FigureInputGatewayTests.cs` - Adds star5 malformed, valid, gesture parity, and zero-extent/sliver gateway cases.
- `tests/BlazorCanvas.Tests/Shapes/Star5ShapeTests.cs` - Adds the named TEST-04 unit boundary for zero width, zero height, and one-unit sliver drawability.

## Decisions Made

- TEST-04 guard coverage was implemented entirely in tests; no production shape, gateway, repository, JavaScript, schema, or dependency change was needed.
- The bbox agreement guard remains one registry-driven whole-table scan. Star coverage is proven by seeding a star row and corrupting that row, not by forking a star-specific scanner.

## Deviations from Plan

None - plan executed exactly as written.

**Total deviations:** 0 auto-fixed.
**Impact on plan:** No scope expansion; the plan stayed test-only.

## TDD Gate Compliance

- The tasks were guard-test extensions over already-shipped Phase 15 production behavior, so no production GREEN commit was required.
- Each task was committed as a test-only atomic commit after focused verification passed.

## Auth Gates

None.

## Issues Encountered

- A parallel Phase 16 plan temporarily introduced an untracked compile-breaking `PreviewRenderSmokeTests.cs`; this blocked one Task 2 focused verification attempt before tests executed. I did not modify that file. After the parallel plan committed its bUnit changes, direct verification passed.
- One parallel focused rerun collided on `BlazorCanvas.dll` build output while another `dotnet test` process was compiling. The command was rerun sequentially and passed.

## User Setup Required

None - no external service configuration required.

## Verification

- `dotnet test tests/BlazorCanvas.Tests/BlazorCanvas.Tests.csproj --no-restore --filter "FullyQualifiedName~HomePreviewSourceTests"` - pass, 3/3.
- `dotnet test tests/BlazorCanvas.Tests/BlazorCanvas.Tests.csproj --no-restore --filter "FullyQualifiedName~BboxCacheAgreementTests"` - pass, 7/7.
- `dotnet test tests/BlazorCanvas.Tests/BlazorCanvas.Tests.csproj --no-restore --filter "FullyQualifiedName~FigureInputGatewayTests|FullyQualifiedName~Star5ShapeTests"` - pass, 103/103.
- `dotnet test BlazorCanvas.sln --no-restore` - pass, 583/583.

## Known Stubs

None.

## Threat Flags

None. No endpoint, auth path, file access pattern, package dependency, schema boundary, or sync protocol surface was introduced.

## Next Phase Readiness

TEST-04 is covered for plan 16-03: preview ownership, star bbox agreement, and malformed/degenerate rejection guards are in place. Phase 16 can continue with the remaining interaction, sync, and preview-render guard summaries already being produced by the parallel wave.

## Self-Check: PASSED

- Summary file created at `.planning/phases/BC-16-interaction-sync-test-guards/16-03-SUMMARY.md`.
- Task commits recorded and present in git history: `921bd1a`, `398a163`, `6838a84`.
- Required verification commands passed after final test changes.

---
*Phase: BC-16-interaction-sync-test-guards*
*Completed: 2026-07-22*
