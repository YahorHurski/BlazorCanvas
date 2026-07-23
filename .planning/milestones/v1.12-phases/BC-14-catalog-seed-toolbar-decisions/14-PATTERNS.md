# Phase 14: Catalog Seed, Toolbar & Decisions - Pattern Map

**Mapped:** 2026-07-22
**Files analyzed:** 13
**Analogs found:** 13 / 13

## File Classification

| New/Modified File | Role | Data Flow | Closest Analog | Match Quality |
|-------------------|------|-----------|----------------|---------------|
| `src/BlazorCanvas/Shapes/DefaultShapes.cs` | config | transform | `src/BlazorCanvas/Shapes/DefaultShapes.cs` | exact |
| `src/BlazorCanvas/Data/V11/Transition/V11Schema.cs` | utility | CRUD | `src/BlazorCanvas/Data/V11/Transition/V11Schema.cs` | exact |
| `src/BlazorCanvas/Data/V11/V11Cutover.cs` | service | batch | `src/BlazorCanvas/Data/V11/V11Cutover.cs` | exact |
| `src/BlazorCanvas/Tools/Tool.cs` | model | transform | `src/BlazorCanvas/Tools/Tool.cs` | exact |
| `src/BlazorCanvas/Components/Canvas/Toolbar.razor` | component | event-driven | `src/BlazorCanvas/Components/Canvas/Toolbar.razor` | exact |
| `src/BlazorCanvas/Components/Canvas/Toolbar.razor.css` | component style | event-driven | `src/BlazorCanvas/Components/Canvas/Toolbar.razor.css` | exact-preserve |
| `tests/BlazorCanvas.Tests/Shapes/DefaultShapesTests.cs` | test | transform | `tests/BlazorCanvas.Tests/Shapes/DefaultShapesTests.cs` | exact |
| `tests/BlazorCanvas.Tests/Shapes/Star5ShapeTests.cs` | test | transform | `tests/BlazorCanvas.Tests/Shapes/Star5ShapeTests.cs` | exact |
| `tests/BlazorCanvas.Tests/Database/V11/V11CutoverTests.cs` | test | CRUD | `tests/BlazorCanvas.Tests/Database/V11/V11CutoverTests.cs` | exact |
| `docs/DECISIONS.md` | documentation | transform | `docs/DECISIONS.md` | exact |
| `.planning/PROJECT.md` | documentation | transform | `.planning/PROJECT.md` | exact |
| `.planning/intel/decisions.md` | documentation | transform | `.planning/intel/decisions.md` | exact |
| `.planning/intel/requirements.md` | documentation | transform | `.planning/intel/requirements.md` | exact |

## Pattern Assignments

### `src/BlazorCanvas/Shapes/DefaultShapes.cs` (config, transform)

**Analog:** `src/BlazorCanvas/Shapes/DefaultShapes.cs`

**Imports / namespace pattern** (line 1):
```csharp
namespace BlazorCanvas.Shapes;
```

**Core registry pattern** (lines 12-19):
```csharp
public static ShapeRegistry CreateRegistry()
{
    var registry = new ShapeRegistry();
    registry.Register(new LineShape());
    registry.Register(new RectangleShape());
    registry.Register(new CircleShape());
    registry.Register(new TriangleShape());
    return registry;
}
```

**Registration semantics source** (from `src/BlazorCanvas/Shapes/ShapeRegistry.cs` lines 25-40):
```csharp
public void Register(IShapeDefinition definition)
{
    ArgumentNullException.ThrowIfNull(definition);

    if (string.IsNullOrWhiteSpace(definition.Name))
    {
        throw new ArgumentException("Shape definition names cannot be null, empty, or whitespace.", nameof(definition));
    }

    if (!_byName.TryAdd(definition.Name, definition))
    {
        throw new ArgumentException($"A shape definition named '{definition.Name}' is already registered.", nameof(definition));
    }

    _definitions.Add(definition);
    _names.Add(definition.Name);
}
```

**Apply:** Add `registry.Register(new Star5Shape());` immediately after `TriangleShape` to preserve canonical seed/display order requested by Phase 14.

---

### `src/BlazorCanvas/Data/V11/Transition/V11Schema.cs` (utility, CRUD)

**Analog:** `src/BlazorCanvas/Data/V11/Transition/V11Schema.cs`

**Imports pattern** (lines 1-2):
```csharp
using BlazorCanvas.Shapes;
using Npgsql;
```

**DDL schema constant pattern** (lines 7-10):
```csharp
public static class V11Schema
{
    public const string SchemaName = "v11";
    public const string Ddl = """
```

**Parameterized idempotent seed pattern** (lines 40-47):
```csharp
public static async Task SeedFigureTypesAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, ShapeRegistry registry, CancellationToken ct = default)
{
    foreach (var name in registry.Names)
    {
        await using var command = new NpgsqlCommand("INSERT INTO v11.figure_types (name) VALUES (@name) ON CONFLICT (name) DO NOTHING", connection, transaction);
        command.Parameters.AddWithValue("name", name);
        await command.ExecuteNonQueryAsync(ct);
    }
}
```

**Apply:** Generalize the target schema with a closed code-owned value (`"v11"` or `"public"`). Keep figure-type names parameterized with `@name`; do not concatenate registry names into SQL values.

---

### `src/BlazorCanvas/Data/V11/V11Cutover.cs` (service, batch)

**Analog:** `src/BlazorCanvas/Data/V11/V11Cutover.cs`

**Imports pattern** (lines 1-3):
```csharp
using BlazorCanvas.Data.V11.Transition;
using BlazorCanvas.Shapes;
using Npgsql;
```

**Transaction + advisory lock pattern** (lines 23-28):
```csharp
ArgumentNullException.ThrowIfNull(dataSource); ArgumentNullException.ThrowIfNull(registry);
await using var connection = await dataSource.OpenConnectionAsync(ct);
await using var transaction = await connection.BeginTransactionAsync(ct);
await using (var lockCommand = new NpgsqlCommand("SELECT pg_advisory_xact_lock(11011)", connection, transaction)) await lockCommand.ExecuteNonQueryAsync(ct);
var state = await GetStateAsync(connection, transaction, ct);
if (state == CatalogState.Completed) { await transaction.RollbackAsync(ct); return; }
```

**Seed during additive/fresh paths** (lines 32-45):
```csharp
await V11Schema.ApplyAsync(connection, transaction, ct);
await ProbeAsync(V11CutoverStage.AfterSchemaApply, failureProbe, ct);
await V11Schema.SeedFigureTypesAsync(connection, transaction, registry, ct);
await ProbeAsync(V11CutoverStage.AfterTypeSeed, failureProbe, ct);
```

**Promotion + commit pattern** (lines 47-56):
```csharp
await using (var drop = new NpgsqlCommand("DROP TABLE IF EXISTS public.figures", connection, transaction)) await drop.ExecuteNonQueryAsync(ct);
await ProbeAsync(V11CutoverStage.AfterDropPublicFigures, failureProbe, ct);
foreach (var table in new[] { "canvases", "figure_types", "figures" })
{
    await using var promote = new NpgsqlCommand($"ALTER TABLE v11.{table} SET SCHEMA public", connection, transaction);
    await promote.ExecuteNonQueryAsync(ct);
    if (table == "canvases") await ProbeAsync(V11CutoverStage.AfterPromoteCanvases, failureProbe, ct);
}
await using (var dropSchema = new NpgsqlCommand("DROP SCHEMA v11", connection, transaction)) await dropSchema.ExecuteNonQueryAsync(ct);
await transaction.CommitAsync(ct);
```

**Apply:** Replace the completed-state rollback/return with a public `figure_types` seed and `CommitAsync`. Keep the existing transaction and advisory lock around completed-catalog convergence.

---

### `src/BlazorCanvas/Tools/Tool.cs` (model, transform)

**Analog:** `src/BlazorCanvas/Tools/Tool.cs`

**Enum/action split pattern** (lines 13-20):
```csharp
public enum Tool
{
    Pointer,
    Line,
    Rectangle,
    Circle,
    Triangle
}
```

**Shape-name mapping pattern** (lines 32-40):
```csharp
public static string? ToShapeName(Tool tool) => tool switch
{
    Tool.Pointer => null,
    Tool.Line => "line",
    Tool.Rectangle => "rectangle",
    Tool.Circle => "circle",
    Tool.Triangle => "triangle",
    _ => throw new ArgumentOutOfRangeException(nameof(tool), tool, "Unknown tool.")
};
```

**Apply:** Add `Tool.Star` after `Triangle` and map it to `"star5"`. Do not add `Tool.Delete`; delete remains an action button outside the armable enum.

---

### `src/BlazorCanvas/Components/Canvas/Toolbar.razor` (component, event-driven)

**Analog:** `src/BlazorCanvas/Components/Canvas/Toolbar.razor`

**Imports / injected logout token pattern** (lines 1-4):
```razor
@using BlazorCanvas.Tools
@inject Microsoft.AspNetCore.Components.Forms.AntiforgeryStateProvider Antiforgery

@{ var token = Antiforgery.GetAntiforgeryToken(); }
```

**Armable button pattern** (lines 33-37):
```razor
<button type="button" class="@ToolButtonClass(Tool.Triangle)" aria-pressed="@(Armed == Tool.Triangle)" aria-label="Draw triangle" @onclick="() => ArmedChanged.InvokeAsync(Tool.Triangle)">
    <svg width="20" height="20" viewBox="0 0 20 20" fill="none" stroke="currentColor" stroke-width="1.5" xmlns="http://www.w3.org/2000/svg">
        <polygon points="10,3 3,17 17,17" stroke-linejoin="round" />
    </svg>
</button>
```

**Delete action pattern** (lines 39-43):
```razor
<button type="button" class="tool-button delete-button" disabled="@(!DeleteEnabled)" aria-label="Delete selected figure" @onclick="() => OnDelete.InvokeAsync()" @onclick:stopPropagation="true">
    <svg width="20" height="20" viewBox="0 0 20 20" fill="none" stroke="currentColor" stroke-width="1.5" xmlns="http://www.w3.org/2000/svg">
        <path d="M4 6h12M8 6V4h4v2M6 6l1 10a1 1 0 0 0 1 1h4a1 1 0 0 0 1-1l1-10" stroke-linecap="round" stroke-linejoin="round" />
    </svg>
</button>
```

**Logout form preservation pattern** (lines 47-56):
```razor
<form method="post" action="/logout" class="logout-form">
    <input type="hidden" name="@token!.FormFieldName" value="@token.Value" />
    <button type="submit" class="tool-button logout-button" aria-label="Log out">
        <svg width="20" height="20" viewBox="0 0 20 20" fill="none" stroke="currentColor" stroke-width="1.5" xmlns="http://www.w3.org/2000/svg">
            <path d="M8 3H4.5A1.5 1.5 0 0 0 3 4.5v11A1.5 1.5 0 0 0 4.5 17H8" stroke-linecap="round" stroke-linejoin="round" />
            <path d="M12 13.5 16.5 9 12 4.5" stroke-linecap="round" stroke-linejoin="round" />
            <path d="M16.5 9H7" stroke-linecap="round" stroke-linejoin="round" />
        </svg>
    </button>
</form>
```

**Parameter + armed class pattern** (lines 63-82):
```razor
[Parameter]
public Tool Armed { get; set; } = Tool.Pointer;

[Parameter]
public EventCallback<Tool> ArmedChanged { get; set; }

private string ToolButtonClass(Tool tool) => Armed == tool ? "tool-button is-armed" : "tool-button";
```

**Apply:** Insert the star armable button between triangle and delete. Use `aria-label="Draw star"`, `ToolButtonClass(Tool.Star)`, `aria-pressed="@(Armed == Tool.Star)"`, and `ArmedChanged.InvokeAsync(Tool.Star)`.

---

### `src/BlazorCanvas/Components/Canvas/Toolbar.razor.css` (component style, event-driven)

**Analog:** `src/BlazorCanvas/Components/Canvas/Toolbar.razor.css`

**Locked toolbar layout** (lines 1-8):
```css
.toolbar {
    display: flex;
    align-items: center;
    height: 48px;
    width: 100%;
    background: #DCE0E5;
    box-sizing: border-box;
}
```

**40x40 icon-button pattern** (lines 15-28):
```css
.tool-button {
    width: 40px;
    height: 40px;
    display: flex;
    align-items: center;
    justify-content: center;
    margin: 4px 0;
    background: transparent;
    color: #1F2937;
    border: none;
    border-radius: 4px;
    cursor: pointer;
    transition: 120ms ease;
}
```

**Spacing / state pattern** (lines 30-45):
```css
.tool-button + .tool-button {
    margin-left: 4px;
}

.tool-button:hover {
    background: #CBD1D8;
}

.tool-button:focus-visible {
    outline: 2px solid #1D4ED8;
    outline-offset: 2px;
}

.tool-button.is-armed {
    background: #1D4ED8;
    color: #FFFFFF;
}
```

**Logout alignment pattern** (lines 66-68):
```css
.logout-form {
    margin-left: auto;
    margin-right: 4px;
}
```

**Apply:** No CSS change should be necessary. The star button should inherit `.tool-button` and remain in the single 48px flex row.

---

### `tests/BlazorCanvas.Tests/Shapes/DefaultShapesTests.cs` (test, transform)

**Analog:** `tests/BlazorCanvas.Tests/Shapes/DefaultShapesTests.cs`

**Canonical order assertion** (lines 8-12):
```csharp
[Fact]
public void CreateRegistry_RegistersCanonicalNamesInSeedOrder()
{
    Assert.Equal(new[] { "line", "rectangle", "circle", "triangle" }, DefaultShapes.CreateRegistry().Names);
}
```

**Round-trip coverage over all registered names** (lines 50-67):
```csharp
var registry = DefaultShapes.CreateRegistry();
var gestures = new Dictionary<string, (CanvasPoint Press, CanvasPoint Cursor)>
{
    ["line"] = (new CanvasPoint(10, 20), new CanvasPoint(110, 60)),
    ["rectangle"] = (new CanvasPoint(10, 20), new CanvasPoint(110, 60)),
    ["circle"] = (new CanvasPoint(300, 300), new CanvasPoint(350, 300)),
    ["triangle"] = (new CanvasPoint(10, 20), new CanvasPoint(110, 60))
};

foreach (var name in registry.Names)
{
    var definition = registry.Get(name);
    var placement = definition.FromGesture(gestures[name].Press, gestures[name].Cursor);
    var json = definition.ToJson(placement.Geometry);
    using var document = JsonDocument.Parse(json);
    Assert.True(definition.TryParseGeometry(document.RootElement, out var parsed));
    Assert.Equal(definition.BoundsOf(placement.Geometry), definition.BoundsOf(parsed));
}
```

**Apply:** Update expected order to include `"star5"` after `"triangle"` and add a gesture entry for `"star5"` matching rectangle/triangle corner-to-corner input.

---

### `tests/BlazorCanvas.Tests/Shapes/Star5ShapeTests.cs` (test, transform)

**Analog:** `tests/BlazorCanvas.Tests/Shapes/Star5ShapeTests.cs`

**Phase 13 fence to remove or invert** (lines 145-149):
```csharp
[Fact]
public void DefaultRegistry_DoesNotContainStar5DuringPhase13()
{
    Assert.Equal(new[] { "line", "rectangle", "circle", "triangle" }, DefaultShapes.CreateRegistry().Names);
}
```

**Direct shape test style** (lines 100-108):
```csharp
public void FromGesture_CornerToCorner_CreatesPointUpTenPointStretchableStar()
{
    var placement = _subject.FromGesture(new CanvasPoint(10, 20), new CanvasPoint(210, 120));

    Assert.Equal(10, placement.X);
    Assert.Equal(20, placement.Y);
    var star = Assert.IsType<Star5Geometry>(placement.Geometry);
    Assert.Equal(Star5Shape.DefaultInnerRatio, star.InnerRatio);
    AssertStarPoints(ExpectedStarPoints(200, 100, Star5Shape.DefaultInnerRatio), star.Points);
}
```

**Apply:** Delete the temporary Phase 13 no-registration fence or replace it with a Phase 14 assertion that the default registry contains `star5` in the canonical position.

---

### `tests/BlazorCanvas.Tests/Database/V11/V11CutoverTests.cs` (test, CRUD)

**Analog:** `tests/BlazorCanvas.Tests/Database/V11/V11CutoverTests.cs`

**Database test fixture pattern** (lines 7-11):
```csharp
[Collection("Database")]
public class V11CutoverTests
{
    private readonly DatabaseFixture fixture;
    public V11CutoverTests(DatabaseFixture fixture) => this.fixture = fixture;
```

**Existing hard-coded catalog-count assertions** (lines 47-59 and 62-73):
```csharp
[Fact]
public async Task AdditiveCatalog_RerunsWithoutDuplicateRows()
{
    await using var scratch = await V11CutoverScratchDatabase.CreateAsync(fixture.ConnectionString);
    await scratch.SetupAdditiveAsync();

    await V11Cutover.EnsureAsync(scratch.DataSource, DefaultShapes.CreateRegistry());

    await AssertFinalPublicCatalogAsync(scratch.DataSource);
    await using var connection = await scratch.DataSource.OpenConnectionAsync();
    Assert.Equal(1L, await ScalarAsync<long>(connection, "SELECT count(*) FROM public.canvases"));
    Assert.Equal(4L, await ScalarAsync<long>(connection, "SELECT count(*) FROM public.figure_types"));
    Assert.Equal(2L, await ScalarAsync<long>(connection, "SELECT count(*) FROM public.figures"));
}
```

```csharp
await V11Cutover.EnsureAsync(scratch.DataSource, DefaultShapes.CreateRegistry());

await AssertFinalPublicCatalogAsync(scratch.DataSource);
await using var connection = await scratch.DataSource.OpenConnectionAsync();
Assert.Equal(0L, await ScalarAsync<long>(connection, "SELECT count(*) FROM public.canvases"));
Assert.Equal(4L, await ScalarAsync<long>(connection, "SELECT count(*) FROM public.figure_types"));
Assert.Equal(0L, await ScalarAsync<long>(connection, "SELECT count(*) FROM public.figures"));
```

**Completed no-op test to amend** (lines 77-87):
```csharp
[Fact]
public async Task CompletedPublicCatalog_NormalEnsureIsAnExactNoOp()
{
    await using var scratch = await V11CutoverScratchDatabase.CreateAsync(fixture.ConnectionString);
    await scratch.SetupCompletedPublicAsync();
    var before = await scratch.SnapshotAsync();

    await V11Cutover.EnsureAsync(scratch.DataSource, DefaultShapes.CreateRegistry());

    Assert.Equal(before, await scratch.SnapshotAsync());
    await AssertFinalPublicCatalogAsync(scratch.DataSource);
}
```

**Scratch completed-catalog setup pattern** (from `tests/BlazorCanvas.Tests/Database/V11/V11CutoverScratchDatabase.cs` lines 73-77):
```csharp
public async Task SetupCompletedPublicAsync()
{
    await SetupFreshUsersOnlyAsync();
    await V11Cutover.EnsureAsync(DataSource, DefaultShapes.CreateRegistry());
}
```

**Apply:** Change count assertions from `4L` to `5L`, add proof that a completed public catalog missing `star5` converges on startup, and prove two consecutive startups leave exactly one `star5` row. The old exact snapshot no-op assertion conflicts with MODEL-08.

---

### `docs/DECISIONS.md` (documentation, transform)

**Analog:** `docs/DECISIONS.md`

**Superseded decision amendment style** (lines 476-487):
```markdown
## D-16 — Shape selection: a toolbar

**Status:** ⚠️ **SUPERSEDED — see D-30 and D-33.** The toolbar is **six** buttons, not four.
This entry is kept for its rationale only. **Do not implement from this entry.**

~~A toolbar with four buttons~~ — **line / rectangle / circle / triangle**. Click one to arm
it; the armed button stays visibly active. Then drag on the canvas to draw that shape.

> **The current toolbar (authoritative):**
> `[ pointer ] [ line ] [ rectangle ] [ circle ] [ triangle ] [ delete ]`
```

**Toolbar decision pattern to amend** (lines 1162-1166):
```markdown
The toolbar is therefore **six buttons**:

```
[ pointer ] [ line ] [ rectangle ] [ circle ] [ triangle ] [ delete ]
```
```

**Storage/catalog decision pattern** (lines 2251-2259):
```markdown
## D-65 — Valid figure types are rows, not a CHECK constraint

**Status:** Locked — supersedes the CHECK half of **D-46**; lifts D-05's "there are only ever four"

`figures.type` is a foreign key into `figure_types(name)`. Adding a figure type is an `INSERT`
(seeded at application start), not an `ALTER TABLE`.
```

**Append location pattern** (lines 2395-2397):
```markdown
---

*Log complete. All decisions were made by the user; nothing here was decided by default.*
```

**Apply:** Add D-70+ entries before the final "Log complete" line. Amend active D-16/D-30/D-33/D-56/D-58 wording so the authoritative toolbar is seven buttons and logout remains outside the count. Keep historical archived milestone files unchanged.

---

### `.planning/PROJECT.md` (documentation, transform)

**Analog:** `.planning/PROJECT.md`

**Current milestone context pattern** (lines 37-64):
```markdown
## Current Milestone: v1.12 Five-pointed star

**Opened 2026-07-22.** Branch `Milestone-v1.12`. Phase 13 is complete: the pure C# `Star5Shape` /
`Star5Geometry` core is built, directly tested, and deliberately not registered yet. The milestone
continues with Phase 14, which exposes that core through the registry, catalog seed, and toolbar.

**Goal:** Add `star5` as the fifth figure type end-to-end — drawn, previewed, persisted, synced,
selected, dragged and deleted exactly like the four that came before it.
```

**Active stale summary to amend** (lines 119-120):
```markdown
- [x] **CANV-01/02** — The SVG canvas at (0, 48); the six-button toolbar — Validated in Phase BC-03: The Canvas & Drawing (2026-07-16). *(Canvas was 1280 × 720 at v1.0; enlarged to 1472 × 828 in v1.1 — see CANV-03.)*
```

**Apply:** Preserve the current milestone section style. Amend active text so Phase 14 records the seven-button update without implying historical v1.0 validation already included the star.

---

### `.planning/intel/decisions.md` (documentation, transform)

**Analog:** `.planning/intel/decisions.md`

**Toolbar intel pattern** (lines 291-297):
```markdown
### D-16 + D-30 + D-33 — The toolbar (six buttons, authoritative)
source: docs/DECISIONS.md (D-16 superseded; D-30, D-33 current) · Locked
```
```markdown
[ pointer ] [ line ] [ rectangle ] [ circle ] [ triangle ] [ delete ]
```
```markdown
**Six buttons.** Click one to arm it; the armed button stays visibly active. Logout sits
right-aligned in the same strip, separate from the six (D-56).
```

**Logout intel pattern** (lines 149-155):
```markdown
### D-56 — Logout sits right-aligned in the toolbar strip
source: docs/DECISIONS.md (D-56) · Locked
Logout lives in the same 48px toolbar strip (D-43), right-aligned, visually separated from the
six tool buttons. It is a small HTML form posting to `POST /logout` — not an interactive button
(clearing a cookie requires an HTTP round-trip). **Toolbar height stays 48px**, so D-43's
coordinate constant is unchanged. The "six buttons" rule stays intact — logout is an account
action, not a drawing tool.
```

**Apply:** Mirror the final D-70+ and seven-button decision wording from `docs/DECISIONS.md`. This is a derived active intel file, so keep it concise and source-linked.

---

### `.planning/intel/requirements.md` (documentation, transform)

**Analog:** `.planning/intel/requirements.md`

**Requirement mirror pattern** (lines 74-82):
```markdown
## REQ-toolbar
source: docs/DECISIONS.md (D-16 superseded, D-30, D-33, D-31, D-58)
A six-button toolbar: `[pointer] [line] [rectangle] [circle] [triangle] [delete]`.

Acceptance:
- Exactly six buttons. The armed button stays visibly active.
- The **pointer tool is armed on page load**.
- The **Delete button is greyed out and unclickable when nothing is selected**.
- Logout is present in the strip but is not one of the six.
```

**Apply:** Update to seven buttons with star between triangle and delete, while preserving pointer-on-load, armed visible state, disabled delete, and logout outside the count.

## Shared Patterns

### Registry Is The Catalog Source
**Source:** `src/BlazorCanvas/Program.cs` lines 41-45 and `src/BlazorCanvas/Data/V11/Transition/V11Schema.cs` lines 40-47
**Apply to:** `DefaultShapes.cs`, `V11Schema.cs`, `V11Cutover.cs`, registry/cutover tests
```csharp
builder.Services.AddSingleton<NpgsqlDataSource>(_ => NpgsqlDataSource.Create(
    builder.Configuration.GetConnectionString("Canvas")
    ?? throw new InvalidOperationException("Connection string 'Canvas' is required.")));
builder.Services.AddSingleton(DefaultShapes.CreateRegistry());
builder.Services.AddScoped<FigureInputGateway>();
```

```csharp
foreach (var name in registry.Names)
{
    await using var command = new NpgsqlCommand("INSERT INTO v11.figure_types (name) VALUES (@name) ON CONFLICT (name) DO NOTHING", connection, transaction);
    command.Parameters.AddWithValue("name", name);
    await command.ExecuteNonQueryAsync(ct);
}
```

### Completed Catalog Startup Must Still Converge
**Source:** `src/BlazorCanvas/Data/V11/V11Cutover.cs` lines 24-29
**Apply to:** `V11Cutover.cs`, `V11CutoverTests.cs`
```csharp
await using var connection = await dataSource.OpenConnectionAsync(ct);
await using var transaction = await connection.BeginTransactionAsync(ct);
await using (var lockCommand = new NpgsqlCommand("SELECT pg_advisory_xact_lock(11011)", connection, transaction)) await lockCommand.ExecuteNonQueryAsync(ct);
var state = await GetStateAsync(connection, transaction, ct);
if (state == CatalogState.Completed) { await transaction.RollbackAsync(ct); return; }
if (state == CatalogState.Invalid) throw new InvalidOperationException("The v1.11 catalog is partial or unsupported; refusing destructive cutover.");
```

### Tool Map Drives Drawing
**Source:** `src/BlazorCanvas/Components/Pages/Home.razor` lines 21-23 and 111-116
**Apply to:** `Tool.cs`, `Toolbar.razor`
```razor
<Toolbar @bind-Armed="armedTool" DeleteEnabled="@(coordinator?.SelectedId is not null)" OnDelete="HandleDeleteAsync" OnDeselect="Deselect" />
<div class="canvas-area">
    <svg @ref="canvasSurface" class="@CanvasSurfaceClass" width="@CanvasBounds.Width" height="@CanvasBounds.Height" data-preview-tool="@ToolMap.ToShapeName(armedTool)" @onpointerdown="OnPointerDown" @onpointermove="OnPointerMove" @onpointerup="OnPointerUp" @onpointerleave="OnPointerLeave">
```

```csharp
if (e.Button != 0 || coordinator is null) return;
Deselect();
var type = ToolMap.ToShapeName(armedTool);
if (type is null) return;
preview ??= new DrawingPreviewSession(ShapeRegistry);
preview.Begin(type, ToCanvasPoint(e));
```

### Database Tests Use Scratch Catalogs
**Source:** `tests/BlazorCanvas.Tests/Database/V11/V11CutoverScratchDatabase.cs` lines 24-33 and 124-135
**Apply to:** `V11CutoverTests.cs`
```csharp
public static async Task<V11CutoverScratchDatabase> CreateAsync(string fixtureConnectionString)
{
    var databaseName = $"canvas_cutover_{Guid.NewGuid():N}";
    var connectionString = new NpgsqlConnectionStringBuilder(fixtureConnectionString) { Database = databaseName, Pooling = false };
    var database = new V11CutoverScratchDatabase(connectionString);
    await using var maintenance = new NpgsqlConnection(database.maintenanceConnectionString.ConnectionString);
    await maintenance.OpenAsync();
    await using var create = new NpgsqlCommand($"CREATE DATABASE \"{databaseName}\"", maintenance);
    await create.ExecuteNonQueryAsync();
    return database;
}
```

```csharp
public async ValueTask DisposeAsync()
{
    await DataSource.DisposeAsync();
    await using var maintenance = new NpgsqlConnection(maintenanceConnectionString.ConnectionString);
    await maintenance.OpenAsync();
    await using (var terminate = new NpgsqlCommand("SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = @database AND pid <> pg_backend_pid()", maintenance))
    {
        terminate.Parameters.AddWithValue("database", connectionString.Database!);
        await terminate.ExecuteNonQueryAsync();
    }
    await using var drop = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{connectionString.Database}\"", maintenance);
    await drop.ExecuteNonQueryAsync();
}
```

### Documentation Amendments Must Update Source And Mirrors
**Source:** `docs/DECISIONS.md` lines 476-487, `.planning/intel/decisions.md` lines 291-297, `.planning/intel/requirements.md` lines 74-82
**Apply to:** `docs/DECISIONS.md`, `.planning/PROJECT.md`, `.planning/intel/decisions.md`, `.planning/intel/requirements.md`
```markdown
> **The current toolbar (authoritative):**
> `[ pointer ] [ line ] [ rectangle ] [ circle ] [ triangle ] [ delete ]`
> — the **pointer** was added by D-30 (to distinguish selecting from drawing) and the
> **delete** button by D-33 (replacing the Delete key).
```

## No Analog Found

None. All Phase 14 files have direct same-role analogs in the existing codebase.

## Metadata

**Analog search scope:** `src/BlazorCanvas`, `tests/BlazorCanvas.Tests`, `docs`, `.planning`
**Files scanned:** 24 candidate files plus targeted documentation ranges
**Pattern extraction date:** 2026-07-22
