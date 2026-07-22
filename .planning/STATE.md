---
gsd_state_version: 1.0
milestone: v1.12
milestone_name: Five-pointed star
current_phase: 15
current_phase_name: Draw, Preview, Render & Persist a Star
status: planning
stopped_at: Completed BC-14-03-PLAN.md
last_updated: "2026-07-22T20:38:59.290Z"
last_activity: 2026-07-22
last_activity_desc: Phase 14 complete, transitioned to Phase 15
progress:
  total_phases: 2
  completed_phases: 2
  total_plans: 4
  completed_plans: 4
  percent: 100
---

# Project State

## Project Reference

See: .planning/PROJECT.md (updated 2026-07-22 at v1.12 milestone open)

**Core value:** The canvas is always the truth, everywhere at once — what you draw persists instantly,
and every other tab shows it happening live, including a figure gliding in real time as you drag it.

**Current focus:** **v1.12 Five-pointed star** — opened 2026-07-22 on branch `Milestone-v1.12`.
Add `star5` as the fifth figure type end-to-end: stretchable, point-up, corner-to-corner gesture,
inner ratio 0.382, geometry `{"points": [[x,y] × 10], "innerRatio": 0.382}` with points authoritative
and the ratio required on parse. Plus a seventh toolbar button between `triangle` and `delete`, an
idempotent `figure_types` seed on every startup, and a `Home.razor.js` star branch with a drift guard.
Scope is **one figure** — carried debt stays carried. Ends with a human acceptance gate.

**Roadmap created 2026-07-22** — Phases 13–17, continuing numbering from v1.11's Phase 12. All 15
v1.12 requirements mapped, 100% coverage, no orphans. See `.planning/ROADMAP.md` → "Phase Details".

v1.11 shipped and archived 2026-07-22 as `override_closeout` — 21/22 requirements satisfied, build
clean, 500/500 tests passing.

**Carried forward — MIGR-03 (accepted gap, not complete).** The fixture-backed migration replay
proof was written in Phase 10, passed 27/27, and was deleted in Phase 11's cutover-cleanup commit
`1aaf45b`. `tests/BlazorCanvas.Tests/Fixtures/v1.1-pre-rewrite.sql` is still committed and copied to
test output but has no C# consumer. Accepted because the migration path is permanently unreachable
(forward risk zero); the residual risk is retrospective. Route to closing it:
`.planning/milestones/v1.11-MILESTONE-AUDIT.md` → "Outstanding Work".

**Carried forward — tech debt.** `ShapeRegistry.All`/`.Names` return live `List` instances behind
`IReadOnlyList` (09-REVIEW WR-03) — **explicitly out of v1.12 scope**. `Home.razor.js` reimplements
shape preview geometry outside the registry with no drift guard; v1.12 does **not** close this, but
it does add a drift-guard test pinning the JS inner-ratio constant to the C# one, so the duplication
becomes loud rather than silent. The unreferenced
`V11DataMigration.RunAsync(NpgsqlDataSource, …)` overload also stays.

**v1.0, v1.1 and v1.11 are all archived** under `.planning/milestones/`. **v1.2** (the remaining
**nine** figures + dynamic toolbar — v1.12 delivers the 5-point star) is scoped in
`.planning/backlog/v1.2-figures-and-toolbar.md` and follows v1.12.

## Current Position

Phase: 15 — Draw, Preview, Render & Persist a Star
Plan: Not started
Status: Ready to plan
Last activity: 2026-07-22 — Phase 14 complete, transitioned to Phase 15

## Performance Metrics

**Velocity:**

- Total plans completed: 44
- Average duration: —
- Total execution time: —

**By Phase:**

| Phase | Plans | Total | Avg/Plan |
|-------|-------|-------|----------|
| BC-02 | 3 | - | - |
| BC-03 | 5 | - | - |
| BC-04 | 4 | - | - |
| BC-05 | 5 | - | - |
| BC-06 | 1 | - | - |
| BC-07 | 2 | - | - |
| BC-08 | 1 | - | - |
| BC-09 | 6 | - | - |
| BC-09 | - | - | - |
| BC-10 | 6 | - | - |
| BC-11 | - | - | - |
| BC-12 | - | - | - |
| 11 | 5 | - | - |
| 12 | 2 | - | - |
| BC-13 | 1 | - | - |
| 14 | 3 | - | - |

**Recent Trend:**

- Last 5 plans: —
- Trend: —

*Updated after each plan completion*
| Phase BC-01 P01 | 5min | 2 tasks | 71 files |
| Phase BC-01 P02 | 7min | 3 tasks | 12 files |
| Phase BC-01 P03 | 15min | 3 tasks | 10 files |
| Phase BC-01 P04 | 30min | 3 tasks | 5 files |
| Phase BC-01 P05 | 15min | 2 tasks | 4 files |
| Phase BC-01 P06 | 5min | 1 tasks | 1 files |
| Phase BC-02 P01 | 8min | 2 tasks | 8 files |
| Phase 02 P02 | 20min | 3 tasks | 3 files |
| Phase 02 P03 | 12min | 2 tasks | 5 files |
| Phase BC-03 P01 | 12min | 2 tasks | 4 files |
| Phase BC-03 P02 | 20min | 3 tasks | 4 files |
| Phase BC-03 P03 | 12min | 3 tasks | 4 files |
| Phase BC-03 P04 | 25min | 2 tasks | 3 files |
**Per-Plan Metrics:**

| Plan | Duration | Tasks | Files |
|------|----------|-------|-------|
| Phase 04 P03 | 50min | 3 tasks | 2 files |
| Phase BC-05 P01 | 35min | 3 tasks | 3 files |
| Phase BC-05 P02 | 35min | 2 tasks | 1 files |
| Phase BC-05 P03 | 35min | 3 tasks | 1 files |
| Phase BC-05 P04 | 35min | 3 tasks | 2 files |
| Phase BC-06 P01 | 6min | 3 tasks | 8 files |
| Phase BC-07 P01 | 15 min | 3 tasks | 4 files |
| Phase BC-07 P02 | 1min | 1 tasks | 1 files |
| Phase BC-08 P01 | 15min | 2 tasks | 3 files |
| Phase BC-09 P01 | 6min | 3 tasks | 14 files |
| Phase BC-09 P02 | 4min | 2 tasks | 3 files |
| Phase BC-09 P03 | 53min | 3 tasks | 3 files |
| Phase BC-09 P04 | 25min | 3 tasks | 10 files |
| Phase BC-09 P05 | 25min | 3 tasks | 4 files |
| Phase BC-09 P06 | 15min | 2 tasks | 3 files |
| Phase BC-10 P01 | 28min | 3 tasks | 5 files |
| Phase BC-10 P02 | 4min | 3 tasks | 6 files |
| Phase BC-10 P03 | 7min | 3 tasks | 4 files |
| Phase BC-10 P04 | 5min | 3 tasks | 5 files |
| Phase BC-10 P05 | 30min | 2 tasks | 2 files |
| Phase BC-10 P06 | 3min | 2 tasks | 3 files |
| Phase BC-13 P01 | 5min | 2 tasks | 3 files |
| Phase BC-14 P01 | 4min | 2 tasks | 7 files |
| Phase BC-14 P02 | 3min | 2 tasks | 4 files |
| Phase BC-14 P03 | 3min | 2 tasks | 5 files |

## Accumulated Context

### Decisions

**v1.12 roadmap sequencing (fixed at roadmap creation, 2026-07-22):** Phase 13 (Star Shape Core) is
pure C#, zero database dependency, purely additive — `Star5Shape`/`Star5Geometry` implement
`IShapeDefinition` in isolation and all 500 existing tests stay green. **Phase 13 deliberately does
not register the shape** (user decision at roadmap approval, 2026-07-22): `V11CutoverTests` lines 58
and 73 assert `count(*) FROM public.figure_types == 4` against scratch databases in `Additive` and
`FreshUsersOnly` state — the two states that *do* run `SeedFigureTypesAsync` — so registration turns
them red and is not additive. Registration moves to Phase 14 with the seed, which also updates those
two assertions 4→5; Phase 13's tests instantiate `Star5Shape` directly, as `PentagonShape`'s do.
Phase 14 (Catalog Seed, Toolbar & Decisions) lands the hard prerequisite for writing any star row — the idempotent
`figure_types` seed fix (MODEL-08), since `V11Cutover.EnsureAsync` returns early at
`CatalogState.Completed` on the existing database and would otherwise leave `star5`'s foreign key
unsatisfiable forever — together with the seventh toolbar button and the decision-doc amendments that
button requires. Phase 15 (Draw, Preview, Render & Persist a Star) is the first phase that actually
writes and renders a star, depending on both prior phases. Phase 16 (Interaction, Sync & Test Guards)
proves selection/drag/delete and live cross-tab sync match the four existing shapes, and folds in
this milestone's test guards (drift guard, bbox agreement, degenerate/malformed rejection) once all
the code they guard exists. Phase 17 (Regression Verification, REG-02) is deliberately last and
stands alone — the milestone's human acceptance gate, mirroring v1.11's Phase 12 — and must not be
folded into any earlier phase's automated tests.

**All 69 ADR decisions (D-01…D-69) are LOCKED.** They are in PROJECT.md `<decisions>`; full text in
`.planning/intel/decisions.md` and `docs/DECISIONS.md`. They are **not open questions** — do not
re-litigate, re-ask, or "improve" them. v1.12's own decisions land by name from **D-70** onward as
Phase 14's ARCH-02 work.

> ⚠️ **v1.11 supersedes part of the locked set.** D-59…D-69 replace the storage model. **The
> documents describe the new model; the code implements it.** Superseded and therefore **NOT to be
> preserved**: **D-12** (two tables), **D-20** (integer coords), **D-22** (four coordinates =
> bounding box), **D-39** (`id` is the z-order), **D-41** (line normalisation), **D-46** (`type` text
> + CHECK, no `created_at`), **D-50** (per-type guard mirroring CHECKs).

The rules most likely to be violated by accident:

- **D-40:** a `move` broadcast is **UPDATE-ONLY, never insert.** D-11's original "idempotent upsert"
  was a **bug** — it resurrects deleted figures. **Unchanged by v1.11 or v1.12.**

- **D-54:** mid-drag, a tab discards **ALL** incoming broadcasts, not just those about the dragged
  figure. **Unchanged by v1.11 or v1.12.**

- **D-53:** the sync contract's *payload* changed in v1.11 (uuid ids, position deltas); its **rules
  hold** for the star too — kinds, echo filter, no `drop` kind, previews never broadcast.

- **D-08:** plaintext passwords are **deliberate and locked**. Do not "fix" this.
- **D-24/D-36:** the clamp survives, reads **`bbox_*`**, not coordinate columns. Still clamp the
  *delta*, then translate; still `clamp → render → broadcast`. Star5's `IsDrawable` uses the same
  width>0 AND height>0 rule rectangle already uses.

- **v1.11's landmines, still live in v1.12:** never trust `geometry`/`style` off the wire (parse →
  validate → re-serialise from the record); `bbox_*` is a cache recomputed in **exactly one place**;
  `z` is unique per canvas and **needs a retry** on concurrent insert.

- ~~**No JavaScript anywhere**~~ — **REMOVED in v1.1.** Hand-authored JS/interop is now permitted.

**v1.11 amendments (user-approved, 2026-07-21)** — recorded in `docs/DECISIONS.md` as D-59…D-69;
rationale and migration plan in `docs/DATA-MODEL-v1.11-DRAFT.md`; mirrored in PROJECT.md + intel.
Position/shape split · `geometry jsonb` per type · `numeric` coordinates · `uuid` ids · `z` unique
per canvas · four tables · validated `style jsonb` · `bbox_*` cache · geometry validated in C#, not
by CHECK constraints. **Goal: the last migration that touches data already written.**

**v1.1 amendments to the locked set (user-approved, 2026-07-20)** — recorded in `docs/DECISIONS.md`
with inline `⚠️ v1.1` notes, mirrored in PROJECT.md + intel:

- **D-19/D-36/D-58/D-18** — canvas **1280×720 → 1472×828** (may grow, never shrink; no migration).
- **D-31/D-58** — selection **red outline → blue+white dashed trace on the figure's own outline,
  topmost**, plus a lifecycle (tool stays armed; one selection at a time; deselect on
  canvas-outside-figure / arm-tool / toolbar-except-Delete).

- **D-06/D-18/D-33/D-37/D-57** — the **"no JavaScript" rule removed**; motivations corrected to MVP
  simplicity. Permissive, no code change.

**Earlier amendment (v1.0-era, user-approved):** D-27/D-58's Docker port. The compose file
publishes **host 5433 → container 5432**; a native `postgresql-x64-18` service permanently occupies
5432 on this machine. Recorded in `docs/DECISIONS.md` § "Docker Compose (D-27)" and PROJECT.md
Constraints. Intent untouched.

**Per-phase implementation decisions (v1.0):** cleared at milestone close. The full log lives in each
phase's `*-SUMMARY.md` under `key-decisions`, preserved in
`.planning/milestones/v1.0-ROADMAP.md` and in git history.

**Per-phase implementation decisions (v1.1):** cleared at milestone close; preserved in each phase's
`*-SUMMARY.md` under `key-decisions` in `.planning/milestones/v1.1-phases/` and in git history. The
one with lasting force: **BC-06** bound the `Home.razor` SVG dimensions to `CanvasBounds` rather than
repeating numeric literals, keeping rendered size and clamp size from drifting — v1.11's renderer
rewrite must not reintroduce those literals.

**v1.11 scope decisions (user-approved at milestone open, 2026-07-21):**

- **Existing figures are migrated, not dropped** — the draft's exact conversion formulas, with a
  v1.1-dump replay test checking rendered vertices and layer order.

- **Zero user-visible change** is a hard invariant, not an aspiration. Newly-unlocked capabilities
  (rotation, vertex editing, z-order control, per-figure style) ship *possible* and *unused*.

- **The `IShapeDefinition` registry is in scope** — the eight scattered type-specific sites collapse
  now, rather than being paid for by v1.2 while it also adds nine shapes.

- **Tests are rebased, not ported** — retire those whose subject no longer exists (inscribed-square
  round-trip, line-normalisation landmine, the 32-case CHECK-mirror matrix); write new guards for
  bbox-vs-geometry agreement, the validation gateway, and z-collision retry.

**v1.11 roadmap sequencing (fixed at roadmap creation, 2026-07-21):** Phase 9 (Shape Registry &
Validation Gateway) is pure C#, zero database dependency, purely additive — the existing app and all
405 tests stay untouched and green. Phase 10 (Storage Schema, Migration & Persistence Layer) is also
additive: new tables, new persistence layer, and the migration/dump-replay proof, built and tested in
isolation while `Home.razor`/`FigureShape.razor`/the old table remain untouched and green. Phase 11
(Renderer, Sync & Cutover) is the one phase where the app is briefly between models — contained
entirely within its own plan sequence, ending with the old table, old code paths, and dead tests
removed and the full rebased suite green again. Phase 12 (Regression Verification, REG-01) is
deliberately last — the milestone's real human acceptance gate, and it must not be skipped or folded
into an earlier phase's automated tests.

- [Phase BC-09]: Geometry and style must be parsed, validated, and re-serialised only through `FigureInputGateway`; Phase 10 persistence and Phase 11 sync must use it for every write.
- [Phase BC-09]: Line and triangle retain ordered local vertices; circle geometry uses the bounding square's top-left origin, with centre `(R, R)`.
- [Phase BC-09]: The redacted, checksum-sealed fixture and rows 3860–3867 are the fixed Phase 10 migration proof input; do not recapture it.
- [Phase BC-10]: New storage tables remain in v11, preserving public.figures until Phase 11 cutover.
- [Phase BC-10]: bbox_* stores local geometry extent so moves write only x and y.
- [Phase BC-10]: Registry seeding uses parameterised ON CONFLICT handling for concurrent callers.
- [Phase ?]: D-60 conversion preserves legacy line point order; it never canonicalises a diagonal.
- [Phase ?]: D-62 legacy IDs map deterministically into frozen, namespace-separated version-8 UUID layouts.
- [Phase ?]: FigureRepository retries at most five times and only for z_unique_per_canvas unique violations.
- [Phase ?]: Phase 11 substitutes FigureRepository for FigureStore; Phase 10 keeps the running application untouched.
- [Phase ?]: V11 migration applies schema and seed, canvases, then figures in one transaction; dropping old tables remains Phase 11 work.
- [Phase ?]: Replay uses deterministic legacy-id mappings and a guarded GUID scratch database with checksum verification.
- [Phase ?]: Migrated created_at is the migration timestamp (D-68); Phase 11 invokes migration before cutover.
- [Phase ?]: The bbox agreement guard scans all v11.figures rows, so every future writer must preserve the local cache invariant.
- [Phase ?]: Stored style JSONB is checked as a key set because PostgreSQL does not preserve object insertion order.
- [Phase ?]: The D-60 geometry-CHECK gap remains explicit: the gateway is the last validation boundary and raw probes roll back.
- [Phase ?]: Star5Shape remains unregistered in DefaultShapes during Phase 13; Phase 14 owns registry/catalog exposure.
- [Phase ?]: Star5Geometry.InnerRatio is required and preserved, but bounds remain a pure function of Points.
- [Phase BC-14]: Star5Shape now participates in the default registry and figure_types seed order immediately after triangle. — MODEL-08 requires newly registered shapes to become writable through registry-driven startup seeding.
- [Phase BC-14]: Completed public catalogs seed missing registry-owned figure_types rows idempotently instead of remaining exact no-ops. — Existing completed databases must gain star5 without migration or manual SQL while preserving transaction/advisory-lock boundaries.
- [Phase BC-14]: Star is represented as an armable Tool enum value and maps to star5; Delete and logout remain action/form controls outside Tool.
- [Phase BC-14]: D-70 locks star5 as the fifth stretchable, point-up, corner-to-corner five-pointed star. — ARCH-02 requires named star decisions from D-70 onward.
- [Phase BC-14]: D-71 locks star geometry as ten ordered points plus required innerRatio, with points authoritative for render and bbox. — Future render and persistence phases need a stable storage contract.
- [Phase BC-14]: D-72 locks registry-owned figure_types startup seed convergence for completed public catalogs. — Existing completed databases must gain star5 without manual SQL or migration.
- [Phase BC-14]: D-73 locks the seven-control toolbar order with Star between Triangle and Delete, while Logout remains outside the count as a POST form. — CANV-04 and ARCH-02 require active docs to match the shipped toolbar.

### Pending Todos

None yet.

### Blockers/Concerns

**None open.** Both items raised at ingest were closed during v1.0 and are cleared here at milestone
close (detail preserved in `.planning/milestones/v1.0-ROADMAP.md` and git history):

- The **D-11/D-54 contradiction** — fixed at source in `docs/DECISIONS.md`; D-54's blanket mid-drag
  discard was built in BC-05-03 and re-confirmed by the v1.0 integration audit.

- **`.planning/config.json`** — created (`granularity: standard`, `project_code: BC`).

**Carried forward (not blocking):** ~11 low-severity items from `01-REVIEW.md` (WR-03…WR-07, WR-09,
IN-01…IN-05), recorded in `.planning/milestones/v1.0-MILESTONE-AUDIT.md`. None blocks a requirement.
WR-01 and WR-08 are locked-by-design (D-36, D-08) and are **not** debt.

## Deferred Items

Items acknowledged and carried forward from previous milestone close:

| Category | Item | Status | Deferred At |
|----------|------|--------|-------------|
| *(none)* | | | |

## Session Continuity

Last session: 2026-07-22T20:19:59.702Z
Stopped at: Completed BC-14-03-PLAN.md
Resume file: None

## Operator Next Steps

- Run `/gsd-plan-phase 15` to plan Draw, Preview, Render & Persist a Star.
