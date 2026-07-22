---
phase: BC-12-regression-verification
verified: 2026-07-22T18:07:25+02:00
status: passed
score: 6/6 must-haves verified
behavior_unverified: 0
overrides_applied: 0
re_verification:
  previous_status: gaps_found
  previous_score: 2/5
  gaps_closed:

    - "The originating tab visibly renders an in-progress drawing preview, while the second tab remains commit-only."
  gaps_remaining: []
  regressions: []
behavior_unverified_items: []
human_verification:

  - test: "Four shapes, persistence/order, and edge clamp/slide"
    expected: "All figures match v1.1 in both windows and after refresh; clamping and edge-slide behavior remain correct."
    result: pass

  - test: "Selection, deselection, drag, and delete"
    expected: "Selection trace and every deselect/delete behavior match v1.1, including cross-window deletion and refresh persistence."
    result: pass

  - test: "Slow committed-drag glide"
    expected: "The second same-profile window visibly glides through intermediate committed positions before release."
    result: pass
---

# Phase 12: Regression Verification Report

**Phase Goal:** A human confirms, on the running application, that the storage model rewrite is invisible — every user-facing behavior is indistinguishable from v1.1.

**Verified:** 2026-07-22

**Status:** passed

**Re-verification:** Yes — preview-gap closure and approved preview-specific two-window retest

## Goal Achievement

### Observable Truths

| # | Truth | Status | Evidence |
| --- | --- | --- | --- |
| 1 | The originating tab visibly updates the in-progress figure while the primary pointer is held. | ✓ VERIFIED | Human approved the corrected retest. `Home.razor.js` attaches a browser-local SVG preview with pointer capture; it is removed on finish. |
| 2 | An in-progress preview remains local; the second tab receives a new figure only after a valid committed draw. | ✓ VERIFIED | Human approved the two-tab boundary: window B stayed unchanged during the gesture and received the figure after release. `Home.razor` imports the local script and commits only through `CanvasInteractionCoordinator.DrawAsync`; focused source-contract and notifier tests pass. |
| 3 | A human draws all four shapes with edge clamping, drags each, deletes them, and confirms behavior matches v1.1. | ✓ VERIFIED | `12-UAT.md` Test 1 passed: four shapes, persistence/order, edge clamp, and edge slide matched v1.1. |
| 4 | A human confirms selection and all documented deselect routes, including the blue-and-white dashed trace, match v1.1. | ✓ VERIFIED | `12-UAT.md` Test 2 passed: selection trace, deselection routes, drag, deletion, and disabled Delete state matched v1.1. |
| 5 | A human confirms a committed drag visibly glides in a second same-account window in real time. | ✓ VERIFIED | `12-UAT.md` Test 3 passed: the second tab visibly moved through intermediate positions and ended at the same final position. |
| 6 | Preflight is healthy and the original failed acceptance preserves evidence without unapproved work. | ✓ VERIFIED | `12-01-SUMMARY.md` records healthy Docker, trusted HTTPS, clean build, 296 tests, one host, and retained logs; the original failure was preserved before the separately authorized preview correction. |

**Score:** 6/6 truths verified

## Required Artifacts

| Artifact | Expected | Status | Details |
| --- | --- | --- | --- |
| `src/BlazorCanvas/Components/Pages/Home.razor` | Local-preview integration and single committed-draw handoff | ✓ VERIFIED | Imports `Home.razor.js`, attaches it to the SVG, holds an independent `DrawingPreviewSession`, and calls `DrawAsync` only after `preview.Complete()`. |
| `src/BlazorCanvas/Components/Pages/Home.razor.js` | Browser-local SVG preview during the gesture | ✓ VERIFIED | Creates SVG elements tagged `data-local-drawing-preview`, uses pointer capture, draws only inside the local canvas DOM, and removes the element on finish. `node --check` passed. |
| `src/BlazorCanvas/Components/Pages/DrawingPreviewSession.cs` | Circuit-local C# gesture state | ✓ VERIFIED | Has only registry/geometry dependencies, no repository, notifier, protocol, or persisted-figure dependency. |
| `tests/BlazorCanvas.Tests/Components/DrawingPreviewSessionTests.cs` | Preview lifecycle and geometry/clamp coverage | ✓ VERIFIED | Focused suite passed 16/16 tests. |
| `.planning/phases/BC-12-regression-verification/12-01-SUMMARY.md` | Initial acceptance outcome and preflight evidence | ✓ VERIFIED | Substantive preserved record of the original failed run and its evidence locations. |

## Key Link Verification

| From | To | Via | Status | Details |
| --- | --- | --- | --- | --- |
| `Home.razor` SVG | `Home.razor.js` | `OnAfterRenderAsync` imports the module and calls `attach(canvasSurface)` | ✓ WIRED | The JS listener performs local DOM preview updates without a .NET render batch. |
| Local preview | Cross-window notifier | `preview.Complete()` then `coordinator.DrawAsync(...)` | ✓ WIRED | `Home.razor` has no `Notifier.Publish`; the coordinator publishes only canonical committed `draw` records. The human approved the resulting two-window boundary. |
| `CanvasInteractionCoordinator` | second window | canonical draw/move/delete notifier messages | ✓ WIRED AND VERIFIED | `FinalPublicCanvasSyncIntegrationTests` covers canonical relay and throttled trailing coordinates; `12-UAT.md` confirms the browser-visible slow committed-drag glide. |

## Behavioral Spot-Checks

| Behavior | Command | Result | Status |
| --- | --- | --- | --- |
| Browser-local preview syntax | `node --check src/BlazorCanvas/Components/Pages/Home.razor.js` | Exit 0 | ✓ PASS |
| Preview lifecycle and local/commit-only boundary | `dotnet test BlazorCanvas.sln --nologo --filter "FullyQualifiedName~DrawingPreviewSessionTests|FullyQualifiedName~CanvasInteractionCoordinatorTests"` | 16 passed, 0 failed | ✓ PASS |
| Full rebased suite | `dotnet test BlazorCanvas.sln --nologo -c Release --no-restore` | 303 passed, 0 failed, 0 skipped | ✓ PASS |

## Requirements Coverage

| Requirement | Source Plan | Description | Status | Evidence |
| --- | --- | --- | --- | --- |
| REG-01 | `12-01-PLAN.md`, `12-02-PLAN.md`, `12-UAT.md` | Human confirmation that the rewrite is indistinguishable from v1.1. | ✓ VERIFIED | Preview-specific regression is closed and approved; the remaining UAT checks for four-shape behavior, selection/delete, and slow committed-drag glide all passed. |

## Anti-Patterns Found

No blocker anti-pattern was found in the current preview/commit path. The browser-local preview deliberately does not enter persistence or synchronization, and the completed UAT covers the remaining acceptance evidence.

## Human Verification Completed

### 1. Four shapes, persistence/order, and edge clamp/slide

**Test:** In two normal same-profile browser windows, draw a diagonal line, non-square rectangle, centre-out circle, and upright triangle. Refresh one window to confirm figures and order. Draw a corner-near circle and drag it outward, then drag the rectangle past the right edge and vertically along it.

**Expected:** All four shapes appear correctly in both windows and remain in the same order after refresh. The circle stays inside the canvas; the rectangle clamps at the right edge and slides vertically without resizing, sticking, crossing the boundary, or diverging in window B.

**Result:** pass, recorded in `12-UAT.md`.

### 2. Selection, deselection, drag, and delete

**Test:** Select every visible figure using Pointer. Deselect via blank canvas, arming a shape tool, and a non-Delete toolbar control. Drag each non-edge figure; delete a selected figure in window A and refresh window B.

**Expected:** Exactly one figure has the topmost white-underlay/blue-dashed trace, with no red outline. Every deselect route removes it. Drags persist, deletion synchronizes and persists after refresh, and Delete is disabled when nothing is selected.

**Result:** pass, recorded in `12-UAT.md`.

### 3. Slow committed-drag glide

**Test:** Leave one committed figure visible and drag it slowly in window A for two to three seconds while observing window B. Capture before, during, and after states.

**Expected:** Window B visibly moves through intermediate positions before release and ends at A's final position, without a duplicate or a jump-only update.

**Result:** pass, recorded in `12-UAT.md`.

## Gaps Summary

The prior preview blocker is closed: a human approved the corrected initiating-tab preview and commit-only remote behavior, and the current focused/full automated checks pass. The remaining REG-01 acceptance observations were completed through `12-UAT.md` with 3 passed, 0 issues, 0 blocked.

---

_Verified: 2026-07-22T18:07:25+02:00_
_Verifier: generic-agent workaround acting as independent GSD verifier_
