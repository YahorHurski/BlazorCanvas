---
status: complete
phase: BC-15-draw-preview-render-persist-a-star
source:
  - 15-VERIFICATION.md
started: 2026-07-22T23:44:00+02:00
updated: 2026-07-23T00:24:00+02:00
---

## Current Test

[testing complete]

## Tests

### 1. Browser Star Draw UAT

expected: The toolbar arms Star, the preview follows the cursor as a five-point star, the shape clamps at the canvas edge, commit creates the same five-point star, and refresh reloads it unchanged without pressing a Save button.
result: pass
previous_result: issue
reported: "not pass. I dont see preview of any figure. But star is creating good, and i see button on the toolbar. So the only problem fjr now is preview"
severity: major
retest_reason: "Gap G-15-1 was fixed by 15-04-SUMMARY.md; browser UAT needs a fresh confirmation."
retest_result: "pass"

Instructions:

1. Run the app.
2. Log in and open the canvas.
3. Arm the Star toolbar button.
4. Drag a star near and beyond a canvas edge.
5. Release the pointer.
6. Refresh the page.
7. Confirm the same star remains visible and unchanged, with no Save button involved.

## Summary

total: 1
passed: 1
issues: 0
pending: 0
skipped: 0
blocked: 0

## Gaps

- gap_id: G-15-1
  truth: "The toolbar arms Star, the preview follows the cursor as a five-point star, the shape clamps at the canvas edge, commit creates the same five-point star, and refresh reloads it unchanged without pressing a Save button."
  status: resolved
  reason: "User reported: not pass. I dont see preview of any figure. But star is creating good, and i see button on the toolbar. So the only problem fjr now is preview"
  severity: major
  test: 1
  resolved_by: "15-04-PLAN.md"
  resolved_at: 2026-07-23T00:20:00+02:00
  root_cause: "Home.razor passes the preview type string parameter as the literal value `preview.Type` (`PreviewType=\"preview.Type\"`) instead of binding the active `preview.Type` value. FigureShape rejects that literal through `Registry.Contains(PreviewType)`, so Geometry stays null and no preview SVG is emitted for any shape."
  artifacts:
    - path: "src/BlazorCanvas/Components/Pages/Home.razor"
      issue: "Active preview FigureShape uses literal string binding for PreviewType."
    - path: "src/BlazorCanvas/Components/Canvas/FigureShape.razor"
      issue: "Preview rendering only uses placement geometry when Registry.Contains(PreviewType) succeeds."
    - path: "tests/BlazorCanvas.Tests/Components/HomePreviewSourceTests.cs"
      issue: "Source contract currently asserts the incorrect literal `PreviewType=\"preview.Type\"` string."
  missing:
    - "Bind PreviewType to the session value in Home.razor."
    - "Update tests so they fail on literal string binding and verify preview parameter/render behavior."
    - "Account for the running UAT app locking BlazorCanvas.exe before any build-backed verification."
  debug_session: ".planning/debug/phase-15-preview-missing.md"
