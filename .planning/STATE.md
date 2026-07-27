---
gsd_state_version: 1.0
milestone: v1.12
milestone_name: Canvas enlargement
status: Awaiting next milestone
stopped_at: Milestone v1.12 completed, archived, and tagged
last_updated: "2026-07-27T14:43:04.952Z"
last_activity: 2026-07-27
last_activity_desc: Milestone v1.12 completed and archived
progress:
  total_phases: 1
  completed_phases: 1
  total_plans: 2
  completed_plans: 2
  percent: 100
current_phase: 11
current_phase_name: canvas-enlargement-to-5760-2634
---

# Project State

## Project Reference

See: .planning/PROJECT.md (updated 2026-07-27 — after v1.12 milestone close)

**Core value:** The canvas is always the truth, everywhere at once — what you draw persists instantly,
and every other tab shows it happening live, including a figure gliding in real time as you drag it.

**Current focus:** No milestone open. **v1.12 (Canvas enlargement) shipped 2026-07-27** — canvas
enlarged to 5760×2634 (3×3 cells), native-scroll navigation, fixed toolbar; tagged
`v1.12-canvas-enlargement`; D-19/D-43/D-18 amendments logged in `docs/DECISIONS.md`. Next step:
`/gsd-new-milestone`.

**Next milestone v1.2** (new figures + dynamic toolbar) is scoped in
`.planning/backlog/v1.2-figures-and-toolbar.md`, sequenced **after** v1.12 — its 4-int-bbox premise
no longer holds now that v1.11 has shipped, so the backlog must be revised before it opens.

## Current Position

Phase: Milestone v1.12 complete
Plan: —
Status: Awaiting next milestone
Last activity: 2026-07-27 — Milestone v1.12 completed and archived

## Performance Metrics

**Velocity:**

- Total plans completed: 35
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
| BC-10 | 6 | - | - |
| BC-11 | 2 | - | - |

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
| Phase 10 P01 | 8min | 3 tasks | 10 files |
| Phase 10 P02 | 20min | 3 tasks | 3 files |
| Phase 10 P03 | 15min | 3 tasks | 5 files |
| Phase 10 P04 | 20min | 2 tasks | 6 files |
| Phase 10 P05 | 25min | 3 tasks | 8 files |
| Phase BC-11 P01 | 12min | 3 tasks | 8 files |

## Accumulated Context

### Decisions

**All 59 ADR decisions (D-01…D-59) are LOCKED.** They are in PROJECT.md `<decisions>`; full text in
`.planning/intel/decisions.md` and `docs/DECISIONS.md`. They are **not open questions** — do not
re-litigate, re-ask, or "improve" them.

The ones most likely to be violated by accident:

- **D-19 (v1.12, amended — logged 2026-07-27):** canvas grew **1472×828 → 5760×2634** (a 3×3 grid of
  1920×878 cells) — pure growth, size may never shrink. Existing figures stay valid, unmoved, no
  migration.

- **D-43 (v1.12, amended — logged 2026-07-27):** the toolbar is now `position: fixed`; the canvas
  carries a 48px top offset so the `canvasX = PageX`, `canvasY = PageY − 48` mapping is preserved
  unchanged at any scroll position. Landmine still applies: `PageX`/`PageY`, never `OffsetX`/`OffsetY`.

- **D-18 (v1.12, amended — logged 2026-07-27):** native browser scroll is now the actual navigation
  mechanism for reaching the rest of the enlarged canvas — previously an "accepted cost", now used on
  purpose. **D-04 is untouched: this is explicitly not a pan feature.** No JS interop is introduced.

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
  *(v1.12 does not need it either — `PageX`/`PageY` already include scroll.)*

**v1.12 amendments (logged in `docs/DECISIONS.md` at milestone close, 2026-07-27):** D-19 (size 1472×828
→ 5760×2634), D-43 (toolbar `position: fixed`; −48 invariant preserved), D-18 (scroll is now the
navigation mechanism) — inline `⚠️ v1.12` banners added, mirrored in PROJECT.md + intel. D-04 untouched.

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
size constant from the v1.1 amendment survives, and v1.12 grows it further.)*

**Earlier amendment (v1.0-era, user-approved):** D-27/D-58's Docker port. The compose file
publishes **host 5433 → container 5432**; a native `postgresql-x64-18` service permanently occupies
5432 on this machine. Recorded in `docs/DECISIONS.md` § "Docker Compose (D-27)" and PROJECT.md
Constraints. Intent untouched.

**Per-phase implementation decisions (v1.0/v1.1/v1.11):** cleared at milestone close. The full log
lives in each phase's `*-SUMMARY.md` under `key-decisions`, preserved in
`.planning/milestones/v1.0-ROADMAP.md` / `v1.1-ROADMAP.md` / `v1.11-ROADMAP.md` and in git history.

**Per-phase implementation decisions (v1.1/v1.11/v1.12):** cleared at milestone close. The full log
lives in each phase's `*-SUMMARY.md` under `key-decisions`, preserved in
`.planning/milestones/v1.1-ROADMAP.md` / `v1.11-ROADMAP.md` / `v1.12-phases/` and in git history.
(v1.12's sole roadmap decision — keeping all 3 requirements in one tightly-coupled phase rather than
padding a split — is recorded in `.planning/milestones/v1.12-ROADMAP.md`.)

### Pending Todos

None yet.

### Blockers/Concerns

**None open.** Both v1.0-era items raised at ingest were closed during v1.0 and are cleared here at
milestone close (detail preserved in `.planning/milestones/v1.0-ROADMAP.md` and git history):

- The **D-11/D-54 contradiction** — fixed at source in `docs/DECISIONS.md`; D-54's blanket mid-drag
  discard was built in BC-05-03 and re-confirmed by the v1.0 integration audit.

- **`.planning/config.json`** — exists (`granularity: standard`, `project_code: BC`); unchanged for
  v1.12.

**Carried forward (not blocking):** ~11 low-severity items from `01-REVIEW.md` (WR-03…WR-07, WR-09,
IN-01…IN-05), recorded in `.planning/milestones/v1.0-MILESTONE-AUDIT.md`. None blocks a requirement.
WR-01 and WR-08 are locked-by-design (D-08; D-36's clamp rule itself is now dropped by D-59) and are
**not** debt.

## Deferred Items

Items acknowledged and carried forward from previous milestone close:

| Category | Item | Status | Deferred At |
|----------|------|--------|-------------|
| Storage | STOR-F1 — off-canvas figure recovery (pan / bring-back) | Partially improved by v1.12 (scroll now reaches any figure inside the enlarged surface); figures dragged past even the larger edge remain unrecoverable until the zoom/pan camera (CANV-F1) lands | v1.11 requirements (2026-07-23); revisited v1.12 requirements (2026-07-27) |
| Storage | STOR-F2 — multi-canvas per user (`canvases` table) | Deferred; cheap additive migration when wanted | v1.11 requirements (2026-07-23) |
| Figures/toolbar | FIG-05, TOOL-01 — new figure types + dynamic toolbar | Deferred to v1.2, sequenced after v1.12; backlog needs revision first | v1.11 requirements (2026-07-23) |
| Canvas | CANV-F1 — zoom/pan viewBox camera | Deferred to a future milestone; pure additive upgrade over v1.12's fixed logical space | v1.12 requirements (2026-07-27) |

## Session Continuity

Last session: 2026-07-27
Stopped at: Milestone v1.12 completed, archived, and tagged (`v1.12-canvas-enlargement`)
branch v1.12-Execution. No milestone open.
Resume file: None

## Operator Next Steps

- Start the next milestone with /gsd-new-milestone
