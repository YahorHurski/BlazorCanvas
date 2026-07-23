# GSD Debug Knowledge Base

Resolved debug sessions. Used by `gsd-debugger` to surface known-pattern hypotheses at the start of new investigations.

---

## local-drawing-preview — Server-rendered transient SVG preview was invisible during a gesture
- **Date:** 2026-07-22
- **Error patterns:** preview absent during pointer draw, committed figure appears on release, no application console error
- **Root cause:** The transient preview relied on Blazor Server render batches during pointer movement, which did not visibly reach the browser before release.
- **Fix:** Draw the temporary SVG directly in the initiating browser using pointer capture; retain C# only for the release-time canonical commit.
- **Files changed:** `src/BlazorCanvas/Components/Pages/Home.razor`, `src/BlazorCanvas/Components/Pages/Home.razor.js`, `tests/BlazorCanvas.Tests/Components/CanvasInteractionCoordinatorTests.cs`
---
