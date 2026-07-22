---
phase: BC-15-draw-preview-render-persist-a-star
verified: 2026-07-22T21:42:14Z
status: human_needed
next_action: human_uat
score: 12/12 must-haves verified
behavior_unverified: 0
overrides_applied: 0
gaps: []
human_verification:
  - test: "In the running app, arm the Star toolbar button, drag a star near and beyond a canvas edge, release, refresh the page, and confirm the same star remains visible."
    expected: "The toolbar arms Star, the preview follows the cursor as a five-point star, the shape clamps at the canvas edge, commit creates the same five-point star, and refresh reloads it unchanged without pressing a Save button."
    why_human: "Source and unit/integration tests verify wiring and data behavior, but the actual visual pointer flow and cursor-relative preview require browser UAT."
verification_debt:
  - id: WR-01
    severity: warning
    source: "15-REVIEW.md"
    item: "Pointercancel/lostpointercapture cleanup is JS-only, so canceled gestures may leave stale Blazor preview state."
    disposition: "Not blocking Phase 15 goal achievement; record as follow-up robustness debt because the phase contract does not require cancellation cleanup."
---

# Phase 15: Draw, Preview, Render & Persist a Star Verification Report

**Phase Goal:** A user can draw a star end-to-end exactly like the four existing shapes - armed from the toolbar, previewed live under the cursor, clamped at the canvas edge, rendered correctly on commit, and persisted immediately with no Save button.  
**Verified:** 2026-07-22T21:42:14Z  
**Status:** human_needed  
**Re-verification:** No - initial verification

## Goal Achievement

### Observable Truths

| # | Truth | Status | Evidence |
|---|---|---|---|
| 1 | User can arm the star tool and draw a star corner-to-corner like rectangle/triangle. | VERIFIED | `Toolbar.razor` has an armable `Tool.Star` button; `ToolMap.ToShapeName(Tool.Star)` returns `star5`; `Home.razor` begins preview on pointerdown and commits `completed.Type` through `coordinator.DrawAsync`; focused tests pass. |
| 2 | Live star preview follows the cursor and uses the same shape as commit. | VERIFIED | `DrawingPreviewSession.Update` recomputes placement through `_registry.Get(Type).FromGesture`; `Home.razor` renders `FigureShape PreviewPlacement="preview.Placement" PreviewType="preview.Type"`; tests compare star preview JSON to registry `Star5Shape` output. |
| 3 | Edge clamping, zero extent rejection, and positive sliver acceptance work for star. | VERIFIED | `Star5Shape.FromGesture` clamps to `CanvasBounds`; coordinator tests cover out-of-bounds clamp, zero-width/height silent rejection, and one-unit positive sliver commit. |
| 4 | Committed star renders from local geometry under the v1.11 translate/rotate transform. | VERIFIED | `FigureShape.razor` switches on `Star5Geometry` and renders `points="@Points(star.Points)"` inside `<g transform="@Transform">`; render contract tests pin transform and point-list rendering. |
| 5 | Star persists immediately with no Save button and reloads unchanged. | VERIFIED | `CanvasInteractionCoordinator.DrawAsync` validates then calls injected `_insert`; `FigureRepository.InsertAsync` writes `public.figures`; final-public integration test loads the same row through a new `FigureRepository`. |
| 6 | Star drawing uses the registry/gateway path, not a per-type draw switch. | VERIFIED | `DrawAsync` calls `FigureInputGateway.TryValidateGesture`; no `case "star5"` production draw switch found; tests assert `star5` rows and geometry are created through the coordinator boundary. |
| 7 | A committed star becomes the selected row immediately. | VERIFIED | `DrawAsync` sets `SelectedId = row.Id`; `StarToolDraw...CommitsSelectedStar5...` asserts the inserted star row is selected. |
| 8 | JS no longer renders star as triangle fallback or duplicate formula. | VERIFIED | `Home.razor.js` only handles pointer capture and stale cleanup; source tests assert no `star5`, `document.createElementNS`, `points`, `Math.cos`, or `Math.sin` preview geometry code. |
| 9 | Preview remains local-only and never enters `CanvasSyncNotifier`. | VERIFIED | Preview state is in `DrawingPreviewSession`; `CommitDrawAsync` is the only preview-to-coordinator boundary; tests assert preview block has no notifier use and final-public star draw emits no `preview` message. |
| 10 | Malformed persisted star geometry fails closed. | VERIFIED | `FigureShape.razor` parses via registry `TryParseGeometry` and catches `JsonException`; render contract test pins this fail-closed path and absence of raw markup. |
| 11 | Final-public committed star draw relays as a committed draw only. | VERIFIED | `Star5Draw_PersistsImmediatelyRelaysCommittedDrawOnlyAndReloadsUnchanged` asserts one draw message with the row and no preview message. |
| 12 | Requirement IDs FIG-05, FIG-06, FIG-07, RENDER-02, DATA-05 are all accounted for. | VERIFIED | PLAN frontmatter covers all five; REQUIREMENTS.md maps exactly these five IDs to Phase 15 and no extra Phase 15 requirement IDs were found. |

**Score:** 12/12 truths verified, with 1 human visual/UAT check still required.

### Required Artifacts

| Artifact | Expected | Status | Details |
|---|---|---|---|
| `src/BlazorCanvas/Components/Pages/Home.razor` | Star preview rendered through Razor/FigureShape and committed through coordinator | VERIFIED | Substantive and wired: toolbar binding, preview session, FigureShape preview child, `CommitDrawAsync`. |
| `src/BlazorCanvas/Components/Pages/Home.razor.js` | Geometry-free pointer capture/cleanup helper | VERIFIED | Contains pointer capture/release and stale `data-local-drawing-preview` cleanup; no SVG shape creation or star fallback formula. |
| `src/BlazorCanvas/Components/Pages/DrawingPreviewSession.cs` | Registry-derived preview placement | VERIFIED | `Begin` and `Update` call registry `FromGesture`; `Complete` captures gesture then clears preview state. |
| `src/BlazorCanvas/Components/Canvas/FigureShape.razor` | Star5Geometry renderer under local transform | VERIFIED | `case Star5Geometry star` polygon uses `Points(star.Points)` inside `Transform`. |
| `src/BlazorCanvas/Shapes/Star5Shape.cs` | Canonical star geometry, clamp, drawable checks | VERIFIED | Produces ten local points, serializes `innerRatio`, rejects degenerate bounds, clamps gesture points. |
| `tests/BlazorCanvas.Tests/Components/CanvasInteractionCoordinatorTests.cs` | Draw/clamp/reject/sliver/selection coverage | VERIFIED | Focused test run passed. |
| `tests/BlazorCanvas.Tests/Components/HomePreviewSourceTests.cs` | Preview source wiring and JS non-ownership coverage | VERIFIED | Focused test run passed. |
| `tests/BlazorCanvas.Tests/Database/V11/FinalPublicCanvasSyncIntegrationTests.cs` | Immediate persistence/reload coverage | VERIFIED | Focused and full solution test runs passed. |

### Key Link Verification

| From | To | Via | Status | Details |
|---|---|---|---|---|
| `Toolbar.razor` | `Home.razor` | `@bind-Armed="armedTool"` | WIRED | Star button invokes `ArmedChanged.InvokeAsync(Tool.Star)`; Home reads `armedTool` through `ToolMap`. |
| `Tool.cs` | `CanvasInteractionCoordinator.DrawAsync` | `ToolMap.ToShapeName(Tool.Star)` -> `completed.Type` | WIRED | Manual trace verifies `star5` reaches `DrawAsync`; helper false-negative was due conceptual from/to wording. |
| `CanvasInteractionCoordinator.cs` | `FigureRepository.InsertAsync` | injected `_insert` callback | WIRED | `DrawAsync` validates through gateway, calls `_insert`, adds row, selects row, publishes committed draw. |
| `DrawingPreviewSession.cs` | `FigureShape.razor` | `Home.razor` renders `preview.Placement` | WIRED | Preview placement is passed to FigureShape as the final SVG drawing child. |
| `FigureShape.razor` | `Star5Geometry.cs` | typed `case Star5Geometry star` | WIRED | Star geometry branch renders ordered local points. |

### Data-Flow Trace (Level 4)

| Artifact | Data Variable | Source | Produces Real Data | Status |
|---|---|---|---|---|
| `Home.razor` | `preview.Placement` | `DrawingPreviewSession.Begin/Update` from pointer events and registry `FromGesture` | Yes | FLOWING |
| `FigureShape.razor` | `Geometry` | persisted `Figure.GeometryJson` parsed through registry, or `PreviewPlacement.Geometry` | Yes | FLOWING |
| `CanvasInteractionCoordinator.cs` | inserted `FigureRow` | `_insert` callback to repository in app/integration wiring | Yes | FLOWING |
| `FigureRepository.cs` | persisted row | parameterized `INSERT ... RETURNING` and `LoadAsync` query | Yes | FLOWING |

### Behavioral Spot-Checks

| Behavior | Command | Result | Status |
|---|---|---|---|
| Phase 15 focused behavior and source contracts | `dotnet test tests/BlazorCanvas.Tests/BlazorCanvas.Tests.csproj --no-restore --filter "FullyQualifiedName~CanvasInteractionCoordinatorTests|FullyQualifiedName~DrawingPreviewSessionTests|FullyQualifiedName~HomePreviewSourceTests|FullyQualifiedName~V11RenderContractTests|FullyQualifiedName~FinalPublicCanvasSyncIntegrationTests"` | 32 passed, 0 failed | PASS |
| Full solution regression | `dotnet test BlazorCanvas.sln --no-restore` | 540 passed, 0 failed | PASS |

### Requirements Coverage

| Requirement | Source Plan | Description | Status | Evidence |
|---|---|---|---|---|
| FIG-05 | 15-01 | User can arm star and draw corner-to-corner like rectangle/triangle. | SATISFIED | Toolbar, ToolMap, Home commit path, coordinator star draw tests. |
| FIG-06 | 15-03 | Live star preview follows cursor and matches committed shape, not triangle/second formula. | SATISFIED | DrawingPreviewSession registry tests and HomePreviewSourceTests; JS geometry removed. |
| FIG-07 | 15-01 | Edge clamp, zero extent rejection, positive sliver acceptance. | SATISFIED | Star5Shape clamp and coordinator edge/degenerate/sliver tests. |
| RENDER-02 | 15-02 | Persisted star renders from local geometry under translate/rotate after reload. | SATISFIED | FigureShape Star5Geometry branch and render contract tests. |
| DATA-05 | 15-01 | Drawn star persists immediately and reappears unchanged after refresh. | SATISFIED | Final-public repository integration test with independent reload. |

No orphaned Phase 15 requirement IDs were found in REQUIREMENTS.md.

### Anti-Patterns Found

| File | Line | Pattern | Severity | Impact |
|---|---:|---|---|---|
| `src/BlazorCanvas/Components/Pages/DrawingPreviewSession.cs` | 49 | `return null` | Info | Expected null return for inactive completion, not a stub. |
| `src/BlazorCanvas/Components/Pages/Home.razor` | 23 | Missing Blazor `pointercancel` handler | Warning | Canceled gestures may leave stale local preview state; not part of Phase 15 must-have path but should be fixed in follow-up. |

### Human Verification Required

#### 1. Browser Star Draw UAT

**Test:** In the running app, arm the Star toolbar button, drag a star near and beyond a canvas edge, release, refresh the page, and confirm the same star remains visible.  
**Expected:** The toolbar arms Star, the preview follows the cursor as a five-point star, the shape clamps at the canvas edge, commit creates the same five-point star, and refresh reloads it unchanged without pressing a Save button.  
**Why human:** Source and unit/integration tests verify wiring and data behavior, but the actual visual pointer flow and cursor-relative preview require browser UAT.

### Gaps Summary

No blocking gaps found. The codebase satisfies the Phase 15 must-haves at the source, wiring, data-flow, and automated behavioral-test levels. The pointer cancellation warning is recorded as verification debt rather than a blocker because the phase goal does not require cancellation cleanup; the normal draw/preview/commit/reload flow is verified.

---

_Verified: 2026-07-22T21:42:14Z_  
_Verifier: the agent (gsd-verifier)_
