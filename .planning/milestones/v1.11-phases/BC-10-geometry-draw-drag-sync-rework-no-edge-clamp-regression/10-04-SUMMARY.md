---
phase: 10-geometry-draw-drag-sync-rework-no-edge-clamp-regression
plan: 04
subsystem: rendering
tags: [csharp, blazor, svg, geometry]

# Dependency graph
requires:
  - phase: 10-geometry-draw-drag-sync-rework-no-edge-clamp-regression
    plan: 03
    provides: "SyncMessage/SyncReceiver on anchor+geometry; Home.razor's receive path delegating to SyncReceiver; dragCurrentBox left as the one remaining box-shaped bridge for this plan to remove"
provides:
  - "ShapeRender — the single anchor+geometry to SVG-coordinate helper (Size, Radius, LineDelta, TrianglePoints), shared by FigureShape and SelectionTrace"
  - "FigureShape and SelectionTrace parameters: X, Y, Geometry replace Box; both decode once per parameter change via OnParametersSet, never in a markup expression"
  - "Home.razor's figures loop, preview block and SelectionTrace block render from Type + anchor + the figure's own stored Geometry string; the dragged figure reads the live drag anchor with its untouched Geometry, no re-derivation"
  - "The 10-02 interim bridge (dragCurrentBox, the dragFigure read-only reference, FigureBox) is deleted; the last bounding-box bridge in Home.razor is gone"
affects: [10-05, 10-06-verification-checkpoint]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "GeometryCodec's per-type wire-format records (BoxGeometry/CircleGeometry/LineGeometry) made internal rather than duplicated: ShapeRender deserialises through the codec's own records, so the w/h/r/dx/dy JSON member names exist in exactly one place and a rename there cannot silently diverge from the renderer (T-10-14)."
    - "A Blazor component decodes its geometry exactly once, in OnParametersSet, into private fields the markup reads — never from a markup expression, which would deserialise the JSON on every attribute evaluation."
    - "The dragged figure's render coordinates come from the same figures list the loop is already iterating, not from a cached read-only reference — once the renderer takes anchor+geometry directly, there is nothing left to derive per pointer-move."

key-files:
  created:
    - src/BlazorCanvas/Geometry/ShapeRender.cs
    - tests/BlazorCanvas.Tests/Geometry/ShapeRenderTests.cs
  modified:
    - src/BlazorCanvas/Geometry/GeometryCodec.cs
    - src/BlazorCanvas/Components/Canvas/FigureShape.razor
    - src/BlazorCanvas/Components/Canvas/SelectionTrace.razor
    - src/BlazorCanvas/Components/Pages/Home.razor

key-decisions:
  - "GeometryCodec's three private nested records were widened to internal (not duplicated, not promoted to full members of ShapeRender) so ShapeRender could deserialise through the codec's own JSON contract types rather than re-typing the w/h/r/dx/dy member names — PA-11's boundary (ShapeRender is a new class, not more members on GeometryCodec) stays intact; only accessibility changed."
  - "dragFigure (the PA-8 read-only Figure reference from 10-02) is deleted, not merely unused: once FigureShape takes the anchor and Geometry directly, the figures loop already holds a reference to the dragged figure on every iteration, and a separate cached reference would have become dead code the compiler could not catch without a real read (only ever assigned, then read once — the interim-bridge derivation this plan removes)."

requirements-completed: [STOR-02, STOR-05]

coverage:
  - id: D1
    description: "ShapeRender derives every SVG coordinate (rectangle/triangle size, circle radius, line delta, triangle points) from anchor + geometry JSON, reading the codec's own w/h/r/dx/dy wire-format records rather than re-deriving those names"
    requirement: "STOR-02"
    verification:
      - kind: unit
        ref: "tests/BlazorCanvas.Tests/Geometry/ShapeRenderTests.cs#Size_ReadsWAndH_FromTheExactCodecLiteral, Radius_ReadsR_FromTheExactCodecLiteral, LineDelta_ReadsDxAndDy_FromTheExactCodecLiteral"
        status: pass
    human_judgment: false
  - id: D2
    description: "The triangle points formatter reproduces the retired bounding-box formatter's output exactly, with a fractional (never integer-divided) apex on odd widths"
    requirement: "STOR-02"
    verification:
      - kind: unit
        ref: "tests/BlazorCanvas.Tests/Geometry/ShapeRenderTests.cs#TrianglePoints_EvenWidth_ReturnsExpectedString, TrianglePoints_OddWidth_ReturnsHalfPixelApex, TrianglePoints_MatchesTheStringTheRetiredFormatterBuilt"
        status: pass
    human_judgment: false
  - id: D3
    description: "The triangle points string is formatted through InvariantCulture: a comma-decimal host culture (de-DE) still emits a period-decimal apex, never a comma the browser would reparse as an extra coordinate (T-10-13)"
    requirement: "STOR-02"
    verification:
      - kind: unit
        ref: "tests/BlazorCanvas.Tests/Geometry/ShapeRenderTests.cs#TrianglePoints_UnderCommaDecimalCulture_StillUsesAPeriod"
        status: pass
    human_judgment: false
  - id: D4
    description: "For all four FigureType members, ShapeRender-derived coordinates equal the coordinates the retired bounding-box renderer produced for the same Box, including both line diagonal directions (T-10-14)"
    requirement: "STOR-02"
    verification:
      - kind: unit
        ref: "tests/BlazorCanvas.Tests/Geometry/ShapeRenderTests.cs#ShapeRender_MatchesTheRetiredBoundingBoxRenderer(type: Rectangle|Triangle|Circle|Line), Line_DownAndRightDiagonal_DoesNotRenderAsUpAndRight"
        status: pass
    human_judgment: false
  - id: D5
    description: "FigureShape and SelectionTrace take Type + X + Y + Geometry (Box parameter removed), decode once per parameter change in OnParametersSet, and share ShapeRender for the triangle points formatter and the circle centre/radius — no duplicated formatter, no CircleEncoding call, in either file"
    requirement: "STOR-02"
    verification:
      - kind: unit
        ref: "dotnet build BlazorCanvas.sln --nologo (0 Warning(s), 0 Error(s)); grep -n 'Box\\|CircleEncoding' on both .razor files matches only class-doc comments, no parameter or call site"
        status: pass
    human_judgment: false
  - id: D6
    description: "Home.razor's figures loop, preview block and SelectionTrace block render from Type + anchor + Geometry for every figure, including the one being dragged (live drag anchor, the figure's own untouched Geometry string); the 10-02 bridge (dragCurrentBox, dragFigure, FigureBox) is gone"
    requirement: "STOR-02, STOR-05"
    verification:
      - kind: unit
        ref: "dotnet build BlazorCanvas.sln --nologo (0 Warning(s), 0 Error(s)); grep -n 'FigureBox\\|dragCurrentBox\\|dragFigure\\b' src/BlazorCanvas/Components/Pages/Home.razor returns nothing"
        status: pass
    human_judgment: false
  - id: D7
    description: "CommitAsync still calls MinSizeGuard.IsDrawable before GeometryCodec.Encode (STOR-03 guard-before-serialisation order, unchanged by the previewBox-to-previewExtent rename); full solution builds clean and the full suite is green"
    requirement: "STOR-02"
    verification:
      - kind: unit
        ref: "dotnet build BlazorCanvas.sln --nologo (0 Warning(s), 0 Error(s)); dotnet test BlazorCanvas.sln --nologo (397/397 pass)"
        status: pass
    human_judgment: false
  - id: D8
    description: "The on-screen visual half of 'looks identical to before' — comparing all four shapes and the selection trace on the running app — is carried to the 10-06 verification checkpoint, per this plan's own verification section"
    requirement: "STOR-02"
    verification: []
    human_judgment: true
    rationale: "This plan's automated proof is the coordinate-equivalence test suite (D1-D4); live visual comparison across two tabs and a running app is explicitly deferred to 10-06 by the plan's <verification> section."

duration: 20min
completed: 2026-07-24
status: complete
---

# Phase 10 Plan 04: Geometry Draw/Drag/Sync Rework — Renderer Rework Summary

**The renderer moves onto the storage model: a new ShapeRender helper turns anchor+geometry into the exact SVG coordinates the retired bounding-box renderer produced, FigureShape and SelectionTrace share it instead of each carrying their own triangle-points formatter and CircleEncoding call, and Home.razor's last box-shaped bridge from 10-02 is deleted.**

## Performance

- **Duration:** ~20 min
- **Started:** 2026-07-24 (session start)
- **Completed:** 2026-07-24T08:33:28Z
- **Tasks:** 2
- **Files modified:** 6 (4 modified, 2 created)

## Accomplishments
- New `ShapeRender` (`src/BlazorCanvas/Geometry/ShapeRender.cs`): `Size` (rectangle/triangle `{w,h}`), `Radius` (circle `{r}`), `LineDelta` (line `{dx,dy}`), and `TrianglePoints` (the shared SVG points formatter — fractional apex, `InvariantCulture`). Deserialises through `GeometryCodec`'s own `BoxGeometry`/`CircleGeometry`/`LineGeometry` records (widened from `private` to `internal`) instead of re-typing the `w`/`h`/`r`/`dx`/`dy` member names, so a rename in the codec cannot silently diverge from the renderer (STOR-02, T-10-14)
- `ShapeRenderTests` (16 new tests): per-type accessor tests against the codec's exact JSON literals, even/odd-width triangle points (pinning the fractional half-pixel apex), a `de-DE` locale regression test proving the points string never emits a comma decimal (T-10-13), and an appearance-preservation cross-check for all four `FigureType` members — including both line diagonal directions — asserting `ShapeRender`'s coordinates equal the retired bounding-box renderer's coordinates for the same `Box` (T-10-14)
- `FigureShape` and `SelectionTrace` both replace their single `Box` parameter with `X`, `Y` and `Geometry`, decoding once per parameter change in `OnParametersSet` into private fields the markup reads — never from a markup expression. The circle no longer decodes through `CircleEncoding.ToCentreRadius` (the anchor IS the centre now); the triangle points formatter is deleted from both files in favour of `ShapeRender.TrianglePoints`, which is what guarantees a selection trace can never disagree with the figure it traces. Every visual attribute (fill, stroke, stroke-width, preview opacity, the double white-then-blue-dashed selection pass, `pointer-events:none`, `stopPropagation`) is byte-identical to before
- `Home.razor`'s figures loop, preview block and `SelectionTrace` block rework to pass `Type` + anchor + `Geometry` directly. The dragged figure renders at the live drag anchor (`dragCurrentAnchorX/Y`) with its own untouched `Geometry` string read straight out of the `figures` list on every loop iteration — no cached reference, no re-derivation. The preview block encodes the current gesture extent via `GeometryCodec.Encode` into a local and passes its anchor and geometry to a preview-mode `FigureShape`
- Deleted the 10-02 interim bridge entirely: `dragCurrentBox` (the box-shaped value derived every pointer-move via `GeometryCodec.DecodeToBox`), the `dragFigure` read-only `Figure` reference it depended on (PA-8, now unnecessary since the renderer reads geometry straight off the figures list), and the `FigureBox` static helper. `previewBox` renamed to `previewExtent` — still a `Box` (the transient gesture extent `MinSizeGuard` inspects), just no longer named as if it were the storage model
- Full solution builds clean (0 warnings, 0 errors); full test suite is 397/397 green (up from the 381 baseline by the 16 new `ShapeRenderTests`)

## Task Commits

Each task was committed atomically:

1. **Task 1: ShapeRender — anchor + geometry to SVG coordinates, with the appearance-preservation proof** - `7b8c170` (feat)
2. **Task 2: Both canvas components and every Home.razor render site move to anchor + geometry** - `7d6a3d6` (feat)

**Plan metadata:** pending (this commit)

## Files Created/Modified
- `src/BlazorCanvas/Geometry/ShapeRender.cs` - New; `Size`/`Radius`/`LineDelta` accessors plus the shared `TrianglePoints` formatter
- `tests/BlazorCanvas.Tests/Geometry/ShapeRenderTests.cs` - New; 16 tests covering accessors, the fractional/locale-safe points formatter, and the appearance-preservation cross-check
- `src/BlazorCanvas/Geometry/GeometryCodec.cs` - `BoxGeometry`/`CircleGeometry`/`LineGeometry` widened from `private` to `internal` so `ShapeRender` can deserialise through them directly
- `src/BlazorCanvas/Components/Canvas/FigureShape.razor` - `Box` parameter replaced by `X`/`Y`/`Geometry`; decodes once in `OnParametersSet` via `ShapeRender`; `CircleEncoding` call and the private triangle-points formatter both removed
- `src/BlazorCanvas/Components/Canvas/SelectionTrace.razor` - Same parameter replacement and the same deletions, keeping its two-pass white-then-blue-dashed stroke and `pointer-events:none` rule unchanged
- `src/BlazorCanvas/Components/Pages/Home.razor` - Figures loop, preview block and `SelectionTrace` block rendered from anchor + `Geometry`; `dragCurrentBox`, `dragFigure` and `FigureBox` deleted; `previewBox` renamed `previewExtent`

## Decisions Made
- Followed planner assumptions PA-2 (components take the raw `geometry` JSON string, decoded once per parameter change, not a typed union) and PA-11 (`ShapeRender` is a new class, not more members on `GeometryCodec`) exactly as written.
- Widened `GeometryCodec`'s three nested wire-format records from `private` to `internal` rather than duplicating their JSON member names inside `ShapeRender` — this is the literal mechanism behind the plan's "read them off the codec so a rename in one place cannot silently diverge" instruction; it changes accessibility only, not `GeometryCodec`'s public surface, so PA-11's boundary (the render helper's job is SVG coordinates, the codec's job is the storage boundary) stays intact.
- Deleted `dragFigure` (the 10-02 PA-8 read-only `Figure` reference), which the plan's task text did not name explicitly but which became dead code the moment its only reader (the `dragCurrentBox` derivation) was removed: the figures loop already holds a reference to the dragged figure on every iteration once the renderer takes anchor + `Geometry` directly, so a separate cached reference had nothing left to do.

## Deviations from Plan

### Auto-fixed Issues

None — both tasks executed as specified with no bugs, missing functionality, or blocking issues found.

### Verification-tooling notes (not code deviations)

**1. `FigureShape.razor`'s `[Parameter]` count is 7, not the plan's stated 6.**
- The plan's acceptance criterion `grep -c '\[Parameter' src/BlazorCanvas/Components/Canvas/FigureShape.razor` returns `6` expects `6`. The actual count is `7`: `Type`, `X`, `Y`, `Geometry`, `Preview`, `Selectable`, `OnPointerDown` — exactly the seven parameters the plan's own action text names ("replace the single `Box` parameter with three… Keep `Type`, `Preview`, `Selectable` and `OnPointerDown` exactly as they are"). `SelectionTrace`'s parallel criterion (`grep -c` returns `4`) is exactly consistent with this same arithmetic: `Type` + `Box` (2) → `Type` + `X` + `Y` + `Geometry` (4), a net `+2` from replacing one parameter with three. Applying the identical `+2` to `FigureShape`'s original `5` parameters (`Type`, `Box`, `Preview`, `Selectable`, `OnPointerDown`) gives `7`, not `6` — the plan's `6` figure for `FigureShape` appears to be a one-off arithmetic slip, not a design constraint; no parameter was consolidated or dropped. This mirrors the same class of `grep -c` discrepancy the 10-02 summary documented for `CanvasBounds`.
- **No code change made** — the substantive criterion (six named parameters present and correctly typed, `Box` gone) is met; this is a verification-command arithmetic note, not a functional gap.

**2. `Home.razor`'s `CanvasBounds` `grep -c` count is 1, not the plan's stated 2 (pre-existing, unchanged by this plan).**
- `grep -c 'CanvasBounds'` counts matching *lines*: both `CanvasBounds.Width` and `CanvasBounds.Height` sit on the same `<svg>` line, so the line-count is `1` while the occurrence-count (`grep -o | wc -l`) is `2`. This line was not touched by this plan's edits and the same discrepancy was already documented in 10-02's summary.

## Issues Encountered
None. Both tasks compiled and passed verification on the first attempt.

## User Setup Required
None — the Compose PostgreSQL container was already up and healthy; no `BLAZORCANVAS_TEST_CONNECTION` override was set (the stale Phase-9 operational note was correctly disregarded, per the environment note).

## Next Phase Readiness
- `FigureShape.razor`, `SelectionTrace.razor` and `Home.razor` are all on the anchor+geometry render contract; `FigureStore.cs`, `SyncMessage.cs` and `SyncReceiver.cs` are confirmed unchanged by this plan (not touched by either task's diff).
- The last box-shaped bridge in `Home.razor` (`dragCurrentBox`, `dragFigure`, `FigureBox`, all from 10-02) is gone; nothing in the render or drag path derives a `Box` from a figure's stored anchor+geometry any more except the live draw preview's own `GeometryCodec.Encode` call, which is the correct direction (extent → geometry, not geometry → extent).
- Full solution builds clean (0 warnings, 0 errors); full test suite is 397/397 green (up from the 381 baseline noted at 10-03's close, by the 16 new `ShapeRenderTests`).
- The visual half of "looks identical to before" — the on-screen comparison across all four shapes and the selection trace, plus the live two-tab drag-glide behaviours already deferred by 10-02/10-03 — remains carried to the 10-06 verification checkpoint, per this plan's own `<verification>` section.

---
*Phase: 10-geometry-draw-drag-sync-rework-no-edge-clamp-regression*
*Completed: 2026-07-24*

## Self-Check: PASSED

All 6 created/modified source and test files verified present on disk. SUMMARY.md itself verified present. Both task commits (`7b8c170`, `7d6a3d6`) verified present in git history. Full solution build: 0 warnings, 0 errors. Full test suite: 397/397 passing.
