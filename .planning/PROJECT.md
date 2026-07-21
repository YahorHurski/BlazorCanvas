# BlazorCanvas

## What This Is

A Blazor Server web app (.NET 10) where a user logs in and draws simple geometric figures — line,
rectangle, circle, triangle — on a fixed 1472 × 828 SVG canvas. Each user has exactly one canvas.
Figures can be **drawn, dragged and deleted**, nothing else. Every operation persists to PostgreSQL
immediately, and the user's other open tabs mirror the canvas live — a drag *glides* on the second
monitor in real time.

It is a deliberate learning project: MinVP and fiercely scoped. *(v1.0 was built without a single
line of hand-authored JavaScript; that self-imposed rule was **removed in v1.1** — see Constraints —
because the real motivation was always MVP simplicity, not JS avoidance.)*

## Core Value

**The canvas is always the truth, everywhere at once** — what you draw persists instantly, and every
other tab you have open shows it happening live, including a figure gliding in real time as you drag it.

## Definition of Done

The project is done when the user can do this, in one sitting:

> "I can log in, draw all four shapes, drag and delete them, open the app on a second monitor, and
> watch a figure GLIDE in real time as I drag it on the first — with everything surviving a refresh."

This deliberately makes the hardest feature — live cross-tab sync with real-time drag glide
(D-11, D-47, D-53, D-54) — part of the definition of success, so **no phase can quietly defer it**.

## Requirements

### Validated

- [x] **AUTH-01/02/03** — Login (unknown username self-registers), session cookie, logout — Validated in Phase BC-02: Login, Session & Logout (2026-07-15)
- [x] **CANV-01/02** — The SVG canvas at (0, 48); the six-button toolbar — Validated in Phase BC-03: The Canvas & Drawing (2026-07-16). *(Canvas was 1280 × 720 at v1.0; enlarged to 1472 × 828 in v1.1 — see CANV-03.)*
- [x] **DATA-01** — One canvas per user; load `WHERE user_id ORDER BY id`; cross-user isolation — Validated in Phase BC-03: The Canvas & Drawing (2026-07-16)
- [x] **FIG-01** — Draw all four shapes: live preview, edge clamp, silent degenerate rejection, immediate insert — Validated in Phase BC-03: The Canvas & Drawing (2026-07-16)
- [x] **FIG-02/03/04** — Select, drag with edge clamping, and delete — Validated in Phase BC-04: Select, Drag & Delete (2026-07-16)
- [x] **DATA-03/04** — Staleness guard and save-failure rollback/reload recovery — Validated in Phase BC-05: Live Cross-Tab Sync (2026-07-17)
- [x] **SYNC-01** — Live cross-tab sync with real-time drag glide — Validated in Phase BC-05: Live Cross-Tab Sync (2026-07-17)
- [x] **DATA-02** — Per-operation persistence; no Save button; migrations at startup; two tables only — Validated in Phase BC-01: Database, Schema & Geometry Core (2026-07-15)
- [x] **TEST-01** — The three mandated tests for the three *silent* failure modes — Validated in Phase BC-01: Database, Schema & Geometry Core (2026-07-15)
- [x] **CANV-03** — Canvas enlarged to 1472 x 828; existing figures remain valid with no migration — Validated in Phase BC-06: Canvas Resize to 1472x828 (2026-07-21)
- [x] **SEL-01** — Selection lifecycle: tools remain armed after drawing, one local selection is maintained, and the required deselect/Delete routes work — Validated in Phase BC-07: Selection Lifecycle & Restyle (2026-07-21)
- [x] **SEL-02** — Selection indicator is a topmost blue-and-white dashed trace on the figure's own outline, replacing the red outline — Validated in Phase BC-07: Selection Lifecycle & Restyle (2026-07-21)

**All 15 v1 requirements validated — shipped in v1.0 (2026-07-17).**

### Active — v1.1 (milestone opened; roadmap created via `/gsd-new-milestone`)

v1.0 shipped the v1 set. **v1.1 is now the active milestone** — four user-approved changes, all
recorded in `docs/DECISIONS.md`. The REQ-IDs below are **final** (assigned when
`/gsd-new-milestone` ran, 2026-07-20). No database migration is needed for any of them.

- [x] **ARCH-01** — The former "no hand-authored JavaScript" constraint is removed and the
  motivations of D-06/D-18/D-33/D-37/D-57 are correctly recorded as MVP simplicity or their
  independent behavioural rationale. Validated in Phase BC-08: Architecture Constraint Cleanup
  (2026-07-21); permissive/doc-only, with no code changes.

**Next milestone (v1.2) is scoped but not started:** new figure types (ellipse, 5-point star,
hexagon, pentagon, right-angle triangle L/R, four arrows) + a dynamic split-button toolbar. Full
plan: `.planning/backlog/v1.2-figures-and-toolbar.md`. Its decision amendments happen when v1.2 is
kicked off. Anything not named in `docs/DECISIONS.md` is still out until added there **by name**.

### Out of Scope

Locked out by D-04, D-14, D-08. Anything not named in `docs/DECISIONS.md` is out until it is added
there by name.

- **resize, rotate, undo/redo, z-order control, multi-select, copy/paste, zoom, pan, export** — D-04:
  the app has exactly three verbs (draw, drag, delete). Scope is the feature.
- **Colours / stroke styling / a colour picker** — D-14: one fixed style, no style columns.
- **Real authentication, password hashing** — D-08: plaintext passwords, deliberately. The `users`
  table exists only to answer "whose canvas do I load?". Throwaway project only.
- **A Save button** — D-09: persistence is per-operation. A Save button would commit the whole canvas
  as one unit and silently erase another tab's work.
- **A Reload button** — D-10: F5 is the documented manual fallback for a stale tab.
- **A canvas list / "new canvas" / naming / switching** — D-03: one user, one canvas.
- ~~**Application-authored JavaScript, in any form**~~ — **no longer out of scope (v1.1).** The
  no-JS rule was removed; hand-authored JS/interop is now allowed where it earns its place (see
  Constraints). It is simply not *needed* for anything built so far.
- **Locking, concurrency tokens, merge UI, CRDT/OT** — D-11: one human has one mouse. Two tabs cannot
  be edited at the same instant. "Which tab wins?" is answered by physics, not by code.

## Context

- **Source of truth:** `docs/DECISIONS.md` — a consolidated ADR set of **58 locked decisions**
  (D-01…D-58), which survived two hostile audits. Synthesized into `.planning/intel/`
  (`SYNTHESIS.md` is the entry point; `decisions.md`, `requirements.md`, `constraints.md`,
  `context.md`).
- **Where the risk actually is.** The ADR audit found the log *airtight wherever the human was in the
  room* (data model, sync semantics, geometry, scope) and *silent wherever the framework was in the
  room* (cookies, HTTP, pointer capture, keyboard focus, schema creation, layout constants, error
  paths) — until D-33…D-58 closed those gaps. Risk is therefore concentrated in **framework seams**
  and **silent geometric bugs**. That is exactly where the three mandated tests point (D-49).
- **The three landmines** (each fails *silently* if missed):
  1. **Never clamp coordinates individually.** Clamp the movement *delta*, then translate all four
     uniformly. Clamping `x2` alone resizes the figure instead of moving it.
  2. **Never normalise a line by sorting its axes.** (0,100)→(100,0) would become (0,0)→(100,100) —
     the opposite diagonal. Swap the whole point pair. (Sorting axes *is* correct for
     rectangle/triangle/circle.)
  3. **Never use `OffsetX`/`OffsetY`.** Use `PageX`/`PageY`. `OffsetX` is relative to the *event
     target* — and every drag and every selection begins *on a figure*.
- **Interaction and storage are different things.** A circle is *drawn* centre-out (press = centre,
  drag = radius) but *stored* as the square it is inscribed in. Every figure is four integers that
  are always its bounding box.
- **Environment:** Windows dev machine. .NET SDKs 8.0.418 / 9.0.311 / 10.0.301 and Docker 29.1.3
  verified present.

## Constraints

- **Tech stack**: **.NET 10, Blazor Server** — a single Blazor Web App project, no Web API layer.
  Components talk to PostgreSQL directly via EF Core/Npgsql. (D-02, D-07, D-28, D-49)
- **Two render modes**: static SSR for `/login` and `POST /logout`; InteractiveServer for the canvas
  at `/`. **An interactive Blazor component cannot set a cookie** — the HTTP response has already
  begun. This is not optional. (D-34, D-51)
- **~~NO APPLICATION-AUTHORED JAVASCRIPT~~ — REMOVED in v1.1.** This self-imposed rule is **no
  longer in force.** It was never the real motivation behind the decisions it was credited with
  (D-18 fixed canvas, D-33 toolbar Delete, D-37 `pointerleave` termination, D-57 no draw-abort) —
  the real motivation was **MVP simplicity**, and each of those decisions has been re-worded to say
  so (see `docs/DECISIONS.md`). Removal is **permissive**: it changed no code and pursues no new JS
  today; it only re-opens future options (a Delete-key shortcut, `setPointerCapture` drag,
  Escape-to-cancel), each of which would be its own decision. Hand-authored JS/interop is now
  **allowed** where it earns its place.
  - *(Historical note, still true: framework JS was never the target. `_framework/blazor.web.js` is
    mandatory — the Blazor Server circuit **is** JavaScript — and `ReconnectModal.razor.js` is
    unmodified `dotnet new blazor` scaffolding. A `*.js` file was never itself a "violation," and now
    there is no violation to flag at all.)*
- **Datastore**: **PostgreSQL 17**, local, via Docker Compose (**host port 5433** → container 5432,
  db `canvas`, `postgres`/`postgres`, named volume). Schema via EF Core migrations applied on startup.
  (D-01, D-27, D-42, D-58)
  > **Port amended in BC-01, user-approved.** D-27/D-58 originally specified host port 5432; a native
  > `postgresql-x64-18` Windows service permanently occupies it on this machine, so compose publishes
  > `"5433:5432"`. See `docs/DECISIONS.md` § "Docker Compose (D-27)". `CanvasDbContextFactory` throws
  > rather than guessing a connection string, so `dotnet ef` cannot silently hit the wrong server.
- **Two tables only** — `users` and `figures`. "The canvas" is not an entity in the database; it is
  simply the set of figures belonging to a user. Canonical DDL in
  `.planning/intel/constraints.md` → `CONSTRAINT-schema`. (D-12, D-46)
- **The coordinate constant**: page margin 0, toolbar exactly **48px**, canvas immediately below at
  document position (0, 48), **no CSS border on the SVG**. `canvasX = PageX`, `canvasY = PageY − 48`.
  Every coordinate in the app flows through this. (D-43, D-18)
- **1472 × 828, inclusive bounds** — `0..1472 × 0..828` *(v1.1; was 1280 × 720)*. **The size may
  GROW but must never SHRINK** — enlarging keeps every stored figure valid (v1.1 did exactly this),
  shrinking would orphan figures off the surface; stored
  coordinates are only meaningful relative to it. (D-19, D-36)
- **Security: none, deliberately.** Passwords are stored and compared in **plaintext**. Acceptable
  ONLY because this is a throwaway learning project. Never real credentials; never deployed to the
  public internet as-is. **Do not "fix" this without an explicit new decision — it is locked.** (D-08)

## Key Decisions

**All 58 decisions below are LOCKED.** They were established through an extensive scoping
conversation and survived two hostile audits. They are **not open questions**. Do not re-litigate,
re-ask, or "improve" them. Full text: `.planning/intel/decisions.md`; original: `docs/DECISIONS.md`.

<decisions status="locked" source="docs/DECISIONS.md" count="58">

### Product scope
| ID | Decision |
|----|----------|
| D-01 | **Datastore: PostgreSQL.** Not negotiable. |
| D-02 | **UI framework: Blazor.** Not negotiable. |
| D-03 | **One canvas per user.** No canvas list, no "new canvas", no naming, no switching. |
| D-04 | **Feature set: draw, drag, delete.** Exactly three verbs. Everything else is out. |
| D-05 | **Four figure types: line, rectangle, circle, triangle.** They do **not** share one draw interaction — line/rectangle/triangle are corner-to-corner; the circle is centre-out (D-13). Storage is uniform (D-22). *(as amended by D-13)* |

### Architecture and hosting
| ID | Decision |
|----|----------|
| D-06 | **SVG, not HTML5 `<canvas>`.** Figures are C# objects rendered as SVG DOM elements — each a real element with its own click handler. Hit-testing and redraw come free — **materially less code than `<canvas>`** *(v1.1: the "no JavaScript" perk phrasing was removed; the SVG choice stands on its own merits)*. |
| D-07 | **Blazor Server (InteractiveServer), one project, no Web API.** Components hit EF Core directly. **There IS HTTP code to write** (login/logout) and the app runs **two render modes** — see D-34. Accepted cost: every pointer-move during a drag is a SignalR round-trip. *(as corrected by D-34)* |
| D-27 | **Local PostgreSQL via Docker Compose.** Affects nothing about app design. |
| D-28 | **.NET 10.** Verified present. No design consequences. |
| D-42 | **Schema via EF Core migrations, applied on startup.** C# model is the source of truth. **Must be configured explicitly or they silently will not exist:** the CHECK constraints (`HasCheckConstraint` in `OnModelCreating`) and the `COMMENT ON TABLE`. |
| D-49 | **One app project + one narrow test project.** Tests cover exactly the three **silent** failure modes: clamp maths (D-36), circle inscribed-square round-trip (D-22), line-normalisation landmine (D-41). |

### Identity, session, routes
| ID | Decision |
|----|----------|
| D-08 | **Username + plaintext password, no auth.** No hashing, no salting. Exists only to answer "whose canvas do I load?". **Locked — do not "fix".** |
| D-17 | **One login form; an unknown username creates the account.** Wrong password → error. **No Register page.** Plain string equality against the plaintext column. |
| D-25 | **Logout button.** Earns its place as a testability feature: it is the only way to verify one-canvas-per-user actually isolates users. |
| D-26 | **Session cookie, no expiry.** F5 keeps you logged in (essential — F5 is the official fix for a stale tab, D-10). Close the browser → logged out. |
| D-34 | **Login is a static-SSR page + cookie-auth middleware.** An InteractiveServer component **cannot set a cookie**. `/login` renders static SSR and POSTs; the handler validates, `SignInAsync`, redirects. Cookie middleware in `Program.cs`. **Not** ASP.NET Core Identity. |
| D-44 | **Usernames are case-insensitive.** Stored lowercased, trimmed, non-empty, UNIQUE. Postgres's default collation is case-sensitive — doing nothing would silently drop a user into a *different, empty canvas* ("my work vanished"). |
| D-51 | **`user_id` is a cookie claim; the circuit reads it — no DB lookup on page load.** Routes: `/login` (static SSR), `/` (InteractiveServer, `[Authorize]`), `POST /logout` (endpoint). Unauthenticated `/` → redirect to `/login`. |
| D-56 | **Logout sits right-aligned in the 48px toolbar strip**, separated from the six tool buttons. A small HTML form posting to `POST /logout` — not an interactive button (clearing a cookie needs an HTTP round-trip). **Toolbar height stays 48px.** |

### Canvas, coordinates, geometry
| ID | Decision |
|----|----------|
| D-13 | **Circle draw-time geometry: centre + radius.** The press point is the centre; drag distance sets the radius. Always a true circle, never an oval. |
| D-18 | **Fixed-size canvas, 1:1** *(v1.1: chosen for MVP simplicity — "no JavaScript" motivation removed)*. One canvas unit = one CSS pixel, on every screen. Does not scale to the window. A fixed **aspect ratio** is still mandatory geometry (else circles render as ovals). `canvasX = PageX`, `canvasY = PageY − toolbarHeight`. **LANDMINE: `PageX/PageY`, never `OffsetX/OffsetY`.** Do not centre the canvas with `margin: auto`. |
| D-19 | **Canvas is 1472 × 828** *(v1.1; was 1280 × 720)*. The size **may grow but must never shrink** — enlarging keeps every stored figure valid (v1.1 did this, no migration); shrinking orphans figures. Coordinates are only meaningful relative to it. |
| D-20 | **Coordinates are integers.** Beyond tidiness: integers can be compared for exact equality in a database CHECK — so geometric invariants are *enforced by the schema*, not trusted in code. |
| D-21 | **Triangle: derived from a drag, 2 points.** Apex top-centre, base along the bottom. Accepted cost: every triangle is isosceles and points upward. |
| D-22 | **Geometry storage: four integers, always — and they ARE the bounding box. A circle is stored as its INSCRIBED SQUARE.** `r = (x2−x1)/2`, `cx = x1+r`, `cy = y1+r`. Drawing is unchanged (centre-out); **only storage is a square**. A move is a uniform translation, so `d` cancels **algebraically** — a circle dragged ten thousand times has a bit-identical radius. Enforced by `CHECK (type <> 'circle' OR (x2-x1 = y2-y1 AND x2 > x1 AND (x2-x1) % 2 = 0))`. **Why: generic clamping reads the raw columns — there is genuinely no type dispatch left in the move or the clamp.** *(REVISED — the original centre+rim encoding is REVERSED and dead)* |
| D-24 | **Figures stop at the canvas edge** and **slide along it** rather than sticking. Clamp applied live on every pointer-move; the clamped position is what persists. **This decision is what forced the reversal of D-22.** |
| D-29 | **Drawing also stops at the canvas edge.** One rule for the whole app: figures live entirely inside the canvas, always. |
| D-36 | **The clamp — exact formula, INCLUSIVE bounds (`0..1472 × 0..828`** *(v1.1; was `0..1280 × 0..720`)*`).** Clamp the *delta*, then translate all four uniformly. **Per-axis independence is the point** — `dx'` never reads `y`, so a figure pinned to the right edge still slides up and down. **Ordering: clamp → render → broadcast.** The one genuinely type-specific rule: the circle draw-clamp `r = min(round(distance), cx, cy, W−cx, H−cy)` — known consequence: **pressing near an edge forces a tiny circle**. No canvas-bounds CHECKs in the DB. |
| D-41 | **Normalise on write — but NOT the same way for every shape.** Rectangle/triangle/circle → sort the axes independently. **LANDMINE — Line → swap the WHOLE POINT PAIR, never sort axes independently** ((0,100)→(100,0) would become the opposite diagonal). Consequence: `x1 ≤ x2` for every shape, but `y1 ≤ y2` **only** for rectangle/triangle/circle — which is why the clamp keeps its min/max bounding-box computation. |

### Interaction
| ID | Decision |
|----|----------|
| D-15 | **Click to select** (visible highlight). Selection is **local UI state only** — never persisted, never broadcast. *(Its Delete-key half is superseded by D-33.)* |
| D-16 + D-30 + D-33 | **The toolbar is SIX buttons:** `[pointer] [line] [rectangle] [circle] [triangle] [delete]`. Click to arm; the armed button stays visibly active. Logout sits right-aligned in the same strip but is not one of the six (D-56). |
| D-30 | **Selection is a pointer tool.** Pointer armed → click selects, drag moves. Shape armed → dragging always draws **even on top of existing figures**. Accepted cost: the app has modes. This is what makes it possible to draw a small circle *inside* a big rectangle. |
| D-31 | **Selection appearance/behaviour** *(v1.1 amended)*. Indicator: a thin **blue + white dashed trace on the figure's own outline**, topmost, `pointer-events:none` (was a red outline). **Lifecycle:** tool **stays armed** after a draw; the drawn figure is **selected**; **at most one selected at a time**; deselect on canvas-outside-figure, arming a tool, or a toolbar press **except Delete**. **Pointer armed on page load.** Overlapping figures: a click hits the topmost = drawn last (free from the DOM). Selection is local-only, never broadcast. |
| D-32 | **Two usability costs, accepted deliberately.** (1) The min-size guard was not raised to ~5px, so a stray 1–2px drag creates a tiny figure. Annoying, not dangerous. (2) **Lines have no widened hit area** — selecting one means clicking within ~a pixel of it (mitigated only by the 2px stroke). Additive; can be added any time. |
| D-33 | **Delete is a toolbar button, not the Delete key** *(v1.1: motivation = MVP simplicity + unambiguous behaviour)*. A pure-Blazor `@onkeydown` fires only on a focused element, so a keyboard Delete breaks the moment focus moves to a toolbar button — the toolbar button avoids that. *(A Delete-key shortcut could now be added later; the no-JS reason is gone.)* |
| D-35 | **Live preview while drawing.** The shape is visible under the cursor as you drag it out. The preview is **local only — never broadcast.** |
| D-37 | **Drag termination** *(v1.1: exists to prevent an unexpected hanging/stranded drag, not to avoid JS)*. (1) **`pointerleave` on the drag surface commits the drag** at its current clamped position; (2) **the `Buttons` guard** — on any `pointermove` while dragging, if the primary button is already up, commit and end (the Alt-Tab case). Coherent because of D-36: by the time the cursor leaves, the figure is *already pinned at the edge* — nothing jumps. Put the handlers on a **page-spanning wrapper**, and use `pointerleave`, **not `pointerout`**. *(A `setPointerCapture` "keep grabbed anywhere" upgrade could now be added later.)* |
| D-48 | **Click vs drag: a 3-pixel threshold.** < 3px → click (select; **no database write**). ≥ 3px → drag (persisted on drop). **Starting a drag also selects**, and it **stays selected after the drop** — so you can drag something and immediately delete it. |
| D-50 | **The minimum-size guard is PER-TYPE.** Line → rejected only when both endpoints are identical (**horizontal and vertical lines are legal and must work**). Rectangle/triangle/circle → rejected when width **or** height is zero. Mirrors the CHECK constraints exactly, so **the app can never write a row the database would refuse.** A rejected draw **fails silently**. *(retracts D-23's "one shared guard")* |
| D-57 | **Leaving the surface mid-DRAW commits the figure** at its clamped preview position. One consistent rule for both gestures. **Consequence: no way to abandon a draw once started** — abandoning is simply out of MVP scope *(v1.1: not "impossible because of no JS")*. Change your mind → draw it, then delete it. *(Escape-to-cancel could now be added later.)* |

### Persistence and sync
| ID | Decision |
|----|----------|
| D-09 | **Persistence is immediate and per-operation. NO Save button.** Draw → `INSERT`; drag (on drop) → `UPDATE`; delete → `DELETE`. Load-bearing: each figure is its own row, so two tabs writing different figures cannot touch each other. The database is always the merged truth. |
| D-10 | **Mandatory: handle the zero-row UPDATE/DELETE (staleness guard).** Use `ExecuteUpdateAsync`/`ExecuteDeleteAsync` and check the affected-row count. Zero rows on UPDATE ⇒ the figure is gone ⇒ **silently remove it from that tab's view.** Not an error — expected staleness. Needed even with live sync (a reconnected tab resumes its *in-memory* state and never re-reads the DB). **Manual fallback: F5.** |
| D-11 | **Multi-tab: live sync with real-time drag glide.** Dragging in tab A makes the figure **glide** in tab B — not jump on release. **The one-mouse premise** deletes all locking/conflict/CRDT thinking. Mechanism: a **DI singleton notifier keyed by `user_id`**, delta payloads (D-53), pushed over Blazor's existing per-tab SignalR channel. **Broadcasting ≠ persisting: Postgres sees exactly ONE UPDATE per drag, on drop.** Irreducible core (all mandatory): unsubscribe in `Dispose()` · `InvokeAsync(StateHasChanged)` · echo filter · **mid-drag discard ALL incoming** (D-54) · **`move` is UPDATE-ONLY** (D-40) · 50ms throttle with guaranteed trailing edge (D-47) · key by `user_id` · clamp→render→broadcast · a zero-row UPDATE broadcasts a delete. *(as amended by D-40/D-47/D-53/D-54)* |
| D-39 | **`figures.id` is a sequential integer, and it IS the z-order.** Load `ORDER BY id`. Within a session "topmost = drawn last" comes free from the DOM — **after F5 it does not**, so creation order must be reconstructed. Accepted cost: the id does not exist until the INSERT completes, so drawing is strictly **insert → get id → broadcast**. |
| D-40 | **Killing the resurrection hole.** (1) **A `move` broadcast may only UPDATE — never INSERT.** Unknown figure → ignore the message entirely. (2) **A zero-row UPDATE broadcasts a delete.** Corrected rule: apply is idempotent, and move is update-only. |
| D-45 | **Database errors: a friendly message, and the app stays alive.** "Could not save — is the database running?" The circuit does not crash. Honest consequence: the picture no longer matches the database — handled by D-52. |
| D-46 | **`type` is `text` + CHECK. No `created_at`.** Not a free choice: D-22's and D-41's CHECKs are written as `type <> 'circle'` — a PG enum or int-mapped C# enum would **silently invalidate them**. `created_at` is dropped; the sequential `id` is the z-order. |
| D-47 | **Drag broadcast throttle: 50 ms, trailing edge GUARANTEED.** The final position is always sent before the drop — otherwise the glide stops short and the other monitor twitches at the end of every drag. A **throttle**, not a debounce. |
| D-52 | **Save-failure policy: retry transient, then roll back everywhere.** Retry ≤ 2 more times **only if transient**. **Never retry** validation errors, CHECK violations, a missing figure, or a zero-row UPDATE. On final failure: **broadcast `rollback`** with the original coordinates → restore locally → modal ("The change could not be saved. The canvas will be reloaded from the database.") → reload from Postgres on OK. **The original coordinates are retained for the entire drag** precisely so this is possible. Forced because the glide broadcasts already went out — without rollback, **every open screen is lying**. |
| D-53 | **The broadcast message contract (canonical).** Kinds: `draw` (insert-or-update; the only kind that may create a figure; sent *after* the INSERT), `move` (**UPDATE ONLY — never insert**; unknown figure → ignore entirely; **carries no `type`**), `delete` (idempotent), `rollback` (update-only, like `move`). **There is no `drop` kind** — a drag's final position is the last `move`. `sender` is a **per-circuit GUID** for the echo filter. **Draw previews are NOT broadcast.** |
| D-54 | **Mid-drag, a tab ignores ALL incoming broadcasts** — `if (_dragging) return;` — not merely those about the figure being dragged. Safe because of the one-mouse premise. Explicitly **rejected**: the narrow "ignore only messages about the dragged figure". Accepted cost, stated honestly: a message from a second device mid-drag is lost permanently until F5. |

### Schema and data
| ID | Decision |
|----|----------|
| D-12 | **Two tables. No `canvases` table.** `users` and `figures`. "The canvas" is not an entity in the database — it is the set of figures belonging to a user. No canvas row, no canvas id, no join. *(Only the two-table principle is normative; D-12's DDL sketch is stale — the authoritative DDL is `CONSTRAINT-schema` in `.planning/intel/constraints.md`.)* |

### Appearance
| ID | Decision |
|----|----------|
| D-14 | **One fixed style. No colours.** No style columns in the database, no colour picker in the UI. |
| D-38 | **White fill, black outline.** **The fill is load-bearing:** SVG does **not** register clicks inside an *unfilled* shape. Had figures been wireframes, D-30's entire rationale would have collapsed. Accepted cost: overlapping figures fully occlude each other. |
| D-43 | **Page layout: a 48px toolbar, no canvas border.** Margin 0; toolbar exactly 48px at the top; canvas immediately below at document position (0, 48), anchored top-left. **No CSS border on the SVG** — a border shifts the interior by its own width, making the mapping `PageY − 48 − 1`: one more constant that can be silently forgotten. **`canvasX = PageX`, `canvasY = PageY − 48`.** |
| D-55 | **Page background: light grey.** **Not cosmetic:** D-43 gives the canvas no border, so contrast with the page is the *only* thing that makes the canvas boundary visible. On a white default, "figures stop at the edge" would look like an inexplicable bug. |
| D-58 | **The remaining constants.** Figure outline **black, 2px**; fill **white**; **selection indicator: ~1px blue+white dashed trace on the figure's own outline, topmost** *(v1.1; was red 2px)*; page background light grey; canvas white **1472×828** *(v1.1; was 1280×720)*, no border; toolbar 48px. 2px (not 1px) is deliberate for figure outlines — the stroke is the only click target a line has. **The Delete button is greyed out and unclickable when nothing is selected.** **Passwords must be non-empty.** Docker Compose: Postgres **17**, port **5432**, db **`canvas`**, **`postgres`/`postgres`**, **named volume**. *(Port amended in BC-01, user-approved: host **5433** → container 5432; a native `postgresql-x64-18` service owns 5432 on this machine. Intent untouched — see Constraints.)* |

### Dead versions — never re-introduce
D-05 (all four shapes share one draw gesture) · D-07 ("no HTTP code to write") · D-11 ("idempotent
upsert" — **this was a BUG**; it resurrects deleted figures) · D-12's DDL sketch (still shows the
dropped `created_at`) · D-15 (the Delete key) · D-16 ("four" buttons) · **D-22 (centre + rim point —
REVERSED)** · D-23 ("one shared guard") · D-30 ("five" buttons) · D-32's shared-guard framing ·
D-45's undecided "and/or".
Full history: `.planning/intel/decisions.md` § "Superseded history".

</decisions>

### One resolved contradiction — CLOSED

`INGEST-CONFLICTS.md` raised **one WARNING**: D-11's own checklist summarised D-54 *backwards*
(claiming D-54 narrowed the mid-drag filter to only the dragged figure). **D-54 decides the
opposite** and explicitly lists the narrow filter under *Rejected*. Synthesis applied **D-54 wins**:
mid-drag, a tab discards **ALL** incoming broadcasts.

**Closed.** The user confirmed D-54 as the intended rule, D-11's item 4 was corrected at source in
`docs/DECISIONS.md`, and BC-05-03 built the blanket discard (`if (_dragging) return;`). The v1.0
integration audit re-confirmed it in `Home.razor`'s `HandleRemoteMessage`. The rejected narrow
filter was never built.

## Current State

**v1.1 feature work is complete.** Phases BC-06 and BC-07 delivered the 1472 x 828 canvas and the
local selection lifecycle with its topmost blue-and-white dashed trace. Phase BC-08 then verified the
permissive JavaScript policy amendment across the active decision, project, and derived-constraint
records with no runtime change; the solution builds and all 405 tests pass.

**v1.1 milestone opened 2026-07-20** via `/gsd-new-milestone`: the four changes are finalised as
REQ-IDs **CANV-03, SEL-01, SEL-02, ARCH-01**, and the phase roadmap continues numbering from BC-06.
See `.planning/REQUIREMENTS.md` and `.planning/ROADMAP.md`.

**v1.0 shipped 2026-07-17.** The definition of done was met — verified both by an end-to-end code
trace (v1.0 milestone audit) and by live human verification on two real screens (BC-05-05).

- **Delivered:** 5 phases, 23 plans, all 15 v1 requirements validated.
- **Codebase:** ~2,500 LOC application (C#/Razor/CSS) + ~2,000 LOC tests; 405 tests passing.
- **Stack:** .NET 10 Blazor Server, EF Core/Npgsql, PostgreSQL 17 in Docker Compose, SVG rendering.
  *(v1.0 shipped with zero hand-authored JavaScript; that self-imposed rule was retired in v1.1.)*
- **Verification:** all 5 phases `passed`; cross-phase integration audited clean, with the
  highest-risk seam (DATA-03's zero-row-UPDATE → delete-broadcast handoff, built across two phases)
  confirmed correctly wired.
- **Known tech debt:** ~11 low-severity items from `01-REVIEW.md` (WR-03…WR-07, WR-09, IN-01…IN-05).
  None blocks a requirement. WR-01 and WR-08 are locked-by-design (D-36, D-08), not debt.
- **Next milestone:** **v1.1 feature phases complete** (canvas 1472×828 · selection UX + restyle ·
  permissive JavaScript policy). **v1.2 scoped** (new figures + dynamic toolbar) in
  `.planning/backlog/v1.2-figures-and-toolbar.md`.
  The "terminal MinVP" framing no longer applies — the project has resumed.

## Evolution

This document evolves at phase transitions and milestone boundaries.

**After each phase transition** (via `/gsd-transition`):
1. Requirements invalidated? → Move to Out of Scope with reason
2. Requirements validated? → Move to Validated with phase reference
3. New requirements emerged? → Add to Active
4. Decisions to log? → Add to Key Decisions
5. "What This Is" still accurate? → Update if drifted

**After each milestone** (via `/gsd-complete-milestone`):
1. Full review of all sections
2. Core Value check — still the right priority?
3. Audit Out of Scope — reasons still valid?
4. Update Context with current state

---
*Last updated: 2026-07-21 — Phase BC-08 validated ARCH-01: the retired policy is removed from active
records, all 405 tests pass, and no runtime or JavaScript change was introduced.
(Prev: 2026-07-21, Phase BC-07 validated SEL-01/SEL-02.)*
