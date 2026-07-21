---
phase: BC-06-canvas-resize-to-1472-828
plan: 01
subsystem: geometry
tags: [blazor, svg, canvas-bounds, geometry-tests]
requires: []
provides:
  - CanvasBounds enlarged to 1472 x 828 with grow-never-shrink documentation
  - Home.razor SVG dimensions bound to CanvasBounds
  - Geometry edge tests re-pinned to the 1472 x 828 inclusive surface
affects: [BC-07-selection-lifecycle-and-restyle, canvas, geometry, drawing, dragging]
tech-stack:
  added: []
  patterns:
    - Single CanvasBounds source of truth for rendered SVG and geometry clamps
key-files:
  created: []
  modified:
    - src/BlazorCanvas/Geometry/CanvasBounds.cs
    - src/BlazorCanvas/Geometry/Movement.cs
    - src/BlazorCanvas/Components/Pages/Home.razor
    - tests/BlazorCanvas.Tests/Geometry/NormalisationTests.cs
    - tests/BlazorCanvas.Tests/Geometry/CanvasCoordinatesTests.cs
    - tests/BlazorCanvas.Tests/Geometry/DrawGestureTests.cs
    - tests/BlazorCanvas.Tests/Geometry/ClampTests.cs
    - tests/BlazorCanvas.Tests/Geometry/CircleEncodingTests.cs
key-decisions:
  - "Bound the Home.razor SVG dimensions to CanvasBounds instead of repeating numeric literals, keeping rendered size and clamp size from drifting."
patterns-established:
  - "Canvas surface size changes must update CanvasBounds and edge-intent tests together."
requirements-completed: [CANV-03]
coverage:
  - id: D1
    description: "CanvasBounds exposes 1472 x 828 and documents that the surface may grow but must never shrink."
    requirement: CANV-03
    verification:
      - kind: other
        ref: "Select-String CanvasBounds.cs for Width = 1472 and Height = 828"
        status: pass
      - kind: integration
        ref: "dotnet build BlazorCanvas.sln"
        status: pass
    human_judgment: false
  - id: D2
    description: "The SVG canvas at / uses CanvasBounds.Width and CanvasBounds.Height instead of hardcoded old literals."
    requirement: CANV-03
    verification:
      - kind: other
        ref: "Select-String Home.razor for width=\"@CanvasBounds.Width\" height=\"@CanvasBounds.Height\""
        status: pass
      - kind: integration
        ref: "dotnet build BlazorCanvas.sln"
        status: pass
    human_judgment: false
  - id: D3
    description: "Geometry tests prove drawing and dragging clamp against 0..1472 x 0..828, including right, bottom, far-corner, line, and circle edge cases."
    requirement: CANV-03
    verification:
      - kind: unit
        ref: "dotnet test BlazorCanvas.sln --nologo"
        status: pass
      - kind: other
        ref: "rg -n '\\b(1280|720)\\b' src/BlazorCanvas/Geometry/CanvasBounds.cs src/BlazorCanvas/Geometry/Movement.cs src/BlazorCanvas/Components/Pages/Home.razor tests/BlazorCanvas.Tests/Geometry"
        status: pass
    human_judgment: false
duration: 6min
completed: 2026-07-20
status: complete
---

# Phase BC-06 Plan 01: Canvas Resize to 1472 x 828 Summary

**Fixed-size SVG canvas enlarged to 1472 x 828 with CanvasBounds-driven rendering and re-pinned geometry edge tests.**

## Performance

- **Duration:** 6 min
- **Started:** 2026-07-20T22:17:09Z
- **Completed:** 2026-07-20T22:23:13Z
- **Tasks:** 3 completed
- **Files modified:** 8

## Accomplishments

- Enlarged `CanvasBounds` to 1472 x 828 and corrected docs to the grow-never-shrink rule.
- Bound the Home page SVG dimensions to `CanvasBounds.Width` and `CanvasBounds.Height`.
- Re-pinned edge-intent geometry tests so clamp, draw, coordinate mapping, and circle radius behavior all target the new boundary.
- Verified the full suite and stale-literal gate: no whole-word old boundary numbers remain in the resized source/test surface.

## Task Commits

Each task was committed atomically:

1. **Task 1: Enlarge the constant, correct its stale doc comments, and bind the SVG element** - `4caafb3` (feat)
2. **Task 2: Classify every hardcoded canvas literal in the tests, then re-pin edges to 1472/828** - `6590d11` (test)
3. **Task 3: Prove the green is meaningful - full suite plus stale-literal and SVG-dimension gates** - `571cc68` (chore, verification-only empty commit)

**Plan metadata:** pending final metadata commit.

## Files Created/Modified

- `src/BlazorCanvas/Geometry/CanvasBounds.cs` - Canvas width/height constants changed to 1472/828 and docs updated.
- `src/BlazorCanvas/Geometry/Movement.cs` - Bounds doc comment updated to 0..1472 x 0..828; executable code unchanged.
- `src/BlazorCanvas/Components/Pages/Home.razor` - SVG dimensions now bind to `CanvasBounds`.
- `tests/BlazorCanvas.Tests/Geometry/NormalisationTests.cs` - Canvas bounds assertion renamed and updated.
- `tests/BlazorCanvas.Tests/Geometry/CanvasCoordinatesTests.cs` - Far-corner mapping updated for 1472 x 828 plus 48px toolbar.
- `tests/BlazorCanvas.Tests/Geometry/DrawGestureTests.cs` - Far-corner draw and invariant grid updated to the new boundary.
- `tests/BlazorCanvas.Tests/Geometry/ClampTests.cs` - Right, bottom, maxima, line, and oversized-bound tests re-pinned.
- `tests/BlazorCanvas.Tests/Geometry/CircleEncodingTests.cs` - Right-edge cap and off-canvas centre cases re-pinned.

## Decisions Made

- Bound rendered SVG dimensions to `CanvasBounds` rather than copying 1472/828 into `Home.razor`, preserving a single source of truth for D-18/D-19/D-36.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] Stopped running BlazorCanvas process that locked build output**
- **Found during:** Task 1 verification
- **Issue:** `dotnet build BlazorCanvas.sln` failed because `BlazorCanvas.exe` was locked by PID 17324.
- **Fix:** Stopped PID 17324, then reran the build.
- **Files modified:** None
- **Verification:** `dotnet build BlazorCanvas.sln` passed with 0 warnings and 0 errors.
- **Committed in:** N/A - environment fix only

---

**Total deviations:** 1 auto-fixed (1 blocking)
**Impact on plan:** Verification was unblocked without changing implementation scope.

## Issues Encountered

- The local Windows sandbox helper was unavailable for some edit/read tools, so targeted workspace edits and read-only scans were run with user-approved escalation. No source behavior changed because of this tooling issue.

## Known Stubs

None.

## User Setup Required

None - no external service configuration required.

## Verification

- `dotnet build BlazorCanvas.sln` - passed, 0 warnings, 0 errors.
- `dotnet test BlazorCanvas.sln --nologo` - passed, 405 passed, 0 failed, 0 skipped.
- `rg -n '\b(1280|720)\b' src/BlazorCanvas/Geometry/CanvasBounds.cs src/BlazorCanvas/Geometry/Movement.cs src/BlazorCanvas/Components/Pages/Home.razor tests/BlazorCanvas.Tests/Geometry` - passed by returning no matches.
- SVG binding scan confirmed `width="@CanvasBounds.Width" height="@CanvasBounds.Height"` and no old width/height attributes.
- Diff scope confirmed only the eight planned production/test files changed across task commits.

## Next Phase Readiness

Phase 6 is complete. Phase 7 can build on the resized `Home.razor` SVG without overlapping uncommitted changes.

## Self-Check: PASSED

- Summary file exists at `.planning/phases/BC-06-canvas-resize-to-1472-828/06-01-SUMMARY.md`.
- Task commits exist: `4caafb3`, `6590d11`, `571cc68`.
- Required files exist and match the plan's eight-file scope.
- No unintended deletions were detected.

---
*Phase: BC-06-canvas-resize-to-1472-828*
*Completed: 2026-07-20*