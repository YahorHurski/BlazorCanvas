# Roadmap: BlazorCanvas

## Milestones

- ✅ **v1.0 MinVP** — Phases 1–5 (shipped 2026-07-17)
- 🚧 **v1.1 Canvas resize · selection UX · no-JS removal** — Phases 6–8 (in progress)

Full detail: [`milestones/v1.0-ROADMAP.md`](milestones/v1.0-ROADMAP.md) ·
Requirements: [`milestones/v1.0-REQUIREMENTS.md`](milestones/v1.0-REQUIREMENTS.md) ·
Audit: [`milestones/v1.0-MILESTONE-AUDIT.md`](milestones/v1.0-MILESTONE-AUDIT.md)

v1.1 requirements: [`REQUIREMENTS.md`](REQUIREMENTS.md)

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

### 🚧 v1.1 Canvas resize · selection UX · no-JS removal (Phases 6–8, in progress)

**Milestone goal:** Enlarge the canvas to 1472 × 828 (no migration, no shrink path); fix and
restyle figure selection (armed-tool persistence after a draw, exactly one figure selected at a
time, a topmost blue+white dashed trace replacing the red outline); and formally remove the
project's "no hand-authored JavaScript" constraint (doc-only — no runtime change, no new JS
written this milestone). All four changes are user-approved and already amended in
`docs/DECISIONS.md`. Phase numbering continues from v1.0's Phase 5 (directories will be
`BC-06-…`, `BC-07-…`, `BC-08-…`, matching the established `BC-01-…`…`BC-05-…` pattern).

- [x] **Phase 6: Canvas Resize to 1472×828** - Enlarge the canvas surface with no migration and no shrink path; existing figures keep their exact position.
- [x] **Phase 7: Selection Lifecycle & Restyle** - Armed-tool persistence after drawing, one-selection-at-a-time deselect rules, and a topmost blue+white dashed trace replacing the red outline. (completed 2026-07-21)
- [ ] **Phase 8: Architecture Constraint Cleanup** - Remove the "no hand-authored JavaScript" constraint from every project doc/comment; correct D-06/D-18/D-33/D-37/D-57 motivations to MVP simplicity.

## Phase Details

### Phase 6: Canvas Resize to 1472×828

**Goal**: The canvas surface is enlarged to 1472 × 828 (16:9) with no shrink path and no database
migration — every figure stored under the old 1280 × 720 size keeps its exact position on the
larger surface.
**Depends on**: Nothing (first phase of v1.1)
**Requirements**: CANV-03
**Success Criteria** (what must be TRUE):

  1. The SVG canvas at `/` measures **1472 × 828**, still anchored at document position (0, 48)
     below the 48px toolbar with no CSS border — a maximized browser window on a 1920 × 1080
     monitor shows the entire canvas with **no scrollbar**.

  2. Figures that existed before the resize render at the **exact same** `(x1, y1, x2, y2)` they
     had under the old 1280 × 720 size — **no migration script runs**, and no figure shifts,
     resizes, or gets clipped.

  3. Drawing and dragging both clamp against the new inclusive bounds `0..1472 × 0..828` — a
     figure can be drawn or dragged flush to the new right/bottom edge, past where the old
     1280 × 720 boundary used to stop it.

  4. `CanvasBounds.cs` exposes only the new 1472 × 828 constant (no configurable shrink path), and
     the geometry/clamp test suite — updated for the new bounds — passes with zero regressions.
**Plans**: 1/1 plans executed

- [x] 06-01-PLAN.md — Enlarge CanvasBounds to 1472×828 + bind the Home.razor SVG + classify-and-re-pin the geometry edge tests

**UI hint**: yes

---

### Phase 7: Selection Lifecycle & Restyle

**Goal**: Selection behaves predictably — the armed tool stays armed after a draw, the just-drawn
figure is selected, and at most one figure is ever selected, with clear deselect rules — and is
visibly a calm blue + white trace on the figure's own outline rather than a red "danger" outline.
**Depends on**: Phase 6 (shares `Home.razor`'s SVG rendering; sequenced after Phase 6 to avoid
overlapping edits to the same file)
**Requirements**: SEL-01, SEL-02
**Success Criteria** (what must be TRUE):

  1. Drawing any figure leaves its tool **armed** (the toolbar button still shows active) and
     **automatically selects** the figure just drawn.

  2. **At most one figure is ever selected** — selecting a different figure or drawing a new one
     always clears any prior selection first.

  3. The current selection **clears** when the user presses the canvas outside the selected
     figure, arms a different tool, or presses any toolbar button **except Delete**; Delete still
     acts on the current selection.

  4. The selected figure shows a **~1px blue + white dashed trace** following its own outline
     (not a bounding box), rendered as the **topmost layer** with `pointer-events: none` —
     remaining visible even when the selected figure was drawn earlier and now sits behind a
     later, larger figure.

  5. The old solid **red 2px** selection outline no longer appears anywhere in the app.

**Plans**: 2/2 plans executed
**Wave 1**

- [x] 07-01-PLAN.md — SEL-01 lifecycle (auto-select on draw, tool stays armed, one-at-a-time deselect rules) + SEL-02 restyle (remove the red outline; new topmost blue+white dashed `SelectionTrace` on the figure's own outline)

**Wave 2** *(blocked on Wave 1 completion)*

- [x] 07-02-PLAN.md — Human-verify the five Phase 7 criteria + the two-tab remote-delete concurrency edge

**UI hint**: yes

---

### Phase 8: Architecture Constraint Cleanup

**Goal**: Every project doc and source comment consistently reflects that the "no hand-authored
JavaScript" rule is gone and that D-06/D-18/D-33/D-37/D-57 read as MVP-simplicity decisions —
verified true with zero runtime or JS change introduced this milestone.
**Depends on**: Phase 7 (documentation close-out; confirms no stale "no-JS" references remain
once the v1.1 code changes have landed)
**Requirements**: ARCH-01
**Success Criteria** (what must be TRUE):

  1. `docs/DECISIONS.md` and `.planning/PROJECT.md` state the "no hand-authored JavaScript"
     constraint as **removed**, with D-06, D-18, D-33, D-37, D-57 each carrying a corrected
     **MVP-simplicity** motivation — no "because no JS" phrasing remains as an active rule on any
     of the five.

  2. A repository-wide search for "no hand-authored JavaScript" / "no JS" / "hand-authored" turns
     up zero hits outside historical/superseded notes explicitly marked as such — no other doc,
     README, or source comment contradicts the removal.

  3. `dotnet build BlazorCanvas.sln` and `dotnet test BlazorCanvas.sln` both pass unchanged from
     before this phase — proving the removal was **permissive-only**, with no new JavaScript and
     no runtime behavior change shipped this milestone.
**Plans**: TBD

## Progress

**Execution Order:**
Phases execute in numeric order: 1 → 2 → 3 → 4 → 5 → 6 → 7 → 8

| Phase | Milestone | Plans Complete | Status | Completed |
|-------|-----------|----------------|--------|-----------|
| 1. Database, Schema & Geometry Core | v1.0 | 6/6 | Complete | 2026-07-15 |
| 2. Login, Session & Logout | v1.0 | 3/3 | Complete | 2026-07-15 |
| 3. The Canvas & Drawing | v1.0 | 5/5 | Complete | 2026-07-16 |
| 4. Select, Drag & Delete | v1.0 | 4/4 | Complete | 2026-07-16 |
| 5. Live Cross-Tab Sync | v1.0 | 5/5 | Complete | 2026-07-17 |
| 6. Canvas Resize to 1472×828 | v1.1 | 1/1 | Complete    | 2026-07-21 |
| 7. Selection Lifecycle & Restyle | v1.1 | 2/2 | Complete    | 2026-07-21 |
| 8. Architecture Constraint Cleanup | v1.1 | 0/TBD | Not started | - |

**v1.0: 5/5 phases, 23/23 plans, 15/15 requirements — milestone audit passed.**
**v1.1: 0/3 phases complete — roadmap created 2026-07-20; requirements CANV-03, SEL-01, SEL-02,
ARCH-01 all mapped.**

## Requirement Coverage

| Phase | Requirements |
|-------|--------------|
| 1 | DATA-02, TEST-01 |
| 2 | AUTH-01, AUTH-02, AUTH-03 |
| 3 | DATA-01, CANV-01, CANV-02, FIG-01 |
| 4 | FIG-02, FIG-03, FIG-04 |
| 5 | SYNC-01, DATA-03, DATA-04 |
| 6 | CANV-03 |
| 7 | SEL-01, SEL-02 |
| 8 | ARCH-01 |

**v1.0: 15/15 requirements mapped. v1.1: 4/4 requirements mapped. No orphans. No duplicates.**

## What's Next

**v1.1 is active** — 3 phases (6, 7, 8) cover the four approved changes; none has started yet.
Suggested next step: `/gsd-plan-phase 6`. Per STATE.md's operator notes, phases 7 and 8 are small
enough that `/gsd-quick` may suit them once phase 6 is underway.

**v1.2 is scoped, not started:** new figure types (ellipse, 5-point star, hexagon, pentagon,
right-angle triangle L/R, four arrows) + a dynamic split-button toolbar. Full plan:
`.planning/backlog/v1.2-figures-and-toolbar.md`. Its decision amendments happen when v1.2 is
kicked off — do not begin until v1.1 ships.

Known v1.0 tech debt (~11 low-severity items from `01-REVIEW.md`) is recorded in
[`milestones/v1.0-MILESTONE-AUDIT.md`](milestones/v1.0-MILESTONE-AUDIT.md). None blocks a
requirement.

---
*Roadmap created: 2026-07-14 from `docs/DECISIONS.md` (58 locked decisions) via `.planning/intel/`*
*v1.0 archived: 2026-07-17*
*v1.1 roadmap added: 2026-07-20 — phases 6–8 continue numbering from BC-05 (see MILESTONES.md).*
