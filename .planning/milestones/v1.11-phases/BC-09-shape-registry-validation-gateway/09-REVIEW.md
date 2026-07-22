---
phase: BC-09-shape-registry-validation-gateway
reviewed: 2026-07-22T00:00:00Z
depth: standard
files_reviewed: 37
files_reviewed_list:
  - src/BlazorCanvas/Shapes/Bbox.cs
  - src/BlazorCanvas/Shapes/CanvasPoint.cs
  - src/BlazorCanvas/Shapes/CircleGeometry.cs
  - src/BlazorCanvas/Shapes/CircleShape.cs
  - src/BlazorCanvas/Shapes/DefaultShapes.cs
  - src/BlazorCanvas/Shapes/FigureInputGateway.cs
  - src/BlazorCanvas/Shapes/FigureStyle.cs
  - src/BlazorCanvas/Shapes/GeometryJson.cs
  - src/BlazorCanvas/Shapes/IFigureGeometry.cs
  - src/BlazorCanvas/Shapes/IShapeDefinition.cs
  - src/BlazorCanvas/Shapes/LineGeometry.cs
  - src/BlazorCanvas/Shapes/LineShape.cs
  - src/BlazorCanvas/Shapes/LocalPoint.cs
  - src/BlazorCanvas/Shapes/RectangleGeometry.cs
  - src/BlazorCanvas/Shapes/RectangleShape.cs
  - src/BlazorCanvas/Shapes/ShapePlacement.cs
  - src/BlazorCanvas/Shapes/ShapeRegistry.cs
  - src/BlazorCanvas/Shapes/StyleGateway.cs
  - src/BlazorCanvas/Shapes/TriangleGeometry.cs
  - src/BlazorCanvas/Shapes/TriangleShape.cs
  - src/BlazorCanvas/Shapes/ValidatedFigureInput.cs
  - tests/BlazorCanvas.Tests/BlazorCanvas.Tests.csproj
  - tests/BlazorCanvas.Tests/Fixtures/v1.1-pre-rewrite-MANIFEST.md
  - tests/BlazorCanvas.Tests/Fixtures/v1.1-pre-rewrite.sql
  - tests/BlazorCanvas.Tests/Shapes/CircleShapeTests.cs
  - tests/BlazorCanvas.Tests/Shapes/DefaultShapesTests.cs
  - tests/BlazorCanvas.Tests/Shapes/FigureInputGatewayTests.cs
  - tests/BlazorCanvas.Tests/Shapes/GeometryModelTests.cs
  - tests/BlazorCanvas.Tests/Shapes/LineShapeTests.cs
  - tests/BlazorCanvas.Tests/Shapes/PentagonShape.cs
  - tests/BlazorCanvas.Tests/Shapes/PointListPrimacyTests.cs
  - tests/BlazorCanvas.Tests/Shapes/RectangleShapeTests.cs
  - tests/BlazorCanvas.Tests/Shapes/ShapeRegistryExtensibilityTests.cs
  - tests/BlazorCanvas.Tests/Shapes/ShapeRegistryTests.cs
  - tests/BlazorCanvas.Tests/Shapes/StyleGatewayTests.cs
  - tests/BlazorCanvas.Tests/Shapes/TriangleShapeTests.cs
  - tests/BlazorCanvas.Tests/Shapes/V11GestureEquivalenceTests.cs
findings:
  critical: 0
  warning: 3
  info: 0
  total: 3
status: issues_found
---

# Phase BC-09: Code Review Report

**Reviewed:** 2026-07-22T00:00:00Z
**Depth:** standard
**Files Reviewed:** 37
**Status:** issues_found

## Summary

Reviewed the complete submitted shape-model, validation-gateway, fixture, and test scope against all six BC-09 plans and summaries. The fixture checksum matches its manifest and the full suite passes (1,032/1,032), but three robustness/coverage defects remain. In particular, the supposed direction-preservation test does not test direction, and two public APIs let later code bypass or corrupt the registry/gateway invariants.

## Narrative Findings (AI reviewer)

## Warnings

### WR-01: The v1.1 line-equivalence test discards endpoint order

**File:** `tests/BlazorCanvas.Tests/Shapes/V11GestureEquivalenceTests.cs:71-82`
**Issue:** The test claims to prove direction preservation but sorts both endpoint sequences before comparing them. Reversing a line's two stored points therefore passes this test exactly, despite violating the BC-09 requirement that line point order carries the original diagonal direction. This makes a future canonicalising/swap regression invisible.
**Fix:** Compare the two absolute endpoint sequences in their emitted order. Remove both `OrderBy(...).ThenBy(...)` chains and assert directly against `[ (X1,Y1), (X2,Y2) ]`; add an explicitly reversed-drag case if the legacy contract needs a separate proof.

### WR-02: `ValidatedFigureInput` can be constructed with unvalidated client values

**File:** `src/BlazorCanvas/Shapes/ValidatedFigureInput.cs:9-15`
**Issue:** The type is public with a compiler-generated public constructor, although its contract says its JSON fields are the only persistence-safe values and that construction outside `FigureInputGateway` defeats the trust boundary. Any later persistence/sync code can instantiate it with attacker-controlled type, JSON, style, or bounds and then appear to use the approved gateway result type. This is a bypass risk for the stated single validation path.
**Fix:** Make construction inaccessible outside the assembly/boundary (for example, replace the public positional record constructor with an `internal` constructor or an `internal` static factory used only by `FigureInputGateway`) and expose read-only public properties. Add a test or API-surface assertion that external callers cannot create an arbitrary result.

### WR-03: Registry read-only views expose mutable backing lists

**File:** `src/BlazorCanvas/Shapes/ShapeRegistry.cs:15-20`
**Issue:** `All` and `Names` are typed as `IReadOnlyList`, but each returns the actual `List` instance. Consumers can downcast and mutate either list, producing a registry whose lookup dictionary, names, and definitions disagree. This can make Phase 10 seed order or registry dispatch inconsistent without going through `Register`.
**Fix:** Keep private read-only wrappers (for example `_definitions.AsReadOnly()` and `_names.AsReadOnly()`) and return those, or return immutable/snapshot collections. Add a regression test showing callers cannot alter a returned view and that registration remains the sole mutation path.

---

_Reviewed: 2026-07-22T00:00:00Z_
_Reviewer: the agent (gsd-code-reviewer; generic-agent workaround)_
_Depth: standard_
