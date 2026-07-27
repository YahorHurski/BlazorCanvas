# Phase 9: Schema, Entity & Data-Preserving Migration - Context

**Gathered:** 2026-07-23
**Status:** Ready for planning
**Source:** Interactive discussion (this session) resolving D-59's five "Left for plan / spec time"
items, grounded against the live codebase (`Figure.cs`, `CanvasDbContext.cs`, `FigureShape.razor`)
and `docs/DECISIONS.md` (D-59 + THE SCHEMA).

> **Why this CONTEXT.md exists.** D-59 locks the storage *model* but explicitly defers five
> implementation decisions to plan time (JSON shape per type, `z` formula, uuid v4/v7, `Figure.Type`
> C# representation, backfill mechanism). All five were decided **by the user** on 2026-07-23 and are
> recorded below as the binding contract for this phase. The per-type geometry shapes were verified
> against the current SVG renderer so the migration cannot alter any figure's appearance.

<domain>
## Phase Boundary

**What this phase delivers:** the `figures` table and `Figure` entity adopt the anchor (`x,y`) +
`geometry jsonb` model of D-59, and **every existing figure survives the upgrade** with its exact
position and appearance preserved — proven by an automated round-trip test against the immutable
v1.1 fixture.

In scope:
1. **New `figures` schema (D-59)** — `uuid id DEFAULT gen_random_uuid()`, integer `x`/`y` anchor,
   `geometry jsonb`, `numeric z` (**no UNIQUE**), `type text` + whitelist CHECK, composite index
   `(user_id, z)`, **no `created_at`**, **no DB CHECK on `geometry`**. `users` is unchanged.
2. **`Figure` entity rewrite** — drop `X1/Y1/X2/Y2`; add `X`, `Y`, `Geometry`, `Z`; `Id` `int → Guid`;
   `Type` **stays `string`**. `CanvasDbContext.OnModelCreating`: new columns; **remove** the three
   geometry CHECKs (`circle_is_a_circle`, `box_is_a_box`, `line_is_a_line`); **keep**
   `figures_type_is_known`; map `geometry` → `jsonb`; composite index `(user_id, z)`; rewrite the
   table `COMMENT`.
3. **EF migration + hand-written data backfill** — an auto-generated migration handles the schema
   delta; the row transform `x1,y1,x2,y2 → x,y,geometry` per type is written as
   `migrationBuilder.Sql(...)` inside the migration (atomic with the schema change), and assigns `z`
   from the old integer `id` order so layer order is preserved 1:1.
4. **The immutable v1.1 fixture + MANIFEST + round-trip test (MIG-02)** — a `v1.1-pre-rewrite.sql`
   snapshot of the pre-rewrite database plus its `…-MANIFEST.md` of expected post-migration values,
   and an automated test that runs the migration against the fixture and asserts every figure's
   migrated anchor+geometry matches the manifest, for all four shape types.
5. **The load query** `SELECT * FROM figures WHERE user_id = @id ORDER BY z, id`.

**Not in this phase (Phase 10):** the draw / drag / delete behavioural rework, **removing the
canvas-edge clamp**, re-expressing `MinSizeGuard` and per-type normalisation in the pointer handlers,
the D-53 **sync-payload** rework, and the broader test-suite rework (schema-shape assertions beyond
what this phase's migration test needs, edge-clamp test removal, TEST-01 re-evaluation). This phase
lays the storage foundation; Phase 10 moves the running behaviour onto it.

**Build-continuity note (key sequencing risk for the planner).** Changing the `Figure` entity
(`X1..Y2 → X,Y,Geometry,Z`, `Id int→Guid`) breaks every current reader of the old columns
(`FigureShape.razor`, `FigureStore`, `Home.razor`, `SyncMessage`, `Movement`, `Normalisation`). The
phase must not end on a red build. The planner decides the sequencing — either adapt the read paths
minimally to the new model *without* the Phase-10 behavioural rework, or introduce the new
model/persistence path so the solution still compiles and `dotnet test` runs. Flag and resolve this
explicitly; the plan-checker should reject a plan that leaves the build broken.

</domain>

<decisions>
## Implementation Decisions

Canonical decisions are **Locked** in `docs/DECISIONS.md`; each `D-NN` below is the tracked contract
and must be visible in a plan. The five sub-bullets under D-59 are the **user's 2026-07-23
resolutions** of D-59's "Left for plan time" items — binding for this phase.

### Storage model
- **D-59 — Anchor (`x,y`) + `geometry jsonb` storage model** supersedes the four-integer bbox
  (D-22). A figure is an anchor plus a JSON form **relative to the anchor**; a drag moves only `x,y`.
  Implement schema from **THE SCHEMA** in `docs/DECISIONS.md`. The five resolutions for this phase:
  - **Per-type JSON geometry (verified against the SVG renderer, appearance-preserving):**
    - `rectangle` → anchor = top-left `(x1,y1)`; `geometry = {"w": x2-x1, "h": y2-y1}` (positive).
    - `triangle` → anchor = bbox top-left `(x1,y1)`; `geometry = {"w": x2-x1, "h": y2-y1}` (renderer
      reconstructs apex `((x+w/2), y)`, base `(x, y+h)`-`(x+w, y+h)`, isosceles, upward — D-21).
    - `circle` → anchor = **centre** `(x1+r, y1+r)`; `geometry = {"r": (x2-x1)/2}`.
    - `line` → anchor = first endpoint `(x1,y1)`; `geometry = {"dx": x2-x1, "dy": y2-y1}` with `dx ≥ 0`
      by normalisation and `dy` of **either sign**; the D-41 "swap the WHOLE point pair, never sort
      axes" landmine carries over ( (0,100)→(100,0) must not flip to the opposite diagonal ).
  - **`z` assignment:** append `z = max(z) + step` for new figures; the migration assigns `z` to
    existing rows by ascending old integer `id` (order preserved 1:1). `numeric`, no `UNIQUE`, so
    future insert-between (midpoint) stays possible; there is no z-order UI yet (D-04).
  - **`id` = `uuid` v4** via `gen_random_uuid()` (the simple default already in THE SCHEMA).
  - **`Figure.Type` stays `string`** (not a C# enum) — upholds D-46; avoids an enum↔text converter
    that must track the `type` whitelist CHECK. The separate `FigureType` enum keeps serving the
    render/logic layer.
  - **Backfill = `migrationBuilder.Sql(...)`** inside the EF migration — atomic in one transaction
    with the schema change, rolls back on error, and is exercised by the MIG-02 round-trip test.
- **D-12 — Two tables only (`users`, `figures`); no `canvases` table.** "The canvas" is the set of a
  user's figures. Every `figures` column is replaced, but the table count is unchanged.
- **D-20 — Anchor `x, y` are `integer` columns.** Pixels at 1:1; no sub-pixel, no zoom.
- **D-46 — `type` is `text` + whitelist CHECK; no `created_at`.** The whitelist CHECK
  (`figures_type_is_known`) is **kept** through the backfill; `Type` remains a C# `string` so the
  CHECK (written `type <> 'circle'`) can never be silently invalidated.
- **D-09 — The server is the sole writer; geometry well-formedness is guaranteed in code, not the DB.**
  Recorded as an explicit trust boundary: there is deliberately **no DB CHECK on `geometry`**.
- **D-42 — Schema via EF Core migrations; CHECKs and the table COMMENT configured explicitly** in
  `OnModelCreating` (EF emits none of them on its own).
- **D-49 — Round-trip test against the immutable fixture** (`tests/.../Fixtures/v1.1-pre-rewrite.sql`
  + `…-MANIFEST.md`) proves the backfill is lossless for every shape type (MIG-02). The fixture is
  **immutable** — captured before the rewrite, never regenerated or hand-edited afterwards.

### Claude's Discretion
- C# names, namespaces, and method signatures for the new entity fields, the persistence path, and
  the migration/backfill helpers.
- The exact `z` `step` value and the numeric precision/scale mapping for `z`.
- EF migration file naming and how the `geometry` jsonb column is mapped (owned type vs raw `jsonb`
  string) — provided the stored JSON matches the per-type shapes above exactly.
- Test framework details, fixture-loading mechanism, and how a disposable Postgres database is
  provisioned for the round-trip test.
- **How the fixture + MANIFEST are obtained:** a real captured pre-rewrite snapshot already exists on
  branch `Milestone-v1.11` (commit `de89dcd`, plan BC-09-03) and can be **ported** rather than
  re-captured — but the MANIFEST's *expected post-migration values* MUST be re-derived/verified
  against the per-type JSON shapes decided above (esp. circle anchor = centre, line `{dx,dy}`), not
  trusted blindly, since that branch's later decisions (D-60…D-69) may differ. Porting the raw
  pre-rewrite `.sql` (old 4-int schema) is safe; the manifest is the part to re-check.

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

- `docs/DECISIONS.md` — read **§THE SCHEMA** (the canonical v1.11 DDL — implement from it), **D-59**
  (the full storage-model entry incl. "Left for plan time", now resolved above), and the amended
  **D-22 / D-24 / D-29 / D-36 / D-39 / D-41 / D-46 / D-53** status notes for supersession context.
- `.planning/intel/constraints.md` — the normalised constraint projection (schema, geometry,
  normalisation, min-size guard) — cross-check the per-type maths.
- `.planning/ROADMAP.md` — **Phase 9** section: goal + four success criteria (schema shape against a
  live DB, the `z, id` load order, migration preserves every figure, the round-trip test).
- `.planning/REQUIREMENTS.md` — **STOR-01** (the new schema), **MIG-01** (data-preserving migration),
  **MIG-02** (round-trip test against the immutable fixture).
- Current code the migration/entity must stay faithful to: `src/BlazorCanvas/Data/Figure.cs`,
  `src/BlazorCanvas/Data/CanvasDbContext.cs`, `src/BlazorCanvas/Components/Canvas/FigureShape.razor`
  (the renderer that fixes each type's appearance), `src/BlazorCanvas/Geometry/CircleEncoding.cs`.

</canonical_refs>

<specifics>
## Specific Ideas

- **Appearance preservation is the acceptance bar for MIG-01.** The per-type JSON shapes above were
  read back off the live renderer: rectangle `<rect x y w h>`, circle `<circle cx=x1+r cy=y1+r
  r=(x2-x1)/2>`, triangle apex-top-centre / base-bottom from the bbox, line endpoints `(x1,y1)-(x2,y2)`.
  A migrated figure must render pixel-identical.
- **Circle is the one anchor that is the centre**, not a corner — because `{r}` is only meaningful
  relative to a centre. Old storage put the circle in an inscribed square with even diameter, so
  `r = (x2-x1)/2` is an exact integer and `cx = x1+r`, `cy = y1+r` are exact — no rounding on migrate.
- **The line landmine carries into the transform:** normalise by swapping the whole endpoint pair,
  never by sorting `x` and `y` independently; the fixture must include a down-right diagonal so the
  round-trip test would catch a flipped line.
- **Build the migration and its round-trip test together** — the phase goal ("every existing figure
  survives") is only *proven* by the test running the real migration against the immutable fixture.
- **`z` for existing rows must reproduce the old order exactly** — old `id` was the z-order (D-39);
  the backfill assigns `z` ascending by old `id` so nothing re-layers.

</specifics>

<deferred>
## Deferred Ideas

Out of this phase, decided and recorded — do not build here (Phase 10 unless noted):

- **Removing the canvas-edge clamp** and the off-canvas drag/draw behaviour (STOR-04, D-24/D-29/D-36
  dropped). Phase 9 changes storage only.
- **Draw / drag / delete rework** on the new model, `MinSizeGuard` re-expression, per-type
  normalisation in the pointer handlers (STOR-02, STOR-03, STOR-05). *Phase 10.*
- **The D-53 sync-payload rework** to carry anchor + geometry (SYNC-02). *Phase 10.*
- **The broader test-suite rework** — edge-clamp test removal/repurpose, schema-shape assertion
  rewrite beyond the migration test, TEST-01 re-evaluation (TEST-02). *Phase 10.*
- **Off-canvas figure recovery** (pan / bring-back) — accepted-risk follow-on STOR-F1. *Later.*
- **Multi-canvas / a `canvases` table** — D-12/D-03 upheld; a cheap additive migration when wanted.

</deferred>

<scope_fence>
## Scope Fence

**Phase 9 is done when the four ROADMAP success criteria hold — the live schema matches D-59, figures
load `ORDER BY z, id`, the migration preserves every fixture figure, and the round-trip test passes —
and `dotnet build` is clean.**

A plan is **out of bounds** if it:
- removes the canvas-edge clamp or changes any draw/drag/delete behaviour (that is Phase 10);
- reworks the D-53 sync/broadcast payload;
- adds a DB CHECK on `geometry`, a `canvases` table, a `created_at` column, or drops the `type`
  whitelist CHECK during the migration;
- makes `Figure.Type` a C# enum, or a PostgreSQL enum;
- stores a circle as anything other than `{r}` about a centre anchor, or normalises a line by sorting
  its axes independently;
- **regenerates or edits the immutable `v1.1-pre-rewrite.sql` fixture** after capture;
- leaves `dotnet build` / `dotnet test` broken at phase end.

</scope_fence>

---

*Phase: BC-09-schema-entity-data-preserving-migration*
*Context gathered: 2026-07-23 via interactive discussion resolving D-59's plan-time items.*
