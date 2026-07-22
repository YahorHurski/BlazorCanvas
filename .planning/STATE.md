---
gsd_state_version: 1.0
milestone: v1.11
milestone_name: Storage Model Rewrite
current_phase: 11
current_phase_name: Renderer, Sync & Cutover
status: planning
stopped_at: Completed BC-10-06-PLAN.md; re-verification required after migration atomicity gap closure.
last_updated: "2026-07-22T12:36:09.288Z"
last_activity: 2026-07-22
last_activity_desc: Phase BC-10 complete, transitioned to Phase 11
progress:
  total_phases: 4
  completed_phases: 2
  total_plans: 12
  completed_plans: 12
  percent: 50
---

# Project State

## Project Reference

See: .planning/PROJECT.md (updated 2026-07-22 — Phase BC-09 complete)

**Core value:** The canvas is always the truth, everywhere at once — what you draw persists instantly,
and every other tab shows it happening live, including a figure gliding in real time as you drag it.

**Current focus:** Phase 10 — Storage Schema, Migration & Persistence Layer
Replace the four-integer bounding-box model with the position/shape split (D-59…D-69): four tables,
`x, y, rotation` + `geometry jsonb` in local coordinates, `numeric` coords, `uuid` ids, a `z` column
unique per canvas, validated `style`, and a `bbox_*` cache. **Phase BC-09 delivered the pure-C# shape
registry and validation foundation; schema, persistence, renderer, and sync cutover remain.**

**Scope boundary confirmed with the user:** a pure storage swap — **zero user-visible change**. Same
four shapes, same three verbs, same look, same live cross-tab glide. Rotation, vertex editing,
z-order and per-figure style become possible and stay unused. Includes the lossless migration with a
v1.1-dump replay test, the `IShapeDefinition` registry, and a retire-and-rewrite test rebase.

**v1.1 shipped 2026-07-21** (canvas 1472 × 828, selection lifecycle + restyle, no-JS rule removed);
archived under `.planning/milestones/v1.1-*`. **v1.2** (new figures + dynamic toolbar) waits in
`.planning/backlog/v1.2-figures-and-toolbar.md` and gets cheaper once v1.11 lands.

## Current Position

Phase: 11 — Renderer, Sync & Cutover
Plan: Not started
Status: Ready to plan
Last activity: 2026-07-22 — Phase BC-10 complete, transitioned to Phase 11
all 22 v1.11 requirements mapped, 100% coverage, no orphans, no duplicates.

Progress: [██████████] 100%

## Performance Metrics

**Velocity:**

- Total plans completed: 33
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

## Accumulated Context

### Decisions

**All 69 ADR decisions (D-01…D-69) are LOCKED.** They are in PROJECT.md `<decisions>`; full text in
`.planning/intel/decisions.md` and `docs/DECISIONS.md`. They are **not open questions** — do not
re-litigate, re-ask, or "improve" them.

> ⚠️ **v1.11 supersedes part of the locked set.** D-59…D-69 replace the storage model. **The
> documents describe the new model; the code still implements the old one.** When reading code, expect
> the four-integer model; when writing code this milestone, build the new one. Superseded and
> therefore **NOT to be preserved**: **D-12** (two tables), **D-20** (integer coords), **D-22** (four
> coordinates = bounding box), **D-39** (`id` is the z-order), **D-41** (line normalisation),
> **D-46** (`type` text + CHECK, no `created_at`), **D-50** (per-type guard mirroring CHECKs).

The rules most likely to be violated by accident:

- **D-40:** a `move` broadcast is **UPDATE-ONLY, never insert.** D-11's original "idempotent upsert"
  was a **bug** — it resurrects deleted figures. **Unchanged by v1.11.**

- **D-54:** mid-drag, a tab discards **ALL** incoming broadcasts, not just those about the dragged
  figure. **Unchanged by v1.11.**

- **D-53:** the sync contract's *payload* changes in v1.11 (uuid ids, position deltas); its **rules
  hold** — kinds, echo filter, no `drop` kind, previews never broadcast.

- **D-08:** plaintext passwords are **deliberate and locked**. Do not "fix" this.
- **D-24/D-36:** the clamp survives, but it now reads **`bbox_*`**, not coordinate columns. Still
  clamp the *delta*, then translate; still `clamp → render → broadcast`.

- **v1.11's own new landmines:** never trust `geometry`/`style` off the wire (parse → validate →
  re-serialise from the record); `bbox_*` is a cache recomputed in **exactly one place**; `z` is
  unique per canvas and **needs a retry** on concurrent insert or a figure silently never appears.

- ~~**No JavaScript anywhere**~~ — **REMOVED in v1.1.** Hand-authored JS/interop is now permitted;
  the rule was never load-bearing (MVP simplicity was the real motivation; D-06/18/33/37/57 re-worded
  in `docs/DECISIONS.md`). It changed no code and is simply not *needed* for anything built so far.

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

Last session: 2026-07-22T12:00:25.490Z
Stopped at: Completed BC-10-06-PLAN.md; re-verification required after migration atomicity gap closure.
Phase BC-09 verified passed (45/45 must-haves), UAT approved, 22/22 requirements mapped.
Resume file: None

## Operator Next Steps

- Re-run Phase 10 verification; the invalid-row regression now proves no v11 schema or type-seed residue.
- Phase 11 performs cutover; Phase 12 remains the final human regression gate.
