---
status: resolved
trigger: "Human REG-01 retest: no visible preview appears in the originating tab while drawing a figure."
created: "2026-07-22"
updated: "2026-07-22"
---

# Debug Session: Local Drawing Preview

## Symptoms

- **Expected:** While the pointer is held during a valid draw gesture, the originating browser tab visibly renders a local preview. A second same-account tab stays empty until the gesture commits on pointer release.
- **Actual:** The originating tab shows no preview during the gesture; the figure appears only after pointer release and commit.
- **Errors:** No Blazor/browser application errors observed. Reported console warnings/errors are from MetaMask and other browser extension content scripts; Blazor startup and WebSocket connection messages are normal.
- **Timeline:** The user reports the preview existed earlier; it is absent after the recent storage-model/cutover work and remains absent after the first preview gap fix.
- **Reproduction:** In the originating tab, select a drawing tool and press-drag on the canvas. Observe no shape until release. The committed figure subsequently appears normally.

## Current Focus

- **hypothesis:** The preview is stored correctly but only reaches the originating screen through Blazor Server render batches; the real browser never receives a visible preview batch while the pointer gesture is active, even though it receives the later committed figure update.
- **test:** Move the ephemeral SVG element into a browser-local event handler, retain the existing C# gesture state only for commit, and prove the client preview cannot cross the notifier boundary.
- **expecting:** The SVG preview is drawn and removed directly in the origin tab, while only `CommitDrawAsync` publishes the completed figure.
- **next_action:** Resolved after the approved two-window retest.
- **reasoning_checkpoint:**
    hypothesis: "Per-pointer server rendering causes the missing visual preview; direct local SVG mutation removes that dependency while retaining server commit semantics."
    confirming_evidence:
      - "The human retest twice observed the committed figure but never an in-gesture preview, which separates the commit path from the preview render path."
      - "Home.razor held the preview only in a Razor FigureShape and called StateHasChanged after each pointer move; it had no browser-local SVG rendering path."
      - "The console capture has only extension errors and normal Blazor startup/WebSocket messages."
    falsification_test: "If the direct SVG preview is still absent while the module is loaded and pointer events occur, the root cause is not the server render-batch dependency."
    fix_rationale: "The new module draws the preview immediately in the browser that owns the pointer. It never publishes or persists preview geometry, so peer tabs continue to see only the existing committed draw message."
    blind_spots: "No automation browser is available in this environment, so final visual verification requires the reporter."

## Evidence

- timestamp: "2026-07-22"
  observation: "BC-12-02 focused tests (16) and full suite (303) passed, but the human retest remained not approved."
- timestamp: "2026-07-22"
  observation: "Browser console capture contains extension-originated MetaMask/content-script failures, not an application runtime exception."
- timestamp: "2026-07-22"
  observation: "The screenshot's Blazor connection is https://localhost:5054/_blazor. The active debug process listens on 5054; no process listens on the previously reported 7281."
- timestamp: "2026-07-22"
  observation: "Home.razor previously represented the preview solely as a FigureShape in the server-rendered Razor tree. Its session/source tests therefore proved state transitions but not a delivered in-gesture visual."
- timestamp: "2026-07-22"
  observation: "Implemented Home.razor.js browser-local SVG previews with pointer capture and pointer-events disabled. The script is not connected to persistence or CanvasSyncNotifier."
- timestamp: "2026-07-22"
  observation: "Release test run passed: 303/303. node --check Home.razor.js and git diff --check passed."
- timestamp: "2026-07-22"
  observation: "Restarted the debug host with the HTTPS launch profile. It now listens on both https://localhost:7281 and http://localhost:5054; an HTTPS request returned the new Home.razor.js module with the pointer-capture and local-preview code."
- timestamp: "2026-07-22"
  observation: "Human approval: the initiating tab visibly previews the figure while drawing, and the second tab remains unchanged until release commits the figure."

## Eliminated

- hypothesis: "A Blazor runtime exception prevents all rendering."
  reason: "The reported Blazor messages show normal startup and WebSocket connection; no application exception was observed."
- hypothesis: "The reporter was testing the announced https://localhost:7281 endpoint."
  reason: "The DevTools connection in the supplied screenshot explicitly identifies https://localhost:5054/_blazor."

## Resolution

- root_cause: "The transient preview depended entirely on Blazor Server render batches during pointer movement. That indirect render path was not visibly delivered during the gesture, while the later commit path was, leaving a correct local state with no user-visible preview."
- fix: "Render the temporary SVG shape in Home.razor.js directly inside the originating canvas using pointer capture. Home keeps its circuit-local DrawingPreviewSession only to supply the existing authoritative release-time commit; no preview data is synchronized or persisted."
- verification: "Release suite: 303 passed. Home preview contract test now verifies the local module, pointer capture, pointer-event isolation, session updates, and existing commit boundary. node --check and git diff --check passed. The human two-window retest was approved: the initiating tab previews during drawing and the second tab remains unchanged until release commits the figure."
- files_changed:
  - "src/BlazorCanvas/Components/Pages/Home.razor"
  - "src/BlazorCanvas/Components/Pages/Home.razor.js"
  - "tests/BlazorCanvas.Tests/Components/CanvasInteractionCoordinatorTests.cs"
