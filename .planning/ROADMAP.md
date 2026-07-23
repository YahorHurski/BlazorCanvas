# Roadmap: BlazorCanvas

## Milestones

- ✅ **v1.0 MinVP** — Phases 1–5 (shipped 2026-07-17)
- ✅ **v1.1 Canvas resize · selection UX · no-JS removal** — Phases 6–8 (shipped 2026-07-21)
- ✅ **v1.11 Storage Model Rewrite** — Phases 9–12 (shipped 2026-07-22)
- 🚧 **v1.12 Five-pointed star** — Phases 13–17 (in progress)
- 📋 **v1.2 Figures & dynamic toolbar** — scoped, waits behind v1.12

v1.0 detail: [`milestones/v1.0-ROADMAP.md`](milestones/v1.0-ROADMAP.md) ·
[`milestones/v1.0-REQUIREMENTS.md`](milestones/v1.0-REQUIREMENTS.md) ·
[`milestones/v1.0-MILESTONE-AUDIT.md`](milestones/v1.0-MILESTONE-AUDIT.md)

v1.1 detail: [`milestones/v1.1-ROADMAP.md`](milestones/v1.1-ROADMAP.md) ·
[`milestones/v1.1-REQUIREMENTS.md`](milestones/v1.1-REQUIREMENTS.md) ·
phase artifacts archived under [`milestones/v1.1-phases/`](milestones/v1.1-phases/)

v1.11 detail: [`milestones/v1.11-ROADMAP.md`](milestones/v1.11-ROADMAP.md) ·
[`milestones/v1.11-REQUIREMENTS.md`](milestones/v1.11-REQUIREMENTS.md) ·
[`milestones/v1.11-MILESTONE-AUDIT.md`](milestones/v1.11-MILESTONE-AUDIT.md) ·
phase artifacts archived under [`milestones/v1.11-phases/`](milestones/v1.11-phases/) ·
design: `docs/DATA-MODEL-v1.11-DRAFT.md` · decisions: `docs/DECISIONS.md` → D-59…D-69

v1.12 requirements: [`REQUIREMENTS.md`](REQUIREMENTS.md) · decisions land from `docs/DECISIONS.md`
→ D-70 onward

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

<details>
<summary>✅ v1.11 Storage Model Rewrite (Phases 9–12) — SHIPPED 2026-07-22</summary>

**Milestone goal:** Replace the four-integer bounding-box storage model with the position/shape
split (D-59…D-69) — `x, y, rotation` for where a figure is, `geometry jsonb` in local coordinates
for what it is — migrating every existing figure intact, so the schema never has to change again for
a feature and the user sees no difference at all.

> **The governing invariant: this milestone succeeds invisibly.** Feature scope untouched — still
> draw, drag, delete; still four shapes. Any user-observable difference from v1.1 is a defect.

- [x] **Phase 9: Shape Registry & Validation Gateway** (6/6 plans) — completed 2026-07-22
      Every type-specific rule behind one `IShapeDefinition`, one validation gateway for client
      geometry and style, and the one-shot v1.1 database dump banked as a test fixture.

- [x] **Phase 10: Storage Schema, Migration & Persistence Layer** (6/6 plans) — completed 2026-07-22
      The four-table schema, the position/shape split, and a new persistence layer — additive,
      proven in isolation before any application code was touched.

- [x] **Phase 11: Renderer, Sync & Cutover** (5/5 plans) — completed 2026-07-22
      `Home.razor`, the renderer, and the UUID sync payload switched onto the new model; the legacy
      table promoted away in one transaction.

- [x] **Phase 12: Regression Verification** (2/2 plans) — completed 2026-07-22
      Human confirmation on the running application that the rewrite is invisible, after closing the
      initiating-tab drawing-preview regression.

**Outcome:** 21/22 requirements satisfied. All 4 phases verified `passed`; REG-01 human acceptance
3/3. `dotnet build` clean; 500/500 tests passing.

**Closed as `override_closeout`** — MIGR-03 (fixture-backed migration replay proof) is recorded as an
accepted gap: the migration path is permanently unreachable, so forward risk is zero, but the
requirement text was deliberately not rewritten to fit what was built. Reasoning:
[`milestones/v1.11-MILESTONE-AUDIT.md`](milestones/v1.11-MILESTONE-AUDIT.md).

</details>

### 🚧 v1.12 Five-pointed star (Phases 13–17) — In Progress

**Milestone goal:** Add `star5` as the fifth figure type end-to-end — drawn, previewed, persisted,
synced, selected, dragged and deleted exactly like the four that came before it. Stretchable (fills
the dragged box, not aspect-locked); point-up (first vertex top-centre, sweep starts at −π/2);
corner-to-corner gesture like triangle/rectangle; inner-radius ratio 0.382 (1/φ²). Stored geometry
`{"points": [[x,y] × 10], "innerRatio": 0.382}` — points authoritative for render and `bbox_*`;
`innerRatio` descriptive but required by `TryParseGeometry`. A seventh toolbar button, between
`triangle` and `delete`. Full design lives in `docs/DECISIONS.md` from D-70 onward.

**Sequencing rationale (why this order):** `Star5Shape`'s geometry (`FromGesture`,
`TryParseGeometry`/`ToJson`, `BoundsOf`) is pure C# with zero database dependency, so it comes first
and keeps the existing app and all 500 tests untouched and green — the same "additive first" pattern
v1.11 used. Phase 13 deliberately stops short of *registering* the shape: `V11CutoverTests` asserts
`count(*) FROM public.figure_types == 4` twice against scratch databases in the two states that do
run `SeedFigureTypesAsync`, so registration is not additive and belongs with the seed in Phase 14.
The `figure_types` catalog seed fix (MODEL-08) is a hard prerequisite for writing any
star row at all: `V11Cutover.EnsureAsync` returns early at `CatalogState.Completed` on the existing
database, so `SeedFigureTypesAsync` never runs again and the foreign key on `figures.type` would
reject every star insert. It lands in Phase 14 together with the toolbar button that arms the tool
and the decision-doc amendments the new button requires — before Phase 15 attempts to draw and
persist a star for the first time. Phase 16 proves the star matches the four existing shapes for
interaction and sync, and folds in this milestone's test guards once the code they guard exists.
Phase 17 — human regression verification — is deliberately last and stands alone, mirroring v1.11's
Phase 12: it is the milestone's real acceptance gate and must not be short-circuited by any earlier
phase's automated tests.

Phase numbering continues from v1.11's Phase 12 (directories will be `BC-13-…` … `BC-17-…`, matching
the established `BC-01-…`…`BC-12-…` pattern).

- [x] **Phase 13: Star Shape Core** - `Star5Shape`/`Star5Geometry` implement `IShapeDefinition` in
      isolation — gesture, parse/serialize round-trip, and points-derived bounds — zero database
      dependency, all 500 existing tests stay green.

- [x] **Phase 14: Catalog Seed, Toolbar & Decisions** - `Star5Shape` joins the registry, the
      `figure_types` seed runs idempotently on every startup, a seventh toolbar button arms the star
      tool, and the locked decisions are amended for seven buttons.

- [x] **Phase 15: Draw, Preview, Render & Persist a Star** - A user draws a star end-to-end — live (completed 2026-07-23)
      preview, edge clamp, correct render on commit and reload, immediate persistence.

- [x] **Phase 16: Interaction, Sync & Test Guards** - Select, drag, and delete a star like the four (completed 2026-07-23)
      existing shapes; live cross-tab glide; preview-ownership, bbox-agreement, and degenerate-rejection
      tests.

- [ ] **Phase 17: Regression Verification** - Human acceptance on the running application confirms
      the milestone's definition of done.

### 📋 v1.2 Figures & dynamic toolbar (Planned)

Scoped, not started, waits behind v1.12. New figure types (ellipse, hexagon, pentagon, right-angle
triangle L/R, four arrows — nine, not ten: v1.12 delivers the 5-point star) plus a dynamic
split-button toolbar. Full plan: [`backlog/v1.2-figures-and-toolbar.md`](backlog/v1.2-figures-and-toolbar.md).
Materially cheaper now that v1.11 has landed — the 4-integer workarounds are gone, and a new type
costs one C# class plus one `figure_types` row. v1.12 also pays down two of its costs in advance: the
`figure_types` seed becomes automatic, and the toolbar's six-button decisions are already amended to
seven. Its remaining decision amendments happen when v1.2 is kicked off.

## Phase Details

### Phase 13: Star Shape Core

**Goal**: A `Star5Shape`/`Star5Geometry` pair fully implements the `IShapeDefinition` contract —
deriving a point-up, ten-point star from a corner-to-corner drag, round-tripping through
`TryParseGeometry`/`ToJson`, and deriving its `bbox_*` purely from the point list — proven correct in
isolation with zero database involvement, leaving the existing app and all 500 tests untouched and
green.
**Depends on**: Nothing (continues from Phase 12; first phase of v1.12)
**Requirements**: SHAPE-04, SHAPE-05, SHAPE-06
**Success Criteria** (what must be TRUE):

  1. `Star5Shape.FromGesture` derives a point-up, ten-point star (five outer, five inner at 0.382 of
     the outer radius) that stretches to fill any dragged box, sweeping from −π/2, the same
     corner-to-corner contract triangle and rectangle already use — proven against the same pattern
     `PentagonShape` (test-only) already demonstrates.

  2. `Star5Shape.BoundsOf` computes the bbox from the ten-point list alone — a test proves a stored
     `innerRatio` that disagrees with the points changes nothing about the derived bounds or a
     re-render.

  3. `Star5Shape.ToJson` / `TryParseGeometry` round-trip a star byte-identically, and a geometry
     payload missing `innerRatio` fails to parse rather than rendering a partially-populated figure.

  4. All 500 existing tests remain green and no existing file changes behaviour — `Star5Shape` is
     **not yet registered** in `DefaultShapes.CreateRegistry()`, so this phase is genuinely additive.
     Its tests instantiate the class directly, exactly as `PentagonShape`'s do today. Registration is
     deliberately deferred to Phase 14: `V11CutoverTests` asserts
     `count(*) FROM public.figure_types == 4` twice (lines 58 and 73), against scratch databases in
     `Additive` and `FreshUsersOnly` state — the two states that *do* run `SeedFigureTypesAsync` — so
     registering here would turn both assertions red and falsify this criterion.

**Plans:** 1/1 plans executed

Plans:

- [x] 13-01-PLAN.md — Add unregistered Star5Geometry/Star5Shape core and direct contract tests.

---

### Phase 14: Catalog Seed, Toolbar & Decisions

**Goal**: The application is ready to accept a drawn star — `Star5Shape` joins the registry, the
`figure_types` catalog seeds `star5` idempotently on every startup even against the existing
`CatalogState.Completed` database, a seventh toolbar button arms the tool, and the locked decisions
are amended to match.
**Depends on**: Phase 13 (registers, seeds and arms the shape Phase 13 built)
**Requirements**: MODEL-08, CANV-04, ARCH-02
**Success Criteria** (what must be TRUE):

  1. `Star5Shape` is registered in `DefaultShapes.CreateRegistry()`, and the two `V11CutoverTests`
     assertions that hard-code the catalog size (`count(*) FROM public.figure_types == 4`, lines 58
     and 73) are updated to 5 — the suite is green again with the fifth shape present. This is the
     phase that owns the registration's blast radius.

  2. Starting the app against the existing database inserts a `star5` row into `figure_types` with no
     manual SQL and no migration, closing the gap where `V11Cutover.EnsureAsync` returns early at
     `CatalogState.Completed`.

  3. Running the seed across two consecutive startups leaves exactly one `star5` row — the seed is
     idempotent on every startup, not merely additive once.

  4. The toolbar shows seven buttons — `[pointer] [line] [rectangle] [circle] [triangle] [star]
     [delete]` — the star button sits between `triangle` and `delete`, and arms/un-arms exclusively
     like every other tool; the 48px strip and right-aligned logout are unchanged.

  5. `docs/DECISIONS.md`, `PROJECT.md` and `.planning/intel/` record the seven-button toolbar as an
     amendment to D-16/D-33/D-58 and to CANV-02's old toolbar-count text, with the star's own
     decisions added by name starting at D-70.

**Plans:** 3/3 plans executed

Plans:

- [x] 14-01-PLAN.md — Register Star5Shape and make registry-driven figure_types seeding converge on completed public catalogs.
- [x] 14-02-PLAN.md — Add the armable star toolbar button and star5 tool mapping while preserving toolbar/logout semantics.
- [x] 14-03-PLAN.md — Amend decisions, PROJECT.md, and active intel mirrors for seven buttons and D-70+ star decisions.

**UI hint**: yes

---

### Phase 15: Draw, Preview, Render & Persist a Star

**Goal**: A user can draw a star end-to-end exactly like the four existing shapes — armed from the
toolbar, previewed live under the cursor, clamped at the canvas edge, rendered correctly on commit,
and persisted immediately with no Save button.
**Depends on**: Phase 14 (needs the seeded catalog row and the armable toolbar button)
**Requirements**: FIG-05, FIG-06, FIG-07, RENDER-02, DATA-05
**Success Criteria** (what must be TRUE):

  1. User can arm the star tool and draw a star by dragging corner-to-corner, exactly as they draw a
     rectangle or triangle.

  2. A live star preview follows the cursor during the drag, showing the same shape the committed
     figure will have — not a triangle fallback, and not a second formula that can drift from
     `Star5Shape`.

  3. A star dragged toward a canvas edge stops at the edge under the same clamp as every other shape;
     a drag with zero width or zero height is silently rejected, while a thin sliver with positive
     width and height is accepted and committed.

  4. The committed star renders from its local geometry under the v1.11 `translate(x,y) rotate(…)`
     transform, both immediately after the drop and after a page reload — it does not silently
     disappear from the renderer's type switch.

  5. The star persists immediately with no Save button and reappears unchanged after a refresh.

**Plans:** 4/4 plans executed

Plans:

- [x] 15-04-PLAN.md

**Wave 1**

- [x] 15-01-PLAN.md — Prove star draw, clamp, degenerate rejection, sliver acceptance, and immediate persistence/reload.
- [x] 15-02-PLAN.md — Pin committed star rendering from local Star5Geometry points under the translate/rotate transform.

**Wave 2** *(blocked on Wave 1 completion)*

- [x] 15-03-PLAN.md — Render live star previews through the C# registry placement and remove the JS triangle fallback path.

**UI hint**: yes

---

### Phase 16: Interaction, Sync & Test Guards

**Goal**: A persisted star behaves exactly like the four existing shapes for selection, drag, and
delete, glides live across a user's open tabs under the unchanged D-53 contract, and this milestone's
silent failure modes are pinned by tests.
**Depends on**: Phase 15 (needs a persisted, rendered star to select, drag, delete and sync)
**Requirements**: FIG-08, SYNC-04, TEST-04
**Success Criteria** (what must be TRUE):

  1. User can select a star — the blue-and-white dashed trace appears on the star's own outline —
     drag it with edge clamping, and delete it, exactly as they can the four existing shapes.

  2. A star appears live in the user's other open tabs on draw, glides in real time during a drag,
     and disappears on delete — under the unchanged D-53 message contract.

  3. A test fails if visible preview geometry drifts back into `Home.razor.js` instead of the
     registry-backed Razor/FigureShape path, and a test proves `bbox_*` agreement for star rows
     against a fresh geometry recompute.

  4. Tests prove degenerate (zero width or height) and malformed (missing `innerRatio`, wrong point
     count) star geometry is rejected at both the unit and gateway boundary.

**Plans:** 4/4 plans executed
and selection-trace paths are already type-blind and handle `star5`, so this phase proves star parity
and folds in the milestone's test guards)

Plans:

**Wave 1** *(fully parallel — disjoint test files)*

- [x] 16-01-PLAN.md — Prove star5 select, click-vs-drag, edge-clamped drag, delete, D-40 update-only,
      D-54 discard-all, and echo filter at the coordinator boundary (FIG-08, SYNC-04).

- [x] 16-02-PLAN.md — Prove two-circuit final-public star draw/glide/delete relay, the D-40
      resurrection guard, and a persisted select/drag-clamp/delete round-trip (SYNC-04, FIG-08).

- [x] 16-03-PLAN.md — Extend the JS↔C# drift guard, fold a star5 row into the whole-table bbox
      agreement scan, and reject degenerate/malformed star geometry at the gateway and unit boundary
      (TEST-04).

- [x] 16-04-PLAN.md — Add a bUnit render-level preview smoke test that renders the active star
      preview through FigureShape and asserts a polygon is emitted at pointermove before commit, with
      a negative control that fails under the G-15-1 unbound-literal binding (TEST-04).

**UI hint**: yes

---

### Phase 17: Regression Verification

**Goal**: A human confirms, on the running application, that the star behaves exactly like the four
existing shapes end-to-end — the milestone's definition of done.
**Depends on**: Phase 16 (nothing left to verify against until interaction and sync are complete)
**Requirements**: REG-02
**Success Criteria** (what must be TRUE):

  1. A human arms the star tool, draws with a live preview, watches it clamp at a canvas edge,
     refreshes the page, and confirms it persists unchanged.

  2. A human selects, drags with edge clamping, and deletes a star, confirming it matches the four
     existing shapes exactly.

  3. A human opens a second browser window on the same account and watches a star glide live in real
     time during a drag from the first — the milestone's acceptance gate.

**Plans:** 1 plan

Plans:

- [ ] 17-01-PLAN.md — Run the acceptance-only REG-02 human UAT gate with preflight smoke checks, one app process, two same-profile browser windows, evidence capture, and fail-fast handling.

## Progress

**Execution Order:**
Phases execute in numeric order: 1 → 2 → 3 → 4 → 5 → 6 → 7 → 8 → 9 → 10 → 11 → 12 → 13 → 14 → 15 →
16 → 17

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
| 9. Shape Registry & Validation Gateway | v1.11 | 6/6 | Complete    | 2026-07-22 |
| 10. Storage Schema, Migration & Persistence Layer | v1.11 | 6/6 | Complete    | 2026-07-22 |
| 11. Renderer, Sync & Cutover | v1.11 | 5/5 | Complete    | 2026-07-22 |
| 12. Regression Verification | v1.11 | 2/2 | Complete    | 2026-07-22 |
| 13. Star Shape Core | v1.12 | 1/1 | Complete | 2026-07-22 |
| 14. Catalog Seed, Toolbar & Decisions | v1.12 | 3/3 | Complete | 2026-07-22 |
| 15. Draw, Preview, Render & Persist a Star | v1.12 | 4/4 | Complete | 2026-07-23 |
| 16. Interaction, Sync & Test Guards | v1.12 | 4/4 | In Progress|  |
| 17. Regression Verification | v1.12 | 0/TBD | Not started | - |

**v1.0: 5/5 phases, 23/23 plans, 15/15 requirements — milestone audit passed.**
**v1.1: 3/3 phases, 4/4 plans, 4/4 requirements — all phases verified `passed`.**
**v1.11: 4/4 phases, 19/19 plans, 21/22 requirements satisfied — shipped 2026-07-22 as
`override_closeout`; MIGR-03 accepted as a documented gap.**
**v1.12: 3/5 phases, 11/15 requirements — Phase 15 complete 2026-07-23; Phase 16 is next.**

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
| 9 | v1.11 | SHAPE-01, SHAPE-02, SHAPE-03, VALID-01, VALID-02, VALID-03 |
| 10 | v1.11 | MODEL-01, MODEL-02, MODEL-03, MODEL-04, MODEL-05, MODEL-06, MODEL-07, MIGR-01, MIGR-02, MIGR-03, TEST-03 |
| 11 | v1.11 | RENDER-01, SYNC-02, SYNC-03, TEST-02 |
| 12 | v1.11 | REG-01 |
| 13 | v1.12 | SHAPE-04, SHAPE-05, SHAPE-06 |
| 14 | v1.12 | MODEL-08, CANV-04, ARCH-02 |
| 15 | v1.12 | FIG-05, FIG-06, FIG-07, RENDER-02, DATA-05 |
| 16 | v1.12 | FIG-08, SYNC-04, TEST-04 |
| 17 | v1.12 | REG-02 |

**v1.0: 15/15 requirements mapped. v1.1: 4/4 requirements mapped. v1.11: 22/22 requirements mapped.
v1.12: 15/15 requirements mapped. No orphans. No duplicates.**

## What's Next

**v1.12 Phase 15 complete 2026-07-23.** Suggested next step: `/gsd-plan-phase 16` to plan star
interaction, sync, and test guards.

**v1.11 is shipped and archived.** MIGR-03 carries forward as an accepted gap, not a completed
requirement. Closing it later means restoring `V11MigrationReplayTests.cs` against the committed
`v1.1-pre-rewrite.sql` fixture — see the audit's "Outstanding Work" section.

**Tech debt carried forward:** `ShapeRegistry` read-only views expose their backing lists
(09-REVIEW WR-03, explicitly out of v1.12 scope). Phase 15 removed the `Home.razor.js` preview
geometry duplication; Phase 16 should keep that as a regression guard while adding bbox and
malformed-geometry guards. The unreferenced
`V11DataMigration.RunAsync(NpgsqlDataSource, …)` overload also stays. Known v1.0 tech debt
(~11 low-severity items) remains recorded in
[`milestones/v1.0-MILESTONE-AUDIT.md`](milestones/v1.0-MILESTONE-AUDIT.md). None blocks a requirement.

**After v1.12: v1.2** — the remaining nine figure types plus a dynamic split-button toolbar, scoped in
`.planning/backlog/v1.2-figures-and-toolbar.md`.

---
*Roadmap created: 2026-07-14 from `docs/DECISIONS.md` (58 locked decisions) via `.planning/intel/`*
*v1.0 archived: 2026-07-17*
*v1.1 archived: 2026-07-21 — phases 6–8 collapsed above; full detail in `milestones/v1.1-ROADMAP.md`.*
*v1.11 archived: 2026-07-22 — phases 9–12 collapsed above; full detail in
`milestones/v1.11-ROADMAP.md`, requirements in `milestones/v1.11-REQUIREMENTS.md`, audit in
`milestones/v1.11-MILESTONE-AUDIT.md`. Closed as `override_closeout` with MIGR-03 accepted as a
documented gap.*
*v1.12 roadmap added: 2026-07-22 — phases 13–17 continue numbering from Phase 12; all 15 requirements
mapped, 100% coverage, no orphans. Phase 15 completed 2026-07-23 after the preview UAT gap was fixed
and retested. Phase 17 (Regression Verification) stands alone as the milestone's human acceptance
gate, mirroring v1.11's Phase 12.*
