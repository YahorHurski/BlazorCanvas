# Requirements: BlazorCanvas — Milestone v1.1

**Defined:** 2026-07-20
**Core Value:** The canvas is always the truth, everywhere at once — what you draw persists instantly, and every other tab shows it happening live, including a figure gliding in real time as you drag it.

Milestone v1.1 makes four user-approved changes, all already recorded in `docs/DECISIONS.md`. No new
figure types, no schema change, and **no database migration**. REQ-IDs continue from the v1.0 set
(the full v1.0 requirement list is archived at `.planning/milestones/v1.0-REQUIREMENTS.md`).

## v1.1 Requirements

Requirements for this milestone. Each maps to exactly one roadmap phase (Traceability below).

### Canvas

- [ ] **CANV-03**: The canvas is **1472 × 828** (16:9), enlarged from the v1.0 1280 × 720. Existing
  stored figures keep their absolute position (no migration); the surface **may grow but must never
  shrink**. A maximized window on a 1920 × 1080 monitor shows the whole canvas with no scroll.
  *Amends D-19/D-36/D-58/D-18. Touches `CanvasBounds.cs`, the `Home.razor` SVG, and geometry/clamp tests.*

### Selection

- [ ] **SEL-01**: After a user draws a figure, the **armed tool stays armed** and the **just-drawn
  figure is selected**; **at most one figure is selected at any time**; the selection **clears** when
  the user presses the canvas outside the selected figure, arms any tool, or presses any toolbar
  button **except Delete**. *Extends D-31/D-30.*

- [ ] **SEL-02**: The selected figure is indicated by a **~1px blue + white dashed trace on the
  figure's own outline**, drawn as the **topmost layer** (`pointer-events: none`) so it is visible
  even when the selected figure sits behind larger figures. Replaces the v1.0 red outline.
  *Amends D-31/D-58.*

### Architecture

- [ ] **ARCH-01**: The **"no hand-authored JavaScript" constraint is removed** from the project, and
  the motivations of D-06/D-18/D-33/D-37/D-57 are corrected to **MVP simplicity**. Doc/constraint
  change only — **no runtime behavior change and no new JS is written this milestone**; it re-opens
  future options (Delete-key shortcut, `setPointerCapture` drag, Escape-to-cancel), each its own
  future decision. *Permissive amendment.*

## v2 Requirements (deferred — v1.2)

Scoped and parked in `.planning/backlog/v1.2-figures-and-toolbar.md`. Tracked but **not** in this
milestone's roadmap. Their decision amendments happen when v1.2 is kicked off.

### Figures

- **FIG-05**: New figure types — ellipse, 5-point star, hexagon, pentagon, right-angle triangle
  (left/right variants), four arrows.

### Toolbar

- **TOOL-01**: A dynamic split-button toolbar to accommodate the expanded figure set.

## Out of Scope

Explicitly excluded from v1.1. Documented to prevent scope creep.

| Feature | Reason |
|---------|--------|
| New figure types / dynamic toolbar | Deferred to v1.2 (backlog); v1.1 changes no figure set. |
| Shrinking the canvas | Forbidden by D-19/D-36 — would orphan stored figures off-surface. |
| Any database migration | v1.1 needs none; enlarging the canvas keeps every stored figure valid. |
| Writing new JavaScript / interop | ARCH-01 only *permits* it; v1.1 writes none. Any actual JS is a future decision. |
| Delete-key, `setPointerCapture` drag, Escape-to-cancel | Re-opened by ARCH-01 but each is a separate future decision, not v1.1 scope. |
| Everything locked out by D-04/D-14/D-08 | resize/rotate/undo/z-order/multi-select/colours/real auth — unchanged from v1.0. |

## Traceability

Which phases cover which requirements. Populated during roadmap creation.

| Requirement | Phase | Status |
|-------------|-------|--------|
| CANV-03 | Phase 6 | Pending |
| SEL-01 | Phase 7 | Pending |
| SEL-02 | Phase 7 | Pending |
| ARCH-01 | Phase 8 | Pending |

**Coverage:**
- v1.1 requirements: 4 total
- Mapped to phases: 4 (Phase 6: CANV-03 · Phase 7: SEL-01, SEL-02 · Phase 8: ARCH-01)
- Unmapped: 0 ✓

---
*Requirements defined: 2026-07-20*
*Last updated: 2026-07-20 after roadmap creation (`/gsd-new-milestone` → roadmapper) — phases 6–8
assigned, 4/4 requirements mapped, 0 unmapped.*
