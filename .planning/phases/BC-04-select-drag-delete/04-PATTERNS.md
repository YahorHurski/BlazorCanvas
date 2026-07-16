# Phase 4: Select, Drag & Delete - Pattern Map

**Mapped:** 2026-07-16
**Files analyzed:** 7 (6 modified, 1 new test-extension)
**Analogs found:** 7 / 7 — this phase is a pure extension of Phase 3's own files; every analog is
the file itself, pre-modification.

## File Classification

| New/Modified File | Role | Data Flow | Closest Analog | Match Quality |
|--------------------|------|-----------|------------------|----------------|
| `src/BlazorCanvas/Components/Pages/Home.razor` | component (page, pointer-event state machine) | event-driven + CRUD (write-on-drop) | itself (Phase 3 version, on disk now) | exact — same file, modify in place |
| `src/BlazorCanvas/Components/Pages/Home.razor.css` | component style | — | itself (Phase 3 version) | exact — same file, modify in place |
| `src/BlazorCanvas/Components/Canvas/FigureShape.razor` | component (SVG shape renderer) | transform (props → markup) | itself (Phase 3 version) | exact — same file, modify in place |
| `src/BlazorCanvas/Components/Canvas/Toolbar.razor` | component (toolbar buttons) | event-driven | itself (Phase 3 version — the Delete `<button>` already exists, disabled, wired to nothing) | exact — same file, modify in place |
| `src/BlazorCanvas/Data/FigureStore.cs` | service/data-access | CRUD | itself (Phase 3 version — `LoadAsync`/`InsertAsync` already establish the `IDbContextFactory` + per-call-context convention) | exact — same file, extend with new methods |
| `tests/BlazorCanvas.Tests/Data/FigureStoreTests.cs` | test | CRUD | itself (Phase 3 version — `TestDbContextFactory` adapter + `CreateStore()` helper already exist) | exact — same file, extend with new `[Fact]`s |
| (no new files) | — | — | — | — |

No new folders, no new files. Every change is an in-place extension of a file Phase 3 already
created. `Movement.cs` (Phase 1, already built, already tested by `ClampTests.cs`) is called from
`Home.razor` but is **not modified**.

## Pattern Assignments

### `src/BlazorCanvas/Components/Pages/Home.razor` (component, event-driven state machine)

**Analog:** itself, current on-disk Phase 3 version (full file, 125 lines, read above).

**Existing draw-gesture state machine to mirror exactly, one level up** (lines 56-108): the new
drag gesture on the page-spanning wrapper must copy this shape verbatim — `if (!flag) return;`
guard first, `(e.Buttons & 1) == 0` mid-move commit-and-return, `OnPointerUp`/`OnPointerLeave` both
just call the same `CommitAsync`-shaped method:
```razor
private async Task OnPointerMove(PointerEventArgs e)
{
    if (!drawing) { return; }
    if ((e.Buttons & 1) == 0) { await CommitAsync(); return; }
    var point = CanvasCoordinates.FromPage(e.PageX, e.PageY);
    previewBox = DrawGesture.Build(previewType, pressX, pressY, point.X, point.Y);
}

private async Task OnPointerUp(PointerEventArgs e)
{
    if (drawing) { await CommitAsync(); }
}

private async Task OnPointerLeave(PointerEventArgs e)
{
    if (drawing) { await CommitAsync(); }
}
```
**Re-entrancy guard pattern to copy exactly** (lines 110-123, `CommitAsync`) — `drawing = false`
is set **first**, before any `await`, and locals are captured immediately after:
```razor
private async Task CommitAsync()
{
    drawing = false;
    var type = previewType;
    var box = previewBox;

    if (!MinSizeGuard.IsDrawable(type, box)) { return; }

    var figure = await Figures.InsertAsync(userId, type, box);
    figures.Add(figure);
}
```
`CommitDragAsync` must follow the identical shape: `dragging = false` first, capture
`dragFigureId`/`dragMoved`/`dragCurrentBox` into locals, guard (`if (!moved) return;` — a click,
no write), then the single `await Figures.UpdateAsync(...)` call, then the `affected == 0` branch.

**Existing coordinate-mapping call to copy verbatim** (line 69, `CanvasCoordinates.FromPage`) —
every new pointer coordinate read in this phase (`HandleFigurePointerDown`,
`OnWrapperPointerMove`) must route through this exact same call, never `OffsetX`/`OffsetY`:
```razor
var point = CanvasCoordinates.FromPage(e.PageX, e.PageY);
```

**Existing `userId`-from-claim pattern to reuse unchanged** (lines 49-54, `OnInitializedAsync`) —
`userId` is already a field read once from the auth cookie claim; every new `Figures.UpdateAsync`/
`DeleteAsync` call in this phase passes this same field, never anything client-supplied:
```razor
protected override async Task OnInitializedAsync()
{
    var state = await AuthStateTask!;
    userId = int.Parse(state.User.FindFirst("user_id")!.Value);
    figures = await Figures.LoadAsync(userId);
}
```

**Existing figure-loop to extend** (line 12, `<Toolbar @bind-Armed="armedTool" />`, and lines
14-24, the `<svg>` markup) — the new page-spanning wrapper `<div>` must be added as a **new
outer** element wrapping both, per 04-RESEARCH.md Pattern 2. The existing `<svg
@onpointerdown="OnPointerDown" ...>` handlers and `@foreach` figure loop stay in place, unmodified
except for adding `Selected`/`Selectable`/`OnPointerDown` parameters to each `<FigureShape>` and
one new line in `OnPointerDown`'s null-tool branch (`if (e.Button == 0) { selectedId = null; }`).

**Two gesture flags stay mutually exclusive by construction** (`drawing` vs. the new `dragging`) —
this project's own docs (04-RESEARCH.md Pattern 2) confirm this is safe because a tool is either
`Tool.Pointer` (drag path) or a shape (draw path), never both — no locking code needed, matching
D-11's "one mouse" premise elsewhere in this codebase.

---

### `src/BlazorCanvas/Components/Pages/Home.razor.css` (component style)

**Analog:** itself, current on-disk Phase 3 version (full file, 21 lines, read above).

**Existing selector convention to copy** (lines 8-21) — comment-then-rule style, one rule per
concern, class names on `.canvas-surface`:
```css
.canvas-surface {
    display: block;
    background: #FFFFFF;
    cursor: default;
    user-select: none;
}

.canvas-surface.shape-armed {
    cursor: crosshair;
}
```
New rules per 04-UI-SPEC.md's ratified Cursor Spec (Option 2, confirmed) follow the identical
`.canvas-surface.<state-class>` shape — add, do not restructure:
```css
.canvas-surface:not(.shape-armed) > :is(rect, line, circle, polygon):hover {
    cursor: grab;
}
.canvas-surface.is-dragging {
    cursor: grabbing;
}
```
Plus one new top-level selector for the page-spanning wrapper (D-37's `min-height: 100vh`
requirement — no existing selector to extend, this is genuinely new chrome):
```css
.app-shell {
    min-height: 100vh;
}
```

---

### `src/BlazorCanvas/Components/Canvas/FigureShape.razor` (component, transform)

**Analog:** itself, current on-disk Phase 3 version (full file, 52 lines, read above).

**Existing hardcoded-stroke pattern to replace with a computed property** (lines 6, 9, 12, 15 —
four `stroke="#000000"` literals, one per shape branch):
```razor
<line x1="@Box.X1" y1="@Box.Y1" x2="@Box.X2" y2="@Box.Y2" stroke="#000000" stroke-width="2" stroke-opacity="@OpacityValue" />
<rect x="@Box.X1" y="@Box.Y1" width="@Box.Width" height="@Box.Height" fill="#FFFFFF" stroke="#000000" stroke-width="2" fill-opacity="@OpacityValue" stroke-opacity="@OpacityValue" />
```
Every `stroke="#000000"` becomes `stroke="@StrokeColor"`, with the existing `OpacityValue`
computed-property pattern (lines 37) as the exact template for the new `StrokeColor` property:
```razor
// Existing pattern to mirror (line 37):
private string OpacityValue => Preview ? "0.7" : "1";

// New property, same shape:
private string StrokeColor => Selected ? "#B91C1C" : "#000000";
```

**Existing `[Parameter, EditorRequired]` / `[Parameter]` declaration style to copy** (lines 25-32):
```razor
[Parameter, EditorRequired]
public FigureType Type { get; set; }

[Parameter, EditorRequired]
public Box Box { get; set; }

[Parameter]
public bool Preview { get; set; }
```
New parameters follow the plain (non-required) `[Parameter]` shape, since `Selected`/`Selectable`
have safe `false` defaults and `OnPointerDown` is optional (never invoked when `Selectable` is
false):
```razor
[Parameter]
public bool Selected { get; set; }

[Parameter]
public bool Selectable { get; set; }

[Parameter]
public EventCallback<PointerEventArgs> OnPointerDown { get; set; }
```
**stopPropagation wiring** — add to the root element of each `@switch` branch, per
04-RESEARCH.md Pattern 1 (not present anywhere in the current file — this is the one genuinely new
mechanism in this component):
```razor
<rect ... @onpointerdown="HandlePointerDown" @onpointerdown:stopPropagation="Selectable" />
```
```razor
private Task HandlePointerDown(PointerEventArgs e) =>
    Selectable ? OnPointerDown.InvokeAsync(e) : Task.CompletedTask;
```

---

### `src/BlazorCanvas/Components/Canvas/Toolbar.razor` (component, event-driven)

**Analog:** itself, current on-disk Phase 3 version (full file, 78 lines, read above). The Delete
button and `DeleteEnabled` parameter **already exist**, unwired.

**Existing button already has the disabled-state and parameter wiring done** (line 39, plus
parameter declaration lines 73-74) — this phase's only change is adding a click handler:
```razor
<button type="button" class="tool-button delete-button" disabled="@(!DeleteEnabled)" aria-label="Delete selected figure">
```
```razor
[Parameter]
public bool DeleteEnabled { get; set; }
```
**Existing `@onclick` EventCallback-invoke pattern to copy** (line 9, the Pointer tool button —
the closest existing "button click raises a parent EventCallback" shape in this file):
```razor
<button type="button" class="@ToolButtonClass(Tool.Pointer)" aria-pressed="@(Armed == Tool.Pointer)" aria-label="Pointer tool" @onclick="() => ArmedChanged.InvokeAsync(Tool.Pointer)">
```
Apply the identical shape to the Delete button, adding a new `OnDelete` EventCallback parameter
(mirrors `ArmedChanged`'s declaration at line 67):
```razor
<button type="button" class="tool-button delete-button" disabled="@(!DeleteEnabled)" aria-label="Delete selected figure" @onclick="() => OnDelete.InvokeAsync()">
```
```razor
[Parameter]
public EventCallback OnDelete { get; set; }
```

---

### `src/BlazorCanvas/Data/FigureStore.cs` (service, CRUD)

**Analog:** itself, current on-disk Phase 3 version (full file, 61 lines, read above).

**Existing conventions every new method must copy exactly:**
1. Primary constructor DI, `factory.CreateDbContextAsync()` per call (line 17, class declaration):
```csharp
public sealed class FigureStore(IDbContextFactory<CanvasDbContext> factory)
```
2. `await using var db = await factory.CreateDbContextAsync();` as the first line of every method
   (line 27, `LoadAsync`; line 44, `InsertAsync`):
```csharp
public async Task<List<Figure>> LoadAsync(int userId)
{
    await using var db = await factory.CreateDbContextAsync();
    ...
}
```
3. XML-doc comment on every public method, citing the D-XX decisions it enforces (lines 19-24,
   36-41 — this file's own convention, distinct from but complementary to `Normalisation.cs`'s
   convention used in Phase 2).

**New methods** — per 04-RESEARCH.md Pattern 4, `Where(f.Id == figureId && f.UserId == userId)`
directly into `ExecuteUpdateAsync`/`ExecuteDeleteAsync` (no existing in-repo analog for these two
EF Core APIs — RESEARCH.md's own code example is the authoritative source for this exact shape,
since no prior phase used `ExecuteUpdateAsync`/`ExecuteDeleteAsync`):
```csharp
public async Task<int> UpdateAsync(int userId, int figureId, Box box)
{
    await using var db = await factory.CreateDbContextAsync();

    return await db.Figures
        .Where(f => f.Id == figureId && f.UserId == userId)
        .ExecuteUpdateAsync(setters => setters
            .SetProperty(f => f.X1, box.X1)
            .SetProperty(f => f.Y1, box.Y1)
            .SetProperty(f => f.X2, box.X2)
            .SetProperty(f => f.Y2, box.Y2));
}

public async Task<int> DeleteAsync(int userId, int figureId)
{
    await using var db = await factory.CreateDbContextAsync();

    return await db.Figures
        .Where(f => f.Id == figureId && f.UserId == userId)
        .ExecuteDeleteAsync();
}
```

---

### `tests/BlazorCanvas.Tests/Data/FigureStoreTests.cs` (test, CRUD)

**Analog:** itself, current on-disk Phase 3 version (full file, 189 lines, read above).

**Existing test-fixture wiring to reuse unchanged** (lines 33-38 — `TestDbContextFactory` adapter
+ `CreateStore()` helper): every new `UpdateAsync`/`DeleteAsync` test calls `CreateStore()`
exactly as every existing test does — no new fixture code needed.
```csharp
private sealed class TestDbContextFactory(DatabaseFixture fixture) : IDbContextFactory<CanvasDbContext>
{
    public CanvasDbContext CreateDbContext() => fixture.CreateContext();
}

private FigureStore CreateStore() => new(new TestDbContextFactory(_fixture));
```
**Existing two-user IDOR test to mirror exactly** (lines 40-66,
`LoadAsync_NeverReturnsAnotherUsersFigures`) — the strongest analog for the new
cross-user-isolation tests `UpdateAsync`/`DeleteAsync` need:
```csharp
[Fact]
public async Task LoadAsync_NeverReturnsAnotherUsersFigures()
{
    var store = CreateStore();

    int userA, userB;
    await using (var context = _fixture.CreateContext())
    {
        userA = await DatabaseFixture.CreateTestUserAsync(context);
        userB = await DatabaseFixture.CreateTestUserAsync(context);
    }

    await store.InsertAsync(userA, FigureType.Rectangle, Rect);
    ...
}
```
Apply the identical two-`CreateTestUserAsync` + cross-check shape for: "userB's `UpdateAsync`
against userA's figure id returns 0 affected rows and does not change the row" and "userB's
`DeleteAsync` against userA's figure id returns 0 affected rows and the row still loads for
userA" — both are the IDOR-mirror tests 04-RESEARCH.md's Pitfall/Threat sections require.

**Existing "insert then assert on the returned/reloaded entity" shape to mirror** (lines 112-129,
`InsertAsync_ReturnsDatabaseAssignedId_PresentInSubsequentLoad`) — the pattern for a new
`UpdateAsync_AffectedRowCount_ReflectsSuccess` test: insert via `store.InsertAsync`, call
`store.UpdateAsync(userId, inserted.Id, newBox)`, assert `affected == 1`, then `LoadAsync` and
assert the reloaded figure's coordinates equal `newBox`. Also add a zero-row case: call
`UpdateAsync`/`DeleteAsync` with a non-existent `figureId` and assert `affected == 0`, mirroring
D-10's "no exception, ever" requirement — this must be a positive assertion (`affected == 0`), not
a `try/catch` (per 04-RESEARCH.md Pitfall 4, this is not an error path).

**Well-formed test boxes to reuse unchanged** (lines 22-25, `Rect`/`SquareCircle` static fields) —
new tests should reuse these constants rather than invent new coordinates, keeping every test's
geometry trivially known-good against the CHECK constraints.

---

## Shared Patterns

### Re-entrancy guard: flag-false-then-capture-locals-before-first-await
**Source:** `src/BlazorCanvas/Components/Pages/Home.razor`, `CommitAsync` (lines 110-123)
**Apply to:** `CommitDragAsync` in `Home.razor` — the exact same shape (`dragging = false` first,
then capture `dragFigureId`/`dragMoved`/`dragCurrentBox` into locals, before the first `await`).

### `pointerleave`, never `pointerout`
**Source:** `src/BlazorCanvas/Components/Pages/Home.razor`, existing SVG-level handlers (line 15,
`@onpointerleave="OnPointerLeave"`)
**Apply to:** the new page-spanning wrapper's `@onpointerleave="OnWrapperPointerLeave"` — same
directive name, same reason (D-37's own explicit warning against `pointerout`).

### `CanvasCoordinates.FromPage`, never `OffsetX`/`OffsetY`
**Source:** `src/BlazorCanvas/Geometry/CanvasCoordinates.cs` (full file, read above) — already the
sole page→canvas mapping function, used once already in `Home.razor` line 69.
**Apply to:** every new pointer-coordinate read this phase adds (`HandleFigurePointerDown`,
`OnWrapperPointerMove`) — call `CanvasCoordinates.FromPage(e.PageX, e.PageY)`, never construct
coordinates any other way.

### `Movement.ClampMove`, never a re-derived clamp
**Source:** `src/BlazorCanvas/Geometry/Movement.cs` (full file, read above), proven by
`tests/BlazorCanvas.Tests/Geometry/ClampTests.cs` (read above, 15 passing cases already covering
every edge case including the CR-02 oversized-box fix).
**Apply to:** `OnWrapperPointerMove` in `Home.razor` — `dragCurrentBox = Movement.ClampMove(dragOriginalBox, dx, dy);`
called with the **original** press-time box and the **total** delta from press, every call — never
an incremental/accumulating variant. No new test file needed for this function; it is already
exhaustively tested.

### `Where(f.Id == figureId && f.UserId == userId)` before every write
**Source:** `src/BlazorCanvas/Data/FigureStore.cs`, `LoadAsync` (line 31, `Where(f => f.UserId == userId)`)
extended by 04-RESEARCH.md Pattern 4 to also filter on `f.Id`.
**Apply to:** `FigureStore.UpdateAsync` and `FigureStore.DeleteAsync` — this is the phase's one
real security control (mirrors Phase 3's `T-03-01` IDOR mitigation for `InsertAsync`'s `userId`
parameter, extended here to filter reads-for-write on both columns).

### Zero-affected-rows is not an error
**Source:** `docs/DECISIONS.md` D-10 (not existing code — this is new application logic, but the
*shape* of "no try/catch, just an `if` branch" is visible in how `Home.razor`'s existing
`CommitAsync` already has zero try/catch anywhere in the file)
**Apply to:** `CommitDragAsync`'s `if (affected == 0) { figures.RemoveAll(...); ... }` and
`HandleDeleteAsync`'s unconditional `figures.RemoveAll(...)` — no message, no exception handling,
no retry logic anywhere in either path.

## No Analog Found

Files/constructs with no close match in the codebase — planner should use RESEARCH.md's Code
Examples/Patterns sections directly:

| File / Construct | Role | Data Flow | Reason |
|-------------------|------|-----------|--------|
| `@onpointerdown:stopPropagation` bound to a C# bool (`FigureShape.razor`) | interaction directive | event-driven | No existing component in this codebase uses any `:stopPropagation`/`:preventDefault` modifier; 04-RESEARCH.md Pattern 1 (citing official Blazor docs) is the authoritative source. |
| Page-spanning wrapper `<div class="app-shell">` with drag-commit handlers (`Home.razor`) | markup structure | event-driven | No existing wrapper-level (as opposed to SVG-level) pointer-event handler exists in the codebase; 04-RESEARCH.md Pattern 2 is the authoritative source, though the *shape* of the handler bodies mirrors the existing SVG-level ones (see Shared Patterns above). |
| `FigureStore.UpdateAsync`/`DeleteAsync` using `ExecuteUpdateAsync`/`ExecuteDeleteAsync` | data-access method | CRUD | No prior phase used these two EF Core APIs (Phase 1-3 only ever `Add`+`SaveChangesAsync`, never `Update`/`Delete`); 04-RESEARCH.md Pattern 4 (citing official EF Core docs) is the authoritative source. The surrounding method *shape* (factory/using/XML-doc) is copied from this same file's existing methods. |

## Metadata

**Analog search scope:** `src/BlazorCanvas/Components/Pages/`, `src/BlazorCanvas/Components/Canvas/`,
`src/BlazorCanvas/Data/`, `src/BlazorCanvas/Geometry/`, `tests/BlazorCanvas.Tests/Data/`,
`tests/BlazorCanvas.Tests/Geometry/`
**Files scanned:** 9 (`Home.razor`, `Home.razor.css`, `FigureShape.razor`, `Toolbar.razor`,
`FigureStore.cs`, `Movement.cs`, `CanvasCoordinates.cs`, `FigureStoreTests.cs`, `ClampTests.cs`)
**Pattern extraction date:** 2026-07-16
