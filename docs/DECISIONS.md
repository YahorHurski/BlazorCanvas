# Decision Log

Design decisions for the drawing-canvas web app. **This is the only specification. There is no
other documentation.**

**Guiding principle:** MinVP. Smallest thing that works. No speculative features.
Every decision here was made explicitly by the user; nothing was defaulted.

---

# ⚠️ READ THIS FIRST — how to use this document

**Entries are NOT in file order past D-32**, and **several early entries were later amended,
superseded, or reversed.** Every amended entry carries a ⚠️ banner naming its amendment — but
**check this index before implementing from any entry.**

> # 🛑 STOP — the storage model was REPLACED in v1.11
>
> **`THE SCHEMA` (the DDL below) and D-22 (`four integers = the bounding box`) are DEAD.**
> Everything about *how a figure is stored* is superseded by **D-59…D-69** at the end of this
> file. Position and shape are now stored separately; shape lives in a `geometry jsonb` column.
>
> The old entries are kept because **why** they were reversed is the useful part — but never
> implement storage from them.

**If you read only three things, read: `D-59…D-69` (the current storage model),
`D-53` (the sync message contract), and `D-36` (the clamp).**

## Status index

| ID | Topic | Status |
|----|-------|--------|
| D-01…D-04 | Postgres · Blazor · one canvas per user · draw/drag/delete | Locked |
| **D-05** | Figure types | ⚠️ **Amended by D-13**; **"only four types" lifted by D-65** — new types are now data, not schema |
| D-06 | SVG, not `<canvas>` | Locked (⚠️ v1.1: "no JS" perk phrasing removed; SVG choice stands) |
| **D-07** | Blazor Server hosting | ⚠️ **Claim retracted by D-34** — "no HTTP code" was false |
| D-08…D-10 | Plaintext passwords · per-operation saves · zero-row guard | Locked |
| **D-11** | Live cross-tab sync | ⚠️ **Amended by D-40, D-47, D-53, D-54** — the "idempotent upsert" it describes is a **bug** |
| **D-12** | Two-table schema | 🛑 **SUPERSEDED by D-64** — four tables; `canvases` now exists |
| D-13 | Circle drawn centre-out | Locked (storage changed by D-60: `{"r": …}`) |
| **D-14** | One fixed style | 🛑 **SUPERSEDED by D-66** — style is per-figure, in a validated `style jsonb` |
| **D-15** | Delete | ⚠️ **Delete *key* superseded by D-33** (toolbar button) |
| **D-16** | Toolbar | ⚠️ **Superseded** — seven buttons, not four (D-30, D-33, D-73) |
| D-17…D-21 | Login · canvas 1:1 · **1472×828** · integers · triangle | ⚠️ D-18/D-19 amended in v1.1; 🛑 **D-20 (integers) superseded by D-61** (`numeric`); 🛑 **D-21 (triangle from a box) superseded by D-60** (three stored points) |
| **D-22** | **Geometry storage** | 🛑 **SUPERSEDED BY D-59/D-60.** Four integers are no longer the model. Read it only for *why* — the reversal reasoning still teaches |
| **D-23** | Guards | ⚠️ Its "one shared guard" claim is **retracted by D-50** |
| D-24 | Figures stop at the edge | Locked (formula in **D-36**) |
| D-25…D-29 | Logout · session cookie · Docker · .NET 10 · draw clamps | Locked |
| **D-30** | Pointer tool | ⚠️ Says "five buttons" — **it is seven** (D-33, D-73) |
| D-31, D-32 | Selection behaviour · accepted usability costs | Locked (⚠️ **D-31 amended in v1.1**: blue dashed trace + selection lifecycle) |
| D-33…D-35 | Delete button · SSR login · draw preview | Locked (⚠️ **D-33 motivation corrected in v1.1**) |
| **D-36** | **The clamp formula + inclusive bounds** | Locked — *appears late in the file* (⚠️ v1.1: W×H now 1472×828) |
| **D-37** | **Drag termination** | Locked — *appears late in the file* (⚠️ v1.1: motivation corrected) |
| D-38…D-49 | Fill · z-order · resurrection fix · normalisation · migrations · layout · usernames · errors · columns · throttle · click-vs-drag · project structure | Locked, **except:** 🛑 **D-39 (`id` IS the z-order) superseded by D-62/D-63**; 🛑 **D-41 (normalisation, the line landmine) superseded by D-60**; 🛑 **D-46 (`type` CHECK, no `created_at`) superseded by D-65/D-68** |
| D-50…D-58 | Per-type guard · identity & routes · save-failure policy · **message contract** · mid-drag rule · background · logout · draw abort · constants | Locked (⚠️ v1.1: **D-57 motivation corrected**, **D-58 selection style + canvas size amended**); 🛑 **D-50 superseded by D-60** — geometry validation is C#-only now |
| **D-59…D-69** | **THE CURRENT STORAGE MODEL** (v1.11) | **Locked — implement from here** |
| **D-70…D-73** | **v1.12 five-pointed star** | **Locked — star5 geometry, catalog exposure, startup seed convergence, seven-button toolbar** |

> ⚠️ **v1.1 milestone note (see `.planning/PROJECT.md` for the full summary):** the
> **"no hand-authored JavaScript" rule is REMOVED.** It was never the real motivation (MVP
> simplicity was); it is corrected in-place at D-06, D-18, D-33, D-37, D-57. Removal is
> permissive — it changes no code today and only re-opens future options (Delete-key,
> `setPointerCapture` drag, Escape-to-cancel). Also amended: **canvas 1280×720 → 1472×828**
> (D-19/D-36/D-58/D-18) and **selection red outline → blue dashed trace + lifecycle** (D-31/D-58).
> Framework JS (`blazor.web.js`, scaffolded `ReconnectModal.js`) was never in scope either way.

## The landmines

Written here because each one **fails silently** if missed:

1. **Never clamp coordinates individually** (D-23, D-36). Clamp the *movement delta*, then
   apply it uniformly. Clamping one edge alone resizes the figure instead of moving it.
   *(Still true after v1.11 — the delta now moves `x, y`, and the clamp reads `bbox_*`.)*
2. ~~**Never normalise a line by sorting its axes**~~ — **DEFUSED in v1.11 (D-60).** A line is
   now stored as its two points, so there is no axis-sorting step to get wrong and no
   line-specific normalisation arm. Kept here so the class of bug is remembered: *a figure that
   still renders after being corrupted reports nothing.*
3. **Never use `OffsetX`/`OffsetY`** (D-18, D-43). Use `PageX`/`PageY`. `OffsetX` is relative to
   the *event target* — and every drag and every selection begins on a figure.
4. **Never trust `geometry` or `style` off the wire** (D-60, D-66). Both are client-supplied
   JSON. Parse into a typed record, validate, and **re-serialise from the record** — never
   store what the client sent.
5. **`bbox_*` is a cache** (D-67). If a write path forgets to recompute it, the figure clamps
   against the wrong edge. Exactly one place computes it.

---

## D-01 — Database: PostgreSQL

**Status:** Locked (given in brief)

PostgreSQL is the datastore. Not negotiable — stated as a project requirement.

---

## D-02 — UI framework: Blazor

**Status:** Locked (given in brief)

Blazor is the UI framework. Not negotiable — stated as a project requirement.

---

## D-03 — Product scope: one canvas per user

**Status:** Locked (given in brief)

A user has exactly one canvas. There is no canvas list, no "new canvas", no naming
or switching between canvases.

---

## D-04 — Feature set: draw, drag, delete

**Status:** Locked

The user can do exactly three things to a figure: **draw** it, **drag** (move) it,
and **delete** it.

Explicitly NOT in scope unless later chosen on purpose: resize, rotate, undo/redo,
z-order control, colours/stroke styling, multi-select, copy/paste, zoom/pan, export.

**Why:** the user is explicitly guarding against unexpected features. Anything not
listed here is out until it is added to this log by name.

---

## D-05 — Figure types: line, rectangle, circle, triangle

**Status:** Locked

Four figure types.

> ⚠️ **AMENDED BY D-13.** This entry originally claimed *"all four are created by the same
> interaction — the user drags out a bounding box."* **That is no longer true.** Line,
> rectangle and triangle are drawn corner-to-corner; a **circle is drawn centre-out** — press
> at the centre, drag outward for the radius (D-13). The circle is a deliberate special case
> at *draw* time.
>
> It is nonetheless **stored** as a bounding box like everything else (D-22, the inscribed
> square). **Interaction and storage are different things** — a distinction this log leans on
> heavily; do not collapse them.

---

## D-06 — Drawing surface: SVG, not HTML5 `<canvas>`

**Status:** Locked

Figures are C# objects rendered by Blazor as SVG elements in the DOM.

**Why:** the brief said "холст" as a *concept* — the literal `<canvas>` element was
never a requirement. With SVG, drag and delete come almost free: each figure is a real
DOM element with its own click handler, so hit-testing and redraw are handled by the
browser. `<canvas>` would mean writing hit-testing by hand (given a click at x,y, which
figure is under it?) and redrawing the whole scene on every change — materially more code
for exactly the three verbs we want. **The motivation is simply less code / free DOM
hit-testing.**

> ⚠️ **v1.1 note:** an earlier version of this decision cited "no JavaScript" as a benefit
> here. That framing is removed — the no-JS rule has been lifted (it was never the real
> motivation; MVP simplicity was). SVG remains the right choice on its own merits above.

**Rejected:** HTML5 `<canvas>`. Only worth it if learning the canvas API were itself a
goal, or if figure counts reached the thousands. Neither applies.

---

## D-07 — Hosting model: Blazor Server (InteractiveServer)

**Status:** Locked

A single Blazor Web App project using the InteractiveServer render mode. Components
talk to PostgreSQL directly via EF Core. There is no Web API layer.

> ⚠️ **RETRACTION — AMENDED BY D-34.** This entry originally also claimed there was
> **"no HTTP code to write."** **That claim was false.** In Blazor Server an interactive
> component **cannot set a cookie** (the HTTP response has already begun by the time the
> circuit runs), so the login form of D-17 cannot issue the session cookie of D-26.
>
> D-34 resolves it: **login/logout are static-SSR pages and endpoints**, and the app therefore
> has **two render modes** — static SSR for `/login` and `POST /logout`, InteractiveServer for
> the canvas (routes in D-51).
>
> The *hosting choice* below stands unchanged. Only the "no HTTP code" boast is retracted.

**Known cost, accepted:** each mouse-move during a drag is a SignalR round-trip to the
server. This is invisible on localhost/LAN and becomes visibly laggy over a poor
connection.

**Side benefit:** Blazor Server already holds a live server→client push channel per
tab, which makes cross-tab live sync comparatively cheap *if* it is ever wanted.

**Rejected:** Blazor WebAssembly + Web API. Dragging would be instant on any network,
but it costs a second project (ASP.NET Core Web API) plus HTTP endpoints and
client-side load/save calls. Too much scope for MinVP.

---

## D-08 — Identity: username + plaintext password, no auth

**Status:** Locked

A `users` table with a password column storing the password **literally as typed —
no hashing, no salting, no security**. Login is a username + password form. This
exists only to answer "whose canvas do I load?".

**Why:** explicitly requested. Real auth (ASP.NET Core Identity, OIDC) is out of scope.

> ⚠️ **This is acceptable only because this is a throwaway learning project.**
> It must never touch real credentials and must never be deployed as-is to the public
> internet. This warning is deliberately recorded so the choice stays a conscious one.

---

## D-09 — Persistence: immediate, per operation. No Save button.

**Status:** Locked

Every operation writes to PostgreSQL the moment it completes:

| User action | Database effect |
|-------------|-----------------|
| Draw a figure | `INSERT` one row into `figures` |
| Drag a figure (on drop) | `UPDATE` that row's coordinates |
| Delete a figure | `DELETE` that row |

**There is NO Save button anywhere in the app.** Nothing to forget; nothing lost on a
crash; no "unsaved changes" state to track or warn about.

**Why this is load-bearing:** it is what makes multiple open tabs *safe*. Each figure is
its own row, so two tabs writing different figures are separate INSERTs that cannot
touch each other. The database is always the merged truth.

**Rejected:** an explicit Save button. It would commit the whole canvas as one unit,
which means a second tab's Save silently erases everything the first tab did since it
loaded — no error, no undo. A Save button is only safe if the canvas is also locked to a
single tab, and that pairing costs more than it is worth here.

---

## D-10 — Mandatory: handle zero-row UPDATE/DELETE (staleness guard)

**Status:** Locked — required regardless of any other decision

Use EF Core's `ExecuteUpdateAsync` / `ExecuteDeleteAsync`, which return an **affected-row
count**. If a drag's UPDATE affects **0 rows**, the figure no longer exists — silently
remove it from that tab's view. No message, no prompt, no merge.

**Why this is needed even with live sync (D-11):** live sync shrinks the "ghost figure"
window to milliseconds, but it does not close it. Blazor Server reconnects a slept or
disconnected tab to its **same in-memory state** — it does not re-read the database. Such
a tab silently misses whatever broadcasts it missed and shows a stale picture
indefinitely. This guard is what stops the user then dragging a figure that isn't there.

**Note:** with `ExecuteUpdateAsync`, no exception is thrown at all. (Had we used tracked
saves instead, EF Core would throw `DbUpdateConcurrencyException` on a 0-row update even
with no concurrency token configured — an unhandled crash of that tab's circuit.)
DELETE-of-a-ghost is naturally idempotent and needs no guard; only UPDATE does.

**Rejected:** concurrency tokens (`xmin` / rowversion). Provably redundant here — the
affected-row count covers the only surviving case. See D-11 for why.

**Manual fallback for the stale-tab case is F5**, which re-reads from Postgres. No
"Reload" button is built: the load-from-database path already runs on page load, so a
button would be permanent UI whose only job is to apologise for the sync feature next to
it.

---

## D-11 — Multi-tab: live sync, with real-time drag glide

**Status:** Locked

Changes made in one tab appear **live** in the user's other tabs. Dragging a figure in
tab A makes it **glide in real time** in tab B — not jump on release.

### The premise that makes this cheap

One human has one mouse. The OS delivers the pointer to one window. **Two tabs therefore
cannot be edited at the same instant** — edits are physically serialised. This is not an
optimisation; it deletes an entire category of work.

**Deleted outright by this premise:** all locking (row locks, advisory locks, "figure is
being edited" flags, drag ownership); all conflict-resolution UX ("this was changed
elsewhere", merge prompts); optimistic concurrency tokens and retry loops; operation
queues, write coalescing, CRDT/OT thinking. The question "which tab wins?" is answered by
physics, not by code.

### Mechanism

A **DI singleton notifier service** holding per-username subscribers. Tabs subscribe on
init and publish after each operation. Blazor Server's existing per-tab SignalR channel
does the actual push when `StateHasChanged` re-renders — this is the "side benefit"
already noted in D-07.

**Delta payloads**, not a "something changed, go re-read" dirty signal. Receiving tabs apply
messages keyed by figure Id.

> ⚠️ **AMENDED BY D-40 — read D-40 before building the sync layer.**
> This entry originally said receiving tabs apply an **"idempotent upsert"**. **That was a
> bug.** An upsert *inserts when the row is absent* — which lets a stale tab's drag broadcasts
> **resurrect a figure another tab correctly deleted.** The corrected rule: **apply is
> idempotent, and a `move` message is UPDATE-ONLY — it must never insert.** See D-40.
>
> ⚠️ **The message contract (kinds and payloads) is specified in D-53, not here.**
> ⚠️ **The throttle is 50 ms with a guaranteed trailing edge — see D-47, not the "~30–50 ms"
> range mentioned below.**

> Deltas are **required** by the live-glide choice: intermediate drag positions are never
> written to Postgres (D-09 saves on drop), so there is nothing for a dirty signal to
> re-read. A dirty-signal design structurally cannot glide.

### Broadcasting ≠ persisting — the crucial distinction

Every `pointermove` during a drag is **already** a server round-trip (D-07's accepted
cost). The server already computes each intermediate position to redraw the acting tab.
Live glide simply **re-broadcasts that position through the in-memory notifier** — one
extra line in a handler that already exists.

**Postgres sees exactly one UPDATE per drag, on drop, exactly as locked in D-09.**
Intermediate positions travel through memory only and are never persisted. Writing them
to the database would be ~100× write amplification and a violation of D-09.

### Irreducible core — must be built, none of it optional

1. **Unsubscribe in `Dispose()`** — else closed tabs leak delegates and publishes hit
   disposed circuits (`ObjectDisposedException`).
2. **`InvokeAsync(StateHasChanged)`** in every handler — the event fires on the
   *publisher's* circuit thread; touching a component from a foreign thread is undefined.
3. **Echo filter** — the acting tab receives its own broadcast. Tag the sender, ignore
   your own. Also prevents a mid-drag self-stomp.
4. **While dragging, ignore ALL incoming broadcasts** — `if (_dragging) return;` — **see D-54.**
   Not merely those about the figure being dragged. Safe under the one-mouse premise: while you
   are dragging, nothing else is happening. **Accepted cost:** this breaks the free multi-device
   degradation noted at the end of this entry — a message arriving from a phone *during* a drag
   is lost permanently until F5. D-54 records that trade deliberately.
5. **Idempotent apply keyed by figure Id — and `move` is UPDATE-ONLY (D-40).**
   ~~Idempotent upsert.~~ An upsert resurrects deleted figures. See the warning above.
6. **Throttle drag broadcasts — 50 ms, trailing edge guaranteed (D-47).** ~~~30–50 ms~~ —
   that was a range, not a value; D-47 pins it.
7. **Key notifier events by `user_id`**, matching the database (D-12) and the cookie claim
   (D-51). ~~By username.~~ Using two different identities for the same user was a needless
   second key.
8. **Clamp → render → broadcast, in that order (D-36).** Never broadcast a raw, unclamped
   position, or the other monitor briefly shows figures outside the canvas.
9. **A zero-row UPDATE broadcasts a delete (D-40).** Otherwise a tab that resurrected a
   ghost keeps it forever.

### Rejected

- **Timer polling** — looks cheapest, costs about the same as the notifier, gives a worse
  result, and does DB work continuously forever. There is no configuration of this app
  where polling is correct.
- **A hand-rolled SignalR hub** — Blazor Server circuits *already are* SignalR
  connections. A hub adds a parallel connection and group management for zero gain.
- **Postgres `LISTEN`/`NOTIFY`** — solves a multi-server problem this app will never have.
- **Client-side tab sync** (`BroadcastChannel`, `localStorage` events) — would add JS
  interop and a second synchronization path for no benefit; Blazor Server's existing circuit
  channel already supplies the required live glide.
- **Canvas locking / single-tab enforcement** — unnecessary once conflicts are impossible.
- **Reconnect-detection plumbing (`CircuitHandler`)** — a real solution to the stale-tab
  case, but D-10's row-count guard plus F5 covers it at a fraction of the cost.

### Accepted consequence (free, undesigned)

Because the apply logic is idempotent and guarded, opening the app on a *second device*
(e.g. a phone — trivially possible since there is no real auth) degrades to
last-write-wins without crashing. Rough multi-device support falls out for free. **Nothing
is built for it.**

---

## D-12 — Schema: two tables. No `canvases` table.

> 🛑 **SUPERSEDED BY D-64 (v1.11) — there are four tables and `canvases` exists.** The objection
> below (a table holding no meaningful data) no longer applies: it holds `width`, `height`,
> `background` and `name`, so the canvas size stops being a compile-time constant. Note that this
> entry's rejection of *"the whole canvas as a single JSONB document"* still stands — v1.11 puts
> **one figure's shape** in jsonb, not the whole canvas; each figure remains its own row, which is
> what D-09's per-operation writes require.

**Status:** Locked

> ⚠️ **The sketch below is illustrative only and is partly out of date** (`created_at` was
> dropped by D-46). **The authoritative DDL is the "THE SCHEMA" section near the end of this
> document.** Implement from that, not from here. Only the *two-table* principle below is
> normative.

```
users     (id, username, password)   ← password stored in PLAINTEXT, see D-08
figures   (id, user_id → users.id, type, x1, y1, x2, y2)
```

**"The canvas" is not an entity in the database.** It is simply the set of figures
belonging to a user. There is no canvas row, no canvas id, no join.

**Why:** a `canvases` table would be one row per user holding no meaningful data — a
table that exists only to be pointed at. If the canvas ever needs real properties (size,
background, a name), it can be introduced then. Today it would be ceremony.

**Rejected:** a three-table `users` / `canvases` / `figures` schema. More conventional,
but it buys nothing for a one-canvas-per-user app.

**Rejected:** storing the whole canvas as a single JSONB document per user. Incompatible
with D-09 — per-operation persistence needs each figure to be its own row so that writes
from different tabs cannot overwrite one another.

---

## D-13 — Circle geometry: centre + radius

**Status:** Locked

For a circle, **the press point is the centre** and the drag distance sets the radius.
This always produces a true circle (never an oval).

**Accepted cost:** the circle is drawn differently from the other three shapes, which are
corner-to-corner. This is a deliberate special case in the drawing code.

**Consequence for storage — resolved by D-22:** the circle's *interaction* is centre-out, but
its *storage* is the square it is inscribed in. So the four shapes **do** end up sharing one
uniform bounding-box model after all: four integers that are always the figure's bounding
box. Interaction and storage are deliberately different things here.

**Rejected:** square-ifying the dragged box (drag a wide flat box, get a small circle that
doesn't fill it — surprising). **Rejected:** allowing ellipses (would change what was
asked for).

---

## D-14 — Appearance: one fixed style. No colours.

> 🛑 **SUPERSEDED BY D-66 (v1.11) at the storage level.** Style is now per-figure, in a validated
> `style jsonb`. **No styling UI ships in v1.11** — every migrated figure keeps exactly the values
> below, so nothing changes on screen. The column exists so that adding a colour picker later is
> UI work only, with no migration.

**Status:** Locked

Every figure renders with the same hard-coded stroke and fill. **No style columns in the
database. No colour picker in the UI.**

Figures are shapes, not decorated shapes — consistent with draw / drag / delete being the
entire feature set (D-04).

**Rejected:** a per-figure colour. It would add a column, a picker control, and then
immediately raise "can I recolour an existing figure?" — which would be a fourth verb we
explicitly do not want.

---

## D-15 — Delete: click to select, then delete

**Status:** ⚠️ **PARTLY SUPERSEDED — the Delete *key* is replaced by a toolbar button (D-33).**
The selection half of this entry still stands. Selection *mechanism* is defined by D-30.

Click a figure to **select** it (rendered with a visible highlight). ~~Press the **Delete**
key to remove it.~~ → **Click the Delete button in the toolbar (D-33).**

**Accepted cost:** this introduces a *selected figure* concept and a highlight state — a
genuinely new idea in the app, though a small one. Selection is **local UI state only**:
it is never persisted, and it is **not** broadcast to other tabs (D-11). Your other
monitor does not show what you have selected.

**Rejected:** right-click to delete. Smaller (no selection concept at all), but instant
and irreversible with no undo, and it would override the browser's context menu.
**Rejected:** an eraser/delete mode in the toolbar. Introduces modes — the app would
behave differently depending on which button is active, and the user must remember to
switch back.

**Note:** a **line** has no interior, so selecting one means clicking near its stroke. A
line needs an invisible wider hit area or it will be frustrating to click.

---

## D-16 — Shape selection: a toolbar

**Status:** ⚠️ **SUPERSEDED — see D-30, D-33 and D-73.** The toolbar is **seven** buttons, not four.
This entry is kept for its rationale only. **Do not implement from this entry.**

~~A toolbar with four buttons~~ — **line / rectangle / circle / triangle**. Click one to arm
it; the armed button stays visibly active. Then drag on the canvas to draw that shape.

> **The current toolbar (authoritative):**
> `[ pointer ] [ line ] [ rectangle ] [ circle ] [ triangle ] [ star ] [ delete ]`
> — the **pointer** was added by D-30 (to distinguish selecting from drawing) and the
> **delete** button by D-33 (replacing the Delete key). The **star** button was added by D-73.

**Rejected:** a dropdown (hides the armed shape behind a click, no at-a-glance state).
**Rejected:** keyboard shortcuts only (fast once learned, but completely undiscoverable —
nothing on screen would tell the user it is possible).

---

## D-17 — Login: one form; an unknown username creates the account

**Status:** Locked

A single username + password form.

- Username **exists** → check the password matches, then load that user's figures.
- Username **does not exist** → create the user right there and open an empty canvas.
- Username exists but **password is wrong** → show an error.

There is **no separate Register page** and no signup flow.

Password comparison is a plain string equality check against the plaintext column (D-08).

**Rejected:** conventional separate Login / Register pages. More familiar and explicit,
but it is a whole second page and second form for an app that has no real accounts.

---

## D-18 — Canvas: fixed size, 1:1

**Status:** Locked — ⚠️ **v1.1: motivation corrected** (fixed size is for MVP simplicity, not
"no JavaScript"; that rule is lifted). Size is now 1472 × 828 (D-19).

The canvas is a **fixed-size rectangle** (exact dimensions: D-19). One canvas unit = one CSS
pixel, on every screen. It does **not** scale to the browser window.

### Technical caveat — a FIXED ASPECT RATIO is not optional (kept regardless of JS)

The user's three hard requirements were: (1) a figure on a big monitor appears in the same
relative place on a small one, (2) size proportions preserved, (3) **a circle must never
render as an oval.**

**A canvas that fills every browser window is mathematically incompatible with these.**
Two windows have different *shapes*. Filling both edge-to-edge while keeping relative
positions means scaling X and Y by different factors — and scaling the axes differently
*is* the definition of turning a circle into an oval. No technology escapes this. The
canvas must therefore have one fixed aspect ratio everywhere. **This is geometry, not a
JavaScript constraint — it holds even now that hand-authored JS is permitted.** If a
window-filling canvas is ever added, it MUST preserve aspect ratio (a letterbox), never
stretch to fit.

### Why a FIXED SIZE (1:1) rather than a scaling letterbox — MVP simplicity

Both a fixed 1:1 canvas and an aspect-preserving *scaling letterbox* (SVG `viewBox` +
`preserveAspectRatio="xMidYMid meet"`) satisfy all three requirements above. The fixed 1:1
canvas was chosen for **MVP simplicity**: at 1:1 the scale factor is pinned to exactly 1, so
the page-to-canvas mapping is two constant subtractions (`canvasX = PageX`,
`canvasY = PageY − toolbar`) with nothing to measure and nothing that can drift.

> ⚠️ **v1.1:** the older text rejected the scaling letterbox *because it needed JavaScript*
> (the render scale factor lives only in the browser and would need a ~10-line interop shim to
> read). The no-JS rule is now lifted, so a scaling letterbox is technically available — but the
> canvas stays **fixed 1:1** anyway: it is simpler, and enlarging the fixed size to 1472 × 828
> (D-19) already gave the wanted extra room without the added complexity.

### Mechanism — how coordinates get in

Anchor the canvas at a **known constant document position**: page margin 0, a toolbar of a
fixed chosen height, canvas immediately below it. Then a pointer event's canvas coordinate
is simply `PageX - 0`, `PageY - toolbarHeight`. Two subtractions that cannot be wrong.

> **Use `PageX`/`PageY` — never `OffsetX`/`OffsetY`.** This is a landmine, not a
> preference. Blazor's `OffsetX/Y` is relative to the **event target** — the topmost
> element under the pointer. Click empty canvas and it is canvas-relative; click *a
> figure* and it becomes relative to **that figure**. Every drag and every selection begins
> on a figure, so an `OffsetX`-based design breaks exactly where it is needed most.
> `PageX/Y` is target-independent and always correct.

Do **not** centre the canvas with `margin: auto` — that reintroduces an unknown offset that
depends on window width, which the server cannot know. Anchor it top-left.

### Accepted costs

- The canvas does not fill the screen. On a large monitor there is visible page background
  around it. The picture is the **same physical size on both monitors** — it does not grow
  on the bigger one.
- If a browser window is **smaller** than the canvas, the page scrolls; a figure near the
  edge is off-screen until scrolled to.
- Browser zoom (Ctrl +/−) scales the whole page uniformly, so it distorts nothing and
  breaks no arithmetic. A free manual escape hatch for odd screen sizes — a workaround, not
  a feature.

### Rejected

- **`preserveAspectRatio="none"` / stretch-to-fit** — fills any window perfectly and
  **ovals every circle.** The most seductive wrong answer.
- **Independent 0..1 normalised coordinates** — the same oval trap smuggled into the
  database, where it is harder to see and permanent. (Is a circle's radius a fraction of
  width, or of height? Either answer distorts.)
- **`preserveAspectRatio="slice"` / crop-to-fill** — no bars, circles stay round, but it
  **crops figures out of existence** on a differently-shaped monitor, with no pan (D-04) to
  reach them. Violates requirement 1 outright.
- **CSS `transform: scale()` + `offsetX`** — browsers have disagreed for years on
  `offsetX` under transforms. Fragile in the invisible way that produces "the shape appears
  40px from where I clicked" bugs.

**Note:** should the user ever want the scaling canvas after all, it is a **pure additive
upgrade** — both designs store coordinates in the same fixed logical space, so the database
schema is identical either way. Nothing built for D-18 would be thrown away.

---

## D-19 — Canvas dimensions: 1472 × 828

**Status:** Locked — ⚠️ **amended in v1.1** (was 1280 × 720; enlarged, user-approved)

The fixed canvas (D-18) is **1472 × 828** logical units — which, at 1:1, are literal CSS
pixels. 16:9 (92×16 by 92×9), matching the shape of most monitors. Sized to fit a maximized
browser window on a 1920 × 1080 monitor — toolbar and browser chrome included — with no
scrolling.

**Enlarging was safe; shrinking would not be.** `0..1280 × 0..720` is a strict subset of
`0..1472 × 0..828`, so every figure stored under the old size stayed legal and kept its exact
position (it now sits in the top-left region of the larger canvas). No migration was needed.
**Shrinking the canvas is still forbidden** — it would push existing figures off the surface,
where they are unreachable and unclampable. That asymmetry is the real content of the old
"this number must never change" rule: coordinates are only meaningful relative to the size, so
the size may grow but must never shrink.

*(v1.0 value, for history: 1280 × 720. The old "accepted cost" — a 1366×768 laptop scrolling
slightly — is obsolete; the new size targets 1080p specifically.)*

---

## D-20 — Coordinates are whole numbers (integers)

> 🛑 **SUPERSEDED BY D-61 (v1.11) — coordinates are `numeric`.** The justification below (integers
> permit exact equality in CHECK constraints) died with the constraints themselves: D-60 moved
> geometry validation into C#. Integers also blocked zoom above 100% and rotation.

**Status:** ~~Locked~~ — superseded

All coordinates are stored as integers. At 1:1 (D-18) they are literally screen pixels, so
sub-pixel precision is invisible to the eye.

**Why it matters beyond tidiness:** integers can be compared for **exact equality** in a
database CHECK constraint. Decimals cannot (comparing floats for exact equality is
unreliable). This means geometric invariants — "this circle's stored points really do
describe a circle" — can be *enforced by the schema* rather than merely trusted in code.
See D-21.

**Accepted cost:** a radius derived from a diagonal drag distance is irrational, so it gets
rounded — by less than one pixel.

---

## D-21 — Triangle: derived from a drag. 2 points.

**Status:** Locked

A triangle is drawn exactly like a rectangle — drag out a box. The triangle has its **apex
at the top-centre** and its **base along the bottom**. Two points define it.

**Accepted cost:** every triangle is isosceles and points upward. Right-angled triangles and
downward triangles are **not possible**. This is a deliberate limitation.

**Rejected:** a free triangle (3 points, 3 clicks, any orientation). It would be a
*different interaction* from all three other shapes — click-click-click instead of drag —
which introduces a half-finished-triangle state to manage (what happens if the user clicks
twice then presses Escape?), plus two more columns.

---

## D-22 — Geometry storage: four coordinates, always. Circle = its inscribed square.

> 🛑 **SUPERSEDED BY D-59 / D-60 (v1.11). This is no longer how figures are stored.**
> Read it for the *reasoning* — the reversal argued below is still instructive, and D-59 keeps its
> central insight (a type-blind move) while dropping the four-integer limit that made vertex
> editing, arbitrary polygons and rotation impossible.

**Status:** ~~Locked~~ — **REVISED, then SUPERSEDED.** *Both this design and its revision were
proposed by the user.* An earlier version of D-22 stored the circle as **centre + rim point**; that was
reversed. See "Why this was reversed" below — the reversal is the important part of this
entry.

**Every figure is stored as exactly four integers — `x1, y1, x2, y2` — all non-null. For
every shape, these four numbers ARE its bounding box.**

| Shape | `(x1, y1)` | `(x2, y2)` |
|-------|-----------|-----------|
| Line | one endpoint | the other endpoint |
| Rectangle | one corner | the opposite corner |
| Triangle | one corner of its box | the opposite corner |
| **Circle** | **top-left corner of the square it is inscribed in** — `(cx − r, cy − r)` | **bottom-right corner** — `(cx + r, cy + r)` |

Recovered on read: **`r = (x2 − x1) / 2`**, **`cx = x1 + r`**, **`cy = y1 + r`**.

> **The drawing interaction is UNCHANGED (D-13).** The user still presses at the centre and
> drags outward; the circle still grows live under the cursor; radius is still the drag
> distance. **Only the storage representation changed.**

### Why this works — the property that makes it safe

Moving a figure is a translation: `x1+=d, y1+=d, x2+=d, y2+=d`. For a circle:

```
recovered radius after move = ((x2 + d) − (x1 + d)) / 2 = (x2 − x1) / 2 = r     ✔ exact
```

**The `d` cancels algebraically, not approximately.** And because coordinates are integers
(D-20), there is no floating-point drift: a circle dragged ten thousand times has a
bit-identical radius.

### Why this was REVERSED from centre + rim point — the decisive reason

**The raw columns must BE the bounding box, or edge-clamping (D-24) cannot be generic.**

Under the old rim-point encoding, a circle at centre (640, 100) with r = 90 was stored as
`(640, 100, 730, 100)`. The min/max of those four columns describes a **horizontal line
segment** — but the circle's true extent is 180 × 180. A generic clamp reading the raw
columns would therefore permit dragging it upward until **90 pixels of circle hung off the
top of the canvas.** Only the right edge worked, and only by coincidence (the rim point
happened to sit there).

Fixing that under rim-point required a **per-type bounding-box function inside the drag
loop** — reintroducing exactly the circle special case the encoding existed to eliminate, and
one that fails *silently* when wrong.

Under the inscribed square, the same circle is stored `(550, 10, 730, 190)` — its actual
bounding box. Clamping is blind again:

```
drag up 50px:  dy_min = 0 − 10 = −10  → clamped → row (550, 0, 730, 180)
               r = (730 − 550) / 2 = 90   ✔ unchanged; top rim exactly on y = 0
```

**There is genuinely no type dispatch left** in either the move or the clamp, for any shape.

> **This reversal is justified by new information, not by changing our minds.** When
> bounding-square was first rejected, **D-24 did not yet exist** — there was no clamping
> requirement, so the encoding's decisive advantage was invisible and its one weakness
> dominated. D-24 changed the facts.

### Bonus: the D-23 landmine becomes LOUD instead of silent

D-23's guard 2 warns against clamping coordinates individually. Under the old encoding that
mistake **silently shrank a circle's radius** and the CHECK still passed. Under the inscribed
square, clamping `x2` alone produces a **non-square box, which violates the CHECK
immediately** — the single easiest way to get this wrong now crashes loudly in development
instead of corrupting quietly in production.

### An oval cannot occur

Two independent defences:

1. **The CHECK is exact.** With integers (D-20), `x2 − x1 = y2 − y1` is exact integer
   arithmetic — no float-equality problem. A non-square circle row **cannot be inserted or
   updated into existence** while the constraint stands.
2. **The renderer takes one scalar.** Render derives `r = (x2 − x1) / 2` — **width only,
   ignoring height** — and emits an SVG `<circle>`, *which has no second radius to distort.*
   Even a corrupted row renders as a circle with a wrong radius, never an oval.

**Honest accounting:** the old rim-point encoding made an oval *unrepresentable by the
encoding itself.* This makes an oval *excluded by an executable constraint plus a renderer
that can only draw circles.* Weaker in principle; equivalent in practice — producing one
would require three independent failures (drop the constraint, hand-edit a row, **and**
rewrite the renderer to emit `<ellipse>`). This is stated plainly rather than papered over.

### The database enforces the convention

```sql
CHECK (type <> 'circle' OR (x2 - x1 = y2 - y1 AND x2 > x1 AND (x2 - x1) % 2 = 0))
```

Exact integer arithmetic. No epsilon, no float-equality problem. The convention is not merely
documented — it is **executable**.

- `x2 - x1 = y2 - y1` — **the box is square.** No oval can be written.
- `x2 > x1` — **the radius is positive.** Catches the degenerate zero-size draw (D-23).
- `(x2 - x1) % 2 = 0` — **the side is even**, so `r` and the centre are always exact whole
  numbers. This costs nothing: **the app can never produce an odd side.** The draw handler
  computes an integer `r = round(distance)`, so the side is always exactly `2r`. The clause
  rejects only rows the app could not have created — a manual `UPDATE`, or a bad migration.
  Radius granularity remains 1 pixel; nothing is quantised away.

### Why this is NOT the rejected "generic columns" trap

The rejected option (see below) stored the **bare radius scalar** in `x2`. That mixed value
spaces: three columns held *coordinates* (which move with the shape) and one held a *length*
(which must NOT change when the shape moves). A uniform translate added `dx` to the radius
and silently grew circles as you dragged them right.

**Here, all four columns are genuine coordinates.** Every value is a real position on the
canvas, and every one responds correctly to translation — the only mutation this app ever
performs.

> **The boundary:** per-type *meaning* is unavoidable in any encoding (the database can
> never know that a rectangle's `(x1,y1)` "means" a corner — meaning always lives in the
> render code). What must never be per-type is **which arithmetic is safe.** Under this
> encoding, `+d` is safe on every column of every row, uniformly. The rejected option was a
> *lie*; this is a *convention* — and conventions are fine when documented and
> machine-checked. Both hold here.

### Benefits

- **Zero NULLs.** Every column is always meaningful.
- **The raw columns ARE the bounding box, for every shape.** This is what makes D-24's
  edge-clamp and D-29's draw-clamp generic. It is the property the previous encoding lacked.
- **One move statement** for all four shapes — no type dispatch, including in the hot
  in-memory drag-glide loop (D-11).
- **One uniform sync payload** for D-11: `{id, type, x1, y1, x2, y2, sender}` — no nullable
  fields, no polymorphic serialisation.
- **One flat C# record** with four integers. No class hierarchy, no null-checks.
- A circle row reads `(340, 240, 460, 360)` — a 120×120 square, so centre (400, 300),
  radius 60. `y2 − y1 = x2 − x1` acts as a **visible checksum**.

### Accepted cost

The convention ("a circle is stored as the square it is inscribed in") must be learned by
anyone reading the table. Less self-documenting than named `cx, cy, radius` columns. Mitigate
with a `COMMENT ON TABLE`.

### Required at draw time

D-13 lets the user press at the centre and drag outward in **any** direction. The draw
handler computes `r = round(distance(press, cursor))`, applies the **circle draw-clamp**
(D-29 — the one genuine type-specific rule left), and writes the square
`(cx − r, cy − r, cx + r, cy + r)`.

### Rejected

- **Centre + rim point** (`(x1,y1)` = centre, `(x2,y2)` = `(cx+r, cy)`; `r = x2 − x1`).
  **This was the previous version of D-22 and was reversed.** It has the same virtues — four
  real coordinates, zero NULLs, exact translation, an oval structurally unrepresentable — but
  its **raw columns are not the bounding box.** A circle's true extent reaches `r` in *all
  four* directions from the centre, and those bounds appear nowhere in the stored numbers.
  D-24's edge-clamp therefore cannot be generic: it needs a per-type bounding-box function
  inside the drag loop, reintroducing the circle special case the encoding existed to
  eliminate — and one that **fails silently.** Reversed once D-24 made clamping mandatory.
- **Generic columns with a bare radius scalar in `x2`** — the trap described above. Silently
  grows circles when dragged right. Looks like the minimal option; is a landmine.
- **Free rim point + Euclidean distance** (`r = √((x2−x1)² + (y2−y1)²)`, rim point in any
  direction). Removes the invariant — but only defends against **rotation**, a verb this app
  will never have (D-04), while admitting irrational radii into an all-integer design and
  destroying at-a-glance readability.
- **Named columns per concept** (`x1,y1,x2,y2` + `cx,cy,radius`, nullable, + CHECK). Equally
  correct and safe; the table would explain itself to a stranger with no convention to learn.
  Rejected in favour of uniformity — 3–4 NULLs per row, a bulkier C# model, and a separate
  move path for circles. A legitimate choice that was consciously not taken.
- **JSONB geometry**, **Postgres geometric types** (`point`/`circle`/`box`), **coordinate
  arrays**, **table-per-type (TPT/TPC)** — all rejected. See the archived analysis; none
  earn their cost for four fixed shapes.

---

## D-23 — Two guards required by D-22

**Status:** Locked — required, not optional

### 1. Reject degenerate draws

A click without a drag (press and release at the same point) while the circle tool is armed
produces `x2 = x1`, which **violates D-22's CHECK constraint and would crash that tab**.

The draw handler must **reject degenerate draws before writing.** This is wanted for all four
shapes regardless — a zero-size rectangle is equally useless — so it is **one shared
minimum-size guard**, not a circle special case.

### 2. Never clamp coordinates individually

Canvas-bounds clamping **is** required (D-24, formula in D-36). It **must clamp the movement
delta and then translate uniformly.** It must **never** clamp `x2`/`y2` independently of
`x1`/`y1` — that resizes the figure instead of moving it, in **every** encoding (it would
distort rectangles too).

> **Good news, since the D-22 reversal:** under the inscribed-square encoding this mistake now
> **fails loudly.** Clamping `x2` alone produces a non-square box, which violates D-22's CHECK
> immediately. Under the old rim-point encoding it silently shrank the radius and the CHECK
> still passed. The easiest way to get this wrong now crashes in development instead of
> corrupting in production.

**Do not add canvas-bounds CHECK constraints to the columns** — but note the *reason*
originally given here ("a figure legitimately overhanging the edge would violate them") is
**stale**: D-24 and D-29 later established that figures live entirely inside the canvas,
always. The correct reason is simply that the app already guarantees it, so the constraints
would be belt-and-braces. See D-36.

---

## Settled: the shape of the geometry

> ⚠️ **This section describes the *interaction*, i.e. what the user's gesture supplies. It is
> NOT the storage format.** Storage is D-22 (four integers, always the bounding box — the
> circle is stored as its **inscribed square**). Do not read the table below as a schema.

Following D-13 (circle drawn centre-out) and D-21 (triangle from a box), the user's gesture
supplies exactly this much:

| Shape | What the gesture gives us |
|-------|---------------------------|
| Line | 2 endpoints |
| Rectangle | 2 opposite corners (axis-aligned — no rotation exists, so 2 points fully determine it) |
| Triangle | 2 corners of the box it is derived from |
| Circle | centre (the press point) + radius (the drag distance) |

> **Note on point counts.** An axis-aligned rectangle needs **2** points, not 3 or 4.
> Storing more would let the database hold points that do not form a right angle — a
> "rectangle" row that isn't a rectangle. Fewer numbers means fewer ways to be wrong.

**There is no square shape.** The shape list is line, rectangle, circle, triangle.

---

## D-24 — Figures stop at the canvas edge

**Status:** Locked

A figure cannot be dragged beyond the canvas boundary. It stops — and it **slides along the
edge** rather than sticking. The clamp is applied **live, on every pointer-move**, and the
clamped position is what gets persisted on release (D-09).

**→ The exact formula, and the inclusive-bounds decision, are in D-36.**

> ⚠️ **Clamping must be applied to the *movement delta*, then translated uniformly across all
> four coordinates. Never clamp `x2`/`y2` independently of `x1`/`y1`** — that would silently
> resize the figure instead of moving it.
>
> **This decision is what forced the reversal of D-22.** Generic clamping requires that a
> figure's four stored columns *be* its bounding box — which the old centre-plus-rim-point
> encoding did not satisfy. See D-22, "Why this was reversed."

**Rejected:** letting figures overhang and be visually clipped. That would have been free
(the SVG clips naturally) and would have required no clamping code at all.

---

## D-25 — Logout button

**Status:** Locked

A logout button clears the session and returns to the login form.

**Why it earns its place in a MinVP:** without it there is no way to become a different
user, which means no way to verify that the one-canvas-per-user rule (D-03) actually works —
that user A genuinely cannot see user B's figures. It is a testability feature as much as a
user-facing one.

---

## D-26 — Session: a session cookie, lasting until the browser closes

**Status:** Locked

After login, a **session cookie** (a cookie with no expiry date) identifies the user. The
browser deletes it when it shuts down.

- Log in **once**; every tab in that browser knows who you are.
- **F5 keeps you logged in** — essential, because F5 is the official fix for a stale tab
  (D-10).
- **Close the browser entirely → logged out.**

This is the standard default behaviour of cookie auth, and it is what makes the two-monitor
scenario (D-11) work without logging in twice.

**Rejected:** a persistent cookie lasting days. Would keep you logged in across browser
restarts, but requires choosing an expiry and means anyone opening the browser is already
signed in as you.

**Rejected:** requiring the password on every page entry. It would fight the app that has
been designed: opening the second monitor's tab would mean logging in again, and **every F5
would throw the user back to the login form.** It also **protects nothing** — passwords are
plaintext by design (D-08), so forced re-entry is inconvenience without benefit.

---

## D-27 — Local PostgreSQL via Docker Compose

**Status:** Locked

A `docker-compose.yml` runs PostgreSQL locally. The user already has Docker, so this costs
nothing new.

**The problem this solves is mundane and worth stating:** PostgreSQL is a server program and
it has to be *running somewhere* for the app to connect to it. That is the entire problem.
**This choice affects nothing about the app's design** — only how a database comes to exist
during development.

**Benefits:** the database is disposable (destroy the container, get a pristine one in
seconds); nothing is permanently installed; the compose file documents exactly what the
project needs, so it runs identically on any machine.

**Rejected:** a native Windows PostgreSQL install (fine, but a permanent background service
and manual version management — and had the user *not* already had Docker, this would have
been the right call; installing Docker Desktop merely to obtain a database would be a bad
trade). **Rejected:** hosted cloud Postgres (Neon/Supabase). Nothing to install, but every
database call would cross the internet — a visible pause on every draw/drop/delete, and no
offline development.

---

## D-28 — .NET 10

**Status:** Locked

.NET 10 (current LTS). Verified present on the machine: SDKs 8.0.418, 9.0.311 and 10.0.301
are installed; Docker 29.1.3 is available.

**Rejected:** .NET 8 (previous LTS — more tutorial material available, which genuinely
matters when learning Blazor, but a shorter remaining support window). **Rejected:** .NET 9
(Standard-Term Support — a shorter support window than either 8 or 10, with no compensating
advantage).

Everything in this design (Blazor Server, EF Core, Npgsql, SVG) behaves identically on all
three versions. This choice carries no design consequences.

---

## D-29 — Drawing also stops at the canvas edge

**Status:** Locked

While drawing a new figure, dragging past the boundary does not grow the shape any further —
the corner clamps to the edge while the cursor keeps moving.

Consistent with D-24, which gives the app **one rule**: *figures live entirely inside the
canvas, always.* Nothing can be created out of bounds, and nothing can be moved out of
bounds.

**→ D-36 carries the formula — including the one genuinely type-specific rule in the app: the
circle draw-clamp, and the "pressing near an edge forces a tiny circle" consequence that falls
out of it.**

**Rejected:** drawing freely and visually clipping the overflow. It would contradict D-24 —
it would be odd to be able to *create* a figure out of bounds but not *move* one there.

---

## D-30 — Selection: a pointer tool

**Status:** Locked — *this closes a genuine hole in D-15, found by the user.*

> ⚠️ **AMENDED BY D-33 AND D-73 — the toolbar has seven buttons, not five.** This entry says "five"
> throughout because it predates D-33, which added the **Delete** button (replacing the Delete
> key), and D-73, which added the **Star** button. **The authoritative toolbar is:**
>
> `[ pointer ] [ line ] [ rectangle ] [ circle ] [ triangle ] [ star ] [ delete ]`
>
> Everything else in this entry — the pointer tool, the modes, the rationale — stands.

**The problem D-15 left open:** it said "click a figure to select it" but never said how the
app distinguishes *selecting* from *drawing*. A shape tool is always armed — so what happens
when you press the mouse down on top of an existing rectangle? Draw a new circle there, or
grab the rectangle? D-15 did not answer this, and it is the core interaction, not a detail.

**Resolution: the toolbar gets a fifth button — a pointer / select tool** (D-16 is amended
from four buttons to five):

```
[ pointer ] [ line ] [ rectangle ] [ circle ] [ triangle ]
```

- **Pointer armed:** clicking a figure selects it; dragging a figure moves it.
- **A shape armed:** dragging always draws that shape — **even on top of existing figures.**

**Accepted cost:** the app has modes. You must remember which tool is armed, and switching
between drawing and moving costs a click.

**Rejected:** no select tool — "press a figure to grab it, press empty canvas to draw."
Would have needed no fifth button and no modes at all. Rejected because of a hard
consequence: **you could never start drawing inside an existing figure.** Drawing a small
circle inside a big rectangle would be impossible — pressing inside the rectangle would grab
the rectangle. You would have to draw the circle outside and drag it in.

---

## D-31 — Selection appearance and behaviour

**Status:** Locked — ⚠️ **amended in v1.1** (new indicator style + explicit selection lifecycle)

### Appearance (v1.1)

- **A selected figure is marked by a thin (~1px) blue + white dashed trace drawn along the
  figure's OWN outline** — not a bounding box — rendered as the **topmost layer** with
  `pointer-events: none` (values pinned in D-58). Because it rides the actual shape and sits on
  top of everything, the selection stays visible even when the figure is dragged *behind* a
  larger figure. Blue is consistent with the app's existing accent (`#1D4ED8`, the login CTA);
  the white under-stroke keeps it legible on the black outline, the white fill, and the grey page.
- The trace lives in a single overlay layer (rendered after all figures), not inside each
  figure's own element — that is what guarantees it is never occluded, and it tracks the live
  drag position.
- *(v1.0 was: a solid **red, 2px** outline on the figure itself. Replaced — red was the palette's
  only outlier and read as "danger" next to the Delete button; it also mutated the figure so
  "selected" and "the figure is red" were ambiguous.)*

### Selection lifecycle (v1.1 — previously unspecified)

- **The pointer tool is armed on page load.** The app opens ready to select and move, not to
  draw — so a stray first click cannot create a figure. (As in Figma, Paint, and most editors.)
- **At most ONE figure is ever selected.** The rules below preserve this invariant automatically.
- **Drawing a figure selects it** (the tab that drew it — never broadcast, see below), and **the
  tool stays armed** so you can keep drawing. The just-drawn figure stays selected until you start
  the next gesture. This is the draw-gesture analog of D-48 ("starting a drag also selects, and it
  stays selected after the drop").
- **Deselection triggers — any of these clears the current selection:** pressing the canvas
  outside the selected figure; arming any tool; pressing anywhere on the **toolbar EXCEPT the
  Delete button** (Delete stops propagation so it can act on the selection). Pressing a figure
  selects *that* figure instead.
- **Overlapping figures:** a click hits the topmost, which in SVG is whichever was drawn last.
  This comes free from the DOM (D-06); no code required.
- Selection is **local UI state only** — never persisted, never broadcast to other tabs (D-11).
  Your other monitor does not show what you have selected; a figure arriving via sync is **not**
  selected in the receiving tab.

**Still rejected:** a thicker stroke as the highlight (reads as a different-looking figure).
**Still rejected — and this is why the v1.1 indicator is a trace, not a box:** a dashed
*bounding box* implies resize handles at its corners, and there is no resize (D-04). The v1.1
indicator deliberately traces the figure's own outline instead, so it never suggests a corner to
drag.

---

## D-32 — Two usability costs, accepted deliberately

**Status:** Locked — recorded so they are known, not discovered

### 1. The minimum-size guard was not raised to a ~5-pixel threshold

> ⚠️ **Framing amended by D-50.** This section originally called the guard "one shared rule".
> **The guard is PER-TYPE** (D-50) — a zero-height *line* is legal, a zero-height *rectangle*
> is not. Only the framing below is stale; its **substance stands**: the guard was deliberately
> not raised to a ~5-pixel threshold, so it rejects only *degenerate* draws, not merely small
> ones.

**Consequence:** a stray click-drag of one or two pixels while a shape is armed will create a
tiny figure that is nearly invisible and very hard to select. Not dangerous — just annoying.

### 2. Lines have no widened hit area

**Consequence:** a line is one pixel thin, so selecting one means clicking within about a
pixel of it. **This will be felt the first time a line needs deleting.**

The standard fix — an invisible thicker transparent stroke behind each line, purely as a
click target — was **considered and not taken.** It is additive and changes nothing else, so
it can be added at any time.

---

---

# Framework-seam decisions (added after the zero-context audit)

> An audit of this log by an implementer with no memory of the conversation found the log
> **airtight wherever the human was in the room** (data model, sync semantics, geometry,
> scope) and **silent wherever the framework was in the room** (cookies, HTTP, pointer
> capture, keyboard focus, schema creation, layout constants, error paths). Every blocker it
> found lived in the second category. The decisions below close those gaps.

## D-33 — Delete is a toolbar button, not the Delete key

**Status:** Locked — supersedes the Delete-key half of D-15.
⚠️ **v1.1: motivation corrected** (MVP simplicity, not "no JavaScript").

**Motivation — MVP simplicity and unambiguous behaviour:** select a figure, then click a
**Delete button in the toolbar.** No focus dependencies, works the same everywhere, obvious at
a glance (the button greys out when nothing is selected, D-58).

**Why not the Delete key (still a real caveat):** a pure-Blazor `@onkeydown` fires only on a
*focused* element, so a keyboard Delete would need the canvas to hold `tabindex`/focus — and the
moment you click any toolbar button, focus moves there and Delete silently stops working until
you click the canvas again. Subtly broken in a way that reads as a bug. The toolbar button has
none of that.

> ⚠️ **v1.1:** the older text rejected the Delete key partly *because a robust version needed
> JavaScript* (a document-level key listener). That rule is now lifted, so a **Delete-key
> shortcut could be added later** as its own decision — the toolbar button stays regardless.

The toolbar is therefore **seven buttons**:

```
[ pointer ] [ line ] [ rectangle ] [ circle ] [ triangle ] [ star ] [ delete ]
```

**Accepted cost:** reaching for the mouse instead of the key that every drawing tool has
trained you to press.

**Rejected:** the Delete key via a focus-dependent handler (works only while the canvas holds
focus — feels broken). **Rejected:** the Delete key via a small JS shim (works perfectly, but
would have been the project's only JavaScript).

---

## D-34 — Login: a static-SSR page plus cookie-auth middleware

**Status:** Locked — **and it corrects a false claim in D-07**

**The problem the audit found:** in Blazor Server's InteractiveServer mode, **an interactive
component cannot set a cookie.** By the time the circuit is running, the HTTP response has
already begun and `HttpContext` is no longer usable — `SignInAsync` needs a real HTTP
request/response cycle. **The login form as described in D-17 physically cannot issue the
cookie that D-26 requires.**

> ⚠️ **This means D-07's claim — "there is no Web API layer and *no HTTP code to write*" —
> was wrong.** Every workable mechanism writes some HTTP-level code. D-07's *hosting* choice
> (Blazor Server) stands; only that parenthetical boast is retracted.

**Resolution:**
- The **login page renders as static SSR** (not interactive). Its form POSTs.
- A handler validates against the `users` table (creating the user if the username is new,
  per D-17), calls `SignInAsync`, and **redirects** to the interactive canvas page.
- **Cookie authentication middleware** in `Program.cs`, configured for a **session cookie**
  (no expiry — dies with the browser, per D-26).
- **Logout** (D-25) posts to a sign-out endpoint that clears the cookie and redirects to
  login.

**Consequence:** the app has **two render modes** — static SSR for login/logout, and
InteractiveServer for the canvas. This is normal for .NET 8+ Blazor Web Apps, but it is a
real structural fact that D-07 did not anticipate.

**Note:** cookie-auth middleware is **not** ASP.NET Core Identity. D-08 rejects Identity
(accounts, hashing, reset flows); this is just the cookie plumbing, with the plaintext
password check of D-08/D-17 sitting behind it.

**Rejected:** hand-written `POST /login` / `POST /logout` endpoints setting a plain unsigned
cookie. Less framework machinery and every line readable — but it is still HTTP code, and it
sacrifices the well-trodden path for no real saving. **Rejected:** per-tab session storage
(no cookie) — it would **break D-26 outright**: the second monitor would need its own login,
and F5 (our fix for a stale tab, D-10) would log the user out.

---

## D-38 — Appearance: white fill, black outline

**Status:** Locked — completes D-14 (which fixed "one style" but never named it)

Every figure renders with a **white fill and a black outline**. (A line has no interior, so
only the stroke applies.)

**Why the fill matters far more than it looks:** SVG does **not** register clicks inside an
*unfilled* shape. A hollow rectangle's interior is not clickable by default. Had figures been
wireframes, **D-30's entire rationale would have collapsed** — "pressing inside a rectangle
grabs the rectangle" would simply not have been true, and (combined with D-32's un-widened
line hit-area) selecting anything would have been miserable. A solid fill makes the interior
clickable with no tricks.

**Accepted cost:** an opaque white fill means overlapping figures **fully occlude** each
other — the one on top hides what's beneath it. Combined with D-31 (a click hits the topmost)
this is coherent, and z-order is deterministic (D-39).

**Rejected:** hollow outlines with `pointer-events` set so interiors stay clickable
(wireframe look, solid-shape behaviour — but one SVG attribute doing invisible magic).
**Rejected:** true wireframes where only the stroke is clickable — undermines D-30.

Selection is drawn as a **coloured outline** (D-31), which reads unambiguously against the
black default.

---

## D-39 — `figures.id` is a sequential integer, and it IS the z-order

> 🛑 **SUPERSEDED BY D-62 / D-63 (v1.11).** The id is now a `uuid` (so undo can restore a figure
> as *the same* figure) and draw order lives in its own `z numeric` column, unique per canvas.
> The problem stated below — *creation order must be reconstructible from the database after F5*
> — is unchanged and is now solved by `ORDER BY z`, with `z` backfilled from the old `id` so the
> existing stacking survives the migration exactly.

**Status:** Locked

`figures.id` is a **database-generated sequential integer**. Figures load with `ORDER BY id`.

**Why this is load-bearing, not a detail:** D-31 says the topmost figure is whichever was
drawn last. Within a session that comes free from the DOM. **After F5 it does not** — the
render order is whatever the load query returns, so creation order must be *reconstructed
from the database.* A sequential id recovers it exactly, and the id doubles as the z-order.

**Accepted cost:** the id does not exist until the INSERT completes. So drawing is strictly
**insert → get id → broadcast** (D-11's payload is keyed by id). The broadcast cannot be
fired optimistically.

**Rejected:** a client-generated UUID. The id would exist *before* the insert, letting the tab
broadcast immediately without waiting for the database — but **a UUID carries no creation
order**, so z-order would depend entirely on `created_at`, and two figures drawn in the same
millisecond would have undefined stacking.

---

## D-40 — Killing the resurrection hole (both fixes)

**Status:** Locked — closes a genuine bug in D-11 that neither the user nor Claude spotted

**The bug the audit found:** D-11's *idempotent upsert* can **resurrect a deleted figure.**
An upsert inserts when the row is absent — that is what "upsert" means. So:

1. Tab A sleeps and misses a delete broadcast (exactly the stale-tab case D-10 exists for).
2. Tab A wakes and drags the ghost figure. Its drag-glide broadcasts go out.
3. Tab B — which correctly deleted the figure — **upserts it back into existence.**
4. Tab A's drop UPDATE hits 0 rows, so tab A drops the ghost (D-10). **But nothing tells tab
   B**, which keeps the resurrected figure until F5.

**Both fixes are applied:**

1. **A move broadcast may only UPDATE — never INSERT.** If the receiving tab does not already
   know the figure, it **ignores the message entirely.** A figure can only ever be created by
   a *draw* broadcast. This kills resurrection at the root: a move can no longer conjure a
   figure into existence.
2. **A zero-row UPDATE broadcasts a delete.** When the dragging tab's UPDATE returns 0 rows
   (D-10 — it discovers the figure is gone), it publishes a delete so every other tab drops
   the ghost too.

Fix 1 prevents the bug; fix 2 cleans up any tab that somehow acquired a ghost anyway. A few
extra lines for a design that cannot drift.

> **Amend D-11:** "idempotent apply makes replays structurally harmless" was **overstated.**
> It is true for *draw* and *delete*; it was false for *move*, which is what created this
> hole. The corrected rule: **apply is idempotent, and move is update-only.**

---

## D-41 — Normalise on write — but NOT the same way for every shape

> 🛑 **SUPERSEDED BY D-60 (v1.11) — and its landmine is DEFUSED, not merely documented.**
> The line's special arm existed only because the line was the one figure whose four columns were
> its endpoints rather than a bounding box. A line is now stored as its two points, so there is
> no axis-sorting step to get wrong. Keep reading it for the *class* of bug it describes: a
> corrupted figure that still renders reports nothing.

**Status:** Locked

Coordinates are put into canonical order **once, before the INSERT.** The database then
contains only well-formed rows, so rendering and the clamp read the four numbers and trust
them. Normalisation exists in **exactly one place** in the app.

> ⚠️ **LANDMINE — the normalisation is not uniform, and getting it uniform would silently
> break lines.**
>
> **Rectangle / triangle / circle → sort the axes independently** (`x1 = min(x1,x2)`, etc.).
> This is safe: corners `(0,100)` and `(100,0)` describe **exactly the same rectangle** as
> `(0,0)` and `(100,100)`. A box is a box. (The triangle derives from that box; the circle is
> already canonical by construction — `(cx−r, cy−r, cx+r, cy+r)`.)
>
> **Line → swap the WHOLE POINT PAIR, never sort axes independently.** A line from `(0,100)`
> to `(100,0)` is a diagonal running up-and-right. Sort its axes separately and you get
> `(0,0)`→`(100,100)` — **the opposite diagonal.** The line is flipped into a different line.
> A line is a segment between two specific points, not a box, and its diagonal direction is
> real information. Swap the points as a unit (if `x1 > x2`, or if `x1 == x2` and `y1 > y2`).

**Consequence:** after normalisation, `x1 ≤ x2` for every shape, and `y1 ≤ y2` for rectangle,
triangle and circle — **but *not* for a line**, whose `y` may still run either way. This is
why **D-36's clamp keeps its `min`/`max` bounding-box computation.** That computation is still
fully generic (no type dispatch); it simply cannot be dropped.

**Why on write:** the alternative — storing raw drag order and sorting at every read — spreads
normalisation across the renderer, the clamp, and the triangle's apex formula. Forgetting it in
any one produces its own distinct bug: an upside-down triangle (breaking D-21's promise that
triangles always point up), a rectangle with negative width that will not render, or a clamp
that lets a figure escape the canvas. **One guaranteed normalisation beats several remembered
ones.**

**Accepted cost:** the stored row is the two points you dragged, *tidied*. For a rectangle or
triangle that is meaningless. For a line it means the endpoints may come back swapped from the
order you drew them — which **nothing in this app can detect** (no arrowheads, no direction, no
start marker).

### The resulting per-type CHECK constraints

```sql
-- circle: square, positive, even side (D-22)
CHECK (type <> 'circle'   OR (x2 - x1 = y2 - y1 AND x2 > x1 AND (x2 - x1) % 2 = 0))
-- rectangle / triangle: a real box, no zero-width or zero-height (D-23 guard 1)
CHECK (type NOT IN ('rectangle','triangle') OR (x2 > x1 AND y2 > y1))
-- line: normalised left-to-right, and not a zero-length point (D-23 guard 1)
CHECK (type <> 'line'     OR (x2 >= x1 AND (x2 > x1 OR y2 <> y1)))
```

Note a **line may legitimately have `y2 < y1`** (an up-and-right diagonal) and may have
`y1 = y2` (horizontal) or `x1 = x2` (vertical) — which is exactly why it gets its own,
looser constraint. Applying the rectangle's constraint to lines would forbid horizontal and
vertical lines entirely.

---

## D-42 — Schema via EF Core migrations, applied on startup

**Status:** Locked

The C# model is the source of truth. EF Core generates migration files (versioned in source
control) and the app applies them automatically at startup.

> ⚠️ **EF will not emit these on its own — they must be configured explicitly, or they will
> silently not exist:**
> - the **CHECK constraints** of D-22 and D-41 (via `HasCheckConstraint` in `OnModelCreating`)
> - the **`COMMENT ON TABLE`** that documents the circle-as-inscribed-square convention
>   (D-22's stated mitigation for its one accepted cost)
>
> Every geometric guarantee in this design rests on those constraints. Forgetting them
> converts a machine-checked invariant back into a hoped-for convention.

**Rejected:** a hand-written SQL init script run by Docker on first container creation. The
DDL would be completely explicit and readable — the CHECKs simply *there* in the file rather
than conjured by a tool — but the C# model and the SQL could drift apart, and rebuilding the
schema would mean destroying the container.

---

## D-43 — Page layout: a 48px toolbar, no canvas border

**Status:** Locked — this is the constant every coordinate in the app flows through

- Page margin **0**.
- **Toolbar: exactly 48px tall**, at the top.
- **Canvas immediately below**, at document position **(0, 48)**. Anchored top-left — *not*
  centred (D-18: `margin: auto` would reintroduce an unknown offset that depends on window
  width, which the server cannot know).
- **No CSS border on the SVG.** The canvas edge is visible from the contrast between the white
  canvas (D-38) and the page background.

The coordinate mapping is therefore exactly:

```
canvasX = PageX
canvasY = PageY − 48
```

**Why no border:** a CSS border **shifts the SVG's interior by its own width**, so a 1px
border would make the mapping `PageY − 48 − 1`. Correct, but it is one more constant that can
be silently forgotten — precisely the off-by-one that produces *"the shape appears slightly
off from where I clicked."* Removing the border removes the trap.

**Reminder (D-18):** use `PageX`/`PageY`, **never** `OffsetX`/`OffsetY`. `OffsetX` is relative
to the *event target*, and every drag and every selection begins on a figure.

---

## D-44 — Usernames are case-insensitive

**Status:** Locked

**"Egor" and "egor" are the same user.** Usernames are stored lowercased (or compared
case-insensitively). Whitespace is trimmed; an empty username is rejected.

**Why this is not a nicety:** PostgreSQL's default collation is **case-sensitive**, so doing
nothing gives you the opposite behaviour — typing your name with a stray capital would
silently drop you into a *different, empty canvas* (because D-17 creates unknown usernames
automatically). To the user that looks exactly like **their work vanished.**

`username` is **UNIQUE** — D-17's create-if-unknown logic presupposes it.

**Rejected:** case-sensitive usernames (Postgres's default — free, and wrong here).

---

## D-45 — Database errors: a friendly message, and the app stays alive

**Status:** Locked

Database failures are caught and surfaced as a readable message (*"Could not save — is the
database running?"*), and the app keeps running so the user can retry. It does not crash the
circuit.

> ⚠️ **The honest consequence, which must be handled and not hidden:** if a save fails, the
> picture on screen **no longer matches the database.** The user sees a figure that was never
> persisted. The message must therefore make that clear (and/or the failed figure must be
> removed from the view) — a silent "oh well" here would be worse than crashing, because the
> user would believe their work was saved.

**Rejected:** letting it crash (Blazor's default error bar). Zero code and honest for a
throwaway project — but an ugly, uninformative failure that effectively kills the page.

**Note:** this is the *only* general error-handling stance in the log. D-10's zero-row guard
remains a separate, specific mechanism — it is not an error at all, but expected staleness.

---

## D-46 — `type` is text + CHECK. No `created_at`.

> 🛑 **BOTH HALVES SUPERSEDED (v1.11).** The `CHECK (type IN (…))` whitelist becomes a foreign key
> into the `figure_types` table (**D-65**) — a new figure type is now an `INSERT`, not an
> `ALTER TABLE`. And `created_at` comes back (**D-68**), for one reason only: it is the single
> column whose late addition would produce false data.

**Status:** Locked

`type` is a **text** column constrained to `('line','rectangle','circle','triangle')`.

> **This is not a free choice — D-22's and D-41's CHECK constraints are written as
> `type <> 'circle'`. They assume text.** A PostgreSQL enum or an int-mapped C# enum would
> silently invalidate them as written.

**`created_at` is dropped.** D-39 made the sequential `id` the z-order, so nothing in the app
reads a timestamp. A column that exists for no functional reason is exactly what this log has
been ruthless about.

**Rejected:** a PostgreSQL enum type (stricter and more compact, but it would force rewriting
the CHECKs, add EF enum-mapping configuration, and buy essentially nothing at this scale).

---

## D-47 — Drag broadcast throttle: 50 ms, trailing edge guaranteed

**Status:** Locked — pins down D-11's vague "~30–50 ms"

Drag-glide broadcasts are **throttled to at most one every 50 ms** (≈20 updates/second).
Smooth to the eye, and conservative with SignalR traffic.

> **The trailing edge is guaranteed:** the **final position is always sent before the drop.**
> Without that guarantee the glide could stop up to 50 ms short of where the figure actually
> landed, and the other monitor would briefly show it in the wrong place until the drop
> broadcast corrected it — a visible twitch at the end of every drag.

This is a **throttle** (send at most once per interval), not a debounce (send only after
movement stops — which would show *nothing* until the drag ended, defeating the whole point).

---

## D-48 — Click vs drag: a 3-pixel threshold

**Status:** Locked

With the pointer tool armed:

- Move **less than 3 px** before releasing → it is a **click**. The figure is selected. **No
  database write.**
- Move **3 px or more** → it is a **drag**. The figure moves and is persisted on drop (D-09).

**Starting a drag also selects** the figure, and it **stays selected after the drop** — as
every editor does, which means you can drag something and then immediately delete it.

**Why the threshold exists:** without it, a 1-pixel hand-wobble while clicking counts as a
move — firing a useless database UPDATE and nudging the figure slightly. A click meant to
select would quietly displace things.

---

## D-49 — Structure: one app project, plus a small test project

**Status:** Locked

- **One Blazor Web App project** — pages, the notifier service, the EF model, all of it.
- **`docker-compose.yml`** at the repository root (D-27).
- **A small test project**, deliberately narrow in scope.

**What the tests cover — and why exactly these three:** they are the places where **a bug
would be silent rather than obvious.**

1. **The clamp maths** (D-36) — per-axis independence, inclusive bounds, the circle
   draw-clamp. A broken clamp lets figures drift off-canvas; you might not notice for weeks.
2. **The circle's inscribed-square round-trip** (D-22) — draw at centre `(cx,cy)` with radius
   `r`, store, reload, and confirm the centre and radius come back **exactly**. Also that
   translation preserves the radius.
3. **The line-normalisation landmine** (D-41) — that a diagonal line drawn up-and-right does
   **not** come back flipped into the opposite diagonal.

Everything else in this app fails *visibly* — you look at the canvas and see it. These three
fail quietly, which is precisely what tests are for.

**Rejected:** no tests at all. Consistent with MinVP, and everything is verifiable by using
the app — but it would leave the three silent failure modes above completely unguarded.

---

## D-50 — The minimum-size guard is PER-TYPE. (D-23's "one shared guard" is retracted.)

> 🛑 **SUPERSEDED BY D-60 (v1.11).** The guard stays per-type, but it is no longer a *mirror* of
> anything: the geometry CHECK constraints are gone, so there is no second copy in SQL to keep in
> agreement and no 32-case matrix test proving they agree. Validation lives in C# only, at one
> choke point. The lesson this entry teaches — *one shared rule either lets a zero-height
> rectangle through or rejects a legal horizontal line* — is why per-type validation survives.

**Status:** Locked — **fixes a real bug.** D-23 promised *"one shared minimum-size guard, not a
circle special case."* **That promise was impossible to keep.**

**The bug:** drag a rectangle horizontally — press (100,100), release (300,100). Start ≠ end,
so a shared "reject only zero-size draws" guard **lets it through.** After normalisation the
row is `(100,100,300,100)` — **height zero** — which violates the `box_is_a_box` CHECK. The
INSERT throws, and D-45/D-52 then reports *"Could not save — is the database running?"*: a
wrong and baffling message for a gesture the app should simply have declined.

**The legality boundary is genuinely per-type**, because a zero-height *line* is legal (that
is a horizontal line) while a zero-height *rectangle* is not.

**The guard — two rules, mirroring the CHECK constraints exactly:**

| Shape | Rejected when |
|-------|---------------|
| **Line** | both endpoints are identical (zero length). **Horizontal and vertical lines are legal and must work.** |
| **Rectangle / triangle / circle** | width **or** height is zero (for a circle: radius zero) |

Because these mirror D-41's constraints exactly, **the app can never write a row the database
would refuse.**

> **Rejected — a uniform "reject any zero-extent draw" rule.** It would have kept D-23's
> promise of a single shared guard, but it makes **horizontal and vertical lines impossible to
> draw** — drag straight across and *nothing appears*, silently. The line tool would work
> diagonally and look broken for the most obvious line there is. Keeping a tidy promise is not
> worth breaking a core tool.

**A rejected draw fails silently** — the figure simply does not appear. No message, no error.
This is what every drawing tool does: a stray click producing no shape is expected behaviour,
not something worth reporting. **Rejected:** showing a "too small to draw" hint — it would
treat a normal, frequent gesture as an error.

---

## D-51 — Identity, routes, and page protection

**Status:** Locked — closes the last structural hole (D-34 issued the cookie but stopped there)

### How the canvas knows who you are

At sign-in, the **numeric `user_id` is written directly into the cookie as a claim.** The
interactive circuit reads it straight out — **no database lookup on page load.**

**Accepted cost:** the id is duplicated (cookie and database), so deleting a user would leave
a cookie pointing at nothing. There is no user-deletion path in this app, so this cannot
arise; recorded anyway.

**Rejected:** carrying only the username and looking up `users.id` on every page load (the more
idiomatic Blazor `AuthenticationState` route). Costs a query per load and buys nothing here.

### Routes

| Route | Render mode | Purpose |
|-------|-------------|---------|
| `/login` | **Static SSR** (D-34 — an interactive component cannot set a cookie) | The username + password form |
| `/` | **InteractiveServer** | The canvas. Marked `[Authorize]`. |
| `POST /logout` | endpoint | Clears the cookie, redirects to `/login` (D-25) |

**An unauthenticated visitor to `/` is redirected to `/login`.**

---

## D-52 — Save-failure policy: retry transient, then roll back everywhere

**Status:** Locked — supersedes D-45's undecided *"and/or"*. **Specified by the user.**

### Retry — but only transient failures

On a failed save, **retry up to 2 additional times with short delays** — but **only if the
failure is transient** (e.g. the connection dropped, the database is briefly unreachable).

> **Never retry non-transient failures:** validation errors, CHECK-constraint violations, a
> missing or deleted figure, or an UPDATE that affected zero rows. Retrying these is
> pointless — they will fail identically every time.
>
> (A zero-row UPDATE is **not an error at all** — it is expected staleness, handled by D-10.)

### If all attempts fail

**The figure's original coordinates are retained for the entire drag**, precisely so this is
possible:

1. **Broadcast a rollback event** carrying the original coordinates to all other active tabs.
2. **Restore the figure to its original position** in the current tab.
3. **Show a modal:** *"The change could not be saved. The canvas will be reloaded from the
   database."*
4. **On OK, reload the canvas state from PostgreSQL.**

### Why this matters — the case that forced it

A failed **drop** is the hard one. The drag-glide broadcasts have **already gone out**
(D-11/D-47), so *every other tab already shows the figure in its new position* — while the
database still holds the old one. Simply showing a message and leaving the figure where it sits
would mean **every open screen is lying**, with only F5 to fix it. The rollback broadcast is
what makes all screens agree with the database again.

---

## D-53 — The broadcast message contract (canonical)

**Status:** Locked — this is the **single authoritative** definition. D-11, D-22 and D-40
described pieces of it inconsistently; this supersedes all of them.

**`sender`** is a **per-circuit GUID**, generated once when a tab's canvas component
initialises. It exists solely for the echo filter (D-11 core rule 3): a tab ignores any message
whose `sender` equals its own.

| Kind | Payload | Receiver's action |
|------|---------|-------------------|
| **`draw`** | `{ kind, sender, id, type, x1, y1, x2, y2 }` | **Insert or update** by `id`. The only kind that may create a figure. Sent *after* the INSERT, because `id` does not exist until then (D-39). |
| **`move`** | `{ kind, sender, id, x1, y1, x2, y2 }` | **UPDATE ONLY — never insert.** If the figure is unknown, **ignore the message entirely.** This is what kills the resurrection bug (D-40). |
| **`delete`** | `{ kind, sender, id }` | Remove the figure by `id`. Idempotent — deleting an unknown figure is a silent no-op. |
| **`rollback`** | `{ kind, sender, id, x1, y1, x2, y2 }` | Restore the figure to the coordinates given. Sent when a save fails after all retries (D-52). Applied as **update-only**, like `move`. |

**There is no separate `drop` kind.** A drag's final position is simply the last `move`
message — guaranteed to be sent by D-47's trailing edge — followed by silence. The DB write
happens on drop (D-09); the broadcast does not need to announce it.

**`move` carries no `type`** — a figure's type never changes, and the receiver already knows it.

> **Draw previews are NOT broadcast** (D-35). Only the finished figure is, via `draw`. Live
> glide (`move`) applies to *dragging an existing figure*, never to *drawing a new one*.

---

## D-54 — Mid-drag, a tab ignores ALL incoming broadcasts

**Status:** Locked

While a local drag is in progress, the tab **discards every incoming broadcast** —
`if (_dragging) return;` — not merely those about the figure being dragged.

**Why this is safe:** the one-mouse premise (D-11). While you are dragging, nothing else is
happening, because you are the only user and you have one pointer.

> ⚠️ **Accepted cost, stated honestly:** this **breaks the free multi-device degradation** that
> D-11 advertises. If you also had the app open on a phone and something were drawn or deleted
> there *during* one of your drags, that message would be **lost permanently** — there is no
> periodic re-read to recover it, so the tab stays wrong until F5.
>
> This is accepted because the multi-device case was never a requirement — D-11 explicitly
> records it as an undesigned freebie, and nothing is built for it.

**Rejected:** ignoring only messages about the figure currently being dragged (one extra
condition; loses nothing, ever). Rejected in favour of the simpler rule.

---

## D-55 — Page background: light grey

**Status:** Locked — closes a gap that would have made the app look broken

The page background is **light grey**. The canvas (D-38, white) sits on it.

**Why this is not cosmetic:** D-43 deliberately gives the canvas **no border** (a CSS border
shifts the SVG interior and would corrupt the coordinate maths). So the *only* thing that makes
the canvas boundary visible is **contrast with the page behind it.** The browser's default page
background is **white** — leaving it unspecified would make the canvas edge **invisible**, and
"figures stop at the edge" (D-24) would look like an inexplicable bug.

---

## D-56 — Logout sits right-aligned in the toolbar strip

**Status:** Locked

The Logout control lives in the **same 48px toolbar strip (D-43), right-aligned**, visually
separated from the seven tool/action buttons (D-33, D-73).

- The **seven-button toolbar rule stays intact** — logout is an *account action*, not a drawing tool,
  and it reads that way.
- It is a small **HTML form posting to `POST /logout`** (D-51), not an interactive button —
  because clearing a cookie requires an HTTP round-trip (D-34).

**The toolbar height stays 48px, so D-43's coordinate constant (`canvasY = PageY − 48`) is
unchanged.**

**Rejected:** a separate header row above the toolbar. Cleaner separation of concerns, but it
would add a second fixed-height row — **changing the one constant every coordinate in the app
depends on.** Not worth it.

---

## D-57 — Leaving the surface mid-DRAW commits the figure

**Status:** Locked — extends D-37 (which covered dragging) to drawing.
⚠️ **v1.1: motivation corrected** (abandoning a draw is simply out of MVP scope, not "impossible
because of no JS").

Releasing the mouse outside the window, or leaving the drag surface, **commits the
in-progress figure** at its clamped preview position — exactly the rule D-37 applies to
dragging. **One consistent rule for both gestures.**

> **Consequence: there is no way to abandon a draw once started.** Abandoning a draw is simply
> **not an MVP feature** — if you start a shape and change your mind, you **draw it and then
> delete it.**
>
> ⚠️ **v1.1:** the older text said an Escape-to-cancel was *impossible because it needs a
> document-level key listener (JavaScript)*. That rule is now lifted, so **Escape-to-cancel
> could be added later** as its own decision. It is still out of scope for now.

**Rejected:** cancelling the draw on leaving the surface. It would have given a genuine escape
hatch (start a shape, sweep out of the canvas, it vanishes) — but the two gestures would then
behave *differently*: a drag commits on leave, a draw cancels. One rule beats two.

---

## D-58 — The remaining constants

**Status:** Locked — ⚠️ **amended in v1.1** (selection style + canvas size; see D-31, D-19)

### Visual

| Constant | Value |
|----------|-------|
| Figure outline | **black, 2px** |
| Figure fill | **white** (D-38) |
| **Selected** figure indicator | **~1px blue + white dashed trace on the figure's own outline**, drawn topmost, `pointer-events: none` (D-31) — *(v1.0 was: red, 2px outline)* |
| Page background | **light grey** (D-55) |
| Canvas | **white, 1472 × 828, no border** (D-19, D-38, D-43) — *(v1.0 was: 1280 × 720)* |
| Toolbar | **48px tall** (D-43) |

A **2px** stroke (not 1px) is deliberate for *figure* outlines: D-32 declined the invisible
widened hit-area for lines, so the stroke itself is the only click target a line has. 2px makes
that survivable. (The **selection trace** is thinner, ~1px, on purpose — on a rectangle it would
otherwise sit exactly on top of the figure's own 2px outline; see D-31.)

### Behaviour

- **The Delete button is greyed out and unclickable when nothing is selected.** Self-evident
  at a glance. (*Rejected:* an always-clickable button that silently does nothing — a control
  that sometimes does nothing with no explanation feels broken.)
- **Passwords must be non-empty.** A formality — it is plaintext and protects nothing (D-08) —
  but it stops the user creating an account whose blank password they then can't recall.
  Usernames are already trimmed and non-empty (D-44).

### Docker Compose (D-27)

Postgres **17**, host port **5433** → container port **5432**, database **`canvas`**,
user/password **`postgres`/`postgres`**, with a **named volume** so figures survive a
container restart. Connection string in `appsettings.Development.json`.

> **Amended in Phase BC-01 (2026-07-15), user-approved.** As originally locked, this entry
> specified host port **5432**. On the development machine a native `postgresql-x64-18`
> Windows service permanently occupies 5432, so the compose file publishes **`"5433:5432"`**
> instead: the container still listens on 5432 internally, only the host-published port moved.
> The user explicitly chose to move the container's port rather than disturb the pre-existing
> native service. This is a deviation of *fact*, not of intent — D-27 decides that Postgres
> runs in Docker Compose with a named volume, and that decision is untouched.
>
> **The port is load-bearing in exactly one way:** `dotnet ef` tooling that guesses a
> connection string would silently target the *wrong* PostgreSQL server on this machine.
> That hazard is closed structurally — `CanvasDbContextFactory` throws instead of guessing
> (BC-01 gap closure, CR-03).

*(Rejected: no volume — a fully disposable database. Cleaner for testing migrations from
scratch, but every `docker compose down` would erase your drawings.)*

---

# THE SCHEMA — 🛑 DEAD AS OF v1.11. DO NOT IMPLEMENT FROM THIS.

> **This DDL described the v1.0/v1.1 schema and is no longer the schema.** It is kept because the
> migration in v1.11 reads *from* it. **The current schema is D-59…D-69** (end of this file) and
> [`DATA-MODEL-v1.11-DRAFT.md`](DATA-MODEL-v1.11-DRAFT.md).
>
> What changed: four coordinate columns → `x, y, rotation` + `geometry jsonb` · `integer` →
> `numeric` · `id` as z-order → `uuid` + a `z` column · two tables → four · geometry CHECK
> constraints → validation in C#.

*(Historical.) A zero-context implementer should not have to reconstruct this from six separate
entries.*

```sql
CREATE TABLE users (
    id       integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    username text NOT NULL UNIQUE,   -- stored LOWERCASED (D-44), trimmed, never empty
    password text NOT NULL           -- PLAINTEXT. Throwaway project only. (D-08)
);

CREATE TABLE figures (
    id      integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY,  -- also the z-order (D-39)
    user_id integer NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    type    text    NOT NULL CHECK (type IN ('line','rectangle','circle','triangle')),
    x1 integer NOT NULL,
    y1 integer NOT NULL,
    x2 integer NOT NULL,
    y2 integer NOT NULL,

    -- circle: stored as the square it is inscribed in (D-22)
    CONSTRAINT circle_is_a_circle CHECK (
        type <> 'circle' OR (x2 - x1 = y2 - y1 AND x2 > x1 AND (x2 - x1) % 2 = 0)),

    -- rectangle / triangle: a real box, normalised, no zero width or height (D-23, D-41)
    CONSTRAINT box_is_a_box CHECK (
        type NOT IN ('rectangle','triangle') OR (x2 > x1 AND y2 > y1)),

    -- line: normalised left-to-right; may run either way vertically; never zero-length
    CONSTRAINT line_is_a_line CHECK (
        type <> 'line' OR (x2 >= x1 AND (x2 > x1 OR y2 <> y1)))
);

CREATE INDEX ix_figures_user_id ON figures(user_id);   -- every page load filters on this

COMMENT ON TABLE figures IS
  'x1,y1,x2,y2 are ALWAYS the figure''s bounding box. A CIRCLE is stored as the square it is '
  'inscribed in: r = (x2-x1)/2, cx = x1+r, cy = y1+r. It is DRAWN centre-out (press centre, '
  'drag for radius) but STORED as a square — interaction and storage are different things. '
  'A LINE is the segment between the two points and may run diagonally in either vertical '
  'direction; it is normalised by swapping the whole point pair, never by sorting axes.';
```

**No `canvases` table** — "the canvas" is just the set of figures belonging to a user (D-12).

**Load query:** `SELECT * FROM figures WHERE user_id = @id ORDER BY id` — the `ORDER BY` is
what reconstructs z-order after a refresh (D-39).

---

## D-36 — The clamp: exact formula, and bounds are inclusive

**Status:** Locked — this is the operative specification for D-24 and D-29.
⚠️ **v1.1: canvas is now `W = 1472`, `H = 828`** (was 1280 × 720; see D-19). The formula is
unchanged — only the two constants moved.

Canvas is `W = 1472`, `H = 828` (D-19). **Bounds are INCLUSIVE: the valid domain is
`0..1472 × 0..828`.** SVG coordinates are geometric edge positions on a continuum, not pixel
cells — so a rectangle with `x2 = 1472` has its right edge exactly *on* the boundary, which
is precisely the "stopped at the edge" state D-24 describes. (An exclusive `0..1471` domain
would mean a figure could never actually touch the right or bottom edge.)

### The move clamp

Given a figure's bounding box — which, thanks to D-22, is **just the min/max of its four raw
columns, for every shape**:

```
bx1 = min(x1,x2)   by1 = min(y1,y2)
bx2 = max(x1,x2)   by2 = max(y1,y2)
```

and a raw mouse delta `(dx, dy)`, the permitted delta is:

```
dx' = clamp(dx, −bx1, W − bx2)        where clamp(v, lo, hi) = min(max(v, lo), hi)
dy' = clamp(dy, −by1, H − by2)

then translate uniformly:   x1 += dx'   y1 += dy'   x2 += dx'   y2 += dy'
```

> **The per-axis independence is the point.** `dx'` never reads `y`; `dy'` never reads `x`.
> So a figure pinned against the right edge (`dx' = 0`) can **still slide up and down**. A
> naive clamp that rejected the *whole* delta when either axis violated would make the figure
> stick and feel broken. It **slides along the wall** — this is what users expect (OS windows
> against screen edges, scrollbar thumbs, sliders).

The permitted interval always contains 0, so a figure can always stay put. A figure exactly
canvas-sized forces `dx' = dy' = 0` with no special case. A figure larger than the canvas
cannot exist (D-29 clamps creation).

### Ordering: clamp → render → broadcast

**Never broadcast the raw delta.** The clamp runs in the pointer-move handler *before* both
the local re-render and the D-11 notifier publish, so the other monitor sees the figure
pinned at the edge exactly as the acting tab does, and the value persisted on drop (D-09)
equals the last broadcast value. Add this to D-11's irreducible-core list.

### The one genuinely type-specific rule: the circle draw-clamp (D-29)

For line / rectangle / triangle, drawing clamps the moving corner per-axis — generic.

**A circle is different, and this is inherent, not a regression.** Its centre is fixed at the
press point (D-13) and growing `r` pushes all four extremes outward at once. So:

```
r = min( round(distance), cx, cy, W − cx, H − cy )
```

> ⚠️ **The consequence is a real UX surprise, and it must be known in advance: pressing near
> an edge forces a tiny circle.** Press at `(10, 360)` and drag 200 px to the right → **the
> radius caps at 10**, because the *left* rim would otherwise exit the canvas. This follows
> unavoidably from D-13 (centre = press point) × D-29 (nothing created out of bounds). It
> would exist under *any* circle encoding. It has simply never been written down until now.

**Rejected:** database CHECK constraints for canvas bounds. D-24 and D-29 already guarantee
no figure can be created or moved out of bounds, so bounds CHECKs would be belt-and-braces on
a rule the app never breaks.

> **Correction to D-23:** D-23 said not to add bounds CHECKs *because "a figure legitimately
> overhanging the edge would violate them."* **That reason is now stale** — D-24 and D-29
> subsequently established that figures live entirely inside the canvas, always. No legal row
> can be out of bounds. The decision not to add them stands; only the stated reason changes.

---

## D-37 — Drag termination

**Status:** Locked — closes the audit's second blocker.
⚠️ **v1.1: motivation corrected** (this exists to *prevent unexpected behaviour*, not to avoid JS).

**The problem — a drag that never ends:** if pointer events stop when the cursor exits the drag
surface (and a mouse-release outside the browser window is never delivered at all), the drag
**hangs forever**: the D-09 write never fires, and every other tab shows the figure stranded at
a position that was never persisted. Preventing that stranded/hanging state is the whole point of
this decision.

**Resolution — two markup-only rules:**

1. **`pointerleave` on the drag surface commits the drag** at its current clamped position.
2. **The `Buttons` guard:** on any `pointermove` while dragging, if `PointerEventArgs.Buttons`
   shows the primary button is already **up**, commit and end the drag. This catches the
   Alt-Tab case — the button released while the window lost focus, with the cursor never
   having left the surface.

### Why this is coherent rather than a hack — and it is D-36 that makes it so

To leave the drag surface, the cursor must cross the canvas boundary. **By then the figure is
already pinned at the edge**, by continuity of the clamp (D-36). So "pointer left → commit"
drops the figure **exactly where the user can already see it.** Nothing jumps. Nothing is
lost. The commit position is always the visually correct one.

> This is why the JavaScript shim was not needed: the edge-clamp turned an ugly failure mode
> into a natural behaviour.

### Refinement, free and recommended

Put the `pointermove` / `pointerup` handlers on a **page-spanning wrapper element**, not on
the SVG itself. D-18's `PageX`/`PageY` arithmetic is deliberately **target-independent**, so
it works correctly anywhere on the page. The drag then survives the cursor wandering *off the
canvas but still inside the window* — only leaving the browser window commits early. Strictly
better, still zero JavaScript.

**Use `pointerleave`, not `pointerout`** — `pointerout` also fires when the cursor moves onto
a child figure, which would end drags constantly.

**Accepted cost:** overshooting the drag surface commits the drag early. The figure lands
where you see it, but you must re-grab to continue. With the page-wide wrapper this is rare.

**Not pursued:** `setPointerCapture` (which keeps the figure grabbed as the cursor wanders
anywhere, exactly like a professional editor). The clamp already handles termination gracefully,
so the current markup-only approach is enough for the MVP.

> ⚠️ **v1.1:** the older text also rejected `setPointerCapture` because *it is JavaScript* and
> would have been the project's only JS. That rule is now lifted, so a `setPointerCapture`-based
> "keep grabbed anywhere" upgrade **could be added later** as its own decision.

---

## D-35 — Live preview while drawing

**Status:** Locked

The shape is **visible under the cursor as you drag it out** — the rectangle stretches, the
circle grows. This is what every drawing tool does, and it is what makes drawing feel like
drawing.

The preview is **local only.** It is *not* broadcast to other tabs — only the finished figure
is. (D-11's live glide covers *dragging an existing figure*, not *drawing a new one*.)

**Accepted cost:** the draw handler maintains transient in-progress geometry and re-renders
on every pointer-move — structurally more code than not having a preview.

**Rejected:** no preview (the figure appears only on release). Genuinely less code, but
drawing anything to a deliberate size would be pure guesswork every single time.

---

# Open questions

**Under active discussion (raised by the audit and by the user):**

| ID | Question | Status |
|----|----------|--------|
| Q-17 | Should D-22 be reversed? | ✅ **Closed — YES.** D-22 rewritten: circle = its inscribed square. |
| Q-18 | Exact clamp formula and inclusive bounds. | ✅ **Closed — D-36.** Bounds inclusive (0..1472 × 0..828 *(v1.1; was 0..1280 × 0..720)*). |
| Q-19 | How does a drag terminate without `setPointerCapture`? | ✅ **Closed — D-37.** The edge-clamp made `pointerleave`-commit coherent. *(v1.1: no longer a no-JS constraint; `setPointerCapture` could be added later.)* |
| Q-20 | Filled or hollow figures? | ✅ **Closed — D-38.** White fill, black outline. |
| Q-23 | Render order after F5. | ✅ **Closed — D-39.** Sequential integer id; `ORDER BY id`. |
| Q-24 | The resurrection hole. | ✅ **Closed — D-40.** Both fixes: move is update-only, *and* a 0-row UPDATE broadcasts a delete. |
| Q-26 | Corner normalisation. | ✅ **Closed — D-41.** On write — but **not the same way for lines** (see the landmine). |
| Q-21 | Remaining DDL. | ✅ **Closed — D-46** + the canonical schema section above. |
| Q-22 | Who creates the schema. | ✅ **Closed — D-42.** EF Core migrations, applied on startup. |
| Q-25 | Toolbar height. | ✅ **Closed — D-43.** 48px, no canvas border. |
| Q-27 | Throttle value. | ✅ **Closed — D-47.** 50 ms, trailing edge guaranteed. |
| Q-28 | Click-vs-drag threshold. | ✅ **Closed — D-48.** 3 px; a drag also selects. |
| Q-29 | Login form semantics. | ✅ **Closed — D-44.** Case-insensitive, trimmed, non-empty, UNIQUE. |
| Q-30 | Error handling. | ✅ **Closed — D-45.** Friendly message, app stays alive. |
| Q-31 | Project structure and tests. | ✅ **Closed — D-49.** One app project + a narrow test project. |

**All audit findings are now closed.**

---

## ~~Closed~~ (pre-audit)

| ID | Question | Resolution |
|----|----------|------------|
| Q-01 | Multi-tab behaviour | **D-11** — live sync, real-time drag glide |
| Q-02 | Save strategy | **D-09** — per-operation, no Save button |
| Q-03 | Schema shape | **D-12** — two tables, no canvas table |
| Q-04 | Circle geometry | **D-13** — centre + radius |
| Q-05 | Styling | **D-14** — one fixed style, no colours |
| Q-06 | Delete interaction | **D-15 → superseded by D-33** — click to select, then click the **Delete button** (the Delete *key* is out) |
| Q-07 | Geometry columns | **D-22 (REVISED)** — four coordinates always; circle = **its inscribed square**. *(The original "centre + rim point" answer was reversed — see D-22.)* |
| Q-08 | Shape selection | **D-16 → superseded by D-30 + D-33** — a **six**-button toolbar |
| Q-09 | Login flow | **D-17** — one form, unknown username creates the account |
| Q-10 | Canvas sizing | **D-18 / D-19** — fixed 1472×828 at 1:1 *(v1.1; was 1280×720)* |
| Q-11 | Triangle geometry | **D-21** — derived from a drag, apex top-centre |
| Q-12 | Canvas dimensions | **D-19** — 1472×828 *(v1.1; was 1280×720)* |
| Q-13 | Figures off the canvas edge | **D-24** — they stop at the edge |
| Q-14 | Drawing off the canvas edge | **D-29** — also stops at the edge |
| Q-15 | How does the app tell "select a figure" from "draw a new one"? | **D-30** — a **pointer tool** in the toolbar |
| Q-16 | What does a selected figure look like; what's armed on load; does clicking blank canvas deselect? | **D-31** |

---

# Summary — what is being built

A Blazor Server web app (.NET 10) where a user draws simple geometric figures on a fixed
1472×828 canvas rendered as SVG *(v1.1; was 1280×720)*. Each user has exactly one canvas.
Figures can be **drawn, dragged and deleted** — nothing else.

**The stack:** Blazor Server (InteractiveServer) · SVG · PostgreSQL via EF Core/Npgsql ·
Docker Compose for local Postgres. *(v1.1: the earlier "no hand-authored JavaScript" rule has
been removed — see the v1.1 notes at D-06/D-18/D-33/D-37/D-57.)*

**The database:** two tables. `users` (username + **plaintext** password — throwaway project
only). `figures` (four integer coordinates, always non-null, plus a type).

**The interesting parts:**

1. **Everything saves immediately** — no Save button. Draw inserts a row, drop updates it,
   delete removes it.
2. **Tabs sync live.** Drag a figure on one monitor and it *glides in real time* on the
   other. This costs almost nothing because Blazor Server already routes every mouse-move
   through the server — the intermediate positions are simply re-broadcast in memory, and
   never written to the database.
3. **Conflict handling doesn't exist, on purpose.** One human has one mouse, so two tabs
   physically cannot be edited at once. That single observation deleted locking, concurrency
   tokens, merge prompts, and retry loops from the design.
4. ~~**Every figure is four integers**~~ — **true through v1.1, false from v1.11.** See D-59.
   Position and shape are now separate: `x, y, rotation` locate the figure, `geometry jsonb`
   describes its shape in local coordinates. Moving is still one blind operation for every
   shape — it just touches `x, y` instead of four coordinates, which is why it now works for a
   1000-vertex polygon too.

**What was deliberately NOT in scope through v1.1:** resize, rotate, undo/redo, z-order,
colours, multi-select, copy/paste, zoom, pan, export, real authentication, password hashing.
**v1.11 does not implement any of these** — it removes the *storage* obstacle to them. See D-69
for what stays deferred and what each will cost.

---

# v1.11 — THE STORAGE MODEL WAS REPLACED (D-59…D-69)

*Decided 2026-07-21. Full reasoning, DDL, migration plan and accepted costs:*
*[`DATA-MODEL-v1.11-DRAFT.md`](DATA-MODEL-v1.11-DRAFT.md). These entries are the normative summary.*

**Why at all:** every new capability the user wants — dragging a vertex, a triangle pointing
down, a 1000-vertex polygon, rotation, layers, undo, per-figure colour — required a *schema*
change under the old model. The goal of v1.11 is to be **the last migration that touches data
already written.**

---

## D-59 — Position and shape are stored separately

**Status:** Locked — supersedes **D-22** entirely

```
x, y, rotation   → WHERE the figure sits      (plain columns)
geometry jsonb   → WHAT shape it is, in LOCAL coordinates from (0,0)
```

**Why this is the whole design.** Moving becomes `update figures set x = x + 20` **at any shape
complexity** — `geometry` is neither read nor written. The type-blind move that D-22 was built to
protect survives, and now covers shapes D-22 could not express at all. This is the scene-graph
model: SVG itself works this way (`<g transform="translate(…)">`).

**Rejected:** keeping the bounding box as the primary geometry (D-22). It caps a figure at four
degrees of freedom — a free triangle needs six, a 1000-gon needs two thousand.

**Rejected:** a table per figure type. Identical columns in every table, `id` no longer unique
across the canvas, z-order spread across independent identity sequences, and a 13-way `UNION` on
every page load. Adding a figure would need a migration; today it needs none.

**Rejected:** storing rendered SVG. Dragging becomes string surgery, the database can validate
nothing, the bounding box needed by the clamp is no longer computable, and stored SVG rendered as
markup is a stored-XSS vector in an app that broadcasts figures to other tabs.

---

## D-60 — Shape lives in `geometry jsonb`; formats are per type

**Status:** Locked — supersedes **D-21**, **D-41**, **D-50**

```json
line          {"points": [[0,0],[100,40]]}
rectangle     {"w": 200, "h": 100}
circle        {"r": 50}
triangle      {"points": [[50,0],[0,80],[100,80]]}
```

**The rule for choosing a format:** *store the shape in the most general representation that
**this same type** will ever need.*

- **Triangle → three points.** Vertex dragging is planned. Storing `{"w","h"}` and deriving the
  vertices would force a conversion of every existing triangle the day that ships.
- **Line → two points.** This **defuses landmine #2**: the line was the only figure whose columns
  were not a bounding box, which is why it needed its own normalisation arm (D-41) whose failure
  silently produced the opposite diagonal. That arm no longer exists.
- **Circle → `{"r"}`, not `{"rx","ry"}`.** A circle never becomes an ellipse — the ellipse is a
  *separate type with its own button*. A circle that cannot be stretched is not a missing feature,
  it is the definition of a circle (and is already how the app behaves — D-13).

**Accepted cost — the database no longer validates geometry.** `circle_is_a_circle`,
`box_is_a_box` and `line_is_a_line` are gone; `{"r": -5}` will be accepted. All geometric
correctness moves into C#, at **one** choke point. D-50's C#/SQL mirror disappears with them —
there is nothing left to mirror.

**Accepted cost — jsonb removes *schema* migrations, not *format* migrations.** Changing the
stored format of an existing type still means rewriting every row of it. Hence two standing
rules: a type's format is settled before its first write, and reads are defensive (a missing key
takes a default, never throws).

---

## D-61 — Coordinates are `numeric`, not integers

**Status:** Locked — supersedes **D-20**

**Why the reversal.** D-20 chose integers so CHECK constraints could compare for exact equality —
that was the entire justification, and D-60 removed those constraints. Integers meanwhile block
zoom above 100% and make rotation impossible to express without drift.

**Why `numeric` and not `double precision`.** `numeric` is exact decimal arithmetic in PostgreSQL,
so accumulated translations never drift. `double` was considered and rejected: float equality is
unreliable, and repeated drags would accumulate error.

*(`bbox_*` is `double precision` — it is a coarse cache, see D-67.)*

---

## D-62 — Figure ids are `uuid`

**Status:** Locked — amends **D-39**

**Why.** With undo planned, a deleted-then-restored figure must come back as **the same figure**.
Under `integer GENERATED ALWAYS AS IDENTITY` it returns with a new id, so its layer position and
any references to it (future `parent_id`) are lost. A uuid restores exactly.

Secondary: the id exists before the INSERT, so D-39's rule that a `draw` broadcast cannot be
optimistic is no longer forced by the schema.

**Accepted cost:** this is the only change that rewrites the identity of existing rows rather than
adding to them. The mapping is computed once, deterministically, during the migration.

---

## D-63 — Draw order is the `z` column, unique per canvas

**Status:** Locked — supersedes **D-39**

`z numeric NOT NULL`, `UNIQUE (canvas_id, z)`. Load is `ORDER BY z`.

**Why `id` could not stay the z-order.** "Bring to front" would require rewriting a primary key.
And after D-62 the id is a uuid — random, so it carries no order at all.

**Why `numeric`.** Reordering inserts *between* neighbours: `1.5`, then `1.25`. `numeric` is exact
and arbitrary-precision, so subdivision never runs out. `integer` would force renumbering every
row below on each move; `double` would exhaust precision after roughly fifty subdivisions and
silently produce two figures on one layer.

**Why unique.** Equal `z` would not blend colours — SVG paints in document order, one figure
simply covers the other — but **which** one ends up on top would be unpredictable.

**Consequence that must be implemented:** a new figure takes `z = max(z) + 1`. Two tabs drawing at
the same instant compute the same value and the second INSERT fails on the unique constraint.
**A retry with recomputation is required** — without it a figure silently fails to appear.

---

## D-64 — Four tables. `canvases` now exists.

**Status:** Locked — supersedes **D-12**

`users` · `canvases` · `figures` · `figure_types`.

D-12 rejected a `canvases` table as "a row per user holding no meaningful data". It now holds
`width`, `height`, `background` and `name` — the canvas size stops being a compile-time constant
(`CanvasBounds`, changed by hand in v1.1) and becomes data.

**v1.11 ships no UI for creating canvases.** Exactly one canvas is created per existing user. The
table exists so that the feature, when it comes, is not another pass over every row of `figures`.

---

## D-65 — Valid figure types are rows, not a CHECK constraint

**Status:** Locked — supersedes the CHECK half of **D-46**; lifts D-05's "there are only ever four"

`figures.type` is a foreign key into `figure_types(name)`. Adding a figure type is an `INSERT`
(seeded at application start), not an `ALTER TABLE`.

**Why it matters:** combined with D-60, adding the fiftieth figure type costs *one C# class, one
seeded row, one toolbar button* — and no schema change, no migration, no touching stored data.
That is the whole point of v1.11.

**Rejected:** a bare `type text` validated only in C#. The lookup table costs almost nothing and
makes a typo (`'rectangel'`) fail at write time instead of surfacing later as a figure that will
not render.

---

## D-66 — Style is per-figure, in a validated `style jsonb`

**Status:** Locked — supersedes **D-14**

**Never store what the client sent.** Incoming JSON is parsed into a typed C# record, validated,
and **re-serialised from the record**; unknown keys never reach the database.

- colours: whitelist `^#[0-9A-Fa-f]{6}$`
- `stroke_width`: clamped to **0.5 – 64** (below 0.5 is invisible; 0 is not painted at all in SVG)
- `opacity`: clamped to 0 – 1

**Why a whitelist and not just escaping.** Style values end up in SVG attributes. Blazor escapes
attribute values, so the ordinary path is safe — but one `MarkupString` or one hand-built
`style="…"` string removes that protection permanently. Where only hex digits are legal, injection
cannot exist. **The same gate is mandatory for `geometry`**, which is more dangerous still because
its numbers feed the clamp.

**Why jsonb and not columns** — the general rule, applied twice in this milestone:
*flat and universal → columns; per-type or structured → jsonb.* Style is jsonb because gradients,
shadows and text formatting are nested structures that would otherwise become sparse columns.

**v1.11 ships no styling UI.** Every migrated figure gets today's fixed style, so nothing changes
on screen.

---

## D-67 — `bbox_*` is a cache, computed by the app, excluding the stroke

**Status:** Locked

Four `double precision` columns holding the figure's axis-aligned extent. Not the source of
truth — a pure function of `geometry`, recomputed on every write.

**Why it exists:** the edge clamp (D-24/D-36) needs a bounding box, and with shape in jsonb it is
no longer free. Without it, clamping a 1000-point path would mean walking every point on every
drag frame. It is also what makes viewport culling possible later, which is the only path to
100 000+ figures.

**Why the app computes it and not a `GENERATED ALWAYS AS … STORED` column.** A generated column
needs an IMMUTABLE SQL function that parses every shape format — i.e. all of the shape logic
duplicated in PL/pgSQL. Then adding a figure type would require changing a database function,
which is a migration, which breaks D-65's guarantee. And this project has already paid for one
"same rule in two places, must agree" arrangement (D-50, proven by a 32-case matrix test).

**Why the staleness risk is acceptable:** the clamp reads `bbox_*` on every drag, so a wrong value
shows up immediately and visibly — the figure stops at the wrong edge. This is not a silent
failure. A test that recomputes every row's bbox from its `geometry` and compares backs it up.

**The stroke is excluded.** An SVG stroke is centred on the outline and extends `W/2` beyond the
shape. Including it would tie the geometry cache to the style — changing stroke width would
require recomputing the bbox. The clamp and the selection trace add `W/2` at render time instead.
This is also why D-66 caps stroke width at 64: the gap between stored extent and visible extent
stays under 32px per side.

---

## D-68 — `created_at` returns

**Status:** Locked — supersedes the "no `created_at`" half of **D-46**

> ✏️ **Amended (Phase 10 planning).** Originally titled "`created_at` returns; `updated_at` joins
> it". The body below only ever argued for `created_at`; `updated_at` appeared in the heading, was
> never given a rationale, and no reader for it existed in v1.11 or anywhere in `src/`. Keeping it
> would have contradicted both D-46's own rule — *"a column that exists for no functional reason is
> exactly what this log has been ruthless about"* — and D-69's *"take now only what would otherwise
> require a pass over rows already written"*. `updated_at` is deferred to **D-69**; `created_at`
> stands on the argument below, which is specific to it.

D-46 dropped `created_at` because nothing read it. It comes back for one reason only: **it is the
only column in this design whose late addition would produce false data.** Added in a year, every
existing figure would claim to have been created that day. `version`, `parent_id` and the rest all
backfill honestly, so they are deferred (D-69).

**Known inaccuracy, accepted:** figures migrated from v1.1 get the migration timestamp. The old
schema did not record the real one.

---

## D-69 — What v1.11 deliberately does NOT add

**Status:** Locked

The rule: **take now only what would otherwise require a pass over rows already written.**
Everything below is instant later — `ADD COLUMN … DEFAULT` does not rewrite a table in
PostgreSQL 11+, and `CREATE TABLE`/`CREATE INDEX` touch no data.

| Deferred | Needed for | Cost when added |
|---|---|---|
| `version` | concurrent editing | `ADD COLUMN DEFAULT 1` — the backfilled `1` is correct |
| `parent_id` | groups and layers | `ADD COLUMN` — existing rows are simply NULL |
| `updated_at` | audit trails, "recently edited" views | `ADD COLUMN … DEFAULT now()` — instant in PG 11+. Deferred from D-68, which named no reader for it. Undo/history does **not** motivate it: that needs `figure_history` regardless (below) |
| GiST index on the bbox | 100 000+ figures | `CREATE INDEX` |
| `figure_history` / operation log | undo/redo across F5 | `CREATE TABLE` — starts empty |
| Soft delete (`deleted_at`) | — | **Not taken.** It only undoes *deletion* — it cannot undo a move or a vertex edit, so undo needs a history table regardless. Meanwhile it adds `WHERE deleted_at IS NULL` to every query forever and introduces an "exists but hidden" state into the D-40 no-resurrection rule. Hard delete stays. |

**Not solved by any schema:** past roughly ten thousand figures the bottleneck is Blazor Server and
the SVG DOM, not the database — every figure is a DOM element that is diffed and sent over
SignalR. D-67's bbox columns make viewport culling *possible*, but a million figures would also
require abandoning SVG elements for canvas/WebGL. The schema does not obstruct that; it does not
achieve it either.

---

## What v1.11 does NOT change

Stated because it is easy to assume otherwise:

- **D-09, D-10** — one write per operation, one UPDATE per drag on drop. Unchanged.
- **D-40, D-47, D-54** — no-resurrection, the 50 ms throttle, mid-drag isolation. The *rules* are
  unchanged and delete stays hard.
- **D-53 — the message contract's RULES hold, but its PAYLOAD changes.** It has to: the fields
  `x1, y1, x2, y2` no longer exist on a figure. `SyncMessage.Id` becomes a `Guid` (D-62), and:
  - `move` carries **`(id, x, y)`** — two numbers instead of four, because a move is now a
    translation of the origin and nothing else. This is strictly less traffic per drag frame.
  - `draw` carries `(id, type, x, y, rotation, geometry, style, z)` — everything the receiving tab
    needs to render a figure it has never seen.
  - `delete` and `rollback` are unchanged apart from the id type.

  Every receiver rule survives verbatim: `move` and `rollback` remain **update-only**, an unknown
  id is still ignored and never inserted, and the sender still filters its own echo.
- **D-35, D-39 (broadcast timing)** — drawing is still visible to other tabs only as a finished
  figure on release, not live as it grows. `uuid` makes an optimistic broadcast *possible*; it was
  explicitly not adopted.
- **D-24, D-36** — figures still stop at the edge, same formula. The clamp now reads `bbox_*`.
- **D-13, D-19, D-31** — circle drawn centre-out, canvas 1472×828, selection behaviour.
- **The user sees no difference on day one.** Same figures, same positions, same style.

---

## D-70 — Five-pointed star geometry

**Status:** Locked

`star5` is the fifth figure type. It is a five-point star, stretchable to fill the dragged box
rather than aspect-locked, and drawn corner-to-corner like rectangle and triangle.

The first outer vertex is top-centre and the sweep starts at `-pi/2`, so the star points up.
The inner radius ratio is fixed at **0.382** (`1/phi^2`).

---

## D-71 — Star storage format

**Status:** Locked

The star geometry is stored as:

```json
{"points": [[x,y] x 10], "innerRatio": 0.382}
```

The ten ordered points are authoritative for rendering and `bbox_*`. `innerRatio` is descriptive
but required by parsing, so a missing-ratio payload is rejected rather than rendered partially.

---

## D-72 — Registry-owned catalog exposure

**Status:** Locked

`star5` participates in the default shape registry immediately after `triangle`. The
`figure_types` catalog is seeded from the registry on every startup, including an existing database
already in `CatalogState.Completed`, using idempotent inserts so repeated starts leave one row.

This amends D-65's "seeded at application start" rule from migration/fresh-install only to every
startup for registry-owned figure types.

---

## D-73 — Seven-button toolbar with Star

**Status:** Locked — amends D-16, D-30, D-33, D-56 and D-58

The current toolbar is:

```
[ pointer ] [ line ] [ rectangle ] [ circle ] [ triangle ] [ star ] [ delete ]
```

Star is an armable drawing tool between triangle and delete. Delete remains an action button, not
an armable drawing mode. Logout remains right-aligned in the same 48px strip, outside the seven
tool/action buttons, and remains a real HTML form posting to `POST /logout` with the existing
antiforgery semantics.

---

*Log complete. All decisions were made by the user; nothing here was decided by default.*
