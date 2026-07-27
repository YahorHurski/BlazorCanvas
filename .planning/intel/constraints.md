# Constraints

Extracted from the single ADR set `docs/DECISIONS.md`. These are the executable/normative
artifacts: schema, contracts, formulas, and fixed constants. Where a constraint has a canonical
form in the source, it is reproduced verbatim.

> 🛑 **v1.11 AMENDMENTS (2026-07-23) — STORAGE MODEL REWRITE (authority: `docs/DECISIONS.md` D-59).**
> `figures` storage becomes an **anchor (`x,y`) + `geometry jsonb`** model with a **`uuid`** id and a
> **`numeric z`** layer order (load `ORDER BY z, id`; index `(user_id, z)`). **CONSTRAINT-schema**,
> **-geometry**, **-normalisation**, **-clamp** and **-messages** below are updated/superseded
> accordingly: the **edge-clamp is REMOVED** (figures may leave the canvas), the three geometry CHECKs
> are gone (**no DB CHECK on `geometry`** — the server is the sole writer), `type text` + whitelist
> CHECK is **kept**, and the sync payload carries anchor + geometry. Existing figures are preserved by
> a hand-written backfill. This documentation amendment records a design change to be built in v1.11.

> ⚠️ **v1.1 AMENDMENTS (2026-07-20).** Superseding facts (authority: `docs/DECISIONS.md`): canvas
> **W=1472, H=828** and valid domain **`0..1472 × 0..828`** (was 1280×720 / `0..1280 × 0..720`) —
> the formula is unchanged, only the constants; **selected-figure indicator = ~1px blue+white dashed
> trace on the figure's own outline, topmost, `pointer-events:none`** (was red 2px); the former
> **no-JS rule is retired** and policy is **permissive** (hand-authored JS/interop may be selected by
> a later decision). No DB migration or runtime change in this documentation-only amendment.

---

## CONSTRAINT-schema — The canonical DDL *(v1.11: anchor + geometry JSON — D-59)*
type: schema
source: docs/DECISIONS.md → section "THE SCHEMA (canonical — v1.11: anchor + geometry JSON; see D-59)"
status: **AUTHORITATIVE.** v1.11 replaced the four-integer bbox schema with the anchor+geometry model
(D-59). Supersedes D-12's illustrative sketch. Implement from this, never from D-12 or the pre-v1.11
schema (kept at the bottom for the migration's reference).

```sql
CREATE TABLE users (            -- UNCHANGED across v1.11
    id       integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    username text NOT NULL UNIQUE,   -- stored LOWERCASED (D-44), trimmed, never empty
    password text NOT NULL           -- PLAINTEXT. Throwaway project only. (D-08)
);

CREATE TABLE figures (
    id       uuid    NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,  -- order carried by z, not id (D-59)
    user_id  integer NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    type     text    NOT NULL CHECK (type IN ('line','rectangle','circle','triangle')),  -- whitelist kept (D-46)
    x        integer NOT NULL,        -- anchor X (D-20: integer)
    y        integer NOT NULL,        -- anchor Y
    geometry jsonb   NOT NULL,        -- shape RELATIVE to the anchor: circle {r}, rectangle {w,h}, ...
    z        numeric NOT NULL         -- layer order; NO UNIQUE; fractional (insert-between = midpoint)
    -- no created_at (D-46); no DB CHECK on geometry — the server is the sole writer (D-09 / D-59)
);

CREATE INDEX ix_figures_user_id_z ON figures(user_id, z);   -- serves the load filter AND the ORDER BY z
```

- **Two tables only. No `canvases` table** — "the canvas" is just the set of figures belonging to
  a user (D-12, upheld). `users` is unchanged.
- **A figure = anchor (`x, y`) + `geometry jsonb`** (D-59). Drag updates only `x, y`, for every shape.
- **No `created_at`** (D-46) — order is carried by the `numeric z` column (D-59).
- **`id` is a `uuid`** (D-59, was integer) — order is `z`, so the id no longer encodes creation order.
- **`z` is `numeric`, NO `UNIQUE`** — fractional layer order; load `ORDER BY z, id`.
- **`type` stays `text` + whitelist CHECK** (D-46, Variant 1) — the DB keeps rejecting unknown types.
- **No DB CHECK on `geometry`** (D-59) — the server is the sole writer (D-09); the app is the guarantor
  of geometry well-formedness (an explicit trust boundary). The three v1.0/v1.1 geometry CHECKs
  (`circle_is_a_circle`, `box_is_a_box`, `line_is_a_line`) are **removed**.
- **No canvas-bounds CHECK constraints** — there is no edge clamp at all now (D-24/D-29/D-36 dropped).
- **Load query:** `SELECT * FROM figures WHERE user_id = @id ORDER BY z, id`.
- **EF Core** (D-42): map `geometry` to `jsonb`, keep the `type` whitelist CHECK and the composite
  index via `OnModelCreating`, rewrite the table `COMMENT`; **remove** the three geometry CHECKs.
- **Migration:** hand-written backfill `x1,y1,x2,y2 → x,y,geometry` per type, tested against the
  immutable fixture `tests/.../Fixtures/v1.1-pre-rewrite.sql` + MANIFEST.

<details><summary>Pre-v1.11 schema (historical — four-integer bounding box, D-22; migration reference)</summary>

```sql
CREATE TABLE figures (
    id      integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY,  -- was also the z-order (D-39)
    user_id integer NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    type    text    NOT NULL CHECK (type IN ('line','rectangle','circle','triangle')),
    x1 integer NOT NULL, y1 integer NOT NULL, x2 integer NOT NULL, y2 integer NOT NULL,
    CONSTRAINT circle_is_a_circle CHECK (type <> 'circle' OR (x2 - x1 = y2 - y1 AND x2 > x1 AND (x2 - x1) % 2 = 0)),
    CONSTRAINT box_is_a_box CHECK (type NOT IN ('rectangle','triangle') OR (x2 > x1 AND y2 > y1)),
    CONSTRAINT line_is_a_line CHECK (type <> 'line' OR (x2 >= x1 AND (x2 > x1 OR y2 <> y1)))
);
CREATE INDEX ix_figures_user_id ON figures(user_id);
-- Load: SELECT * FROM figures WHERE user_id = @id ORDER BY id
```

</details>

---

## CONSTRAINT-geometry — Storage encoding *(v1.11: anchor + geometry JSON — D-59)*
type: schema / invariant
source: docs/DECISIONS.md (D-59; supersedes D-22) · D-20, D-13

**A figure = an anchor (`x, y`, integers) + a `geometry jsonb` holding the shape's form RELATIVE to
the anchor.** Dragging updates **only `x, y`**, for every shape; the form never changes on a move.

| Shape | anchor `(x, y)` | `geometry` (relative to anchor) |
|---|---|---|
| Circle | centre (or a fixed reference — pinned at plan time) | `{r}` — a single scalar; an oval is unrepresentable |
| Rectangle / triangle | min corner | `{w, h}` — positive extents |
| Line | one endpoint | `{dx, dy}` — either sign; **swap the whole point pair on normalise, never sort axes** |

Exact per-type JSON shape (esp. the **line**) is pinned at plan/spec time (D-59, "Left for plan time").
**No DB CHECK on `geometry`** — the server is the sole writer (D-09) and validates by construction.

> Pre-v1.11 (historical, D-22): every figure was four integers `x1,y1,x2,y2` that were always the
> bounding box; a circle was its inscribed square (`r = (x2−x1)/2`), machine-checked by
> `circle_is_a_circle`. Superseded by the anchor+geometry model above.

---

## CONSTRAINT-normalisation — Canonical order on write *(v1.11: re-expressed for anchor+geometry — D-59)*
type: invariant
source: docs/DECISIONS.md (D-41, re-expressed by D-59)

Applied **once, before the INSERT**, in **exactly one place** in the app. Re-expressed for the
anchor+geometry model:

- **Rectangle / triangle** → anchor = min corner, geometry = **positive `{w, h}`** ("sort the axes"
  becomes "positive extents").
- **Line** → anchor = one endpoint, geometry = `{dx, dy}` of either sign. **Swap the WHOLE POINT
  PAIR, never sort a line's axes independently** — (0,100)→(100,0) sorted per-axis becomes
  (0,0)→(100,100), the opposite diagonal. **This landmine carries over unchanged from D-41.**

*(Pre-v1.11: normalisation sorted the four bbox integers for rectangle/triangle/circle and swapped
the point pair for lines. The line landmine is identical.)*

---

## CONSTRAINT-min-size-guard — Per-type draw rejection *(v1.11: code-side only — D-59)*
type: invariant
source: docs/DECISIONS.md (D-50; D-23 #1 kept code-side by D-59)

| Shape | Rejected when |
|---|---|
| Line | both endpoints identical (zero length). **Horizontal and vertical lines are legal.** |
| Rectangle / triangle / circle | width **or** height is zero (circle: radius zero) |

**v1.11:** this is now **purely a code-side guard** (`MinSizeGuard` in the commit path, on C#
primitives before geometry is serialised) — the mirrored database CHECKs are gone (no DB CHECK on
`geometry`, D-59; only the `type` whitelist remains). It rejects only **strictly zero-size** draws (a
permanent invisible, unselectable poison row). A rejected draw fails **silently** — no message, no error.

---

## CONSTRAINT-clamp — 🛑 REMOVED in v1.11 (no canvas-edge clamp — D-59)
type: invariant / nfr
source: docs/DECISIONS.md (D-24/D-29/D-36 DROPPED by D-59)

**v1.11 drops the canvas-edge clamp entirely.** Figures may be dragged off-canvas (accepted risk:
currently unrecoverable — no pan/undo — taken because the roadmap intends to remove canvas bounds
later). Drag now **translates the anchor (`x, y`) only**, with no bounds arithmetic; the circle
draw-clamp goes away with it. The canvas keeps its **1472 × 828** size (D-19) as a visual surface,
but nothing is clamped to it.

**Retained ordering (without the clamp):** render → broadcast — never broadcast a raw position mid-drag
before the local re-render (the sync semantics of D-47/D-54 are unchanged).

> Pre-v1.11 (historical, D-36): the move clamped the movement delta against the inclusive domain
> `0..1472 × 0..828` and translated all four bbox columns uniformly (per-axis independent, "slides
> along the wall"); the circle draw-clamp `r = min(round(distance), cx, cy, W−cx, H−cy)` forced a
> tiny circle near an edge. All removed in v1.11.

---

## CONSTRAINT-messages — The broadcast message contract (canonical) *(v1.11: anchor+geometry payload — D-59)*
type: protocol
source: docs/DECISIONS.md (D-53, payload amended by D-59)
status: **AUTHORITATIVE.** Supersedes the partial/inconsistent descriptions in D-11, D-22, D-40.
**All semantics unchanged in v1.11 — only the payload shape changed** (bbox → anchor + geometry, `id` is a `uuid`).

`sender` is a **per-circuit GUID**, generated once when a tab's canvas component initialises. It
exists solely for the echo filter: a tab ignores any message whose `sender` equals its own.

| Kind | Payload *(v1.11)* | Receiver's action |
|---|---|---|
| `draw` | `{ kind, sender, id, type, x, y, geometry }` | Insert or update by `id`. **The only kind that may create a figure.** Sent *after* the INSERT. |
| `move` | `{ kind, sender, id, x, y }` | **UPDATE ONLY — never insert.** Unknown figure → **ignore the message entirely.** (Kills the resurrection bug — D-40.) Carries only the anchor — the form never changes on a move (D-59). |
| `delete` | `{ kind, sender, id }` | Remove by `id`. Idempotent — deleting an unknown figure is a silent no-op. |
| `rollback` | `{ kind, sender, id, x, y }` | Restore the figure to the given anchor. Sent when a save fails after all retries (D-52). Applied **update-only**, like `move`. |

*(Pre-v1.11: `draw`/`move`/`rollback` carried `x1,y1,x2,y2` and `id` was an integer. Exact serialised
shape of `geometry` in the `draw` payload is pinned at plan time.)*

- **No `drop` kind.** A drag's final position is simply the last `move` (guaranteed sent by D-47's
  trailing edge), followed by silence.
- **`move` carries no `type`** — a figure's type never changes and the receiver already knows it.
- **Draw previews are NOT broadcast** (D-35). Live glide applies to *dragging an existing figure*,
  never to *drawing a new one*.
- Notifier events are keyed by **`user_id`** (D-11 core rule 7), matching the DB and the cookie claim.
- **Throttle: 50 ms, trailing edge guaranteed** (D-47) — a throttle, not a debounce.
- **Mid-drag, a tab discards ALL incoming broadcasts** (D-54) — `if (_dragging) return;`.

---

## CONSTRAINT-sync-core — The irreducible sync core (none optional)
type: invariant
source: docs/DECISIONS.md (D-11, as amended by D-36/D-40/D-47/D-53/D-54)

1. Unsubscribe in `Dispose()` (else `ObjectDisposedException` on publish to closed tabs)
2. `InvokeAsync(StateHasChanged)` in every handler (the event fires on the publisher's thread)
3. Echo filter via per-circuit `sender` GUID
4. Mid-drag: discard ALL incoming broadcasts (D-54)
5. Idempotent apply keyed by figure Id — **`move` is UPDATE-ONLY** (D-40)
6. Throttle drag broadcasts: 50 ms, trailing edge guaranteed (D-47)
7. Key notifier events by `user_id` (D-51)
8. Render → broadcast, in that order *(v1.11: the clamp step is gone — D-59; pre-v1.11 this was clamp → render → broadcast, D-36)*
9. A zero-row UPDATE broadcasts a `delete` (D-40)

---

## CONSTRAINT-persistence — Write policy
type: invariant
source: docs/DECISIONS.md (D-09, D-10, D-52)

| User action | Database effect |
|---|---|
| Draw a figure | `INSERT` one row into `figures` |
| Drag a figure (on drop only) | `UPDATE` that row's coordinates |
| Delete a figure | `DELETE` that row |

- **No Save button anywhere.** No "unsaved changes" state.
- **Postgres sees exactly ONE UPDATE per drag** — intermediate glide positions travel through
  memory only. Writing them would be ~100× write amplification.
- Use `ExecuteUpdateAsync` / `ExecuteDeleteAsync` and check the **affected-row count**. Zero rows
  on UPDATE ⇒ the figure is gone ⇒ silently remove from that tab's view **and broadcast a delete**.
  This is **not an error** — it is expected staleness.
- Save failure: retry ≤ 2 more times **only if transient**. Never retry validation errors, CHECK
  violations, missing figures, or zero-row UPDATEs. On final failure: broadcast `rollback` →
  restore locally → modal → reload the canvas from Postgres on OK.
- The figure's **original coordinates must be retained for the entire drag** to make rollback possible.

---

## CONSTRAINT-layout — The coordinate constants
type: invariant
source: docs/DECISIONS.md (D-43, D-18, D-19, D-56)

```
canvasX = PageX
canvasY = PageY − 48
```
- Page margin **0**; toolbar exactly **48px** tall at the top; canvas immediately below at document
  position **(0, 48)**, anchored top-left (**never** `margin: auto`).
- **No CSS border on the SVG** (a border shifts the SVG interior by its own width and corrupts the
  mapping).
- Toolbar height stays 48px even with Logout in the strip (D-56).
- **LANDMINE: use `PageX`/`PageY`, NEVER `OffsetX`/`OffsetY`** — `OffsetX/Y` is relative to the
  event target, and every drag and every selection begins on a figure.

---

## CONSTRAINT-visual — Fixed style constants
type: nfr
source: docs/DECISIONS.md (D-58, D-38, D-55, D-19, D-43)

| Constant | Value |
|---|---|
| Figure outline | black, **2px** |
| Figure fill | **white** (makes the interior clickable — SVG does not register clicks inside an unfilled shape) |
| Selected figure indicator | **~1px blue+white dashed trace on the figure's own outline, topmost, `pointer-events:none`** *(v1.1; was red 2px)* |
| Page background | **light grey** (the only thing that makes the borderless canvas edge visible) |
| Canvas | white, **1472 × 828** *(v1.1; was 1280 × 720)*, **no border** |
| Toolbar | **48px** tall, six buttons: `[pointer] [line] [rectangle] [circle] [triangle] [delete]`, logout right-aligned |

- 2px (not 1px) is deliberate: the stroke is the only click target a line has (D-32 declined a
  widened hit-area).
- **Delete button is greyed out and unclickable when nothing is selected** (D-58).
- No style columns in the database; no colour picker (D-14).

---

## CONSTRAINT-thresholds — Interaction constants
type: nfr
source: docs/DECISIONS.md (D-48, D-47, D-31)

- **Click vs drag: 3 px.** < 3 px → click (select, **no DB write**); ≥ 3 px → drag (persist on drop).
- Starting a drag also selects; the figure stays selected after the drop.
- **Drag broadcast throttle: 50 ms**, trailing edge guaranteed.
- **The pointer tool is armed on page load** — a stray first click cannot create a figure.

---

## CONSTRAINT-env — Runtime and infrastructure
type: nfr
source: docs/DECISIONS.md (D-28, D-27, D-58, D-49, D-06, D-18, D-33, D-37, D-57)

- **.NET 10** (current LTS). Verified: SDKs 8.0.418 / 9.0.311 / 10.0.301; Docker 29.1.3.
- **Blazor Server (InteractiveServer)** + **static SSR** for `/login` and `POST /logout` — two
  render modes. No Web API project.
- **PostgreSQL 17** via Docker Compose: port **5432**, database **`canvas`**, user/password
  **`postgres`/`postgres`**, **named volume** (figures survive a container restart). Connection
  string in `appsettings.Development.json`. `docker-compose.yml` at repo root.
- EF Core / Npgsql. Migrations applied automatically on startup.
- **Retired policy (v1.1):** the former application-authored JavaScript prohibition is not an
  active constraint. JavaScript or interop is **permissive** only when a later decision elects to
  use it; this documentation-only amendment adds neither JavaScript/interop nor a runtime,
  gesture, or keyboard feature. D-06 remains SVG for its DOM hit-testing and lower-code benefit;
  D-18 keeps fixed 1:1 sizing for MVP simplicity and required geometry; D-33 keeps toolbar Delete
  for MVP simplicity and unambiguous behaviour; D-37 prevents a stranded drag; and D-57 keeps one
  committed-draw rule with cancellation outside MVP scope. A Delete-key shortcut,
  `setPointerCapture`, or Escape-to-cancel each requires its own later decision.
- Project structure: one Blazor Web App project + one narrow test project.

---

## CONSTRAINT-security — Explicitly accepted non-security
type: nfr
source: docs/DECISIONS.md (D-08)

Passwords are stored and compared **in plaintext**. No hashing, no salting, no real auth.
**Acceptable ONLY because this is a throwaway learning project.** It must never touch real
credentials and must never be deployed as-is to the public internet. Recorded deliberately so the
choice stays conscious. Do not "fix" this without an explicit new decision — it is locked.
