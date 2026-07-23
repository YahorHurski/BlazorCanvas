# Phase 14: Catalog Seed, Toolbar & Decisions - Research

**Researched:** 2026-07-22  
**Domain:** BlazorCanvas shape registry exposure, PostgreSQL catalog seeding, toolbar mapping, and decision-doc amendments  
**Confidence:** HIGH

## User Constraints

No phase `CONTEXT.md` exists; the user explicitly chose to continue without context and research first. [VERIFIED: user prompt]

Locked phase scope: `Star5Shape` joins `DefaultShapes.CreateRegistry()`, `figure_types` seeds `star5` idempotently on every startup even when the database is already in `CatalogState.Completed`, the toolbar gains a seventh button between triangle and delete, and decision/project/intel documents are amended. [VERIFIED: user prompt] [VERIFIED: .planning/ROADMAP.md]

Phase requirement IDs that must be planned: `MODEL-08`, `CANV-04`, `ARCH-02`. [VERIFIED: user prompt] [VERIFIED: .planning/REQUIREMENTS.md]

Out of scope for Phase 14: drawing a star, live preview, renderer branch, persistence write path, selection/drag/delete behavior for persisted stars, cross-tab star sync, drift-guard test, bbox-agreement test, and degenerate/malformed gateway tests beyond the registration surface. [VERIFIED: .planning/ROADMAP.md] [VERIFIED: .planning/REQUIREMENTS.md]

## Summary

Phase 14 should be a small exposure-and-seed phase. The core star shape already exists from Phase 13 and is deliberately unregistered; Phase 14 should register it in canonical seed order after triangle, update registry tests and the two v11 cutover catalog-count assertions from four to five, and remove the Phase 13 no-registration fence. [VERIFIED: src/BlazorCanvas/Shapes/Star5Shape.cs] [VERIFIED: src/BlazorCanvas/Shapes/DefaultShapes.cs] [VERIFIED: tests/BlazorCanvas.Tests/Database/V11/V11CutoverTests.cs]

The highest-risk implementation detail is the completed-catalog early return. `V11Cutover.EnsureAsync` currently rolls back and returns when public `canvases`, `figure_types`, and `figures` already exist, so `SeedFigureTypesAsync` never runs again on the live upgraded database. `V11Schema.SeedFigureTypesAsync` also hard-codes `v11.figure_types`, so the planner should add a public-schema seed path rather than treating this as an EF migration or manual SQL task. [VERIFIED: src/BlazorCanvas/Data/V11/V11Cutover.cs] [VERIFIED: src/BlazorCanvas/Data/V11/Transition/V11Schema.cs]

The toolbar change is local: add `Tool.Star`, map it to `"star5"`, insert a star icon button between triangle and delete, and preserve the existing `48px` strip, logout form, delete action semantics, and exclusive arm state. No package install is needed; existing inline SVG button patterns are sufficient. [VERIFIED: src/BlazorCanvas/Tools/Tool.cs] [VERIFIED: src/BlazorCanvas/Components/Canvas/Toolbar.razor] [VERIFIED: src/BlazorCanvas/Components/Canvas/Toolbar.razor.css]

**Primary recommendation:** Implement Phase 14 as three tightly scoped slices: registry + tests, reusable schema-qualified idempotent catalog seed called for completed public catalogs, and toolbar/docs amendments.

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|--------------|----------------|-----------|
| Shape registration | API / Backend C# domain layer | Database / Storage | `DefaultShapes.CreateRegistry()` is the composition root used by DI and v11 seed/migration paths. [VERIFIED: src/BlazorCanvas/Program.cs] |
| Catalog seed | Database / Storage | API / Backend | `figure_types` is a database FK target, but the registry is the source of seeded names. [VERIFIED: docs/DECISIONS.md] [VERIFIED: src/BlazorCanvas/Data/V11/Transition/V11Schema.cs] |
| Toolbar arming | Browser / Client component | API / Backend mapping | `Toolbar.razor` exposes the UI buttons; `ToolMap` converts armable tools to registry names used by the drawing pipeline. [VERIFIED: src/BlazorCanvas/Tools/Tool.cs] |
| Decision amendments | Documentation / Planning | - | ARCH-02 is documentation-only but locked; planner must amend source-of-truth and derived intel files. [VERIFIED: .planning/REQUIREMENTS.md] |

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| MODEL-08 | `figure_types` catalog is seeded from the registry at every startup, including existing completed databases, with no manual SQL and no migration. | Move seeding into the completed-catalog path and support `public.figure_types` as the target; keep `ON CONFLICT (name) DO NOTHING` idempotency. [VERIFIED: src/BlazorCanvas/Data/V11/V11Cutover.cs] |
| CANV-04 | Toolbar presents `[pointer] [line] [rectangle] [circle] [triangle] [star] [delete]`, with star between triangle and delete and normal exclusive arming. | Add `Tool.Star`, update `ToolMap.ToShapeName`, and insert one `Toolbar.razor` button before the delete action. [VERIFIED: src/BlazorCanvas/Tools/Tool.cs] [VERIFIED: src/BlazorCanvas/Components/Canvas/Toolbar.razor] |
| ARCH-02 | Decision docs record seven-button toolbar amendments and star decisions from D-70 onward. | Amend `docs/DECISIONS.md`, `.planning/PROJECT.md`, `.planning/intel/decisions.md`, and `.planning/intel/requirements.md` where six-button/CANV-02 text is still authoritative. [VERIFIED: docs/DECISIONS.md] [VERIFIED: .planning/PROJECT.md] |
</phase_requirements>

## Project Constraints (from AGENTS.md)

None. No `AGENTS.md`, `CLAUDE.md`, `.claude/CLAUDE.md`, `.claude/skills/`, or `.agents/skills/` exists in the project root. [VERIFIED: shell]

## Standard Stack

### Core

| Library / Runtime | Version | Purpose | Why Standard |
|-------------------|---------|---------|--------------|
| .NET SDK | 10.0.301 | Build and test BlazorCanvas. | Project targets `net10.0`. [VERIFIED: dotnet --version] [VERIFIED: src/BlazorCanvas/BlazorCanvas.csproj] |
| Blazor Server / Razor Components | .NET 10 built-in | Toolbar component and app UI. | Existing app is a single Blazor Web App with InteractiveServer canvas. [VERIFIED: src/BlazorCanvas/Program.cs] |
| Npgsql / EF Core provider | Npgsql.EntityFrameworkCore.PostgreSQL 10.0.3 | Startup database connection and PostgreSQL tests. | Existing v11 cutover uses `NpgsqlDataSource` and raw Npgsql commands. [VERIFIED: src/BlazorCanvas/BlazorCanvas.csproj] |
| xUnit | 2.9.3 | Unit and database regression tests. | Existing test project uses xUnit; no component-test dependency is installed. [VERIFIED: tests/BlazorCanvas.Tests/BlazorCanvas.Tests.csproj] |

### Supporting

| Library / Runtime | Version | Purpose | When to Use |
|-------------------|---------|---------|-------------|
| Docker | 29.1.3 | Runs local PostgreSQL for database test collection. | Required for `V11CutoverTests` and any completed-catalog seed proof. [VERIFIED: docker --version] [VERIFIED: tests/BlazorCanvas.Tests/Database/DatabaseFixture.cs] |
| PostgreSQL CLI `psql` | not on PATH | Optional manual inspection only. | Do not plan manual `psql` verification as required; use Npgsql/xUnit instead. [VERIFIED: shell] |

### Alternatives Considered

| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| Registry-driven seed | Manual SQL insert into live DB | Rejected by MODEL-08; startup must make `star5` writable with no manual SQL. [VERIFIED: .planning/REQUIREMENTS.md] |
| Idempotent startup seed | EF migration adding one `star5` row | Rejected by phase scope; D-65 made figure types data, not schema, and success criteria say no migration. [VERIFIED: docs/DECISIONS.md] |
| Inline SVG star icon | Install an icon/component package | Unnecessary package risk; toolbar already uses inline SVG icon buttons and this phase installs no packages. [VERIFIED: src/BlazorCanvas/Components/Canvas/Toolbar.razor] |
| Full dynamic toolbar | Generate buttons from registry | Out of scope; v1.2 owns dynamic split-button flyout toolbar. [VERIFIED: .planning/REQUIREMENTS.md] |

**Installation:** No package installation. [VERIFIED: project files]

## Package Legitimacy Audit

No external packages are installed in Phase 14. [VERIFIED: project files]

| Package | Registry | Age | Downloads | Source Repo | Verdict | Disposition |
|---------|----------|-----|-----------|-------------|---------|-------------|
| none | - | - | - | - | - | No install |

**Packages removed due to [SLOP] verdict:** none  
**Packages flagged as suspicious [SUS]:** none

## Architecture Patterns

### System Architecture Diagram

```text
Program startup
  -> DI singleton ShapeRegistry = DefaultShapes.CreateRegistry()
       -> line, rectangle, circle, triangle, star5
  -> V11Cutover.EnsureAsync(dataSource, registry)
       -> GetStateAsync()
          -> Completed public catalog
             -> seed public.figure_types from registry with ON CONFLICT DO NOTHING
             -> commit
          -> Legacy/Additive/Fresh states
             -> apply v11 schema
             -> seed v11.figure_types from registry with ON CONFLICT DO NOTHING
             -> migrate/promote
             -> commit

Toolbar render
  -> Tool enum values
  -> button click sets ArmedChanged(Tool.X)
  -> ToolMap.ToShapeName(Tool.Star) returns "star5"
  -> later draw phases use registry/gateway for star geometry
```

### Recommended Project Structure

```text
src/BlazorCanvas/
├── Shapes/DefaultShapes.cs                  # Add Star5Shape registration after TriangleShape
├── Data/V11/V11Cutover.cs                   # Seed completed public catalogs before returning
├── Data/V11/Transition/V11Schema.cs         # Generalize figure_types seeding target
├── Tools/Tool.cs                            # Add Tool.Star and "star5" mapping
└── Components/Canvas/Toolbar.razor          # Insert star button between triangle and delete

tests/BlazorCanvas.Tests/
├── Shapes/DefaultShapesTests.cs             # Registry order now includes star5
├── Shapes/Star5ShapeTests.cs                # Remove/update Phase 13 no-registration fence
└── Database/V11/V11CutoverTests.cs          # Counts 4->5 and completed/idempotent seed proof
```

### Pattern 1: Registry as Catalog Source

**What:** `DefaultShapes.CreateRegistry()` returns a fresh registry in seed/display order; `V11Schema.SeedFigureTypesAsync` iterates `registry.Names`. [VERIFIED: src/BlazorCanvas/Shapes/DefaultShapes.cs] [VERIFIED: src/BlazorCanvas/Data/V11/Transition/V11Schema.cs]  
**When to use:** Any time production persistence needs to know which shape names are writable.  
**Example:**

```csharp
// Source pattern: src/BlazorCanvas/Shapes/DefaultShapes.cs
registry.Register(new TriangleShape());
registry.Register(new Star5Shape());
```

### Pattern 2: Parameterized Idempotent Seed

**What:** Insert one registry name at a time using an `NpgsqlParameter` and `ON CONFLICT (name) DO NOTHING`. [VERIFIED: src/BlazorCanvas/Data/V11/Transition/V11Schema.cs]  
**When to use:** Use for both temporary `v11.figure_types` during cutover and existing `public.figure_types` after cutover completion.  
**Example:**

```csharp
// Source pattern: src/BlazorCanvas/Data/V11/Transition/V11Schema.cs
await using var command = new NpgsqlCommand(
    "INSERT INTO public.figure_types (name) VALUES (@name) ON CONFLICT (name) DO NOTHING",
    connection,
    transaction);
command.Parameters.AddWithValue("name", name);
await command.ExecuteNonQueryAsync(ct);
```

### Pattern 3: Armable Tool Maps to Shape Name

**What:** `Tool` represents only armable modes; delete is an action button, not a `Tool` enum member. [VERIFIED: src/BlazorCanvas/Tools/Tool.cs]  
**When to use:** Add `Tool.Star` as an armable drawing mode and map it to the exact registered shape name `"star5"`. Keep delete outside the enum.  
**Example:**

```csharp
// Source pattern: src/BlazorCanvas/Tools/Tool.cs
Tool.Star => "star5",
```

### Anti-Patterns to Avoid

- **Leaving completed catalogs as no-op:** this preserves the exact MODEL-08 bug because the live database already has public v11 tables. [VERIFIED: src/BlazorCanvas/Data/V11/V11Cutover.cs]
- **Seeding only `v11.figure_types`:** completed public catalogs no longer have schema `v11`, so the helper must target public too. [VERIFIED: src/BlazorCanvas/Data/V11/Transition/V11Schema.cs]
- **Adding `star5` to docs but not the registry seed path:** the FK remains unsatisfied and Phase 15 cannot insert a star. [VERIFIED: docs/DECISIONS.md] [VERIFIED: .planning/REQUIREMENTS.md]
- **Adding Delete to `Tool`:** violates the existing action-vs-mode split and makes delete armable. [VERIFIED: src/BlazorCanvas/Tools/Tool.cs]
- **Changing toolbar height or logout layout while inserting star:** violates CANV-04 and D-43/D-56. [VERIFIED: src/BlazorCanvas/Components/Canvas/Toolbar.razor.css]

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Figure type discovery | A second hard-coded seed list | `DefaultShapes.CreateRegistry().Names` | Avoids registry/catalog drift; this is the v1.11 extensibility contract. [VERIFIED: .planning/milestones/v1.11-phases/BC-10-storage-schema-migration-persistence-layer/10-01-SUMMARY.md] |
| Catalog upsert | String-concatenated SQL or manual SQL script | Parameterized `INSERT ... ON CONFLICT DO NOTHING` | Existing seed handles idempotency and injection risk. [VERIFIED: src/BlazorCanvas/Data/V11/Transition/V11Schema.cs] |
| Toolbar state model | A parallel shape-name string state in the UI | `Tool` enum + `ToolMap.ToShapeName` | Current drawing path maps armable modes to exact registry names. [VERIFIED: src/BlazorCanvas/Tools/Tool.cs] |
| Component testing dependency | New bUnit install just for one markup change | Source-level/assertion tests plus focused unit tests | No component test stack exists and package installation is unnecessary. [VERIFIED: tests/BlazorCanvas.Tests/BlazorCanvas.Tests.csproj] |

**Key insight:** The phase is about making an already-built shape visible to existing composition points; it is not the phase to build star drawing/rendering behavior. [VERIFIED: .planning/ROADMAP.md]

## Common Pitfalls

### Pitfall 1: Completed State Still Returns Early

**What goes wrong:** The app starts successfully but `star5` is absent from `public.figure_types`, so Phase 15 inserts fail with the `figures.type` FK. [VERIFIED: .planning/PROJECT.md]  
**Why it happens:** `CatalogState.Completed` currently rolls back and returns before any seed. [VERIFIED: src/BlazorCanvas/Data/V11/V11Cutover.cs]  
**How to avoid:** In the completed path, seed `public.figure_types` inside the startup transaction and commit.  
**Warning signs:** Existing `CompletedPublicCatalog_NormalEnsureIsAnExactNoOp` remains unchanged and no test deletes or omits `star5` before rerunning `EnsureAsync`. [VERIFIED: tests/BlazorCanvas.Tests/Database/V11/V11CutoverTests.cs]

### Pitfall 2: Exact No-Op Test Conflicts with MODEL-08

**What goes wrong:** Keeping snapshot equality on completed catalogs prevents any startup seed mutation. [VERIFIED: tests/BlazorCanvas.Tests/Database/V11/V11CutoverTests.cs]  
**Why it happens:** v1.11 wanted completed cutover to be a no-op; v1.12 now explicitly amends that for registry catalog rows. [VERIFIED: .planning/REQUIREMENTS.md]  
**How to avoid:** Replace the exact no-op assertion with a narrower idempotency assertion: missing registry rows are inserted; existing rows and table structure remain unchanged; a second startup leaves one `star5` row.  
**Warning signs:** Test name or assertion still says "ExactNoOp".

### Pitfall 3: Phase 13 Registry Fence Left Behind

**What goes wrong:** `Star5ShapeTests.DefaultRegistry_DoesNotContainStar5DuringPhase13` fails after correct registration. [VERIFIED: tests/BlazorCanvas.Tests/Shapes/Star5ShapeTests.cs]  
**Why it happens:** The Phase 13 fence was intentionally temporary. [VERIFIED: .planning/phases/BC-13-star-shape-core/13-01-SUMMARY.md]  
**How to avoid:** Remove or invert that test during the same plan that updates `DefaultShapesTests`.

### Pitfall 4: Toolbar Click Bubbles to Deselect

**What goes wrong:** Clicking the star button arms the star but also triggers toolbar deselection unexpectedly. [ASSUMED]  
**Why it happens:** The toolbar root has `@onclick="() => OnDeselect.InvokeAsync()"`; existing shape buttons do not stop propagation. [VERIFIED: src/BlazorCanvas/Components/Canvas/Toolbar.razor]  
**How to avoid:** Follow the existing shape-button pattern exactly so star behaves like the other armable tools; do not invent special propagation behavior for star.  
**Warning signs:** Star button includes different event modifiers from line/rectangle/circle/triangle.

### Pitfall 5: Decision Mirrors Drift

**What goes wrong:** `docs/DECISIONS.md` says seven buttons but `.planning/PROJECT.md` or `.planning/intel/requirements.md` still says exactly six. [VERIFIED: .planning/PROJECT.md] [VERIFIED: .planning/intel/requirements.md]  
**Why it happens:** ARCH-02 spans source-of-truth docs and derived planning intel. [VERIFIED: .planning/REQUIREMENTS.md]  
**How to avoid:** Grep all active docs for "six button", "six-button", "exactly six", and CANV-02 after editing; leave historical milestone archives unchanged unless the text is part of active current context.  
**Warning signs:** `rg "six buttons|six-button|exactly six|D-70|star5" docs .planning` still shows active stale claims.

## Code Examples

### Generalize Figure Type Seed Target

```csharp
// Source: src/BlazorCanvas/Data/V11/Transition/V11Schema.cs pattern, adapted for Phase 14.
public static async Task SeedFigureTypesAsync(
    NpgsqlConnection connection,
    NpgsqlTransaction transaction,
    string schema,
    ShapeRegistry registry,
    CancellationToken ct = default)
{
    foreach (var name in registry.Names)
    {
        await using var command = new NpgsqlCommand(
            $"INSERT INTO {schema}.figure_types (name) VALUES (@name) ON CONFLICT (name) DO NOTHING",
            connection,
            transaction);
        command.Parameters.AddWithValue("name", name);
        await command.ExecuteNonQueryAsync(ct);
    }
}
```

Planner note: if using interpolated schema names, keep schema values closed over constants/enums only (`"v11"` or `"public"`), never user input. [VERIFIED: src/BlazorCanvas/Data/V11/Transition/V11Schema.cs]

### Completed Catalog Seed Flow

```csharp
// Source: src/BlazorCanvas/Data/V11/V11Cutover.cs pattern, adapted for Phase 14.
if (state == CatalogState.Completed)
{
    await V11Schema.SeedPublicFigureTypesAsync(connection, transaction, registry, ct);
    await transaction.CommitAsync(ct);
    return;
}
```

### Toolbar Star Button

```razor
@* Source: src/BlazorCanvas/Components/Canvas/Toolbar.razor pattern *@
<button type="button" class="@ToolButtonClass(Tool.Star)"
        aria-pressed="@(Armed == Tool.Star)"
        aria-label="Draw star"
        @onclick="() => ArmedChanged.InvokeAsync(Tool.Star)">
    <!-- Inline 20x20 SVG icon, matching existing tool buttons. -->
</button>
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Four fixed figure types | Figure types are rows seeded from registry | v1.11 D-65, amended by v1.12 MODEL-08 | Adding `star5` is registry + row, not schema. [VERIFIED: docs/DECISIONS.md] |
| Cutover seed only during migration/fresh install | Startup seed runs against completed public catalog too | Phase 14 requirement | Existing upgraded DB becomes writable for new registered shapes. [VERIFIED: .planning/REQUIREMENTS.md] |
| Six-button toolbar | Seven-button toolbar with star before delete | Phase 14 requirement | Active decision docs must amend D-16/D-33/D-58 and CANV-02 text. [VERIFIED: .planning/REQUIREMENTS.md] |

**Deprecated/outdated:**

- "CompletedPublicCatalog normal ensure is an exact no-op" is outdated for registry seed rows; the new invariant is idempotent convergence to registry contents. [VERIFIED: tests/BlazorCanvas.Tests/Database/V11/V11CutoverTests.cs]
- "Exactly six buttons" is outdated in active docs for v1.12; historical milestone archives may remain historical. [VERIFIED: .planning/REQUIREMENTS.md]
- Phase 13's default-registry no-star test is outdated once Phase 14 starts implementation. [VERIFIED: tests/BlazorCanvas.Tests/Shapes/Star5ShapeTests.cs]

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | Clicking the star toolbar button may bubble to the toolbar deselect handler, but should follow existing button behavior. | Common Pitfalls | Low; copying existing button markup preserves behavior. |

## Open Questions (RESOLVED)

1. **Exact D-70+ decision wording**
   - What we know: ARCH-02 requires seven-button amendments to D-16/D-33/D-58/CANV-02 and star decisions by name from D-70 onward. [VERIFIED: .planning/REQUIREMENTS.md]
   - RESOLVED: Plan 14-03 Task 1 owns the accepted D-70+ wording. The executor should write concise entries for the locked Phase 14 decision boundaries: `star5` as the registry/catalog startup seed exposure, the star toolbar button between triangle and delete, and no star rendering or persistence write-path work in Phase 14. Geometry wording may reference the already-locked Phase 13 facts, but Phase 14 docs must not imply preview, render, draw, select, drag, delete, sync, or save paths are implemented in this phase.

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|-------------|-----------|---------|----------|
| .NET SDK | Build and test | yes | 10.0.301 | none |
| Docker | PostgreSQL test container | yes | 29.1.3 | none for database tests |
| PostgreSQL via Docker Compose | `V11CutoverTests` | expected by fixture | Postgres configured on host port 5433 | Run `docker compose up -d` if tests fail to connect. [VERIFIED: tests/BlazorCanvas.Tests/Database/DatabaseFixture.cs] |
| `psql` CLI | Optional manual SQL inspection | no | - | Use Npgsql/xUnit tests; do not require manual CLI checks. |

**Missing dependencies with no fallback:** none identified.  
**Missing dependencies with fallback:** `psql` is absent; existing Npgsql tests are the fallback. [VERIFIED: shell]

## Security Domain

Security enforcement is enabled by default because `.planning/config.json` does not set `security_enforcement: false`. [VERIFIED: .planning/config.json]

### Applicable ASVS Categories

| ASVS Category | Applies | Standard Control |
|---------------|---------|------------------|
| V2 Authentication | no | Phase 14 does not alter login or passwords. [VERIFIED: .planning/ROADMAP.md] |
| V3 Session Management | no | Phase 14 does not alter cookies or logout endpoint behavior; preserve existing logout form. [VERIFIED: src/BlazorCanvas/Components/Canvas/Toolbar.razor] |
| V4 Access Control | no | No new endpoints or authorization paths. [VERIFIED: .planning/ROADMAP.md] |
| V5 Input Validation | yes | Registry exposes `star5` to `FigureInputGateway`; rely on Phase 13 `Star5Shape.TryParseGeometry` and `IsDrawable`. [VERIFIED: src/BlazorCanvas/Shapes/FigureInputGateway.cs] [VERIFIED: src/BlazorCanvas/Shapes/Star5Shape.cs] |
| V6 Cryptography | no | No crypto added; plaintext password choice remains locked and unrelated. [VERIFIED: docs/DECISIONS.md] |

### Known Threat Patterns for This Stack

| Pattern | STRIDE | Standard Mitigation |
|---------|--------|---------------------|
| SQL injection through figure-type names | Tampering | Use parameterized `@name`; never concatenate registry names into value positions. [VERIFIED: src/BlazorCanvas/Data/V11/Transition/V11Schema.cs] |
| Unsafe schema-name interpolation | Tampering | If schema must be interpolated, accept only closed constants (`v11`, `public`) controlled by code. [VERIFIED: codebase analysis] |
| Client-supplied `star5` geometry becomes accepted after registration | Tampering | `FigureInputGateway` parses, validates, and reserializes through `Star5Shape`. [VERIFIED: src/BlazorCanvas/Shapes/FigureInputGateway.cs] |
| Toolbar action changes logout/session semantics | Spoofing | Do not convert logout into an interactive button; preserve POST form with antiforgery token. [VERIFIED: src/BlazorCanvas/Components/Canvas/Toolbar.razor] |

## Sources

### Primary (HIGH confidence)

- `.planning/REQUIREMENTS.md` - MODEL-08, CANV-04, ARCH-02 and out-of-scope boundaries. [VERIFIED: shell]
- `.planning/ROADMAP.md` - Phase 14 scope, sequencing, and success criteria. [VERIFIED: shell]
- `.planning/STATE.md` - locked v1.12 sequencing and Phase 14 rationale. [VERIFIED: shell]
- `.planning/PROJECT.md` - current milestone context and active stale six-button text to amend. [VERIFIED: shell]
- `docs/DECISIONS.md` - authoritative decision log and current D-65 storage model. [VERIFIED: shell]
- `src/BlazorCanvas/Shapes/DefaultShapes.cs`, `ShapeRegistry.cs`, `Star5Shape.cs` - registry and shape implementation surfaces. [VERIFIED: shell]
- `src/BlazorCanvas/Data/V11/V11Cutover.cs`, `Transition/V11Schema.cs` - completed-catalog early return and seed implementation. [VERIFIED: shell]
- `src/BlazorCanvas/Tools/Tool.cs`, `Components/Canvas/Toolbar.razor`, `Toolbar.razor.css` - toolbar state, mapping, and layout. [VERIFIED: shell]
- `tests/BlazorCanvas.Tests/Database/V11/V11CutoverTests.cs`, `DefaultShapesTests.cs`, `Star5ShapeTests.cs` - required test updates. [VERIFIED: shell]

### Secondary (MEDIUM confidence)

- None used for implementation guidance; this is a codebase-specific phase. [VERIFIED: research-plan seam]

### Tertiary (LOW confidence)

- WebSearch for project-specific Phase 14 terms returned no authoritative relevant source; findings were cached only as negative research. [VERIFIED: websearch]

## Metadata

**Confidence breakdown:**

- Standard stack: HIGH - versions and dependencies are read from local project files and local CLI probes. [VERIFIED: shell]
- Architecture: HIGH - phase surfaces are directly visible in code and roadmap. [VERIFIED: shell]
- Pitfalls: HIGH for seed/registry/test/doc risks from local files; LOW for the single event-bubbling caution marked as assumed. [VERIFIED: shell]

**Research date:** 2026-07-22  
**Valid until:** 2026-08-21, or until Phase 14 changes `V11Cutover`, `DefaultShapes`, or toolbar files.
