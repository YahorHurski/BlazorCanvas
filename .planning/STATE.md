---
gsd_state_version: 1.0
milestone: v1.1
milestone_name: Canvas resize · selection UX · no-JS removal
status: planning
last_updated: "2026-07-20T21:00:00.000Z"
last_activity: 2026-07-20
progress:
  total_phases: 3
  completed_phases: 0
  total_plans: 0
  completed_plans: 0
  percent: 0
---

# Project State

## Project Reference

See: .planning/PROJECT.md (updated 2026-07-20 — v1.1 requirements + amended constraints)

**Core value:** The canvas is always the truth, everywhere at once — what you draw persists instantly,
and every other tab shows it happening live, including a figure gliding in real time as you drag it.
**Current focus:** **v1.1** — four user-approved changes, decisions already amended in
`docs/DECISIONS.md` (see PROJECT.md → *Requirements → Active*): (1) canvas enlarged to **1472 × 828**;
(2) **selection lifecycle** fix (tool stays armed, one selection at a time, toolbar-press deselects
except Delete); (3) **selection restyle** to a blue+white dashed trace on the figure's own shape;
(4) **no-JS rule removed** (motivations corrected on D-06/18/33/37/57). No migration; mostly geometry
tests + a selection-overlay refactor. **Next milestone v1.2** (new figures + dynamic toolbar) is
scoped in `.planning/backlog/v1.2-figures-and-toolbar.md`.

## Current Position

Phase: 6 of 8 (Canvas Resize to 1472×828) — first phase of v1.1
Plan: — (roadmap created; no plans yet)
Status: Ready to plan
Last activity: 2026-07-20 — Roadmap created for v1.1 (Phases 6–8: Canvas Resize, Selection
Lifecycle & Restyle, Architecture Constraint Cleanup); REQUIREMENTS.md traceability filled
(4/4 mapped, 0 unmapped)

Progress: [░░░░░░░░░░] 0%

## Performance Metrics

**Velocity:**

- Total plans completed: 23
- Average duration: —
- Total execution time: —

**By Phase:**

| Phase | Plans | Total | Avg/Plan |
|-------|-------|-------|----------|
| BC-02 | 3 | - | - |
| BC-03 | 5 | - | - |
| BC-04 | 4 | - | - |
| BC-05 | 5 | - | - |

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

## Accumulated Context

### Decisions

**All 58 ADR decisions (D-01…D-58) are LOCKED.** They are in PROJECT.md `<decisions>`; full text in
`.planning/intel/decisions.md` and `docs/DECISIONS.md`. They are **not open questions** — do not
re-litigate, re-ask, or "improve" them.

The ones most likely to be violated by accident:

- **D-22 (REVISED):** a circle is stored as its **inscribed square**, not centre + rim point. The
  original encoding is REVERSED and dead.

- **D-40:** a `move` broadcast is **UPDATE-ONLY, never insert.** D-11's original "idempotent upsert"
  was a **bug** — it resurrects deleted figures.

- **D-54:** mid-drag, a tab discards **ALL** incoming broadcasts, not just those about the dragged figure.
- **D-50:** the minimum-size guard is **per-type** — a zero-height *line* is legal (it is a horizontal
  line); a zero-height rectangle is not.

- **D-08:** plaintext passwords are **deliberate and locked**. Do not "fix" this.
- ~~**No JavaScript anywhere**~~ — **REMOVED in v1.1.** Hand-authored JS/interop is now permitted;
  the rule was never load-bearing (MVP simplicity was the real motivation; D-06/18/33/37/57 re-worded
  in `docs/DECISIONS.md`). It changed no code and is simply not *needed* for anything built so far.

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

**Roadmap decisions (v1.1, this session):** CANV-03 is Phase 6 (independent — `CanvasBounds.cs` +
`Home.razor` SVG + geometry/clamp tests). SEL-01 + SEL-02 are combined into Phase 7 (both touch the
selection overlay in `Home.razor`; sequenced after Phase 6 to avoid overlapping edits to the same
file). ARCH-01 is its own tiny Phase 8 (doc/constraint verification only — no code) since its work
is unrelated in kind to the other two; likely a `/gsd-quick` candidate.

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

Last session: 2026-07-20T21:00:00.000Z
Stopped at: v1.1 roadmap created — Phases 6, 7, 8 written to ROADMAP.md; REQUIREMENTS.md
traceability filled (4/4 mapped, 0 unmapped); awaiting roadmap approval.
Resume file: None

## Operator Next Steps

- **⚠️ BRANCH WORKFLOW (user-directed):** ALL work stays on branch **`NewBranch`** — **never commit
  to `master` directly.** `git.branching_strategy = "none"` (config.json), so GSD commits to the
  current branch; **stay checked out on `NewBranch`** for every GSD command. `master` is updated only
  via a **single reviewed PR (`NewBranch → master`) at `/gsd-ship`**. Push `NewBranch` after each phase
  so progress is visible on GitHub. Do NOT set branching_strategy to phase/milestone (those fork off
  `origin/master`, which lacks the v1.1 amendments).

- **Roadmap created.** v1.1 has 3 phases: **Phase 6** (Canvas Resize to 1472×828, CANV-03),
  **Phase 7** (Selection Lifecycle & Restyle, SEL-01/SEL-02), **Phase 8** (Architecture Constraint
  Cleanup, ARCH-01). Full detail in `.planning/ROADMAP.md`. Next: review/approve the roadmap, then
  `/gsd-plan-phase 6`. Phases 7 and 8 are small enough that `/gsd-quick` may suit them.

- **v1.2 is scoped and waiting** in `.planning/backlog/v1.2-figures-and-toolbar.md` — start it only
  after v1.1 ships; its decision amendments happen at that point.

- Optional/independent: triage the ~11 low-severity `01-REVIEW.md` items.
