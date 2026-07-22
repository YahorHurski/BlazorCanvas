# Phase 13: Star Shape Core - Research

**Researched:** 2026-07-22  
**Domain:** BlazorCanvas shape-definition geometry core  
**Confidence:** HIGH

## User Constraints

No phase `CONTEXT.md` exists, and the user explicitly selected "Continue without CONTEXT.md." [VERIFIED: user prompt]

Locked phase constraints: implement `Star5Shape`/`Star5Geometry` as a pure C# additive shape core; keep zero database involvement; do not register `Star5Shape` in `DefaultShapes.CreateRegistry()` during Phase 13; keep all 500 existing tests green. [VERIFIED: user prompt] [VERIFIED: .planning/ROADMAP.md:182]

Out of scope for Phase 13: catalog seed changes, toolbar changes, renderer switch changes, persistence, sync, JavaScript preview, and decision-log amendments. [VERIFIED: .planning/ROADMAP.md:214] [VERIFIED: .planning/REQUIREMENTS.md:127]

## Summary

Phase 13 should add a production `Star5Geometry` record and `Star5Shape` definition under `src/BlazorCanvas/Shapes`, plus direct unit tests under `tests/BlazorCanvas.Tests/Shapes`. [VERIFIED: codebase grep] The existing `IShapeDefinition` contract already has every hook needed: parse, canonical JSON serialization, drawability, bbox derivation, and gesture-to-placement conversion. [VERIFIED: src/BlazorCanvas/Shapes/IShapeDefinition.cs:8]

The closest implementation pattern is `TriangleShape` for production point-list storage and `PentagonShape` for point-up polar vertex generation from a corner-to-corner drag. [VERIFIED: src/BlazorCanvas/Shapes/TriangleShape.cs:69] [VERIFIED: tests/BlazorCanvas.Tests/Shapes/PentagonShape.cs:72] The star should derive ten alternating points from the dragged rectangle, starting at top-center with angle `-Math.PI / 2`, using the full dragged box as an ellipse-like scale frame: `radiusX = width / 2`, `radiusY = height / 2`, outer radius multiplier `1`, inner radius multiplier `0.382`. [VERIFIED: .planning/REQUIREMENTS.md:24] [VERIFIED: tests/BlazorCanvas.Tests/Shapes/PentagonShape.cs:80]

**Primary recommendation:** Implement `Star5Shape` as an unregistered point-list shape whose JSON canonical form is exactly `{"points":[...],"innerRatio":0.382}`, with parse requiring both `points` and finite positive `innerRatio`, while render and bbox semantics use only the ordered point list. [VERIFIED: .planning/REQUIREMENTS.md:27]

## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| SHAPE-04 | Point-up star, five outer points and five inner vertices at `0.382` outer radius, stretch to any dragged box, first vertex top-centre. | Use `PentagonShape.FromGesture` polar generation pattern, but generate 10 points with alternating radius multipliers. [VERIFIED: tests/BlazorCanvas.Tests/Shapes/PentagonShape.cs:83] |
| SHAPE-05 | Persist ordered ten-point list plus `innerRatio`; rendering and bbox derive from points alone. | Use `TriangleShape`/`LineShape` point-list storage and `BoundsOf` min/max pattern; never use `InnerRatio` inside `BoundsOf`. [VERIFIED: src/BlazorCanvas/Shapes/TriangleShape.cs:49] |
| SHAPE-06 | `TryParseGeometry`/`ToJson` byte-identical round-trip; missing `innerRatio` fails parse. | Use `GeometryJson.Serialise`, `WritePoints`, and `TryReadFiniteDouble`; write properties in stable order: `points`, then `innerRatio`. [VERIFIED: src/BlazorCanvas/Shapes/GeometryJson.cs:15] [VERIFIED: src/BlazorCanvas/Shapes/GeometryJson.cs:99] |

## Project Constraints (from AGENTS.md)

None. No `AGENTS.md` exists at the project root. [VERIFIED: codebase grep]

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|--------------|----------------|-----------|
| Star geometry derivation | API / Backend C# domain layer | Browser / Client later | `IShapeDefinition.FromGesture` owns gesture-to-local-geometry conversion in C# today; Phase 15 may add preview/render plumbing later. [VERIFIED: src/BlazorCanvas/Shapes/IShapeDefinition.cs:41] |
| Star JSON validation | API / Backend C# domain layer | Database / Storage later | `FigureInputGateway` delegates geometry parsing to registered shape definitions and reserializes canonical geometry before any write. [VERIFIED: src/BlazorCanvas/Shapes/FigureInputGateway.cs:24] |
| Star bbox derivation | API / Backend C# domain layer | Database / Storage cache later | Shape definitions compute local bbox; database `bbox_*` is only a cache in later write paths. [VERIFIED: src/BlazorCanvas/Shapes/IShapeDefinition.cs:35] [VERIFIED: docs/DECISIONS.md] |
| Shape registration and persistence | Database / Storage later | API / Backend later | Registration is explicitly deferred to Phase 14 because current cutover tests expect four seeded `figure_types`. [VERIFIED: .planning/ROADMAP.md:206] |

## Standard Stack

### Core

| Library / Runtime | Version | Purpose | Why Standard |
|-------------------|---------|---------|--------------|
| .NET SDK | 10.0.301 | Build and test the existing BlazorCanvas solution. | Project targets `net10.0` in app and test projects. [VERIFIED: dotnet --version] [VERIFIED: src/BlazorCanvas/BlazorCanvas.csproj] |
| System.Text.Json | Built into .NET | Parse `JsonElement` and emit canonical minified JSON through `Utf8JsonWriter`. | Existing `GeometryJson` helper already centralizes shape JSON parsing and serialization. [VERIFIED: src/BlazorCanvas/Shapes/GeometryJson.cs:1] |
| xUnit | 2.9.3 | Unit tests for `Star5Shape`. | Test project already uses xUnit and imports `Xunit`. [VERIFIED: tests/BlazorCanvas.Tests/BlazorCanvas.Tests.csproj] |

### Supporting

| Library / Runtime | Version | Purpose | When to Use |
|-------------------|---------|---------|-------------|
| Microsoft.NET.Test.Sdk | 17.14.1 | Runs the test project. | Required for `dotnet test`. [VERIFIED: tests/BlazorCanvas.Tests/BlazorCanvas.Tests.csproj] |
| Npgsql / EF Core | Existing only | Not needed for Phase 13 implementation. | Do not touch for this phase; database work begins in Phase 14+. [VERIFIED: .planning/ROADMAP.md:190] |

### Alternatives Considered

| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| Native C# shape definition | SVG path stored as JSON | Rejected by locked storage model; stored SVG would bypass typed validation and create injection/geometry-cache hazards. [VERIFIED: docs/DECISIONS.md] |
| Point-list geometry | Store only width/height and derive star on render | Rejected by SHAPE-05; points are authoritative and ratio mismatch must not affect screen or bbox. [VERIFIED: .planning/REQUIREMENTS.md:27] |
| Register shape immediately | Add `Star5Shape` to `DefaultShapes.CreateRegistry()` | Rejected for Phase 13; two cutover tests still assert four `figure_types`. [VERIFIED: tests/BlazorCanvas.Tests/Database/V11/V11CutoverTests.cs:58] |

**Installation:** No package installation. [VERIFIED: user prompt]

## Package Legitimacy Audit

No external packages are installed in Phase 13. [VERIFIED: user prompt]

| Package | Registry | Age | Downloads | Source Repo | Verdict | Disposition |
|---------|----------|-----|-----------|-------------|---------|-------------|
| none | - | - | - | - | - | No install |

**Packages removed due to [SLOP] verdict:** none  
**Packages flagged as suspicious [SUS]:** none

## Architecture Patterns

### System Architecture Diagram

```text
CanvasPoint press/cursor
  -> Star5Shape.FromGesture
     -> round away from zero
     -> clamp to CanvasBounds
     -> derive placement origin from min corner
     -> generate 10 ordered local points
     -> Star5Geometry(points, innerRatio)
        -> IsDrawable validates finite, count, distinct/non-zero-area shape
        -> ToJson emits canonical {points, innerRatio}
        -> TryParseGeometry requires both points and innerRatio
        -> BoundsOf scans points only
```

### Recommended Project Structure

```text
src/BlazorCanvas/Shapes/
├── Star5Geometry.cs     # sealed record with Points and InnerRatio
└── Star5Shape.cs        # IShapeDefinition implementation, not registered yet

tests/BlazorCanvas.Tests/Shapes/
└── Star5ShapeTests.cs   # direct tests for gesture, parse, JSON, bbox, drawability
```

### Pattern 1: Point-List Geometry Definition

**What:** Use a sealed geometry record with an ordered `IReadOnlyList<LocalPoint>` and compare point sequences in tests instead of relying on record equality. [VERIFIED: src/BlazorCanvas/Shapes/TriangleGeometry.cs]  
**When to use:** Use for shapes whose drawn outline is authoritative and may vary independently of a simple bbox formula. [VERIFIED: docs/DECISIONS.md]

**Example:**

```csharp
// Source: src/BlazorCanvas/Shapes/TriangleGeometry.cs
public sealed record Star5Geometry(IReadOnlyList<LocalPoint> Points, double InnerRatio) : IFigureGeometry;
```

### Pattern 2: Canonical Geometry JSON

**What:** Use `GeometryJson.Serialise` and `Utf8JsonWriter` helper methods; never hand-build JSON strings. [VERIFIED: src/BlazorCanvas/Shapes/GeometryJson.cs:99]  
**When to use:** Every `IShapeDefinition.ToJson` implementation. [VERIFIED: src/BlazorCanvas/Shapes/RectangleShape.cs:32]

**Example:**

```csharp
// Source pattern: src/BlazorCanvas/Shapes/TriangleShape.cs and GeometryJson.cs
return GeometryJson.Serialise(writer =>
{
    writer.WriteStartObject();
    GeometryJson.WritePoints(writer, "points", star.Points);
    GeometryJson.WriteNumber(writer, "innerRatio", star.InnerRatio);
    writer.WriteEndObject();
});
```

### Pattern 3: Gesture Normalization

**What:** Round browser-supplied coordinates away from zero, then clamp each point into inclusive `CanvasBounds` before shape-specific math. [VERIFIED: src/BlazorCanvas/Shapes/RectangleShape.cs:67]  
**When to use:** Every `FromGesture` implementation because circuit coordinates are untrusted. [VERIFIED: src/BlazorCanvas/Shapes/IShapeDefinition.cs:37]

### Anti-Patterns to Avoid

- **Registering `Star5Shape` in `DefaultShapes`:** breaks Phase 13's additive boundary and known catalog-count tests. [VERIFIED: .planning/ROADMAP.md:206]
- **Using `InnerRatio` inside `BoundsOf`:** violates SHAPE-05 because mismatched ratio must change nothing on screen or bbox. [VERIFIED: .planning/REQUIREMENTS.md:27]
- **Accepting missing `innerRatio`:** violates SHAPE-06 even if ten points are present. [VERIFIED: .planning/REQUIREMENTS.md:30]
- **Deriving render shape from bbox later:** demotes the persisted ordered point list and repeats the old bbox-as-truth problem. [VERIFIED: tests/BlazorCanvas.Tests/Shapes/PointListPrimacyTests.cs:35]

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| JSON parsing/formatting | String concatenation or culture-sensitive formatting | `GeometryJson.TryReadPointList`, `TryReadFiniteDouble`, `WritePoints`, `WriteNumber`, `Serialise` | Existing helpers already reject non-finite values and emit stable invariant JSON. [VERIFIED: src/BlazorCanvas/Shapes/GeometryJson.cs:29] |
| Gesture bounds handling | New coordinate clamp behavior | Copy existing `NormaliseGesturePoint` pattern | All existing shapes round and clamp the same way before shape math. [VERIFIED: src/BlazorCanvas/Shapes/LineShape.cs:74] |
| Bbox cache logic | Store bbox in star JSON or use `innerRatio` | Scan min/max over `Points` | Current point-list shapes derive bbox from points; SHAPE-05 requires points to be authoritative. [VERIFIED: src/BlazorCanvas/Shapes/TriangleShape.cs:49] |
| Registration/persistence | Seed `star5` early | Defer to Phase 14 | Phase 13 must have zero database involvement and not alter registry seed order. [VERIFIED: .planning/ROADMAP.md:206] |

**Key insight:** Star5 is a new shape definition, not a data-model or renderer phase. [VERIFIED: .planning/ROADMAP.md:190]

## Common Pitfalls

### Pitfall 1: Phase Boundary Leak

**What goes wrong:** Adding `Star5Shape` to `DefaultShapes.CreateRegistry()` makes production registry size five in Phase 13. [VERIFIED: src/BlazorCanvas/Shapes/DefaultShapes.cs:12]  
**Why it happens:** Shape-core work can feel incomplete unless registered. [ASSUMED]  
**How to avoid:** Instantiate `new Star5Shape()` directly in tests and leave `DefaultShapesTests.CreateRegistry_RegistersCanonicalNamesInSeedOrder` unchanged at four names. [VERIFIED: tests/BlazorCanvas.Tests/Shapes/DefaultShapesTests.cs:9]  
**Warning signs:** `V11CutoverTests` catalog assertions move from 4 to 5 during this phase. [VERIFIED: tests/BlazorCanvas.Tests/Database/V11/V11CutoverTests.cs:58]

### Pitfall 2: Ratio Becomes Source of Truth

**What goes wrong:** `BoundsOf` recomputes the theoretical star using `InnerRatio`, so edited or mismatched point lists render/clamp differently. [VERIFIED: .planning/REQUIREMENTS.md:27]  
**Why it happens:** `innerRatio` looks like enough information to regenerate the star. [ASSUMED]  
**How to avoid:** `BoundsOf` should only compute `minX`, `minY`, `maxX`, `maxY` from `Star5Geometry.Points`. [VERIFIED: src/BlazorCanvas/Shapes/TriangleShape.cs:49]  
**Warning signs:** A test mutating only `InnerRatio` changes `BoundsOf` or serialized `points`. [VERIFIED: .planning/REQUIREMENTS.md:27]

### Pitfall 3: Canonical JSON Order Drift

**What goes wrong:** `ToJson` emits `innerRatio` before `points`, indented JSON, or extra fields, breaking byte-identical round-trip expectations. [VERIFIED: .planning/REQUIREMENTS.md:30]  
**Why it happens:** `System.Text.Json` can serialize records in property order that may not match the required wire shape. [ASSUMED]  
**How to avoid:** Use `GeometryJson.Serialise` manually and assert exact string output. [VERIFIED: src/BlazorCanvas/Shapes/GeometryJson.cs:99]  
**Warning signs:** Tests compare only semantic parse results and not the exact JSON string. [VERIFIED: tests/BlazorCanvas.Tests/Shapes/RectangleShapeTests.cs:39]

### Pitfall 4: Degenerate Star Accepted

**What goes wrong:** A zero-width or zero-height drag produces duplicate or collinear points and passes `IsDrawable`. [VERIFIED: .planning/ROADMAP.md:201]  
**Why it happens:** `TryReadPointList` validates count and finiteness, not geometric area. [VERIFIED: src/BlazorCanvas/Shapes/GeometryJson.cs:29]  
**How to avoid:** Use point count, finite checks, distinct point count, and polygon area/non-zero extent checks in `IsDrawable`. [VERIFIED: tests/BlazorCanvas.Tests/Shapes/PentagonShape.cs:40]  
**Warning signs:** `TryValidateGesture("star5", same press/cursor, ...)` succeeds after Phase 14 registration. [VERIFIED: src/BlazorCanvas/Shapes/FigureInputGateway.cs:76]

## Code Examples

### Ten-Point Star Gesture Formula

```csharp
// Source pattern: tests/BlazorCanvas.Tests/Shapes/PentagonShape.cs
var radiusX = width / 2;
var radiusY = height / 2;
var points = Enumerable.Range(0, 10)
    .Select(index =>
    {
        var theta = (-Math.PI / 2) + (index * (Math.PI / 5));
        var scale = index % 2 == 0 ? 1.0 : Star5Shape.DefaultInnerRatio;
        return new LocalPoint(
            radiusX + (radiusX * scale * Math.Cos(theta)),
            radiusY + (radiusY * scale * Math.Sin(theta)));
    })
    .ToArray();
```

### Parse Requires Points and Inner Ratio

```csharp
// Source pattern: src/BlazorCanvas/Shapes/TriangleShape.cs and GeometryJson.cs
if (json.ValueKind != JsonValueKind.Object
    || !GeometryJson.TryReadPointList(json, "points", 10, out var points)
    || !GeometryJson.TryReadFiniteDouble(json, "innerRatio", out var innerRatio)
    || innerRatio <= 0)
{
    geometry = null!;
    return false;
}
```

### Bounds from Points Only

```csharp
// Source pattern: src/BlazorCanvas/Shapes/TriangleShape.cs
var minX = star.Points.Min(point => point.X);
var minY = star.Points.Min(point => point.Y);
var maxX = star.Points.Max(point => point.X);
var maxY = star.Points.Max(point => point.Y);
return new Bbox(minX, minY, maxX - minX, maxY - minY);
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Four columns as primary geometry | `geometry jsonb` with per-type typed records | v1.11, D-59/D-60 | New shape types need C# definitions and seeded catalog rows, not schema changes. [VERIFIED: docs/DECISIONS.md] |
| Bbox as source of truth | `bbox_*` as app-computed local cache | v1.11, D-67 | Phase 13 must implement `BoundsOf`; later persistence writes cache from that. [VERIFIED: docs/DECISIONS.md] |
| SQL CHECK mirrors per shape | C# validation gateway | v1.11, D-60/D-66 | `TryParseGeometry` and `IsDrawable` are the trust boundary for star geometry. [VERIFIED: src/BlazorCanvas/Shapes/FigureInputGateway.cs:24] |

**Deprecated/outdated:**

- `D-22` four-integer bounding-box storage is superseded; do not model star geometry as four columns. [VERIFIED: docs/DECISIONS.md]
- `D-50` C#/SQL geometry CHECK mirroring is superseded; do not add database validation for `star5` in Phase 13. [VERIFIED: docs/DECISIONS.md]

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | Developers may feel tempted to register the shape to make core work feel complete. | Common Pitfalls | Low; roadmap explicitly forbids registration in Phase 13. |
| A2 | `innerRatio` may look sufficient to regenerate the star. | Common Pitfalls | Medium; using it outside parse/serialize would violate SHAPE-05. |
| A3 | Record serialization order may not match required canonical geometry. | Common Pitfalls | Low; planner can avoid the risk by using existing manual writer helpers. |

## Open Questions

1. **Should `InnerRatio` accept any positive finite value on parse, or exactly `0.382`?**
   - What we know: SHAPE-05 says mismatched ratio changes nothing on screen, implying parse can preserve a mismatched ratio as descriptive metadata. [VERIFIED: .planning/REQUIREMENTS.md:27]
   - What's unclear: Requirements only state missing `innerRatio` fails parse; they do not explicitly say non-`0.382` ratios fail. [VERIFIED: .planning/REQUIREMENTS.md:30]
   - Recommendation: Accept any finite positive `innerRatio`, serialize it back byte-stably, and ensure `BoundsOf`/render-relevant tests ignore it. [ASSUMED]

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|-------------|-----------|---------|----------|
| .NET SDK | Build and tests | yes | 10.0.301 | none needed. [VERIFIED: dotnet --version] |
| xUnit test runner | Shape unit tests | yes via project package | xunit 2.9.3, runner 3.1.4 | none needed. [VERIFIED: tests/BlazorCanvas.Tests/BlazorCanvas.Tests.csproj] |
| PostgreSQL / Docker | Not required by Phase 13 | not probed | - | Shape tests should avoid database collections. [VERIFIED: .planning/ROADMAP.md:190] |
| Running app process | Test/build verification | present and locking apphost | PID 17748 | Stop process before full `dotnet test`, or run after app exits. [VERIFIED: Get-Process BlazorCanvas] |

**Missing dependencies with no fallback:** none identified for Phase 13. [VERIFIED: codebase grep]

**Missing dependencies with fallback:** none identified for Phase 13. [VERIFIED: codebase grep]

**Verification caveat:** `dotnet test BlazorCanvas.sln --nologo --no-restore` failed during research because `D:\Project1\src\BlazorCanvas\bin\Debug\net10.0\BlazorCanvas.exe` was locked by running process `BlazorCanvas (17748)`. [VERIFIED: dotnet test output]

## Security Domain

Security enforcement is enabled by default because `.planning/config.json` does not set `security_enforcement: false`. [VERIFIED: .planning/config.json]

### Applicable ASVS Categories

| ASVS Category | Applies | Standard Control |
|---------------|---------|------------------|
| V2 Authentication | no | Phase 13 does not touch login/session code. [VERIFIED: .planning/ROADMAP.md:190] |
| V3 Session Management | no | Phase 13 has no cookie/session changes. [VERIFIED: .planning/ROADMAP.md:190] |
| V4 Access Control | no | Phase 13 does not add endpoints or database writes. [VERIFIED: .planning/ROADMAP.md:190] |
| V5 Input Validation | yes | `TryParseGeometry` rejects malformed JSON shapes; `FigureInputGateway` remains the shared trust boundary after registration. [VERIFIED: src/BlazorCanvas/Shapes/FigureInputGateway.cs:24] |
| V6 Cryptography | no | No crypto added; plaintext-password decision is unrelated and locked. [VERIFIED: docs/DECISIONS.md] |

### Known Threat Patterns for Shape JSON

| Pattern | STRIDE | Standard Mitigation |
|---------|--------|---------------------|
| Non-finite numeric values | Tampering | Use `GeometryJson.TryReadPointList` and `TryReadFiniteDouble`. [VERIFIED: src/BlazorCanvas/Shapes/GeometryJson.cs:15] |
| Hostile extra JSON content | Tampering | Parse typed geometry and reserialize with `ToJson`; unknown fields are dropped. [VERIFIED: tests/BlazorCanvas.Tests/Shapes/FigureInputGatewayTests.cs:63] |
| Malformed or deeply nested JSON | Denial of Service | Existing gateway `JsonDocumentOptions` caps depth and catches `JsonException`; direct shape tests should cover shape-level malformed objects. [VERIFIED: src/BlazorCanvas/Shapes/FigureInputGateway.cs:11] |

## Sources

### Primary (HIGH confidence)

- `.planning/ROADMAP.md` - Phase 13 scope, success criteria, no-registration boundary, Phase 14 deferral. [VERIFIED: codebase grep]
- `.planning/REQUIREMENTS.md` - SHAPE-04, SHAPE-05, SHAPE-06 exact behavior. [VERIFIED: codebase grep]
- `docs/DECISIONS.md` - current v1.11 storage model, point-list geometry, bbox cache, validation gateway. [VERIFIED: codebase grep]
- `src/BlazorCanvas/Shapes/IShapeDefinition.cs` - shape contract. [VERIFIED: codebase grep]
- `src/BlazorCanvas/Shapes/GeometryJson.cs` - JSON helpers. [VERIFIED: codebase grep]
- Existing shape implementations and tests - local implementation patterns. [VERIFIED: codebase grep]

### Secondary (MEDIUM confidence)

- None needed; this is a codebase-only implementation phase. [VERIFIED: codebase grep]

### Tertiary (LOW confidence)

- GSD research-plan websearch for project-specific terms returned no authoritative public source and is not used for implementation guidance. [VERIFIED: websearch]

## Metadata

**Confidence breakdown:**

- Standard stack: HIGH - versions and tooling are read from project files and local `dotnet --version`. [VERIFIED: dotnet --version]
- Architecture: HIGH - shape contract and phase boundary are directly visible in code and roadmap. [VERIFIED: src/BlazorCanvas/Shapes/IShapeDefinition.cs:8]
- Pitfalls: HIGH for phase-boundary and JSON/bbox risks from tests/docs; MEDIUM for human-error motivations marked as assumptions. [VERIFIED: .planning/ROADMAP.md:206]

**Research date:** 2026-07-22  
**Valid until:** 2026-08-21, or until Phase 14 changes `DefaultShapes.CreateRegistry()`.
