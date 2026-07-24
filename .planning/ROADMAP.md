# Roadmap: BlazorCanvas

## Milestones

- ✅ **v1.0 MinVP** — Phases 1–5 (shipped 2026-07-17)
- ✅ **v1.1 Canvas resize · selection UX · no-JS removal** — Phases 6–8 (shipped 2026-07-21)
- 🚧 **v1.11 Storage model rewrite (anchor + geometry JSON)** — Phases 9–10 (in progress, opened 2026-07-23)
- 📋 **v1.2 Figures & dynamic toolbar** — scoped, sequenced after v1.11, not started

v1.0 detail: [`milestones/v1.0-ROADMAP.md`](milestones/v1.0-ROADMAP.md) ·
[`milestones/v1.0-REQUIREMENTS.md`](milestones/v1.0-REQUIREMENTS.md) ·
[`milestones/v1.0-MILESTONE-AUDIT.md`](milestones/v1.0-MILESTONE-AUDIT.md)

v1.1 detail: [`milestones/v1.1-ROADMAP.md`](milestones/v1.1-ROADMAP.md) ·
[`milestones/v1.1-REQUIREMENTS.md`](milestones/v1.1-REQUIREMENTS.md) ·
phase artifacts archived under [`milestones/v1.1-phases/`](milestones/v1.1-phases/)

## Phases

<details>
<summary>✅ v1.0 MinVP (Phases 1–5) — SHIPPED 2026-07-17</summary>

- [x] **Phase 1: Database, Schema & Geometry Core** (6/6 plans) — completed 2026-07-15
      Postgres 17 in Docker, the two-table schema whose CHECKs enforce the geometry, and the tested
      clamp/normalise/circle maths.

- [x] **Phase 2: Login, Session & Logout** (3/3 plans) — completed 2026-07-15
      Static-SSR login, cookie auth with the `user_id` claim, and an authenticated shell surviving F5.

- [x] **Phase 3: The Canvas & Drawing** (5/5 plans) — completed 2026-07-16
      The 1280×720 SVG at (0,48), the six-button toolbar, and drawing all four shapes — persisted.

- [x] **Phase 4: Select, Drag & Delete** (4/4 plans) — completed 2026-07-16
      The three verbs complete: 3px click-vs-drag, edge clamping that slides, and a Delete button.

- [x] **Phase 5: Live Cross-Tab Sync** (5/5 plans) — completed 2026-07-17
      The notifier, the real-time drag glide, and the consistency rules that stop any screen lying.

**Definition of done — met:**

> "I can log in, draw all four shapes, drag and delete them, open the app on a second monitor, and
> watch a figure GLIDE in real time as I drag it on the first — with everything surviving a refresh."

Verified by end-to-end code trace (v1.0 milestone audit) and by live human verification on two real
screens (BC-05-05).

</details>

<details>
<summary>✅ v1.1 Canvas resize · selection UX · no-JS removal (Phases 6–8) — SHIPPED 2026-07-21</summary>

**Milestone goal:** Enlarge the canvas to 1472 × 828 (no migration, no shrink path); fix and restyle
figure selection (armed-tool persistence after a draw, exactly one figure selected at a time, a
topmost blue+white dashed trace replacing the red outline); and formally remove the project's
"no hand-authored JavaScript" constraint (doc-only — no runtime change, no new JS written).

- [x] **Phase 6: Canvas Resize to 1472×828** (1/1 plans) — completed 2026-07-21
      `CanvasBounds`-driven rendering at the enlarged surface, no migration, geometry edge tests
      re-pinned to the new inclusive bounds.

- [x] **Phase 7: Selection Lifecycle & Restyle** (2/2 plans) — completed 2026-07-21
      Armed-tool persistence after drawing, one-selection-at-a-time deselect rules, and a topmost
      blue+white dashed `SelectionTrace` replacing the red outline — human-verified live, including
      the two-tab remote-delete edge.

- [x] **Phase 8: Architecture Constraint Cleanup** (1/1 plans) — completed 2026-07-21
      The retired JavaScript policy reconciled across ADR, project summary, and derived constraint;
      D-06/D-18/D-33/D-37/D-57 motivations corrected to MVP simplicity. Documentation-only.

**Outcome:** 4/4 requirements validated (CANV-03, SEL-01, SEL-02, ARCH-01). All 3 phases verified
`passed`. 11 source/test files changed (+106/−60); `dotnet build` clean, 405/405 tests passing.

</details>

### 🚧 v1.11 Storage model rewrite (anchor + geometry JSON) (In Progress, opened 2026-07-23)

**Milestone goal:** Replace the four-integer bounding-box storage (`x1,y1,x2,y2`, D-22) with the
extensible **anchor (`x,y`) + `geometry jsonb`** model (D-59), so future figure types need no schema
change. Every existing figure is **preserved via a data migration** tested against the immutable
v1.1 fixture. The canvas-edge clamp is **removed**. Adds **no new user-facing feature** — a database
model change plus the downstream code churn it forces.

- [x] **Phase 9: Schema, Entity & Data-Preserving Migration** - The new anchor+geometry `figures` (completed 2026-07-23)
      schema and Figure entity, plus a hand-written backfill of every existing figure verified by a
      round-trip test against the immutable v1.1 fixture.

- [ ] **Phase 10: Geometry, Draw, Drag & Sync Rework (No Edge Clamp) + Regression** - All four shapes
      draw, drag, delete, and sync live on the new model; the canvas-edge clamp is removed; the
      degenerate-draw guard and per-type normalisation are re-expressed in code; the D-53 broadcast
      payload carries anchor+geometry; and the full test suite is reworked and green.

## Phase Details

### Phase 9: Schema, Entity & Data-Preserving Migration

**Goal**: The `figures` table and Figure entity adopt the anchor (`x,y`) + `geometry jsonb` model
(D-59), and every existing figure survives the upgrade with its exact position and appearance
preserved.
**Depends on**: Nothing (first phase of v1.11)
**Requirements**: STOR-01, MIG-01, MIG-02
**Success Criteria** (what must be TRUE):

  1. The live `figures` table matches THE SCHEMA (D-59): `uuid` id (`gen_random_uuid()`), integer
     `x`/`y` anchor columns, `geometry jsonb`, `numeric z`, `type text` + whitelist CHECK, composite
     index `(user_id, z)`, no `created_at`, no CHECK on `geometry` — confirmed against a live
     Postgres database, not merely a green build.

  2. Figures load in `z, id` order via `SELECT * FROM figures WHERE user_id = @id ORDER BY z, id`.
  3. Running the new EF migration against a database seeded from the immutable `v1.1-pre-rewrite.sql`
     fixture leaves every pre-existing figure's position and appearance unchanged — no figure is lost
     or visually altered.

  4. An automated round-trip test compares the migrated rows to `v1.1-pre-rewrite-MANIFEST.md`'s
     expected anchor+geometry values for every one of the four shape types, and passes.
**Plans**: 6/6 plans executed
**Wave 1**

- [x] 09-01-PLAN.md — GeometryCodec: Box ↔ anchor+geometry per type (TDD foundation) [wave 1]
- [x] 09-02-PLAN.md — Port immutable v1.1 fixture + re-derive expected-values MANIFEST [wave 1]

**Wave 2** *(blocked on Wave 1 completion)*

- [x] 09-03-PLAN.md — Storage-model swap (Figure entity + DbContext + FigureStore + SyncMessage + Home.razor), build-green production [wave 2]

**Wave 3** *(blocked on Wave 2 completion)*

- [x] 09-04-PLAN.md — EF AnchorGeometryRewrite migration + hand-written backfill + apply-to-live + live-schema assertion [wave 3]

**Wave 4** *(blocked on Wave 3 completion)*

- [x] 09-05-PLAN.md — Test-suite build-continuity adaptation → full solution green on the new model [wave 4]

**Wave 5** *(blocked on Wave 4 completion)*

- [x] 09-06-PLAN.md — Round-trip migration test against the immutable fixture (MIG-02 proof) [wave 5]

### Phase 10: Geometry, Draw, Drag & Sync Rework (No Edge Clamp) + Regression

**Goal**: All four shapes draw, drag, delete, and sync live on the anchor+geometry model, with the
canvas-edge clamp removed and geometry well-formedness guaranteed in code rather than the database;
the D-53 broadcast payload carries anchor+geometry with unchanged sync semantics; and the full test
suite is reworked to the new model and green on a clean build.
**Depends on**: Phase 9
**Requirements**: STOR-02, STOR-03, STOR-04, STOR-05, SYNC-02, TEST-02
**Success Criteria** (what must be TRUE):

  1. Each of the four shapes (line, rectangle, circle, triangle) renders from its anchor (`x,y`) +
     `geometry` (circle `{r}`, rectangle/triangle `{w,h}`, line `{dx,dy}`) instead of the retired
     bounding box, and looks identical to before. Dragging any figure updates only its `x,y` anchor —
     in the database and in the rendered position; its `geometry` never changes on a move.

  2. A figure may now be drawn or dragged past the canvas edge — no clamping occurs for any shape, on
     draw or on drag. A strictly zero-size draw (a click without a drag) is still rejected in code
     before persistence, for all four shapes — no figure appears, no error is shown.

  3. All three verbs — draw, drag, delete — still work end to end for all four shapes and persist
     per-operation (`INSERT`/`UPDATE`/`DELETE`), including a diagonal line surviving reload without
     flipping to the opposite diagonal.

  4. Live cross-tab sync works on the reworked payload (D-53 amended): a `draw` carries anchor +
     `type` + `geometry` and appears in another tab; a `move` carries only the anchor, is UPDATE-only,
     and is ignored by a receiving tab if the figure is unknown; a figure glides in real time in the
     other tab; mid-drag a tab discards all incoming broadcasts and the 50 ms trailing edge holds; a
     save failure broadcasts an anchor-only rollback, restores locally, and reloads from Postgres.

  5. The test suite is reworked to the new model: schema-shape assertions assert the anchor+geometry+
     `uuid`+`numeric z` model; the old edge-clamp tests are removed or repurposed to assert the new
     no-clamp behaviour; TEST-01's three silent-failure tests are re-evaluated (the circle round-trip
     becomes a `geometry {r}` assertion; the line-normalisation landmine test carries over unchanged).

  6. `dotnet build BlazorCanvas.sln` is clean (0 warnings, 0 errors) and the full `dotnet test` suite
     passes.
**Plans**: 1/6 plans executed

**Wave 1**

- [x] 10-01-PLAN.md — Remove the canvas-edge clamp from the geometry core; re-express MinSizeGuard on
      the geometry primitives (STOR-03, STOR-04, TEST-02) [wave 1]

**Wave 2** *(blocked on Wave 1)*

- [ ] 10-02-PLAN.md — Anchor-only persistence: MoveAsync writes only x,y; drag translates the anchor;
      the move clamp is deleted (STOR-02, STOR-04, STOR-05) [wave 2]

**Wave 3** *(blocked on Wave 2)*

- [ ] 10-03-PLAN.md — D-53 anchor+geometry broadcast payload + a unit-testable SyncReceiver
      (SYNC-02, STOR-05) [wave 3]

**Wave 4** *(blocked on Wave 3)*

- [ ] 10-04-PLAN.md — Render from anchor + geometry via a shared ShapeRender helper, with
      appearance-preservation tests (STOR-02, STOR-05) [wave 4]

**Wave 5** *(blocked on Wave 4)*

- [ ] 10-05-PLAN.md — Test-suite rework to the anchor+geometry model; the D-41 landmine asserted
      through to stored JSON (TEST-02, STOR-02, STOR-03) [wave 5]

**Wave 6** *(blocked on Wave 5)*

- [ ] 10-06-PLAN.md — Clean build, full green suite, and live two-tab verification (STOR-04, STOR-05,
      SYNC-02, TEST-02) [wave 6]

## Progress

| Phase | Milestone | Plans Complete | Status | Completed |
|-------|-----------|----------------|--------|-----------|
| 1. Database, Schema & Geometry Core | v1.0 | 6/6 | Complete | 2026-07-15 |
| 2. Login, Session & Logout | v1.0 | 3/3 | Complete | 2026-07-15 |
| 3. The Canvas & Drawing | v1.0 | 5/5 | Complete | 2026-07-16 |
| 4. Select, Drag & Delete | v1.0 | 4/4 | Complete | 2026-07-16 |
| 5. Live Cross-Tab Sync | v1.0 | 5/5 | Complete | 2026-07-17 |
| 6. Canvas Resize to 1472×828 | v1.1 | 1/1 | Complete | 2026-07-21 |
| 7. Selection Lifecycle & Restyle | v1.1 | 2/2 | Complete | 2026-07-21 |
| 8. Architecture Constraint Cleanup | v1.1 | 1/1 | Complete | 2026-07-21 |
| 9. Schema, Entity & Data-Preserving Migration | v1.11 | 6/6 | Complete    | 2026-07-23 |
| 10. Geometry, Draw, Drag & Sync Rework (No Edge Clamp) + Regression | v1.11 | 1/6 | In Progress|  |

**v1.0: 5/5 phases, 23/23 plans, 15/15 requirements — milestone audit passed.**
**v1.1: 3/3 phases, 4/4 plans, 4/4 requirements — all phases verified `passed`.**
**v1.11: 1/2 phases complete, 3/9 requirements validated — Phase 9 verified `passed`; Phase 10 remains.**

## Requirement Coverage

| Phase | Milestone | Requirements |
|-------|-----------|--------------|
| 1 | v1.0 | DATA-02, TEST-01 |
| 2 | v1.0 | AUTH-01, AUTH-02, AUTH-03 |
| 3 | v1.0 | DATA-01, CANV-01, CANV-02, FIG-01 |
| 4 | v1.0 | FIG-02, FIG-03, FIG-04 |
| 5 | v1.0 | SYNC-01, DATA-03, DATA-04 |
| 6 | v1.1 | CANV-03 |
| 7 | v1.1 | SEL-01, SEL-02 |
| 8 | v1.1 | ARCH-01 |
| 9 | v1.11 | STOR-01, MIG-01, MIG-02 |
| 10 | v1.11 | STOR-02, STOR-03, STOR-04, STOR-05, SYNC-02, TEST-02 |

**v1.0: 15/15 requirements mapped. v1.1: 4/4 requirements mapped. v1.11: 9/9 requirements mapped.
No orphans. No duplicates.**

## What's Next

**v1.11 is open, Phase 9 is complete, and Phase 10 is planned (6 plans, 6 waves).** Next step:
`/gsd-execute-phase 10` — move the running draw/drag/sync behavior fully onto the anchor+geometry
model and remove the canvas-edge clamp. Every wave needs Compose Postgres up
(`docker compose up -d --wait`, host 5433) and `BLAZORCANVAS_TEST_CONNECTION` pointed at
`canvas_phase09`; 10-06 is a blocking human-verification checkpoint on two real screens.

**v1.2 is scoped, sequenced after v1.11:** new figure types (ellipse, 5-point star, hexagon,
pentagon, right-angle triangle L/R, four arrows) + a dynamic split-button toolbar. Full plan:
`.planning/backlog/v1.2-figures-and-toolbar.md`. Its decision amendments happen when v1.2 is kicked
off; its 4-int-bbox premise no longer holds once v1.11 ships and must be revised first — run
`/gsd-new-milestone` to open it after v1.11 closes.

Known v1.0 tech debt (~11 low-severity items from `01-REVIEW.md`) is recorded in
[`milestones/v1.0-MILESTONE-AUDIT.md`](milestones/v1.0-MILESTONE-AUDIT.md). None blocks a
requirement. v1.1 added no new tech debt.

---
*Roadmap created: 2026-07-14 from `docs/DECISIONS.md` (58 locked decisions) via `.planning/intel/`*
*v1.0 archived: 2026-07-17*
*v1.1 archived: 2026-07-21 — phases 6–8 collapsed above; full detail in `milestones/v1.1-ROADMAP.md`.*
*v1.11 roadmap created: 2026-07-23 — Phases 9–10 derived from the 9 v1.11 requirements (D-59); 9/9
requirements mapped, 0 unmapped. No research phase (design complete in D-59; research intentionally
skipped). (Revised from an initial 4-phase draft: the sync-payload rework and the test rework were
folded into Phase 10, since broadcasting lives in the same draw/drag handlers and this project builds
tests alongside code — user decision, 2026-07-23.)*
