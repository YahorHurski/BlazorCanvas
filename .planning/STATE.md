---
gsd_state_version: 1.0
milestone: v1.11
milestone_name: Storage model rewrite (anchor + geometry JSON)
current_phase: 9
current_phase_name: Schema, Entity & Data-Preserving Migration
status: planning
stopped_at: Completed 09-01-PLAN.md
last_updated: "2026-07-23T17:55:16.630Z"
last_activity: 2026-07-23
last_activity_desc: ROADMAP.md written and revised to 2 phases (9–10) per user; the 9 v1.11
progress:
  total_phases: 2
  completed_phases: 0
  total_plans: 6
  completed_plans: 1
  percent: 0
---

# Project State

## Project Reference

See: .planning/PROJECT.md (updated 2026-07-23 — v1.11 opened)

**Core value:** The canvas is always the truth, everywhere at once — what you draw persists instantly,
and every other tab shows it happening live, including a figure gliding in real time as you drag it.

**Current focus:** Milestone v1.11 — storage model rewrite. Roadmap is now written (Phases 9–10,
9/9 requirements mapped). No new user-facing feature; a database model change (anchor `x,y` +
`geometry jsonb`, D-59) plus the downstream code churn it forces, with every existing figure
preserved via a tested data migration and the canvas-edge clamp removed. Phase 9 = schema + entity +
data-preserving migration; Phase 10 = geometry/draw/drag/sync rework (no clamp) + full test regression.

**Next milestone v1.2** (new figures + dynamic toolbar) is scoped in
`.planning/backlog/v1.2-figures-and-toolbar.md`, sequenced **after** v1.11 — its 4-int-bbox premise
no longer holds once v1.11 ships and the backlog must be revised before it opens.

## Current Position

Phase: 9 of 10 (Schema, Entity & Data-Preserving Migration)
Plan: — (not yet planned)
Status: Roadmap complete — ready to plan
Last activity: 2026-07-23 — ROADMAP.md written and revised to 2 phases (9–10) per user; the 9 v1.11
requirements are mapped with 100% coverage, REQUIREMENTS.md traceability filled in.

## Performance Metrics

**Velocity:**

- Total plans completed: 21
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

## Accumulated Context

### Decisions

**All 59 ADR decisions (D-01…D-59) are LOCKED.** They are in PROJECT.md `<decisions>`; full text in
`.planning/intel/decisions.md` and `docs/DECISIONS.md`. They are **not open questions** — do not
re-litigate, re-ask, or "improve" them.

The ones most likely to be violated by accident:

- **D-59 (v1.11, authoritative storage model):** a figure is now an **anchor `x,y` + `geometry
  jsonb`**, relative to the anchor. Drag updates **only `x,y`**, for every shape — the geometry never
  changes on a move. Supersedes D-22 (circle was the inscribed square) and D-39 (`id` was a sequential
  integer / z-order); order is now carried by an explicit `numeric z`.

- **D-59 also drops the canvas-edge clamp** (D-24/D-29/D-36 dropped) — figures may leave the canvas.
  No DB CHECK on `geometry` — the server is the sole writer; `type text` + whitelist CHECK stays.

- **D-40:** a `move` broadcast is **UPDATE-ONLY, never insert.** D-11's original "idempotent upsert"
  was a **bug** — it resurrects deleted figures. Unchanged in v1.11; only the payload shape changes
  (D-53 amended by D-59).

- **D-54:** mid-drag, a tab discards **ALL** incoming broadcasts, not just those about the dragged figure.
- **D-50:** the minimum-size guard is **per-type** — a zero-height *line* is legal (it is a horizontal
  line); a zero-height rectangle is not. In v1.11 this guard is kept **code-side only** (STOR-03).

- **D-08:** plaintext passwords are **deliberate and locked**. Do not "fix" this.
- ~~**No JavaScript anywhere**~~ — **REMOVED in v1.1.** Hand-authored JS/interop is now permitted;
  the rule was never load-bearing (MVP simplicity was the real motivation; D-06/18/33/37/57 re-worded
  in `docs/DECISIONS.md`). It changed no code and is simply not *needed* for anything built so far.

**v1.11 amendments to the locked set (user-approved, 2026-07-23)** — recorded in `docs/DECISIONS.md`
as new **D-59** plus inline `⚠️ v1.11` banners, mirrored in PROJECT.md + intel:

- **D-22 superseded** — circle now stored as `{r}`, not the inscribed square.
- **D-39 superseded** — `id` `integer` → `uuid`; order carried by `numeric z`.
- **D-24 / D-29 / D-36 dropped** — no canvas-edge clamp.
- **D-53 amended** — sync payload → anchor + geometry.
- **D-46 amended** — `created_at` stays dropped; `type text` + CHECK whitelist kept.
- **D-23 amended** — degenerate-draw guard kept code-side; "never clamp individually" is moot.
- **D-41 re-expressed** — normalisation for anchor+geometry; the line swap-pair landmine carries over.
- **D-20 / D-12 / D-03 upheld.**

**Earlier v1.1 amendments (still active):** D-19/D-36*/D-58/D-18 — canvas **1280×720 → 1472×828**
(may grow, never shrink); D-31/D-58 — selection blue+white dashed trace + lifecycle; D-06/D-18/D-33/
D-37/D-57 — the "no JavaScript" rule removed. *(D-36 itself is now dropped by D-59; only the canvas
size constant from the v1.1 amendment survives.)*

**Earlier amendment (v1.0-era, user-approved):** D-27/D-58's Docker port. The compose file
publishes **host 5433 → container 5432**; a native `postgresql-x64-18` service permanently occupies
5432 on this machine. Recorded in `docs/DECISIONS.md` § "Docker Compose (D-27)" and PROJECT.md
Constraints. Intent untouched.

**Per-phase implementation decisions (v1.0/v1.1):** cleared at milestone close. The full log lives in
each phase's `*-SUMMARY.md` under `key-decisions`, preserved in `.planning/milestones/v1.0-ROADMAP.md`
/ `v1.1-ROADMAP.md` and in git history.

**Roadmap decisions (v1.11, this session):** Phase 9 groups the new schema/entity with the data
migration and its round-trip test (STOR-01, MIG-01, MIG-02) — the migration cannot be built or tested
without the new entity existing first, and this is the milestone's headline data-preservation risk, so
it is kept together and sequenced first. Phase 10 groups **all** downstream app-layer churn on the new
model (STOR-02, STOR-03, STOR-04, STOR-05, SYNC-02, TEST-02) — one coherent "the app works correctly on
the new model, with no edge clamp, and stays fully tested" capability. **Revised from an initial
4-phase draft (user, 2026-07-23):** the D-53 sync-payload rework was folded in because broadcasting
lives in the very same `Home.razor` draw/drag handlers Phase 10 rewrites (order is now
`render → broadcast`), and the test rework was distributed rather than made a trailing phase because
this project builds tests alongside code (TDD-first). The clean build + full green suite is Phase 10's
closing success criterion, not a separate phase.

- [Phase BC-06]: Bound the Home.razor SVG dimensions to CanvasBounds instead of repeating numeric literals, keeping rendered size and clamp size from drifting. — This preserves D-18/D-19/D-36 as one source of truth after the canvas resize.
- [Phase BC-08]: Retired the application-authored JavaScript prohibition; future JavaScript or interop requires a separate affirmative decision.
- [Phase BC-08]: Reconciled CONSTRAINT-env and the D-11 rejection rationale with the ADR while preserving MVP and behavioural decisions.

### Pending Todos

None yet.

### Blockers/Concerns

**None open.** Both v1.0-era items raised at ingest were closed during v1.0 and are cleared here at
milestone close (detail preserved in `.planning/milestones/v1.0-ROADMAP.md` and git history):

- The **D-11/D-54 contradiction** — fixed at source in `docs/DECISIONS.md`; D-54's blanket mid-drag
  discard was built in BC-05-03 and re-confirmed by the v1.0 integration audit.

- **`.planning/config.json`** — exists (`granularity: standard`, `project_code: BC`); unchanged for
  v1.11.

**Carried forward (not blocking):** ~11 low-severity items from `01-REVIEW.md` (WR-03…WR-07, WR-09,
IN-01…IN-05), recorded in `.planning/milestones/v1.0-MILESTONE-AUDIT.md`. None blocks a requirement.
WR-01 and WR-08 are locked-by-design (D-08; D-36's clamp rule itself is now dropped by D-59) and are
**not** debt.

## Deferred Items

Items acknowledged and carried forward from previous milestone close:

| Category | Item | Status | Deferred At |
|----------|------|--------|-------------|
| Storage | STOR-F1 — off-canvas figure recovery (pan / bring-back) | Deferred to a future milestone, once canvas bounds are redesigned | v1.11 requirements (2026-07-23) |
| Storage | STOR-F2 — multi-canvas per user (`canvases` table) | Deferred; cheap additive migration when wanted | v1.11 requirements (2026-07-23) |
| Figures/toolbar | FIG-05, TOOL-01 — new figure types + dynamic toolbar | Deferred to v1.2, sequenced after v1.11; backlog needs revision first | v1.11 requirements (2026-07-23) |

## Session Continuity

Last session: 2026-07-23T17:55:16.613Z
Stopped at: Completed 09-01-PLAN.md
captured D-59's five plan-time decisions. Committed db63895 + 0a3878f on branch v1.11.
Resume file: None

## Operator Next Steps

- Execute Phase 9 (Schema, Entity & Data-Preserving Migration) with `/gsd-execute-phase 9` — 6 plans
  in 5 waves, verified. Waves 3–5 need Compose Postgres up (`docker compose up -d --wait`, host 5433).

- Then plan Phase 10 (geometry/draw/drag/sync rework + regression) with `/gsd-plan-phase 10`.
