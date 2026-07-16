# Phase 4: Select, Drag & Delete - Research

**Researched:** 2026-07-16
**Domain:** Blazor Server pointer-event UI (select/drag/delete) over an existing SVG canvas; EF Core bulk write path
**Confidence:** HIGH

## Summary

This phase adds no new technology to the stack — it is pure extension of what Phase 3 already
built: the same `svg` canvas, the same `FigureShape` renderer, the same `FigureStore` /
`IDbContextFactory<CanvasDbContext>` data path, and the same Phase-1 geometry core. The three
verbs (select, drag, delete) are implemented entirely with markup-only Blazor pointer-event
attributes (`@onpointerdown`, `@onpointermove`, `@onpointerup`, `@onpointerleave`) and their
`:stopPropagation` modifier — no JavaScript, confirmed against official Blazor docs.

The single most important finding: **the move-clamp math already exists and is already tested.**
`Movement.ClampMove(Box, int dx, int dy) -> Box` was built in Phase 1 (`src/BlazorCanvas/Geometry/Movement.cs`)
specifically for this phase and has never been called from application code. It implements D-36's
exact formula (`dx' = clamp(dx, -bx1, W-bx2)`, per-axis independent, edge-sliding) and is proven
by `tests/BlazorCanvas.Tests/Geometry/ClampTests.cs`. Nothing in this phase should re-derive clamp
math — call `Movement.ClampMove` directly.

The second major finding, confirmed against Microsoft's official Blazor docs: `@onpointerdown:stopPropagation`
accepts a **bound C# boolean expression**, not just a literal `true`. This is the mechanism that
resolves the phase's central architectural tension — a press on a figure must select/drag when the
pointer tool is armed, but must fall through to the SVG's existing draw-start handler when a shape
tool is armed (D-30's "draws even on top of existing figures"). Binding `stopPropagation` to
`armedTool == Tool.Pointer` on each figure's own pointerdown handler solves this with zero JavaScript
and zero conflict with Phase 3's existing SVG-level handlers, which already no-op whenever the
pointer tool is armed.

The third finding: `ExecuteUpdateAsync` / `ExecuteDeleteAsync` (confirmed against official EF Core
docs) execute immediately, bypass the change tracker entirely, and return the affected-row count
with **no exception ever thrown for zero rows** — exactly the semantics D-10 requires. Both must be
combined with a `Where(f => f.Id == figureId && f.UserId == userId)` filter, mirroring Phase 3's
`T-03-01` IDOR pattern: this is not just correctness, it is the phase's only real security control.

**Primary recommendation:** Extend `Home.razor` with a new page-spanning wrapper `<div>` around the
existing `<Toolbar>` + `<div class="canvas-area">` markup, carrying the drag's
`@onpointermove` / `@onpointerup` / `@onpointerleave` handlers (D-37); give `FigureShape` three new
parameters (`Selected`, `Selectable`, `OnPointerDown`); give `Toolbar` one new parameter (`OnDelete`);
add `UpdateAsync`/`DeleteAsync` to `FigureStore` using `ExecuteUpdateAsync`/`ExecuteDeleteAsync`
filtered by `userId`. No new NuGet packages, no new render mode, no new UI-SPEC needed beyond what
`03-UI-SPEC.md`'s "Canvas & Cursor Spec" section already locked for this exact phase.

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| Hit-testing "which figure was clicked" / topmost resolution | Browser / Client | — | Native SVG pointer-event targeting on filled shapes (D-38) combined with document paint order (D-39, ascending `id`) resolves this with zero C# code — confirmed by research priority #4 below |
| Click-vs-drag classification (3px threshold) | Frontend Server (Blazor circuit, C#) | — | Every `pointermove` is already a server round-trip (D-07); the threshold check is pure C# state in `Home.razor`'s `@code` block |
| Edge-clamp math | Frontend Server (Blazor circuit, C#) | — | `Movement.ClampMove` — pure C#, already built, zero Blazor dependency (reusable identically by Phase 5's glide loop) |
| Selection state | Frontend Server (per-circuit C# field) | — | D-31: local UI state only, never persisted, never broadcast |
| Persistence (UPDATE on drop, DELETE on click) | API / Backend (`FigureStore` over EF Core) | Database / Storage | `ExecuteUpdateAsync`/`ExecuteDeleteAsync` issue the SQL directly; PostgreSQL's CHECK constraints remain the final backstop (unreachable here since a move only translates an already-legal box) |
| Delete-button enable/disable | Frontend Server (Blazor circuit) → rendered to Browser | — | `DeleteEnabled` is server-computed (`selectedId.HasValue`) and rendered as the native `disabled` attribute — no client-side JS needed |

## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| FIG-02 | Select: click selects (red outline), click empty canvas deselects, topmost on overlap, click = no DB write | Native SVG hit-testing (no C# topmost logic needed); `@onpointerdown:stopPropagation` bound to `armedTool == Tool.Pointer` isolates select-press from draw-press; selection is local-only state |
| FIG-03 | Drag: ≥3px moves and persists on drop, stays selected, edge-slides, exactly one UPDATE, survives release-outside-window / Alt-Tab | `Movement.ClampMove` (already built, Phase 1) for the edge-slide; page-spanning wrapper + `pointerleave` + `Buttons` guard (D-37, same pattern Phase 3 already used for draw) for commit-without-`setPointerCapture`; `ExecuteUpdateAsync` filtered by id+userId for the single write |
| FIG-04 | Delete: toolbar button, disabled until selection exists, no Delete-key handler | `Toolbar.OnDelete` EventCallback (new); `ExecuteDeleteAsync` filtered by id+userId, naturally idempotent per D-10 |

## Standard Stack

No new packages this phase. Every capability is built from packages already present in
`src/BlazorCanvas/BlazorCanvas.csproj` (`Microsoft.AspNetCore.Components.Web` for
`PointerEventArgs`, `Microsoft.EntityFrameworkCore` + `Npgsql.EntityFrameworkCore.PostgreSQL` for
`ExecuteUpdateAsync`/`ExecuteDeleteAsync`) and the app's own Phase 1–3 code.

**Version verification:** `ExecuteUpdateAsync`/`ExecuteDeleteAsync` require EF Core 7.0+; this
project is on EF Core 10.0.10 (confirmed present, `.planning/STATE.md` Phase BC-01 entry: *"Aligned
tests/BlazorCanvas.Tests.csproj EF Core package versions to 10.0.10"*). `[VERIFIED: project STATE.md
+ official EF Core docs confirming ExecuteUpdate/ExecuteDelete availability since EF Core 7]` — no
`npm view`/`pip index` equivalent applies; this is an existing project dependency, not a new install.

**Installation:** none required.

## Package Legitimacy Audit

Not applicable — this phase installs no new packages. All symbols used (`PointerEventArgs`,
`ExecuteUpdateAsync`, `ExecuteDeleteAsync`) come from packages already vetted and installed in
Phases 1–3.

## Architecture Patterns

### System Architecture Diagram

```
Browser (SVG canvas, figures painted in ascending id order = z-order, D-39)
   |
   | pointerdown on a <rect>/<line>/<circle>/<polygon> child element (native hit-test
   | picks the TOPMOST painted shape under the cursor -- free, no C# code)
   v
FigureShape.razor  --OnPointerDown EventCallback-->  Home.razor (@code)
   |  @onpointerdown:stopPropagation="Selectable"        |
   |  (Selectable = armedTool == Tool.Pointer)            |  HandleFigurePointerDown(figureId, e):
   |  -> when true, the SVG's OWN pointerdown handler      |    - e.Button != 0 -> return
   |     (Phase 3's draw-start handler) never fires        |    - selectedId = figureId
   |  -> when false (a shape tool is armed), propagation   |    - dragOriginalBox = current Box
   |     is NOT stopped, so the SVG's draw-start handler    |    - dragCurrentBox  = dragOriginalBox
   |     fires normally -- D-30's "draw on top of a figure" |    - dragPressX/Y = CanvasCoordinates.FromPage(e)
   |     keeps working unmodified                           |    - dragging = true; dragMoved = false
   v
Page-spanning wrapper <div>  (D-37 -- NOT the SVG, so a drag survives the cursor
   |  @onpointermove / @onpointerup / @onpointerleave        wandering off the canvas but still inside
   |                                                          the browser window)
   v
Home.razor (@code)
   - OnWrapperPointerMove: if !dragging return
                           if (e.Buttons & 1) == 0 -> CommitDragAsync(); return   (Alt-Tab case)
                           cursor = CanvasCoordinates.FromPage(e)
                           dx = cursor.X - dragPressX; dy = cursor.Y - dragPressY
                           if !dragMoved: dragMoved = Euclidean(dx,dy) >= 3         (D-48)
                           dragCurrentBox = Movement.ClampMove(dragOriginalBox, dx, dy)   (D-36, already built)
   - OnWrapperPointerUp / OnWrapperPointerLeave: if dragging -> CommitDragAsync()   (D-37 rule 1)
   - CommitDragAsync(): dragging = false FIRST (re-entrancy guard, same pattern as Phase 3's CommitAsync)
                         if !dragMoved: return                                     (pure click, no write)
                         affected = await Figures.UpdateAsync(userId, dragFigureId, dragCurrentBox)
                         if affected == 0: remove figure from `figures`; clear selectedId  (D-10, local half only)
                         else: mutate the in-memory figure's X1..Y2 to dragCurrentBox       (selectedId stays set, D-48)
   v
FigureStore.UpdateAsync(userId, figureId, box)
   db.Figures.Where(f => f.Id == figureId && f.UserId == userId)     <- IDOR guard, mirrors T-03-01
             .ExecuteUpdateAsync(s => s.SetProperty(x1)...SetProperty(y2))
   -> returns affected-row count directly, no SaveChangesAsync, no exception on 0 rows (D-10)
   v
PostgreSQL: exactly one UPDATE per drag, on drop only (never during move)

Toolbar.razor Delete button --OnDelete EventCallback--> Home.HandleDeleteAsync()
   - if !selectedId.HasValue return
   - affected = await Figures.DeleteAsync(userId, selectedId.Value)
   - remove from `figures` if present; selectedId = null           (naturally idempotent, D-10)
   v
FigureStore.DeleteAsync(userId, figureId)
   db.Figures.Where(f => f.Id == figureId && f.UserId == userId).ExecuteDeleteAsync()
```

### Recommended Project Structure

No new folders. Modified/extended files only:

```
src/BlazorCanvas/
├── Components/Pages/Home.razor          # + page-spanning wrapper div, new @code fields/handlers
├── Components/Pages/Home.razor.css      # + wrapper CSS, + grab/grabbing cursor rules
├── Components/Canvas/FigureShape.razor  # + Selected, Selectable, OnPointerDown parameters
├── Components/Canvas/Toolbar.razor      # + OnDelete parameter, @onclick on the Delete button
├── Data/FigureStore.cs                  # + UpdateAsync, DeleteAsync
tests/BlazorCanvas.Tests/
├── Data/FigureStoreTests.cs             # + UpdateAsync/DeleteAsync tests (affected-row count, cross-user isolation)
```

### Pattern 1: Conditional `stopPropagation` to route one pointerdown to two different handlers

**What:** A figure's own `@onpointerdown` handler stops propagation to its ancestor's handler
*only when the pointer tool is armed*; when a shape tool is armed, propagation is not stopped, and
the ancestor SVG's own (Phase 3) draw-start handler receives the event normally.

**When to use:** Any time a child element's press must sometimes be "claimed" by the child and
sometimes bubble to a parent, and the choice depends on runtime state (here: which tool is armed) —
without JavaScript's `event.stopPropagation()`.

**Example:**
```razor
@* FigureShape.razor -- root element gets stopPropagation bound to a parameter *@
<rect ... @onpointerdown="HandlePointerDown" @onpointerdown:stopPropagation="Selectable" />

@code {
    [Parameter] public bool Selectable { get; set; }
    [Parameter] public EventCallback<PointerEventArgs> OnPointerDown { get; set; }

    private Task HandlePointerDown(PointerEventArgs e) =>
        Selectable ? OnPointerDown.InvokeAsync(e) : Task.CompletedTask;
}
```
```razor
@* Home.razor -- Selectable is computed once per render from armedTool *@
<FigureShape @key="f.Id" ... Selectable="@(armedTool == Tool.Pointer)"
             Selected="@(f.Id == selectedId)"
             OnPointerDown="e => HandleFigurePointerDown(f.Id, e)" />
```
Source: `@on{DOM EVENT}:stopPropagation` directive attribute, confirmed to accept a bound `bool`
expression — [learn.microsoft.com/aspnet/core/blazor/components/event-handling](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/event-handling)
`[CITED: learn.microsoft.com/aspnet/core/blazor/components/event-handling]`.

> ⚠️ **Caveat from the same doc, load-bearing:** *"The `stopPropagation` directive attribute's
> effect is limited to the Blazor scope and doesn't extend to the HTML DOM."* This is fine here —
> there is no non-Blazor JS listener competing for these events anywhere in this app (D-37) — but
> it means this pattern would NOT protect against a hand-written JS listener if one ever existed.

### Pattern 2: The page-spanning drag-commit wrapper (D-37, extended from Phase 3's draw pattern)

**What:** Phase 3 already put `@onpointerdown/move/up/leave` **on the `svg` element itself** for
the *draw* gesture, because a draw starts and ends on the canvas (03-05-PLAN.md, explicit note:
*"D-37's page-spanning wrapper is Phase 4's drag surface; this phase's gesture starts and ends on
the canvas"*). This phase must add a **second, outer** set of handlers on a wrapper that encloses
`<Toolbar>` and the canvas, for the *drag*, because a drag legitimately needs to survive the cursor
wandering onto the toolbar or the grey page background and still commit correctly.

**When to use:** Any gesture that must survive the pointer leaving its "natural" originating
element but not leaving the browser window.

**Why the two handler sets never conflict:** the SVG-level `OnPointerMove`/`OnPointerUp`/`OnPointerLeave`
(Phase 3) all begin with `if (!drawing) return;`, and `drawing` can only become `true` when a shape
tool is armed (`ToolMap.ToFigureType(armedTool)` is non-null). The new wrapper-level handlers all
begin with `if (!dragging) return;`, and `dragging` can only become `true` via a figure press while
the pointer tool is armed. The two booleans are mutually exclusive by construction (a tool is either
`Tool.Pointer` or a shape — never both), so at most one of the two gesture state machines is ever
active.

**Example:**
```razor
<div class="app-shell" @onpointermove="OnWrapperPointerMove"
                        @onpointerup="OnWrapperPointerUp"
                        @onpointerleave="OnWrapperPointerLeave">
    <Toolbar @bind-Armed="armedTool" OnDelete="HandleDeleteAsync" DeleteEnabled="selectedId.HasValue" />
    <div class="canvas-area">
        <svg class="@CanvasSurfaceClass" width="1280" height="720"
             @onpointerdown="OnPointerDown" @onpointermove="OnPointerMove"
             @onpointerup="OnPointerUp" @onpointerleave="OnPointerLeave">
            @* ... figures, draw preview ... *@
        </svg>
    </div>
</div>
```
```css
/* Home.razor.css -- must be at least viewport-sized so pointerleave only fires at the
   browser window boundary, not at some interior edge (D-37's "page-spanning" requirement). */
.app-shell {
    min-height: 100vh;
}
```

### Pattern 3: The click-vs-drag threshold, computed from the fixed press point every move (not incrementally)

**What:** `dx`/`dy` are always measured from the ORIGINAL press point (`dragPressX`/`dragPressY`),
never accumulated incrementally move-to-move. `Movement.ClampMove` is called with the figure's
`dragOriginalBox` (its position *at press time*) every single call, not with the previously-clamped
box. This is the same pattern `DrawGesture.Build` already uses for the draw gesture (fixed
`pressX`/`pressY`, varying `cursorX`/`cursorY`) — reuse it, do not invent an incremental-delta
variant.

**Why it matters:** an incremental approach (`box = ClampMove(box, smallDx, smallDy)` on every move,
accumulating) would compound rounding and, more importantly, would make repeated small moves against
an edge "stick" differently than one large move — the clamp must always be evaluated against the
figure's true original position and the *total* delta so far, exactly as D-36's formula is written.

**Example:**
```csharp
// Home.razor @code
private void OnWrapperPointerMove(PointerEventArgs e)
{
    if (!dragging) { return; }

    if ((e.Buttons & 1) == 0)
    {
        // async commit; see CommitDragAsync below -- fire-and-forget pattern matches Phase 3
    }

    var cursor = CanvasCoordinates.FromPage(e.PageX, e.PageY);
    var dx = cursor.X - dragPressX;
    var dy = cursor.Y - dragPressY;

    if (!dragMoved)
    {
        // D-48: 3px threshold. Formula not specified by any locked decision -- Euclidean
        // distance chosen for consistency with DrawGesture's existing circle-radius distance
        // calculation. See Assumptions Log, A1.
        var distance = Math.Sqrt((double)(dx * dx + dy * dy));
        dragMoved = distance >= 3;
    }

    dragCurrentBox = Movement.ClampMove(dragOriginalBox, dx, dy);
}
```

### Pattern 4: The IDOR-safe write path (mirrors Phase 3's T-03-01 exactly)

**What:** Every `UPDATE`/`DELETE` filters on **both** the figure's own id and the caller's
`user_id` — never the id alone. `ExecuteUpdateAsync`/`ExecuteDeleteAsync` translate the `Where`
clause directly into the SQL `WHERE`, so this filter is enforced by the database, not by an
application-level ownership check that could be forgotten.

**Example:**
```csharp
// FigureStore.cs -- new methods
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
Source: `ExecuteUpdateAsync`/`ExecuteDeleteAsync` semantics (immediate execution, no change-tracker
interaction, return affected-row count, no exception on zero rows, `Where` filter usable for
optimistic-concurrency-style ownership checks) —
[learn.microsoft.com/ef/core/saving/execute-insert-update-delete](https://learn.microsoft.com/en-us/ef/core/saving/execute-insert-update-delete)
`[CITED: learn.microsoft.com/ef/core/saving/execute-insert-update-delete]`. The doc's own
"Concurrency control and rows affected" section shows the identical `Where(id && token).ExecuteUpdateAsync(...); if (numUpdated == 0) ...`
shape this phase needs for D-10.

### Anti-Patterns to Avoid

- **Re-deriving the clamp formula inline in `Home.razor`.** `Movement.ClampMove` already exists,
  already handles the `lo > hi` degenerate case (CR-02's fix), and is already tested against every
  boundary condition in `ClampTests.cs`. A second implementation is a second source of truth.
- **A translucent "drag preview" ghost figure**, mirroring the draw gesture's `Preview="true"`
  pattern. A drag moves the *real* figure live — there is no separate preview shape, and the
  dragged figure should render at full opacity throughout (D-48 does not describe any transparency
  during a drag; only the draw gesture has a preview, D-35).
- **Filtering `Update`/`Delete` by figure id alone.** Omitting the `userId` filter is the one
  concrete way this phase could let user A silently move or delete user B's figure — the database
  has no `figures.user_id`-scoped RLS policy, so the application query is the only guard.
- **Using `SaveChangesAsync` + tracked entities for the move/delete.** Would reintroduce
  `DbUpdateConcurrencyException` on a zero-row case (the exact crash D-10 exists to avoid) and would
  require a round-trip load before every write. `ExecuteUpdateAsync`/`ExecuteDeleteAsync` are the
  correct tool specifically because they never throw on zero rows.
- **`@onpointerout` instead of `@onpointerleave`** anywhere in this phase — `pointerout` also fires
  when the cursor moves onto a *child* element, and every figure is a child of the wrapper the drag
  handlers live on. This is D-37's own explicit warning, already correctly avoided once in Phase 3;
  it must be avoided again here on the new wrapper.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|--------------|-----|
| Per-axis edge clamp | A new `ClampMove`-equivalent | `Movement.ClampMove(Box, dx, dy)` (Phase 1, already tested) | Already proven against every boundary case (`ClampTests.cs`); a second implementation risks silently diverging from D-36's formula |
| "Which figure is topmost under the cursor" | A C# hit-test / z-order resolver | Native SVG/browser pointer-event targeting on filled shapes (D-38), combined with DOM paint order = ascending `id` (D-39) | The browser already does this correctly and for free — see research priority #4 below; writing a resolver would be strictly redundant code with its own bug surface |
| Optimistic-concurrency / staleness guard | A `rowversion`/`xmin` concurrency token column | `ExecuteUpdateAsync`'s affected-row count | D-10 explicitly rejects concurrency tokens as "provably redundant" — the affected-row count already covers the only surviving case |
| stopping a press from reaching two handlers | `event.stopPropagation()` via a JS interop call | Blazor's `@on{event}:stopPropagation` directive, bound to a C# bool | Confirmed to work purely server-side; a JS interop call would be the project's first hand-authored JavaScript (forbidden, D-37) |

**Key insight:** every piece of new infrastructure this phase might be tempted to build already
exists from Phase 1 or Phase 3 — the actual work is *wiring*, not *inventing*. The roadmap's own
framing ("everything this plan needs already exists... if you find yourself writing geometry here,
stop") from `03-05-PLAN.md` applies identically here for the clamp math.

## Common Pitfalls

### Pitfall 1: Clamping `x2`/`y2` independently ("the landmine of this phase")

**What goes wrong:** calling something equivalent to `box.X2 = Math.Clamp(box.X2, 0, 1280)` instead
of clamping the *delta* and translating uniformly. This resizes the figure instead of moving it.

**Why it happens:** it looks like the obvious per-coordinate generalization of "keep it in bounds,"
especially if someone doesn't first go read `Movement.ClampMove`'s existing implementation.

**How to avoid:** always call `Movement.ClampMove(originalBox, dx, dy)` — never touch `X1`/`Y1`/`X2`/`Y2`
directly during a drag.

**Warning signs:** a rectangle that grows or shrinks while being dragged near an edge (silent, for
rectangles — D-22's reversal means a circle instead throws a `DbUpdateException` on the
`circle_is_a_circle` CHECK the moment such a row is written, so a circle bug here is loud, not
silent).

### Pitfall 2: `pointerout` instead of `pointerleave` on the new wrapper

**What goes wrong:** using `@onpointerout` on the page-spanning wrapper commits the drag every time
the cursor crosses onto any child element — which, on this page, is constantly (every figure, every
toolbar button).

**Why it happens:** `pointerout`/`pointerleave` sound interchangeable; the difference (does it fire
when entering a *descendant*, or only when truly leaving the element and all descendants) is easy to
miss.

**How to avoid:** `@onpointerleave`, never `@onpointerout`. Phase 3 already got this right once on
the SVG (03-05-PLAN.md's acceptance criteria literally grep for zero occurrences of `pointerout`);
apply the same discipline to the new wrapper-level handlers.

### Pitfall 3: Forgetting the re-entrancy guard on `CommitDragAsync`

**What goes wrong:** a `pointerup` arriving while a `pointerleave`-triggered commit (or the Buttons-guard
commit from `pointermove`) is still awaiting the `UPDATE` can fire a second `UpdateAsync` call for the
same drag, or — worse — a subsequent `pointerdown` on a different figure can be misread as still
belonging to the in-flight drag.

**Why it happens:** Blazor Server dispatches events one at a time, but `await` yields control, so two
logically-sequential events can interleave around an `await` boundary.

**How to avoid:** set `dragging = false` and capture `dragFigureId`/`dragCurrentBox`/`dragMoved` into
locals **before** the first `await` in `CommitDragAsync` — the exact pattern Phase 3's `CommitAsync`
already established for the draw gesture (`03-05-SUMMARY.md`'s T-03-15 threat-register entry).

### Pitfall 4: Treating a zero-row UPDATE as an error

**What goes wrong:** wrapping the `UpdateAsync`/`DeleteAsync` call in error-handling that shows the
D-45 "Could not save" message when the affected-row count is 0.

**Why it happens:** conflating "the database refused the write" (a real error) with "the row is
simply gone" (expected staleness, D-10) — the same distinction Phase 3 was careful to keep separate
(`03-05-SUMMARY.md`'s T-03-17: *"D-45/D-52 ... own the retry policy ... a bare try/catch ... would
swallow the failure"* — the inverse mistake here would be showing an error for a non-error).

**How to avoid:** a 0 affected-row count on `UpdateAsync` means: silently remove the figure from
`figures`, clear `selectedId` if it matches, no message, no exception, no retry. This is explicitly
**not** a Phase 5 concern to defer — the roadmap note says the write-path guard belongs here; only
the cross-tab *broadcast* half (telling other tabs) is Phase 5's DATA-03.

### Pitfall 5: Blazor Server round-trip cost during a drag (flagged risk, no action required)

**What it is:** every `pointermove` during a drag is a full SignalR round-trip to the server (D-07's
accepted cost, unchanged from Phase 3's draw gesture, which already exercises the identical code
path). This phase does not introduce any *new* per-move server work beyond what Phase 3 already
does for drawing — `Movement.ClampMove` is O(1) integer arithmetic, no database access happens on
`pointermove` (only on drop).

**Investigated and rejected as unnecessary:**
- `@onpointermove:preventDefault` — this suppresses the browser's *default action* for the event
  (e.g. text selection, native drag-image), not the SignalR dispatch itself; Phase 3 already solved
  the text-selection problem with plain CSS (`user-select: none` on `.canvas-surface`), not this
  modifier. It does not reduce round-trip volume.
- **Any throttling of the `pointermove` dispatch itself** — there is no non-JS mechanism to throttle
  *inbound* browser→server event dispatch in Blazor Server; `@onpointermove:preventDefault` and
  `@onpointermove:stopPropagation` only affect what happens after the event already reached the
  server. Throttling belongs to the **outbound** cross-tab broadcast (D-47's 50ms trailing-edge
  throttle) — that is Phase 5's concern, not this one's.
- **Conclusion:** accept the round-trip cost, exactly as Phase 3 already did and as D-07 already
  named explicitly ("invisible on localhost/LAN, becomes visibly laggy over a poor connection").
  No code changes are needed in this phase to address it; flagged here only so the planner does not
  spend effort solving an already-accepted, already-scoped cost.

## Code Examples

### Selection + delete-enabled wiring end to end

```razor
@* Home.razor -- new/changed pieces only, layered onto the existing Phase 3 file *@
<div class="app-shell" @onpointermove="OnWrapperPointerMove"
                        @onpointerup="OnWrapperPointerUp"
                        @onpointerleave="OnWrapperPointerLeave">
    <Toolbar @bind-Armed="armedTool" DeleteEnabled="selectedId.HasValue" OnDelete="HandleDeleteAsync" />

    <div class="canvas-area">
        <svg class="@CanvasSurfaceClass" width="1280" height="720"
             @onpointerdown="OnPointerDown" @onpointermove="OnPointerMove"
             @onpointerup="OnPointerUp" @onpointerleave="OnPointerLeave">
            @foreach (var f in figures)
            {
                var box = f.Id == dragFigureId ? dragCurrentBox : new Box(f.X1, f.Y1, f.X2, f.Y2);
                <FigureShape @key="f.Id" Type="FigureTypeNames.Parse(f.Type)" Box="box"
                             Selected="@(f.Id == selectedId)"
                             Selectable="@(armedTool == Tool.Pointer)"
                             OnPointerDown="e => HandleFigurePointerDown(f.Id, e)" />
            }
            @if (drawing)
            {
                <FigureShape Type="previewType" Box="previewBox" Preview="true" />
            }
        </svg>
    </div>
</div>

@code {
    // ... existing Phase 3 fields (userId, armedTool, figures, drawing, previewType, pressX, pressY, previewBox) ...

    private int? selectedId;
    private bool dragging;
    private int? dragFigureId;
    private Box dragOriginalBox;   // retained for the WHOLE drag -- Phase 5's D-52 rollback source
    private Box dragCurrentBox;
    private int dragPressX;
    private int dragPressY;
    private bool dragMoved;

    private void HandleFigurePointerDown(int figureId, PointerEventArgs e)
    {
        if (e.Button != 0) { return; }

        var figure = figures.First(f => f.Id == figureId);
        selectedId = figureId;
        dragFigureId = figureId;
        dragOriginalBox = new Box(figure.X1, figure.Y1, figure.X2, figure.Y2);
        dragCurrentBox = dragOriginalBox;

        var point = CanvasCoordinates.FromPage(e.PageX, e.PageY);
        dragPressX = point.X;
        dragPressY = point.Y;

        dragging = true;
        dragMoved = false;
    }

    private async Task OnWrapperPointerMove(PointerEventArgs e)
    {
        if (!dragging) { return; }

        if ((e.Buttons & 1) == 0)
        {
            await CommitDragAsync();
            return;
        }

        var cursor = CanvasCoordinates.FromPage(e.PageX, e.PageY);
        var dx = cursor.X - dragPressX;
        var dy = cursor.Y - dragPressY;

        if (!dragMoved)
        {
            dragMoved = Math.Sqrt((double)(dx * dx + dy * dy)) >= 3;   // D-48, see Assumptions A1
        }

        dragCurrentBox = Movement.ClampMove(dragOriginalBox, dx, dy);
    }

    private async Task OnWrapperPointerUp(PointerEventArgs e)
    {
        if (dragging) { await CommitDragAsync(); }
    }

    private async Task OnWrapperPointerLeave(PointerEventArgs e)
    {
        if (dragging) { await CommitDragAsync(); }
    }

    private async Task CommitDragAsync()
    {
        dragging = false;
        var figureId = dragFigureId!.Value;
        var moved = dragMoved;
        var box = dragCurrentBox;

        if (!moved) { return; }   // a click: already selected, no write (D-48)

        var affected = await Figures.UpdateAsync(userId, figureId, box);
        if (affected == 0)
        {
            figures.RemoveAll(f => f.Id == figureId);
            if (selectedId == figureId) { selectedId = null; }
            return;
        }

        var figure = figures.First(f => f.Id == figureId);
        figure.X1 = box.X1; figure.Y1 = box.Y1; figure.X2 = box.X2; figure.Y2 = box.Y2;
        // selectedId stays set -- D-48: "stays selected after the drop"
    }

    private async Task HandleDeleteAsync()
    {
        if (!selectedId.HasValue) { return; }

        var affected = await Figures.DeleteAsync(userId, selectedId.Value);
        figures.RemoveAll(f => f.Id == selectedId.Value);
        selectedId = null;
    }

    // Existing OnPointerDown (Phase 3) gains one line in its null-tool branch:
    private void OnPointerDown(PointerEventArgs e)
    {
        var type = ToolMap.ToFigureType(armedTool);
        if (type is null)
        {
            if (e.Button == 0) { selectedId = null; }   // NEW: click empty canvas deselects (D-31)
            return;
        }
        // ... unchanged draw-start logic ...
    }
}
```

### FigureShape's selected-outline switch

```razor
@* FigureShape.razor -- StrokeColor replaces the hardcoded "#000000" literal *@
<rect x="@Box.X1" y="@Box.Y1" width="@Box.Width" height="@Box.Height"
      fill="#FFFFFF" stroke="@StrokeColor" stroke-width="2"
      fill-opacity="@OpacityValue" stroke-opacity="@OpacityValue" />

@code {
    [Parameter] public bool Selected { get; set; }

    // #000000 (black, D-58 default) vs #B91C1C (red, D-58/03-UI-SPEC Destructive token, selected)
    private string StrokeColor => Selected ? "#B91C1C" : "#000000";
}
```

## Runtime State Inventory

Not applicable — this is a pure feature-addition phase (select/drag/delete), not a rename, refactor,
or migration. No stored data, service config, OS-registered state, secrets, or build artifacts carry
an old name that needs updating.

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|-------------------|---------------|--------|
| Tracked `SaveChangesAsync` + manual `DbUpdateConcurrencyException` handling for zero-row updates | `ExecuteUpdateAsync`/`ExecuteDeleteAsync`, which never throw on zero rows and return the affected-row count directly | EF Core 7 (Nov 2022); this project is on EF Core 10 | This is precisely why D-10's zero-row guard is cheap to implement correctly here — no try/catch needed anywhere in the write path, only an `if (affected == 0)` branch |

No other "old vs. new" axis applies — Blazor Server's markup-only pointer-event model (`PointerEventArgs`,
the `:stopPropagation`/`:preventDefault` modifiers) has been stable since .NET 5/6 and nothing in
.NET 10 changes its shape for this phase's purposes.

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|----------------|
| A1 | The 3px click-vs-drag threshold (D-48) is measured as Euclidean distance from the press point (`sqrt(dx²+dy²) >= 3`), not per-axis (Chebyshev) or Manhattan distance. D-48's own text ("move less than 3 px") does not specify a formula. | Pattern 3, Code Examples | Low — the only user-visible difference between Euclidean and Chebyshev at a 3px threshold is a few sub-pixel diagonal cases; either choice satisfies every locked decision's plain-language wording. Recommended because it matches the distance formula `DrawGesture.Build` already uses for the circle radius (`Math.Sqrt(dx*dx+dy*dy)`), keeping the app's one "distance" concept consistent. Flag for a one-line confirmation if the planner wants certainty rather than a reasonable default. |
| A2 | A page-spanning wrapper `<div>` with `min-height: 100vh` (wrapping `<Toolbar>` + `<div class="canvas-area">`) is sufficient to satisfy D-37's "page-spanning wrapper element" requirement for the drag's `pointerleave`/`pointermove`/`pointerup` handlers. | Pattern 2 | Low — D-37 itself only requires "not the SVG"; a `min-height: 100vh` block-level div spanning the full viewport width (default block behavior) satisfies the letter and intent of the decision. The one edge case (a canvas wider than the viewport, D-19's accepted narrow-screen cost) is already an accepted cost elsewhere and does not change this phase's correctness, only the rare early-commit UX already described in D-37's own "Accepted cost" paragraph. |
| A3 | `03-UI-SPEC.md`'s existing "Canvas & Cursor Spec" section (grab/grabbing cursor states, the `Destructive` `#B91C1C` selected-outline color) is treated as binding guidance for this phase, even though the document's own scope note says it is binding "for the canvas and toolbar only — the surfaces Phase 3 creates." | Code Examples, Architecture Patterns | Low — the document's content explicitly describes Phase-4-only states (drag cursor, selected outline) under a still-`status: draft` frontmatter; if the planner or user wants a fresh `04-UI-SPEC.md` instead, the color/cursor values do not change (they are the same locked D-58 values), only the document of record does. |

## Open Questions

1. **Should Phase 4 produce its own `04-UI-SPEC.md`, or extend `03-UI-SPEC.md` in place?**
   - What we know: `03-UI-SPEC.md`'s "Canvas & Cursor Spec" section already fully describes the
     pointer-tool hover/drag cursor states and the selected-figure red outline color that this phase
     needs — nothing new to design.
   - What's unclear: whether the project's convention (one `NN-UI-SPEC.md` per phase, matching
     `03-UI-SPEC.md`'s own precedent) should be followed literally, producing a thin `04-UI-SPEC.md`
     that mostly cross-references `03-UI-SPEC.md`, or whether extending the existing file in place is
     acceptable since it was written with this exact phase's behavior already in mind.
   - Recommendation: planner's/user's call, not a research question — the visual contract itself is
     already fully specified either way.

2. **Exact click-vs-drag distance formula (Euclidean vs. per-axis).**
   - See Assumptions Log A1. Not expected to be user-visible in practice; flagged for the planner
     to either accept the recommended default or ask for explicit confirmation.

3. **Cursor CSS selector for the `grab`/`grabbing` states.**
   - What we know: `03-UI-SPEC.md` sketches the idea (`.tool-pointer svg > * { cursor: grab; }`) but
     the actual CSS class names in the shipped code differ (`Home.razor.css` currently uses
     `.canvas-surface.shape-armed` for the crosshair state, not `.tool-pointer`).
   - What's unclear: the exact selector to use so `grab` applies to figures (not empty canvas) when
     the pointer tool is armed, and does not leak through when a shape tool is armed and
     `.shape-armed`'s `cursor: crosshair` should win.
   - Recommendation: scope a new rule such as `.canvas-surface:not(.shape-armed) > :is(rect,line,circle,polygon):hover { cursor: grab; }`
     plus a `.is-dragging` class toggled on `.canvas-surface` for `cursor: grabbing` during an active
     drag — cosmetic only, does not affect any of the five ROADMAP success criteria, safe to leave to
     implementation-time discretion.

## Environment Availability

Unchanged from Phase 3 — no new external dependencies introduced.

| Dependency | Required By | Available | Version | Fallback |
|------------|--------------|-----------|---------|----------|
| PostgreSQL 17 (Docker) | `FigureStore.UpdateAsync`/`DeleteAsync` | Yes (confirmed running through Phase 3) | 17, container port per D-27's recorded deviation (5433 host-side on this dev machine) | — |
| .NET 10 SDK | Build/run | Yes (confirmed, `.planning/PROJECT.md`: "10.0.301 verified present") | 10.0.301 | — |
| EF Core / Npgsql packages | `ExecuteUpdateAsync`/`ExecuteDeleteAsync` | Yes, already referenced (10.0.10 per STATE.md) | 10.0.10 | — |

**Missing dependencies with no fallback:** none.
**Missing dependencies with fallback:** none.

## Security Domain

`security_enforcement` is not set to `false` in `.planning/config.json` — treated as enabled.

### Applicable ASVS Categories

| ASVS Category | Applies | Standard Control |
|----------------|---------|--------------------|
| V4 Access Control | **Yes — the phase's one real security surface** | `FigureStore.UpdateAsync`/`DeleteAsync` filter on `f.Id == figureId && f.UserId == userId`, mirroring Phase 3's `T-03-01` IDOR mitigation for `InsertAsync`. `userId` is read once from the `user_id` cookie claim (D-51) in `OnInitializedAsync` and never from client-supplied data. |
| V5 Input Validation | Yes | Every pointer coordinate crosses `CanvasCoordinates.FromPage` then `Movement.ClampMove`, exactly as Phase 3's `T-03-02` mitigation already established for draw coordinates — a crafted/out-of-range `PageX`/`PageY` cannot produce an out-of-canvas write. |
| V2 Authentication | No (unchanged from prior phases) | `[Authorize]` on the canvas page already gates the whole component (D-51); this phase adds no new entry point. |
| V3 Session Management | No (unchanged) | — |
| V6 Cryptography | No | Not applicable to this phase. |

### Known Threat Patterns for this stack

| Pattern | STRIDE | Standard Mitigation |
|---------|--------|-----------------------|
| IDOR — dragging/deleting another user's figure by guessing/observing its `id` | Elevation of Privilege | `Where(f.Id == figureId && f.UserId == userId)` on both `UpdateAsync` and `DeleteAsync` — the row simply won't match for a foreign figure, and the affected-row count is 0, which the app already treats as benign staleness (D-10), so no special-case error handling is needed to close this hole. |
| Crafted out-of-range pointer coordinates producing an out-of-canvas write | Tampering | `Movement.ClampMove`'s per-axis clamp runs before every render and before the value that gets persisted on drop — identical trust boundary to Phase 3's `T-03-02`. |
| Double-write from overlapping commit paths (`pointerup` racing a `pointerleave`-triggered commit) | Tampering / double-write | The `dragging = false` + local-capture-before-first-`await` re-entrancy guard in `CommitDragAsync`, identical in shape to Phase 3's `T-03-15` mitigation for `CommitAsync`. |

## Sources

### Primary (HIGH confidence)
- `docs/DECISIONS.md` (D-01 through D-58, `THE SCHEMA` section) — the project's sole specification; every locked-decision citation above (D-04, D-09, D-10, D-15, D-22, D-24, D-29, D-30, D-31, D-33, D-36, D-37, D-38, D-39, D-41, D-45, D-48, D-50, D-51, D-52, D-58) traced directly against this file.
- `src/BlazorCanvas/Geometry/Movement.cs`, `Box.cs`, `CanvasBounds.cs`, `CanvasCoordinates.cs`, `DrawGesture.cs`, `CircleEncoding.cs`, `MinSizeGuard.cs`, `FigureType.cs`, `FigureTypeNames.cs` — read directly, current on-disk state.
- `src/BlazorCanvas/Data/FigureStore.cs`, `Figure.cs`, `CanvasDbContext.cs` — read directly, current on-disk state.
- `src/BlazorCanvas/Components/Pages/Home.razor`, `Home.razor.css` — read directly, current on-disk state (post-Phase-3).
- `src/BlazorCanvas/Components/Canvas/Toolbar.razor`, `Toolbar.razor.css`, `FigureShape.razor` — read directly, current on-disk state.
- `tests/BlazorCanvas.Tests/Geometry/ClampTests.cs`, `Data/FigureStoreTests.cs` — read directly, proving `Movement.ClampMove` and `FigureStore`'s existing test conventions.
- [PointerEventArgs Class (Microsoft.AspNetCore.Components.Web)](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.components.web.pointereventargs?view=aspnetcore-10.0) — fetched directly; confirms `Button` (0/1/2), `Buttons` (bitmask, left=1), `PageX`/`PageY`, `ClientX`/`ClientY`, `OffsetX`/`OffsetY`, `ScreenX`/`ScreenY` all exist on the inherited `MouseEventArgs` base, plus `PointerId`/`IsPrimary`/`PointerType` on `PointerEventArgs` itself. `[VERIFIED: learn.microsoft.com official API reference]`
- [ASP.NET Core Blazor event handling](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/event-handling?view=aspnetcore-10.0) — fetched directly; confirms `@on{DOM EVENT}:stopPropagation` accepts a bound `bool` C# expression, with the documented caveat that its effect is scoped to Blazor's own event dispatch and does not extend to native DOM propagation outside Blazor's control. `[VERIFIED: learn.microsoft.com official conceptual doc]`
- [ExecuteUpdate and ExecuteDelete - EF Core](https://learn.microsoft.com/en-us/ef/core/saving/execute-insert-update-delete) — fetched directly; confirms immediate execution, no change-tracker interaction, affected-row-count return value used for the exact `if (numUpdated == 0)` pattern D-10 needs, and the "only relational providers" limitation (satisfied — Npgsql is relational). `[VERIFIED: learn.microsoft.com official EF Core doc]`

### Secondary (MEDIUM confidence)
- `.planning/phases/BC-03-the-canvas-drawing/03-05-PLAN.md`, `03-05-SUMMARY.md` — the closest existing analog (draw gesture state machine, re-entrancy guard pattern, `pointerleave`-not-`pointerout` discipline, threat-register shape) — read directly, treated as an established in-project pattern to mirror, not an external source.
- `.planning/phases/BC-03-the-canvas-drawing/03-UI-SPEC.md` — the "Canvas & Cursor Spec" and Color-table sections anticipate this exact phase's grab/grabbing cursor states and selected-outline color; read directly.

### Tertiary (LOW confidence)
- None — every claim above traces to either the project's own locked decisions/existing code, or an official Microsoft Learn page fetched directly in this session.

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — no new packages; all APIs already present and already exercised by Phase 1–3 code
- Architecture: HIGH — every pattern (stopPropagation binding, ExecuteUpdate/Delete semantics, page-spanning wrapper) confirmed against official Microsoft docs fetched directly this session, not training-data recall
- Pitfalls: HIGH — four of five pitfalls are direct extensions of pitfalls Phase 3 already hit and documented fixes for (re-entrancy, pointerout/pointerleave, zero-row-is-not-an-error); the fifth (Blazor round-trip cost) is an explicitly accepted, already-scoped cost from D-07

**Research date:** 2026-07-16
**Valid until:** 30 days (stable framework APIs; no fast-moving dependency in this phase's scope) — but re-verify against `docs/DECISIONS.md` first if any decision referenced above is amended before planning begins, since that document, not this research, remains the sole source of truth.
