---
phase: BC-14-catalog-seed-toolbar-decisions
reviewed: 2026-07-22T20:24:30Z
depth: standard
files_reviewed: 12
files_reviewed_list:
  - src/BlazorCanvas/Shapes/DefaultShapes.cs
  - src/BlazorCanvas/Data/V11/Transition/V11Schema.cs
  - src/BlazorCanvas/Data/V11/V11Cutover.cs
  - tests/BlazorCanvas.Tests/Shapes/DefaultShapesTests.cs
  - tests/BlazorCanvas.Tests/Shapes/Star5ShapeTests.cs
  - tests/BlazorCanvas.Tests/Shapes/ShapeRegistryExtensibilityTests.cs
  - tests/BlazorCanvas.Tests/Database/V11/V11CutoverTests.cs
  - src/BlazorCanvas/Tools/Tool.cs
  - src/BlazorCanvas/Components/Canvas/Toolbar.razor
  - tests/BlazorCanvas.Tests/Tools/ToolMapTests.cs
  - tests/BlazorCanvas.Tests/Components/ToolbarSourceTests.cs
  - docs/DECISIONS.md
findings:
  critical: 1
  warning: 0
  info: 0
  total: 1
status: issues_found
---

# Phase 14: Code Review Report

**Reviewed:** 2026-07-22T20:24:30Z
**Depth:** standard
**Files Reviewed:** 12
**Status:** issues_found

## Summary

Reviewed the Phase 14 catalog seed, v1.11 cutover, toolbar/tool mapping, tests, and decision-log updates. The catalog and toolbar now expose `star5`, but the existing render and selection components do not handle `Star5Geometry`, so the new user-facing tool can create persisted figures that are invisible and cannot be selected from the canvas.

`dotnet test` passed with 530 tests, which confirms the current test suite does not cover the missing renderer path.

## Narrative Findings (AI reviewer)

## Critical Issues

### CR-01: [BLOCKER] Star tool creates persisted figures that do not render

**File:** `src/BlazorCanvas/Components/Canvas/Toolbar.razor:39`

**Issue:** The toolbar exposes `Tool.Star`, and `src/BlazorCanvas/Tools/Tool.cs:40` maps it to the registry-owned `star5` type seeded by `src/BlazorCanvas/Shapes/DefaultShapes.cs:19`. That makes Star a real drawing tool, but the render path still only switches over `LineGeometry`, `RectangleGeometry`, `CircleGeometry`, and `TriangleGeometry` in `src/BlazorCanvas/Components/Canvas/FigureShape.razor:8-22`. `SelectionTrace.razor:8-26` has the same missing case. A user can draw a star, the row can be inserted as `star5`, and then preview/render/selection output no SVG for that geometry. The result is a persisted invisible figure that is not selectable by clicking it.

**Fix:** Add `Star5Geometry` cases anywhere geometry is rendered or traced, and add a component-level test that renders a `star5` figure instead of only asserting toolbar source text.

```razor
case Star5Geometry star:
    <polygon points="@Points(star.Points)"
             fill="@Style.Fill"
             stroke="@Style.Stroke"
             stroke-width="@Number(Style.StrokeWidth)"
             fill-opacity="@Opacity"
             stroke-opacity="@Opacity"
             @onpointerdown="HandlePointerDown"
             @onpointerdown:stopPropagation="Selectable" />
    break;
```

For `SelectionTrace.razor`, render the same `polygon` twice with the existing white/blue dashed trace styling used for triangles.

---

_Reviewed: 2026-07-22T20:24:30Z_
_Reviewer: the agent (gsd-code-reviewer)_
_Depth: standard_
