# Constraints

Extracted from the single ADR set `docs/DECISIONS.md`. These are the executable/normative
artifacts: schema, contracts, formulas, and fixed constants. Where a constraint has a canonical
form in the source, it is reproduced verbatim.

> # 🛑 v1.11 AMENDMENTS (2026-07-21) — THE STORAGE MODEL WAS REPLACED
>
> **Authority: `docs/DECISIONS.md` → D-59…D-69.** Full reasoning and the migration plan:
> `docs/DATA-MODEL-v1.11-DRAFT.md`.
>
> The following constraints below are **REWRITTEN** for v1.11: `CONSTRAINT-schema`,
> `CONSTRAINT-geometry`, `CONSTRAINT-normalisation`, `CONSTRAINT-min-size-guard`,
> `CONSTRAINT-messages`, `CONSTRAINT-visual`. Each carries its own 🛑 note.
>
> One-line summary: a figure is no longer four integers. **Position (`x, y, rotation`) and shape
> (`geometry jsonb`, in local coordinates) are stored separately.** Ids are `uuid`, draw order is
> a `z` column, there are four tables, and geometry is validated in C# rather than by CHECK
> constraints.
>
> Unchanged: the clamp formula, the write policy (one UPDATE per drag on drop), the sync *rules*
> (no-resurrection, 50 ms throttle, mid-drag isolation, echo filter), layout constants, and
> interaction thresholds.

> ⚠️ **v1.1 AMENDMENTS (2026-07-20).** Superseding facts (authority: `docs/DECISIONS.md`): canvas
> **W=1472, H=828** and valid domain **`0..1472 × 0..828`** (was 1280×720 / `0..1280 × 0..720`) —
> the formula is unchanged, only the constants; **selected-figure indicator = ~1px blue+white dashed
> trace on the figure's own outline, topmost, `pointer-events:none`** (was red 2px); the former
> **no-JS rule is retired** and policy is **permissive** (hand-authored JS/interop may be selected by
> a later decision). No DB migration or runtime change in this documentation-only amendment.

---

## CONSTRAINT-schema — The canonical DDL
type: schema
source: docs/DECISIONS.md → D-59…D-69 (v1.11). Full rationale: docs/DATA-MODEL-v1.11-DRAFT.md
status: **AUTHORITATIVE.** 🛑 Replaces the v1.0/v1.1 DDL entirely. `THE SCHEMA` in
`docs/DECISIONS.md` is dead and marked as such; never implement from it or from D-12/D-22.

```sql
CREATE TABLE users (                       -- UNCHANGED
    id       integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    username text NOT NULL UNIQUE,   -- stored LOWERCASED (D-44), trimmed, never empty
    password text NOT NULL           -- PLAINTEXT. Throwaway project only. (D-08)
);

-- D-64. One row per user in v1.11; NO UI for creating more.
CREATE TABLE canvases (
    id         uuid        PRIMARY KEY,
    owner_id   integer     NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    name       text        NOT NULL DEFAULT 'Canvas',
    width      integer     NOT NULL DEFAULT 1472,      -- was CanvasBounds, now data
    height     integer     NOT NULL DEFAULT 828,
    background text        NOT NULL DEFAULT '#FFFFFF',
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now()
);

CREATE INDEX ix_canvases_owner ON canvases(owner_id);

-- D-65. Adding a figure type is an INSERT here, never an ALTER TABLE.
CREATE TABLE figure_types (
    name text PRIMARY KEY
);
-- seeded at application start: 'line', 'rectangle', 'circle', 'triangle'

CREATE TABLE figures (
    id        uuid PRIMARY KEY,                                        -- D-62
    canvas_id uuid NOT NULL REFERENCES canvases(id)    ON DELETE CASCADE,
    type      text NOT NULL REFERENCES figure_types(name),             -- D-65

    -- POSITION (D-59). A move touches ONLY these.
    x        numeric(12,3) NOT NULL,                                   -- D-61
    y        numeric(12,3) NOT NULL,
    rotation numeric(7,3)  NOT NULL DEFAULT 0,

    -- SHAPE (D-60), in LOCAL coordinates from (0,0). Source of truth.
    geometry jsonb NOT NULL,

    -- STYLE (D-66). Written only through the C# validator.
    style jsonb NOT NULL DEFAULT '{}',

    z numeric NOT NULL,                                                -- D-63

    -- BBOX CACHE (D-67). Pure function of geometry. EXCLUDES the stroke.
    bbox_x double precision NOT NULL,
    bbox_y double precision NOT NULL,
    bbox_w double precision NOT NULL,
    bbox_h double precision NOT NULL,

    created_at timestamptz NOT NULL DEFAULT now(),                     -- D-68
    updated_at timestamptz NOT NULL DEFAULT now(),

    CONSTRAINT z_unique_per_canvas unique (canvas_id, z),              -- D-63
    CONSTRAINT style_is_object     CHECK (jsonb_typeof(style)    = 'object'),
    CONSTRAINT geometry_is_object  CHECK (jsonb_typeof(geometry) = 'object'),
    CONSTRAINT bbox_is_positive    CHECK (bbox_w >= 0 AND bbox_h >= 0)
);

CREATE INDEX ix_figures_canvas_z ON figures(canvas_id, z);
```

- **Four tables** (D-64). `canvases` exists; `figures.user_id` is gone, replaced by `canvas_id`.
- **NO geometry CHECK constraints** (D-60). `circle_is_a_circle`, `box_is_a_box` and
  `line_is_a_line` are deleted. The database cannot validate shape any more — **all geometric
  correctness lives in C#, at one choke point.** This is an accepted cost, not an oversight.
- **Load query:** `SELECT * FROM figures WHERE canvas_id = @id ORDER BY z` (D-63).
- **`z` is unique per canvas.** A new figure takes `z = max(z) + 1`; **a retry on unique violation
  is REQUIRED** (two tabs drawing simultaneously compute the same value). Without it a figure
  silently fails to appear.
- **`bbox_*` is maintained by the app, NOT a generated column** (D-67) — a generated column would
  need shape logic duplicated in PL/pgSQL, which would make every new figure type a migration.
- **DEFERRED, do not add** (D-69): `version`, `parent_id`, a GiST index on the bbox, a history
  table, soft delete. Each is an instant additive change later.
- **EF Core must be told about all of this explicitly** (D-42): the check constraints, the unique
  constraint, and the jsonb column mappings. EF will not infer them.

---

## CONSTRAINT-geometry — Storage encoding
type: schema / invariant
source: docs/DECISIONS.md (D-59, D-60, D-61)
status: 🛑 **REWRITTEN in v1.11.** The old "four integers ARE the bounding box" encoding is dead.

**Position and shape are separate. `geometry` holds the shape in LOCAL coordinates from (0,0);
`x, y, rotation` place it on the canvas.**

| Shape | `geometry` |
|---|---|
| Line | `{"points": [[0,0],[100,40]]}` |
| Rectangle | `{"w": 200, "h": 100}` |
| Circle | `{"r": 50}` |
| Triangle | `{"points": [[50,0],[0,80],[100,80]]}` |

Future types need **no schema change**: `{"rx","ry"}` (ellipse), `{"n","rx","ry"}` (regular
n-gon), `{"points":[…]}` of any length, `{"segments":[…]}` (bezier).

**Format rule (D-60):** store a shape in the most general representation *that same type* will
ever need. Triangle and line are point lists because vertex editing is planned; circle is a single
`r` because a circle never becomes an ellipse — the ellipse is a separate type.

**The invariant that survives:** a move is still type-blind — it changes `x, y` and nothing else,
at any shape complexity, including a 1000-vertex path. `geometry` is neither read nor written by
a move.

**Accepted cost:** jsonb removes *schema* migrations, not *format* migrations. Changing a stored
type's format still rewrites every row of it. Hence: settle a type's format before its first
write, and read defensively (a missing key takes a default, never throws).

---

## CONSTRAINT-normalisation — Canonical order on write
type: invariant
source: docs/DECISIONS.md (D-60, superseding D-41)
status: 🛑 **LARGELY DISSOLVED in v1.11 — and the line landmine is DEFUSED.**

The line's special normalisation arm existed only because the line was the one figure whose four
columns were its endpoints rather than a bounding box. **A line is now stored as its two points,
so there is no axis-sorting step to get wrong.**

What remains: a draw gesture must resolve to an origin (`x, y`) plus a local shape. For box-shaped
gestures the origin is the minimum corner. Applied **once, before the INSERT, in exactly one
place**, as before.

*Keep the historical landmine in mind as a class of bug: a figure that still renders after being
corrupted reports nothing.*

---

## CONSTRAINT-min-size-guard — Per-type draw rejection
type: invariant
source: docs/DECISIONS.md (D-60, superseding D-50)
status: 🛑 **AMENDED in v1.11 — still per-type, but no longer a mirror.**

| Shape | Rejected when |
|---|---|
| Line | both endpoints identical (zero length). **Horizontal and vertical lines are legal.** |
| Rectangle / triangle / circle | width **or** height is zero (circle: radius zero) |

**The rules are unchanged; their authority is not.** There are no geometry CHECK constraints left,
so this guard has no SQL counterpart to agree with, and the 32-case matrix test proving agreement
is obsolete. It is now the **only** thing standing between a malformed shape and the database.
A rejected draw still fails **silently** — no message, no error.

Validation belongs in **one place** alongside the shape's other per-type logic
(`IShapeDefinition`), never copied into each write path.

---

## CONSTRAINT-clamp — The clamp formula, inclusive bounds
type: invariant / nfr
source: docs/DECISIONS.md (D-36; operative spec for D-24 and D-29)

`W = 1472`, `H = 828` *(v1.1; was 1280 × 720)*. **Bounds are INCLUSIVE: valid domain `0..1472 × 0..828`.**

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

🛑 **v1.11: the RULES below are unchanged; the PAYLOAD changes.** `x1, y1, x2, y2` no longer exist
on a figure, and `id` is a `uuid` (D-62). Receiver semantics — update-only, ignore-unknown-id,
echo filter — survive verbatim.

| Kind | Payload | Receiver's action |
|---|---|---|
| `draw` | `{ kind, sender, id, type, x, y, rotation, geometry, style, z }` | Insert or update by `id`. **The only kind that may create a figure.** Still sent *after* the INSERT (D-35/D-39: drawing is not broadcast live). |
| `move` | `{ kind, sender, id, x, y }` | **UPDATE ONLY — never insert.** Unknown figure → **ignore the message entirely.** (Kills the resurrection bug — D-40.) |
| `delete` | `{ kind, sender, id }` | Remove by `id`. Idempotent — deleting an unknown figure is a silent no-op. |
| `rollback` | `{ kind, sender, id, x, y }` | Restore the figure to the given position. Sent when a save fails after all retries (D-52). Applied **update-only**, like `move`. |

- **No `drop` kind.** A drag's final position is simply the last `move` (guaranteed sent by D-47's
  trailing edge), followed by silence.
- **`move` shrank from four numbers to two** — a move is now a translation of the origin and
  nothing else, so each drag frame costs less traffic than in v1.1.
- **`move` carries no `type` and no `geometry`** — neither changes during a drag, and the receiver
  already has them.
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
| Draw a figure | `INSERT` one row into `figures` *(v1.11: with a retry if `z` collides — see CONSTRAINT-schema)* |
| Drag a figure (on drop only) | `UPDATE` that row's **`x, y`** *(v1.11: two columns, not four)* |
| Delete a figure | `DELETE` that row — **hard delete stays** (D-69 declined soft delete) |

- **No Save button anywhere.** No "unsaved changes" state.
- **Postgres sees exactly ONE UPDATE per drag** — intermediate glide positions travel through
  memory only. Writing them would be ~100× write amplification.
- Use `ExecuteUpdateAsync` / `ExecuteDeleteAsync` and check the **affected-row count**. Zero rows
  on UPDATE ⇒ the figure is gone ⇒ silently remove from that tab's view **and broadcast a delete**.
  This is **not an error** — it is expected staleness.
- Save failure: retry ≤ 2 more times **only if transient**. Never retry validation errors, CHECK
  violations, missing figures, or zero-row UPDATEs. On final failure: broadcast `rollback` →
  restore locally → modal → reload the canvas from Postgres on OK.
- 🛑 **v1.11 adds ONE retryable non-transient case:** a unique violation on `(canvas_id, z)` during
  INSERT. It is retried **with a recomputed `z`**, not with the same value. This is the sole
  exception to "never retry constraint violations" and exists because two tabs drawing at the same
  instant legitimately compute the same `z` (D-63).
- The figure's **original position must be retained for the entire drag** to make rollback possible.

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
| Toolbar | **48px** tall, seven controls: `[pointer] [line] [rectangle] [circle] [triangle] [star] [delete]`, logout right-aligned outside the count |

- 2px (not 1px) is deliberate: the stroke is the only click target a line has (D-32 declined a
  widened hit-area).
- **Delete button is greyed out and unclickable when nothing is selected** (D-58).
- 🛑 **v1.11: "no style columns in the database" is obsolete** (D-66). Style is now per-figure in a
  validated `style jsonb`. **The values above are unchanged and still what every figure gets** —
  they become the column defaults, and v1.11 ships **no styling UI**. Nothing changes on screen.
  Validator bounds: colours must match `^#[0-9A-Fa-f]{6}$`, `stroke_width` clamps to **0.5 – 64**,
  `opacity` clamps to 0 – 1.
- 🛑 **v1.11: canvas size is no longer a compile-time constant.** 1472 × 828 becomes the default in
  `canvases.width/height` (D-64). `CanvasBounds` stops being the authority.

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
