---
phase: BC-01-database-schema-geometry-core
verified: 2026-07-15T00:00:00Z
status: gaps_found
score: 4/6 must-haves verified
behavior_unverified: 0
overrides_applied: 0
gaps:
  - truth: "The clamp and circle-draw maths robustly keep every figure inside the canvas (D-24/D-29/D-36) — the exact premise the schema relies on to justify having NO canvas-bounds CHECK constraint"
    status: failed
    reason: "CircleEncoding.ClampDrawRadius has no lower bound and Movement.ClampDelta silently inverts when lo > hi. Both were confirmed by direct code inspection (2026-07-15) and independently reproduced live against the running database: INSERT INTO figures (..., 'circle', -10, 355, 0, 365) — the exact off-canvas box CR-01 predicts from ClampDrawRadius(cx: -5, cy: 360, distance: 50) — succeeds and satisfies circle_is_a_circle, because it is a valid square (side 10, even, x2>x1) that merely happens to sit outside 0..1280x0..720. Neither MinSizeGuard nor any CHECK constraint rejects it. This is a live, reproducible instance of the exact class of silent failure the phase exists to guard against (D-50's own framing), located in the same clamp code the phase's mandated tests were meant to prove correct — the tests just don't reach this input region."
    artifacts:
      - path: src/BlazorCanvas/Geometry/CircleEncoding.cs
        issue: "ClampDrawRadius (lines 27-36) has no Math.Max(0, ...) floor and does not clamp the centre into the canvas first; a centre outside 0..1280x0..720 (reachable from raw pointer coordinates per BC-02's future drag handler) produces a negative radius, which FromCentreRadius + Normalisation then turn into a legal-looking off-canvas circle that both MinSizeGuard and circle_is_a_circle accept."
      - path: src/BlazorCanvas/Geometry/Movement.cs
        issue: "ClampDelta (line 10) is `Math.Min(Math.Max(v, lo), hi)` with no guard for lo > hi. ClampMove(box, 0, 0) is therefore NOT the identity for any box wider/taller than the canvas, or already partly out of bounds — a zero-delta move silently teleports the figure. Confirmed no existing test exercises this: `dotnet test --filter FullyQualifiedName~ClampMove` lists no test with an out-of-canvas or oversized input box."
    missing:
      - "Floor ClampDrawRadius's return at 0, or clamp the centre into the canvas before capping (01-REVIEW.md CR-01's suggested fix)"
      - "Guard ClampDelta's lo > hi case so a degenerate/oversized box does not move (01-REVIEW.md CR-02's suggested fix)"
      - "A regression test for both: a negative-radius-inducing centre, and ClampMove(oversizedBox, 0, 0) == oversizedBox — none of the current 145 tests cover either input region"
  - truth: "dotnet ef design-time tooling reliably targets this project's own PostgreSQL 17 container, never the unrelated native PostgreSQL 18 service that also listens on this exact machine"
    status: failed
    reason: "CanvasDbContextFactory.CreateDbContext falls back to a hardcoded `Host=localhost;Port=5432;...` connection string whenever ConnectionStrings:Canvas cannot be found (both AddJsonFile calls are optional:true and the base path is Directory.GetCurrentDirectory(), not the project directory). Confirmed by direct code read (src/BlazorCanvas/Data/CanvasDbContextFactory.cs:25). Port 5432 on this machine is the native postgresql-x64-18 Windows service, not this project's container — which plan 01-03 deliberately moved to port 5433 for exactly this reason (see 01-03-SUMMARY.md's documented deviation). Running `dotnet ef migrations add` / `database update` from the repository root (rather than src/BlazorCanvas/) silently applies migration DDL to the wrong PostgreSQL server with no warning."
    artifacts:
      - path: src/BlazorCanvas/Data/CanvasDbContextFactory.cs
        issue: "Line 25: `?? \"Host=localhost;Port=5432;Database=canvas;Username=postgres;Password=postgres\"` — a silent wrong-server fallback. Does not affect the running app (Program.cs's own registration is separate and correctly reads Port=5433 from appsettings.Development.json), but is reachable by any `dotnet ef` invocation from the repo root."
    missing:
      - "Throw an actionable InvalidOperationException instead of falling back when ConnectionStrings:Canvas is unavailable at design time (01-REVIEW.md CR-03's suggested fix)"
---

# Phase BC-01: Database, Schema & Geometry Core Verification Report

**Phase Goal:** A running PostgreSQL holds a schema that *machine-enforces* the geometry laws, and the pure geometry maths those laws mirror is written and proven. The three silent failure modes are guarded before any UI exists to hide them.

**Verified:** 2026-07-15
**Status:** gaps_found
**Re-verification:** No — initial verification

## Goal Achievement

### Observable Truths

| # | Truth | Status | Evidence |
|---|-------|--------|----------|
| 1 | `docker compose up` starts PostgreSQL 17, database `canvas`, named volume; rows survive a container restart (ROADMAP SC1) | ✓ VERIFIED | `docker exec canvas-postgres psql -c "select version()"` → `PostgreSQL 17.10`. `docker volume ls` → `project1_canvas-pgdata` (named, not anonymous). Container reported `Up ... (healthy)`. Persistence with real EF-written data re-proven by `GuardMirrorsChecksTests.FiguresWrittenViaEfCore_SurviveContainerTeardown`, which shells a real `docker compose down`/`up -d --wait`. **Known deviation:** host port is 5433, not the D-27/ROADMAP-documented 5432, because a native `postgresql-x64-18` Windows service permanently occupies 5432 on this machine. This is a user-approved deviation from a checkpoint (see 01-03-SUMMARY.md); `docs/DECISIONS.md` D-27 was not amended to match — flagged as open documentation drift, not a functional gap. |
| 2 | Exactly two tables (`users`, `figures`) via EF Core migrations applied automatically at startup — no `canvases`, no `created_at` — carrying the CHECK constraints, the `user_id` index, and the circle-convention `COMMENT ON TABLE` (ROADMAP SC2) | ✓ VERIFIED | Live query: `select table_name from information_schema.tables where table_schema='public'` → exactly `__EFMigrationsHistory`, `figures`, `users`. `figures` columns → exactly `id, user_id, type, x1, y1, x2, y2` (7, `type` is `text`). `users` columns → exactly `id, username, password` (3). `pg_constraint` → exactly 4 CHECKs: `box_is_a_box`, `circle_is_a_circle`, `figures_type_is_known`, `line_is_a_line` (3 geometry CHECKs + the type-validity CHECK, matching `CONSTRAINT-schema`). `pg_indexes` → `ix_figures_user_id` present. `obj_description('figures'::regclass)` → contains "inscribed in". `pg_type` enum count → 0. `id` columns are `is_identity = YES`. `users.username` has a `UNIQUE` index; FK `user_id → users.id` is `ON DELETE CASCADE`. All confirmed by direct `psql` queries against the live container, independent of the test suite and the SUMMARY claims. |
| 3 | The database itself refuses an illegal row — non-square/odd-sided circle, zero-area rectangle, zero-length line — rejected by a CHECK constraint, not application code (ROADMAP SC3) | ✓ VERIFIED | Live, raw-SQL reproduction (bypassing the app and the C# guard entirely, transaction rolled back): `('circle',0,0,10,8)` → `ERROR: violates check constraint "circle_is_a_circle"`. `('circle',0,0,9,9)` (odd-sided) → same constraint. `('rectangle',10,10,90,10)` (zero height) → `ERROR: violates check constraint "box_is_a_box"`. `('line',10,10,10,10)` (zero length) → `ERROR: violates check constraint "line_is_a_line"`. `('line',10,10,90,10)` (horizontal, legal) → inserts successfully, confirming the CHECKs discriminate rather than blanket-reject. `CheckConstraintTests.cs` independently confirms this pattern (13 rejection cases + acceptance cases) via `PostgresException.SqlState == "23514"` assertions, bypassing `MinSizeGuard`/`Normalisation` per its own file comment (line 9), confirmed by inspection. |
| 4 | The three mandated TEST-01 tests pass: clamp maths, circle inscribed-square round-trip, line normalisation (ROADMAP SC4) | ✓ VERIFIED | Confirmed by test enumeration (`dotnet test --list-tests`): `NormalisationTests.Line_UpAndRightDiagonal_IsNotFlippedToOppositeDiagonal`, `ClampTests.FlushRightEdge_XClippedToZero_YPassesThroughAtFullDelta`, `CircleEncodingTests.Radius_SurvivesTenSuccessiveTranslations_IncludingTwoEdgeClipped` all exist and match the mandated assertions verbatim. Spot-check run (single named test, not full suite): `dotnet test --filter FullyQualifiedName~Line_UpAndRightDiagonal_IsNotFlippedToOppositeDiagonal` → 1 passed. Orchestrator-confirmed full run: `dotnet test BlazorCanvas.sln` → 145/145 passing, 0 build warnings. `Normalisation.cs` and `MinSizeGuard.cs` source read directly and matches `CONSTRAINT-schema`'s predicates character-for-character with the live `pg_constraint` SQL text. |
| 5 | The clamp/circle-draw maths robustly keeps every figure inside the canvas — the premise the schema relies on for having no bounds CHECK (D-24/D-29/D-36, underlying the "three silent failure modes are guarded" framing of the phase goal) | ✗ FAILED | Two unresolved Critical defects, both confirmed independently of 01-REVIEW.md by direct code read and (for the circle case) live reproduction against the running database. See Gaps. |
| 6 | `dotnet ef` design-time tooling cannot silently target the wrong PostgreSQL server on this machine | ✗ FAILED | `CanvasDbContextFactory.CreateDbContext` falls back to a hardcoded `Port=5432` connection string, confirmed by direct code read. See Gaps. |

**Score:** 4/6 truths verified (0 present, behavior-unverified)

### Required Artifacts

| Artifact | Expected | Status | Details |
|----------|----------|--------|---------|
| `docker-compose.yml` | PostgreSQL 17, named volume, `canvas` db | ✓ VERIFIED | Present, correct image/env/named volume/healthcheck. Port `5433:5432` per approved deviation. |
| `BlazorCanvas.sln` + 2 projects | Two-project net10.0 solution | ✓ VERIFIED | Confirmed via `dotnet sln list` claims in 01-01-SUMMARY.md and file presence; build/test independently spot-checked green. |
| `src/BlazorCanvas/Geometry/*.cs` (8 files) | Pure C# geometry core | ✓ VERIFIED | All 8 files present, read in full (Box, CanvasBounds, FigureType, FigureTypeNames, Normalisation, MinSizeGuard, Movement, CircleEncoding). Zero non-`System` usings. |
| `src/BlazorCanvas/Data/*.cs` | Entities, DbContext, design-time factory | ✓ VERIFIED (with defect) | `User.cs`, `Figure.cs`, `CanvasDbContext.cs` present and correct (CHECK predicates verified live). `CanvasDbContextFactory.cs` present but carries CR-03's defect (see Gaps). |
| `src/BlazorCanvas/Migrations/*` | Initial migration, applied at startup | ✓ VERIFIED | Live schema matches the migration's intent exactly (verified via `pg_constraint`/`information_schema`, not the migration source alone). |
| `tests/BlazorCanvas.Tests/Geometry/*.cs` (4 files) | TEST-01 mandated tests | ✓ VERIFIED | All 4 files present; test names enumerated and match plan's mandated assertions; one spot-run confirmed passing. |
| `tests/BlazorCanvas.Tests/Database/*.cs` (4 files) | DB-refuses-illegal-row + D-50 mirror tests | ✓ VERIFIED | All 4 files present; `CheckConstraintTests` confirmed to bypass the guard (grep + live reproduction of its core assertions). |

### Key Link Verification

| From | To | Via | Status | Details |
|------|----|----|--------|---------|
| `docker-compose.yml` | `appsettings.Development.json` | port/db/credentials match | ✓ WIRED | Both use `Port=5433` (moved from 5432 together, consistently, per 01-03's deviation). |
| `CanvasDbContext.OnModelCreating` (`HasCheckConstraint` x4) | live `figures` table | migration → `pg_constraint` | ✓ WIRED | All 4 named constraints present live, predicates match source character-for-character. |
| `MinSizeGuard.IsDrawable` | the 3 named CHECK constraints | D-50 mirror | ✓ WIRED (bounded) | Confirmed by direct predicate comparison (matches 01-REVIEW.md's own table) and by `GuardMirrorsChecksTests`'s 32-case matrix — but the matrix (per 01-REVIEW.md and my own confirmation via `dotnet test --list-tests`) only probes coordinates in `[-20, 1300]`-ish ranges without a dedicated negative-radius or oversized-box case that isolates CR-01/CR-02, so agreement is proven within a bounded, non-adversarial region, not universally. |
| `CircleEncoding.FromCentreRadius`/`Normalisation.Normalise` | `MinSizeGuard`/`circle_is_a_circle` | round-trip validity | ⚠️ HOLLOW (edge case) | Holds for every centre inside the canvas (proven). Fails to holds for a centre outside the canvas: `ClampDrawRadius` produces a negative radius that `Normalisation` silently turns into a *valid-looking* off-canvas circle both the guard and the CHECK accept. Reproduced live (see Gaps). |

### Requirements Coverage

| Requirement | Source Plan | Description | Status | Evidence |
|-------------|-------------|-------------|--------|----------|
| DATA-02 | 01-01, 01-03 | Every operation writes to PostgreSQL; no Save button; schema via EF Core migrations at startup, incl. CHECKs/comment; two tables only | ✓ SATISFIED | Schema, migrations, and persistence proven live (Truths 1-2). The runtime write-policy half of DATA-02 (INSERT/UPDATE/DELETE, no Save button) is explicitly out of scope for this phase (01-CONTEXT.md `<deferred>`) — correctly deferred to Phases 3-5, not a gap here. |
| TEST-01 | 01-02, 01-04 | Three mandated silent-failure-mode tests: clamp maths, circle round-trip, line normalisation | ✓ SATISFIED (narrowly) | The three tests exist, are named correctly, and pass (Truth 4). **Caveat:** the phase's own stated purpose — "the three silent failure modes are guarded before any UI exists to hide them" — is only partially true: the mandated tests' literal scope passes, but the same clamp/circle-encoding code has two independently-confirmed unresolved Critical defects (CR-01, CR-02) in the exact functions TEST-01 exists to validate, reachable by ordinary off-canvas pointer input the moment BC-02 wires up drawing. See Truth 5 / Gaps. |

No orphaned requirements — `REQUIREMENTS.md`'s traceability table maps only DATA-02 and TEST-01 to Phase 1/BC-01, and both are declared in plan frontmatter (`01-01-PLAN.md`, `01-02-PLAN.md`, `01-03-PLAN.md`, `01-04-PLAN.md`).

### Behavioral Spot-Checks

| Behavior | Command | Result | Status |
|----------|---------|--------|--------|
| Mandated test 3 (line normalisation) passes | `dotnet test tests/BlazorCanvas.Tests --filter FullyQualifiedName~Line_UpAndRightDiagonal_IsNotFlippedToOppositeDiagonal --no-build` | 1 passed | ✓ PASS |
| No test covers `ClampMove` identity for an out-of-canvas/oversized box (CR-02 gap) | `dotnet test --filter FullyQualifiedName~ClampMove_ZeroDelta --no-build` | "No test matches the given filter" | ✓ PASS (confirms the gap — absence proven, not assumed) |
| Database rejects a non-square circle, odd-sided circle, zero-area rectangle, zero-length line; accepts a horizontal line | Raw `psql` INSERT/ROLLBACK against `figures` (see Truth 3) | All 4 illegal cases rejected by name; horizontal line accepted | ✓ PASS |
| CR-01 reproduced live: off-canvas circle box `(-10,355,0,365)` passes every guard/CHECK | Raw `psql` INSERT/ROLLBACK against `figures` with that exact box | `INSERT 0 1` — succeeded | ✗ FAIL (confirms the gap) |
| Out-of-canvas rectangle `(-99999,-99999,999999,999999)` is accepted (WR-01, intentional per D-36 — no bounds CHECK exists by design) | Raw `psql` INSERT/ROLLBACK | `INSERT 0 1` — succeeded | ℹ️ INFO (expected per locked D-36; not itself a gap, but the context in which Truth 5's gap matters) |

### Anti-Patterns Found

| File | Line | Pattern | Severity | Impact |
|------|------|---------|----------|--------|
| `src/BlazorCanvas/Geometry/CircleEncoding.cs` | 27-36 | Missing lower-bound clamp (CR-01) | 🛑 Blocker | Off-canvas circle passes every guard and CHECK — see Gaps |
| `src/BlazorCanvas/Geometry/Movement.cs` | 10 | `lo > hi` inversion (CR-02) | 🛑 Blocker | Zero-delta move can teleport an out-of-bounds figure — see Gaps |
| `src/BlazorCanvas/Data/CanvasDbContextFactory.cs` | 25 | Hardcoded wrong-server fallback (CR-03) | 🛑 Blocker | `dotnet ef` from repo root can silently DDL the wrong PostgreSQL server — see Gaps |

No `TBD`/`FIXME`/`XXX`/`TODO`/`HACK`/`PLACEHOLDER` markers found in any file touched by this phase (`src/BlazorCanvas/Geometry`, `src/BlazorCanvas/Data`, `src/BlazorCanvas/Program.cs`, `docker-compose.yml`, `tests/BlazorCanvas.Tests/{Geometry,Database}`).

The 9 Warnings and 5 Info items from `01-REVIEW.md` (WR-01 through WR-09, IN-01 through IN-05) are not repeated here individually — they are lower severity than the three Critical items above, are not required by any PLAN.md must-have, and do not block the phase goal on their own. They remain open follow-up work.

### Human Verification Required

None. Every item above was verifiable programmatically (source inspection, live database queries, and test enumeration/execution) — no visual, real-time, or subjective judgment was needed to resolve these findings.

### Gaps Summary

Three of the four ROADMAP success criteria (1, 2, 3) hold cleanly and were independently re-verified against the live database and repository, not merely trusted from SUMMARY.md. Success criterion 4 ("the three mandated tests pass") is also literally true — the tests exist, are correctly named, and pass.

However, the phase's own framing of its purpose — *"the three silent failure modes are guarded before any UI exists to hide them"* — is not fully achieved. Two independently-confirmed, unresolved **Critical** defects (`01-REVIEW.md` CR-01 and CR-02, both re-verified here by direct code reading and, for CR-01, live database reproduction) sit in the exact clamp/circle-encoding functions the mandated tests were meant to prove sound:

- **CR-01** — `CircleEncoding.ClampDrawRadius` has no floor at zero. A centre outside the canvas (reachable the moment BC-02 wires up raw pointer drag input) produces a negative radius, which `Normalisation` silently turns into a *valid-looking* off-canvas circle that both `MinSizeGuard` and `circle_is_a_circle` accept. Reproduced live: `INSERT INTO figures (..., 'circle', -10, 355, 0, 365)` succeeds.
- **CR-02** — `Movement.ClampDelta` inverts when `lo > hi` (an out-of-canvas or oversized box). `ClampMove(box, 0, 0)` is therefore not the identity, meaning a zero-delta drag interaction can silently teleport a figure. No existing test covers this input region — confirmed by filtering the test suite for a matching name and finding none.

A third Critical defect, **CR-03**, does not affect the running app (which reads the correct port-5433 connection string) but means `dotnet ef` tooling run from the repository root will silently target the wrong PostgreSQL server on this exact machine (the native `postgresql-x64-18` service on port 5432) — a real, not hypothetical, risk given that exact port conflict is what forced the documented D-27 deviation in the first place.

None of these three defects are required by any PLAN.md `must_haves` entry, and none cause a currently-existing test to fail — which is precisely why they are dangerous: they are new instances of the same *silent* failure category (D-50's framing) that this phase exists to eliminate, hiding in the 8-hand-picked-box `GuardMirrorsChecksTests` matrix's blind spot (no negative or out-of-canvas coordinates), not caught by anything.

**Recommendation:** These are genuine, reproducible defects with known fixes already drafted in `01-REVIEW.md`. Given the phase's central claim is specifically about eliminating silent geometric failure modes, closing CR-01/CR-02 (and ideally CR-03) before Phase BC-02 starts consuming this geometry core for live pointer input is strongly advised — BC-02's drag handler is exactly the code path that will first produce an off-canvas centre. If the team decides these can be deferred (e.g., tracked as BC-02 prerequisites rather than BC-01 blockers), that is a legitimate call — but it should be a conscious, recorded decision (an override or an explicit ROADMAP note), not a silent pass.

---

_Verified: 2026-07-15_
_Verifier: Claude (gsd-verifier)_
