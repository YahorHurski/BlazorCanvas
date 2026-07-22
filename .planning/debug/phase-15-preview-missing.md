---
status: diagnosed
trigger: "Diagnose root cause only for UAT gap G-15-1. Preview missing for any figure; Star button and commit work. Do not edit source files and do not apply fixes."
created: 2026-07-23T00:00:12.9954059+02:00
updated: 2026-07-23T00:03:14.5894824+02:00
---

## Current Focus
<!-- OVERWRITE on each update - reflects NOW -->

hypothesis: Preview is invisible because Home.razor passes the string parameter PreviewType as the literal "preview.Type", so FigureShape refuses to render preview geometry even though DrawingPreviewSession placement is valid.
test: Compare Home.razor parameter syntax with FigureShape.OnParametersSet registry check; run focused preview/source tests with --no-build.
expecting: Existing focused tests pass because they assert the incorrect literal source string, while code inspection shows Registry.Contains("preview.Type") is false for every figure.
next_action: Return ROOT CAUSE FOUND to caller with suggested fix direction; do not edit source files.

## Symptoms
<!-- Written during gathering, then IMMUTABLE -->

expected: The toolbar arms Star, the preview follows the cursor as a five-point star, the shape clamps at the canvas edge, commit creates the same five-point star, and refresh reloads it unchanged without pressing a Save button.
actual: User reports: "not pass. I dont see preview of any figure. But star is creating good, and i see button on the toolbar. So the only problem fjr now is preview"
errors: No explicit runtime error reported; automated Phase 15 tests passed before browser UAT found missing preview.
reproduction: Test 1 in D:/Project1/.planning/phases/BC-15-draw-preview-render-persist-a-star/15-UAT.md
started: Discovered during browser UAT after automated Phase 15 tests passed.

## Eliminated
<!-- APPEND only - prevents re-investigating -->

## Evidence
<!-- APPEND only - facts discovered -->

- timestamp: 2026-07-23T00:02:06.0676544+02:00
  checked: D:/Project1/.planning/phases/BC-15-draw-preview-render-persist-a-star/15-UAT.md
  found: Test 1 failed only for live preview visibility; toolbar Star and committed star creation persisted correctly.
  implication: Pointerdown/move/up and commit path are mostly functional; failure is specific to active preview rendering.
- timestamp: 2026-07-23T00:02:06.0676544+02:00
  checked: D:/Project1/src/BlazorCanvas/Components/Pages/Home.razor
  found: The active preview block renders `<FigureShape PreviewPlacement="preview.Placement" PreviewType="preview.Type" Selectable="false" />`.
  implication: `PreviewPlacement` is a non-string component parameter and can bind as an expression, but `PreviewType` is a string parameter and the markup passes the literal text "preview.Type" rather than the session's current type value.
- timestamp: 2026-07-23T00:02:06.0676544+02:00
  checked: D:/Project1/src/BlazorCanvas/Components/Canvas/FigureShape.razor
  found: Preview rendering only sets `Geometry = placement.Geometry` when `PreviewPlacement is { } placement && Registry.Contains(PreviewType)`.
  implication: `Registry.Contains("preview.Type")` is false for line, rectangle, circle, triangle, and star5, so Geometry remains null and the component emits no SVG child for any active preview.
- timestamp: 2026-07-23T00:02:06.0676544+02:00
  checked: D:/Project1/src/BlazorCanvas/Components/Pages/DrawingPreviewSession.cs
  found: Begin and Update compute Placement through the injected ShapeRegistry, and Complete returns the captured Type/Press/Cursor used by coordinator.DrawAsync.
  implication: The same preview session can produce a good committed star while the rendered preview remains invisible due to the downstream PreviewType binding.
- timestamp: 2026-07-23T00:02:06.0676544+02:00
  checked: D:/Project1/tests/BlazorCanvas.Tests/Components/HomePreviewSourceTests.cs
  found: Source tests assert `PreviewType="preview.Type"` appears in Home.razor.
  implication: The test contract pins the bug instead of catching it; it checks source text, not actual component parameter semantics.
- timestamp: 2026-07-23T00:03:14.5894824+02:00
  checked: dotnet test tests/BlazorCanvas.Tests/BlazorCanvas.Tests.csproj --no-restore --filter "FullyQualifiedName~DrawingPreviewSessionTests|FullyQualifiedName~HomePreviewSourceTests"
  found: Build-backed test run could not complete because running process `BlazorCanvas (13316)` locked D:/Project1/src/BlazorCanvas/bin/Debug/net10.0/BlazorCanvas.exe.
  implication: The local UAT app appears to be running; use --no-build for read-only test evidence without stopping the user's process.
- timestamp: 2026-07-23T00:03:14.5894824+02:00
  checked: dotnet test tests/BlazorCanvas.Tests/BlazorCanvas.Tests.csproj --no-restore --no-build --filter "FullyQualifiedName~DrawingPreviewSessionTests|FullyQualifiedName~HomePreviewSourceTests"
  found: Passed 9/9 tests.
  implication: Existing automated coverage does not catch the bug; the string source-contract test passes while the runtime parameter value remains wrong.

## Resolution
<!-- OVERWRITE as understanding evolves -->

root_cause: Home.razor passes the preview type string parameter as a literal (`PreviewType="preview.Type"`) instead of binding the `preview.Type` value, causing FigureShape's registry guard to reject every preview as an unknown shape type and render nothing.
fix: Not applied; diagnose-only mode. Suggested direction: bind PreviewType to the session value (for example with Razor expression syntax) and update source/render tests so they fail on literal string binding.
verification: Diagnose-only. Focused no-build tests passed 9/9, confirming the existing automated tests miss the root cause; no source fix applied.
files_changed: []
