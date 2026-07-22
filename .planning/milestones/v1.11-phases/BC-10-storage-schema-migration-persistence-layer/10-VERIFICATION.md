---
phase: BC-10-storage-schema-migration-persistence-layer
verified: 2026-07-22T12:35:03Z
status: passed
score: 6/6 must-haves verified
behavior_unverified: 0
overrides_applied: 0
re_verification:
  previous_status: gaps_found
  previous_score: 5/6
  gaps_closed:
    - "V11DataMigration.RunAsync applies schema, type seeding, canvases, and figures in one transaction so a failure leaves the database exactly as it was found."
  gaps_remaining: []
  regressions: []
---

# Phase BC-10: Storage Schema, Migration & Persistence Layer Verification Report

**Phase Goal:** The database and a new persistence layer fully implement the four-table schema and the position/shape split, and every existing figure is proven to migrate losslessly — all additive at the data layer, before any application code is touched.

**Verified:** 2026-07-22T12:35:03Z  
**Status:** passed  
**Re-verification:** Yes — after 10-06 migration-atomicity gap closure

## Goal Achievement

### Observable Truths

| # | Truth | Status | Evidence |
| --- | --- | --- | --- |
| 1 | Isolated four-table v11 storage supports data-driven types. | ✓ VERIFIED | `V11Schema.Ddl` creates `v11.canvases`, `v11.figures`, and `v11.figure_types`; the prior catalog coverage remains green in the 1,293-test solution run. |
| 2 | Repository separates position from shape, creates UUIDs before inserts, and safely retries `z` collisions. | ✓ VERIFIED | `FigureRepository` has x/y-only `MoveSql`, application-generated IDs, `MAX(z)+1`, and a bounded retry scoped to `z_unique_per_canvas`; regression suite passed. |
| 3 | The committed v1.1 fixture migrates losslessly with one canvas per user, preserved geometry, stacking order, style, and bbox cache. | ✓ VERIFIED | `V11MigrationReplayTests` passed 27/27, including 708 users, 795 figures, figureless users, rendered vertices, old-id `z` ordering, JSONB style, bbox recomputation, and a repeat-run idempotency check. |
| 4 | Migration failure leaves the database exactly as it was found. | ✓ VERIFIED | `RunAsync` begins its only transaction before DDL/seeding; all schema, seed, read, canvas, and repository commands receive it. `InvalidLegacyFigure_AbortsAndRollsBackAllData` passes and asserts no `v11` namespace/table plus exact legacy-row retention. |
| 5 | Fixed style and local bbox cache are written through the validated persistence boundary. | ✓ VERIFIED | Repository accepts `ValidatedFigureInput` and binds its four local `Bounds` values to `bbox_*`; standing cache and hostile-input coverage remains green. |
| 6 | New regression guards catch cache drift, hostile input, and z collisions. | ✓ VERIFIED | The full suite passed 1,293/1,293, including `BboxCacheAgreementTests`, `HostileInputRejectionTests`, and `ZCollisionRetryTests`. |

**Score:** 6/6 truths verified (0 present, behavior-unverified).

### Required Artifacts

| Artifact | Expected | Status | Details |
| --- | --- | --- | --- |
| `src/BlazorCanvas/Data/V11/V11Schema.cs` | Additive schema plus transaction-aware schema/type seeding | ✓ VERIFIED | DDL and each parameterized seed command construct `NpgsqlCommand` with the optional caller transaction; standalone callers retain no-transaction overload behavior. |
| `src/BlazorCanvas/Data/V11/V11DataMigration.cs` | Atomic, lossless migration | ✓ VERIFIED | Starts one `BeginTransactionAsync` before `ApplyAsync`/`SeedFigureTypesAsync`, propagates it to reads, canvas insert, repository insertion, and commits only after report integrity validation. |
| `tests/BlazorCanvas.Tests/Database/V11/V11MigrationReplayTests.cs` | Guarded replay and rollback proof | ✓ VERIFIED | Invalid-row test uses the guarded fresh scratch fixture, validates exception redaction and public-row retention, then asserts `to_regnamespace('v11')` and `to_regclass('v11.figure_types')` are null. |
| `src/BlazorCanvas/Data/V11/FigureRepository.cs` | Transaction-aware migration write path | ✓ VERIFIED | The internal migration overload constructs its insert command with the supplied connection and transaction, so no second transaction or bbox-writing SQL path is introduced. |

### Key Link Verification

| From | To | Via | Status | Details |
| --- | --- | --- | --- | --- |
| `V11DataMigration.RunAsync` | `V11Schema.ApplyAsync` / `SeedFigureTypesAsync` | Same transaction passed after opening it at line 60 | ✓ WIRED | Schema DDL and all type seed inserts share the migration rollback boundary. |
| `V11DataMigration.RunAsync` | legacy reads, canvas inserts, repository migration insert | Same `NpgsqlTransaction` argument | ✓ WIRED | Read commands, canvas insert command, and `InsertWithIdAndZAsync(connection, transaction, ...)` all use the exact transaction instance. |
| Invalid-legacy replay test | PostgreSQL catalog | `to_regnamespace` / `to_regclass` after expected throw | ✓ WIRED | A legacy-only scratch database proves rollback removed the v11 namespace; absence of the namespace also precludes type-table/seed residue. |
| v11 data layer | running application | No Phase-10 cutover | ✓ VERIFIED | The phase remains additive: migration reads `public` tables only; Phase 11 owns destructive cutover. |

### Behavioral Spot-Checks

| Behavior | Command | Result | Status |
| --- | --- | --- | --- |
| Migration replay, invalid-row rollback, and idempotency | `dotnet test BlazorCanvas.sln --no-restore --nologo -v minimal --filter "FullyQualifiedName~BlazorCanvas.Tests.Database.V11.V11MigrationReplayTests"` | 27 passed, 0 failed | ✓ PASS |
| Full regression | `dotnet test BlazorCanvas.sln --no-restore --nologo -v minimal` | 1,293 passed, 0 failed | ✓ PASS |

### Probe Execution

No phase-declared or conventional `probe-*.sh` probes exist for this phase.

### Requirements Coverage

| Requirement | Status | Evidence |
| --- | --- | --- |
| MODEL-01 | ✓ VERIFIED | Position/shape columns are separated and `MoveSql` writes only `x` and `y`. |
| MODEL-02 | ✓ VERIFIED | Additive `v11` schema supplies canvases, figures, and figure types alongside unchanged `public.users`; replay proves one 1472×828 canvas per user. |
| MODEL-03 | ✓ VERIFIED | `figures.type` references seeded `figure_types`; schema tests cover data-driven type addition. |
| MODEL-04 | ✓ VERIFIED | Schema uses `numeric` coordinates and repository/migration use application-generated UUIDs. |
| MODEL-05 | ✓ VERIFIED | `z_unique_per_canvas`, `MAX(z)+1`, and targeted retry are covered by collision tests. |
| MODEL-06 | ✓ VERIFIED | Replay tests assert the fixed JSONB style on every migrated row. |
| MODEL-07 | ✓ VERIFIED | `ValidatedFigureInput.Bounds` is the cache-write boundary; agreement tests recompute every stored row. |
| MIGR-01 | ✓ VERIFIED | Invalid legacy input now rolls back transactional DDL, seed rows, canvases, and figures; successful migration preserves the existing picture. |
| MIGR-02 | ✓ VERIFIED | Replay test asserts 708 canvases, 795 figures, 173 figureless-user canvases, default dimensions, and ownership. |
| MIGR-03 | ✓ VERIFIED | Hash-guarded v1.1 fixture replay validates rendered vertices and stacking order. |
| TEST-03 | ✓ VERIFIED | Cache-agreement, hostile-input, and z-collision guards are present and pass in the full suite. |

### Anti-Patterns Found

None. The previous blocker (DDL/type seeding outside the migration transaction) is closed. No Phase-11 cutover or legacy-table mutation was introduced.

### Gaps Summary

None. The re-verification confirms a failure before commit leaves a legacy-only database unchanged: no `v11` namespace, v11 relations, or seeded type rows remain, while the invalid legacy row and its user remain intact. Successful replay and second-run idempotency still pass.

---

_Verified: 2026-07-22T12:35:03Z_  
_Verifier: generic-agent workaround acting as gsd-verifier_
