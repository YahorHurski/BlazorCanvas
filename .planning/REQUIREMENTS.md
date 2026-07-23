# Requirements: BlazorCanvas — Milestone v1.11

**Defined:** 2026-07-23
**Core Value:** The canvas is always the truth, everywhere at once — what you draw persists instantly,
and every other tab shows it happening live, including a figure gliding in real time as you drag it.

Milestone v1.11 is a **storage-model rewrite** (four-integer bounding box → anchor `x,y` + `geometry
jsonb`) and the downstream code churn it forces. It **adds no new user-facing feature**. All decisions
are already recorded in `docs/DECISIONS.md` — new **D-59** (authoritative storage model); D-22/D-39
superseded, D-24/D-29/D-36 dropped, D-53/D-46/D-23 amended, D-41 re-expressed, D-20/D-12/D-03 upheld.
Backlog: `.planning/backlog/v1.11-storage-rewrite.md`.

REQ-IDs continue from the existing set (v1.0 archived at `.planning/milestones/v1.0-REQUIREMENTS.md`,
v1.1 at `.planning/milestones/v1.1-REQUIREMENTS.md`). `FIG-05` and `TOOL-01` are **reserved for v1.2**
and are deliberately not reused here — the new work uses the `STOR`/`MIG` categories plus `SYNC-02`
and `TEST-02`.

## v1.11 Requirements

Requirements for this milestone. Each maps to exactly one roadmap phase (Traceability below).

### Storage model (STOR)

- [ ] **STOR-01**: `figures` uses the new schema — `uuid` id (`gen_random_uuid()`), anchor `x,y`
  (integer), `geometry jsonb`, `numeric z` (fractional layer order, **no UNIQUE**), `type text` +
  whitelist CHECK, index `(user_id, z)`, **no `created_at`**, **no DB CHECK on `geometry`**. `users`
  is unchanged. Load query is `SELECT * FROM figures WHERE user_id = @id ORDER BY z, id`.
  *(D-59; supersedes D-22/D-39, upholds D-12/D-20/D-46.)*

- [ ] **STOR-02**: A figure's shape lives in `geometry`, stored **relative to the anchor** (circle
  `{r}`, rectangle/triangle `{w,h}`, line `{dx,dy}` of either sign — exact per-type JSON shapes pinned
  at plan/spec time). A drag (move) updates **only `x,y`**, for every shape; the geometry never changes
  on a move. Normalisation is re-expressed per type (rectangle → positive `{w,h}`; line → one endpoint
  + `{dx,dy}`, **swap the whole point pair, never sort axes** — the D-41 landmine carries over).
  *(D-59, D-41.)*

- [ ] **STOR-03**: Geometry well-formedness is guaranteed by **server-side code, not the database**
  (explicit trust boundary — the server is the sole writer, D-09). A strictly-zero-size draw is
  rejected **code-side** (`MinSizeGuard`, on C# primitives before serialisation), producing no figure
  and no error. *(D-59, D-23 #1, D-32.)*

- [ ] **STOR-04**: The **canvas-edge clamp is removed** — a figure may be drawn or dragged past the
  canvas edge. An off-canvas figure is an **accepted, currently-unrecoverable** state (no pan/undo),
  taken deliberately because the roadmap intends to remove canvas bounds entirely later. Drag
  translates the anchor only, with no bounds arithmetic; the circle draw-clamp is gone.
  *(D-24/D-29/D-36 dropped by D-59.)*

- [ ] **STOR-05**: After the rewrite, all four shapes (line, rectangle, circle, triangle) still
  **draw, drag, and delete** and persist per-operation, behaving as in v1.1 **except** for STOR-04
  (no edge clamp). Regression guard — the three verbs (D-04) and immediate persistence (D-09) are
  preserved on the new model. *(D-04, D-09.)*

### Migration (MIG)

- [ ] **MIG-01**: All existing figures are **preserved via a data migration** — the row transform
  `x1,y1,x2,y2 → x,y,geometry` per type runs as part of the EF migration, so every pre-existing figure
  keeps its position and on-screen appearance after the upgrade. No figure is lost or visually altered.
  *(D-59.)*

- [ ] **MIG-02**: The migration/backfill is verified by an **automated round-trip test** against the
  immutable v1.1 fixture (`tests/.../Fixtures/v1.1-pre-rewrite.sql` + its `…-MANIFEST.md` expected
  values) — the transformed rows match the manifest's expected anchor+geometry for every type.
  *(D-59, D-49.)*

### Sync (SYNC)

- [ ] **SYNC-02**: Live cross-tab sync works on the new model — the D-53 broadcast payload carries
  **anchor + geometry** with a `uuid` id (`draw`: anchor + `type` + `geometry`; `move`/`rollback`:
  anchor only). Semantics are unchanged: `draw` is the only kind that may create a figure, `move` is
  UPDATE-only (unknown figure ignored), mid-drag discards all incoming (D-54), and the 50 ms
  trailing-edge throttle holds (D-47). A drag still glides in real time in the other tab.
  *(D-53 amended by D-59; D-40/D-47/D-54 unchanged.)*

### Tests (TEST)

- [ ] **TEST-02**: The test suite is reworked to the new model — schema-shape assertions updated to
  the anchor+geometry schema, the edge-clamp tests removed or repurposed, the migration round-trip test
  (MIG-02) added, and TEST-01's three silent-failure tests re-evaluated (the circle inscribed-square
  round-trip becomes a `geometry {r}` assertion; the line swap-pair landmine test carries over). The
  build stays clean and the full suite passes. *(D-49, D-59.)*

## v2 Requirements (deferred)

Tracked but **not** in this milestone's roadmap.

### Figures / toolbar — v1.2 (parked)

- **FIG-05**: New figure types — ellipse, 5-point star, hexagon, pentagon, right-angle triangle
  (left/right variants), four arrows. *(Now sequenced **after** v1.11; the v1.2 backlog
  `.planning/backlog/v1.2-figures-and-toolbar.md` must be **revised** first — its 4-int-bbox premise
  no longer holds. New shapes will store as `geometry jsonb`, which is exactly what v1.11 enables.)*
- **TOOL-01**: A dynamic split-button toolbar to accommodate the expanded figure set. *(v1.2.)*

### Storage follow-ons (deferred)

- **STOR-F1**: Off-canvas figure recovery (pan, or a "bring back on-canvas" affordance) — deferred;
  the roadmap intends to remove canvas bounds entirely later, at which point recovery is designed
  properly. *(Accepted risk for v1.11 — STOR-04.)*
- **STOR-F2**: Multi-canvas per user (a `canvases` table + `figures.canvas_id` + backfill + per-canvas
  load/sync scope). A cheap additive migration when wanted; not now (D-12/D-03 upheld).

## Out of Scope

Explicitly excluded from v1.11. Documented to prevent scope creep.

| Feature | Reason |
|---------|--------|
| Any new user-facing feature / new figure types / toolbar change | v1.11 is a storage rewrite only; figure set and UI are unchanged. New figures are v1.2. |
| Off-canvas figure recovery UI (pan / undo / bring-back) | Accepted risk this milestone (STOR-04); designed later when canvas bounds are removed. |
| Revising the v1.2 backlog | Deferred — happens as the first step of `/gsd-new-milestone` for v1.2 (user decision, 2026-07-23). |
| Multi-canvas / a `canvases` table | D-12/D-03 upheld; a cheap additive migration when actually wanted. |
| Dropping the `type` whitelist CHECK during the migration | Rejected (D-59) — belt-and-braces is most valuable *through* the bulk backfill; revisit only if `type` becomes a C# enum, and not during a migration. |
| A DB CHECK on `geometry` | Rejected (D-59) — the server is the sole writer; per-type jsonb CHECKs would guard only manual SQL / bad migrations. |
| A `figure_types` lookup table | Rejected (D-59) — a new type always needs a code deploy, so "add a row not a migration" is illusory ceremony (D-12). |
| Shrinking the canvas | Still forbidden (D-19) — the surface may grow, never shrink. |
| Everything locked out by D-04/D-14/D-08 | resize / rotate / undo/redo / z-order UI / multi-select / colours / real auth — unchanged. |

## Traceability

Which phases cover which requirements. Populated during roadmap creation.

| Requirement | Phase | Status |
|-------------|-------|--------|
| STOR-01 | Phase [N] | Pending |
| STOR-02 | Phase [N] | Pending |
| STOR-03 | Phase [N] | Pending |
| STOR-04 | Phase [N] | Pending |
| STOR-05 | Phase [N] | Pending |
| MIG-01 | Phase [N] | Pending |
| MIG-02 | Phase [N] | Pending |
| SYNC-02 | Phase [N] | Pending |
| TEST-02 | Phase [N] | Pending |

**Coverage:**

- v1.11 requirements: 9 total
- Mapped to phases: 0 (filled by roadmap)
- Unmapped: 9 ⚠️ (until roadmap runs)

---
*Requirements defined: 2026-07-23*
*Last updated: 2026-07-23 after initial definition (`/gsd-new-milestone`).*
