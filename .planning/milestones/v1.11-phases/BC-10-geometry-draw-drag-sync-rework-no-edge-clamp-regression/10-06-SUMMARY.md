---
phase: 10-geometry-draw-drag-sync-rework-no-edge-clamp-regression
plan: 06
type: execute
status: complete
requirements: [STOR-04, STOR-05, SYNC-02, TEST-02]
completed: 2026-07-24
---

# 10-06 Summary — Phase regression gate + live verification

Closed the phase. Ran the clean-state build and full suite as one regression gate, then
verified on real screens. A real runtime defect was found at the live checkpoint, fixed, and
re-verified.

## Task 1 — Phase regression gate (automated)

Recorded from a clean `bin`/`obj` state:

- `dotnet build BlazorCanvas.sln --nologo` → **0 Warning(s), 0 Error(s)**.
- `dotnet test BlazorCanvas.sln --nologo` → **416 passed, 0 failed, 0 skipped** (416 total), DB up.
- `MigrationRoundTrip` filter → still passing; Phase-9 MIG-02 proof untouched.

Retired-surface sweep — all four deleted files absent, all four new files present:

- Absent: `Movement.cs`, `Geometry/ClampTests.cs`, `Database/GuardMirrorsChecksTests.cs`, `UnitTest1.cs`.
- Present: `Geometry/ShapeRender.cs`, `Sync/SyncReceiver.cs`, `Geometry/ShapeRenderTests.cs`, `Sync/SyncReceiverTests.cs`.
- `CircleEncoding` exposes exactly two members (`FromCentreRadius`, `ToCentreRadius`); the `grep -c 'public static'` count of 3 is the class-declaration line plus the two members (documented grep-pattern artifact, also noted in 10-01).

Requirement traceability (each ID against a named satisfying check):

| Req | Satisfied by |
|-----|-------------|
| STOR-02 | `FigureStoreTests` (10-02) — anchor-only move; geometry byte-identical across a move for all four types |
| STOR-03 | `MinSizeGuardTests` (10-01) — per-type zero-extent guard reading the serialised `{dx,dy}`/`{w,h}`/`{r}` |
| STOR-04 | `FigureStoreTests` off-canvas round-trip + `GeometryCodecTests` off-canvas cases (10-02, 10-05) + live steps D |
| STOR-05 | `ShapeRenderTests` equivalence + `SyncReceiver`/store ordering tests (10-04, 10-02) + live steps B/E |
| SYNC-02 | `SyncReceiverTests` + `CanvasSyncNotifierTests` (10-03) + live step E |
| TEST-02 | full 416-test suite reworked to anchor+geometry (10-01→10-05) |

## Task 2 — Live verification (human checkpoint)

### Defect found and fixed (was invisible to build + unit tests)

At first live attempt **every draw threw** and the page showed the Blazor unhandled-error UI.
Root cause: the three render sites in `Home.razor` bound the string-typed `Geometry` parameter
as a **literal** — `Geometry="f.Geometry"` / `"previewGeometry.Geometry"` / `"selected.Geometry"` —
so the component received the source text (`"f.Geometry"`, …) and `ShapeRender` tried to JSON-parse
it, throwing `'f.Geometry' is an invalid JSON literal`. Razor takes a quoted string-typed attribute
literally unless it is `@`-prefixed; the non-string params (`Type`, `X`, `Y`) evaluated fine, which
is why it compiled and the unit tests (which drive `ShapeRender` directly, never the `.razor` wiring)
stayed green.

The 10-04 plan's own acceptance criterion baked in the bug (it required `grep -c 'Geometry="f.Geometry"'`),
so the executor produced compiling-but-broken markup faithfully. This is exactly the render-wiring
class of defect this live checkpoint exists to catch.

Fix (commit `84b65b6`): added the `@` prefix to all three `Geometry` bindings. Clean rebuild
(0/0), app restarted without exceptions, defect gone.

### Human confirmation (after fix)

Verifier **approved** the verifiable steps: draw/render for all four shapes, drag/delete/persist
across reload, the diagonal-line landmine surviving reload, the no-clamp behaviours (off-canvas
figures stay out; large clipped near-edge circle; silent rejected click), and live cross-tab sync
(draw/glide/delete propagation and the D-40 resurrection check).

### Recorded as UNVERIFIED (environmental — not assumed to pass)

- **SC1 "looks identical to *before* the rewrite"** — no pre-rewrite figures exist to compare
  against: the DB volume was reset during this run to clear a cross-branch schema conflict, so all
  prior data was destroyed. The coordinate-level half of appearance preservation is covered by
  `ShapeRenderTests`; the pixel-diff-against-old-data half could not be performed. Freshly drawn
  shapes were confirmed to render correctly (white fill, black outline, blue-white dashed selection).
- **Step 13 (blanket mid-drag broadcast discard, D-54)** — requires two simultaneous pointers
  (hold a drag in window 1 while drawing in window 2); single-pointer environment. The 50 ms
  trailing-edge glide (step 10/14) was confirmed; the mid-drag *discard* specifically was not.

## Environment notes

- Test DB: Compose `canvas-postgres` on host port 5433, default `canvas` database (volume reset
  clean at phase start). The plans' `canvas_phase09` / `BLAZORCANVAS_TEST_CONNECTION` operational
  notes are stale Phase-9 advice and were not used.
- App run via `dotnet run --project src/BlazorCanvas` at http://localhost:5054.

## Deviations

- One code change outside the plan's `files_modified: []` — the `Home.razor` `Geometry` binding fix
  (commit `84b65b6`). Justified: the checkpoint's own purpose is to surface exactly this defect, and
  the plan's prohibition is against *recording unmade observations*, not against fixing a defect the
  observation reveals. Fix is minimal (3 chars), re-verified live.

## Self-Check: PASSED
