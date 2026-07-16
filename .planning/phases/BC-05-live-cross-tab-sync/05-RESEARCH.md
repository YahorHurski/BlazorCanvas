# Phase 5: Live Cross-Tab Sync - Research

**Researched:** 2026-07-16
**Domain:** Blazor Server cross-circuit pub/sub (DI singleton notifier), throttled real-time state broadcast, EF Core/Npgsql transient-failure retry and rollback
**Confidence:** HIGH

## Summary

This phase adds exactly one new piece of infrastructure — an in-memory DI **singleton** notifier keyed
by `user_id` — and wires it into code paths that Phase 4 already shipped and left ready for exactly
this purpose. `Home.razor` already has `dragging`, `dragOriginalBox` (explicitly commented *"retained
until the drag commits, so Phase 5 can use it as D-52's rollback source"*), `dragCurrentBox`,
`CommitDragAsync`, and `HandleDeleteAsync` — none of which currently broadcast anything or handle a
write failure. `FigureStore.UpdateAsync`/`DeleteAsync` use `ExecuteUpdateAsync`/`ExecuteDeleteAsync`
correctly for the zero-row case (D-10) but have **zero exception handling** — a transient database
error today would crash the circuit, which is exactly the D-45/D-52 gap this phase closes.

No new NuGet package is required. The official Microsoft Blazor Server pattern for "a component reacts
to an external singleton service's event, unsubscribes in `Dispose()`, and calls
`InvokeAsync(StateHasChanged)`" is documented and current (fetched directly this session,
`aspnetcore-10.0` moniker, updated 2025-11-11) — it is a direct, almost line-for-line match for D-11's
"irreducible core" list. The one adaptation needed is that the official example registers its notifier
as **Scoped** (single-user, single-circuit); this phase's notifier must be **Singleton** and keyed by
`user_id` so it bridges across the *different* DI scopes each open tab's circuit gets — this is exactly
D-11 core rule 7 and is a deliberate, locked departure from the doc's own default.

The second major finding: **the "guaranteed trailing edge" throttle needs no timer, `PeriodicTimer`, or
background thread at all.** Because Blazor Server dispatches one circuit event at a time and every
`pointermove` is already a synchronous server round-trip (D-07), the throttle is simply a
"time-since-last-broadcast" gate checked inline inside the existing `OnWrapperPointerMove` handler,
plus one **unconditional, un-throttled** broadcast call added to the existing `CommitDragAsync` — which
already runs exactly once, synchronously, on every drop/leave/Alt-Tab commit. This is simpler than every
timer-based throttle pattern found in research and introduces no new concurrency to reason about.

The third major finding, confirmed against Npgsql's own EF Core docs: `EnableRetryOnFailure(maxRetryCount:
2, ...)` on the Npgsql provider is the correct, non-hand-rolled mechanism for D-52's "retry up to 2
additional times, only if transient." It relies on `NpgsqlException.IsTransient`, which already excludes
CHECK-constraint violations and other non-transient PostgreSQL errors — so **no manual exception-type
classification code is needed anywhere in this app.** The only application code required is a single
`try/catch` around the call site that needs D-52's rollback behavior, catching whatever ultimately
escapes after the execution strategy's own retries are exhausted (or immediately, for a non-transient
error). This is flagged as the phase's one genuine open question: whether that escaping exception is a raw
`Npgsql.PostgresException`/`NpgsqlException` or a wrapped `Microsoft.EntityFrameworkCore.DbUpdateException`
for `ExecuteUpdateAsync`/`ExecuteDeleteAsync` specifically is inconsistently described even in
authoritative-adjacent sources — see Assumptions Log A2.

**Primary recommendation:** Add a new `CanvasSyncNotifier` DI singleton (`Subscribe(userId, handler) ->
IDisposable`, `Publish(userId, SyncMessage)`), a `SyncMessage` record matching D-53's contract exactly,
register `EnableRetryOnFailure(maxRetryCount: 2, ...)` once in `Program.cs`'s existing
`AddDbContextFactory<CanvasDbContext>` call, and extend `Home.razor`'s already-shipped drag/delete/draw
handlers to broadcast, subscribe/unsubscribe, and catch-and-roll-back — without touching any geometry,
clamp, or persistence-shape code that Phases 1–4 already built and tested.

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| Cross-tab message delivery (subscribe/publish) | Frontend Server (in-process DI singleton, per ASP.NET Core process) | — | Pure in-memory pub/sub inside the same process that hosts every circuit for this app (D-11 explicitly rejects SignalR hubs, `LISTEN`/`NOTIFY`, and any cross-process mechanism as unnecessary) |
| Drag-glide broadcast + 50ms trailing-edge throttle | Frontend Server (per-circuit C# state in `Home.razor`) | — | Every `pointermove` is already a server round-trip (D-07); the throttle gate and the unconditional final broadcast are both pure C# logic with no I/O |
| Echo filter / mid-drag blanket discard | Frontend Server (per-circuit C# state) | — | D-53's `sender` GUID and D-54's `if (_dragging) return;` are both local circuit state, never touching the database or another tier |
| `move` UPDATE-ONLY apply + zero-row->delete broadcast | Frontend Server (apply logic) | Database / Storage (affected-row count) | The receiving tab's apply logic is pure C#; the zero-row *fact* that triggers a delete broadcast on the *sending* side comes from `ExecuteUpdateAsync`'s return value, a genuine DB-tier signal |
| Save-failure retry (transient only) | API / Backend (`FigureStore` + Npgsql execution strategy) | Database / Storage | `NpgsqlRetryingExecutionStrategy` operates directly against the Npgsql connection/command layer; this is backend-tier resilience configuration, not application logic |
| Rollback broadcast + modal + reload-from-DB | Frontend Server (broadcast + modal state) | Database / Storage (the reload read) | The broadcast and modal are circuit-local UI state; "reload from PostgreSQL on OK" is a second, ordinary `FigureStore.LoadAsync` call — no new read path needed |

## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| SYNC-01 | DI singleton notifier keyed by `user_id`; subscribe on init, unsubscribe in `Dispose()`; `draw`/`move`/`delete`/`rollback` kinds, no `drop`; `move` UPDATE-ONLY; 50ms throttle with guaranteed trailing edge; clamp→render→broadcast order; echo filter via per-circuit `sender` GUID; mid-drag blanket discard; zero-row UPDATE broadcasts delete; no locking/CRDT | Pattern 1 (official Microsoft notifier pattern, adapted to Singleton + per-user keying); Pattern 2 (throttle-with-guaranteed-trailing-edge needs no timer); Pattern 3 (message contract record + apply logic); Code Examples section |
| DATA-03 | Stale tab never operates on a gone figure; affected-row counts checked on every write; zero-row UPDATE silently removes locally **and broadcasts delete** so ghost-holding tabs drop it too | Already half-built in Phase 4 (`CommitDragAsync`'s `if (affected == 0)` branch exists) — Phase 5 adds exactly one line: broadcast `delete` in that branch. Pattern 3, Code Examples |
| DATA-04 | Transient failures retry ≤2 more times; non-transient never retried; on final failure broadcast `rollback` with original coordinates, restore locally, show the exact modal, reload from PostgreSQL on OK; app stays alive | Pattern 4 (`EnableRetryOnFailure`, no hand-rolled classification); Pattern 5 (rollback sequencing); Common Pitfalls 3–5; Assumptions Log A2 (the one open question: exact exception type to catch) |

## Standard Stack

No new packages this phase. Every capability is built from packages already present in
`src/BlazorCanvas/BlazorCanvas.csproj` (`Npgsql.EntityFrameworkCore.PostgreSQL` for
`EnableRetryOnFailure`; `Microsoft.AspNetCore.Components.Web` for `PointerEventArgs`, already used by
Phase 3/4) plus the .NET base class library (`System.Collections.Concurrent`, no package needed) and the
app's own Phase 1–4 code (`Movement.ClampMove`, `Box`, `CanvasCoordinates`, `FigureStore`).

**Version verification:**
```
$ dotnet list src/BlazorCanvas/BlazorCanvas.csproj package
> Npgsql.EntityFrameworkCore.PostgreSQL   10.0.3    10.0.3
> Microsoft.EntityFrameworkCore.Design    10.0.10   10.0.10
```
`[VERIFIED: dotnet list package, run directly against this repo this session]`. `EnableRetryOnFailure`
has existed on `NpgsqlDbContextOptionsBuilder` since early Npgsql EF Core provider versions and is
present and documented for the installed 10.0.3 — confirmed against
`https://www.npgsql.org/efcore/misc/other.html` (fetched via search this session).
`[CITED: npgsql.org/efcore/misc/other.html]`.

**Installation:** none required — no `dotnet add package` command needed for this phase.

## Package Legitimacy Audit

Not applicable — this phase installs no new packages. The only "new" symbols
(`ConcurrentDictionary<TKey,TValue>`, `Environment.TickCount64`) are .NET BCL types, not NuGet packages.
`EnableRetryOnFailure` is an existing member of the already-installed, already-vetted
`Npgsql.EntityFrameworkCore.PostgreSQL` 10.0.3 package (Phase 1).

## Architecture Patterns

### System Architecture Diagram

```
Tab A circuit (Home.razor instance A)                  Tab B circuit (Home.razor instance B)
  _senderA = Guid (created once, OnInitializedAsync)      _senderB = Guid
  |                                                          |
  | OnInitializedAsync: Notifier.Subscribe(userId, ...)      | OnInitializedAsync: Notifier.Subscribe(userId, ...)
  v                                                          v
  +----------------------------------------------------------------------------------+
  |                    CanvasSyncNotifier  (DI Singleton, ONE per process)            |
  |   ConcurrentDictionary<int userId, ConcurrentDictionary<Guid subId, handler>>     |
  +----------------------------------------------------------------------------------+
  ^                                                          ^
  | Publish(userId, SyncMessage)                             | handler invoked on Tab A's
  | called from Tab B's circuit thread                        | PUBLISHING thread (Tab B's) --
  | during Tab B's drag (throttled) or commit (unconditional) | must marshal back via InvokeAsync
  |                                                          |
Tab B: OnWrapperPointerMove (drag in progress)          Tab A: HandleRemoteMessage(msg)
  clamp (Movement.ClampMove, D-36)                         if msg.Sender == _senderA: return        (rule 3, echo)
  render (dragCurrentBox assigned; Blazor auto-rerenders)   _ = InvokeAsync(() => {
  if elapsed >= 50ms since last broadcast:                    if (dragging) return;                  (rule 4, D-54 BLANKET)
      broadcast move(id, x1,y1,x2,y2, _senderB)                ApplyMessage(msg);   (idempotent, move=UPDATE-ONLY, D-40)
                                                                StateHasChanged();
Tab B: CommitDragAsync (drop / pointerleave / Alt-Tab)       })
  dragging = false (re-entrancy guard, unchanged from P4)
  if !moved: return (click, no write, no broadcast)
  mutate local figure box (optimistic local render)
  broadcast FINAL move UNCONDITIONALLY (bypasses throttle -- guaranteed trailing edge, D-47)
  try {
      affected = await Figures.UpdateAsync(userId, id, box)   -- Npgsql EnableRetryOnFailure retries
                                                                  transient failures transparently
      if affected == 0:
          remove figure locally; broadcast delete(id, _senderB)      (D-10 + D-40 rule 9)
  } catch (write ultimately failed -- transient exhausted, or non-transient) {
      restore local figure to dragOriginalBox
      broadcast rollback(id, dragOriginalBox, _senderB)              (D-52)
      show modal "The change could not be saved..."
      on OK: figures = await Figures.LoadAsync(userId)                (reload from Postgres)
  }
```

### Recommended Project Structure

```
src/BlazorCanvas/
├── Sync/
│   ├── CanvasSyncNotifier.cs      # NEW - DI singleton, Subscribe/Publish, keyed by user_id
│   └── SyncMessage.cs             # NEW - the D-53 message contract as one flat record
├── Data/FigureStore.cs            # unchanged shape; Program.cs configures retry on the factory instead
├── Components/Pages/Home.razor    # + sender Guid, @implements IDisposable, subscribe/unsubscribe,
│                                   #   broadcast calls in OnWrapperPointerMove/CommitDragAsync/
│                                   #   HandleDeleteAsync/CommitAsync, HandleRemoteMessage, ApplyMessage,
│                                   #   try/catch + rollback + modal state
├── Components/Pages/Home.razor.css # + modal overlay styling (new, small)
├── Program.cs                     # + builder.Services.AddSingleton<CanvasSyncNotifier>()
│                                   # + options.UseNpgsql(cs, npgsql => npgsql.EnableRetryOnFailure(2, ...))
tests/BlazorCanvas.Tests/
├── Sync/CanvasSyncNotifierTests.cs # NEW - subscribe/publish/dispose/multi-subscriber/cross-user isolation
```

### Pattern 1: The DI singleton notifier — official Microsoft shape, adapted to per-user keying

**What:** Microsoft's own "Invoke component methods externally to update state" example (fetched directly
this session, `aspnetcore-10.0`, updated 2025-11-11) is the exact shape D-11 asks for: a service raising
notifications, a component subscribing in `OnInitialized`, unsubscribing in `Dispose()`, and marshalling
back with `InvokeAsync`. The one adaptation: their example registers the notifier **Scoped** ("For
server-side development, register the services as scoped") because their scenario is single-user. This
phase's notifier **must be Singleton** and **keyed by `user_id`**, because the whole point is bridging
*different* circuits (different DI scopes) that happen to share a `user_id` — a Scoped registration would
give every circuit its own private instance and nothing would ever cross a tab boundary.

**When to use:** Any Blazor Server cross-circuit broadcast where the recipients are not enumerable in
advance and must be able to come and go (open/close tabs) without the publisher needing to know who is
currently listening.

**Example:**
```csharp
// Sync/CanvasSyncNotifier.cs
using System.Collections.Concurrent;

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
        if (!_subscribers.TryGetValue(userId, out var bucket))
        {
            return;
        }

        // Snapshot before iterating: ConcurrentDictionary tolerates concurrent mutation during
        // enumeration without throwing, but a snapshot avoids invoking a handler that unsubscribed
        // a moment ago and keeps Publish's cost independent of concurrent Dispose() calls.
        foreach (var handler in bucket.Values.ToArray())
        {
            handler(message);
        }
    }

    private sealed class Subscription(Action onDispose) : IDisposable
    {
        private Action? _onDispose = onDispose;

        public void Dispose() => Interlocked.Exchange(ref _onDispose, null)?.Invoke();
    }
}
```
```csharp
// Program.cs -- one new line alongside the existing FigureStore registration
builder.Services.AddSingleton<CanvasSyncNotifier>();
```
```razor
@* Home.razor -- subscribe/unsubscribe, mirroring the official pattern exactly *@
@implements IDisposable
@inject CanvasSyncNotifier Notifier

@code {
    private readonly Guid _sender = Guid.NewGuid();   // per-circuit, created once (D-53)
    private IDisposable? _subscription;

    protected override async Task OnInitializedAsync()
    {
        var state = await AuthStateTask!;
        userId = int.Parse(state.User.FindFirst("user_id")!.Value);
        figures = await Figures.LoadAsync(userId);
        _subscription = Notifier.Subscribe(userId, HandleRemoteMessage);   // D-11 rule 1 setup
    }

    public void Dispose() => _subscription?.Dispose();   // D-11 rule 1: unsubscribe in Dispose()
}
```
Source: [ASP.NET Core Blazor synchronization context — "Invoke component methods externally to update
state"](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/synchronization-context?view=aspnetcore-10.0)
`[VERIFIED: learn.microsoft.com, fetched directly this session, aspnetcore-10.0 moniker, ms.date
2025-11-11]` — the article's own `NotifierService`/`TimerService`/`Notifications.razor` triad is the
direct ancestor of this pattern; only the registration lifetime (Singleton vs. their Scoped) and the
per-user keying are this app's own addition, required by D-11 rule 7.

> ⚠️ **Caveat found in the same doc, load-bearing:** *"A component is re-entrant at any point where it
> awaits an incomplete Task... [lifecycle methods] may be called before the asynchronous control flow
> resumes."* Combined with the disposal race described in Pattern 1's own threat model (D-11 rule 1's
> stated failure mode is `ObjectDisposedException`), `HandleRemoteMessage`'s `InvokeAsync` call should be
> wrapped defensively — see Common Pitfall 1.

### Pattern 2: The 50ms throttle with guaranteed trailing edge — no timer needed

**What:** D-47 requires "throttle to at most one per 50ms" **and** "the final position is always sent
before the drop." Every timer-based throttle/debounce pattern found in research (`PeriodicTimer`,
`System.Threading.RateLimiting`, a background `Task.Delay` loop) is unnecessary here, because Blazor
Server already delivers `pointermove` events one at a time on the circuit's single logical thread (per
the official synchronization-context doc fetched above), and the "end" of a drag is already a distinct,
synchronously-reached point in the existing code (`CommitDragAsync`). The throttle is therefore just an
inline elapsed-time gate plus one unconditional call at the known end-point — no concurrency, no
disposal risk, no second moving part.

**When to use:** Any "send at most once per N ms, but always send the final value" requirement where the
triggering events are already serialized on a single logical thread and the "final" event is a
distinguishable, already-existing code path (as opposed to "stops arriving," which would need a real
debounce timer).

**Example:**
```csharp
// Home.razor @code -- extends the EXISTING OnWrapperPointerMove (unchanged clamp/threshold logic above it)
private long _lastBroadcastTicks = long.MinValue;

private async Task OnWrapperPointerMove(PointerEventArgs e)
{
    if (!dragging) { return; }
    if ((e.Buttons & 1) == 0) { await CommitDragAsync(); return; }

    var point = CanvasCoordinates.FromPage(e.PageX, e.PageY);
    var dx = point.X - dragPressX;
    var dy = point.Y - dragPressY;
    if (!dragMoved) { dragMoved = Math.Sqrt((double)(dx * dx + dy * dy)) >= 3; }

    dragCurrentBox = Movement.ClampMove(dragOriginalBox, dx, dy);   // 1. CLAMP (D-36, unchanged)
                                                                      // 2. RENDER happens for free: Blazor
                                                                      //    re-renders at the end of this
                                                                      //    handler because dragCurrentBox
                                                                      //    changed (existing @foreach reads it)
    var now = Environment.TickCount64;                               // 3. BROADCAST -- last, per D-36
    if (dragMoved && now - _lastBroadcastTicks >= 50)
    {
        _lastBroadcastTicks = now;
        Notifier.Publish(userId, SyncMessage.Move(dragFigureId!.Value, dragCurrentBox, _sender));
    }
}

private async Task CommitDragAsync()
{
    dragging = false;
    var figureId = dragFigureId;
    var moved = dragMoved;
    var box = dragCurrentBox;
    if (!figureId.HasValue || !moved) { return; }   // a click: no write, no broadcast (D-48)

    var figure = figures.FirstOrDefault(f => f.Id == figureId.Value);
    if (figure is not null) { figure.X1 = box.X1; figure.Y1 = box.Y1; figure.X2 = box.X2; figure.Y2 = box.Y2; }

    // UNCONDITIONAL final broadcast -- bypasses the 50ms gate entirely. This IS D-47's "guaranteed
    // trailing edge": it is not "the most recent throttled move," it is a distinct, always-sent message.
    Notifier.Publish(userId, SyncMessage.Move(figureId.Value, box, _sender));

    // ... UpdateAsync + try/catch + rollback -- see Pattern 4/5 ...
}
```
**Why this satisfies "throttle, not debounce" (D-47's own distinction):** intermediate positions ARE sent
during the drag (gated only by elapsed time, never suppressed until the drag ends) — a debounce would
send nothing until `CommitDragAsync`, which D-47 explicitly rejects ("a debounce would show nothing until
the drag ended, defeating the whole point").

### Pattern 3: The message contract as one flat record, and idempotent apply keyed by figure Id

**What:** D-53's four kinds share one shape closely enough to model as a single record with nullable
geometry fields, mirroring D-22's own "one flat C# record, no class hierarchy" preference for `Figure`
itself.

**Example:**
```csharp
// Sync/SyncMessage.cs
namespace BlazorCanvas.Sync;

public sealed record SyncMessage(string Kind, Guid Sender, int Id, string? Type, int? X1, int? Y1, int? X2, int? Y2)
{
    public static SyncMessage Draw(Figure f, Guid sender) =>
        new("draw", sender, f.Id, f.Type, f.X1, f.Y1, f.X2, f.Y2);

    public static SyncMessage Move(int id, Box box, Guid sender) =>
        new("move", sender, id, null, box.X1, box.Y1, box.X2, box.Y2);   // move carries no Type (D-53)

    public static SyncMessage Delete(int id, Guid sender) =>
        new("delete", sender, id, null, null, null, null, null);

    public static SyncMessage Rollback(int id, Box originalBox, Guid sender) =>
        new("rollback", sender, id, null, originalBox.X1, originalBox.Y1, originalBox.X2, originalBox.Y2);
}
```
```csharp
// Home.razor @code -- the receiving side, called only from inside InvokeAsync (see Pattern 1's caveat
// and Common Pitfall 1 for the wrapping this call needs)
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
        case "rollback":                                            // rollback applies exactly like move (D-53)
            var existing = figures.FirstOrDefault(f => f.Id == msg.Id);
            if (existing is null) { return; }                        // UPDATE-ONLY: unknown figure -> ignore ENTIRELY (D-40)
            existing.X1 = msg.X1!.Value; existing.Y1 = msg.Y1!.Value;
            existing.X2 = msg.X2!.Value; existing.Y2 = msg.Y2!.Value;
            break;

        case "delete":
            figures.RemoveAll(f => f.Id == msg.Id);                  // idempotent no-op if already gone
            if (selectedId == msg.Id) { selectedId = null; }
            break;
    }
}
```
**Why `draw` must check `!figures.Any(...)` before adding:** unlike `move`, `draw` IS allowed to insert
— but two tabs could each hold a local copy already (the originating tab already added it locally in
`CommitAsync` before broadcasting) — this guard just makes the apply idempotent for `draw`, exactly as
D-53 requires ("insert or update by id").

### Pattern 4: Retry via `EnableRetryOnFailure` — no hand-rolled transient classification

**What:** Configure the Npgsql provider's built-in retrying execution strategy once, at the single point
where `CanvasDbContext` options are built. Every `FigureStore` method automatically inherits it — no
per-call retry loop, no manual `catch (PostgresException ex) when (ex.SqlState == "...")` needed anywhere.

**Example:**
```csharp
// Program.cs -- extends the EXISTING AddDbContextFactory call, one new lambda parameter
builder.Services.AddDbContextFactory<CanvasDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("Canvas"),
        npgsql => npgsql.EnableRetryOnFailure(
            maxRetryCount: 2,                              // D-52: "up to 2 additional times"
            maxRetryDelay: TimeSpan.FromMilliseconds(200),  // D-52: "short delays"
            errorCodesToAdd: null)));
```
**Why no application code needs to classify transient-vs-not:** `NpgsqlRetryingExecutionStrategy`
decides via `NpgsqlException.IsTransient`, which is false for CHECK-constraint violations (SQLSTATE class
`23`, integrity constraint violation) and true for connection-level failures — this is exactly D-52's
"never retry validation errors, CHECK violations" boundary, enforced by the provider, not by app code.
`[CITED: npgsql.org/efcore/misc/other.html + npgsql.org/efcore/api/...NpgsqlRetryingExecutionStrategy.html]`

> ⚠️ **Known classification gaps, reported against Npgsql/EF Core (community GitHub issues, not official
> docs — treat as a documented risk, not a blocker):** a deadlock (`40P01`) has been reported as
> `IsTransient = false` even though deadlocks are conceptually transient, and a command timeout has been
> reported as falsely `IsTransient = true`. **Neither is reachable in this app's normal operation**: D-11's
> one-mouse premise means there is no concurrent writer to the same row (no deadlock possible), and this
> app's writes are single-row point UPDATEs/DELETEs with no long-running query (a command timeout would
> indicate the database is genuinely down, at which point retry-then-rollback is the correct outcome
> either way). Recorded so the planner does not spend effort defending against an unreachable case.

### Pattern 5: The rollback sequencing — broadcast happens before the DB call, not after

**What:** D-52's own stated rationale ("the drag-glide broadcasts have already gone out ... every other
tab already shows the figure in its new position — while the database still holds the old one") means
the **final `move` broadcast is optimistic** — sent at drop time, before the `UPDATE` is even attempted,
exactly like every earlier throttled `move` during the glide. Only a **subsequent** failure produces a
**second** message (`rollback`) that corrects it. This is not a new design choice; it is the only reading
of D-52's text that is internally consistent with D-47's "final position always sent" guarantee.

**Example (completes `CommitDragAsync` from Pattern 2):**
```csharp
private async Task CommitDragAsync()
{
    // ... unchanged reentrancy-guard capture, unmoved-click return, local mutation ...
    // ... unconditional final move broadcast (Pattern 2) ...

    try
    {
        var affected = await Figures.UpdateAsync(userId, figureId.Value, box);
        if (affected == 0)
        {
            figures.RemoveAll(f => f.Id == figureId.Value);
            if (selectedId == figureId.Value) { selectedId = null; }
            Notifier.Publish(userId, SyncMessage.Delete(figureId.Value, _sender));   // D-40 rule 9
        }
    }
    catch (Exception)   // see Assumptions Log A2 for the exact exception type to narrow this to
    {
        // All retry attempts exhausted (or a non-transient failure occurred immediately).
        if (figure is not null)
        {
            figure.X1 = dragOriginalBox.X1; figure.Y1 = dragOriginalBox.Y1;
            figure.X2 = dragOriginalBox.X2; figure.Y2 = dragOriginalBox.Y2;
        }
        Notifier.Publish(userId, SyncMessage.Rollback(figureId.Value, dragOriginalBox, _sender));
        showSaveFailedModal = true;   // renders the D-52 modal; OK handler reloads:
                                       // figures = await Figures.LoadAsync(userId); showSaveFailedModal = false;
    }
}
```
**Why `dragOriginalBox` (not `box`) is the rollback payload:** it is retained unmutated for the *entire*
drag (Phase 4's own comment: *"retained until the drag commits, so Phase 5 can use it as D-52's rollback
source"*) — this is precisely why Phase 4 never overwrote it during the glide, only `dragCurrentBox`.

### Anti-Patterns to Avoid

- **A `PeriodicTimer` or `System.Threading.RateLimiting` limiter for the drag throttle.** Unnecessary
  complexity and a second thread to reason about — see Pattern 2. Blazor Server's own event-serialization
  already gives this phase everything it needs.
- **Registering `CanvasSyncNotifier` as Scoped**, copying the official Microsoft doc's example literally.
  A Scoped registration gives every circuit its own private instance and nothing crosses a tab boundary —
  this single lifetime mismatch would silently make cross-tab sync a no-op while looking correct in code
  review (no compile error, no runtime error — the feature would just never fire).
- **Broadcasting the raw, unclamped `dx`/`dy` or the pre-clamp cursor position.** D-36's own explicit
  requirement: clamp → render → broadcast, in that order, every time.
- **A per-figure or global lock, mutex, or semaphore anywhere in the notifier or the apply logic.** D-11's
  entire premise is that these are unnecessary; adding one would be actively regressive and contradict a
  locked decision.
- **Checking `msg.Sender != _sender` with `==` on a boxed/reference-typed sender field instead of `Guid`
  value equality.** Use `Guid` (a value type) for `Sender`, not `object`/`string` comparison pitfalls.
- **Retrying inside `FigureStore` with a hand-written loop** (e.g., `for (var i = 0; i < 3; i++) { try {
  ...} catch { await Task.Delay(...); } }`). This duplicates what `EnableRetryOnFailure` already does
  correctly and risks double-retrying (the hand-written loop retrying an already-retried operation) or
  retrying a non-transient error the built-in strategy would have correctly skipped.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|--------------|-----|
| Transient-vs-permanent PostgreSQL error classification | A hand-written `switch` on `PostgresException.SqlState` | `NpgsqlDbContextOptionsBuilder.EnableRetryOnFailure()` (`NpgsqlException.IsTransient`) | Already ships the exact classification D-52 wants (CHECK violations excluded, connection failures included); a hand-rolled version is a second, divergence-prone source of truth for the same list |
| Cross-circuit "is this component still alive" tracking | A custom circuit-registry / heartbeat mechanism | `Dispose()` unsubscribing from `CanvasSyncNotifier` + the official `InvokeAsync` pattern | D-11's own "Rejected" list explicitly declines `CircuitHandler`-based reconnect-detection plumbing as more than this app needs |
| The drag-glide throttle | Any timer/debounce library (`System.Reactive`, `System.Threading.RateLimiting`) | An inline elapsed-ticks gate + one unconditional final call (Pattern 2) | Blazor Server's synchronous per-circuit event dispatch already serializes everything a timer would exist to serialize |
| The edge-clamp during the glide | A second clamp implementation for "broadcast position" vs. "render position" | `Movement.ClampMove` (Phase 1, already tested) — the SAME call already made for rendering | D-36: clamp → render → broadcast is one clamp result reused for both, never two independent clamps that could disagree |
| Ownership/ IDOR filtering on the write path | Anything new | The existing `Where(f.Id == figureId && f.UserId == userId)` filter in `UpdateAsync`/`DeleteAsync` (Phase 4) | Unchanged by this phase; the notifier operates entirely after a write has already passed this filter |

**Key insight:** every piece of *new* infrastructure this phase needs (the notifier, the throttle gate,
the retry configuration) is either a documented, current Microsoft/Npgsql pattern or a direct
consequence of one already-shipped Phase 4 design decision (retaining `dragOriginalBox` for the whole
drag). There is no genuinely novel algorithm to invent in this phase.

## Common Pitfalls

### Pitfall 1: `ObjectDisposedException` from a publish racing a dispose

**What goes wrong:** Tab A closes (its `Home.razor` disposes, unsubscribing). Microseconds earlier, Tab
B's `CommitDragAsync` had already called `Notifier.Publish`, which took a snapshot of Tab A's handler and
is about to invoke it. The handler calls `InvokeAsync(...)` on a component whose circuit has already been
torn down — Blazor Server throws `ObjectDisposedException` from `InvokeAsync` in this exact race.

**Why it happens:** `Dispose()` guarantees no *future* publish reaches you; it cannot retroactively cancel
a publish that already grabbed your handler reference microseconds earlier. This is a genuine, small race
window inherent to any multi-circuit pub/sub, not a bug in the notifier's collection design.

**How to avoid:** wrap the body of `HandleRemoteMessage`'s `InvokeAsync` call in a
`try { ... } catch (ObjectDisposedException) { }` — silently drop the message; the tab is gone, there is
nothing to update. This is defense-in-depth *alongside* (not instead of) unsubscribing in `Dispose()`,
which remains the primary mechanism that makes this race rare rather than routine.

**Warning signs:** an intermittent, hard-to-reproduce circuit crash logged only when a tab is closed at
almost exactly the same instant another tab is mid-drag.

### Pitfall 2: Forgetting the mid-drag rule is a BLANKET discard, not a per-figure filter

**What goes wrong:** implementing `HandleRemoteMessage` as
`if (_dragging && msg.Id == dragFigureId) return;` — i.e., only ignoring messages about the figure
currently being dragged, and applying everything else (an unrelated draw/delete from another device)
immediately.

**Why it happens:** it reads as the more "correct," least-lossy behavior, and it is exactly what D-11's
own (buggy) internal summary of D-54 originally implied — this exact confusion is the one WARNING
`INGEST-CONFLICTS.md` raised, and the user has explicitly confirmed the blanket rule wins.

**How to avoid:** `if (dragging) return;` — a single boolean check, no `msg.Id` comparison at all, checked
**before** any other logic in the handler (including before the echo-filter check has any further effect,
though echo-filtering itself can stay first since it's even cheaper and order between rules 3 and 4
doesn't change the outcome).

**Warning signs:** a test or manual check that opens a third tab/device, draws or deletes something there
*during* a drag in tab A, and observes tab A (not just tab B) update mid-drag — this must NOT happen.

### Pitfall 3: Broadcasting `delete` (or `move`) optimistically, before the write is confirmed

**What goes wrong:** calling `Notifier.Publish(userId, SyncMessage.Delete(...))` before or without
awaiting `Figures.DeleteAsync`, mirroring the currently-shipped Phase 4 `HandleDeleteAsync`, which already
does `figures.RemoveAll(...)` **before** `await Figures.DeleteAsync(userId, figureId)` with no
try/catch at all.

**Why it happens:** it is the shape Phase 4 already shipped for the *local* UI (optimistic remove), and
copying that same shape for the *broadcast* looks consistent — but D-39 establishes the opposite
principle for `draw` ("the broadcast cannot be fired optimistically... insert → get id → broadcast"), and
nothing in the ADR set carves out an exception for `delete`.

**How to avoid:** flip `HandleDeleteAsync` to await-then-mutate-then-broadcast (matching `draw`'s already
established discipline), wrapped in the same try/catch shape as `CommitDragAsync`. On failure, there is
no "coordinates" to roll back (nothing was ever broadcast, since nothing succeeded yet) — the local
`figures` list simply never had the figure removed in the first place if the delete is restructured this
way, which is strictly simpler than the drag case. See Open Questions #1 for the scope call this implies.

**Warning signs:** an unhandled exception on delete crashing the circuit (D-45 violation) is the sign the
current Phase 4 code has this exact gap unaddressed; it must not survive into Phase 5.

### Pitfall 4: Treating a zero-row DELETE the same way as a zero-row UPDATE

**What goes wrong:** adding a "zero-row -> broadcast delete" branch to `DeleteAsync`'s call site, mirroring
`UpdateAsync`'s zero-row handling.

**Why it happens:** the two call sites look symmetric at a glance.

**How to avoid:** D-10 is explicit that DELETE-of-a-ghost is *already* naturally idempotent and needs no
guard — a zero-row delete IS the desired end state (the figure is gone either way), so there is nothing to
react to. Only `UpdateAsync`'s zero-row case is a signal that something is newly wrong (a stale tab
believed a figure still existed and just found out otherwise) — that asymmetry is deliberate, not an
oversight to "fix" into symmetry.

### Pitfall 5: Applying `EnableRetryOnFailure` project-wide changes the semantics of `LoadAsync` too

**What goes wrong:** not realizing that configuring retry once on the shared `AddDbContextFactory` call
also silently makes the initial page-load `LoadAsync` query retry on transient failure — which is
harmless and arguably desirable (D-45's general "app stays alive" stance), but is a scope expansion beyond
what the phase's own success criteria describe, and worth calling out explicitly rather than discovering
incidentally during review.

**How to avoid:** no code change needed — just document this as an intentional, accepted side effect (it
strictly improves resilience and contradicts no locked decision) rather than treating it as scope creep to
undo.

## Code Examples

### The modal, on final failure (D-52 step 3/4)

```razor
@* Home.razor -- new markup, added once, outside the <svg> *@
@if (showSaveFailedModal)
{
    <div class="modal-backdrop">
        <div class="modal-dialog" role="alertdialog" aria-modal="true">
            <p>The change could not be saved. The canvas will be reloaded from the database.</p>
            <button type="button" @onclick="ReloadFromDatabaseAsync">OK</button>
        </div>
    </div>
}

@code {
    private bool showSaveFailedModal;

    private async Task ReloadFromDatabaseAsync()
    {
        figures = await Figures.LoadAsync(userId);   // D-52 step 4: reload from PostgreSQL
        selectedId = null;                            // the reloaded set has no meaningful prior selection
        showSaveFailedModal = false;
    }
}
```
The modal text is reproduced **verbatim** from the phase description and D-52: *"The change could not be
saved. The canvas will be reloaded from the database."* — do not paraphrase.

### `CanvasSyncNotifier` registration alongside the existing `FigureStore`

```csharp
// Program.cs
builder.Services.AddDbContextFactory<CanvasDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("Canvas"),
        npgsql => npgsql.EnableRetryOnFailure(maxRetryCount: 2, maxRetryDelay: TimeSpan.FromMilliseconds(200), errorCodesToAdd: null)));

builder.Services.AddScoped<FigureStore>();
builder.Services.AddSingleton<CanvasSyncNotifier>();   // NEW -- Singleton, not Scoped (see Pattern 1 anti-pattern)
```

## Runtime State Inventory

Not applicable — this is a pure feature-addition phase (in-process pub/sub + retry policy), not a
rename, refactor, or migration. No stored data, service config, OS-registered state, secrets, or build
artifacts carry an old name or identity that needs updating. The one piece of new "state" (the
`CanvasSyncNotifier`'s subscriber dictionary) is deliberately in-memory-only and is expected to be empty
again after every process restart — there is no persistence concern to inventory.

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|-------------------|---------------|--------|
| Manual retry loops around `SaveChangesAsync`/raw ADO.NET calls, classifying `SqlException`/`NpgsqlException` codes by hand | `DbContextOptionsBuilder.EnableRetryOnFailure()` / `NpgsqlRetryingExecutionStrategy`, using `NpgsqlException.IsTransient` | Available since early Npgsql EF Core provider releases; unchanged in shape for the installed 10.0.3 | This is precisely why D-52's retry policy needs zero classification code in this app — it is provider configuration, not application logic |
| `ExecuteUpdateAsync`/`ExecuteDeleteAsync` requiring a manually-invoked execution strategy wrapper for retry to apply | A single, non-transaction-spanning `ExecuteUpdateAsync`/`ExecuteDeleteAsync` call is automatically wrapped by the configured execution strategy with no caller-side wrapping needed | EF Core 7+ (already the baseline for Phase 4's `FigureStore`) | `FigureStore.UpdateAsync`/`DeleteAsync` need no code changes to become retry-aware — only the `Program.cs` configuration change is required |

No other "old vs. new" axis applies — the Blazor Server `InvokeAsync`/synchronization-context model has
been stable since .NET 5/6 and nothing in .NET 10 changes its shape for this phase's purposes (confirmed
directly against the fetched .NET 10-monikered doc).

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|----------------|
| A1 | `Environment.TickCount64` (a monotonic millisecond counter) is preferred over `DateTime.UtcNow` for the throttle's elapsed-time comparison, to avoid any theoretical clock-adjustment skew. | Pattern 2 | Very low — on a single dev/demo machine over a single drag's timescale (seconds), `DateTime.UtcNow` would behave identically in practice; `TickCount64` is simply the more textbook-correct choice for elapsed-time measurement and costs nothing extra. Not user-visible either way. |
| A2 | **The exact exception type that escapes `ExecuteUpdateAsync`/`ExecuteDeleteAsync` after `EnableRetryOnFailure`'s retries are exhausted (or immediately, for a non-transient failure) is uncertain from sources available this session.** Blog-level sources claim EF Core wraps *all* database exceptions in `DbUpdateException`, including for `ExecuteUpdateAsync`; this contradicts the earlier, `[CITED]`-tier finding (04-RESEARCH.md, Pattern 4) that `ExecuteUpdateAsync`/`ExecuteDeleteAsync` "bypass the change tracker entirely" — the `DbUpdateException` wrapping is understood to happen specifically in the `SaveChanges`/change-tracker pipeline, which the bulk-update APIs explicitly do not use. No official Microsoft Learn page fetched this session settles this definitively for `ExecuteUpdateAsync` specifically. | Pattern 5, Code Examples (`catch (Exception)`) | **Medium.** If the planner narrows the `catch` clause to a specific type (e.g., `DbUpdateException` or `Npgsql.NpgsqlException`) based on an incorrect guess, a real failure of the *other* type would go uncaught and crash the circuit — the exact D-45 violation this phase exists to close. **Recommended mitigation, stated plainly so the planner does not have to guess:** either (a) keep the broad `catch (Exception)` shown in Pattern 5 at this one specific, narrowly-scoped call site (acceptable here because this catch's only job is "did the write ultimately fail," not "why," and the call site is already the single most specific place in the app this could go — not a global catch-all), or (b) spend one small verification task early in the phase's execution (e.g., a throwaway integration test that stops the Postgres container mid-`UpdateAsync` and asserts on the caught exception's runtime type) before writing the final catch clause. Either resolves the ambiguity without blocking planning. |
| A3 | Delete's broadcast-after-success restructuring (Common Pitfall 3) is **in scope for this phase**, even though the ROADMAP's Phase 5 description and D-52's own text discuss the rollback/modal mechanism using drag-specific language ("the figure's original coordinates"). | Common Pitfall 3, Open Questions #1 | Low-medium — if out of scope, Phase 4's existing `HandleDeleteAsync` keeps its current optimistic-remove-then-await shape with no try/catch, which is a live D-45 violation (an unhandled exception there would crash the circuit) independent of anything Phase 5 adds. Recommended treatment: in scope, using the simpler award-then-remove restructuring (no "rollback" concept needed for delete, since nothing is removed locally until the delete is confirmed). |

**If this table is empty:** not applicable — see entries above; every claim tagged `[ASSUMED]` in
practice traces to A1–A3.

## Open Questions

1. **Does DATA-04's retry/rollback/modal policy apply only to the drag (`move`) write path, or to
   `draw`/`delete` writes too?**
   - What we know: the phase description's Success Criterion 5 and D-52's own worked example both use
     drag-specific language ("restored to the figure's original coordinates"); D-45 is stated as "the
     ONLY general error-handling stance in the log," implying it is not drag-exclusive; the currently
     shipped `CommitAsync` (draw) and `HandleDeleteAsync` (delete) both have zero exception handling
     today, which is a real crash risk regardless of how this question is answered.
   - What's unclear: whether a failed `draw` or `delete` should show the *same* modal text (which is
     generic, not drag-specific, in its own wording) and reload-from-DB, or whether Phase 5's scope is
     narrowly the drag/glide path and draw/delete failure-hardening should be a separate concern.
   - Recommendation: treat it as in scope, using the SAME modal and reload-from-DB for any of the three
     write paths' final failure (simplest, most consistent, and the modal text itself is generic) — but
     only `move`/drag additionally gets a `rollback` **broadcast** to other tabs, since only `move` had
     already optimistically broadcast a (possibly-wrong) position to other tabs before the write was
     attempted. `draw` and `delete` failures need no cross-tab correction because neither broadcasts
     until *after* a confirmed success (D-39's discipline, extended to `delete` per Common Pitfall 3).

2. **Exact wrapper struct/record shape for `CanvasSyncNotifier`'s internal subscriber storage.**
   - What we know: no locked decision pins a specific collection type; `ConcurrentDictionary<int,
     ConcurrentDictionary<Guid, Action<SyncMessage>>>` (Pattern 1) satisfies every constraint (thread-safe
     multi-subscriber-per-user, O(1) add/remove, no lock needed).
   - What's unclear: whether the planner prefers this shape or an equivalent (e.g., an
     `ImmutableList`-swap-via-`Interlocked.CompareExchange` pattern, which trades a slightly more complex
     implementation for marginally cheaper reads under very high subscriber counts — irrelevant at this
     app's scale, at most a handful of tabs per user).
   - Recommendation: use the `ConcurrentDictionary`-of-`ConcurrentDictionary` shape shown in Pattern 1;
     revisit only if a future scale requirement ever appears (none exists in this ADR set).

3. **See Assumptions Log A2 — the exact exception type to catch at the `CommitDragAsync`/`HandleDeleteAsync`
   call sites.** Not expected to change the *architecture* of this phase either way; flagged for the
   planner to either accept the broad-catch recommendation or schedule the small empirical-verification
   task described in A2's mitigation.

## Environment Availability

Unchanged from Phases 1–4 — no new external dependencies introduced.

| Dependency | Required By | Available | Version | Fallback |
|------------|--------------|-----------|---------|----------|
| PostgreSQL 17 (Docker) | `FigureStore` read/write paths (unchanged), now retry-wrapped | Yes (confirmed running through Phase 4) | 17, container port per D-27's recorded deviation (5433 host-side on this dev machine, per `appsettings.Development.json`) | — |
| .NET 10 SDK | Build/run | Yes | 10.0.301 (confirmed this session, `dotnet --version`) | — |
| Npgsql.EntityFrameworkCore.PostgreSQL | `EnableRetryOnFailure` | Yes, already referenced | 10.0.3 (confirmed this session, `dotnet list package`) | — |

**Missing dependencies with no fallback:** none.
**Missing dependencies with fallback:** none.

## Security Domain

`security_enforcement` is not set to `false` in `.planning/config.json` — treated as enabled.

### Applicable ASVS Categories

| ASVS Category | Applies | Standard Control |
|----------------|---------|--------------------|
| V4 Access Control | Yes (unchanged mechanism, new consumer) | `CanvasSyncNotifier` is keyed strictly by the server-read `userId` claim (D-51) — never by any client-supplied value — so a message published for user A's `userId` bucket is structurally unreachable from user B's circuit, which subscribes only to its own `userId`. No new cross-user surface is introduced; the notifier only ever carries data that has already passed `FigureStore`'s existing `Where(... && f.UserId == userId)` IDOR filter (Phase 4) before being broadcast. |
| V5 Input Validation | Yes (unchanged) | Every coordinate broadcast has already passed `Movement.ClampMove` (drag) or `DrawGesture.Build` (draw) before this phase's code ever sees it — the notifier carries only already-validated, already-clamped values, never raw client input. |
| V2 Authentication | No (unchanged) | `[Authorize]` on the canvas page already gates the whole component (D-51); this phase adds no new entry point. |
| V3 Session Management | No (unchanged) | — |
| V6 Cryptography | No | Not applicable. |

### Known Threat Patterns for this stack

| Pattern | STRIDE | Standard Mitigation |
|---------|--------|-----------------------|
| A crafted/compromised circuit publishing a forged `SyncMessage` directly to `CanvasSyncNotifier` (bypassing `FigureStore`'s validation) | Tampering | Not reachable from outside the server process — `CanvasSyncNotifier.Publish` is only ever called from this app's own `Home.razor` code, never exposed as a client-invokable JS-interop or API endpoint. No new external attack surface is created by this phase. |
| Resource exhaustion via an unbounded number of open tabs each adding a subscriber that is never cleaned up | Denial of Service | `Dispose()`-based unsubscribe (D-11 rule 1) bounds the subscriber count to the number of currently-open tabs; a leaked subscriber would only accumulate if `Dispose()` is never called, which Blazor Server guarantees on circuit termination (confirmed by the official synchronization-context/component-disposal docs). |
| A save-failure retry storm amplifying load against PostgreSQL during an outage | Denial of Service (self-inflicted) | `EnableRetryOnFailure(maxRetryCount: 2, maxRetryDelay: 200ms)` bounds each failed write to at most 2 extra attempts with a short capped delay — not an unbounded or exponentially-growing retry storm. |

## Sources

### Primary (HIGH confidence)
- `docs/DECISIONS.md` (D-01 through D-58, `THE SCHEMA` section) — the project's sole specification;
  every locked-decision citation above (D-07, D-09, D-10, D-11, D-36, D-39, D-40, D-45, D-47, D-48,
  D-51, D-52, D-53, D-54) traced directly against this file, including the two full re-reads needed to
  cover D-40/D-47/D-52/D-53/D-54 in full per the task brief.
- `.planning/intel/decisions.md`, `.planning/intel/constraints.md` — the synthesized, normative extracts,
  cross-checked against `docs/DECISIONS.md` for consistency; `CONSTRAINT-sync-core`'s nine rules used
  directly as the phase's acceptance checklist shape.
- `.planning/INGEST-CONFLICTS.md` — the one WARNING (D-11/D-54 mid-drag filter contradiction) confirmed
  resolved in favor of D-54's blanket rule, per the task brief's explicit instruction; documented in
  Common Pitfall 2.
- `src/BlazorCanvas/Components/Pages/Home.razor`, `Home.razor.css`, `Components/Canvas/FigureShape.razor`,
  `Components/Canvas/Toolbar.razor`, `Data/FigureStore.cs`, `Data/Figure.cs`, `Data/CanvasDbContext.cs`,
  `Program.cs`, `Geometry/Movement.cs`, `Geometry/Box.cs`, `Geometry/CanvasCoordinates.cs`,
  `Geometry/DrawGesture.cs`, `BlazorCanvas.csproj` — read directly, current on-disk state
  (post-Phase-4). Confirmed that `dragOriginalBox` is already retained for the whole drag specifically
  for this phase's benefit (existing code comment), that `FigureStore.UpdateAsync`/`DeleteAsync` have no
  exception handling today, and that `HandleDeleteAsync` currently removes locally before awaiting the
  delete (Common Pitfall 3's finding).
- `tests/BlazorCanvas.Tests/Data/FigureStoreTests.cs` — read directly; confirms the existing IDOR-proof
  test conventions this phase's new `CanvasSyncNotifierTests.cs` should mirror.
- [ASP.NET Core Blazor synchronization context](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/synchronization-context?view=aspnetcore-10.0)
  — fetched directly this session, `aspnetcore-10.0` moniker, `ms.date: 2025-11-11`; confirms the exact
  `NotifierService`/`InvokeAsync`/`Dispose()`-unsubscribe pattern this phase's notifier is built from.
  `[VERIFIED: learn.microsoft.com, fetched directly, current .NET 10 version]`
- [Other | Npgsql Documentation](https://www.npgsql.org/efcore/misc/other.html) and
  [NpgsqlRetryingExecutionStrategy | Npgsql Documentation](https://www.npgsql.org/efcore/api/Npgsql.EntityFrameworkCore.PostgreSQL.NpgsqlRetryingExecutionStrategy.html)
  — confirm `EnableRetryOnFailure`'s API shape and its reliance on `NpgsqlException.IsTransient`.
  `[CITED: npgsql.org official provider docs]`
- `dotnet list src/BlazorCanvas/BlazorCanvas.csproj package` and `dotnet --version` — run directly against
  this repo this session, confirming Npgsql.EntityFrameworkCore.PostgreSQL 10.0.3 and .NET SDK 10.0.301.
  `[VERIFIED: command run directly this session]`

### Secondary (MEDIUM confidence)
- `.planning/phases/BC-04-select-drag-delete/04-RESEARCH.md` — the closest existing in-project analog
  (re-entrancy-guard pattern in `CommitDragAsync`, the `pointerleave`-not-`pointerout` discipline, the
  zero-row-is-not-an-error framing) — read directly, treated as an established in-project pattern to
  extend, not an external source.
- GitHub issue titles (npgsql/efcore.pg #2327 "Deadlock is IsTransient = false", npgsql/npgsql #2239
  "Command timeout is falsely identified as transient") — community-reported edge cases in
  `IsTransient` classification, used only to document an accepted, unreachable-in-this-app risk (Pattern
  4's caveat), not as a basis for any design decision.
- Blog-level sources (haacked.com, thereformedprogrammer.net) claiming `ExecuteUpdateAsync` exceptions
  are wrapped in `DbUpdateException` — used only to surface Assumptions Log A2's open question, not
  treated as settled fact, since it appears to conflict with `ExecuteUpdateAsync`'s documented
  change-tracker bypass (04-RESEARCH.md's own `[CITED]` finding).

### Tertiary (LOW confidence)
- None beyond what is folded into Assumptions Log A2/A3 above — every other claim traces to either the
  project's own locked decisions/existing code, or an official Microsoft/Npgsql page fetched or searched
  directly this session.

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — no new packages; `EnableRetryOnFailure` and the notifier pattern both use
  already-installed, already-vetted infrastructure, confirmed against official docs fetched this session
- Architecture: HIGH — the notifier/throttle/rollback sequencing all derive directly from either an
  official Microsoft pattern (fetched, current) or the ADR set's own stated rationale (D-36, D-47, D-52),
  not invented from scratch
- Pitfalls: HIGH for 4 of 5 (echo/blanket-discard/zero-row-asymmetry/project-wide-retry-scope are direct,
  unambiguous readings of locked decisions); MEDIUM for Pitfall 1 (`ObjectDisposedException` mitigation
  is a defense-in-depth recommendation, not something the ADR set specifies a mechanism for)

**Research date:** 2026-07-16
**Valid until:** 30 days (stable framework/provider APIs; the one fast-moving element — the exact
exception type escaping `ExecuteUpdateAsync` after retry exhaustion, Assumptions Log A2 — should be
verified empirically during execution regardless of elapsed time, since it depends on this project's
exact installed Npgsql/EF Core minor version behavior, not on anything that becomes stale with age).
