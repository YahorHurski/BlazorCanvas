---
phase: BC-10-storage-schema-migration-persistence-layer
plan: "05"
subsystem: testing
tags: [postgresql, npgsql, jsonb, validation, bbox, xunit]
requires:
  - phase: BC-10-storage-schema-migration-persistence-layer
    provides: v11 schema, FigureRepository, z-collision retry, and validation gateway
provides:
  - Whole-table bbox-versus-geometry agreement guard with rollback-only fail-first probes
  - Database-boundary proof that hostile geometry creates no rows and hostile styles are stored only sanitised
  - Explicit regression coverage for the database's retained structural checks and D-60 geometry-validation boundary
affects: [BC-11-renderer-sync-cutover, BC-12-regression-verification]
tech-stack:
  added: []
  patterns: [whole-table schema invariants, rollback-only corruption probes, stored-jsonb boundary assertions]
key-files:
  created:
    - tests/BlazorCanvas.Tests/Database/V11/BboxCacheAgreementTests.cs
    - tests/BlazorCanvas.Tests/Database/V11/HostileInputRejectionTests.cs
  modified: []
key-decisions:
  - "The bbox agreement guard deliberately scans the whole v11.figures table with no WHERE or ordering, making every writer responsible for a valid local cache."
  - "Style boundary assertions read JSONB back from PostgreSQL and compare object keys as a set because jsonb does not preserve insertion order."
  - "D-60's deliberate absence of geometry CHECK constraints is pinned alongside the gateway rejection, so a future bypass cannot silently erase that trust boundary."
requirements-completed: [MODEL-07, TEST-03]
coverage:
  - id: D1
    description: Every stored v11 bbox matches a fresh exact BoundsOf recomputation, including zero-extent lines and rows from other tests.
    requirement: MODEL-07
    verification:
      - kind: integration
        ref: tests/BlazorCanvas.Tests/Database/V11/BboxCacheAgreementTests.cs#EveryStoredBboxAgreesExactlyWithAFreshGeometryRecompute
        status: pass
      - kind: integration
        ref: tests/BlazorCanvas.Tests/Database/V11/BboxCacheAgreementTests.cs#AgreementGuardDetectsDeliberatelyCorruptedCache
        status: pass
    human_judgment: false
  - id: D2
    description: Hostile geometry never produces a stored row, hostile styles are sanitised in PostgreSQL, and the repository exposes no raw-text bypass.
    requirement: TEST-03
    verification:
      - kind: integration
        ref: tests/BlazorCanvas.Tests/Database/V11/HostileInputRejectionTests.cs#HostileGeometryNeverBecomesARow
        status: pass
      - kind: integration
        ref: tests/BlazorCanvas.Tests/Database/V11/HostileInputRejectionTests.cs#HostileStyleIsSanitisedInTheValuePostgreSqlActuallyStores
        status: pass
      - kind: integration
        ref: tests/BlazorCanvas.Tests/Database/V11/HostileInputRejectionTests.cs#RepositoryCannotBeReachedWithRawGeometryOrStyleText
        status: pass
    human_judgment: false
duration: 30min
completed: 2026-07-22
status: complete
---

# Phase BC-10 Plan 05: Standing Storage Guards Summary

**The v11 storage model now proves every cached local bbox agrees with fresh geometry and proves hostile client input either creates no row or lands only as sanitised JSONB.**

## Performance

- **Duration:** 30 min
- **Completed:** 2026-07-22T13:58:49+02:00
- **Tasks:** 2/2
- **Files created:** 2
- **Verification:** 6 focused bbox tests, 51 focused hostile-input tests, and 1,293 full-suite tests passed with zero failures.

## Accomplishments

- Added a whole-table bbox guard that reparses every stored `geometry`, recomputes `BoundsOf`, compares all four values exactly, proves its own sensitivity with rolled-back `bbox_w` and `bbox_x` corruption, and protects legal zero-extent lines.
- Added database-boundary hostile-input coverage: rejected geometry leaves a dedicated canvas empty; accepted hostile styles are asserted from the stored JSONB value; canonical geometry drops hostile extras; and reflection prevents a raw-text repository bypass.
- Pinned D-60's deliberately surrendered geometric database validation while retaining proofs for `style_is_object`, `geometry_is_object`, non-null bbox values, and the `bbox_is_positive` constraint.

## TEST-03 Split and Phase Boundaries

- TEST-03's third guard, forced `z` collision recovery, lives in `10-03` as `ZCollisionRetryTests` because it was delivered next to the bounded retry mechanism it proves. These two guards complete the remaining cache and validation portions without duplicating that concurrency proof.
- The bbox guard intentionally scans the entire `v11.figures` table, not merely rows this class creates. Any future test or write path that leaves a stale cache will therefore make it fail; tests that use raw writes must preserve the invariant or roll their work back.
- TEST-02 retirements remain Phase 11 work. The inscribed-square, line-normalisation, and CHECK-mirror tests still guard the live old table while the application remains on it, so removing them before cutover would weaken current coverage.

## Task Commits

1. **Task 1: The bbox-versus-geometry agreement guard, proven fail-first** — `e6d7a37` (test)
2. **Task 2: Hostile geometry and style, refused at the database boundary** — `8b7d997` (test)
3. **Task 1 correction: count rows actually inspected by the whole-table guard** — `7859156` (fix)

## Files Created

- `tests/BlazorCanvas.Tests/Database/V11/BboxCacheAgreementTests.cs` — whole-table exact cache comparison, edge constraints, duplicate independence, movement stability, and rollback-only sensitivity checks.
- `tests/BlazorCanvas.Tests/Database/V11/HostileInputRejectionTests.cs` — hostile corpus persistence boundary, stored JSONB sanitisation, canonical geometry, raw-path reflection, and explicit D-60 structural-boundary tests.

## Decisions Made

- Keep the comparison exact: both the stored value and expected value derive from the same parsed doubles and `BoundsOf`, so a tolerance would conceal a real cache round-trip failure.
- Treat PostgreSQL JSONB object key order as non-semantic; style tests prove exactly the four keys as a set and compare the parsed sanitised style.
- Assert the accepted D-60 gap explicitly: raw negative-radius JSON succeeds only inside a rolled-back transaction while the same payload is rejected by the gateway.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Test reliability] Accounted for PostgreSQL JSONB object key reordering.**

- **Found during:** Task 2 focused verification.
- **Issue:** JSONB returned the four style keys in its internal order, making an insertion-order assertion fail despite the correct stored object.
- **Fix:** Asserted the required four-key set in sorted order while retaining exact parsed style-value assertions.
- **Verification:** 51 focused hostile-input tests and the full solution suite passed.
- **Committed in:** `8b7d997`

**2. [Rule 1 - Transaction isolation] Isolated structural-constraint probes after an expected PostgreSQL failure.**

- **Found during:** Task 2 focused verification.
- **Issue:** PostgreSQL marks a transaction aborted after a failed constraint probe, preventing a second assertion on the same transaction.
- **Fix:** Ran each expected constraint rejection in its own uncommitted transaction.
- **Verification:** Both named constraints report SQLSTATE `23514`; focused and full suites passed.
- **Committed in:** `8b7d997`

**3. [Rule 1 - Proof strength] Counted the rows read by the guard rather than comparing two database counts.**

- **Found during:** Final self-check.
- **Issue:** The non-vacuity assertion must prove the guard's reader visited every table row, not merely that two `count(*)` queries agree.
- **Fix:** Added a table-inspection result that carries the actual reader iteration count and compare it with `SELECT count(*)`.
- **Verification:** Focused bbox suite passed 6/6 after the correction.
- **Committed in:** `7859156`

**Total deviations:** 3 Rule 1 test corrections. No production code, schema, dependency, or Phase 11 cutover scope changed.

## Issues Encountered

None remaining. Existing PostgreSQL infrastructure and project dependencies were sufficient.

## User Setup Required

None.

## Next Phase Readiness

Phase BC-10 is complete. Phase 11 can replace the live persistence path and renderer using the validated `FigureRepository`, call the proven migration before cutover, and retire only the old-model tests after the old table is no longer live.

## Self-Check: PASSED

- Both planned test artifacts and task commits `e6d7a37`, `8b7d997`, and `7859156` exist.
- Focused bbox verification passed 6/6; focused hostile-input verification passed 51/51; the full solution passed 1,293/1,293.
- `dotnet build BlazorCanvas.sln --nologo` completed with zero warnings and zero errors.
- Live PostgreSQL reports zero rows with negative bbox extents.
- No package was installed, no app cutover code was touched, and no unresolved stub marker was introduced.

---
*Phase: BC-10-storage-schema-migration-persistence-layer*
*Completed: 2026-07-22*
