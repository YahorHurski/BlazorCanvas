# Requirements

**Provenance note:** the ingest set contains **no PRD**. The single source is an ADR set
(`docs/DECISIONS.md`). The requirements below are **derived** from locked decisions — every one
traces to a specific D-number. Nothing here is invented: where the source document does not state
an acceptance criterion, none is asserted.

Because there is only one source document, there are **no competing acceptance variants**.

> ⚠️ **v1.1 AMENDMENTS (2026-07-20)** — these v1.0-derived requirements are partly superseded.
> Authority: `docs/DECISIONS.md` + `.planning/PROJECT.md` (Requirements → Active). Changed:
> **canvas 1472×828** (was 1280×720); **selection = blue+white dashed trace on the figure's own
> outline** + a **selection lifecycle** (tool stays armed, one selection at a time, deselect on
> canvas-outside-figure / arm-tool / toolbar-except-Delete); **the no-JS requirement is dropped**
> (hand-authored JS permitted). New v1.1 REQ-IDs (CANV-03, SEL-01, SEL-02, ARCH-01) are in PROJECT.md.

---

## REQ-login
source: docs/DECISIONS.md (D-17, D-08, D-44, D-34, D-51, D-58)
A single username + password form at `/login` (static SSR).

Acceptance:
- Existing username + correct password → the user's figures load.
- Existing username + wrong password → an error is shown.
- Unknown username → the user is created and an empty canvas opens. No separate Register page.
- Usernames are trimmed, lowercased/compared case-insensitively, UNIQUE, and non-empty.
- Passwords are non-empty, stored and compared as **plaintext**.
- On success the handler calls `SignInAsync` and redirects to the interactive canvas.

## REQ-session
source: docs/DECISIONS.md (D-26, D-34, D-51)
A session cookie (no expiry) identifies the user.

Acceptance:
- Log in once → every tab in that browser is authenticated.
- **F5 keeps the user logged in.**
- Closing the browser logs the user out.
- The numeric `user_id` is carried in the cookie as a claim; the canvas circuit reads it with **no
  database lookup on page load**.
- An unauthenticated visitor to `/` is redirected to `/login`.

## REQ-logout
source: docs/DECISIONS.md (D-25, D-56, D-51)
A Logout control clears the session and returns to the login form.

Acceptance:
- Right-aligned in the 48px toolbar strip, visually separated from the six tool buttons.
- Implemented as an HTML form posting to `POST /logout` (not an interactive button).
- After logout, a different user can log in and sees only their own figures.

## REQ-one-canvas-per-user
source: docs/DECISIONS.md (D-03, D-12, D-39)
Each user has exactly one canvas: the set of figures with their `user_id`.

Acceptance:
- No canvas list, no "new canvas", no naming, no switching. *(Still true in v1.11 — the `canvases`
  table exists, but exactly one row per user is created and there is no UI to make more.)*
- 🛑 *(v1.11)* Load query is `SELECT * FROM figures WHERE canvas_id = @id ORDER BY z`
  *(was `WHERE user_id = @id ORDER BY id`)*.
- User A cannot see user B's figures.

## REQ-canvas-surface
source: docs/DECISIONS.md (D-06, D-18, D-19, D-43, D-55, D-38)
A fixed **1472 × 828** *(v1.1; was 1280 × 720)* SVG canvas at document position (0, 48), rendered at 1:1.

Acceptance:
- One canvas unit = one CSS pixel on every screen; the canvas does not scale to the window.
- Anchored top-left, no `margin: auto`, **no CSS border** on the SVG.
- Canvas is white; the page background is light grey (this contrast is what makes the edge visible).
- Coordinates derive as `canvasX = PageX`, `canvasY = PageY − 48`, using `PageX/PageY` only.
- A circle never renders as an oval on any screen size.

## REQ-toolbar
source: docs/DECISIONS.md (D-16 superseded, D-30, D-33, D-31, D-58)
A six-button toolbar: `[pointer] [line] [rectangle] [circle] [triangle] [delete]`.

Acceptance:
- Exactly six buttons. The armed button stays visibly active.
- The **pointer tool is armed on page load**.
- The **Delete button is greyed out and unclickable when nothing is selected**.
- Logout is present in the strip but is not one of the six.

## REQ-draw-figure
source: docs/DECISIONS.md (D-04, D-05, D-13, D-21, D-35, D-29, D-36, D-41, D-50, D-09, D-39)
With a shape tool armed, dragging on the canvas draws that shape — **even on top of existing figures**.

Acceptance:
- Four types: line, rectangle, circle, triangle.
- Line / rectangle / triangle are drawn **corner-to-corner**; the **circle is drawn centre-out**
  (press = centre, drag distance = radius).
- The triangle's apex is top-centre, base along the bottom (always isosceles, always upward).
- **Live preview** under the cursor while dragging; the preview is local, never broadcast.
- Drawing clamps at the canvas edge (D-36), including the circle draw-clamp
  `r = min(round(distance), cx, cy, W − cx, H − cy)`.
- Coordinates are normalised on write (per-type — see CONSTRAINT-normalisation) and stored as four
  integers that are always the bounding box.
- Degenerate draws are rejected **silently** per the per-type guard (D-50); a horizontal or vertical
  **line must still be drawable**.
- Completion is `INSERT` → get `id` → broadcast `draw`. The broadcast cannot be fired optimistically.

## REQ-select-figure
source: docs/DECISIONS.md (D-15, D-30, D-31, D-38, D-48, D-58)
With the pointer tool armed, clicking a figure selects it.

Acceptance *(v1.1 amended — SEL-01/SEL-02)*:
- The selected figure is marked by a **~1px blue+white dashed trace on its own outline**, drawn as
  the **topmost layer** (`pointer-events:none`), visible even when it slides behind a larger figure
  *(v1.1; was a red 2px outline)*.
- **At most one figure is selected at a time.** **Drawing a figure selects it** and **the tool stays
  armed**; the just-drawn figure stays selected until the next gesture.
- **Deselect triggers:** pressing the canvas outside the selected figure; arming any tool; pressing
  the toolbar **except the Delete button**. Pressing another figure selects that one instead.
- Overlapping figures: a click hits the **topmost**, which is whichever was drawn last (z-order = `id`).
- A click is a movement of **< 3 px** before release, and performs **no database write**.
- Selection is **local UI state only** — never persisted, never broadcast; a figure arriving via sync
  is not selected in the receiving tab.

## REQ-drag-figure
source: docs/DECISIONS.md (D-04, D-24, D-36, D-37, D-48, D-09, D-10, D-52)
With the pointer tool armed, dragging a figure moves it.

Acceptance:
- Movement of **≥ 3 px** is a drag; starting a drag also selects, and it stays selected after the drop.
- The figure **stops at the canvas edge and slides along it** (per-axis independent delta clamp).
- Exactly **one `UPDATE` on drop** — no intermediate writes.
- `pointerleave` on the drag surface commits the drag at its current clamped position; a
  `Buttons`-up check on `pointermove` also commits (the Alt-Tab case). *(v1.1: this exists to prevent
  a hanging/stranded drag, not to avoid JS.)*
- A zero-row UPDATE means the figure is gone: remove it from the view silently **and broadcast a delete**.

## REQ-delete-figure
source: docs/DECISIONS.md (D-04, D-33, D-15, D-58, D-09)
Select a figure, then click the toolbar Delete button.

Acceptance:
- No Delete-key handler exists *(chosen for MVP simplicity + unambiguous behaviour; a pure-Blazor
  `@onkeydown` breaks when focus moves to a toolbar button. v1.1: a Delete-key shortcut could be added
  later now that JS is permitted — out of scope for now)*.
- Deleting issues `DELETE` on that row; deleting an already-gone figure is a silent no-op.
- The button is disabled whenever nothing is selected.

## REQ-persistence
source: docs/DECISIONS.md (D-09, D-42, D-46, D-39, D-12)
Every operation writes to PostgreSQL the moment it completes. **There is no Save button.**

Acceptance:
- Draw → INSERT; drag (on drop) → UPDATE; delete → DELETE.
- Schema is created by EF Core migrations applied automatically at startup, including the CHECK
  constraints and the `COMMENT ON TABLE` (they must be configured explicitly).
- 🛑 *(v1.11)* **Four tables** (`users`, `canvases`, `figures`, `figure_types`); `created_at`
  returns (D-68) *(was: two tables, no `canvases`, no `created_at`)*.
- 🛑 *(v1.11)* After F5, figures reload by `ORDER BY z`, preserving z-order *(was `ORDER BY id`)*.
  The observable requirement is unchanged — only its mechanism.

## REQ-live-sync
source: docs/DECISIONS.md (D-11, D-40, D-47, D-53, D-54, D-36, D-51)
Changes in one tab appear live in the same user's other tabs; a drag **glides in real time**.

Acceptance:
- A DI singleton notifier keyed by `user_id`; tabs subscribe on init and **unsubscribe in `Dispose()`**.
- Message kinds exactly: `draw`, `move`, `delete`, `rollback` (see CONSTRAINT-messages). No `drop` kind.
- **`move` is UPDATE-ONLY and never inserts** — an unknown figure means ignore the message entirely.
- Drag broadcasts throttled to 50 ms with a **guaranteed trailing edge** (the final position is
  always sent before the drop).
- Order is **clamp → render → broadcast**; a raw unclamped position is never broadcast.
- A tab ignores its own broadcasts (echo filter on the per-circuit `sender` GUID).
- **Mid-drag, a tab discards all incoming broadcasts.**
- **Draw previews are never broadcast** — only the finished figure, via `draw`.
- No locking, no concurrency tokens, no merge UI (the one-mouse premise makes conflicts impossible).

## REQ-staleness-guard
source: docs/DECISIONS.md (D-10, D-40)
A stale tab must never operate on a figure that no longer exists.

Acceptance:
- `ExecuteUpdateAsync`/`ExecuteDeleteAsync` affected-row counts are checked on every write.
- A zero-row UPDATE silently removes the figure from that tab's view **and broadcasts a `delete`**
  so any tab that acquired a ghost drops it too.
- No message, no prompt, no merge. F5 is the documented manual fallback; no Reload button is built.

## REQ-save-failure
source: docs/DECISIONS.md (D-45, D-52)
A failed save must never leave the screen lying about the database.

Acceptance:
- Transient failures retry up to 2 additional times with short delays.
- **Non-transient failures are never retried** (validation, CHECK violations, missing figure,
  zero-row UPDATE).
- On final failure: broadcast `rollback` with the **original** coordinates → restore locally → show
  the modal "The change could not be saved. The canvas will be reloaded from the database." → on OK,
  reload from PostgreSQL.
- The original coordinates are retained for the entire drag so this is possible.
- The app stays alive; the circuit does not crash.

## REQ-tests
source: docs/DECISIONS.md (D-49)
A small test project covering exactly the three **silent** failure modes.

Acceptance:
- Clamp maths (D-36): per-axis independence, inclusive bounds `0..1472 × 0..828` *(v1.1; was `0..1280 × 0..720`)*, circle draw-clamp.
- Circle inscribed-square round-trip (D-22): centre and radius return exactly after store + reload;
  translation preserves the radius.
- Line normalisation (D-41): an up-and-right diagonal does **not** come back as the opposite diagonal.

---

## Out of scope (explicitly locked out — D-04, D-14, D-08)

resize · rotate · undo/redo · z-order control · colours / stroke styling · multi-select ·
copy/paste · zoom / pan · export · real authentication · password hashing · a Save button ·
a Reload button · a canvas list. *(v1.1: "JavaScript of any kind" is **no longer** out of scope —
the no-JS rule was removed; hand-authored JS is permitted, just not currently needed.)*

Anything not named in `docs/DECISIONS.md` is out until it is added there by name. *(v1.1 added:
canvas resize, selection lifecycle + restyle, no-JS removal. v1.2 scoped: new figures + toolbar —
see `.planning/backlog/`.)*
