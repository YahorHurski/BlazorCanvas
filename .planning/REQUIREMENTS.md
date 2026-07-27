# Requirements: BlazorCanvas — Milestone v1.12

**Defined:** 2026-07-27
**Core Value:** The canvas is always the truth, everywhere at once — what you draw persists instantly,
and every other tab shows it happening live, including a figure gliding in real time as you drag it.

Milestone v1.12 is a **single change: enlarge the fixed canvas so it feels borderless** — from
1472×828 to **5760×2634** (a 3×3 grid of 1920×878 cells). It **adds no new interaction** (the three
verbs are unchanged) and **no scaling** — coordinates stay 1:1, circles stay round. Zoom/pan (a viewBox
"camera") is **explicitly deferred** to a later milestone as a pure additive upgrade.

This milestone **amends `docs/DECISIONS.md`** (to be logged at ship): **D-19** (size 1472×828 →
5760×2634 — the anticipated grow-only change), **D-43** (toolbar becomes `position: fixed`; the
`PageY − 48` coordinate invariant is preserved via a 48px canvas offset), **D-18** (native page scroll
becomes the actual navigation mechanism — previously an "accepted cost", now used). **D-04 is
untouched** — native browser scroll is not a pan feature.

REQ-IDs continue from the existing set. `CANV-04` continues the `CANV` category (last was `CANV-03`,
the v1.1 resize to 1472×828). `TEST-03` continues `TEST`. `FIG-05`/`TOOL-01` remain reserved for v1.2.

## v1.12 Requirements

Requirements for this milestone. Each maps to exactly one roadmap phase (Traceability below).

### Canvas (CANV)

- [x] **CANV-04**: The fixed canvas is enlarged to **5760×2634** (a 3×3 grid of 1920×878 cells),
  driven by the `CanvasBounds` constants and the SVG surface size. This is **pure growth**
  (D-19: the surface may grow, never shrink), so **existing figures remain valid with no data
  migration** and keep their exact positions. On page load the viewport shows the **top-left cell**
  (canvas anchored at document origin under the toolbar), so a returning user's existing figures are
  **visible immediately and unmoved**. There is **no scaling** — one canvas unit stays one CSS pixel,
  so circles stay round (the total 5760×2634 shape is intentionally not 16:9). *(D-19 amended; D-18.)*

- [x] **CANV-05**: The rest of the enlarged canvas is reachable by **native browser scroll** (no pan
  feature, no zoom — D-04 intact). The **toolbar stays visible at all times** while scrolling
  (`position: fixed`), and the canvas carries a 48px top offset so it is not hidden beneath it.
  Drawing and dragging remain **pixel-accurate at any scroll position**: the `PageX` / `PageY − 48`
  coordinate mapping is unchanged and **no JavaScript interop is introduced** (`PageX`/`PageY` are
  document-relative and already include scroll). A figure drawn while scrolled lands exactly under the
  pointer. *(D-43 amended — toolbar fixed, −48 invariant preserved; D-18; D-04.)*

### Tests (TEST)

- [x] **TEST-03**: The suite is updated to the enlarged surface — the bounds assertion that hard-codes
  1472×828 (`NormalisationTests.Bounds_AreTheFixed1472x828Canvas`) is updated and renamed to
  5760×2634, and stale "past the canvas edge (1472,828)" comments in the coordinate/store/gesture/codec
  tests are corrected. The **build stays clean** and the **full suite passes**; the four shapes still
  draw, drag, and delete with per-operation persistence on the larger surface (regression guard, D-04 /
  D-09). *(D-49.)*

## v2 Requirements (deferred)

Tracked but **not** in this milestone's roadmap.

### Zoom / pan — the viewBox "camera" (parked)

- **CANV-F1**: The user can **zoom** in/out and **pan** across the enlarged canvas, implemented as an
  SVG `viewBox` "camera" over the fixed logical space (pan = translate the viewBox; zoom = scale it,
  `preserveAspectRatio="xMidYMid meet"` so circles stay round). This is a **pure additive upgrade** —
  the stored coordinates are already in the fixed logical space it reads, so nothing built in v1.12 is
  thrown away. It **will** change the coordinate mapping (`FromPage` must invert the live viewBox,
  needing a small JS-interop read of scale/offset) — that cost is inherent to zoom and travels with it.
  *(Deferred by user decision, 2026-07-27; keeps D-04's "zoom/pan added by name only" discipline —
  logged when the milestone opens.)*

### Figures / toolbar — v1.2 (parked)

- **FIG-05**: New figure types — ellipse, 5-point star, hexagon, pentagon, right-angle triangle
  (left/right variants), four arrows. *(v1.2; the backlog `.planning/backlog/v1.2-figures-and-toolbar.md`
  must be revised first — its 4-int-bbox premise no longer holds after v1.11.)*

- **TOOL-01**: A dynamic split-button toolbar to accommodate the expanded figure set. *(v1.2.)*

### Storage follow-ons (deferred)

- **STOR-F1**: Off-canvas figure recovery. v1.12 **improves** this — native scroll now reaches any
  figure inside the 5760×2634 surface — but figures dragged past even the enlarged edge (still possible;
  no clamp since D-59) remain an accepted, unrecoverable state until the zoom/pan camera (CANV-F1) lands.

- **STOR-F2**: Multi-canvas per user (a `canvases` table + `figures.canvas_id` + backfill). A cheap
  additive migration when wanted; not now (D-12/D-03 upheld).

## Out of Scope

Explicitly excluded from v1.12. Documented to prevent scope creep.

| Feature | Reason |
|---------|--------|
| Zoom, pan, a viewBox "camera", any coordinate rescale | Deferred to a later milestone (CANV-F1). v1.12 is a size change only — no scaling, `FromPage` untouched. |
| Any JavaScript interop | Not needed — `PageX`/`PageY` already include scroll, so the −48 mapping holds unchanged. Interop arrives only with zoom (CANV-F1). |
| Shrinking the canvas / any non-grow resize | Still forbidden (D-19) — the surface may grow, never shrink. |
| Off-canvas figure recovery UI (bring-back / undo) | Accepted risk (STOR-F1); designed with the zoom/pan camera later. |
| A minimap / scroll-position indicator / "recenter" control | Not requested; native browser scrollbars are the only navigation this milestone. |
| New figure types / toolbar change | v1.2 (FIG-05 / TOOL-01), unchanged here. |
| Everything locked out by D-04/D-14/D-08 | resize / rotate / undo-redo / z-order UI / multi-select / colours / real auth — unchanged. |

## Traceability

Which phases cover which requirements. Populated during roadmap creation.

| Requirement | Phase | Status |
|-------------|-------|--------|
| CANV-04 | Phase 11 | Complete |
| CANV-05 | Phase 11 | Complete |
| TEST-03 | Phase 11 | Complete |

**Coverage:**

- v1.12 requirements: 3 total
- Mapped to phases: 3 (Phase 11: CANV-04, CANV-05, TEST-03)
- Unmapped: 0 ✓

---
*Requirements defined: 2026-07-27*
*Last updated: 2026-07-27 after roadmap creation (`/gsd-new-milestone` → roadmapper) — Phase 11
assigned (single phase; the milestone's footprint is too small and tightly-coupled to split), 3/3
requirements mapped, 0 unmapped.*
