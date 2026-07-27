# Milestones

## v1.12 Canvas enlargement (Shipped: 2026-07-27)

**Phases completed:** 1 phase, 2 plans, 3 tasks

**Delivered:** Enlarged the fixed canvas from 1472×828 to 5760×2634 (a 3×3 grid of 1920×878 cells) as
a pure, grow-only size change (D-19) — existing figures stay valid, unmoved, no data migration. The
rest of the surface is reached by native browser scroll (D-18); the toolbar is now `position: fixed`
with a 48px canvas offset, preserving the `PageX` / `PageY − 48` mapping (D-43) with zero JavaScript
interop.

**Verification:** Phase 11 verified `passed`; a human approved all five success criteria live in a
browser (first-screen top-left, native-scroll reachability with the toolbar staying fixed,
figure-under-pointer accuracy at any scroll offset, all four shapes draw/drag/delete + persist).
`dotnet build BlazorCanvas.sln` clean (0/0); 416/416 tests passing. Source footprint: 8 files changed
(+23/−18). Tag: `v1.12-canvas-enlargement` (the plain `v1.12` tag belongs to the parallel star5
topology — kept distinct on purpose).

**Amendments logged (`docs/DECISIONS.md`):** D-19 (size 1472×828 → 5760×2634), D-43 (toolbar
`position: fixed`, the −48 invariant preserved via a 48px canvas offset), D-18 (native browser scroll
is now the navigation mechanism, previously an accepted cost). D-04 untouched — this is not a pan
feature.

**Key accomplishments:**

- Grew `CanvasBounds.Width`/`Height` from 1472/828 to 5760/2634 via two constants — Home.razor is
  unchanged because it already binds the SVG surface to those constants (a single size source).
- Took the toolbar out of document flow (`position: fixed; top:0; left:0; z-index:10`, below the
  100/101 save-failed modal layers) and gave the canvas a compensating `padding-top: 48px`, keeping
  `canvasY = PageY − 48` correct at any scroll offset with no JS interop.
- Re-pinned five test files to the enlarged bounds (renamed the `CanvasBounds` fact to 5760×2634,
  bumped far-corner and off-canvas cases genuinely past the new edge) — no stale 1472/828 literals
  remain, fixtures untouched, 416/416 green.

---

## v1.11 Storage model rewrite (anchor + geometry JSON) (Shipped: 2026-07-24)

**Phases completed:** 2 phases, 12 plans, 29 tasks

**Delivered:** Replaced the four-integer bounding-box storage with the anchor (`x,y`) + `geometry jsonb`
model (D-59) — `uuid` id, `numeric z` order — preserving every existing figure via a tested migration
and removing the canvas-edge clamp. No new user-facing feature.

**Verification:** Both phases verified `passed`; milestone audit passed (9/9 requirements: STOR-01…05,
MIG-01/02, SYNC-02, TEST-02). `dotnet build` clean (0/0); 416/416 tests passing. Git range
`28bd5f6`→`985a5c9` (51 commits); 40 source/test files changed (+4,654/−817). Tag: `v1.11`.

**Accepted caveats (non-blocking):** pre-rewrite pixel comparison permanently unverifiable (pre-rewrite
data destroyed by a deliberate DB reset; coordinate-level equivalence covered by `ShapeRenderTests`);
D-54 mid-drag blanket discard code-present but not live-exercised (needs two simultaneous pointers);
off-canvas figures are an accepted, currently-unrecoverable state.

**Key accomplishments:**

- Box values now round-trip through D-59 anchor coordinates and compact per-type geometry JSON.
- The immutable v1.1 SQL fixture is restored and every row has D-59 expected migration values.
- The app project now compiles on Guid figures stored as anchor coordinates plus geometry JSON.
- The real EF migration rewrites figures to uuid anchor+geometry storage and verifies the live Postgres catalog.
- The existing xUnit suite is green on the migrated Guid anchor+geometry schema.
- The real migration preserves all 795 immutable fixture figures against the D-59 manifest.
- DrawGesture reproduces pointer coordinates exactly with no canvas-edge clamp, MinSizeGuard now reads the exact {dx,dy}/{w,h}/{r} primitives GeometryCodec serialises, and CanvasCoordinates.FromPage is total over every double.
- FigureStore.MoveAsync sets exactly x and y under the user-scoped filter (no geometry parameter exists to carry one), Home.razor's drag adds the raw pointer delta to the figure's anchor with zero bounds arithmetic, Movement.cs and its clamp tests are gone, and the store suite proves geometry survives byte-identical across any move for all four shapes.
- SyncMessage reshaped from a box to anchor+geometry (D-53 amended by D-59); the D-40 receiver rules — draw-creates-only, move/rollback-update-only, idempotent delete — extracted into a new UI-free SyncReceiver and unit-tested for the first time; Home.razor's receive path now delegates entirely to it.
- The renderer moves onto the storage model: a new ShapeRender helper turns anchor+geometry into the exact SVG coordinates the retired bounding-box renderer produced, FigureShape and SelectionTrace share it instead of each carrying their own triangle-points formatter and CircleEncoding call, and Home.razor's last box-shaped bridge from 10-02 is deleted.
- The suite is finished with TEST-02: the D-41 line landmine is now proven end to end into stored geometry, the database helpers and schema assertions speak anchor+geometry natively with no bounding-box re-encoding, and the CHECK-mirroring test class is retired to what it still proves after D-59 deleted the CHECKs it used to mirror.

---

## v1.1 Canvas resize · selection UX · no-JS removal (Shipped: 2026-07-21)

**Phases completed:** 3 phases, 4 plans, 9 tasks

**Key accomplishments:**

- Fixed-size SVG canvas enlarged to 1472 x 828 with CanvasBounds-driven rendering and re-pinned geometry edge tests.
- Local draw selection and a topmost blue-and-white dashed SVG trace replace the previous red selection outline.
- A human approved all five selection UX criteria and the two-tab remote-delete edge on the running application.
- The derived runtime constraint now mirrors the ADR's permissive retired-JavaScript policy, with no application-surface change.

---

## v1.0 MinVP (Shipped: 2026-07-17)

**Phases completed:** 5 phases, 23 plans, 56 tasks

**Key accomplishments:**

- PostgreSQL 17 running in Docker Compose with a proven-persistent named volume, plus a two-project .NET 10 solution (Blazor Server app + xUnit tests) with EF Core/Npgsql packages pinned and a verified matching connection string — zero schema, zero UI, ground only.
- Pure C# geometry core in `BlazorCanvas.Geometry` — per-type normalisation, per-type min-size guard, the delta-clamp move formula, and circle-as-inscribed-square encoding — proven by all three TEST-01 mandated tests (line landmine, clamp per-axis independence, circle round-trip under translation), 77/77 tests green.
- EF Core migration wires CanvasDbContext (User, Figure entities) to a live PostgreSQL 17 container, with all four CHECK constraints, the user_id index, and the inscribed-square table comment verified present via pg_constraint — not merely assumed from a green build.
- 68 new xUnit tests against the live PostgreSQL container prove ROADMAP success criterion 3 (the database itself rejects a non-square circle, zero-area box, and zero-length line via named CHECK constraints) and the decisive D-50 claim (MinSizeGuard.IsDrawable agrees with the live database across a 32-case matrix, in both directions) — including a real `docker compose down`/`up -d` container-teardown test proving the named volume holds EF-written figures.
- Hardened `Movement.ClampDelta` (0 instead of inverting when lo > hi) and `CircleEncoding.ClampDrawRadius` (centre-clamp + zero-floor), each proven by a named regression test that reproduces the exact live database repro from 01-VERIFICATION.md and was RED on the pre-fix code.
- CanvasDbContextFactory.CreateDbContext now throws an actionable InvalidOperationException instead of silently falling back to a hardcoded localhost:5432 connection string when ConnectionStrings:Canvas is unresolved.
- Stripped the default Blazor Web App template down to a blank pass-through shell — Bootstrap bundle deleted, `MainLayout` reduced to `@Body` + framework error-UI, and the NavMenu/Counter/Weather demo scaffold removed — clearing the way for 02-03's login card and toolbar to own their chrome entirely via `02-UI-SPEC.md` tokens.
- Cookie-authentication backbone wired in Program.cs (AddCookie/LoginPath, AddAuthorization, AddCascadingAuthenticationState, correct Auth->Authz->Antiforgery middleware order), a CSRF-protected POST /logout endpoint, and the single-source UsernameNormalizer (D-44) built TDD-first.
- Static-SSR /login with race-safe create-on-unknown handler, plus an InteractiveServer [Authorize] canvas shell that reads the user_id cookie claim with zero DB lookups and carries a right-aligned, antiforgery-protected Logout form.
- CanvasCoordinates (page-to-canvas mapping) and DrawGesture (press+cursor+type -> clamped, normalised Box), both pure functions built entirely on the Phase 1 geometry core with zero Blazor dependency, proven by 214 new xUnit tests.
- FigureStore built on IDbContextFactory<CanvasDbContext> — `WHERE user_id = @id ORDER BY id` load and an INSERT that returns the database-assigned id, with Login.razor migrated off the scoped context.
- Six-button toolbar (with migrated Logout form) and a four-shape SVG renderer, both unmounted until plan 03-04 wires them into Home.razor
- Home.razor rewritten as pure wiring — mounts the six-button Toolbar, places a 1280x720 borderless white SVG at document (0, 48) on a #DCE0E5 page, and loads/renders the logged-in user's own figures in creation order via FigureStore.LoadAsync
- Blazor SVG drawing now previews live, commits on release or canvas leave, and persists database-assigned figures immediately.
- Owner-filtered EF Core update/delete paths with database tests proving affected-row counts and cross-user isolation
- Selectable SVG figures and a live Delete button callback without changing layout or CSS
- Local selection, page-spanning drag commits, clamped drop persistence, and immediate toolbar deletion on the canvas page
- Human-approved select, drag, and delete behavior after automated build and database tests passed
- D-53 sync messages and a user-keyed in-memory notifier with tests proving isolation and subscription cleanup
- Program.cs now wires the process-wide sync notifier and the bounded Npgsql retry strategy that later cross-tab and rollback plans depend on.
- Home.razor now mirrors draw, delete, and drag-glide state across a user's open tabs through the in-memory notifier while preserving the locked no-resurrection and one-UPDATE-per-drag rules.
- DATA-04 save failures now restore cross-tab truth with rollback and a forced database reload modal.
- Real two-tab verification approved live cross-tab sync, failed-save recovery, one-UPDATE drag persistence, and the markup-only autofocus tradeoff.

---
