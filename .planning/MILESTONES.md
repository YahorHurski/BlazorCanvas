# Milestones

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
