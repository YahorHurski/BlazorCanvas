# Context

Non-normative background from `docs/DECISIONS.md`: rationale, rejected alternatives, accepted
costs, and the failure modes the log explicitly warns about. Normative content lives in
`decisions.md` / `constraints.md` / `requirements.md`.

> ⚠️ **v1.1 AMENDMENTS (2026-07-20).** Superseded here (authority: `docs/DECISIONS.md`): **canvas
> is 1472×828** (was 1280×720); **the "no JavaScript" constraint below is REMOVED** — it was never
> the real motivation (MVP simplicity was), and D-06/18/33/37/57 are re-worded. Selection is now a
> blue+white dashed trace with a lifecycle (see PROJECT.md). Keep reading the topic below for
> *history*, but it no longer describes a live constraint.

---

## Topic: What is being built
source: docs/DECISIONS.md (Summary section)

A Blazor Server web app (.NET 10) where a user draws simple geometric figures on a fixed **1472×828**
*(v1.1; was 1280×720)* SVG canvas. Each user has exactly one canvas. Figures can be **drawn, dragged
and deleted** — nothing else.

Stack: Blazor Server (InteractiveServer) · SVG · PostgreSQL via EF Core/Npgsql · Docker Compose for
local Postgres. *(v1.1: the "no JavaScript" rule was removed.)*

Guiding principle: **MinVP — the smallest thing that works. No speculative features.** The source
states that every decision was made explicitly by the user; nothing was defaulted.

---

## Topic: The three landmines (each fails SILENTLY if missed)
source: docs/DECISIONS.md ("The three landmines", D-23, D-36, D-41, D-18, D-43)

1. **Never clamp coordinates individually.** Clamp the *movement delta*, then translate all four
   uniformly. Clamping `x2` alone resizes the figure instead of moving it.
   *(Since the D-22 reversal this now fails LOUDLY for circles — a non-square box violates the CHECK
   immediately. It still fails silently for rectangles.)*
2. **Never normalise a line by sorting its axes.** (0,100)→(100,0) would become (0,0)→(100,100) —
   the opposite diagonal. Swap the whole point pair instead. (Sorting axes *is* correct for
   rectangle / triangle / circle.)
3. **Never use `OffsetX`/`OffsetY`.** Use `PageX`/`PageY`. `OffsetX` is relative to the *event
   target* — and every drag and every selection begins on a figure.

---

## Topic: The one-mouse premise — why there is no concurrency machinery
source: docs/DECISIONS.md (D-11)

One human has one mouse; the OS delivers the pointer to one window. **Two tabs therefore cannot be
edited at the same instant.** This premise deletes an entire category of work: row/advisory locks,
"figure is being edited" flags, drag ownership, conflict-resolution UX, merge prompts, optimistic
concurrency tokens, retry loops, operation queues, write coalescing, CRDT/OT.

"Which tab wins?" is answered by physics, not by code.

The premise has one accepted hole: a **second device** (e.g. a phone — trivially possible with no
real auth) degrades to last-write-wins. This is an **undesigned freebie**; nothing is built for it,
and D-54 partially breaks it (a broadcast arriving mid-drag is lost permanently until F5).

---

## Topic: Interaction vs storage — the distinction the log leans on
source: docs/DECISIONS.md (D-05, D-13, D-22, "Settled: the shape of the geometry")

What the *gesture* supplies is not what the *database* holds:

| Shape | What the gesture gives | What is stored |
|---|---|---|
| Line | 2 endpoints | the 2 endpoints |
| Rectangle | 2 opposite corners | the 2 corners |
| Triangle | 2 corners of its box | the 2 corners of the box |
| **Circle** | **centre (press point) + radius (drag distance)** | **the inscribed square's opposite corners** |

An axis-aligned rectangle needs **2** points, not 3 or 4 — storing more would let the database hold
points that do not form a right angle. **There is no square shape.**

---

## Topic: Why D-22 was reversed (the most important history in the log)
source: docs/DECISIONS.md (D-22, D-24)

The original encoding stored a circle as **centre + rim point**. A circle at (640,100) with r = 90
was stored `(640, 100, 730, 100)` — whose min/max describes a **horizontal line segment**, though
the circle's true extent is 180 × 180. A generic clamp reading the raw columns would let the user
drag it upward until **90 px of circle hung off the top of the canvas**.

Fixing that required a per-type bounding-box function **inside the drag loop** — reintroducing
exactly the circle special case the encoding existed to eliminate, and one that fails *silently*.

Under the inscribed square the same circle is `(550, 10, 730, 190)` — its actual bounding box — and
clamping is blind again. **There is genuinely no type dispatch left** in the move or the clamp.

The log is careful to note: **the reversal was justified by new information, not by changing minds.**
When the bounding-square was first rejected, **D-24 (edge clamping) did not yet exist**, so the
encoding's decisive advantage was invisible. D-24 changed the facts.

**Honest accounting recorded in the source:** the old rim-point encoding made an oval
*unrepresentable by the encoding itself*; the new one makes an oval *excluded by an executable
constraint plus a renderer that can only draw circles*. Weaker in principle, equivalent in practice
— producing an oval would require three independent failures.

---

## Topic: The "no JavaScript" constraint — HISTORY (removed in v1.1)
source: docs/DECISIONS.md (D-06, D-18, D-33, D-37, D-57)

> ⚠️ **v1.1: this constraint is REMOVED.** It was recorded as "load-bearing," but the real
> motivation was **MVP simplicity**, not JS avoidance — the four decisions below were re-worded
> accordingly in `docs/DECISIONS.md`. Removal is permissive (no code changed); it only re-opens the
> parenthesised alternatives as *possible future* decisions. Kept below for history.

Four decisions were once framed as existing *only* because JavaScript was excluded (now re-motivated):
- **D-18** — a 1:1 fixed canvas rather than a scaling `viewBox` *(now: chosen for MVP simplicity; a
  scaling letterbox is technically available but not pursued — the fixed size was enlarged to
  1472×828 instead)*. The fixed **aspect ratio** remains mandatory geometry regardless (else ovals).
- **D-33** — Delete is a toolbar button *(now: MVP simplicity + unambiguous; a Delete-key shortcut
  could be added later)*.
- **D-37** — drag termination via `pointerleave` + a `Buttons` guard *(now: to prevent a hanging
  drag; a `setPointerCapture` upgrade could be added later)*.
- **D-57** — no way to abandon a draw *(now: simply out of MVP scope; Escape-to-cancel could be added
  later)*.

Each of these had a "correct tool" alternative (~5–10 lines of JS) that was consciously rejected to
keep the project JavaScript-free.

---

## Topic: Accepted costs, recorded deliberately
source: docs/DECISIONS.md (D-07, D-18, D-19, D-30, D-32, D-38, D-39, D-51, D-54, D-57, D-36)

- Every drag mouse-move is a SignalR round-trip — invisible on localhost/LAN, laggy on a poor link.
- The canvas does not fill the screen; a smaller window scrolls. Browser zoom is the escape hatch.
- The app **has modes** — you must remember which tool is armed; switching costs a click.
- A stray 1–2 px drag creates a tiny, nearly-invisible figure (the guard rejects only zero-size).
- **Lines have no widened hit area** — selecting one means clicking within ~a pixel (mitigated only
  by the 2px stroke). "This will be felt the first time a line needs deleting."
- Opaque white fill means overlapping figures **fully occlude** each other.
- Drawing is strictly insert → get id → broadcast; the broadcast cannot be optimistic.
- **Pressing near an edge forces a tiny circle** (the circle draw-clamp).
- Overshooting the drag surface commits the drag early.
- **You cannot abandon a draw** — draw it, then delete it.
- Mid-drag broadcasts from another device are lost permanently until F5.
- The circle-as-inscribed-square convention must be learned by anyone reading the table.

---

## Topic: Notable rejected alternatives (do not re-litigate without a new decision)
source: docs/DECISIONS.md (throughout)

- HTML5 `<canvas>` + JS interop (hand-written hit-testing; full-scene redraw)
- Blazor WebAssembly + a Web API project
- A Save button (a second tab's Save would silently erase the first tab's work)
- Timer polling · a hand-rolled SignalR hub · Postgres LISTEN/NOTIFY · `BroadcastChannel` ·
  canvas locking · `CircuitHandler` reconnect plumbing
- Concurrency tokens (`xmin`/rowversion) — provably redundant given the affected-row count
- A three-table users/canvases/figures schema · one JSONB document per user · JSONB geometry ·
  Postgres geometric types · table-per-type
- Centre + rim point (reversed) · a bare radius scalar in `x2` (silently grows circles when dragged
  right) · named `cx, cy, radius` columns (correct, but 3–4 NULLs per row and a separate move path)
- `preserveAspectRatio="none"` (ovals every circle — "the most seductive wrong answer") ·
  `slice`/crop (crops figures out of existence) · normalised 0..1 coordinates (the same oval trap,
  in the database, permanently)
- A client-generated UUID for `figures.id` (carries no creation order)
- A PostgreSQL enum for `type` (would silently invalidate the CHECKs as written)
- Right-click to delete · an eraser mode · a dropdown shape picker · keyboard-only shortcuts
- A uniform "reject any zero-extent draw" rule (would make horizontal and vertical lines impossible
  to draw — silently)
- A separate header row above the toolbar (would change the one constant every coordinate depends on)
- A ~5-line JS shim for `setPointerCapture` (the correct tool; rejected to stay JS-free)
- No tests at all (would leave the three silent failure modes unguarded)

---

## Topic: Open questions
source: docs/DECISIONS.md ("Open questions", "Closed (pre-audit)")

**All Q-01…Q-31 are closed.** The source states: "All audit findings are now closed."
Notably Q-17 ("Should D-22 be reversed?") closed **YES**.

There are **no open questions carried into planning** from this document.

---

## Topic: Provenance of the framework-seam decisions
source: docs/DECISIONS.md (header of the "Framework-seam decisions" section)

D-33…D-58 were added after a zero-context audit by an implementer with no memory of the
conversation. That audit found the log **airtight wherever the human was in the room** (data model,
sync semantics, geometry, scope) and **silent wherever the framework was in the room** (cookies,
HTTP, pointer capture, keyboard focus, schema creation, layout constants, error paths). Every
blocker it found lived in the second category.

This is useful signal for planning: the *product* decisions are settled and deep; the risk in this
project is concentrated in **framework seams and silent geometric bugs**, which is exactly where the
three mandated tests point (D-49).
