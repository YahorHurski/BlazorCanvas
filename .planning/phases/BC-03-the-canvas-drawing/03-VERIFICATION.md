---
phase: BC-03-the-canvas-drawing
verified: 2026-07-16T15:03:17Z
status: gaps_found
score: 20/21 must-haves verified
behavior_unverified: 0
overrides_applied: 0
next_action: plan_gaps
next_command: "$gsd-plan-phase BC-03 --gaps"
gaps:
  - truth: "The project remains free of application-authored JavaScript while delivering the canvas drawing phase"
    status: failed
    reason: "The locked project contract says no JavaScript anywhere, but a source-level reconnect modal JavaScript module exists and is wired into the app shell."
    artifacts:
      - path: "src/BlazorCanvas/Components/Layout/ReconnectModal.razor"
        issue: "Imports `<script type=\"module\" src=\"@Assets[\"Components/Layout/ReconnectModal.razor.js\"]\"></script>`."
      - path: "src/BlazorCanvas/Components/Layout/ReconnectModal.razor.js"
        issue: "Contains custom JavaScript event handlers and calls to `Blazor.reconnect()`, `Blazor.resumeCircuit()`, and `location.reload()`."
      - path: "src/BlazorCanvas/Components/App.razor"
        issue: "Renders `<ReconnectModal />`, so the JavaScript-backed component is part of the runtime app shell."
    missing:
      - "Remove or replace the custom reconnect modal JavaScript path so the app satisfies the locked no-JavaScript project constraint."
---

# Phase BC-03: The Canvas & Drawing Verification Report

**Phase Goal:** A logged-in user sees their own canvas and can draw all four shapes on it -- and the drawing is still there after a refresh.
**Verified:** 2026-07-16T15:03:17Z
**Status:** gaps_found
**Re-verification:** No -- initial verification

## Goal Achievement

### Observable Truths

| # | Truth | Status | Evidence |
|---|---|---|---|
| 1 | `/` shows a white 1280x720 SVG canvas anchored below the 48px toolbar on a light-grey page, with no CSS border and no rescale. | VERIFIED | `Home.razor` renders `<Toolbar>` then `<svg class="@CanvasSurfaceClass" width="1280" height="720">`; no `viewBox`/`preserveAspectRatio`; `Home.razor.css` sets `.canvas-surface { display: block; background: #FFFFFF; }` and no border; `app.css` sets `html, body { margin: 0; background: #DCE0E5; }`. Human checkpoint approved the real browser bounding box and no-rescale checks. |
| 2 | Toolbar shows exactly six tool/action buttons in locked order, pointer armed on page load, Delete greyed out, Logout right-aligned and separate. | VERIFIED | `Toolbar.razor` has five armable tool buttons (`Pointer`, `Line`, `Rectangle`, `Circle`, `Triangle`) plus Delete, then divider + logout form. `Tool.Pointer` is the default; `aria-pressed` is on the five armable buttons; Delete uses native `disabled="@(!DeleteEnabled)"`; `.logout-form { margin-left: auto; }`. |
| 3 | Page-to-canvas mapping is `canvasX = PageX`, `canvasY = PageY - 48`, in one pure function. | VERIFIED | `CanvasCoordinates.ToolbarHeight = 48`; `FromPage` subtracts `ToolbarHeight` and rounds away from zero. `CanvasCoordinatesTests` cover origin, far corner, rounding, and deliberate no-clamp behavior. `Home.razor` calls `CanvasCoordinates.FromPage(e.PageX, e.PageY)` on down and move, with no `OffsetX/OffsetY`. |
| 4 | Draw gesture uses Phase 1 geometry core for clamp, type dispatch, normalisation, and circle encoding. | VERIFIED | `DrawGesture.Build` clamps all four inputs with `Movement.ClampDelta`, calls `CircleEncoding.ClampDrawRadius`/`FromCentreRadius` for circles, and `Normalisation.Normalise` for other shapes. No geometry is reimplemented in `Home.razor`. |
| 5 | All four shapes draw correctly: line/rectangle/triangle corner-to-corner, circle centre-out, triangle apex top-centre. | VERIFIED | `DrawGestureTests` cover corner-to-corner boxes, line diagonal preservation, centre-out circle, and edge circle clamp. `FigureShape.razor` renders raw line endpoints, rectangles from `Box`, circles via `CircleEncoding.ToCentreRadius`, and triangles with invariant-culture top-centre apex. Human checkpoint approved the visual shape behavior. |
| 6 | Dragging on top of an existing figure draws a new figure rather than moving it. | VERIFIED | Shape tools map through `ToolMap.ToFigureType`; pointer tool returns null and does nothing in this phase. SVG pointer handlers are on the parent SVG, use `PageX/PageY`, and the human checkpoint approved drawing a small circle inside a big rectangle. |
| 7 | Drawing stops at canvas edge; circles never render as ovals. | VERIFIED | `DrawGesture.Build` clamps press/cursor to `0..1280 x 0..720`; `CircleEncoding.ClampDrawRadius` caps radius by nearest edge and floors at zero. `DrawGestureTests.EveryResult_LiesEntirelyInsideTheCanvas_AndCirclesAreAlwaysSquare` passed. Human checkpoint approved edge clamping and circle shape. |
| 8 | Degenerate draw rejection is silent, while horizontal and vertical lines still draw. | VERIFIED | `CommitAsync` gates with `MinSizeGuard.IsDrawable(type, box)` and returns without UI/logging before insert when false. Tests assert press==cursor rejected for all types, horizontal line drawable, vertical line drawable. Human checkpoint approved silent no-op and legal axis-aligned lines. |
| 9 | Leaving the canvas mid-draw commits at the clamped preview position. | VERIFIED | `Home.razor` wires `@onpointerleave="OnPointerLeave"` on the SVG; `OnPointerLeave` calls `CommitAsync` when `drawing`. Human checkpoint approved commit-on-leave. |
| 10 | Every drawn figure is INSERTed immediately with no Save button; database-assigned id is read after insert before joining the view. | VERIFIED | `CommitAsync`: `drawing = false`, captures type/box, checks `MinSizeGuard`, then `await Figures.InsertAsync(userId, type, box); figures.Add(figure);`. `FigureStore.InsertAsync` calls `SaveChangesAsync()` before returning the `Figure`. No Save button exists in the canvas page. |
| 11 | After F5, figures reload in creation order with same overlap/occlusion. | VERIFIED | `FigureStore.LoadAsync` uses `AsNoTracking().Where(f => f.UserId == userId).OrderBy(f => f.Id)`; `Home.razor` renders `figures` in list order with `@key="f.Id"` and no sort/reverse/filter. `FigureStoreTests.LoadAsync_ReturnsFiguresInCreationOrder` passed. Human checkpoint approved F5 occlusion. |
| 12 | Second user sees only their own figures. | VERIFIED | `Home.razor` derives `userId` from `state.User.FindFirst("user_id")`; `FigureStore.LoadAsync(userId)` filters on `UserId`; `FigureStoreTests.LoadAsync_NeverReturnsAnotherUsersFigures` passed. Human checkpoint approved user isolation. |
| 13 | Runtime data access uses short-lived `IDbContextFactory` contexts, not a long-lived circuit DbContext. | VERIFIED | `Program.cs` registers `AddDbContextFactory<CanvasDbContext>` and `AddScoped<FigureStore>`; `FigureStore` creates/disposes a context per call; `Login.razor` migrated to `IDbContextFactory`. |
| 14 | No application-authored JavaScript is present or wired. | FAILED | `src/BlazorCanvas/Components/Layout/ReconnectModal.razor` imports `Components/Layout/ReconnectModal.razor.js`; that file contains custom JS and is mounted from `App.razor` via `<ReconnectModal />`. See Gaps. |

**Score:** 20/21 must-haves verified (0 present, behavior-unverified)

### Required Artifacts

| Artifact | Expected | Status | Details |
|---|---|---|---|
| `src/BlazorCanvas/Geometry/CanvasCoordinates.cs` | Pure page-to-canvas mapping | VERIFIED | Constant 48 and tested `FromPage`. |
| `src/BlazorCanvas/Geometry/DrawGesture.cs` | Pure draw composition | VERIFIED | Delegates to Phase 1 clamp/normalise/circle core. |
| `src/BlazorCanvas/Data/FigureStore.cs` | Load/insert data path | VERIFIED | Per-user `ORDER BY id` load and post-save insert return. |
| `src/BlazorCanvas/Tools/Tool.cs` | Armable tool enum | VERIFIED | Pointer first; no delete armable mode; maps shape tools to `FigureType`. |
| `src/BlazorCanvas/Components/Canvas/Toolbar.razor` | Six-button toolbar + logout | VERIFIED | Mounted from `Home.razor`; Delete disabled by default. |
| `src/BlazorCanvas/Components/Canvas/FigureShape.razor` | SVG renderer for all four shapes and preview | VERIFIED | Bare SVG child elements; white fill on filled shapes; raw line endpoints. |
| `src/BlazorCanvas/Components/Pages/Home.razor` | Canvas page, gesture state machine, commit path | VERIFIED | Toolbar, fixed SVG, ordered figure render, pointer handlers, preview, guarded insert. |
| `src/BlazorCanvas/Components/Layout/ReconnectModal.razor.js` | No custom JavaScript | FAILED | Custom JavaScript exists and is referenced. |

### Key Link Verification

| From | To | Via | Status | Details |
|---|---|---|---|---|
| `PointerEventArgs.PageX/PageY` | `CanvasCoordinates.FromPage` | `Home.razor` down/move handlers | WIRED | Two call sites; no `OffsetX/OffsetY`. |
| `CanvasCoordinates.FromPage` | `DrawGesture.Build` | `Home.razor` preview state | WIRED | Down creates zero-size preview; move recomputes clamped preview. |
| `DrawGesture.Build` | `MinSizeGuard.IsDrawable` | `CommitAsync` | WIRED | Guard runs before `FigureStore.InsertAsync`. |
| `FigureStore.InsertAsync` | `figures.Add(figure)` | `CommitAsync` | WIRED | Append occurs after awaited insert returns the database-assigned id. |
| Cookie `user_id` claim | `FigureStore.LoadAsync(userId)` / `InsertAsync(userId)` | `Home.razor` field | WIRED | `userId` is assigned from claim and passed to both load and insert. |
| `FigureStore.LoadAsync` | SVG document order | `ORDER BY id` + `foreach` | WIRED | Store order flows directly to render order. |
| `ReconnectModal.razor` | custom JS module | `<script type="module" ...>` | NOT_ALLOWED | Violates the locked no-JavaScript constraint. |

### Data-Flow Trace (Level 4)

| Artifact | Data Variable | Source | Produces Real Data | Status |
|---|---|---|---|---|
| `Home.razor` | `figures` | `FigureStore.LoadAsync(userId)` | Yes -- EF query against `figures` filtered by `user_id`, ordered by `id` | FLOWING |
| `Home.razor` | `previewBox` | `DrawGesture.Build(previewType, pressX, pressY, cursorX, cursorY)` | Yes -- recomputed from pointer events through geometry core | FLOWING |
| `FigureStore` | returned inserted `Figure` | EF `SaveChangesAsync()` identity insert | Yes -- returned after database assigns `Id` | FLOWING |

### Behavioral Spot-Checks

| Behavior | Command | Result | Status |
|---|---|---|---|
| Build succeeds | `dotnet build BlazorCanvas.sln` | Passed, 0 warnings, 0 errors | PASS |
| Geometry + data-store phase tests pass | `dotnet test tests/BlazorCanvas.Tests/BlazorCanvas.Tests.csproj --filter "FullyQualifiedName~DrawGesture|FullyQualifiedName~CanvasCoordinates|FullyQualifiedName~FigureStore"` | Passed: 229 passed, 0 failed, 0 skipped. One transient copy warning appeared because build/test were launched in parallel; a subsequent build was clean. | PASS |
| Full suite already run after plan 03-05 | Provided verification context | `388 passed, 0 failed, 0 skipped` | PASS |
| Real-screen Phase 3 flow | 03-05 human checkpoint | Approved by user | PASS |
| Custom JavaScript absence | `Get-ChildItem -Recurse src/BlazorCanvas -Include *.js,*.ts` | Found `Components/Layout/ReconnectModal.razor.js` | FAIL |

### Probe Execution

No phase probes were declared or discovered for BC-03. Step 7c skipped.

### Requirements Coverage

| Requirement | Source Plan | Description | Status | Evidence |
|---|---|---|---|---|
| CANV-01 | 03-01, 03-04 | Fixed 1280x720 SVG at (0,48), 1:1, no border, PageX/PageY mapping | SATISFIED | `CanvasCoordinates`, fixed SVG attrs/CSS, tests, human checkpoint. |
| CANV-02 | 03-03, 03-04 | Six-button toolbar, pointer armed, Delete disabled, Logout separate | SATISFIED | `Toolbar.razor`/CSS mounted from `Home.razor`, human checkpoint. |
| FIG-01 | 03-01, 03-03, 03-05 | Draw all four shapes, preview, clamp, silent rejection, immediate insert | SATISFIED | `DrawGesture`, `FigureShape`, `Home.razor` state machine, tests, human checkpoint. |
| DATA-01 | 03-02, 03-04, 03-05 | One canvas per user; load `WHERE user_id ORDER BY id`; isolation | SATISFIED | `FigureStore`, claim-sourced `userId`, integration tests, human checkpoint. |

No orphaned Phase 3 requirements found: ROADMAP and REQUIREMENTS map exactly DATA-01, CANV-01, CANV-02, FIG-01 to Phase 3, and all appear in plan frontmatter.

### Anti-Patterns Found

| File | Line | Pattern | Severity | Impact |
|---|---:|---|---|---|
| `src/BlazorCanvas/Components/Layout/ReconnectModal.razor.js` | 1 | Application-authored JavaScript | BLOCKER | Violates the locked project no-JavaScript constraint. |

Debt/stub scan over BC-03 modified files found no `TBD`, `FIXME`, `XXX`, `TODO`, `HACK`, `PLACEHOLDER`, placeholder text, empty implementations, or console-log handlers. The broad `try|catch` scan of `Home.razor` matched only the substring in `Geometry`; no `try` or `catch` statement exists in the canvas component.

### Human Verification Required

None remaining. The runtime/visual items that cannot be proven from source alone were covered by the blocking 03-05 human checkpoint, which the provided context says the user approved.

### Gaps Summary

The BC-03 drawing goal is implemented and verified: a logged-in user gets a fixed canvas, all four shape tools, live preview, clamped drawing, silent degenerate rejection, immediate insert persistence, reload order, and cross-user isolation.

However, the phase cannot be marked passed because the project-wide locked prohibition "no JavaScript anywhere" is currently false. `ReconnectModal.razor.js` is present in app source and wired through `ReconnectModal.razor`/`App.razor`. That is outside the drawing path, but it is still part of the shipped app shell and violates a non-negotiable constraint carried by ROADMAP/PROJECT.

---

_Verified: 2026-07-16T15:03:17Z_
_Verifier: Codex (gsd-verifier)_
