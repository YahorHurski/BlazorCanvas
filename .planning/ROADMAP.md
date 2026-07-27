# Roadmap: BlazorCanvas

## Milestones

- ✅ **v1.0 MinVP** — Phases 1–5 (shipped 2026-07-17)
- ✅ **v1.1 Canvas resize · selection UX · no-JS removal** — Phases 6–8 (shipped 2026-07-21)
- ✅ **v1.11 Storage model rewrite (anchor + geometry JSON)** — Phases 9–10 (shipped 2026-07-24)
- 🚧 **v1.12 Canvas enlargement** — Phase 11 (in progress, opened 2026-07-27)
- 📋 **v1.2 Figures & dynamic toolbar** — scoped, sequenced after v1.12, not started

v1.0 detail: [`milestones/v1.0-ROADMAP.md`](milestones/v1.0-ROADMAP.md) ·
[`milestones/v1.0-REQUIREMENTS.md`](milestones/v1.0-REQUIREMENTS.md) ·
[`milestones/v1.0-MILESTONE-AUDIT.md`](milestones/v1.0-MILESTONE-AUDIT.md)

v1.1 detail: [`milestones/v1.1-ROADMAP.md`](milestones/v1.1-ROADMAP.md) ·
[`milestones/v1.1-REQUIREMENTS.md`](milestones/v1.1-REQUIREMENTS.md) ·
phase artifacts archived under [`milestones/v1.1-phases/`](milestones/v1.1-phases/)

v1.11 detail: [`milestones/v1.11-ROADMAP.md`](milestones/v1.11-ROADMAP.md) ·
[`milestones/v1.11-REQUIREMENTS.md`](milestones/v1.11-REQUIREMENTS.md) ·
[`milestones/v1.11-MILESTONE-AUDIT.md`](milestones/v1.11-MILESTONE-AUDIT.md) ·
phase artifacts archived under [`milestones/v1.11-phases/`](milestones/v1.11-phases/)

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
<summary>✅ v1.11 Storage model rewrite (anchor + geometry JSON) (Phases 9–10) — SHIPPED 2026-07-24</summary>

**Milestone goal:** Replace the four-integer bounding-box storage (`x1,y1,x2,y2`, D-22) with the
extensible **anchor (`x,y`) + `geometry jsonb`** model (D-59), so future figure types need no schema
change. Every existing figure is **preserved via a tested data migration**; the canvas-edge clamp is
**removed**. Adds **no new user-facing feature** — a database model change plus the downstream code
churn it forces.

- [x] **Phase 9: Schema, Entity & Data-Preserving Migration** (6/6 plans) — completed 2026-07-23
      The new anchor+geometry `figures` schema (`uuid` id, `numeric z`, `geometry jsonb`, type
      whitelist CHECK only) and Figure entity, plus a hand-written backfill of every existing figure
      verified by a round-trip test against the immutable v1.1 fixture (all 795 rows).

- [x] **Phase 10: Geometry, Draw, Drag & Sync Rework (No Edge Clamp) + Regression** (6/6 plans) —
      completed 2026-07-24
      All four shapes draw, drag, delete, and sync live on the new model; the canvas-edge clamp is
      removed; the degenerate-draw guard and per-type normalisation are re-expressed in code; the
      D-53 broadcast payload carries anchor+geometry (UI-free `SyncReceiver`); a shared `ShapeRender`
      helper renders from anchor+geometry; and the full suite is reworked and green.

**Outcome:** 9/9 requirements validated (STOR-01…05, MIG-01/02, SYNC-02, TEST-02). Both phases
verified `passed`; milestone audit passed. 40 source/test files changed (+4,654/−817); `dotnet build`
clean (0/0), 416/416 tests passing. Two Phase-10 behaviours (pre-rewrite pixel comparison,
D-54 mid-drag blanket discard) accepted as non-blocking environmental caveats.

</details>

### 🚧 v1.12 Canvas enlargement (In Progress, opened 2026-07-27)

**Milestone goal:** Enlarge the fixed canvas from **1472×828 to 5760×2634** (a 3×3 grid of
1920×878 cells) so it feels borderless — a **pure size change**, D-19-compliant (grow-only, never
shrink). No scaling, no zoom/pan, no JS interop. The first screen on load is the **top-left cell** —
existing figures stay exactly where they are, unmoved, no data migration. The rest of the canvas is
reached by **native browser scroll** (D-18 — an already-accepted behavior, now the actual navigation
mechanism; explicitly *not* a pan feature, so D-04 stays intact). The **toolbar becomes
`position: fixed`**, with the canvas offset 48px below it, so the `PageX` / `PageY − 48` coordinate
mapping (D-43) holds unchanged at any scroll position. Deliberately small and self-contained: two
`CanvasBounds` constants, `Home.razor.css` layout, and one test-suite update — a single tightly-coupled
phase.

- [x] **Phase 11: Canvas Enlargement to 5760×2634** (completed 2026-07-27)
      Grows `CanvasBounds` and the SVG surface to 5760×2634, fixes the toolbar in place with a 48px
      canvas offset, and updates the suite's hard-coded 1472×828 bounds assertion and stale edge
      comments — the full milestone in one coherent change.

## Phase Details

### Phase 11: Canvas Enlargement to 5760×2634

**Goal**: The canvas grows from 1472×828 to 5760×2634 (a 3×3 grid of 1920×878 cells) as a pure,
grow-only size change — existing figures stay valid and visible, unmoved, with no data migration —
and the rest of the enlarged surface is reachable by ordinary browser scrolling while the toolbar
stays pinned in place and every coordinate stays pixel-accurate.
**Depends on**: Nothing (first phase of v1.12)
**Requirements**: CANV-04, CANV-05, TEST-03
**Success Criteria** (what must be TRUE):

  1. On page load, the user sees the top-left region of the canvas (anchored at document origin under
     the toolbar) with any existing figures visible exactly where they were before the resize —
     unmoved, no migration needed.

  2. The user can scroll (native browser scroll, no dedicated pan control) to reach a figure placed
     near the far edge of the enlarged 5760×2634 surface, and the toolbar remains visible, fixed at
     the top of the viewport, throughout the scroll.

  3. A figure drawn or dragged while scrolled to any position lands exactly under the pointer — the
     `PageX` / `PageY − 48` coordinate mapping holds with no drift, and no JavaScript interop is
     introduced.

  4. All four shapes (line, rectangle, circle, triangle) still draw, drag, and delete correctly
     anywhere on the larger surface, each change persisting immediately, per-operation (regression
     guard, D-09).

  5. `dotnet build BlazorCanvas.sln` is clean (0 warnings, 0 errors) and the full `dotnet test` suite
     passes, including the bounds assertion renamed and re-pinned to 5760×2634 and stale
     "past the canvas edge (1472,828)" comments corrected.
**Plans**: 2/2 plans executed

  - [x] 11-01-PLAN.md — Grow CanvasBounds to 5760×2634, fix the toolbar (position: fixed) with a 48px
    canvas offset (PageY−48 preserved, no JS interop), and re-pin the test suite to the enlarged bounds
    (CANV-04, CANV-05, TEST-03) — wave 1, autonomous

  - [x] 11-02-PLAN.md — Human-verify the five success criteria in a browser: first-screen top-left,
    native scroll with the toolbar staying fixed, figure-under-pointer accuracy while scrolled, all four
    shapes draw/drag/delete and persist (wave 2, blocking checkpoint)
**UI hint**: yes

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
| 9. Schema, Entity & Data-Preserving Migration | v1.11 | 6/6 | Complete | 2026-07-23 |
| 10. Geometry, Draw, Drag & Sync Rework (No Edge Clamp) + Regression | v1.11 | 6/6 | Complete | 2026-07-24 |
| 11. Canvas Enlargement to 5760×2634 | v1.12 | 2/2 | Complete    | 2026-07-27 |

**v1.0: 5/5 phases, 23/23 plans, 15/15 requirements — milestone audit passed.**
**v1.1: 3/3 phases, 4/4 plans, 4/4 requirements — all phases verified `passed`.**
**v1.11: 2/2 phases, 12/12 plans, 9/9 requirements — both phases verified `passed`; milestone audit passed.**
**v1.12: 0/1 phases complete, 0/3 requirements validated — roadmap written, ready to plan.**

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
| 11 | v1.12 | CANV-04, CANV-05, TEST-03 |

**v1.0: 15/15 requirements mapped. v1.1: 4/4 requirements mapped. v1.11: 9/9 requirements mapped.
v1.12: 3/3 requirements mapped. No orphans. No duplicates.**

## What's Next

**v1.12 is open and Phase 11 is roadmapped** (single phase, 3/3 requirements mapped, 0 unmapped).
Next step: `/gsd-plan-phase 11` — grow `CanvasBounds` to 5760×2634, fix the toolbar's CSS position,
offset the canvas 48px below it, and update the suite's hard-coded bounds. No research phase needed
(design is fully specified by CANV-04/CANV-05/TEST-03 and D-19/D-43/D-18).

**v1.2 is scoped, sequenced after v1.12:** new figure types (ellipse, 5-point star, hexagon,
pentagon, right-angle triangle L/R, four arrows) + a dynamic split-button toolbar. Full plan:
`.planning/backlog/v1.2-figures-and-toolbar.md`. **Its 4-int-bbox premise no longer holds** — after
v1.11, new shapes store as `geometry jsonb`; the backlog must be revised before v1.2 opens.

Known v1.0 tech debt (~11 low-severity items from `01-REVIEW.md`) is recorded in
[`milestones/v1.0-MILESTONE-AUDIT.md`](milestones/v1.0-MILESTONE-AUDIT.md). None blocks a
requirement. Neither v1.1, v1.11, nor v1.12 (so far) added new tech debt.

---
*Roadmap created: 2026-07-14 from `docs/DECISIONS.md` (58 locked decisions) via `.planning/intel/`*
*v1.0 archived: 2026-07-17*
*v1.1 archived: 2026-07-21 — phases 6–8 collapsed above; full detail in `milestones/v1.1-ROADMAP.md`.*
*v1.11 archived: 2026-07-24 — phases 9–10 collapsed above; full detail in `milestones/v1.11-ROADMAP.md`.*
*v1.12 roadmap created: 2026-07-27 — Phase 11 derived from the 3 v1.12 requirements (CANV-04, CANV-05,
TEST-03); a single phase, matching the milestone's small, tightly-coupled footprint (two
`CanvasBounds` constants + `Home.razor.css` layout + one test-suite update) — no genuine dependency
forced a split. 3/3 requirements mapped, 0 unmapped. No research phase (design fully specified by
D-19/D-43/D-18 amendments; research intentionally skipped).*
