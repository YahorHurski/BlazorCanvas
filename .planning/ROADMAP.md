# Roadmap: BlazorCanvas

## Overview

Five phases along a single dependency spine, each one demonstrable on its own, ending exactly at the
definition of done.

We start at the bottom, because that is where the risk is. The ADR audit found this project
**airtight wherever the human was in the room** (data model, sync semantics, geometry, scope) and
**silent wherever the framework was in the room** (cookies, HTTP, pointer capture, schema creation,
layout constants, error paths). So Phase 1 builds the database, the schema that *machine-enforces*
the geometry laws, and the pure geometry maths — with the three mandated tests that guard the three
*silent* failure modes. Phase 2 gets a user identified and keeps them logged in across tabs and F5.
Phase 3 puts a real canvas on the screen and lets the user draw all four shapes, persisted. Phase 4
completes the three verbs — select, drag (clamped at the edge), delete. Phase 5 is the hard one and
the one the success metric is built around: the notifier, the real-time drag glide, and the
consistency rules that keep every open screen honest.

**Definition of done** (the gate on Phase 5):

> "I can log in, draw all four shapes, drag and delete them, open the app on a second monitor, and
> watch a figure GLIDE in real time as I drag it on the first — with everything surviving a refresh."

**Non-negotiables that shape every phase:** no JavaScript anywhere · no Save button (every operation
persists immediately) · 1280 × 720 with inclusive bounds · `canvasY = PageY − 48` · plaintext
passwords (locked, deliberate) · **all 58 ADR decisions are LOCKED and must not be re-litigated.**

## Phases

**Phase Numbering:**

- Integer phases (1, 2, 3): Planned milestone work
- Decimal phases (2.1, 2.2): Urgent insertions (marked with INSERTED)

- [x] **Phase 1: Database, Schema & Geometry Core** - Postgres in Docker, the two-table schema whose CHECKs enforce the geometry, and the tested clamp/normalise/circle maths (completed 2026-07-14)
- [x] **Phase 2: Login, Session & Logout** - Static-SSR login, cookie auth with the `user_id` claim, and an authenticated shell that survives F5 (completed 2026-07-15)
- [ ] **Phase 3: The Canvas & Drawing** - The 1280×720 SVG at (0,48), the six-button toolbar, and drawing all four shapes — persisted, and back after a refresh
- [ ] **Phase 4: Select, Drag & Delete** - The three verbs complete: 3px click-vs-drag, edge clamping that slides, and a Delete button
- [ ] **Phase 5: Live Cross-Tab Sync** - The notifier, the real-time drag glide, and the consistency rules that stop any screen from lying

## Phase Details

### Phase 1: Database, Schema & Geometry Core

**Goal**: A running PostgreSQL holds a schema that *machine-enforces* the geometry laws, and the pure geometry maths those laws mirror is written and proven. The three silent failure modes are guarded before any UI exists to hide them.
**Depends on**: Nothing (first phase)
**Requirements**: DATA-02, TEST-01
**Success Criteria** (what must be TRUE):

  1. `docker compose up` starts PostgreSQL 17 on port 5432 with database `canvas` and a **named volume** — rows survive a container restart.
  2. Running the app creates **exactly two tables** (`users`, `figures`) via EF Core migrations applied automatically at startup — no `canvases` table, no `created_at` — carrying all three CHECK constraints, the `user_id` index, and the `COMMENT ON TABLE` that documents the circle convention.
  3. **The database itself refuses an illegal row**: a non-square or odd-sided circle, a zero-area rectangle, or a zero-length line is rejected by a CHECK constraint, not by application code.
  4. The three mandated tests pass: **clamp maths** (per-axis independence — a figure pinned to the right edge still moves vertically; inclusive bounds `0..1280 × 0..720`; the circle draw-clamp), **circle inscribed-square round-trip** (centre and radius come back exact after store + reload, and after translation), and **line normalisation** (an up-and-right diagonal does not come back as the opposite diagonal).

**Plans**: 6/6 plans complete

Plans:
**Wave 1**

- [x] 01-01-PLAN.md — PostgreSQL 17 in Docker Compose with a named volume, and the .NET 10 two-project solution (wave 1)

**Wave 2** *(blocked on Wave 1 completion)*

- [x] 01-02-PLAN.md — The pure C# geometry core and the three mandated tests: normalise, clamp, circle inscribed-square, per-type min-size guard (wave 2)
- [x] 01-03-PLAN.md — EF Core schema and migrations applied at startup: two tables, three CHECK constraints, the index and the COMMENT (wave 2)

**Wave 3** *(blocked on Wave 2 completion)*

- [x] 01-04-PLAN.md — Prove the database itself refuses an illegal row, and that the min-size guard mirrors the CHECKs exactly (wave 3)

**Gap Closure** *(post-verification — closes the 3 Critical blockers in 01-VERIFICATION.md; run via `/gsd-execute-phase 1 --gaps-only`)*

- [x] 01-05-PLAN.md — Harden the clamp/circle-encoding maths: floor ClampDrawRadius at 0 + clamp its centre (CR-01), guard ClampDelta's lo>hi inversion (CR-02), with regression tests (wave 1)
- [x] 01-06-PLAN.md — Make the design-time DbContext factory fail loudly instead of silently targeting the wrong PostgreSQL server on port 5432 (CR-03) (wave 1)

**Notes for planning:**

- The authoritative DDL is `CONSTRAINT-schema` in `.planning/intel/constraints.md`. **Never implement from D-12's sketch** — it still shows the dropped `created_at`.
- `type` must be **`text`** + CHECK (D-46). A PostgreSQL enum or an int-mapped C# enum would *silently invalidate* the CHECKs, which are written as `type <> 'circle'`.
- EF Core will **not** emit the CHECKs or the COMMENT on its own — configure them explicitly via `HasCheckConstraint` in `OnModelCreating` (D-42). Every geometric guarantee rests on them.
- The geometry core (normalise, clamp, circle encode/decode) is **pure C#, no Blazor dependency**. That is what makes it testable here, and it is what Phases 3–5 call.
- The per-type min-size guard (D-50) mirrors the CHECKs *exactly*, so **the app can never write a row the database would refuse**. Build them together; they are two halves of one rule.

---

### Phase 2: Login, Session & Logout

**Goal**: A user can identify themselves, and the app knows whose canvas to load — across every tab in the browser and across F5.
**Depends on**: Phase 1
**Requirements**: AUTH-01, AUTH-02, AUTH-03
**Success Criteria** (what must be TRUE):

  1. A **new** username at `/login` creates the account and lands on the canvas page; an existing username with the correct password lands on the same account; a wrong password shows an error. **There is no Register page.**
  2. "Egor" and "egor" are the **same account** (trimmed, lowercased, UNIQUE); an empty username or an empty password is rejected.
  3. After logging in, **F5 keeps the user logged in**, and a second tab in the same browser is already authenticated without logging in again. Closing the browser logs them out.
  4. An unauthenticated visit to `/` **redirects to `/login`**; the authenticated page reads `user_id` straight from the cookie claim with **no database lookup on page load**.
  5. A **right-aligned Logout form** in the 48px toolbar strip posts to `POST /logout`, clears the cookie, and returns to the login form — after which a different user can log in and land on their own, separate canvas.

**Plans**: 3/3 plans complete

- [x] 02-01-PLAN.md
- [x] 02-02-PLAN.md
- [x] 02-03-PLAN.md

**UI hint**: yes

**Notes for planning:**

- **This is the framework seam the ADR was originally silent about.** An InteractiveServer component **cannot set a cookie** — the HTTP response has already begun. `/login` is **static SSR** and its form POSTs; `POST /logout` is an endpoint; the canvas at `/` is InteractiveServer and `[Authorize]`. The app genuinely runs **two render modes** (D-34, D-51).
- This is **cookie plumbing, not ASP.NET Core Identity** — D-08 rejects Identity.
- D-44 (case-insensitivity) is not tidiness: Postgres's default collation is case-sensitive, so doing nothing would silently drop a returning user into a *different, empty canvas*, which reads as **"my work vanished"**.
- This phase builds the **48px toolbar strip** with Logout in it. Phase 3 fills the same strip with the six tool buttons. The 48px height is the constant every coordinate in the app depends on — it does not change (D-43, D-56).
- Visual constants are **locked** by D-58/D-43/D-55. A UI step here should *transcribe* them, not invent.

---

### Phase 3: The Canvas & Drawing

**Goal**: A logged-in user sees their own canvas and can draw all four shapes on it — and the drawing is still there after a refresh.
**Depends on**: Phase 2
**Requirements**: DATA-01, CANV-01, CANV-02, FIG-01
**Success Criteria** (what must be TRUE):

  1. `/` shows a **white 1280 × 720 SVG canvas anchored at document position (0, 48)** below the 48px toolbar, on a **light-grey page**, with **no CSS border**. One canvas unit is one CSS pixel on every screen, and the canvas does not rescale with the window.
  2. The toolbar shows **exactly six buttons** — pointer, line, rectangle, circle, triangle, delete — with **pointer armed on page load** and the armed button visibly active. Logout stays right-aligned and separate. The Delete button is greyed out (nothing is selectable yet).
  3. With a shape armed, dragging on the canvas **previews it live under the cursor** and commits it on release: line/rectangle/triangle **corner-to-corner**, circle **centre-out** (press = centre, drag = radius), triangle apex **top-centre**. Dragging on top of an existing figure **draws a new figure** rather than moving it — a small circle can be drawn *inside* a big rectangle.
  4. **Drawing stops at the canvas edge**: the shape stops growing at the boundary while the cursor keeps moving, and a circle **never renders as an oval**. A zero-size drag creates nothing, **silently** — but a **horizontal or vertical line still draws**.
  5. Every drawn figure is **INSERTed immediately** (there is no Save button), and after **F5** the whole canvas reloads in the same drawing order with the same overlap/occlusion. A second user logging in sees **only their own figures**.

**Plans**: TBD
**UI hint**: yes

**Notes for planning:**

- **Landmine: `PageX`/`PageY`, never `OffsetX`/`OffsetY`.** `OffsetX/Y` is relative to the *event target*, and every drag and every selection begins *on a figure*. `canvasX = PageX`, `canvasY = PageY − 48`.
- **No CSS border on the SVG** — a border shifts the interior by its own width and turns the mapping into `PageY − 48 − 1`: the classic "the shape appears slightly off from where I clicked" bug.
- The white **fill is load-bearing** (D-38): SVG does **not** register clicks inside an *unfilled* shape. Wireframe figures would make Phase 4's "press inside a rectangle grabs it" simply untrue.
- Drawing is strictly **insert → get id → broadcast** (D-39) — the `id` does not exist until the INSERT completes, so the Phase 5 broadcast cannot be fired optimistically. Build the write path in that order now.
- Leaving the surface mid-draw **commits** the figure at its clamped preview position (D-57). There is deliberately **no way to abandon a draw** — change your mind, draw it, then delete it.
- Reuse the Phase 1 geometry core for normalisation, the clamp, and the circle encoding. Do not re-implement.

---

### Phase 4: Select, Drag & Delete

**Goal**: The three verbs are complete — a figure can be picked up, moved anywhere inside the canvas, and removed. On one screen, the app is finished.
**Depends on**: Phase 3
**Requirements**: FIG-02, FIG-03, FIG-04
**Success Criteria** (what must be TRUE):

  1. With pointer armed, clicking a figure **selects** it (red 2px outline; all others stay black on white); **clicking empty canvas deselects**; a click on overlapping figures hits the **topmost** — the one drawn last.
  2. Moving **< 3 px** before release is a **click** (select only, **no database write**); **≥ 3 px** is a **drag**, which also selects, and the figure **stays selected after the drop** — so it can be dragged and immediately deleted.
  3. A dragged figure **stops at the canvas edge and slides along it** — pinned against the right edge it still moves freely up and down — and it lands exactly where it was released. Postgres sees **exactly one UPDATE per drag**, on drop.
  4. Releasing the pointer **outside the window**, or **Alt-Tabbing away mid-drag**, commits the figure at its clamped position instead of leaving it stuck to the cursor. Nothing jumps.
  5. The **Delete button is greyed out until something is selected**; with a figure selected, clicking it removes the figure and its row. **There is no Delete-key handler.** After F5, every move and every deletion is still there.

**Plans**: TBD
**UI hint**: yes

**Notes for planning:**

- **The landmine of this phase: never clamp coordinates individually.** Clamp the movement **delta**, then translate all four uniformly. Clamping `x2` alone *resizes* the figure instead of moving it — and for rectangles it fails **silently** (for circles it now fails loudly against the CHECK, thanks to the D-22 reversal).
- **Per-axis independence is the whole point** of the clamp: `dx'` never reads `y`. That is what makes a figure *slide along* the wall instead of sticking to it.
- **No JavaScript, so no `setPointerCapture`** (D-37). Two markup-only rules: (1) `pointerleave` on the drag surface **commits** the drag; (2) the **`Buttons` guard** — on any `pointermove` while dragging, if the primary button is already up, commit and end. Put the handlers on a **page-spanning wrapper**, and use `pointerleave`, **not `pointerout`** (`pointerout` also fires when the cursor moves onto a child figure).
- **Retain the figure's original coordinates for the entire drag.** Phase 5's rollback (D-52) is impossible without them — decide this now, not later.
- The zero-row UPDATE guard (D-10) belongs to the write path built here, but its *broadcast a delete* half lands in Phase 5 (DATA-03). Use `ExecuteUpdateAsync`/`ExecuteDeleteAsync` and check the affected-row count from the start.
- Selection is **local UI state only** — never persisted, never broadcast. Your other monitor does not show what you have selected.

---

### Phase 5: Live Cross-Tab Sync

**Goal**: The user's other tabs mirror the canvas in real time — a figure **glides** on the second monitor as it is dragged on the first — and no open screen is ever left showing something the database does not hold.
**Depends on**: Phase 4
**Requirements**: SYNC-01, DATA-03, DATA-04
**Success Criteria** (what must be TRUE):

  1. With the same user open in **two tabs**, drawing in one makes the figure appear in the other; deleting removes it from both. Closing a tab leaves the others working (no leaked subscribers, no `ObjectDisposedException`).
  2. **Dragging a figure in tab A makes it GLIDE in tab B in real time** — throttled to 50 ms, with the final position always arriving, so the glide never stops short. It does **not** merely jump on release. Meanwhile **Postgres sees exactly one UPDATE for the whole drag**.
  3. A tab **never reacts to its own broadcast**, **never shows another tab's draw preview**, and **discards all incoming broadcasts while its own drag is in progress**. A `move` for a figure a tab does not know is **ignored entirely** — a deleted figure is never resurrected.
  4. Dragging a figure that another tab already deleted **silently removes it** from this tab's view, and any tab still showing that ghost drops it too. No error, no prompt, no merge.
  5. If a save fails after retries, **every tab is restored to the figure's original coordinates**, and the user gets one modal — *"The change could not be saved. The canvas will be reloaded from the database."* — which reloads from PostgreSQL on OK. The app stays alive; the circuit does not crash.

**Plans**: TBD

**Notes for planning:**

- **This phase is the definition of done.** It cannot be deferred, trimmed, or "phase 6'd".
- **The nine irreducible sync rules (`CONSTRAINT-sync-core`) are ALL mandatory** — none is optional, and each one exists because its absence produced a specific bug. Read them before planning.
- **`move` is UPDATE-ONLY, never insert.** The original D-11 said "idempotent upsert" — **that was a bug**: an upsert inserts when the row is absent, so a stale tab's drag broadcasts can *resurrect* a figure another tab correctly deleted. Only `draw` may ever create a figure (D-40, D-53).
- **Broadcasting ≠ persisting.** Every pointer-move is *already* a SignalR round-trip; the glide re-broadcasts that position through the **in-memory** notifier. Intermediate positions **never** touch Postgres — writing them would be ~100× write amplification.
- **Order is clamp → render → broadcast.** Never broadcast a raw, unclamped position.
- **⚠ One thing to confirm before building (from `INGEST-CONFLICTS.md`, the single WARNING):** D-11's own checklist summarises D-54 *backwards*, claiming it narrowed the mid-drag filter to only the dragged figure. **D-54 decides the opposite** and lists the narrow filter under *Rejected*. Synthesis resolved this as **D-54 wins — mid-drag, discard ALL incoming broadcasts** (`if (_dragging) return;`), and that is what will be built. An implementer following D-11's checklist verbatim would build the rejected filter. One sentence of confirmation closes this.
- Rollback (D-52) is **forced, not defensive**: on a failed drop the glide broadcasts have *already gone out*, so every other tab already shows the new position while the database still holds the old one. Without the rollback broadcast, **every open screen is lying**.
- Retry **only transient** failures (≤ 2 more attempts). **Never** retry validation errors, CHECK violations, a missing figure, or a zero-row UPDATE — the last of these **is not an error at all**, it is expected staleness.

## Progress

**Execution Order:**
Phases execute in numeric order: 1 → 2 → 3 → 4 → 5

| Phase | Plans Complete | Status | Completed |
|-------|----------------|--------|-----------|
| 1. Database, Schema & Geometry Core | 6/6 | Complete   | 2026-07-15 |
| 2. Login, Session & Logout | 3/3 | Complete   | 2026-07-15 |
| 3. The Canvas & Drawing | 0/TBD | Not started | - |
| 4. Select, Drag & Delete | 0/TBD | Not started | - |
| 5. Live Cross-Tab Sync | 0/TBD | Not started | - |

## Requirement Coverage

| Phase | Requirements |
|-------|--------------|
| 1 | DATA-02, TEST-01 |
| 2 | AUTH-01, AUTH-02, AUTH-03 |
| 3 | DATA-01, CANV-01, CANV-02, FIG-01 |
| 4 | FIG-02, FIG-03, FIG-04 |
| 5 | SYNC-01, DATA-03, DATA-04 |

**15 / 15 v1 requirements mapped. No orphans. No duplicates.**

---
*Roadmap created: 2026-07-14 from `docs/DECISIONS.md` (58 locked decisions) via `.planning/intel/`*
