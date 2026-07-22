---
phase: BC-12-regression-verification
verified: 2026-07-22T18:07:25+02:00
status: human_needed
score: 3/6 must-haves verified
behavior_unverified: 3
overrides_applied: 0
re_verification:
  previous_status: gaps_found
  previous_score: 2/5
  gaps_closed:
    - "The originating tab visibly renders an in-progress drawing preview, while the second tab remains commit-only."
  gaps_remaining: []
  regressions: []
behavior_unverified_items:
  - truth: "A human draws all four shapes with edge clamping, drags each, deletes them, and confirms behavior matches v1.1."
    test: "In two normal same-profile windows, draw a diagonal line, non-square rectangle, centre-out circle, and upright triangle; refresh to confirm order and persistence; then perform the circle edge-clamp and rectangle edge-slide checks."
    expected: "All four committed figures preserve their geometry and order in both windows and after refresh; the circle remains in bounds, and the rectangle stops at the right boundary while sliding vertically without resizing, sticking, or divergence."
    why_human: "The approved retest covered only the browser-local preview and commit boundary. Unit tests cover gesture geometry and persistence contracts, not the complete visual two-window flow."
  - truth: "A human confirms selection and every documented deselect route, including the topmost blue-and-white dashed trace, match v1.1."
    test: "With Pointer, select each visible figure; deselect via blank canvas, arming a shape tool, and a non-Delete toolbar control; then select a figure and delete it."
    expected: "Exactly one figure has a topmost white-underlay/blue-dashed trace with no red outline; every deselect route clears it; Delete removes the selected figure and is disabled with no selection."
    why_human: "Source and tests establish the component and coordinator paths, but they cannot establish interactive visual appearance or input behavior in a browser."
  - truth: "A human opens two same-account browser windows and confirms a committed figure drag glides live in the second window."
    test: "Leave one committed figure visible and drag it slowly for two to three seconds in window A while observing window B; capture before, mid-drag, and after states."
    expected: "Window B visibly traverses intermediate positions before pointer release, finishes at A's final location, and shows neither duplicate figures nor a jump-only update."
    why_human: "Automated integration tests verify canonical move delivery and trailing-edge persistence, not browser-visible timing between two live circuits."
human_verification:
  - test: "Four shapes, persistence/order, and edge clamp/slide"
    expected: "All figures match v1.1 in both windows and after refresh; clamping and edge-slide behavior remain correct."
    why_human: "This portion of the original seven-step REG-01 script is not recorded as completed after the preview correction."
  - test: "Selection, deselection, drag, and delete"
    expected: "Selection trace and every deselect/delete behavior match v1.1, including cross-window deletion and refresh persistence."
    why_human: "Visual selection and interaction flow require a human browser check."
  - test: "Slow committed-drag glide"
    expected: "The second same-profile window visibly glides through intermediate committed positions before release."
    why_human: "The two-window visual timing acceptance was not recorded after the preview correction."
---

# Phase 12: Regression Verification Report

**Phase Goal:** A human confirms, on the running application, that the storage model rewrite is invisible — every user-facing behavior is indistinguishable from v1.1.

**Verified:** 2026-07-22

**Status:** human_needed

**Re-verification:** Yes — preview-gap closure and approved preview-specific two-window retest

## Goal Achievement

### Observable Truths

| # | Truth | Status | Evidence |
| --- | --- | --- | --- |
| 1 | The originating tab visibly updates the in-progress figure while the primary pointer is held. | ✓ VERIFIED | Human approved the corrected retest. `Home.razor.js` attaches a browser-local SVG preview with pointer capture; it is removed on finish. |
| 2 | An in-progress preview remains local; the second tab receives a new figure only after a valid committed draw. | ✓ VERIFIED | Human approved the two-tab boundary: window B stayed unchanged during the gesture and received the figure after release. `Home.razor` imports the local script and commits only through `CanvasInteractionCoordinator.DrawAsync`; focused source-contract and notifier tests pass. |
| 3 | A human draws all four shapes with edge clamping, drags each, deletes them, and confirms behavior matches v1.1. | ⚠️ PRESENT_BEHAVIOR_UNVERIFIED | Registry, coordinator, repository, and regression tests are present, but the full human observation (four shapes, refresh order/persistence, clamp, and edge slide) has not been recorded after the preview correction. |
| 4 | A human confirms selection and all documented deselect routes, including the blue-and-white dashed trace, match v1.1. | ⚠️ PRESENT_BEHAVIOR_UNVERIFIED | Existing component/coordinator paths remain wired; no completed post-fix visual acceptance evidence covers all selection routes. |
| 5 | A human confirms a committed drag visibly glides in a second same-account window in real time. | ⚠️ PRESENT_BEHAVIOR_UNVERIFIED | Final-public integration tests exercise draw/move/delete delivery and trailing-edge persistence, but not the browser-visible two-window timing. |
| 6 | Preflight is healthy and the original failed acceptance preserves evidence without unapproved work. | ✓ VERIFIED | `12-01-SUMMARY.md` records healthy Docker, trusted HTTPS, clean build, 296 tests, one host, and retained logs; the original failure was preserved before the separately authorized preview correction. |

**Score:** 3/6 truths verified (3 present, behavior-unverified)

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
| `CanvasInteractionCoordinator` | second window | canonical draw/move/delete notifier messages | ✓ WIRED, visual timing unverified | `FinalPublicCanvasSyncIntegrationTests` covers canonical relay and throttled trailing coordinates; the slow-drag visual criterion remains a human check. |

## Behavioral Spot-Checks

| Behavior | Command | Result | Status |
| --- | --- | --- | --- |
| Browser-local preview syntax | `node --check src/BlazorCanvas/Components/Pages/Home.razor.js` | Exit 0 | ✓ PASS |
| Preview lifecycle and local/commit-only boundary | `dotnet test BlazorCanvas.sln --nologo --filter "FullyQualifiedName~DrawingPreviewSessionTests|FullyQualifiedName~CanvasInteractionCoordinatorTests"` | 16 passed, 0 failed | ✓ PASS |
| Full rebased suite | `dotnet test BlazorCanvas.sln --nologo -c Release --no-restore` | 303 passed, 0 failed, 0 skipped | ✓ PASS |

## Requirements Coverage

| Requirement | Source Plan | Description | Status | Evidence |
| --- | --- | --- | --- | --- |
| REG-01 | `12-01-PLAN.md`, `12-02-PLAN.md` | Human confirmation that the rewrite is indistinguishable from v1.1. | ⚠️ HUMAN NEEDED | Preview-specific regression is closed and approved, but the remaining required observations were not captured as a complete seven-step acceptance run. |

## Anti-Patterns Found

No blocker anti-pattern was found in the current preview/commit path. The browser-local preview deliberately does not enter persistence or synchronization. The outstanding concern is acceptance evidence, not an observable missing implementation.

## Human Verification Required

### 1. Four shapes, persistence/order, and edge clamp/slide

**Test:** In two normal same-profile browser windows, draw a diagonal line, non-square rectangle, centre-out circle, and upright triangle. Refresh one window to confirm figures and order. Draw a corner-near circle and drag it outward, then drag the rectangle past the right edge and vertically along it.

**Expected:** All four shapes appear correctly in both windows and remain in the same order after refresh. The circle stays inside the canvas; the rectangle clamps at the right edge and slides vertically without resizing, sticking, crossing the boundary, or diverging in window B.

**Why human:** The current test suite does not prove the complete visual two-window interaction.

### 2. Selection, deselection, drag, and delete

**Test:** Select every visible figure using Pointer. Deselect via blank canvas, arming a shape tool, and a non-Delete toolbar control. Drag each non-edge figure; delete a selected figure in window A and refresh window B.

**Expected:** Exactly one figure has the topmost white-underlay/blue-dashed trace, with no red outline. Every deselect route removes it. Drags persist, deletion synchronizes and persists after refresh, and Delete is disabled when nothing is selected.

**Why human:** These are visual and interactive lifecycle requirements.

### 3. Slow committed-drag glide

**Test:** Leave one committed figure visible and drag it slowly in window A for two to three seconds while observing window B. Capture before, during, and after states.

**Expected:** Window B visibly moves through intermediate positions before release and ends at A's final position, without a duplicate or a jump-only update.

**Why human:** Tests verify the protocol and persistence, not the perceived timing between live browser circuits.

## Gaps Summary

The prior preview blocker is closed: a human approved the corrected initiating-tab preview and commit-only remote behavior, and the current focused/full automated checks pass. REG-01 is nevertheless not complete until the remaining original acceptance observations above are performed and recorded. This is a **human_needed** gate, not a product-code gap.

---

_Verified: 2026-07-22T18:07:25+02:00_
_Verifier: generic-agent workaround acting as independent GSD verifier_
