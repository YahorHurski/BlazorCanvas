# Requirements: BlazorCanvas — Milestone v1.12 Five-pointed star

**Defined:** 2026-07-22
**Core Value:** The canvas is always the truth, everywhere at once — what you draw persists instantly,
and every other tab shows it happening live, including a figure gliding in real time as you drag it.

**Milestone goal:** Add `star5` as the fifth figure type end-to-end — drawn, previewed, persisted,
synced, selected, dragged and deleted exactly like the four that came before it.

**Definition of done:**

> "I can arm a star tool in the toolbar, drag a box on the canvas, and watch a five-pointed star
> preview follow my cursor and commit — persisting across a refresh, appearing live in a second tab,
> and dragging, selecting, and deleting exactly like the four shapes that came before it."

**Numbering note.** IDs continue the project-wide sequence; they do not restart per milestone.
Previous highs: `SHAPE-03`, `VALID-03`, `FIG-04`, `CANV-03`, `DATA-04`, `SYNC-03`, `MODEL-07`,
`RENDER-01`, `TEST-03`, `ARCH-01`, `REG-01`.

## v1.12 Requirements

### Star geometry

- [ ] **SHAPE-04**: A star drawn into any dragged box renders point-up with five outer points and
      five inner vertices at **0.382** of the outer radius, stretching to fill the box — a wide box
      gives a wide star, and the first vertex sits at top-centre.
- [ ] **SHAPE-05**: A star's geometry persists as an ordered ten-point list plus its `innerRatio`;
      rendering and the `bbox_*` cache derive from the **points alone**, so a stored ratio that
      disagrees with the points changes nothing on screen.
- [ ] **SHAPE-06**: A star round-trips through `TryParseGeometry` / `ToJson` byte-identically, and a
      geometry payload missing `innerRatio` fails to parse rather than rendering a partial figure.

### Drawing

- [ ] **FIG-05**: User can arm a star tool and draw a star by dragging corner-to-corner, exactly as
      they draw a rectangle or triangle.
- [ ] **FIG-06**: A live star preview follows the cursor during the drag, showing the same shape the
      committed figure will have — not a triangle, and not a shape derived from a second formula that
      can drift from the first.
- [ ] **FIG-07**: A star drawn toward a canvas edge stops at the edge under the same clamp as every
      other shape, and a drag with zero width or zero height is silently rejected while a thin sliver
      with positive width and height is accepted.
- [ ] **FIG-08**: User can select, drag with edge clamping, and delete a star exactly as they can the
      four existing shapes, including the blue-and-white dashed selection trace on the star's own
      outline.

### Toolbar

- [ ] **CANV-04**: The toolbar presents seven buttons — `[pointer] [line] [rectangle] [circle]
      [triangle] [star] [delete]` — with the star button between `triangle` and `delete`, arming and
      un-arming like every other tool, and the 48px strip and right-aligned logout unchanged.

### Rendering

- [ ] **RENDER-02**: A persisted star renders from its local geometry under the v1.11
      `translate(x,y) rotate(…)` transform, so it survives a page reload with the identical picture
      rather than silently disappearing from the renderer's type switch.

### Persistence and sync

- [ ] **DATA-05**: A drawn star persists immediately with no Save button and reappears unchanged
      after a refresh.
- [ ] **SYNC-04**: A star appears live in the user's other open tabs on draw, glides in real time
      during a drag, and disappears on delete — under the unchanged D-53 contract.
- [ ] **MODEL-08**: The `figure_types` catalog is seeded from the shape registry at every startup, so
      a newly registered shape is writable on an existing database with no manual SQL and no
      migration — closing the gap where `V11Cutover` returns early at `CatalogState.Completed` and
      leaves the type's foreign key unsatisfiable.

### Decisions

- [ ] **ARCH-02**: `docs/DECISIONS.md`, `PROJECT.md` and `.planning/intel/` record the seven-button
      toolbar as an amendment to D-16/D-33/D-58 and to CANV-02's "exactly six buttons" text, and the
      star's own decisions are added by name from **D-70** onward.

### Tests

- [ ] **TEST-04**: Guards exist for this milestone's *silent* failure modes — a drift guard failing
      if the `Home.razor.js` inner-ratio constant diverges from the C# one, `bbox_*` agreement for
      star rows, and rejection of degenerate and malformed star geometry.

### Regression

- [ ] **REG-02**: Human acceptance on the running application confirms the definition of done: arm,
      draw with a live preview, edge-clamp, refresh, select, drag, delete, and watch a star glide in
      a second window.

## Future Requirements

Deferred. Tracked, not in this roadmap.

### More figures and the dynamic toolbar (v1.2)

- **v1.2** — the remaining nine figure types (ellipse, hexagon, pentagon, right-angle triangle L/R,
  four arrows) plus the dynamic split-button flyout toolbar. Scoped in
  `.planning/backlog/v1.2-figures-and-toolbar.md`. v1.12 delivers the 5-point star, so v1.2's shape
  table drops that row.

### Capabilities unlocked but unused

- **Per-star pointiness** — `innerRatio` is stored per figure, so a future editor could vary it. No
  UI exposes it in v1.12.
- **Rotation, vertex editing, z-order control, per-figure style** — possible since v1.11, still
  unused. Out until named in `docs/DECISIONS.md`.

## Out of Scope

| Feature | Reason |
|---------|--------|
| The other nine v1.2 figure types | This milestone is deliberately one figure. Adding the star alone proves the registry claim before nine more depend on it. |
| The dynamic split-button flyout toolbar | v1.2 work. v1.12 adds a seventh flat button, which is what a single new shape needs. |
| A UI for choosing `innerRatio` or point count | The user draws a star; they supply no parameters. The ratio is a fixed constant. |
| Star as a *regular* (aspect-locked) shape | Decided stretchable. Regularity would replicate the circle's clamp machinery per shape and add the D-50 silent-failure surface. |
| Centre-out star gesture | Decided corner-to-corner, matching triangle and rectangle. |
| Fixing `ShapeRegistry.All`/`.Names` live-list exposure (09-REVIEW WR-03) | Carried tech debt, explicitly out. Scope is one figure. |
| Closing MIGR-03 | Accepted gap from v1.11; the migration path is permanently unreachable and forward risk is zero. |
| Removing the unreferenced `V11DataMigration.RunAsync(NpgsqlDataSource, …)` overload | Carried tech debt, explicitly out. |
| Driving the JS preview from the C# registry | The larger fix for preview-geometry duplication. v1.12 adds a drift guard instead, making the duplication loud rather than silent. |
| A rotate/resize handle on a drawn star | D-04: the app has exactly three verbs — draw, drag, delete. |

## Traceability

Which phases cover which requirements. Updated during roadmap creation.

| Requirement | Phase | Status |
|-------------|-------|--------|
| SHAPE-04 | Phase 13 | Pending |
| SHAPE-05 | Phase 13 | Pending |
| SHAPE-06 | Phase 13 | Pending |
| MODEL-08 | Phase 14 | Pending |
| CANV-04 | Phase 14 | Pending |
| ARCH-02 | Phase 14 | Pending |
| FIG-05 | Phase 15 | Pending |
| FIG-06 | Phase 15 | Pending |
| FIG-07 | Phase 15 | Pending |
| RENDER-02 | Phase 15 | Pending |
| DATA-05 | Phase 15 | Pending |
| FIG-08 | Phase 16 | Pending |
| SYNC-04 | Phase 16 | Pending |
| TEST-04 | Phase 16 | Pending |
| REG-02 | Phase 17 | Pending |

**Coverage:**
- v1.12 requirements: 15 total
- Mapped to phases: 15
- Unmapped: 0 ✓
- No orphans. No duplicates.

---
*Requirements defined: 2026-07-22*
*Last updated: 2026-07-22 — roadmap created, all 15 requirements mapped to Phases 13–17 (100% coverage).*
