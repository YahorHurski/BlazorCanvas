# Milestones

## v1.11 Storage Model Rewrite (Shipped: 2026-07-22)

**Phases completed:** 4 phases, 19 plans, 35 tasks

**Key accomplishments:**

- Typed local geometry, an ordinal shape registry, and finite culture-invariant JSON primitives establish the pure-C# foundation for the v1.11 shape model.
- A typed style boundary now converts hostile browser JSON into bounded, allowlisted values and emits only a four-key canonical payload.
- An approved, restore-proven, redacted v1.1 PostgreSQL snapshot now preserves the exact migration subject Phase 10 needs, including ordered edge-case figures and their expected converted geometry.
- Four isolated shape definitions now preserve local geometry precisely, reject hostile or degenerate input, and reproduce the v1.1 drawing behaviour.
- The shape registry is now mechanically proven equivalent to v1.1 gestures, extensible by one test-only shape class, and resistant to bounding-box-derived point-list regressions.
- A single registry-backed gateway now rejects unsafe figure geometry, sanitises all style input, and returns only canonical JSON re-serialised from typed records.
- An additive, idempotent v11 PostgreSQL schema now holds canvases, data-driven figure types, and JSON-backed figures while the public legacy model remains intact.
- Lossless v1.1 figure conversion and stable version-8 UUID mapping are proven independently of PostgreSQL.
- A gateway-fed v11 repository now writes type-blind positions, exact numeric layers, and local bounding-box caches while preserving canvas ownership and deterministic z-collision recovery.
- A guarded scratch-database replay now proves the v1.1 fixture migrates all 708 users and 795 figures losslessly into v11, with deterministic canvases, preserved layers, fixed style, and cached bounds.
- The v11 storage model now proves every cached local bbox agrees with fresh geometry and proves hostile client input either creates no row or lands only as sanitised JSONB.
- The v11 migration now rolls back its schema, seeded registry, canvases, and figures as one PostgreSQL transaction when legacy conversion rejects a row.
- The application now prepares the additive v1.11 store before any interactive circuit, and each authenticated owner can lazily and idempotently resolve only their deterministic 1472×828 canvas.
- A browser-local SVG drawing preview restores initiating-tab feedback during a gesture while keeping figure creation commit-only across tabs.
- REG-01 human acceptance passed 3/3 on the running application: four shapes with edge clamping, selection and deletion across two windows, and a visibly gliding committed drag.

> The two preceding bullets replace auto-extracted one-liners from `12-01-SUMMARY.md` and
> `12-02-SUMMARY.md`, which recorded the *first*, failed acceptance run. That gap was closed in
> Phase 12 and re-verified — `12-VERIFICATION.md` reads `passed`, 6/6.

**Closeout type:** `override_closeout` — 21/22 requirements satisfied.

**Final state:** `dotnet build` clean (0 warnings, 0 errors); 500/500 tests passing.

### Evidence restoration at close

The milestone audit found that Phase 11's cutover-cleanup commit `1aaf45b` deleted 20 test files
(4,239 lines). Eleven (1,904 lines) were correct TEST-02 retirement — their subjects are genuinely
gone. The other nine removed the only executable proof for requirements still in force, and the same
commit rewrote `LegacyFigureConversion.cs` while deleting its unit tests.

Six of those nine were restored and rebased onto the promoted `public.*` schema before close (commit
`2f58086`), returning 197 tests and taking the suite from 303 to 500. This closed TEST-03, MODEL-01,
MODEL-05, MODEL-07, MIGR-01, and MIGR-02, which the audit had scored unsatisfied or partial.

### Known Gaps

| Requirement | Status | Detail |
|---|---|---|
| **MIGR-03** | Accepted gap | *"A test loads a v1.1-era database dump, runs the migration, and verifies every figure's rendered vertices and stacking order against expected values — the migration is proven lossless, not assumed."* No test loads the committed fixture; `V11MigrationReplayTests.cs` and `V11MigrationReplayFixture.cs` remain deleted. `v1.1-pre-rewrite.sql` is still committed and copied to test output, but has no C# consumer. |

**Why accepted:** the migration path is permanently unreachable. The sole database is in
`CatalogState.Completed`, so `V11Cutover.EnsureAsync` returns before touching it; a fresh volume
takes the `FreshUsersOnly` path, which never calls `LegacyFigureConversion`; and D-08 locks the
project against deployment. Forward risk is zero. The restored `LegacyFigureConversionTests` covers
all 8 curated manifest rows with values transcribed from the manifest. What remains unproven is the
795-row database round-trip — jsonb storage, `z` backfill from the old id, canvas attachment,
cross-user stacking, and second-run idempotency. The residual risk is retrospective rather than
forward-looking.

**The requirement text was deliberately not rewritten to fit what was built.** Full reasoning and the
route to closing it later: `milestones/v1.11-MILESTONE-AUDIT.md`.

### Carried Tech Debt

- `ShapeRegistry.All`/`.Names` return live `List` instances behind `IReadOnlyList` (09-REVIEW WR-03).
- `Home.razor.js` reimplements shape preview geometry outside the registry, with no drift guard —
  worth closing before v1.2 adds figure types.
- `V11SchemaShapeTests.cs` not restored; overlap with `V11CutoverTests.AssertFinalPublicCatalogAsync`
  is partial.
- `V11DataMigration.RunAsync(NpgsqlDataSource, …)` is now unreferenced in production.

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
