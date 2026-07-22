# Roadmap: BlazorCanvas

## Milestones

- ✅ **v1.0 MinVP** — Phases 1–5 (shipped 2026-07-17)
- ✅ **v1.1 Canvas resize · selection UX · no-JS removal** — Phases 6–8 (shipped 2026-07-21)
- ✅ **v1.11 Storage Model Rewrite** — Phases 9–12 (shipped 2026-07-22)
- 📋 **v1.2 Figures & dynamic toolbar** — scoped, next up

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

### 📋 v1.2 Figures & dynamic toolbar (Planned)

Scoped, not started. New figure types (ellipse, 5-point star, hexagon, pentagon, right-angle
triangle L/R, four arrows) plus a dynamic split-button toolbar. Full plan:
[`backlog/v1.2-figures-and-toolbar.md`](backlog/v1.2-figures-and-toolbar.md). Materially cheaper now
that v1.11 has landed — the 4-integer workarounds are gone, and a new type costs one C# class plus
one `figure_types` row. Its decision amendments happen when v1.2 is kicked off.

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
| 12. Regression Verification | v1.11 | 2/2 | Complete    | 2026-07-22 |

**v1.0: 5/5 phases, 23/23 plans, 15/15 requirements — milestone audit passed.**
**v1.1: 3/3 phases, 4/4 plans, 4/4 requirements — all phases verified `passed`.**
**v1.11: 4/4 phases, 19/19 plans, 21/22 requirements satisfied — shipped 2026-07-22 as
`override_closeout`; MIGR-03 accepted as a documented gap.**

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

**v1.11 is shipped and archived.** Suggested next step: `/gsd-new-milestone` to open v1.2
(questioning → research → requirements → roadmap).

**One item carries forward from v1.11:** MIGR-03 is an accepted gap, not a completed requirement.
Closing it later means restoring `V11MigrationReplayTests.cs` against the committed
`v1.1-pre-rewrite.sql` fixture — see the audit's "Outstanding Work" section.

**Tech debt carried forward:** `ShapeRegistry` read-only views expose their backing lists
(09-REVIEW WR-03); `Home.razor.js` duplicates shape preview geometry outside the registry with no
drift guard — worth closing before v1.2 adds figure types. Known v1.0 tech debt (~11 low-severity
items) remains recorded in
[`milestones/v1.0-MILESTONE-AUDIT.md`](milestones/v1.0-MILESTONE-AUDIT.md). None blocks a requirement.

---
*Roadmap created: 2026-07-14 from `docs/DECISIONS.md` (58 locked decisions) via `.planning/intel/`*
*v1.0 archived: 2026-07-17*
*v1.1 archived: 2026-07-21 — phases 6–8 collapsed above; full detail in `milestones/v1.1-ROADMAP.md`.*
*v1.11 archived: 2026-07-22 — phases 9–12 collapsed above; full detail in
`milestones/v1.11-ROADMAP.md`, requirements in `milestones/v1.11-REQUIREMENTS.md`, audit in
`milestones/v1.11-MILESTONE-AUDIT.md`. Closed as `override_closeout` with MIGR-03 accepted as a
documented gap.*
