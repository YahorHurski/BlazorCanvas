# Phase 5: Live Cross-Tab Sync - Pattern Map

**Mapped:** 2026-07-16
**Files analyzed:** 7 (2 new source, 2 modified source, 1 new CSS addition, 1 test file, 1 config)
**Analogs found:** 7 / 7

## File Classification

| New/Modified File | Role | Data Flow | Closest Analog | Match Quality |
|-------------------|------|-----------|-----------------|---------------|
| `src/BlazorCanvas/Sync/CanvasSyncNotifier.cs` | service (DI singleton pub/sub) | event-driven | Microsoft's official Blazor notifier pattern (no exact in-repo analog for pub/sub; `FigureStore.cs` used for DI/doc-comment conventions) | role-match (external pattern) + convention-match (in-repo) |
| `src/BlazorCanvas/Sync/SyncMessage.cs` | model (message contract, flat record) | transform | `src/BlazorCanvas/Data/Figure.cs` (flat record/entity shape), `src/BlazorCanvas/Geometry/Box.cs` | exact (record/factory-method style) |
| `src/BlazorCanvas/Components/Pages/Home.razor` (modified) | component (page, drag/gesture state machine) | request-response + event-driven | itself, current on-disk state (Phase 4 shipped) | exact — same file, extend existing handlers |
| `src/BlazorCanvas/Components/Pages/Home.razor.css` (modified) | component style | n/a | `src/BlazorCanvas/Components/Pages/Login.razor.css` (modal surface tokens) | exact (modal token source) |
| `src/BlazorCanvas/Data/FigureStore.cs` | service (data access) | CRUD | itself — unchanged shape; only `Program.cs` DbContext options change | exact (no code edits needed here per RESEARCH) |
| `src/BlazorCanvas/Program.cs` (modified) | config (DI registration, EF options) | n/a | itself, current on-disk state (existing `AddDbContextFactory`/`AddScoped<FigureStore>` block) | exact |
| `tests/BlazorCanvas.Tests/Sync/CanvasSyncNotifierTests.cs` | test | event-driven | `tests/BlazorCanvas.Tests/Data/FigureStoreTests.cs` (xUnit conventions, `[Collection("Database")]`-style fixture use, doc-comment header) | role-match |

## Pattern Assignments

### `src/BlazorCanvas/Sync/CanvasSyncNotifier.cs` (service, event-driven)

**Analog:** Official Microsoft Blazor Server synchronization-context doc pattern (`NotifierService`), adapted per D-11 rule 7 to Singleton + per-`user_id` keying. In-repo convention analog: `src/BlazorCanvas/Data/FigureStore.cs` for doc-comment style (`<summary>` explaining *why*, referencing decision IDs) and constructor-injection-via-primary-constructor style.

**Namespace/DI convention** (mirror `FigureStore.cs` lines 1-18):
```csharp
using BlazorCanvas.Geometry;
using Microsoft.EntityFrameworkCore;

namespace BlazorCanvas.Data;

public sealed class FigureStore(IDbContextFactory<CanvasDbContext> factory)
{
    /// <summary>
    /// ... explains WHY, cites decision IDs (D-xx) ...
    /// </summary>
```
Apply the same primary-constructor + `sealed class` + doc-comment-with-decision-citations style to `CanvasSyncNotifier`.

**Core pub/sub pattern** (from RESEARCH.md Pattern 1, already vetted against the official doc):
```csharp
namespace BlazorCanvas.Sync;

public sealed class CanvasSyncNotifier
{
    private readonly ConcurrentDictionary<int, ConcurrentDictionary<Guid, Action<SyncMessage>>> _subscribers = new();

    public IDisposable Subscribe(int userId, Action<SyncMessage> handler)
    {
        var subscriptionId = Guid.NewGuid();
        var bucket = _subscribers.GetOrAdd(userId, _ => new ConcurrentDictionary<Guid, Action<SyncMessage>>());
        bucket[subscriptionId] = handler;
        return new Subscription(() => bucket.TryRemove(subscriptionId, out _));
    }

    public void Publish(int userId, SyncMessage message)
    {
        if (!_subscribers.TryGetValue(userId, out var bucket)) { return; }
        foreach (var handler in bucket.Values.ToArray()) { handler(message); }
    }

    private sealed class Subscription(Action onDispose) : IDisposable
    {
        private Action? _onDispose = onDispose;
        public void Dispose() => Interlocked.Exchange(ref _onDispose, null)?.Invoke();
    }
}
```

**Registration convention** (mirror `Program.cs` line 25, same block):
```csharp
builder.Services.AddScoped<FigureStore>();
builder.Services.AddSingleton<CanvasSyncNotifier>();   // NEW — note: Singleton, not Scoped
```

---

### `src/BlazorCanvas/Sync/SyncMessage.cs` (model, transform)

**Analog:** `src/BlazorCanvas/Data/Figure.cs` and `src/BlazorCanvas/Geometry/Box.cs` — this project's established "one flat record, no class hierarchy" convention (D-22 for `Box`, same discipline extended to `SyncMessage` per RESEARCH).

**Core pattern** (record with static factory methods per message kind, RESEARCH.md Pattern 3):
```csharp
namespace BlazorCanvas.Sync;

public sealed record SyncMessage(string Kind, Guid Sender, int Id, string? Type, int? X1, int? Y1, int? X2, int? Y2)
{
    public static SyncMessage Draw(Figure f, Guid sender) =>
        new("draw", sender, f.Id, f.Type, f.X1, f.Y1, f.X2, f.Y2);

    public static SyncMessage Move(int id, Box box, Guid sender) =>
        new("move", sender, id, null, box.X1, box.Y1, box.X2, box.Y2);

    public static SyncMessage Delete(int id, Guid sender) =>
        new("delete", sender, id, null, null, null, null, null);

    public static SyncMessage Rollback(int id, Box originalBox, Guid sender) =>
        new("rollback", sender, id, null, originalBox.X1, originalBox.Y1, originalBox.X2, originalBox.Y2);
}
```
Use `sealed record`, matching every other data-shape type in this codebase (`Figure`, `Box`).

---

### `src/BlazorCanvas/Components/Pages/Home.razor` (modified — component, request-response + event-driven)

**Analog:** itself, current on-disk state. This is a targeted extension, not a rewrite — every existing handler stays intact; new code is added alongside.

**Imports/directives pattern to extend** (current lines 1-8):
```razor
@page "/"
@rendermode InteractiveServer
@attribute [Authorize]
@using BlazorCanvas.Tools
@using BlazorCanvas.Components.Canvas
@using BlazorCanvas.Data
@using BlazorCanvas.Geometry
@inject FigureStore Figures
```
Add: `@using BlazorCanvas.Sync`, `@inject CanvasSyncNotifier Notifier`, `@implements IDisposable`.

**Lifecycle subscribe/dispose pattern** (extends existing `OnInitializedAsync`, lines 80-85):
```csharp
private readonly Guid _sender = Guid.NewGuid();
private IDisposable? _subscription;

protected override async Task OnInitializedAsync()
{
    var state = await AuthStateTask!;
    userId = int.Parse(state.User.FindFirst("user_id")!.Value);
    figures = await Figures.LoadAsync(userId);
    _subscription = Notifier.Subscribe(userId, HandleRemoteMessage);
}

public void Dispose() => _subscription?.Dispose();
```

**Drag/throttle-broadcast pattern** (extends existing `OnWrapperPointerMove`, lines 139-162 — clamp logic at line 161 is UNCHANGED, broadcast is appended after it):
```csharp
private long _lastBroadcastTicks = long.MinValue;

private async Task OnWrapperPointerMove(PointerEventArgs e)
{
    if (!dragging) { return; }
    if ((e.Buttons & 1) == 0) { await CommitDragAsync(); return; }

    var point = CanvasCoordinates.FromPage(e.PageX, e.PageY);
    var dx = point.X - dragPressX;
    var dy = point.Y - dragPressY;
    if (!dragMoved) { dragMoved = Math.Sqrt((double)(dx * dx + dy * dy)) >= 3; }

    dragCurrentBox = Movement.ClampMove(dragOriginalBox, dx, dy);   // unchanged, existing line 161

    var now = Environment.TickCount64;
    if (dragMoved && now - _lastBroadcastTicks >= 50)
    {
        _lastBroadcastTicks = now;
        Notifier.Publish(userId, SyncMessage.Move(dragFigureId!.Value, dragCurrentBox, _sender));
    }
}
```

**Commit/write + rollback pattern** (extends existing `CommitDragAsync`, lines 228-258 — reentrancy guard and local mutation at lines 230-247 are UNCHANGED):
```csharp
private async Task CommitDragAsync()
{
    dragging = false;                          // unchanged (existing re-entrancy guard)
    var figureId = dragFigureId;
    var moved = dragMoved;
    var box = dragCurrentBox;
    if (!figureId.HasValue || !moved) { return; }   // unchanged

    var figure = figures.FirstOrDefault(f => f.Id == figureId.Value);
    if (figure is not null) { figure.X1 = box.X1; figure.Y1 = box.Y1; figure.X2 = box.X2; figure.Y2 = box.Y2; }  // unchanged

    Notifier.Publish(userId, SyncMessage.Move(figureId.Value, box, _sender));   // NEW — unconditional trailing edge

    try
    {
        var affected = await Figures.UpdateAsync(userId, figureId.Value, box);   // unchanged call
        if (affected == 0)
        {
            figures.RemoveAll(f => f.Id == figureId.Value);
            if (selectedId == figureId.Value) { selectedId = null; }
            Notifier.Publish(userId, SyncMessage.Delete(figureId.Value, _sender));   // NEW
        }
    }
    catch (Exception)   // see 05-RESEARCH.md Assumptions Log A2 for exact type to (optionally) narrow
    {
        if (figure is not null)
        {
            figure.X1 = dragOriginalBox.X1; figure.Y1 = dragOriginalBox.Y1;
            figure.X2 = dragOriginalBox.X2; figure.Y2 = dragOriginalBox.Y2;
        }
        Notifier.Publish(userId, SyncMessage.Rollback(figureId.Value, dragOriginalBox, _sender));
        showSaveFailedModal = true;
    }
}
```

**Receiving-side apply pattern** (NEW method, follows existing `CommitAsync`/`HandleDeleteAsync` mutation style — direct `List<Figure>` mutation, no LINQ-heavy rewrite):
```csharp
private void HandleRemoteMessage(SyncMessage msg)
{
    _ = InvokeAsync(async () =>
    {
        try
        {
            if (msg.Sender == _sender) { return; }        // echo filter (rule 3)
            if (dragging) { return; }                       // D-54 BLANKET discard (rule 4) — no msg.Id check

            ApplyMessage(msg);
            StateHasChanged();
        }
        catch (ObjectDisposedException) { /* circuit torn down mid-publish; drop silently */ }
    });
}

private void ApplyMessage(SyncMessage msg)
{
    switch (msg.Kind)
    {
        case "draw":
            if (!figures.Any(f => f.Id == msg.Id))
            {
                figures.Add(new Figure { Id = msg.Id, UserId = userId, Type = msg.Type!,
                                          X1 = msg.X1!.Value, Y1 = msg.Y1!.Value, X2 = msg.X2!.Value, Y2 = msg.Y2!.Value });
            }
            break;
        case "move":
        case "rollback":
            var existing = figures.FirstOrDefault(f => f.Id == msg.Id);
            if (existing is null) { return; }               // UPDATE-ONLY, D-40
            existing.X1 = msg.X1!.Value; existing.Y1 = msg.Y1!.Value;
            existing.X2 = msg.X2!.Value; existing.Y2 = msg.Y2!.Value;
            break;
        case "delete":
            figures.RemoveAll(f => f.Id == msg.Id);
            if (selectedId == msg.Id) { selectedId = null; }
            break;
    }
}
```

**Delete restructuring pattern** (mirrors `CommitAsync`'s already-established await-then-mutate-then-broadcast discipline; extends existing `HandleDeleteAsync`, lines 260-274 — currently removes-then-awaits with NO try/catch, this is the one behavioral fix Pitfall 3 calls for):
```csharp
private async Task HandleDeleteAsync()
{
    if (!selectedId.HasValue) { return; }
    var figureId = selectedId.Value;

    try
    {
        await Figures.DeleteAsync(userId, figureId);   // await FIRST now, not after local removal
        selectedId = null;
        figures.RemoveAll(f => f.Id == figureId);
        Notifier.Publish(userId, SyncMessage.Delete(figureId, _sender));   // only after confirmed success (D-39 discipline)
    }
    catch (Exception)
    {
        showSaveFailedModal = true;   // no rollback broadcast needed — nothing was removed/broadcast yet
    }
}
```

**Draw/commit broadcast pattern** (extends existing `CommitAsync`, lines 213-226 — insert-then-add-then-broadcast, matching the already-shipped insert→get-id ordering, D-39):
```csharp
private async Task CommitAsync()
{
    drawing = false;
    var type = previewType;
    var box = previewBox;
    if (!MinSizeGuard.IsDrawable(type, box)) { return; }

    try
    {
        var figure = await Figures.InsertAsync(userId, type, box);   // unchanged call
        figures.Add(figure);                                          // unchanged
        Notifier.Publish(userId, SyncMessage.Draw(figure, _sender));  // NEW — only after confirmed success
    }
    catch (Exception)
    {
        showSaveFailedModal = true;
    }
}
```

**Modal markup pattern** (new markup block, styled as a sibling of `Login.razor.css`'s `.login-card`, structured like `ReconnectModal.razor`'s native `<dialog>` — see Shared Patterns below for the CSS source):
```razor
@if (showSaveFailedModal)
{
    <dialog open class="save-failed-modal" role="alertdialog" aria-modal="true">
        <p>The change could not be saved. The canvas will be reloaded from the database.</p>
        <button type="button" autofocus @onclick="ReloadFromDatabaseAsync">OK</button>
    </dialog>
}

@code {
    private bool showSaveFailedModal;

    private async Task ReloadFromDatabaseAsync()
    {
        figures = await Figures.LoadAsync(userId);
        selectedId = null;
        showSaveFailedModal = false;
    }
}
```
Note: locked copy is verbatim — do not paraphrase (05-UI-SPEC.md § Save-Failure Modal).

---

### `src/BlazorCanvas/Components/Pages/Home.razor.css` (modified)

**Analog:** `src/BlazorCanvas/Components/Pages/Login.razor.css` — the `.login-card` block is the exact token source for the modal surface (per 05-UI-SPEC.md's explicit instruction: sibling of the login card, NOT of `ReconnectModal.razor.css`).

**Tokens to copy from** (`Login.razor.css` lines 8-17, `.login-card`):
```css
.login-card {
    margin: 64px auto 0;
    max-width: 360px;
    background: #FFFFFF;
    border: 1px solid #8B939E;
    border-radius: 4px;
    box-shadow: 0 1px 3px rgba(0, 0, 0, 0.08);
    padding: 32px;
    box-sizing: border-box;
}
```
And the CTA button tokens (`Login.razor.css` lines 82-106, `.cta` / `.cta:hover` / `.cta:active` / `.cta:focus-visible`) — reuse `#1D4ED8` / `#1E40AF` / `#1E3A8A`, 40px height, 4px radius, weight 600.

**Explicitly do NOT copy from** `ReconnectModal.razor.css` — its `#6b9ed2` blue, `0.5rem` radius, `0 3px 6px 2px rgba(0,0,0,0.3)` shadow, and slide-up/fade `@keyframes` are framework-scaffolded defaults foreign to this app's token set (per locked decision UI-05-04). The only structural idea worth borrowing from `ReconnectModal.razor` is "native `<dialog>` element + `::backdrop` pseudo-element," not any of its visual values.

**Backdrop token** (new, from `ReconnectModal.razor.css` line 43-44 pattern, but using this app's dim value):
```css
.save-failed-modal::backdrop {
    background-color: rgba(0, 0, 0, 0.4);
}
```

**No-transition/no-animation rule** (locked — 05-UI-SPEC.md § Motion & Glide + § Save-Failure Modal): do not add any `transition`/`animation` property to `.save-failed-modal`, and none may target `rect`, `line`, `circle`, `polygon` inside `.canvas-surface` in this file.

---

### `src/BlazorCanvas/Data/FigureStore.cs` (unchanged — no analog needed)

RESEARCH.md confirms this file needs **no code changes**: `ExecuteUpdateAsync`/`ExecuteDeleteAsync` automatically inherit the execution-strategy retry configured once in `Program.cs`. Listed here only so the planner does not assign it a plan unnecessarily.

---

### `src/BlazorCanvas/Program.cs` (modified — config)

**Analog:** itself, current on-disk state (line 22-25 block).

**Retry configuration pattern** (extends existing `AddDbContextFactory` call at lines 22-23):
```csharp
builder.Services.AddDbContextFactory<CanvasDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("Canvas"),
        npgsql => npgsql.EnableRetryOnFailure(
            maxRetryCount: 2,
            maxRetryDelay: TimeSpan.FromMilliseconds(200),
            errorCodesToAdd: null)));

builder.Services.AddScoped<FigureStore>();
builder.Services.AddSingleton<CanvasSyncNotifier>();   // NEW
```
Preserve the existing doc-comment style above the `AddDbContextFactory` block (lines 15-21) — extend it, do not delete it, to explain the new retry addition inline.

---

### `tests/BlazorCanvas.Tests/Sync/CanvasSyncNotifierTests.cs` (test)

**Analog:** `tests/BlazorCanvas.Tests/Data/FigureStoreTests.cs`

**Doc-comment header convention** (lines 8-14):
```csharp
/// <summary>
/// Proves the one real security surface this phase introduces: ... (cites decision IDs, cites
/// ROADMAP criterion, explains WHY this test exists, not just what it checks).
/// </summary>
```
Apply the same header style to `CanvasSyncNotifierTests`, citing D-11 (per-user isolation), D-54 (blanket discard is circuit-local, not notifier-level), and the "no leaked subscriber" success criterion.

**Test method naming convention** (lines 40-41, 68-69): `MethodUnderTest_Condition_ExpectedBehavior`, e.g. `Publish_ForDifferentUser_DoesNotInvokeHandler`, `Subscribe_ThenDispose_StopsReceivingMessages`, `Publish_ToMultipleSubscribersOfSameUser_InvokesAll`.

**No DI container / no mocking package convention** (lines 27-38, `TestDbContextFactory` adapter pattern): `CanvasSyncNotifierTests` needs no adapter since `CanvasSyncNotifier` takes no constructor dependencies — instantiate directly with `new CanvasSyncNotifier()`, consistent with this project's "no DI/mocking package" discipline (D-49).

This test class does NOT need `[Collection("Database")]` or `DatabaseFixture` — it is pure in-memory logic with no database dependency, unlike `FigureStoreTests`.

---

## Shared Patterns

### Doc-comment style (decision-citing, "why not what")
**Source:** `src/BlazorCanvas/Data/FigureStore.cs` (class-level and method-level `<summary>` blocks)
**Apply to:** `CanvasSyncNotifier.cs`, `SyncMessage.cs`, new/modified `Home.razor` methods, `Program.cs` additions
```csharp
/// <summary>
/// ... explains the design rationale, cites D-xx decision numbers, states what would go wrong
/// without this code ...
/// </summary>
```

### IDbContextFactory short-lived-context pattern (unchanged, reused as-is)
**Source:** `src/BlazorCanvas/Data/FigureStore.cs` lines 26-35, `Program.cs` lines 15-23
**Apply to:** No new call sites this phase — `FigureStore.UpdateAsync`/`DeleteAsync`/`InsertAsync`/`LoadAsync` are called exactly as before from `Home.razor`; only `Program.cs`'s factory options change.

### IDisposable component lifecycle (subscribe in OnInitializedAsync, unsubscribe in Dispose)
**Source:** Official Microsoft Blazor Server pattern (no existing in-repo analog — this is genuinely new to the codebase); doc-comment convention from `FigureStore.cs`
**Apply to:** `Home.razor` — `@implements IDisposable`, `_subscription = Notifier.Subscribe(...)` in `OnInitializedAsync`, `_subscription?.Dispose()` in `Dispose()`.

### InvokeAsync marshalling for cross-circuit callbacks
**Source:** Official Microsoft Blazor Server pattern, wrapped per RESEARCH.md Pitfall 1
**Apply to:** `Home.razor.HandleRemoteMessage` — always call `_ = InvokeAsync(() => { ... })`, wrapped in `try/catch (ObjectDisposedException)`.

### Modal visual tokens (login-card sibling, NOT ReconnectModal)
**Source:** `src/BlazorCanvas/Components/Pages/Login.razor.css` (`.login-card`, `.cta`)
**Apply to:** New `.save-failed-modal` rules in `Home.razor.css`. Do NOT source from `ReconnectModal.razor.css`.

### Zero-row-is-not-an-error framing (existing Phase 4 discipline, extended)
**Source:** `src/BlazorCanvas/Data/FigureStore.cs` lines 63-68 (`UpdateAsync` doc comment), `Home.razor` lines 248-258 (existing `if (affected == 0)` branch)
**Apply to:** `CommitDragAsync`'s existing zero-row branch — add exactly one line (`Notifier.Publish(..., SyncMessage.Delete(...))`), do not otherwise alter the branch's shape.

### Reentrancy-guard-first pattern (`dragging = false` before any await)
**Source:** `Home.razor` lines 230, existing `CommitDragAsync` (Phase 4 established, cited directly in 05-RESEARCH.md Secondary Sources)
**Apply to:** Keep unchanged; every new addition to `CommitDragAsync` must come after this line, never before.

## No Analog Found

None — every file in this phase either has a direct in-repo predecessor (`Home.razor`, `Program.cs`, `FigureStore.cs`, `FigureStoreTests.cs`) or a directly-cited, fetched-this-session official Microsoft pattern for the one genuinely new construct (`CanvasSyncNotifier`'s pub/sub shape).

## Metadata

**Analog search scope:** `src/BlazorCanvas/` (Data, Components/Pages, Components/Layout, Geometry), `tests/BlazorCanvas.Tests/Data`, `Program.cs`
**Files scanned:** `Home.razor`, `Home.razor.css`, `FigureStore.cs`, `Figure.cs` (referenced), `Box.cs` (referenced), `Program.cs`, `Login.razor.css`, `ReconnectModal.razor`, `ReconnectModal.razor.css`, `FigureStoreTests.cs`
**Pattern extraction date:** 2026-07-16
