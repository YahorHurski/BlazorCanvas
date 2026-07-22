---
phase: BC-12-regression-verification
plan: "02"
subsystem: canvas-drawing
tags: [blazor-server, svg-canvas, local-preview, cross-tab-sync, regression]
requires:
  - phase: BC-12-regression-verification
    provides: Failed REG-01 acceptance evidence identifying the missing initiating-tab preview.
provides:
  - Circuit-local drawing-preview session that updates through the shape registry during a gesture.
  - Explicit interactive render request for in-progress previews without a synchronization path.
  - Automated preview lifecycle and create-only publication regression coverage.
affects: [REG-01, canvas-drawing, cross-tab-sync]
tech-stack:
  added: []
  patterns:
    - A drawing preview is ephemeral circuit state and is cleared before the existing coordinator commits a validated gesture.
    - Only CanvasInteractionCoordinator.DrawAsync publishes the canonical committed create message.
key-files:
  created:
    - src/BlazorCanvas/Components/Pages/DrawingPreviewSession.cs
    - tests/BlazorCanvas.Tests/Components/DrawingPreviewSessionTests.cs
  modified:
    - src/BlazorCanvas/Components/Pages/Home.razor
    - tests/BlazorCanvas.Tests/Components/CanvasInteractionCoordinatorTests.cs
key-decisions:
  - "The preview session has no repository, notifier, protocol, or persisted-figure dependency; it remains local to the initiating Blazor circuit."
  - "Home explicitly invokes StateHasChanged after preview begin and pointer-move mutations, then clears the session before handing its immutable gesture to the unchanged coordinator draw boundary."
patterns-established:
  - "Pointer-driven visual state that must not synchronize belongs in a small circuit-local session and is exercised through focused lifecycle tests plus a Razor source contract."
requirements-completed: []
coverage:
  - id: D1
    description: Local drawing preview is created, updated using every current shape definition, clamped through registry semantics, and cleared after immutable gesture capture.
    requirement: REG-01
    verification:
      - kind: unit
        ref: "tests/BlazorCanvas.Tests/Components/DrawingPreviewSessionTests.cs#BeginAndUpdate_Rectangle_ExposesTheCurrentLocalPlacement"
        status: pass
      - kind: unit
        ref: "tests/BlazorCanvas.Tests/Components/DrawingPreviewSessionTests.cs#Update_PreservesRegistryPlacementAndCanvasEdgeClamping"
        status: pass
    human_judgment: true
    rationale: "Unit tests prove lifecycle and geometry, but a browser must confirm the SVG visibly updates while the pointer is held down."
  - id: D2
    description: Preview mutations remain local and a committed draw publishes only the canonical create message, never a position message.
    requirement: REG-01
    verification:
      - kind: unit
        ref: "tests/BlazorCanvas.Tests/Components/CanvasInteractionCoordinatorTests.cs#Draw_PublishesCreateWithoutPositionMessage"
        status: pass
      - kind: unit
        ref: "tests/BlazorCanvas.Tests/Components/CanvasInteractionCoordinatorTests.cs#HomeDrawingPreview_IsCircuitLocalAndUsesCompletedGestureForCommit"
        status: pass
    human_judgment: true
    rationale: "The source and notifier tests prove the code boundary; the two-browser visual timing and committed glide still require REG-01 acceptance."
  - id: D3
    description: Human retest confirms that the initiating tab visibly renders an in-progress preview while drawing.
    requirement: REG-01
    verification:
      - kind: manual_procedural
        ref: "BC-12-02 two-window human retest on 2026-07-22"
        status: fail
    human_judgment: true
    rationale: "The user reported that no preview is still visible while creating a figure in the initiating tab. Automated tests did not exercise rendered browser output."
duration: 20min
completed: 2026-07-22
status: complete
---

# Phase BC-12 Plan 02: Drawing Preview Gap Closure Summary

**Automated preview-state and sync-boundary tests pass, but the human retest was not approved: the initiating tab still shows no visible preview while creating a figure.**

## Performance

- **Duration:** 20 min
- **Tasks:** 2/2 completed
- **Files created/modified:** 4

## Accomplishments

- Added `DrawingPreviewSession`, an isolated per-circuit state object that builds placements solely through `ShapeRegistry` and exposes immutable completed gesture data.
- Replaced scattered Home draw-preview fields with the session, rendering its placement only in the current circuit and explicitly requesting a render after begin and pointer-move updates.
- Preserved the commit path: clearing and capturing the local preview precedes the unchanged `CanvasInteractionCoordinator.DrawAsync` call.
- Added focused lifecycle, clamp, source-contract, and canonical create-without-position-message tests.

## Verification

- `dotnet test BlazorCanvas.sln --nologo --filter "FullyQualifiedName~DrawingPreviewSessionTests|FullyQualifiedName~CanvasInteractionCoordinatorTests"` — **16 passed**.
- `dotnet build BlazorCanvas.sln --nologo -v q` — **0 warnings, 0 errors**.
- `dotnet test BlazorCanvas.sln --nologo` — **303 passed, 0 failed, 0 skipped**.

## Human Acceptance Retest

**Result: NOT APPROVED — REG-01 remains incomplete.**

The user reported the following exact failed observation after the corrective build was hosted:

> There is still no visible preview while creating a figure in the initiating tab.

This means the implementation's automated evidence does not prove the browser-visible SVG update required by REG-01. The test suite confirms only state lifecycle, source wiring, and the commit-only notifier boundary; it does not exercise an interactive rendered browser gesture.

No screenshot, recording, or browser-console capture was supplied with this retest. Preserve the failure as reported. Before further diagnosis, request a short screen recording (or screenshots while the primary pointer is held down) of both same-account tabs, plus any browser DevTools console errors, so the initiating-tab render path can be observed without conflating it with cross-tab creation.

## Task Commits

1. **Task 1: Isolate and prove the originating-tab drawing-preview lifecycle** — `7d6aa91` (`feat(BC-12-02): add local drawing preview session`).
2. **Task 2: Wire immediate local SVG refresh and lock down the commit-only sync boundary** — `e03c1d8` (`fix(BC-12-02): render local drawing previews during gesture`).

## Files Created/Modified

- `src/BlazorCanvas/Components/Pages/DrawingPreviewSession.cs` — local preview lifecycle and completed-gesture capture.
- `src/BlazorCanvas/Components/Pages/Home.razor` — session-based pointer and SVG render wiring.
- `tests/BlazorCanvas.Tests/Components/DrawingPreviewSessionTests.cs` — local lifecycle and shape-clamp coverage.
- `tests/BlazorCanvas.Tests/Components/CanvasInteractionCoordinatorTests.cs` — create-only notifier and Razor contract coverage.

## Decisions Made

- Keep in-progress preview state entirely out of repositories, notifier messages, sync protocol, and persisted figures.
- Reuse `IShapeDefinition.FromGesture` for every preview update so current geometry and canvas-edge rules remain authoritative.
- Retain the existing coordinator as the only handoff that can validate, persist, and publish a new figure.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Test equality] Compare serialized geometry rather than record identity for line and triangle placements.**

- **Found during:** Task 1
- **Issue:** Point-array geometry records compare by array identity even when produced from the same registry gesture.
- **Fix:** The regression test now compares placement coordinates and canonical geometry JSON produced by the registered definition.
- **Files modified:** `tests/BlazorCanvas.Tests/Components/DrawingPreviewSessionTests.cs`
- **Verification:** All five preview-session tests pass.
- **Committed in:** `7d6aa91`

**Total deviations:** 1 auto-fixed (1 test correctness).

## Issues Encountered

- **Human retest failed:** the initiating tab still displayed no visible preview while creating a figure. REG-01 remains blocked.
- The prior failed acceptance in `12-01-SUMMARY.md` remains preserved and was not overwritten.

## Evidence Required Before Further Diagnosis

Do not treat automated test success as visual acceptance. Capture the following from a fresh retest before changing product code again:

1. A short screen recording, or paired screenshots, while the primary pointer is still held down in both tabs.
2. Browser DevTools console errors from the initiating tab, if any.
3. The shape used, pointer-down/move/release sequence, and whether the figure appears after release.

After the visible-preview failure is resolved, rerun the complete seven-step two-window REG-01 script from `12-01-PLAN.md` (including commit-only remote visibility and slow committed-drag glide).

## Next Phase Readiness

- Automated coverage remains green, but the failed browser-visible preview blocks REG-01 approval and requires targeted diagnosis with captured evidence.
- The local acceptance host for this retest (`BlazorCanvas.exe`, PID 14740) was safely stopped after the report.
- No product code was changed while recording this failed retest; no schema, persistence model, protocol kinds, notifier behavior, or committed-drag synchronization changed during this continuation.

## Self-Check: PASSED

- The session remains independent of `CanvasInteractionCoordinator`, repositories, `CanvasSyncNotifier`, `SyncMessage`, and `FigureRow`.
- The only Home draw handoff is the completed-gesture call to `coordinator.DrawAsync`.
- The full suite passes, but the user-reported visual preview failure is recorded as authoritative for REG-01.
- Unrelated `.planning/config.json` and root-PDF changes remain unstaged and untouched.

---
*Phase: BC-12-regression-verification*
*Completed: 2026-07-22*
