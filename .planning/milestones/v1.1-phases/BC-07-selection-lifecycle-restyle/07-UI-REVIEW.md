# UI Review — BC-07: Selection Lifecycle & Restyle

**Audit mode:** Code-only. The local application was not available through an accessible browser session at `http://localhost:5054`, so no independent screenshots were captured. Screenshot storage was therefore not created or changed. The phase's recorded manual verification (`07-02-SUMMARY.md`) reports that all six required runtime checks were approved.

**Baseline:** Abstract six-pillar standards; this phase has no UI-SPEC.

## Overall score: 20/24 — Strong, with follow-up validation recommended

| Pillar | Score | Evidence-based assessment |
| --- | ---: | --- |
| Copywriting | 3/4 | The selection state avoids unnecessary copy, appropriate for a direct-manipulation canvas. Existing controls retain descriptive `aria-label`s (for example, “Delete selected figure”); however, no visible or announced confirmation tells a non-visual user which figure became selected. |
| Visuals | 4/4 | `SelectionTrace.razor` renders a geometry-matched two-stroke overlay for every supported figure type. `Home.razor` emits it after all figures and previews, making it the final SVG child; `pointer-events="none"` preserves interaction with content below. |
| Color | 4/4 | The calm blue `#1D4ED8` dashed stroke over a white under-stroke is distinct from the permanently black figure outline and the red Delete hover. The same blue also anchors armed and focus states in the toolbar, giving the interaction state a coherent visual language. |
| Typography | 3/4 | The phase does not add text-heavy UI, and its icon controls have accessible labels. Typography was not independently rendered in this code-only audit; any visual type hierarchy, text scaling, or modal wrapping needs live verification. |
| Spacing | 3/4 | The overlay follows the selected figure rather than adding a bounding box, handles, or layout displacement. Existing toolbar controls retain 40 × 40 px targets with 4 px separation. Dense overlaps and dash cadence at different zoom/device-pixel ratios could not be inspected live. |
| Experience design | 3/4 | The lifecycle is coherent: a completed draw selects itself while leaving the tool armed; empty-canvas and non-Delete toolbar presses clear selection; Delete retains propagation control; remote deletes clear a matching local selection. The state still depends solely on a fine dashed visual cue and pointer interaction, so discoverability and non-pointer feedback remain limited. |

## Evidence reviewed

- `Home.razor` owns a single nullable `selectedId`, selects completed draws, clears on empty-canvas press, and clears when a remote delete removes the selected id.
- Its final `<SelectionTrace>` block resolves the selection from the live figure list and substitutes `dragCurrentBox` while dragging, so the trace follows the selected geometry and vanishes when its figure no longer exists.
- `SelectionTrace.razor` uses `CircleEncoding.ToCentreRadius` and invariant triangle points, matching `FigureShape.razor` geometry for line, rectangle, circle, and triangle.
- `FigureShape.razor` uses an unconditional black stroke, preventing the superseded red selected-outline treatment.
- `Toolbar.razor` routes ordinary toolbar clicks to `OnDeselect`, while Delete stops propagation and retains its action; its styles reserve red only for enabled Delete hover/focus.
- `07-02-SUMMARY.md` records approved live checks for auto-selection, single selection, all deselection routes, topmost geometry-matched trace, removal of red selected outlines, and cross-tab deletion.

## Findings and top fixes

1. **Add an accessible selected-state announcement or semantic representation.** The SVG trace is intentionally inert and the selected figure itself exposes no explicit selected state. Provide an `aria-live` status (for example, “Rectangle selected”) or an equivalent accessible canvas summary so selection does not exist only as a visual dashed line. **Pillar:** Experience design / Copywriting. **Priority:** Medium.
2. **Run a targeted visual regression capture once a browser session is available.** Inspect all four figure types, dense figure overlap, a selected shape over white fill, and a live drag at normal and high-DPI scaling. Verify that the 2 px white under-stroke cleanly separates the 1 px `#1D4ED8` dash without appearing to erase adjacent content. **Pillar:** Visuals / Spacing. **Priority:** Medium.
3. **Validate keyboard-only and zoomed usage.** Toolbar focus treatment is present, but selection and dragging are pointer-event driven. Verify a keyboard user can understand canvas state and that focus order, 200% zoom, and icon labels remain usable. **Pillar:** Experience design / Typography. **Priority:** Medium.

## Screenshot status

No screenshots captured. The browser-control runtime reported no browser available for the local app; therefore no screenshot directory or `.gitignore` change was needed.

## Verdict

The implementation meets the phase's visual intent in code and has recorded human approval for the required runtime paths. The principal residual risk is accessibility and independent visual confirmation under real rendering conditions, not an identified mismatch with the selection lifecycle or trace design.
