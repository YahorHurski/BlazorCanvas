# Phase 1: Database, Schema & Geometry Core - Context

**Gathered:** 2026-07-14
**Status:** Ready for planning
**Source:** ADR Ingest Express Path (`docs/DECISIONS.md`, via the conflict-resolved projection in `.planning/intel/`)

> **Ingest note.** `docs/DECISIONS.md` is a 58-entry decision *registry*, not a single ADR: entries
> are not in file order past D-32, and several are amended, superseded, or reversed in prose. The raw
> ADR parser cannot honour that supersession (it extracted 5 of 58 entries and misread the status
> index table as a status field). This CONTEXT.md is therefore ingested from `.planning/intel/`,
> which is the already-normalised, conflict-resolved projection of that same document, produced by
> the bootstrap `/gsd-ingest-docs` run. Same decisions — correct supersession.

<domain>
## Phase Boundary

**What this phase delivers:** A running PostgreSQL whose *schema machine-enforces* the geometry
laws, plus the pure geometry maths those laws mirror — written and proven by test. The three silent
failure modes are guarded **before any UI exists to hide them**.

In scope:
1. **Docker Compose** — PostgreSQL 17, port 5432, database `canvas`, **named volume** (rows survive
   a container restart).
2. **EF Core schema + migrations**, applied automatically at startup: exactly two tables (`users`,
   `figures`), all three CHECK constraints, the `user_id` index, and the `COMMENT ON TABLE`.
3. **Pure C# geometry core** — normalise, clamp, circle encode/decode, per-type min-size guard. No
   Blazor dependency; this is what makes it testable here and it is what Phases 3–5 call.
4. **The three mandated tests** — clamp maths, circle inscribed-square round-trip, line normalisation.

**There is no UI in this phase.** No SVG, no toolbar, no components, no login flow. The `users`
table is *created* here; the login that writes to it is Phase 2.

</domain>

<decisions>
## Implementation Decisions

All decisions below are **Locked** in `docs/DECISIONS.md`. IDs are the tracked contract — each must
be visible in a plan.

### Infrastructure & runtime
- **D-27 — Local PostgreSQL 17 via Docker Compose** port **5432**, database **`canvas`**,
  user/password `postgres`/`postgres`, **named volume**. `docker-compose.yml` at repo root;
  connection string in `appsettings.Development.json`.
- **D-28 — .NET 10** the pinned runtime.
- **D-49 — Project structure: one Blazor Web App project + one narrow test project** The
  geometry core lives in the app project but must carry **no Blazor dependency**.

### Schema (authoritative DDL: `CONSTRAINT-schema` in `.planning/intel/constraints.md`)
- **D-12 — Two tables only (`users`, `figures`)** **No `canvases` table** — "the canvas" is just
  the set of figures belonging to a user. ⚠️ **Never implement from D-12's own DDL sketch** — it is
  stale (still shows the dropped `created_at`). Implement from `CONSTRAINT-schema`.
- **D-46 — `type` is text + CHECK; no `created_at`** A PostgreSQL enum or an
  int-mapped C# enum would **silently invalidate** the CHECKs, which are written as `type <> 'circle'`.
- **D-39 — `figures.id` is a sequential integer and it IS the z-order** Load query is
  `SELECT * FROM figures WHERE user_id = @id ORDER BY id`.
- **D-42 — Schema via EF Core migrations, applied on startup** ⚠️ EF Core will **not** emit the
  CHECK constraints or the `COMMENT ON TABLE` on its own — configure them **explicitly** via
  `HasCheckConstraint` in `OnModelCreating`. **Every geometric guarantee in this design rests on
  them.** Forgetting them converts a machine-checked invariant back into a hoped-for convention.
- **D-08 — Passwords stored and compared in plaintext** (`password text NOT NULL`). Locked and
  deliberate: throwaway learning project only. **Do not "fix" this** without a new explicit decision.
  *(Phase 1 delivers the column; the login that fills it is Phase 2.)*
- **D-44 — Usernames are case-insensitive** `username text NOT NULL UNIQUE`, stored **lowercased**,
  trimmed, never empty. *(Phase 1 delivers the `UNIQUE` column; the lowercase-on-write lands with
  login in Phase 2.)*

### The three CHECK constraints — the heart of this phase
The database itself must **refuse an illegal row**. Not application code.
- `circle_is_a_circle` — `type <> 'circle' OR (x2 - x1 = y2 - y1 AND x2 > x1 AND (x2 - x1) % 2 = 0)`
  → rejects a non-square or odd-sided circle.
- `box_is_a_box` — `type NOT IN ('rectangle','triangle') OR (x2 > x1 AND y2 > y1)`
  → rejects a zero-area rectangle or triangle.
- `line_is_a_line` — `type <> 'line' OR (x2 >= x1 AND (x2 > x1 OR y2 <> y1))`
  → rejects a zero-length line. **A horizontal or vertical line is legal.**
- Plus `CREATE INDEX ix_figures_user_id ON figures(user_id)` — every page load filters on it.
- **No canvas-bounds CHECK constraints** (D-36) — D-24/D-29 already guarantee figures live inside
  the canvas; bounds CHECKs would be belt-and-braces on a rule the app never breaks.

### Geometry core (pure C#)
- **D-22 — Geometry storage: four integers, always, and they are ALWAYS the bounding box** *(REVISED)*
  A **circle is stored as the square it is inscribed in**. Recovery on read:
  `r = (x2−x1)/2`, `cx = x1+r`, `cy = y1+r`. The earlier "centre + rim point" encoding is **dead**.
  A move is a uniform translation, so `d` cancels algebraically and the radius is **exactly**
  preserved across any number of drags (integers ⇒ no float drift).
- **D-13 — A circle is drawn centre-out but stored as a square** (press centre, drag for radius).
  Interaction and storage are different things.
- **D-20 — Coordinates are integers** no floats anywhere in storage.
- **D-19 — Canvas is 1280 × 720** the fixed drawing surface.
- **D-21 — Triangle is derived from a 2-point drag box** the same bounding box as a rectangle.
- **D-41 — Normalise on write, but NOT the same way for every shape** Applied once, before the
  INSERT, in **exactly one place**.
  - Rectangle / triangle / circle → sort the axes **independently**.
  - Line → **swap the WHOLE POINT PAIR** (if `x1 > x2`, or `x1 == x2` and `y1 > y2`).
    ⚠️ **NEVER sort a line's axes independently** — (0,100)→(100,0) would become (0,0)→(100,100),
    **the opposite diagonal**.
  - Post-condition: `x1 ≤ x2` for every shape; `y1 ≤ y2` for rectangle/triangle/circle but **not**
    for a line. This is exactly why the clamp keeps its min/max bounding-box computation.
- **D-36 — The clamp formula; bounds are INCLUSIVE** (`W=1280`, `H=720`, valid domain
  `0..1280 × 0..720`). Clamp the movement **delta**, then translate all four coordinates uniformly:
  ```
  bx1 = min(x1,x2)  by1 = min(y1,y2)   bx2 = max(x1,x2)  by2 = max(y1,y2)
  dx' = clamp(dx, −bx1, W − bx2)       dy' = clamp(dy, −by1, H − by2)
  ```
  ⚠️ **Never clamp `x2`/`y2` independently of `x1`/`y1`** — that resizes instead of moving.
  ⚠️ **Per-axis independence is required:** `dx'` never reads `y`. A figure pinned to the right edge
  must still slide up and down.
- **D-24 — Figures stop at the canvas edge** a move never carries a figure outside `0..1280 × 0..720`;
  the operative formula is D-36's move clamp.
- **D-29 — Drawing also stops at the canvas edge** which yields the circle draw-clamp, the one
  genuinely type-specific rule in the app:
  `r = min( round(distance), cx, cy, W − cx, H − cy )`. Known and accepted consequence: pressing near
  an edge forces a tiny circle.
- **D-50 — The minimum-size guard is PER-TYPE, not shared** It **mirrors the CHECK constraints
  exactly**, so *the app can never write a row the database would refuse*. Build the guard and the
  CHECKs together — they are two halves of one rule.
  - Line → rejected only when both endpoints are identical. **Horizontal/vertical lines are legal.**
  - Rectangle / triangle / circle → rejected when width **or** height is zero.
  - A rejected draw fails **silently** — no message, no error.
  - ⚠️ D-23's "one shared guard" is **retracted**: a shared guard lets a zero-height *rectangle* drag
    through (start ≠ end), and the INSERT then violates `box_is_a_box`.

### Tests (TEST-01) — exactly the three silent failure modes
1. **Clamp maths** (D-36) — per-axis independence (a figure pinned to the right edge still moves
   vertically); inclusive bounds `0..1280 × 0..720`; the circle draw-clamp.
2. **Circle inscribed-square round-trip** (D-22) — centre and radius come back **exact** after store
   + reload, **and after translation**.
3. **Line normalisation** (D-41) — an up-and-right diagonal does **not** come back as the opposite
   diagonal.

### Claude's Discretion
- C# namespaces, file/class names, and internal method signatures of the geometry core.
- EF Core `DbContext` naming, entity class shape, and migration file naming.
- Test framework choice and test file organisation within the test project.
- How the DB connection is waited on / retried at startup before migrations apply.
- `docker-compose.yml` service naming and healthcheck details (beyond the pinned port/db/volume).

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### The authoritative schema and geometry contracts
- `.planning/intel/constraints.md` — **the normative artifacts.** Read in full. Specifically:
  - `CONSTRAINT-schema` — **the canonical DDL. AUTHORITATIVE. Implement from this, never from D-12.**
  - `CONSTRAINT-geometry` — storage encoding, circle-as-inscribed-square, recovery formulas
  - `CONSTRAINT-normalisation` — canonical order on write, per shape
  - `CONSTRAINT-min-size-guard` — the per-type draw rejection table
  - `CONSTRAINT-clamp` — the clamp formula, inclusive bounds, circle draw-clamp
  - `CONSTRAINT-env` — .NET 10, PostgreSQL 17, Docker Compose, project structure
  - `CONSTRAINT-security` — plaintext passwords, locked and deliberate

### Decision log (normalised)
- `.planning/intel/decisions.md` — the conflict-resolved decision set (D-01…D-58)
- `.planning/INGEST-CONFLICTS.md` — the supersession record; explains *why* D-12's sketch, D-23's
  shared guard, and D-22's old encoding are dead
- `docs/DECISIONS.md` — original source. If read directly, read **§THE SCHEMA**, **D-22**, **D-36**,
  **D-41**, **D-42**, **D-46**, **D-50** — and check the status index first.

### Phase scope
- `.planning/ROADMAP.md` — Phase 1 section (goal, 4 success criteria, planning notes)
- `.planning/REQUIREMENTS.md` — **DATA-02** (persistence/schema), **TEST-01** (the three tests)

</canonical_refs>

<specifics>
## Specific Ideas

- **The DDL is reproduced verbatim** in `CONSTRAINT-schema`. Copy it; do not re-derive it.
- **Build the CHECK constraints and the per-type min-size guard together** (D-50 ↔ the three CHECKs).
  They are two halves of one rule, and the phase goal — "the database itself refuses an illegal row"
  — is only true if they agree exactly.
- **Success criterion 3 is a test the database must pass, not the app**: attempting to INSERT a
  non-square circle, a zero-area rectangle, or a zero-length line must be rejected **by a CHECK
  constraint**. A test that only exercises the C# guard does not prove this.
- The geometry core is called by Phases 3–5. Keep it **pure** — no Blazor, no EF, no I/O.

</specifics>

<deferred>
## Deferred Ideas

Out of this phase, decided and recorded — do not build:

- **UI of any kind** — SVG surface, six-button toolbar, selection, live preview, drag termination,
  click-vs-drag threshold (D-06, D-16/D-30/D-33, D-31, D-32, D-35, D-37, D-43, D-48, D-57, D-58,
  D-38, D-55, D-14, D-18). *Phases 3–5.*
- **Login, session, logout** — the static-SSR login page, cookie auth, identity claim, route
  protection (D-17, D-26, D-34, D-51, D-25, D-56). *Phase 2.* The `users` **table** is Phase 1; the
  flow that writes to it is not.
- **The runtime write policy half of DATA-02** — draw → INSERT, drag-on-drop → UPDATE, delete →
  DELETE, no Save button, zero-row-UPDATE staleness guard (D-09, D-10). Phase 1 delivers the
  **schema and migrations** half of DATA-02; the CRUD paths need a UI to exist. *Phases 3–5.*
- **Multi-tab sync** — the broadcast message contract, throttle, echo filter, resurrection fix,
  mid-drag discard (D-11, D-40, D-47, D-53, D-54). *Later.*
- **Error handling / save-failure policy** — friendly DB error messages, retry-then-rollback
  (D-45, D-52). *Later.*

</deferred>

<scope_fence>
## Scope Fence

**Phase 1 is done when all four ROADMAP success criteria hold — and not one line before or after.**

A plan is **out of bounds** if it:
- adds any `.razor` component, any SVG, or any Blazor interactivity;
- implements the login form, cookie auth, or logout;
- writes broadcast/sync/notifier code;
- adds a `canvases` table, a `created_at` column, a PostgreSQL enum for `type`, or any bounds CHECK;
- implements the min-size guard as a **single shared** rule (D-23's retracted form);
- normalises a **line** by sorting its axes independently;
- clamps `x2`/`y2` independently of `x1`/`y1`.

**Open item, explicitly NOT in this phase:** `INGEST-CONFLICTS.md` carries one unresolved WARNING —
D-11's summary of the mid-drag receive filter contradicts D-54 (synthesis ruled **D-54 wins**:
blanket discard). That is a **sync** concern and has **no bearing on Phase 1**. It must be confirmed
before the sync phase, not now.

</scope_fence>

---

*Phase: BC-01-database-schema-geometry-core*
*Context gathered: 2026-07-14 via ADR Ingest Express Path*
