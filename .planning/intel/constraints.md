# Constraints

Extracted from the single ADR set `docs/DECISIONS.md`. These are the executable/normative
artifacts: schema, contracts, formulas, and fixed constants. Where a constraint has a canonical
form in the source, it is reproduced verbatim.

---

## CONSTRAINT-schema — The canonical DDL
type: schema
source: docs/DECISIONS.md → section "THE SCHEMA (canonical — assembled from D-12, D-22, D-39, D-41, D-46, D-44, D-50)"
status: **AUTHORITATIVE.** Supersedes D-12's illustrative sketch (which still shows the dropped
`created_at` column). Implement from this, never from D-12.

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

- **Two tables only. No `canvases` table** — "the canvas" is just the set of figures belonging to
  a user (D-12).
- **No `created_at`** (D-46) — the sequential `id` is the z-order (D-39).
- **`type` must be `text`** (D-46) — the CHECKs are written as `type <> 'circle'`; a PG enum or an
  int-mapped C# enum would silently invalidate them.
- **No canvas-bounds CHECK constraints** (D-36) — D-24/D-29 already guarantee figures live inside
  the canvas.
- **Load query:** `SELECT * FROM figures WHERE user_id = @id ORDER BY id` — the `ORDER BY` is what
  reconstructs z-order after F5 (D-39).
- **EF Core must be told about all of this explicitly** (D-42): CHECK constraints via
  `HasCheckConstraint` in `OnModelCreating`, plus the `COMMENT ON TABLE`. EF will not emit them on
  its own, and every geometric guarantee rests on them.

---

## CONSTRAINT-geometry — Storage encoding
type: schema / invariant
source: docs/DECISIONS.md (D-22 revised, D-20, D-13)

**Every figure is exactly four integers `x1, y1, x2, y2`, all non-null, and for every shape those
four numbers ARE its bounding box.**

| Shape | (x1, y1) | (x2, y2) |
|---|---|---|
| Line | one endpoint | the other endpoint |
| Rectangle | one corner | the opposite corner |
| Triangle | one corner of its box | the opposite corner |
| Circle | top-left of the inscribed square `(cx − r, cy − r)` | bottom-right `(cx + r, cy + r)` |

Circle recovery on read: `r = (x2 − x1) / 2`, `cx = x1 + r`, `cy = y1 + r`.
Invariant: a move is a uniform translation; `d` cancels algebraically, so the radius is exactly
preserved across any number of drags (integers ⇒ no float drift).
Machine-checked by `circle_is_a_circle`: square · positive · even side.

---

## CONSTRAINT-normalisation — Canonical order on write
type: invariant
source: docs/DECISIONS.md (D-41)

Applied **once, before the INSERT**, in **exactly one place** in the app.

- **Rectangle / triangle / circle** → sort the axes independently (`x1 = min(x1,x2)`, etc.).
- **Line** → **swap the WHOLE POINT PAIR** (if `x1 > x2`, or if `x1 == x2` and `y1 > y2`).
  **NEVER sort a line's axes independently** — (0,100)→(100,0) would become (0,0)→(100,100), the
  opposite diagonal.

Post-condition: `x1 ≤ x2` for every shape; `y1 ≤ y2` for rectangle/triangle/circle but **not** for
a line. This is exactly why the clamp keeps its min/max bounding-box computation.

---

## CONSTRAINT-min-size-guard — Per-type draw rejection
type: invariant
source: docs/DECISIONS.md (D-50, retracting D-23's shared guard)

| Shape | Rejected when |
|---|---|
| Line | both endpoints identical (zero length). **Horizontal and vertical lines are legal.** |
| Rectangle / triangle / circle | width **or** height is zero (circle: radius zero) |

Mirrors the CHECK constraints exactly, so **the app can never write a row the database would
refuse.** A rejected draw fails **silently** — no message, no error.

---

## CONSTRAINT-clamp — The clamp formula, inclusive bounds
type: invariant / nfr
source: docs/DECISIONS.md (D-36; operative spec for D-24 and D-29)

`W = 1280`, `H = 720`. **Bounds are INCLUSIVE: valid domain `0..1280 × 0..720`.**

Move clamp:
```
bx1 = min(x1,x2)   by1 = min(y1,y2)
bx2 = max(x1,x2)   by2 = max(y1,y2)

dx' = clamp(dx, −bx1, W − bx2)      clamp(v, lo, hi) = min(max(v, lo), hi)
dy' = clamp(dy, −by1, H − by2)

translate uniformly:  x1 += dx'  y1 += dy'  x2 += dx'  y2 += dy'
```
- **Clamp the movement DELTA, then translate all four uniformly.** Never clamp `x2`/`y2`
  independently of `x1`/`y1` — that resizes instead of moving (and under the inscribed-square
  encoding it now fails loudly by violating the circle CHECK).
- **Per-axis independence is required:** `dx'` never reads `y`. A figure pinned to the right edge
  must still slide up and down.
- **Ordering: clamp → render → broadcast.** Never broadcast a raw, unclamped position.

Circle draw-clamp (**the one genuinely type-specific rule in the app**):
```
r = min( round(distance), cx, cy, W − cx, H − cy )
```
Known consequence: **pressing near an edge forces a tiny circle** (press at (10,360), drag 200 px
right → r caps at 10). Inherent to D-13 × D-29; would exist under any encoding.

---

## CONSTRAINT-messages — The broadcast message contract (canonical)
type: protocol
source: docs/DECISIONS.md (D-53)
status: **AUTHORITATIVE.** Supersedes the partial/inconsistent descriptions in D-11, D-22, D-40.

`sender` is a **per-circuit GUID**, generated once when a tab's canvas component initialises. It
exists solely for the echo filter: a tab ignores any message whose `sender` equals its own.

| Kind | Payload | Receiver's action |
|---|---|---|
| `draw` | `{ kind, sender, id, type, x1, y1, x2, y2 }` | Insert or update by `id`. **The only kind that may create a figure.** Sent *after* the INSERT (the `id` does not exist until then — D-39). |
| `move` | `{ kind, sender, id, x1, y1, x2, y2 }` | **UPDATE ONLY — never insert.** Unknown figure → **ignore the message entirely.** (Kills the resurrection bug — D-40.) |
| `delete` | `{ kind, sender, id }` | Remove by `id`. Idempotent — deleting an unknown figure is a silent no-op. |
| `rollback` | `{ kind, sender, id, x1, y1, x2, y2 }` | Restore the figure to the given coordinates. Sent when a save fails after all retries (D-52). Applied **update-only**, like `move`. |

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
8. Clamp → render → broadcast, in that order (D-36)
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
| Selected figure outline | **red, 2px** |
| Page background | **light grey** (the only thing that makes the borderless canvas edge visible) |
| Canvas | white, **1280 × 720**, **no border** |
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
source: docs/DECISIONS.md (D-28, D-27, D-58, D-49, D-06)

- **.NET 10** (current LTS). Verified: SDKs 8.0.418 / 9.0.311 / 10.0.301; Docker 29.1.3.
- **Blazor Server (InteractiveServer)** + **static SSR** for `/login` and `POST /logout` — two
  render modes. No Web API project.
- **PostgreSQL 17** via Docker Compose: port **5432**, database **`canvas`**, user/password
  **`postgres`/`postgres`**, **named volume** (figures survive a container restart). Connection
  string in `appsettings.Development.json`. `docker-compose.yml` at repo root.
- EF Core / Npgsql. Migrations applied automatically on startup.
- **NO JAVASCRIPT ANYWHERE.** This is a hard constraint and it is load-bearing: it is what forced
  D-33 (toolbar Delete instead of the Delete key), D-37 (no `setPointerCapture`), D-57 (no Escape
  to abandon a draw), and D-18 (1:1 canvas instead of a scaling viewBox).
- Project structure: one Blazor Web App project + one narrow test project.

---

## CONSTRAINT-security — Explicitly accepted non-security
type: nfr
source: docs/DECISIONS.md (D-08)

Passwords are stored and compared **in plaintext**. No hashing, no salting, no real auth.
**Acceptable ONLY because this is a throwaway learning project.** It must never touch real
credentials and must never be deployed as-is to the public internet. Recorded deliberately so the
choice stays conscious. Do not "fix" this without an explicit new decision — it is locked.
