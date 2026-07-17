---
phase: BC-01-database-schema-geometry-core
verified: 2026-07-17T00:00:00Z
status: passed
score: 6/6 must-haves verified
behavior_unverified: 0
overrides_applied: 0
re_verification:
  previous_status: gaps_found
  previous_score: 4/6
  gaps_closed:
    - "The clamp and circle-draw maths robustly keep every figure inside the canvas (D-24/D-29/D-36) — CR-01 (CircleEncoding.ClampDrawRadius negative radius) and CR-02 (Movement.ClampDelta lo>hi inversion)"
    - "dotnet ef design-time tooling reliably targets this project's own PostgreSQL 17 container, never the unrelated native PostgreSQL 18 service on port 5432 — CR-03 (CanvasDbContextFactory hardcoded fallback)"
  gaps_remaining: []
  regressions: []
---

# Phase BC-01: Database, Schema & Geometry Core Verification Report

**Phase Goal:** A running PostgreSQL holds a schema that *machine-enforces* the geometry laws, and the pure geometry maths those laws mirror is written and proven. The three silent failure modes are guarded before any UI exists to hide them.

**Verified:** 2026-07-17
**Status:** passed
**Re-verification:** Yes — this supersedes the 2026-07-15 initial report (`status: gaps_found`, 4/6), which recorded three Critical defects (CR-01, CR-02, CR-03) in `01-REVIEW.md`. Two gap-closure plans (`01-05` for CR-01/CR-02, `01-06` for CR-03) subsequently ran; this report independently re-verifies their claims against the current codebase and a live database, rather than trusting `01-05-SUMMARY.md` / `01-06-SUMMARY.md`.

## Goal Achievement

### Observable Truths

| # | Truth | Status | Evidence |
|---|-------|--------|----------|
| 1 | `docker compose up` starts PostgreSQL 17, database `canvas`, named volume; rows survive a container restart (ROADMAP SC1) | ✓ VERIFIED | `docker exec canvas-postgres psql -c "select version()"` → `PostgreSQL 17.10`. `docker ps` shows `canvas-postgres` `Up ... (healthy)` on `0.0.0.0:5433->5432`. `docker volume ls` → `project1_canvas-pgdata` (named). Unchanged since the initial report; re-confirmed live, not assumed. Port 5433 (not 5432) remains a user-approved deviation from D-27 — documentation drift in `docs/DECISIONS.md`, not a functional gap (unchanged open item, not re-litigated). |
| 2 | Exactly two tables (`users`, `figures`) via EF Core migrations, carrying the CHECK constraints, the `user_id` index, and the circle-convention `COMMENT ON TABLE` (ROADMAP SC2) | ✓ VERIFIED | Live query: `select table_name from information_schema.tables where table_schema='public'` → exactly `__EFMigrationsHistory`, `figures`, `users`. `pg_constraint` on `figures` → exactly `box_is_a_box`, `circle_is_a_circle`, `figures_type_is_known`, `line_is_a_line` (plus PK/FK) — identical to the initial report; no schema drift from the CR-01/CR-02/CR-03 gap-closure plans (both plans' scope fences explicitly forbade schema changes, and this was independently confirmed, not just trusted). |
| 3 | The database refuses an illegal row — non-square/odd-sided circle, zero-area rectangle, zero-length line — rejected by a CHECK constraint (ROADMAP SC3) | ✓ VERIFIED | Live, raw-SQL re-reproduction (rolled back): `('circle',0,0,10,8)` → `ERROR: violates check constraint "circle_is_a_circle"`. `('rectangle',10,10,90,10)` (zero height) → `ERROR: violates check constraint "box_is_a_box"`. `('line',10,10,10,10)` (zero length) → `ERROR: violates check constraint "line_is_a_line"`. Identical behaviour to the initial report — no regression. |
| 4 | The three mandated TEST-01 tests pass: clamp maths, circle inscribed-square round-trip, line normalisation (ROADMAP SC4) | ✓ VERIFIED | `dotnet test --filter "FullyQualifiedName~BlazorCanvas.Tests.Geometry\|FullyQualifiedName~BlazorCanvas.Tests.Database"` → 372/372 passed. Full solution `dotnet test BlazorCanvas.sln` → 405/405 passed, 0 failures (the total grew from 153 to 405 because Phases BC-02 through BC-05 have since executed and added their own tests — not a BC-01 regression signal; the BC-01-scoped subset was isolated and re-run above). |
| 5 | The clamp/circle-draw maths robustly keeps every figure inside the canvas — CR-01 and CR-02 | ✓ VERIFIED (closed) | **Re-read `src/BlazorCanvas/Geometry/CircleEncoding.cs` directly (current state, not the SUMMARY's description):** `ClampDrawRadius` now clamps the centre via `Movement.ClampDelta(cx, 0, CanvasBounds.Width)` / `(cy, 0, CanvasBounds.Height)` *before* computing the four edge-distance terms, and wraps the final cap in `Math.Max(0, capped)` — confirmed by direct inspection, lines 32-45. **Re-read `src/BlazorCanvas/Geometry/Movement.cs`:** `ClampDelta` is now `lo > hi ? 0 : Math.Min(Math.Max(v, lo), hi)` — confirmed by direct inspection, line 16. **Behavioural re-proof (not assumed from the plan's acceptance criteria):** ran the single named test `ClampDrawRadius_OffCanvasCentre_ProducesNoLegalCircle` in isolation — 1 passed — which asserts exactly `ClampDrawRadius(-5, 360, 50) == 0` (was -5) and that the resulting normalised box is rejected by `MinSizeGuard.IsDrawable`. All 8 new regression tests (5 named, 4 of which are theory cases) enumerated via `--list-tests` and confirmed present. Full BC-01-scoped suite green. |
| 6 | `dotnet ef` design-time tooling cannot silently target the wrong PostgreSQL server on this machine (CR-03) | ✓ VERIFIED (closed) | **Re-read `src/BlazorCanvas/Data/CanvasDbContextFactory.cs` directly:** the hardcoded `Host=localhost;Port=5432;...` fallback and the `Username=postgres;Password=postgres` literal are both gone; `configuration.GetConnectionString("Canvas") ?? throw new InvalidOperationException(...)` is now in place (confirmed by inspection, lines 28-33; `grep -c 'Port=5432'` / `grep -c 'Username=postgres'` → 0 matches in this file). `.AddEnvironmentVariables()` is present in the `ConfigurationBuilder` chain (line 22). **Live behavioural reproduction (executed independently, not taken from the SUMMARY):** temporarily moved `src/BlazorCanvas/appsettings.Development.json` aside, ran `dotnet ef migrations list` from `src/BlazorCanvas/` — it failed with exactly the actionable message `"ConnectionStrings:Canvas is not configured. Run \`dotnet ef\` from src/BlazorCanvas/ ... Refusing to guess a connection string: port 5432 on this machine is a DIFFERENT PostgreSQL server..."` — then restored the file; `git status`/`git diff` on that file confirmed byte-for-byte clean afterward. Ran `dotnet ef migrations list` again with the file restored — exit 0, lists `20260714212457_InitialSchema` against the correct port-5433 container. |

**Score:** 6/6 truths verified (0 present, behavior-unverified)

### Required Artifacts

| Artifact | Expected | Status | Details |
|----------|----------|--------|---------|
| `docker-compose.yml` | PostgreSQL 17, named volume, `canvas` db | ✓ VERIFIED | Unchanged since initial report; port `5433:5432` per approved deviation. Gap-closure plans did not touch this file (confirmed: neither `01-05` nor `01-06`'s `files_modified` list it, and it is untouched in the diffs read). |
| `src/BlazorCanvas/Geometry/CircleEncoding.cs` | Robust circle draw-clamp, no negative radius | ✓ VERIFIED | Read in full; centre-clamp + zero-floor present and correct (Truth 5). Zero non-`System` usings (purity check: `grep -h '^using' src/BlazorCanvas/Geometry/*.cs \| grep -v '^using System' \| wc -l` → 0). |
| `src/BlazorCanvas/Geometry/Movement.cs` | Robust `ClampDelta`, no `lo > hi` inversion | ✓ VERIFIED | Read in full; `lo > hi` guard present and correct (Truth 5). |
| `src/BlazorCanvas/Data/CanvasDbContextFactory.cs` | Fails loudly, no wrong-server fallback | ✓ VERIFIED | Read in full; throw present, fallback and credential literal both removed, `.AddEnvironmentVariables()` added (Truth 6). |
| `tests/BlazorCanvas.Tests/Geometry/ClampTests.cs` | 3 new CR-02 regression tests | ✓ VERIFIED | `ClampMove_OversizedWidthBox_ZeroDelta_IsIdentity`, `ClampMove_OversizedHeightBox_ZeroDelta_IsIdentity`, `ClampDelta_WhenLoGreaterThanHi_ReturnsZero` all enumerated via `--list-tests` and pass. |
| `tests/BlazorCanvas.Tests/Geometry/CircleEncodingTests.cs` | 2 new CR-01 regression tests (one a 4-case theory) | ✓ VERIFIED | `ClampDrawRadius_OffCanvasCentre_IsNeverNegative` (4 theory cases: `(-5,360)`, `(1285,360)`, `(640,-5)`, `(640,725)`) and `ClampDrawRadius_OffCanvasCentre_ProducesNoLegalCircle` all enumerated and pass. |
| `src/BlazorCanvas/Program.cs` | Untouched by either gap-closure plan | ✓ VERIFIED | Neither `01-05` nor `01-06`'s `files_modified` lists it; commit diffs (`93e485e`, `481ed64`, `ae9d772`) confirm only the claimed files changed. |

### Key Link Verification

| From | To | Via | Status | Details |
|------|----|----|--------|---------|
| `CircleEncoding.ClampDrawRadius` (centre clamp) | `Movement.ClampDelta` | direct call, same assembly | ✓ WIRED | Confirmed by source read: `ClampDrawRadius` calls `Movement.ClampDelta(cx, 0, CanvasBounds.Width)` and `(cy, 0, CanvasBounds.Height)` before computing the cap — reuses the hardened primitive rather than duplicating clamp logic. |
| `CircleEncoding.FromCentreRadius`/`Normalisation.Normalise` | `MinSizeGuard.IsDrawable`/`circle_is_a_circle` | round-trip validity | ✓ WIRED (closed) | Previously ⚠️ HOLLOW for off-canvas centres (CR-01). Now: an off-canvas centre yields radius 0 → a degenerate box → rejected by `MinSizeGuard.IsDrawable`, proven by the passing `ClampDrawRadius_OffCanvasCentre_ProducesNoLegalCircle` test, re-run in isolation as part of this verification. |
| `Movement.ClampDelta` (lo > hi guard) | `Movement.ClampMove` | oversized box → zero delta | ✓ WIRED (closed) | Previously defeatable (CR-02). Now: `ClampMove(oversizedBox, 0, 0)` is the identity, proven by the two oversized-box regression tests, re-run and confirmed passing. |
| `CanvasDbContextFactory.CreateDbContext` | `dotnet ef` design-time tooling | missing config → throw | ✓ WIRED (closed) | Previously fell back to `Port=5432` (CR-03). Now: confirmed by live reproduction (config removed → throw fires with the exact actionable message → config restored → command succeeds against port 5433). |
| `MinSizeGuard.IsDrawable` | the 3 named CHECK constraints | D-50 mirror | ✓ WIRED | Unchanged and re-confirmed live; predicates still match character-for-character. |

### Requirements Coverage

| Requirement | Source Plan | Description | Status | Evidence |
|-------------|-------------|-------------|--------|----------|
| DATA-02 | 01-01, 01-03, 01-06 | Schema via EF Core migrations at startup, incl. CHECKs/comment; two tables only; design-time tooling cannot silently target the wrong server | ✓ SATISFIED | Schema, migrations, and persistence proven live (Truths 1-3). CR-03 closure confirmed live (Truth 6). Runtime write-policy (INSERT/UPDATE/DELETE, no Save button) remains correctly deferred to later phases per `01-CONTEXT.md`. |
| TEST-01 | 01-02, 01-04, 01-05 | Three mandated silent-failure-mode tests, hardened against the input region the original suite never probed | ✓ SATISFIED | The three mandated tests still exist and pass (Truth 4). The two Critical gaps in the exact functions TEST-01 exists to validate (CR-01, CR-02) are now closed and proven by named regression tests that were RED on the pre-fix code (Truth 5) — the phase's own stated purpose ("the three silent failure modes are guarded before any UI exists to hide them") is now fully, not narrowly, satisfied. |

`REQUIREMENTS.md`'s traceability table maps only DATA-02 and TEST-01 to Phase 1/BC-01; both are declared across plan frontmatter (`01-01`, `01-02`, `01-03`, `01-04`, `01-05`, `01-06`). No orphaned requirements.

### Behavioral Spot-Checks

| Behavior | Command | Result | Status |
|----------|---------|--------|--------|
| CR-02 closed: `ClampDelta_WhenLoGreaterThanHi_ReturnsZero` passes | `dotnet test --filter FullyQualifiedName~ClampDelta_WhenLoGreaterThanHi_ReturnsZero --no-build` | 1 passed | ✓ PASS |
| CR-02 closed: both oversized-box identity tests pass | `dotnet test --filter FullyQualifiedName~ClampMove_Oversized --no-build` | 2 passed | ✓ PASS |
| CR-01 closed: negative-radius regression test passes | `dotnet test --filter FullyQualifiedName~ClampDrawRadius_OffCanvasCentre --no-build` | 5 passed (1 test + 4 theory cases) | ✓ PASS |
| CR-01 closed: exact live repro from the initial report | `dotnet test --filter FullyQualifiedName~ClampDrawRadius_OffCanvasCentre_ProducesNoLegalCircle --no-build` | 1 passed | ✓ PASS |
| CR-03 closed: missing config throws the actionable message, does not fall back | Moved `appsettings.Development.json` aside; ran `dotnet ef migrations list` from `src/BlazorCanvas/`; restored the file | Threw `"ConnectionStrings:Canvas is not configured. ... Refusing to guess..."`; file restored byte-for-byte (`git diff` clean) | ✓ PASS |
| CR-03 regression check: legitimate path (port 5433) still works | `dotnet ef migrations list` from `src/BlazorCanvas/` with config intact | Exit 0, lists `20260714212457_InitialSchema` | ✓ PASS |
| Database still rejects the three illegal-row classes (SC3 unaffected) | Raw `psql` INSERT/ROLLBACK — non-square circle, zero-height box, zero-length line | All 3 rejected by name (`circle_is_a_circle`, `box_is_a_box`, `line_is_a_line`) | ✓ PASS |
| Full BC-01-scoped suite green (no regressions from the gap-closure plans) | `dotnet test --filter "FullyQualifiedName~BlazorCanvas.Tests.Geometry\|FullyQualifiedName~BlazorCanvas.Tests.Database" --no-build` | 372 passed, 0 failed | ✓ PASS |
| Full solution suite green | `dotnet test BlazorCanvas.sln` | 405 passed, 0 failed, 0 build warnings | ✓ PASS |
| Geometry core purity unaffected by the CircleEncoding→Movement composition | `grep -h '^using' src/BlazorCanvas/Geometry/*.cs \| grep -v '^using System' \| wc -l` | `0` | ✓ PASS |

### Anti-Patterns Found

None in the files touched by the gap-closure plans. `grep -iE "TBD|FIXME|XXX|TODO|HACK|PLACEHOLDER"` against `CircleEncoding.cs`, `Movement.cs`, `CanvasDbContextFactory.cs`, `ClampTests.cs`, `CircleEncodingTests.cs` returns no matches. The three previously-flagged 🛑 Blockers (CR-01, CR-02, CR-03) are resolved and are not re-listed here — see Truths 5-6 for closure evidence.

The 9 Warnings and 5 Info items from `01-REVIEW.md` (WR-01 through WR-09, IN-01 through IN-05) remain open, lower-severity follow-up items — unchanged since the initial report, not re-litigated individually here, and none block the phase goal.

### Human Verification Required

None. Every item above was verifiable programmatically (direct source inspection of the current file state, live database queries against the running `canvas-postgres` container, live `dotnet ef` reproduction with config removed/restored, and isolated test execution) — no visual, real-time, or subjective judgment was needed.

### Gaps Summary

All three Critical defects recorded in the 2026-07-15 initial report are closed, and this closure was independently re-verified against the current codebase and a live database rather than trusted from `01-05-SUMMARY.md` / `01-06-SUMMARY.md`:

- **CR-01 (closed):** `CircleEncoding.ClampDrawRadius` now clamps the press centre into the canvas via `Movement.ClampDelta` before computing the cap, and floors the result at 0. Re-read directly from `src/BlazorCanvas/Geometry/CircleEncoding.cs` (current state). The exact live repro from the initial report — `ClampDrawRadius(-5, 360, 50)` — is now proven to return 0 (not -5) and to be rejected by `MinSizeGuard.IsDrawable`, via the isolated, passing `ClampDrawRadius_OffCanvasCentre_ProducesNoLegalCircle` test.
- **CR-02 (closed):** `Movement.ClampDelta` now returns 0 when `lo > hi`. Re-read directly from `src/BlazorCanvas/Geometry/Movement.cs`. `ClampMove(oversizedBox, 0, 0)` is now the identity, proven by two isolated, passing regression tests covering both the width and height axes.
- **CR-03 (closed):** `CanvasDbContextFactory.CreateDbContext` no longer falls back to a hardcoded `Port=5432` connection string; it throws an actionable `InvalidOperationException`. Re-read directly from `src/BlazorCanvas/Data/CanvasDbContextFactory.cs`, and independently reproduced live: with `appsettings.Development.json` temporarily removed, `dotnet ef migrations list` failed loudly with the exact message rather than silently targeting the native `postgresql-x64-18` service on port 5432; the legitimate port-5433 path still works with the file restored.

All four ROADMAP success criteria (SC1-SC4) continue to hold, re-confirmed live against the database and were not affected by the gap-closure plans' changes (both plans' scope fences forbade schema changes, and no `src/BlazorCanvas/Data` or `src/BlazorCanvas/Migrations` file besides `CanvasDbContextFactory.cs` was touched). The full solution test suite is green (405/405 — grown from 153 because later phases BC-02 through BC-05 have since executed; the BC-01-scoped subset, 372 tests, was isolated and independently confirmed green).

The phase's stated purpose — *"the three silent failure modes are guarded before any UI exists to hide them"* — is now fully achieved, not narrowly. The 9 Warnings / 5 Info items from `01-REVIEW.md` remain open as lower-severity follow-up work, unchanged and not re-litigated here.

**No gaps remain. This phase is verified as `passed`.**

---

_Verified: 2026-07-17_
_Verifier: Claude (gsd-verifier)_
