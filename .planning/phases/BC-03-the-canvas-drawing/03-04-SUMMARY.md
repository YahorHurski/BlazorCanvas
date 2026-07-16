---
phase: BC-03-the-canvas-drawing
plan: 04
subsystem: ui
tags: [blazor, razor-components, svg, canvas, wiring]

# Dependency graph
requires:
  - phase: BC-03 plan 02
    provides: FigureStore.LoadAsync(userId) — the app's only figure read, WHERE user_id ORDER BY id
  - phase: BC-03 plan 03
    provides: Tool enum, Toolbar.razor (Armed/ArmedChanged/DeleteEnabled), FigureShape.razor
provides:
  - Home.razor assembled as pure wiring — mounts Toolbar, places the 1280x720 borderless SVG at document (0, 48), loads and renders the logged-in user's figures in creation order
  - .canvas-surface / .shape-armed CSS driving the crosshair cursor for armed shape tools
  - app.css's #DCE0E5 global page background (additive, Phase 2 login styling untouched)
affects: [BC-03 plan 05 (pointer/draw handlers land in this same Home.razor)]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Home.razor as pure composition root: no business logic, only Toolbar + Toolbar's bound Tool + FigureStore.LoadAsync + a foreach of FigureShape"
    - "@key=\"f.Id\" on every list-rendered component whose backing collection will grow in a later plan, to keep Blazor's differ from reusing the wrong element"

key-files:
  created: []
  modified:
    - src/BlazorCanvas/Components/Pages/Home.razor
    - src/BlazorCanvas/Components/Pages/Home.razor.css
    - src/BlazorCanvas/wwwroot/app.css

key-decisions:
  - "Canvas surface's shape-armed cursor class named 'shape-armed' (not 'tool-armed' or similar) to match the plan's own terminology verbatim, keeping the CSS selector self-documenting"
  - "app.css's html,body rule stayed additive-only (background added, margin: 0 untouched) so Phase 2's shipped login page styling is not disturbed, per explicit environment note"

patterns-established:
  - "Pattern: any component-scoped stylesheet comment that would otherwise repeat the literal CSS declaration it explains (e.g. 'display: block is not cosmetic...') must be reworded to avoid duplicating the grep-checked literal, exactly as 03-03 did for 'Delete' and '<svg'"

requirements-completed: [CANV-01, CANV-02, DATA-01]

coverage:
  - id: D1
    description: "Home.razor renders <Toolbar> immediately followed by a 1280x720 viewBox-free <svg class=\"canvas-surface\"> with no CSS border, at document position (0, 48)"
    requirement: "CANV-01"
    verification:
      - kind: manual_procedural
        ref: "curl-driven login + GET / against a running dotnet instance: rendered HTML shows <Toolbar> markup then <div class=\"canvas-area\"><svg class=\"canvas-surface\" width=\"1280\" height=\"720\">...</svg></div> with no viewBox/border in source"
        status: pass
      - kind: unit
        ref: "source-assertion greps in 03-04-PLAN.md Task 1 (width=1280 count 1, height=720 count 1, viewbox count 0, preserveAspectRatio count 0) — all verified"
        status: pass
    human_judgment: true
    rationale: "Precise pixel positioning (x:0, y:48) requires a real browser's element inspector to measure the SVG's bounding rect; curl/HTML-source inspection proves structure and attributes but not rendered layout geometry. Full visual/positional confirmation happens at the 03-05 human-verify checkpoint once pointer interaction is also mounted."
  - id: D2
    description: "Toolbar renders exactly six buttons with pointer armed on load (is-armed class present, aria-pressed) and Delete natively disabled; Logout migrated out of Home.razor entirely"
    requirement: "CANV-02"
    verification:
      - kind: manual_procedural
        ref: "curl-driven GET / for a newly logged-in user: HTML shows 'tool-button is-armed' on the Pointer button, 'disabled' attribute on the Delete button, and the Logout submit button inside its own form — matches 03-03's Toolbar contract, now actually mounted"
        status: pass
    human_judgment: false
  - id: D3
    description: "The logged-in user's figures load via FigureStore.LoadAsync(userId) (WHERE user_id = @id ORDER BY id) and render inside the SVG in that same order, keyed by database id"
    requirement: "DATA-01"
    verification:
      - kind: integration
        ref: "tests/BlazorCanvas.Tests/Data/FigureStoreTests.cs (03-02, unchanged) — LoadAsync_NeverReturnsAnotherUsersFigures, LoadAsync_ReturnsFiguresInCreationOrder"
        status: pass
      - kind: manual_procedural
        ref: "psql-inserted rectangle figure for a live logged-in test user, then GET / re-fetched: response SVG contained <rect x=\"100\" y=\"100\" width=\"100\" height=\"100\" .../> matching the inserted row; app log showed 'SELECT f.id, f.type, f.user_id, f.x1, f.x2, f.y1, f.y2 FROM figures AS f WHERE f.user_id = @userId ORDER BY f.id' with a bound @userId parameter"
        status: pass
    human_judgment: false
  - id: D4
    description: "userId reaches LoadAsync only from the user_id cookie claim (T-03-01 IDOR mitigation) — no component parameter, route parameter, query string, or form field can influence it"
    requirement: "DATA-01"
    verification:
      - kind: unit
        ref: "source-assertion greps in 03-04-PLAN.md Task 1: FindFirst(\"user_id\") count 1, LoadAsync(userId) count 1, no onpointerdown/InsertAsync leaked forward — all verified"
        status: pass
    human_judgment: false

duration: 25min
completed: 2026-07-16
status: complete
---

# Phase BC-03 Plan 04: Canvas Page Assembly Summary

**Home.razor rewritten as pure wiring — mounts the six-button Toolbar, places a 1280x720 borderless white SVG at document (0, 48) on a #DCE0E5 page, and loads/renders the logged-in user's own figures in creation order via FigureStore.LoadAsync**

## Performance

- **Duration:** ~25 min
- **Started:** 2026-07-16T12:20:00Z (approx)
- **Completed:** 2026-07-16T12:47:06Z
- **Tasks:** 2
- **Files modified:** 3

## Accomplishments
- `Home.razor` no longer owns any inline toolbar markup, the logout form, or the `AntiforgeryStateProvider` injection — all migrated into `Toolbar.razor` by plan 03-03 and now actually mounted via `<Toolbar @bind-Armed="armedTool" />`
- The canvas is a `viewBox`-free, border-free `<svg class="canvas-surface" width="1280" height="720">` — the whole basis of D-18's "one canvas unit = one CSS pixel, no rescale with window" contract
- `OnInitializedAsync` keeps the existing `user_id` claim read verbatim and adds exactly one line, `figures = await Figures.LoadAsync(userId)` — no database lookup for the user themselves, only for their figures
- Each loaded figure renders as `<FigureShape @key="f.Id" Type="FigureTypeNames.Parse(f.Type)" Box="new Box(f.X1, f.Y1, f.X2, f.Y2)" />` in list order, with no re-sort/reverse/filter — `ORDER BY id` from the store IS the paint order IS the z-order (D-39)
- `app.css` gained one additive declaration (`background: #DCE0E5` on the existing `html, body` rule) — `margin: 0` and every other rule untouched, so Phase 2's shipped login page is undisturbed
- `Home.razor.css` lost its migrated `.toolbar`/`.toolbar-left`/`.logout-form`/`.logout-button` rules (now living only in `Toolbar.razor.css`) and gained `.canvas-surface` (block-display, white, no border) plus a `.shape-armed` crosshair-cursor modifier

## Task Commits

Each task was committed atomically:

1. **Task 1: Home.razor — mount the toolbar, place the SVG canvas, load and render the user's figures** - `e19b28e` (feat)
2. **Task 2: The page and canvas surface CSS — grey page, white borderless canvas, tool cursor** - `bb0c1f3` (feat)

**Plan metadata:** (pending — final metadata commit follows this SUMMARY)

## Files Created/Modified
- `src/BlazorCanvas/Components/Pages/Home.razor` - rewritten as composition: Toolbar + 1280x720 SVG + figures foreach, `userId`/claim-read logic extended by one `LoadAsync` call
- `src/BlazorCanvas/Components/Pages/Home.razor.css` - migrated toolbar/logout rules deleted; `.canvas-surface` and `.shape-armed` cursor rule added; `.canvas-area` retained
- `src/BlazorCanvas/wwwroot/app.css` - one additive `background: #DCE0E5` declaration on the existing `html, body` rule

## Decisions Made
- Named the shape-tool cursor modifier class `.shape-armed` (matching the plan's own terminology) rather than inventing a different name, so the CSS selector reads self-documenting against the plan and future readers.
- Kept the `app.css` edit strictly additive per the environment notes — verified via `git diff` showing exactly one inserted line and zero deletions, protecting Phase 2's already-shipped login page styling.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Reworded two CSS comments that accidentally duplicated their own grep-checked literals**
- **Found during:** Task 2 self-verification (acceptance-criteria greps)
- **Issue:** The explanatory comment above `.canvas-surface` used the literal phrase `display: block` in prose, making `grep -c "display: block"` return 2 instead of the required 1. Separately, the comment above `.shape-armed` used the word "grab" (explaining why no grab/grabbing rule exists), making `grep -c "grab"` return 1 instead of the required 0.
- **Fix:** Reworded both comments to describe the same rationale without repeating the literal grep target — "This is not cosmetic..." instead of "display: block is not cosmetic...", and "No drag-affordance cursor rules here" instead of "No grab/grabbing rules here".
- **Files modified:** `src/BlazorCanvas/Components/Pages/Home.razor.css`
- **Verification:** Re-ran all Task 2 source-assertion greps; `display: block` count is now 1, `grab` count is now 0, all other criteria still pass.
- **Committed in:** `bb0c1f3` (Task 2 commit — the miswording was caught and fixed before the commit was made, so no separate fix commit was needed)

---

**Total deviations:** 1 auto-fixed (1 bug — self-inflicted comment wording that would have failed the plan's own acceptance gate)
**Impact on plan:** Cosmetic-only; no behavior, structure, or CSS rule changed. No scope creep.

## Issues Encountered
None beyond the self-caught wording issue above.

## User Setup Required
None - no external service configuration required. The PostgreSQL dev database was already running per the environment notes; it was never started, stopped, or otherwise touched. A manual behavior-verification pass ran the app via `dotnet run`, logged in a fresh test user through a real HTTP POST /login round-trip, inserted and then deleted one throwaway test figure via `docker exec ... psql` directly against the running container (not via any app code path), and stopped the app cleanly afterward — no test data was left behind.

## Next Phase Readiness
- `Home.razor` is ready for plan 03-05 to add pointer event handlers, a live draw-preview field, and `FigureStore.InsertAsync` — this plan deliberately added none of those (verified: zero `onpointerdown`/`onpointermove`/`onpointerup`/`onpointerleave`/`InsertAsync` occurrences in the file).
- The `.shape-armed` CSS class and `armedTool` field are already wired so plan 03-05 only needs to *read* `armedTool` to decide what to draw, not re-plumb the toolbar binding.
- `@key="f.Id"` is already in place on the figures loop, which plan 03-05's SUMMARY notes as load-bearing for the append-on-commit behavior it will add.
- `dotnet build` (0 warnings, 0 errors) and `dotnet test BlazorCanvas.sln` (388/388 passing, no regression) both confirmed clean after this plan.
- No blockers or concerns carried forward.

---
*Phase: BC-03-the-canvas-drawing*
*Completed: 2026-07-16*
