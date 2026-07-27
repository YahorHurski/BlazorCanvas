# Decisions (LOCKED)

Synthesized from a single ADR set: `docs/DECISIONS.md` (58 decisions, D-01…D-58; **59 with v1.11's
D-59**). Classification: ADR / locked: true / precedence 0 (manifest-declared).

**Every decision below is CURRENT.** Superseded/reversed text has been excluded — see
"Superseded history" at the end for what was dropped and why, so it is never re-introduced.

> 🛑 **v1.11 AMENDMENTS (2026-07-23) — STORAGE MODEL REWRITE. Authority: `docs/DECISIONS.md` (D-59 +
> inline `⚠️ v1.11` banners).** The four-integer bounding-box storage is replaced by an **anchor
> (`x,y`) + `geometry jsonb`** model — new **D-59**, the authoritative storage entry. Summary:
> **D-22 SUPERSEDED** (circle → `{r}`); **D-39 SUPERSEDED** (`id` `integer` → `uuid`; order by
> `numeric z`, load `ORDER BY z, id`); **D-24/D-29/D-36 DROPPED** (no canvas-edge clamp — figures
> may leave the canvas); **D-53 AMENDED** (payload → anchor + geometry, `id` uuid; semantics
> unchanged); **D-46 AMENDED** (`created_at` stays dropped, `type text` + CHECK whitelist kept);
> **D-23 AMENDED** (degenerate guard kept code-side; "never clamp" moot); **D-41 RE-EXPRESSED**
> (line swap-pair landmine carries over). **D-20/D-12/D-03 UPHELD.** Existing figures preserved via
> a hand-written backfill. This mirror carries D-59 below + short markers on affected entries;
> `docs/DECISIONS.md` is the source of truth.

> ⚠️ **v1.12 AMENDMENTS (2026-07-27) — authoritative text in `docs/DECISIONS.md`.** **D-19** — canvas
> **1472×828 → 5760×2634** (3×3 grid of 1920×878 cells; grow-only, no migration, no scaling). **D-43**
> — toolbar now `position: fixed` with a 48px canvas offset (−48 mapping preserved, no JS interop).
> **D-18** — native browser scroll is now the navigation mechanism (not a pan feature — D-04 intact).

> ⚠️ **v1.1 AMENDMENTS (2026-07-20) — for the authoritative amended text, read `docs/DECISIONS.md`
> (it carries inline `⚠️ v1.1` notes on each changed decision).** This mirror has been spot-corrected
> for the concrete facts but is not the source of truth for v1.1. Summary of changes:
> **D-19/D-36/D-58/D-18** — canvas **1280×720 → 1472×828** (may grow, never shrink; no migration).
> **D-31/D-58** — selection indicator **red 2px outline → ~1px blue+white dashed trace on the
> figure's own outline, topmost**; plus a selection **lifecycle** (tool stays armed after a draw,
> one figure selected at a time, deselect on canvas-outside-figure / arm-tool / toolbar-except-Delete).
> **D-06/D-18/D-33/D-37/D-57** — the **"no JavaScript" rule is REMOVED**; its motivations were
> corrected to MVP simplicity. Next: **v1.2** (new figures + dynamic toolbar) in `.planning/backlog/`.

**The three authoritative artifacts** (per the source document's own "READ THIS FIRST"):
`THE SCHEMA` (canonical DDL — v1.11 anchor+geometry), `D-59` (geometry storage — v1.11, supersedes
`D-22`), `D-53` (broadcast message contract). All three are extracted into `constraints.md`.

---

## Product scope

### D-01 — Datastore: PostgreSQL
source: docs/DECISIONS.md (D-01) · Locked (given in brief)
PostgreSQL is the datastore. Not negotiable.

### D-02 — UI framework: Blazor
source: docs/DECISIONS.md (D-02) · Locked (given in brief)
Blazor is the UI framework. Not negotiable.

### D-03 — One canvas per user
source: docs/DECISIONS.md (D-03) · Locked
A user has exactly one canvas. No canvas list, no "new canvas", no naming, no switching.

### D-04 — Feature set: draw, drag, delete
source: docs/DECISIONS.md (D-04) · Locked
Exactly three verbs against a figure: draw, drag (move), delete.
Explicitly OUT of scope: resize, rotate, undo/redo, z-order control, colours/stroke styling,
multi-select, copy/paste, zoom/pan, export. Anything not named in the log is out.

### D-05 — Figure types: line, rectangle, circle, triangle
source: docs/DECISIONS.md (D-05, as amended by D-13) · Locked
Four figure types. **They do NOT share one draw interaction** — line/rectangle/triangle are
drawn corner-to-corner; the circle is drawn centre-out (D-13). All four are *stored* uniformly
as a bounding box (D-22). Interaction and storage are deliberately different things.

---

## Architecture and hosting

### D-06 — Drawing surface: SVG, not HTML5 `<canvas>`
source: docs/DECISIONS.md (D-06) · Locked
Figures are C# objects rendered by Blazor as SVG DOM elements. Each figure is a real DOM
element with its own click handler — hit-testing and redraw come free (materially less code than
`<canvas>`). *(v1.1: the "No JavaScript anywhere" perk phrasing was removed; SVG stands on its merits.)*

### D-07 — Hosting: Blazor Server (InteractiveServer)
source: docs/DECISIONS.md (D-07, as corrected by D-34) · Locked
A single Blazor Web App project. Components talk to PostgreSQL directly via EF Core. **No Web
API layer.** Accepted cost: every pointer-move during a drag is a SignalR round-trip.
**There IS HTTP code to write** (login/logout endpoints) — see D-34. The app runs **two render
modes**: static SSR for login/logout, InteractiveServer for the canvas.

### D-28 — .NET 10
source: docs/DECISIONS.md (D-28) · Locked
.NET 10 (current LTS). Verified present (SDKs 8.0.418 / 9.0.311 / 10.0.301; Docker 29.1.3).
No design consequences.

### D-27 — Local PostgreSQL via Docker Compose
source: docs/DECISIONS.md (D-27) · Locked
`docker-compose.yml` runs Postgres locally. Affects nothing about app design — only how a
database comes to exist during development.

### D-42 — Schema via EF Core migrations, applied on startup
source: docs/DECISIONS.md (D-42) · Locked
C# model is the source of truth; EF Core migrations are versioned in source control and applied
automatically at startup.
**Must be configured explicitly or they silently will not exist:**
- the CHECK constraints of D-22 / D-41 (via `HasCheckConstraint` in `OnModelCreating`)
- the `COMMENT ON TABLE` documenting the circle-as-inscribed-square convention

### D-49 — Structure: one app project + a small test project
source: docs/DECISIONS.md (D-49) · Locked
One Blazor Web App project (pages, notifier service, EF model). `docker-compose.yml` at repo
root. One narrow test project covering exactly the three **silent** failure modes:
1. clamp maths (D-36) — per-axis independence, inclusive bounds, circle draw-clamp
2. circle inscribed-square round-trip (D-22) — centre/radius exact after store+reload+translate
3. line-normalisation landmine (D-41) — an up-and-right diagonal must not come back flipped

---

## Identity, session, routes

### D-08 — Identity: username + plaintext password, no auth
source: docs/DECISIONS.md (D-08) · Locked
`users` table stores the password **literally as typed** — no hashing, no salting. Exists only
to answer "whose canvas do I load?".
WARNING recorded in-source: acceptable ONLY as a throwaway learning project. Never real
credentials; never deployed to the public internet as-is.

### D-17 — Login: one form; an unknown username creates the account
source: docs/DECISIONS.md (D-17) · Locked
Single username + password form. Username exists → check password, load figures. Username does
not exist → create the user and open an empty canvas. Wrong password → error. **No Register page.**
Password comparison is plain string equality against the plaintext column.

### D-44 — Usernames are case-insensitive
source: docs/DECISIONS.md (D-44) · Locked
"Egor" and "egor" are the same user. Stored lowercased, whitespace trimmed, empty rejected,
`username` is UNIQUE. Postgres's default collation is case-sensitive — doing nothing would
silently drop a user into a *different, empty canvas* (which reads as "my work vanished").

### D-26 — Session cookie, lasting until the browser closes
source: docs/DECISIONS.md (D-26) · Locked
A session cookie (no expiry). Log in once — every tab in that browser knows who you are.
**F5 keeps you logged in** (essential: F5 is the official fix for a stale tab, D-10).
Close the browser → logged out.

### D-34 — Login is a static-SSR page plus cookie-auth middleware
source: docs/DECISIONS.md (D-34) · Locked — corrects D-07
An InteractiveServer component **cannot set a cookie** (the HTTP response has already begun).
Therefore:
- `/login` renders as **static SSR**; its form POSTs.
- A handler validates against `users` (creating the user if new, per D-17), calls `SignInAsync`,
  and redirects to the interactive canvas.
- **Cookie authentication middleware** in `Program.cs`, configured as a session cookie (D-26).
- Logout posts to a sign-out endpoint that clears the cookie and redirects to login.
This is cookie plumbing, **not** ASP.NET Core Identity (D-08 rejects Identity).

### D-51 — Identity claim, routes, page protection
source: docs/DECISIONS.md (D-51) · Locked
The numeric `user_id` is written directly into the cookie as a claim; the interactive circuit
reads it straight out — **no database lookup on page load.**
Routes:
- `/login` — static SSR — the username + password form
- `/` — InteractiveServer — the canvas, marked `[Authorize]`
- `POST /logout` — endpoint — clears the cookie, redirects to `/login`
An unauthenticated visitor to `/` is redirected to `/login`.

### D-25 — Logout button
source: docs/DECISIONS.md (D-25) · Locked
Clears the session and returns to the login form. Earns its place as a testability feature: it
is the only way to verify one-canvas-per-user (D-03) actually isolates users.

### D-56 — Logout sits right-aligned in the toolbar strip
source: docs/DECISIONS.md (D-56) · Locked
Logout lives in the same 48px toolbar strip (D-43), right-aligned, visually separated from the
six tool buttons. It is a small HTML form posting to `POST /logout` — not an interactive button
(clearing a cookie requires an HTTP round-trip). **Toolbar height stays 48px**, so D-43's
coordinate constant is unchanged. The "six buttons" rule stays intact — logout is an account
action, not a drawing tool.

---

## Canvas, coordinates, geometry

### D-18 — Canvas: fixed size, 1:1 *(v1.1: motivation = MVP simplicity, not "no JavaScript")*
source: docs/DECISIONS.md (D-18) · Locked
Fixed-size bordered rectangle. **One canvas unit = one CSS pixel, on every screen.** Does not
scale to the window. A canvas that fills every window is mathematically incompatible with
(same relative position + preserved proportions + circles never ovals) — scaling axes
differently *is* the definition of ovalling a circle. At 1:1 the scale factor is pinned to 1.
Mechanism: anchor the canvas at a known constant document position (margin 0, fixed-height
toolbar, canvas immediately below). Canvas coordinate = `PageX`, `PageY − toolbarHeight`.
**LANDMINE: use `PageX`/`PageY`, never `OffsetX`/`OffsetY`.** `OffsetX/Y` is relative to the
event target, and every drag and every selection begins *on a figure*.
Do **not** centre the canvas with `margin: auto` — reintroduces a window-width-dependent offset
the server cannot know.

### D-19 — Canvas dimensions: 5760 × 2634 *(v1.12; was 1472 × 828 in v1.1, 1280 × 720 in v1.0)*
source: docs/DECISIONS.md (D-19) · Locked · ⚠️ amended v1.12 (size) · v1.1 (size)
5760 × 2634 logical units = literal CSS pixels at 1:1 — a 3×3 grid of 1920×878 cells. No scaling
(circles stay round; intentionally not 16:9). The rest of the surface is reached by native browser
scroll (D-18); the toolbar is `position: fixed` (D-43).
**The size may GROW but must never SHRINK** — enlarging keeps every stored figure valid (v1.1 and
v1.12 both did this, no migration); shrinking would orphan figures. Coordinates are only meaningful
relative to it.

### D-20 — Coordinates are integers
source: docs/DECISIONS.md (D-20) · Locked
All coordinates stored as integers. Beyond tidiness: integers can be compared for **exact
equality in a database CHECK constraint** — so geometric invariants are *enforced by the schema*
rather than trusted in code. Accepted cost: a diagonal-derived radius is rounded (< 1 px).

### D-13 — Circle geometry: centre + radius (draw-time)
source: docs/DECISIONS.md (D-13) · Locked
**The press point is the centre**; drag distance sets the radius. Always a true circle, never an
oval. Deliberate special case at *draw* time only; storage is uniform (D-22).

### D-21 — Triangle: derived from a drag, 2 points
source: docs/DECISIONS.md (D-21) · Locked
Drawn exactly like a rectangle (drag out a box). **Apex at top-centre, base along the bottom.**
Accepted cost: every triangle is isosceles and points upward. Right-angled and downward
triangles are not possible.

### D-22 — Geometry storage: four coordinates, always. Circle = its inscribed square. (REVISED)
> 🛑 **v1.11: SUPERSEDED by D-59** (anchor + `geometry jsonb`; circle = `{r}`). Historical below.
source: docs/DECISIONS.md (D-22, revised) · **superseded by D-59 (v1.11)** — historical
**Every figure is exactly four integers — `x1, y1, x2, y2`, all non-null. For every shape these
four numbers ARE its bounding box.**
- Line: `(x1,y1)` one endpoint, `(x2,y2)` the other
- Rectangle: opposite corners
- Triangle: opposite corners of its box
- **Circle: top-left `(cx − r, cy − r)` and bottom-right `(cx + r, cy + r)` of the square it is
  inscribed in.** Recovered on read: `r = (x2 − x1) / 2`, `cx = x1 + r`, `cy = y1 + r`.

Drawing interaction is UNCHANGED (D-13: press centre, drag outward). **Only storage is a square.**

Why it is safe: a move is a translation `x1+=d, y1+=d, x2+=d, y2+=d`; for a circle the `d`
cancels **algebraically, not approximately** — with integers (D-20) a circle dragged ten
thousand times has a bit-identical radius.

**Why the raw columns must BE the bounding box:** generic edge-clamping (D-24/D-36) reads the
raw columns. Any encoding whose columns are not the bounding box forces a per-type bounding-box
function inside the drag loop — the exact circle special case the encoding exists to eliminate,
and one that fails *silently*. **There is genuinely no type dispatch left** in the move or the clamp.

Enforced by the database: `CHECK (type <> 'circle' OR (x2 - x1 = y2 - y1 AND x2 > x1 AND (x2 - x1) % 2 = 0))`
(square · positive radius · even side, so `r` and the centre are always exact whole numbers).
An oval cannot occur: the CHECK is exact integer arithmetic, and the renderer derives one scalar
`r = (x2 − x1) / 2` and emits `<circle>`, which has no second radius to distort.
Accepted cost: the convention must be learned by anyone reading the table — mitigate with
`COMMENT ON TABLE` (D-42).

### D-41 — Normalise on write — but NOT the same way for every shape
> ⚠️ **v1.11: RE-EXPRESSED for anchor+geometry (D-59)** — rectangle → positive `{w,h}`; line → one
> endpoint + `{dx,dy}`, **swap the whole point pair, never sort axes** (the landmine carries over).
source: docs/DECISIONS.md (D-41) · Locked (re-expressed by D-59 in v1.11)
Coordinates are put into canonical order **once, before the INSERT**, in **exactly one place**.
- **Rectangle / triangle / circle → sort the axes independently** (`x1 = min(x1,x2)`, etc.). Safe:
  a box is a box. (A circle is already canonical by construction.)
- **LANDMINE — Line → swap the WHOLE POINT PAIR, never sort axes independently.** A line from
  (0,100) to (100,0) sorted per-axis becomes (0,0)→(100,100): **the opposite diagonal.** Swap the
  points as a unit (if `x1 > x2`, or if `x1 == x2` and `y1 > y2`).

Consequence: after normalisation `x1 ≤ x2` for every shape, and `y1 ≤ y2` for rectangle/triangle/
circle — **but not for a line**, whose `y` may run either way. This is why D-36's clamp keeps its
min/max bounding-box computation (still fully generic, but it cannot be dropped).

### D-24 — Figures stop at the canvas edge
> 🛑 **v1.11: DROPPED** (no edge clamp; figures may leave the canvas — D-59). Historical.
source: docs/DECISIONS.md (D-24) · **dropped in v1.11** — historical
A figure cannot be dragged beyond the canvas boundary. It stops, and **slides along the edge**
rather than sticking. Clamp is applied live on every pointer-move; the clamped position is what
persists on release. Formula in D-36. **This decision is what forced the reversal of D-22.**

### D-29 — Drawing also stops at the canvas edge
> 🛑 **v1.11: DROPPED** with D-24 (no draw-time edge clamp — D-59). Historical.
source: docs/DECISIONS.md (D-29) · **dropped in v1.11** — historical
While drawing, dragging past the boundary does not grow the shape — the corner clamps to the
edge while the cursor keeps moving. Gives the app **one rule: figures live entirely inside the
canvas, always.** Nothing created out of bounds, nothing moved out of bounds.

### D-36 — The clamp: exact formula, bounds are inclusive
> 🛑 **v1.11: DROPPED** with D-24/D-29 (no clamp — D-59; the canvas keeps its 1472×828 size). Historical.
source: docs/DECISIONS.md (D-36) · **dropped in v1.11** — historical
`W = 1472`, `H = 828` *(v1.1; was 1280 × 720)*. **Bounds are INCLUSIVE: the valid domain is `0..1472 × 0..828`.** SVG
coordinates are geometric edge positions, not pixel cells — `x2 = 1472` means the right edge sits
exactly *on* the boundary, which is the "stopped at the edge" state.

Move clamp (bounding box = min/max of the four raw columns, for every shape, thanks to D-22):
```
bx1 = min(x1,x2)   by1 = min(y1,y2)
bx2 = max(x1,x2)   by2 = max(y1,y2)

dx' = clamp(dx, −bx1, W − bx2)      where clamp(v, lo, hi) = min(max(v, lo), hi)
dy' = clamp(dy, −by1, H − by2)

then translate uniformly:  x1 += dx'  y1 += dy'  x2 += dx'  y2 += dy'
```
**Per-axis independence is the point:** `dx'` never reads `y`. A figure pinned against the right
edge can still slide up and down — it slides along the wall rather than sticking.

**Ordering: clamp → render → broadcast.** Never broadcast a raw, unclamped position.

**The one genuinely type-specific rule — the circle draw-clamp:** the centre is fixed at the press
point and growing `r` pushes all four extremes outward at once, so
`r = min( round(distance), cx, cy, W − cx, H − cy )`.
Known UX consequence: **pressing near an edge forces a tiny circle** (press at (10,360), drag
200 px right → radius caps at 10, because the left rim would otherwise exit the canvas). This is
inherent to D-13 × D-29 and would exist under any circle encoding.

No canvas-bounds CHECK constraints in the database — D-24/D-29 already guarantee it.

---

## Interaction: toolbar, selection, drawing, dragging, deleting

### D-16 + D-30 + D-33 — The toolbar (six buttons, authoritative)
source: docs/DECISIONS.md (D-16 superseded; D-30, D-33 current) · Locked
```
[ pointer ] [ line ] [ rectangle ] [ circle ] [ triangle ] [ delete ]
```
**Six buttons.** Click one to arm it; the armed button stays visibly active. Logout sits
right-aligned in the same strip, separate from the six (D-56).

### D-30 — Selection: a pointer tool
source: docs/DECISIONS.md (D-30) · Locked
- **Pointer armed:** clicking a figure selects it; dragging a figure moves it.
- **A shape armed:** dragging always draws that shape — **even on top of existing figures.**
Accepted cost: the app has modes; switching between drawing and moving costs a click.
This is what makes it possible to draw a small circle *inside* a big rectangle.

### D-15 — Delete: click to select, then delete (selection half only)
source: docs/DECISIONS.md (D-15) · Locked in part
Click a figure to **select** it (visible highlight). Selection is **local UI state only** — never
persisted, never broadcast (your other monitor does not show what you have selected).
The Delete *key* is gone — see D-33.

### D-33 — Delete is a toolbar button, not the Delete key
source: docs/DECISIONS.md (D-33) · Locked — supersedes the Delete-key half of D-15
*(v1.1: motivation = MVP simplicity + unambiguous behaviour.)* A pure-Blazor `@onkeydown` fires only
on a focused element; clicking any toolbar button moves focus and a keyboard Delete would silently
stop working. Resolution: select a figure, then click the **Delete button in the toolbar.** *(A
Delete-key shortcut could now be added later — the no-JS reason is gone.)*

### D-31 — Selection appearance and behaviour *(⚠️ v1.1 amended)*
source: docs/DECISIONS.md (D-31) · Locked (v1.1 style pinned by D-58)
- The selected figure is marked by a **~1px blue+white dashed trace on its own outline, drawn
  topmost, `pointer-events:none`** *(v1.1; was a red 2px outline)* — visible even behind larger figures.
- **At most one figure selected at a time.** **Drawing selects the drawn figure** and **the tool
  stays armed**.
- **Deselect on:** pressing the canvas outside the selected figure; arming any tool; pressing the
  toolbar **except Delete**. Pressing a figure selects that one.
- **The pointer tool is armed on page load** — a stray first click cannot create a figure.
- **Overlapping figures:** a click hits the topmost, which in SVG is whichever was drawn last
  (free from the DOM; no code).
- Selection is local UI state only, never broadcast (a synced figure is not selected in the receiver).

### D-35 — Live preview while drawing
source: docs/DECISIONS.md (D-35) · Locked
The shape is visible under the cursor as you drag it out (the rectangle stretches, the circle
grows). The preview is **local only — never broadcast** (D-53). Only the finished figure is.

### D-48 — Click vs drag: a 3-pixel threshold
source: docs/DECISIONS.md (D-48) · Locked
With the pointer tool armed: move **< 3 px** before release → **click** (select; **no database
write**). Move **≥ 3 px** → **drag** (moves, persisted on drop).
**Starting a drag also selects** the figure, and it **stays selected after the drop** — so you can
drag something and immediately delete it. Without the threshold, a 1-px hand-wobble fires a
useless UPDATE and nudges the figure.

### D-37 — Drag termination *(v1.1: prevents a hanging drag, not a no-JS workaround)*
source: docs/DECISIONS.md (D-37) · Locked
Prevents a drag that never ends (a release outside the window would strand the figure unpersisted).
Two markup-only rules *(a `setPointerCapture` "keep grabbed anywhere" upgrade could now be added later)*:
1. **`pointerleave` on the drag surface commits the drag** at its current clamped position.
2. **The `Buttons` guard:** on any `pointermove` while dragging, if `PointerEventArgs.Buttons`
   shows the primary button is already up, commit and end the drag (catches the Alt-Tab case).
Coherent because of D-36: to leave the surface the cursor must cross the boundary, and by then
the figure is **already pinned at the edge** — so "pointer left → commit" drops the figure exactly
where the user can already see it. Nothing jumps.
Recommended refinement: put `pointermove`/`pointerup` on a **page-spanning wrapper element**, not
the SVG (D-18's `PageX/PageY` arithmetic is target-independent). Use `pointerleave`, **not
`pointerout`** (`pointerout` also fires when the cursor moves onto a child figure).

### D-57 — Leaving the surface mid-DRAW commits the figure
source: docs/DECISIONS.md (D-57) · Locked — extends D-37 to drawing
Releasing outside the window, or leaving the drag surface, **commits the in-progress figure** at
its clamped preview position. One consistent rule for both gestures.
**Consequence: there is NO way to abandon a draw once started.** Escape is impossible (it needs a
document-level key listener → JavaScript). Change your mind → draw it, then delete it.

### D-50 — The minimum-size guard is PER-TYPE
source: docs/DECISIONS.md (D-50) · Locked — retracts D-23's "one shared guard"
A shared "reject only zero-size draws" guard is **impossible to keep**: dragging a rectangle
horizontally (press (100,100), release (300,100)) has start ≠ end, so a shared guard lets it
through — and the row has **zero height**, violating `box_is_a_box`, throwing on INSERT, and
surfacing a wrong, baffling "Could not save — is the database running?" message.

The guard — two rules, mirroring the CHECK constraints exactly:
- **Line:** rejected when both endpoints are identical (zero length). **Horizontal and vertical
  lines are legal and must work.**
- **Rectangle / triangle / circle:** rejected when width **or** height is zero (circle: radius zero).

Because these mirror D-41's constraints exactly, **the app can never write a row the database
would refuse.** A rejected draw **fails silently** — the figure simply does not appear. No message.

### D-32 — Two usability costs, accepted deliberately
source: docs/DECISIONS.md (D-32) · Locked
1. The minimum-size guard was **not** raised to a ~5-px threshold, so a stray 1–2 px drag creates
   a tiny, nearly-invisible, hard-to-select figure. Annoying, not dangerous.
2. **Lines have no widened hit area.** Selecting a line means clicking within ~a pixel of it
   (mitigated only by D-58's 2px stroke). The standard fix (an invisible thick transparent stroke)
   was considered and not taken; it is additive and can be added at any time.

---

## Persistence and sync

### D-09 — Persistence: immediate, per operation. No Save button.
source: docs/DECISIONS.md (D-09) · Locked
Every operation writes to PostgreSQL the moment it completes: draw → `INSERT` one row; drag (on
drop) → `UPDATE` that row's coordinates; delete → `DELETE` that row.
**There is NO Save button anywhere.**
Load-bearing: it is what makes multiple open tabs safe — each figure is its own row, so two tabs
writing different figures are separate INSERTs that cannot touch each other. The database is
always the merged truth. (A Save button would commit the whole canvas as one unit and silently
erase the other tab's work.)

### D-10 — Mandatory: handle zero-row UPDATE/DELETE (staleness guard)
source: docs/DECISIONS.md (D-10) · Locked — required regardless of any other decision
Use `ExecuteUpdateAsync` / `ExecuteDeleteAsync` (they return an affected-row count). If a drag's
UPDATE affects **0 rows**, the figure no longer exists — **silently remove it from that tab's
view.** No message, no prompt, no merge.
Needed even with live sync: Blazor Server reconnects a slept tab to its **same in-memory state**;
it does not re-read the database, so such a tab misses broadcasts and shows a stale picture
indefinitely. With `ExecuteUpdateAsync` no exception is thrown at all.
DELETE-of-a-ghost is naturally idempotent and needs no guard; only UPDATE does.
**Manual fallback for the stale tab is F5.** No "Reload" button is built.

### D-11 — Multi-tab: live sync with real-time drag glide
source: docs/DECISIONS.md (D-11, as amended by D-40/D-47/D-53/D-54) · Locked
Changes in one tab appear **live** in the user's other tabs. Dragging in tab A makes the figure
**glide in real time** in tab B — not jump on release.

**The premise that makes this cheap:** one human has one mouse. Two tabs physically cannot be
edited at the same instant — edits are serialised by physics. This **deletes** all locking, all
conflict-resolution UX, optimistic concurrency tokens, operation queues, and CRDT/OT thinking.

**Mechanism:** a **DI singleton notifier service** holding per-`user_id` subscribers. Tabs
subscribe on init, publish after each operation; Blazor Server's existing per-tab SignalR channel
does the push when `StateHasChanged` re-renders. **Delta payloads** (D-53), not a dirty signal —
intermediate drag positions are never written to Postgres, so a dirty signal would have nothing
to re-read and structurally could not glide.

**Broadcasting ≠ persisting:** every `pointermove` is *already* a server round-trip; live glide
re-broadcasts that position through the in-memory notifier. **Postgres sees exactly one UPDATE
per drag, on drop** (D-09). Intermediate positions travel through memory only.

**Irreducible core — all mandatory:**
1. **Unsubscribe in `Dispose()`** — else closed tabs leak delegates and publishes hit disposed
   circuits (`ObjectDisposedException`).
2. **`InvokeAsync(StateHasChanged)`** in every handler — the event fires on the *publisher's*
   circuit thread.
3. **Echo filter** — tag the sender (per-circuit GUID, D-53), ignore your own broadcast.
4. **Mid-drag, discard ALL incoming broadcasts** — `if (_dragging) return;` (D-54).
   *(See INGEST-CONFLICTS.md: D-11's own summary of D-54 states the opposite of D-54. D-54 wins.)*
5. **Idempotent apply keyed by figure Id — and `move` is UPDATE-ONLY, never insert** (D-40).
6. **Throttle drag broadcasts: 50 ms, trailing edge guaranteed** (D-47).
7. **Key notifier events by `user_id`**, matching the database and the cookie claim (D-51).
8. **Clamp → render → broadcast, in that order** (D-36).
9. **A zero-row UPDATE broadcasts a delete** (D-40).

### D-40 — Killing the resurrection hole (both fixes)
source: docs/DECISIONS.md (D-40) · Locked
The bug: an "idempotent upsert" **inserts when the row is absent** — so a stale tab's drag
broadcasts can **resurrect a figure another tab correctly deleted**, and nothing tells the other
tab to drop it again.
Both fixes are applied:
1. **A `move` broadcast may only UPDATE — never INSERT.** If the receiving tab does not already
   know the figure, it **ignores the message entirely.** A figure can only ever be created by a
   `draw` broadcast.
2. **A zero-row UPDATE broadcasts a delete** — so every other tab drops the ghost too.
Corrected rule: **apply is idempotent, and move is update-only.**

### D-47 — Drag broadcast throttle: 50 ms, trailing edge guaranteed
source: docs/DECISIONS.md (D-47) · Locked
Drag-glide broadcasts throttled to at most one per **50 ms** (≈20/sec).
**The trailing edge is guaranteed: the final position is always sent before the drop** — otherwise
the glide stops short and the other monitor twitches at the end of every drag.
This is a **throttle**, not a debounce (a debounce would show nothing until the drag ended).

### D-53 — The broadcast message contract (canonical)
> ⚠️ **v1.11: AMENDED payload (D-59)** — `move`/`rollback` carry the anchor `x,y`; `draw` carries
> anchor + `type` + `geometry`; the figure `id` is now a `uuid`. **All semantics unchanged** (draw
> creates; move is UPDATE-only; move carries no `type`; no `drop` kind; mid-drag discard; 50 ms throttle).
source: docs/DECISIONS.md (D-53) · Locked (payload amended by D-59 in v1.11) — **AUTHORITATIVE**,
supersedes the partial descriptions in D-11, D-22 and D-40. Full contract in `constraints.md`.
`sender` is a **per-circuit GUID**, generated once when a tab's canvas component initialises;
it exists solely for the echo filter.
Kinds: `draw` (insert-or-update; the only kind that may create a figure; sent *after* the INSERT
because `id` does not exist until then), `move` (**UPDATE ONLY — never insert**; unknown figure →
ignore entirely), `delete` (idempotent no-op if unknown), `rollback` (restore to given coords;
applied update-only, like `move`).
**There is no separate `drop` kind** — a drag's final position is the last `move` (guaranteed by
D-47's trailing edge). **`move` carries no `type`** — a figure's type never changes.
**Draw previews are NOT broadcast** (D-35).

### D-54 — Mid-drag, a tab ignores ALL incoming broadcasts
source: docs/DECISIONS.md (D-54) · Locked
While a local drag is in progress the tab **discards every incoming broadcast** —
`if (_dragging) return;` — not merely those about the figure being dragged. Safe because of the
one-mouse premise. Explicitly **rejected**: the narrower "ignore only messages about the figure
currently being dragged".
Accepted cost, stated honestly: this **breaks the free multi-device degradation** D-11 advertises
— a message arriving from a phone mid-drag is lost permanently until F5. Accepted because
multi-device was never a requirement.

### D-39 — `figures.id` is a sequential integer, and it IS the z-order
> 🛑 **v1.11: SUPERSEDED by D-59** (`id` `integer` → `uuid`; order carried by `numeric z`). Historical.
source: docs/DECISIONS.md (D-39) · **superseded by D-59 (v1.11)** — historical
Database-generated sequential integer. Figures load with `ORDER BY id`.
Load-bearing: within a session "topmost = drawn last" comes free from the DOM — **after F5 it does
not**, so creation order must be reconstructed from the database. The sequential id recovers it
exactly and doubles as the z-order.
Accepted cost: the id does not exist until the INSERT completes, so drawing is strictly
**insert → get id → broadcast**. The broadcast cannot be fired optimistically.
(Rejected: a client-generated UUID — it carries no creation order.)

### D-45 — Database errors: a friendly message, and the app stays alive
source: docs/DECISIONS.md (D-45) · Locked (the undecided "and/or" is settled by D-52)
DB failures are caught and surfaced as a readable message ("Could not save — is the database
running?"). The app keeps running; it does not crash the circuit.
**Honest consequence:** if a save fails, the picture on screen no longer matches the database — a
silent "oh well" would be worse than crashing. Handled by D-52.
This is the *only* general error-handling stance. D-10's zero-row guard is separate and is **not
an error at all** — it is expected staleness.

### D-52 — Save-failure policy: retry transient, then roll back everywhere
source: docs/DECISIONS.md (D-52) · Locked — supersedes D-45's undecided "and/or"
**Retry up to 2 additional times with short delays — but only if the failure is transient**
(connection dropped, database briefly unreachable).
**Never retry non-transient failures:** validation errors, CHECK-constraint violations, a missing
or deleted figure, or an UPDATE that affected zero rows (which is not an error at all — D-10).
If all attempts fail:
1. **Broadcast a `rollback` event** carrying the original coordinates to all other active tabs.
2. **Restore the figure to its original position** in the current tab.
3. **Show a modal:** "The change could not be saved. The canvas will be reloaded from the database."
4. **On OK, reload the canvas state from PostgreSQL.**
**The figure's original coordinates are retained for the entire drag**, precisely so this is possible.
Why it is forced: on a failed *drop*, the drag-glide broadcasts have already gone out — every other
tab already shows the new position while the database still holds the old one. Without the rollback
broadcast, **every open screen is lying**.

---

## Schema and data

### D-12 — Two tables. No `canvases` table.
source: docs/DECISIONS.md (D-12) · Locked — **only the two-table principle is normative; its DDL
sketch is stale. The authoritative DDL is `THE SCHEMA` (see constraints.md).**
`users` and `figures`. **"The canvas" is not an entity in the database** — it is simply the set of
figures belonging to a user. No canvas row, no canvas id, no join.
(Rejected: a three-table users/canvases/figures schema; and storing the canvas as one JSONB
document per user — incompatible with D-09's per-operation persistence.)

### D-46 — `type` is text + CHECK. No `created_at`.
> ⚠️ **v1.11: AMENDED (D-59)** — `created_at` stays dropped (order is now `numeric z`); `type text` +
> whitelist CHECK **kept** (Variant 1, most valuable through the backfill); a `figure_types` lookup
> table rejected. The old "CHECKs are written as `type <> 'circle'`" rationale no longer applies (the
> geometry CHECKs are gone), but the text column stands on its own.
source: docs/DECISIONS.md (D-46) · Locked (amended by D-59 in v1.11)
`type` is a **text** column constrained to `('line','rectangle','circle','triangle')`.
**`created_at` is dropped** — order is carried by the `numeric z` column (D-59), so nothing reads a timestamp.

### D-59 — Storage model: anchor (x, y) + geometry JSON (supersedes D-22) *(v1.11)*
source: docs/DECISIONS.md (D-59) · Locked — **AUTHORITATIVE storage model from v1.11 on**
A figure = an **anchor** (`x, y`, integer columns — D-20 upheld) + a **`geometry jsonb`** column
holding the shape's form **relative to the anchor** (circle `{r}`, rectangle `{w,h}`, …). **Drag
updates only `x, y`, for every shape** — the form never changes on a move. Replaces D-22's four
integers. `id` is a **`uuid`** (`gen_random_uuid()` default); layer order is a **`numeric z`** — no
UNIQUE, fractional (insert-between = midpoint) — loaded `ORDER BY z, id`; index **`(user_id, z)`**
replaces `ix_figures_user_id`. **No canvas-edge clamp** (D-24/D-29/D-36 dropped) — figures may leave
the canvas (accepted risk: currently unrecoverable, no pan/undo). **No DB CHECK on `geometry`** — the
server is the sole writer (D-09), an explicit trust boundary; **`type text` + whitelist CHECK kept**
(D-46, Variant 1 — most valuable through the backfill). Degenerate-draw guard kept **code-side**
(`MinSizeGuard`, D-23 #1). Normalisation re-expressed (D-41): rectangle → positive `{w,h}`; line →
one endpoint + `{dx,dy}`, **swap the whole point pair, never sort axes**. Existing figures
**preserved via a hand-written backfill** (`x1,y1,x2,y2 → x,y,geometry`) tested against the immutable
fixture `tests/.../Fixtures/v1.1-pre-rewrite.sql` + MANIFEST. Left for plan time: per-type JSON shape
(esp. line), `z` formula, uuid v4-vs-v7, `Type`-as-C#-enum, backfill mechanism.
Rejected: keeping the bbox; a `figure_types` lookup table; dropping the `type` CHECK during the
migration; a DB CHECK on `geometry`.

---

## Appearance

### D-14 — One fixed style. No colours.
source: docs/DECISIONS.md (D-14) · Locked (values in D-38, D-58)
Every figure renders with the same hard-coded stroke and fill. **No style columns in the database.
No colour picker in the UI.**

### D-38 — Appearance: white fill, black outline
source: docs/DECISIONS.md (D-38) · Locked
Every figure renders with a **white fill and a black outline** (a line has no interior, so only the
stroke applies).
**Why the fill is load-bearing:** SVG does **not** register clicks inside an *unfilled* shape. Had
figures been wireframes, **D-30's entire rationale would have collapsed** — "pressing inside a
rectangle grabs the rectangle" would simply not be true.
Accepted cost: an opaque white fill means overlapping figures **fully occlude** each other. Coherent
with D-31 (click hits topmost) and D-39 (deterministic z-order).

### D-55 — Page background: light grey
source: docs/DECISIONS.md (D-55) · Locked
**Not cosmetic:** D-43 gives the canvas **no border**, so the *only* thing that makes the canvas
boundary visible is contrast with the page behind it. The browser default page background is white
— leaving it unspecified would make the canvas edge **invisible**, and "figures stop at the edge"
(D-24) would look like an inexplicable bug.

### D-43 — Page layout: a 48px toolbar, no canvas border
source: docs/DECISIONS.md (D-43) · Locked — **the constant every coordinate in the app flows through**
- Page margin **0**
- **Toolbar: exactly 48px tall**, at the top
- **Canvas immediately below, at document position (0, 48)**. Anchored top-left, *not* centred.
- **No CSS border on the SVG** — a border shifts the SVG's interior by its own width, making the
  mapping `PageY − 48 − 1`: one more constant that can be silently forgotten (the classic
  "the shape appears slightly off from where I clicked" bug).

Coordinate mapping: `canvasX = PageX`, `canvasY = PageY − 48`.
Reminder: `PageX`/`PageY`, **never** `OffsetX`/`OffsetY`.

### D-58 — The remaining constants *(⚠️ v1.1 amended)*
source: docs/DECISIONS.md (D-58) · Locked — full table in `constraints.md`
Figure outline **black, 2px**; fill **white**; **selected** figure indicator **~1px blue+white
dashed trace on the figure's own outline, topmost** *(v1.1; was red 2px)*; page background light
grey; canvas white **5760×2634** *(v1.12; was 1472×828 in v1.1, 1280×720 in v1.0)*, no border; toolbar 48px *(v1.12: `position: fixed`)*.
A **2px** stroke (not 1px) is deliberate — D-32 declined the widened hit-area, so the stroke itself
is the only click target a line has.
Behaviour: **the Delete button is greyed out and unclickable when nothing is selected.**
**Passwords must be non-empty** (a formality — plaintext protects nothing — but it stops a user
creating an account whose blank password they cannot recall).
Docker Compose: Postgres **17**, port **5432**, database **`canvas`**, user/password
**`postgres`/`postgres`**, **named volume** so figures survive a restart. Connection string in
`appsettings.Development.json`.

---

## Superseded history (recorded so it is never re-introduced)

These are the *dead* versions. They exist in `docs/DECISIONS.md` and are explicitly flagged there;
none of their content is carried into the current decisions above.

- **D-05 (original)** — claimed all four shapes are created by the same bounding-box drag.
  **Amended by D-13:** the circle is drawn centre-out.
- **D-07 (original)** — claimed "no HTTP code to write". **RETRACTED by D-34:** an interactive
  component cannot set a cookie; login/logout are static-SSR/endpoints and the app has two render modes.
- **D-11 (original)** — "idempotent upsert" on receive. **This was a BUG** (it resurrects deleted
  figures). Amended by D-40 (move is update-only), D-47 (throttle pinned to 50 ms, not "~30–50 ms"),
  D-53 (message contract), D-54 (mid-drag rule). Also: notifier keyed by `user_id`, not username.
- **D-12 (DDL sketch)** — stale; still shows the dropped `created_at` column. **Use `THE SCHEMA`.**
- **D-15 (Delete key)** — superseded by **D-33** (a toolbar Delete button). The selection half stands.
  Its note "a line needs an invisible wider hit area" is superseded by **D-32** (declined) + **D-58**
  (2px stroke instead).
- **D-16** — "four toolbar buttons". **It is SIX** (D-30 added pointer, D-33 added delete).
- **D-22 (original)** — circle stored as **centre + rim point**. **REVERSED.** The raw columns were
  not the bounding box, so a generic clamp permitted 90 px of circle hanging off the top of the
  canvas. The circle is stored as its **INSCRIBED SQUARE**. Also dead: D-22's "one uniform sync
  payload `{id, type, x1, y1, x2, y2, sender}`" — superseded by D-53 (`move` carries no `type`).
- **D-23** — "one shared minimum-size guard, not a circle special case". **RETRACTED by D-50:** the
  guard is **per-type** (a zero-height line is legal; a zero-height rectangle is not). D-23's guard 2
  ("never clamp coordinates individually") **stands** and is folded into D-36. D-23's stated *reason*
  for omitting bounds CHECKs ("a figure legitimately overhanging the edge") is also stale — corrected
  by D-36 (the decision stands; only the reason changed).
- **D-30** — "five toolbar buttons". **It is SIX** (D-33).
- **D-45** — its undecided "and/or" on save failure. **Settled by D-52.**
- **D-22 (inscribed square) & D-39 (integer id = z-order)** — **SUPERSEDED by D-59 (v1.11):** storage
  is now anchor + `geometry jsonb`, `id` is `uuid`, order is `numeric z` (`ORDER BY z, id`). These
  were *correct* for v1.0/v1.1 (unlike the dead rim-point encoding) — superseded by a model change,
  not a bug. Dropped with the model: **D-24/D-29/D-36** (the canvas-edge clamp) — figures may now
  leave the canvas.
