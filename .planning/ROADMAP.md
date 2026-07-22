# Roadmap: BlazorCanvas

## Milestones

- ✅ **v1.0 MinVP** — Phases 1–5 (shipped 2026-07-17)
- ✅ **v1.1 Canvas resize · selection UX · no-JS removal** — Phases 6–8 (shipped 2026-07-21)
- 🚧 **v1.11 Storage Model Rewrite** — Phases 9–12 (in progress)
- 📋 **v1.2 Figures & dynamic toolbar** — scoped, waiting behind v1.11

v1.0 detail: [`milestones/v1.0-ROADMAP.md`](milestones/v1.0-ROADMAP.md) ·
[`milestones/v1.0-REQUIREMENTS.md`](milestones/v1.0-REQUIREMENTS.md) ·
[`milestones/v1.0-MILESTONE-AUDIT.md`](milestones/v1.0-MILESTONE-AUDIT.md)

v1.1 detail: [`milestones/v1.1-ROADMAP.md`](milestones/v1.1-ROADMAP.md) ·
[`milestones/v1.1-REQUIREMENTS.md`](milestones/v1.1-REQUIREMENTS.md) ·
phase artifacts archived under [`milestones/v1.1-phases/`](milestones/v1.1-phases/)

v1.11 requirements: [`REQUIREMENTS.md`](REQUIREMENTS.md) · design: `docs/DATA-MODEL-v1.11-DRAFT.md`
· decisions: `docs/DECISIONS.md` → D-59…D-69

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

### 🚧 v1.11 Storage Model Rewrite (Phases 9–12, in progress)

**Milestone goal:** Replace the four-integer bounding-box storage model with the position/shape
split (D-59…D-69) — `x, y, rotation` for where a figure is, `geometry jsonb` in local coordinates for
what it is — migrating every existing figure intact via the draft's exact formulas, so the schema
never has to change again for a feature and the user sees no difference at all. Four tables
(`users`, `canvases`, `figures`, `figure_types`), `numeric` coordinates, `uuid` ids, a `z` column
unique per canvas with collision retry, a single `IShapeDefinition` registry, one validation gateway
for client-supplied `geometry`/`style`, and a `bbox_*` cache computed in exactly one place. Full
design: `docs/DATA-MODEL-v1.11-DRAFT.md`.

**Sequencing rationale (why this order):** the storage model change touches the schema, the geometry
core, the persistence layer, the renderer, the sync payload, and the test suite — too much for one
phase, but not fully independent either, since the app cannot build against a half-changed `Figure`
entity. The registry and validation gateway are pure C# with zero database dependency, so they come
first and keep 100% of the existing 405 tests green while adding their own. The new schema,
migration, and a new persistence layer follow as a second, purely *additive* phase — new tables and
a new data-access path proven correct in isolation, while `Home.razor`, `FigureShape.razor`, the old
table, and all existing tests stay untouched and green. Only the third phase performs the actual
cutover (renderer, sync payload, old-table and dead-test removal) — the one phase where the app is
briefly "between" models, contained entirely within its own plan sequence so every phase boundary
still builds and passes its tests. Human regression verification (REG-01) is deliberately the last
phase: it is the milestone's real acceptance gate, and it must not be short-circuited by any earlier
phase's automated tests.

Phase numbering continues from v1.1's Phase 8 (directories will be `BC-09-…`, `BC-10-…`, `BC-11-…`,
`BC-12-…`, matching the established `BC-01-…`…`BC-08-…` pattern).

- [x] **Phase 9: Shape Registry & Validation Gateway** - Collapse every type-specific figure rule and all client-input validation into two pure-C# abstractions, proven in isolation before any schema or UI change. Also banks the v1.1-era database dump that Phase 10's migration proof depends on (one-shot capture — see phase detail). (completed 2026-07-22)
- [x] **Phase 10: Storage Schema, Migration & Persistence Layer** - Land the four-table schema and a new persistence layer, and prove every existing figure migrates losslessly — all additive; execution complete, re-verification pending. (completed 2026-07-22)
- [x] **Phase 11: Renderer, Sync & Cutover** - Switch `Home.razor`, the renderer, and the sync payload onto the new model; retire the old table and its dead tests. (completed 2026-07-22)
- [ ] **Phase 12: Regression Verification** - A human confirms on the running application that the rewrite is invisible.

## Phase Details

### Phase 9: Shape Registry & Validation Gateway

**Goal**: All type-specific figure logic (parse, validate, drawability, bounds, draw-gesture) and all
client-supplied JSON validation live behind two new, pure-C# abstractions — `IShapeDefinition` and a
single validation gateway — proven correct by unit tests with zero database involvement and zero
change to the running application.
**Depends on**: Nothing (first phase of v1.11)
**Requirements**: SHAPE-01, SHAPE-02, SHAPE-03, VALID-01, VALID-02, VALID-03
**Success Criteria** (what must be TRUE):

  1. A single `IShapeDefinition` implementation exists for each of line, rectangle, circle, and
     triangle, and every type-specific rule for a given shape (parse, drawability, bounds, draw
     gesture) is reachable through that one interface — verified by unit tests, no database.

  2. Line and triangle geometry is represented as a point list, not derived from a bounding box —
     proven by a test that builds a downward- or sideways-pointing triangle using the existing
     `IShapeDefinition` with no new formula.

  3. A test registers and round-trips (parse → bounds → draw-gesture) a fifth shape type the
     application does not ship, with zero changes to any other type's code and no schema involved —
     proving a new shape costs exactly one class.

  4. Hostile `geometry` input (negative or zero size, degenerate/duplicate points) is parsed into a
     typed record and rejected by validation before it can reach a bounds computation, preserving
     today's silent-rejection behavior for a degenerate draw.

  5. Hostile `style` input (a non-hex colour string, an out-of-range or absurd stroke width, an
     attempted markup/attribute injection) is clamped or replaced by the validator's own defaults and
     never re-emitted verbatim — proven by a test asserting the sanitised record never contains the
     raw hostile input.

**Plans**: 6/6 plans executed

Plans:
**Wave 1**

- [x] 09-01-PLAN.md — Typed geometry model, `IShapeDefinition` contract, `ShapeRegistry`, `GeometryJson` helpers (wave 1)
- [x] 09-02-PLAN.md — `FigureStyle` record and the style sanitising gateway (wave 1)
- [x] 09-03-PLAN.md — Capture and commit the v1.1-era pre-rewrite database dump fixture (wave 1, blocking checkpoint)

**Wave 2** *(blocked on Wave 1 completion)*

- [x] 09-04-PLAN.md — The four `IShapeDefinition` implementations and the `DefaultShapes` registry (wave 2)

**Wave 3** *(blocked on Wave 2 completion)*

- [x] 09-05-PLAN.md — v1.1 gesture-equivalence grid, fifth-shape extensibility proof, point-list primacy invariant (wave 3)
- [x] 09-06-PLAN.md — `FigureInputGateway`, the single validation choke point for client geometry and style (wave 3)

Note: this phase is purely additive — the existing `BlazorCanvas.Geometry` classes, `Home.razor`,
`FigureShape.razor`, and all 405 existing tests are untouched and remain green throughout.

> ⚠️ **Also in this phase: capture the v1.1-era database dump** that Phase 10's MIGR-03 replay test
> consumes. It lives here rather than in Phase 10 because the capture is **one-shot and
> irreversible**: Phase 10 is the phase that applies the migration, and once it runs against the dev
> database the pre-rewrite state is gone and the replay test has no subject. Phase 9 touches no
> database at all, so banking the dump here removes the ordering hazard entirely. Commit the dump as
> a test fixture, drawn from a database holding at least one of every existing type (line,
> rectangle, circle, triangle) — including a diagonal line in each direction and overlapping figures,
> so stacking order is actually observable in the replay.

---

### Phase 10: Storage Schema, Migration & Persistence Layer

**Goal**: The database and a new persistence layer fully implement the four-table schema and the
position/shape split, and every existing figure is proven to migrate losslessly — all additive at
the data layer, before any application code is touched.
**Depends on**: Phase 9 (uses `IShapeDefinition.BoundsOf` and the validation gateway)
**Requirements**: MODEL-01, MODEL-02, MODEL-03, MODEL-04, MODEL-05, MODEL-06, MODEL-07, MIGR-01, MIGR-02, MIGR-03, TEST-03
**Success Criteria** (what must be TRUE):

  1. The live database has four tables — `users`, `canvases`, `figures`, `figure_types` — and a new
     figure type can be added with a single `INSERT` into `figure_types`, never an `ALTER TABLE`.

  2. A new persistence layer moves a figure by writing only `x` and `y` (no `geometry` read, no
     per-type branch), generates a `uuid` id before the `INSERT`, and assigns `z = max(z) + 1` —
     proven to retry and succeed, leaving both figures present, when two inserts collide on `z`.

  3. Running the migration against a captured pre-rewrite (v1.1-era) database dump gives every
     existing user exactly one 1472×828 canvas holding all of their figures, and a replay test
     confirms every migrated figure's rendered vertices and stacking order match its pre-migration
     values exactly — proven, not assumed.

  4. Every migrated figure's `style` equals today's fixed appearance exactly (`#000000` / `2` /
     `#FFFFFF` / `1`), and a `bbox_*` cache exists for every row, computed by exactly one code path.

  5. Three new regression tests exist and pass: stored `bbox_*` agrees with a fresh recompute from
     `geometry` for every row; the validation gateway rejects hostile `geometry`/`style`; and a `z`
     collision produces both figures rather than silently losing one.

**Plans**: 6/6 plans executed (re-verification pending)

Plans:
**Wave 1**

- [x] 10-01-PLAN.md — The `v11` four-table schema, its idempotent applier, and live-catalog shape assertions (wave 1, blocking schema push)
- [x] 10-02-PLAN.md — The four v1.1→v1.11 conversion formulas and deterministic id derivation, database-free (wave 1)

**Wave 2** *(blocked on Wave 1 completion)*

- [x] 10-03-PLAN.md — `FigureRepository`: type-blind move, uuid-before-insert, `z = max(z)+1` with collision retry (wave 2)

**Wave 3** *(blocked on Wave 2 completion)*

- [x] 10-04-PLAN.md — `V11DataMigration` and the scratch-database replay proof against the committed v1.1 dump (wave 3)
- [x] 10-05-PLAN.md — TEST-03 guards: whole-table bbox-vs-geometry agreement and hostile-input rejection at the database boundary (wave 3)

**Wave 4 — gap closure**

- [x] 10-06-PLAN.md — Encloses v11 DDL, type seeding, canvases, and figures in one migration transaction and proves invalid legacy rows leave no catalog residue.

Notes: the v1.1-era dump that criterion 3's replay test consumes is **captured in Phase 9**, not
here — see the warning at the end of Phase 9. This phase consumes that committed fixture; it must
not attempt to capture a "pre-migration" dump itself, because by the time this phase's migration has
been developed the database may already have been migrated locally. The existing application
(`Home.razor`, the old `FigureStore`, the old four-column table) is untouched and continues to
build, run, and pass every one of its existing tests throughout this phase; the new schema and
persistence layer are proven in isolation, not yet wired in.

---

### Phase 11: Renderer, Sync & Cutover

**Goal**: The running application draws, drags, deletes, and syncs across tabs exactly as it did in
v1.1, but every pixel and every message now comes from the new storage model — the old table, the
old type-specific code paths, and the tests whose subject no longer exists are gone.
**Depends on**: Phase 10 (wires the app onto the new schema and persistence layer)
**Requirements**: RENDER-01, SYNC-02, SYNC-03, TEST-02
**Success Criteria** (what must be TRUE):

  1. Every figure renders as a `<g transform="translate(x, y) rotate(...)">` wrapper around a shape
     drawn in local coordinates, and a side-by-side comparison with v1.1 shows no visual difference —
     including the selection trace and the 48px toolbar offset.

  2. Dragging a figure in one tab still glides in a second open tab in real time, using the new
     `uuid`-keyed, position-delta payload, while preserving D-53's message kinds, D-40's update-only
     move, D-54's blanket mid-drag discard, and D-47's 50ms throttle with guaranteed trailing edge.

  3. A save failure still rolls back every open tab and forces the documented reload modal, and a
     zero-row `UPDATE` (a stale figure) still silently removes that figure from a tab's view — both
     unchanged from v1.1, now running on the new model.

  4. The old `figures` table, the old `Figure`/`Box`-based entity and geometry classes, and every test
     whose subject no longer exists (the circle inscribed-square round-trip, the line-normalisation
     landmine test, the 32-case guard-vs-CHECK matrix) are all removed, leaving no dead scaffolding —
     and the full solution builds cleanly and passes its whole rebased test suite.

**Plans**: 5 plans in 5 waves (including 2 verification-gap closure plans)

**Wave 1**

- [x] 11-01-PLAN.md — Additive v11 runtime bootstrap, owner-scoped canvas resolver, and DI composition.

**Wave 2** *(blocked on Wave 1 completion)*

- [x] 11-02-PLAN.md — Local-coordinate renderer, UUID position sync, and live Home circuit wiring.

**Wave 3** *(blocked on Wave 2 completion)*

- [x] 11-03-PLAN.md — Transactional schema promotion, legacy removal, and rebased full-suite proof.

**Wave 4** *(blocked on Wave 3 completion; gap closure)*

- [x] 11-04-PLAN.md — Guarded scratch-database cutover state and atomic-rollback proof.

**Wave 5** *(blocked on Wave 4 completion; gap closure)*

- [x] 11-05-PLAN.md — Final-public two-circuit persistence, queued-delivery, stale-row, and reload-convergence proof.

**Cross-cutting constraints:** v1.1-visible selection and 48px toolbar mapping stay unchanged; drag messages remain UUID-keyed, update-only, 50ms-throttled with a trailing edge; save failure rolls every tab back to the documented reload path.

**UI hint**: yes

---

### Phase 12: Regression Verification

**Goal**: A human confirms, on the running application, that the storage model rewrite is invisible —
every user-facing behavior is indistinguishable from v1.1.
**Depends on**: Phase 11 (nothing left to verify against until the cutover is complete)
**Requirements**: REG-01
**Success Criteria** (what must be TRUE):

  1. A human draws all four shapes (with edge clamping), drags each of them, and deletes them, and
     confirms every behavior matches v1.1 exactly.

  2. A human exercises selection (select, deselect via each documented route) and confirms it behaves
     identically to v1.1, including the topmost blue-and-white dashed trace.

  3. A human opens two browser windows on the same account and confirms a drag glides live in the
     second window in real time, exactly as before — the milestone's acceptance gate.

**Plans**: TBD

## Progress

**Execution Order:**
Phases execute in numeric order: 1 → 2 → 3 → 4 → 5 → 6 → 7 → 8 → 9 → 10 → 11 → 12

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
| 12. Regression Verification | v1.11 | 0/TBD | Not started | - |

**v1.0: 5/5 phases, 23/23 plans, 15/15 requirements — milestone audit passed.**
**v1.1: 3/3 phases, 4/4 plans, 4/4 requirements — all phases verified `passed`.**
**v1.11: 0/4 phases complete — roadmap created 2026-07-21; all 22 requirements mapped.**

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

**v1.0: 15/15 requirements mapped. v1.1: 4/4 requirements mapped. v1.11: 22/22 requirements mapped.
No orphans. No duplicates.**

## What's Next

**v1.11 is active** — 4 phases (9, 10, 11, 12) cover all 22 requirements; none has started yet.
Suggested next step: `/gsd-plan-phase 9`. Phase 9 has zero database dependency and is a natural fit
to plan and execute first; phases 10 and 11 depend on it in sequence, and phase 12 (human
regression verification) must be last.

**v1.2 is scoped, not started, and waits behind v1.11:** new figure types (ellipse, 5-point star,
hexagon, pentagon, right-angle triangle L/R, four arrows) + a dynamic split-button toolbar. Full
plan: `.planning/backlog/v1.2-figures-and-toolbar.md` — materially cheaper once v1.11 lands (its
4-integer workarounds disappear). Its decision amendments happen when v1.2 is kicked off.

Known v1.0 tech debt (~11 low-severity items from `01-REVIEW.md`) is recorded in
[`milestones/v1.0-MILESTONE-AUDIT.md`](milestones/v1.0-MILESTONE-AUDIT.md). None blocks a
requirement. v1.1 added no new tech debt.

---
*Roadmap created: 2026-07-14 from `docs/DECISIONS.md` (58 locked decisions) via `.planning/intel/`*
*v1.0 archived: 2026-07-17*
*v1.1 archived: 2026-07-21 — phases 6–8 collapsed above; full detail in `milestones/v1.1-ROADMAP.md`.*
*v1.11 roadmap added: 2026-07-21 — phases 9–12 continue numbering from Phase 8; all 22 requirements
mapped, 100% coverage, no orphans.*
*v1.11 roadmap amended at approval: the v1.1-dump capture moved from Phase 10 into Phase 9. The
capture is one-shot and irreversible — Phase 10 applies the migration, so a dump taken there risks
having no pre-rewrite state left to record. MIGR-03 itself stays mapped to Phase 10 (the replay test
lives with the migration it proves); only the fixture capture moved. Coverage unchanged: 22/22.*
