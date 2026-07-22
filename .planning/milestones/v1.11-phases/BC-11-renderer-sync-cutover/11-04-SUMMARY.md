---
phase: BC-11-renderer-sync-cutover
plan: 04
subsystem: v11-cutover-verification
tags: [postgresql, npgsql, transaction, migration, xunit]
requires:
  - phase: BC-11-renderer-sync-cutover
    plan: 03
    provides: guarded public-schema v1.11 cutover
provides:
  - Disposable catalog-level proof of v1.11 cutover states
  - Test-only transaction-local failure probes for each destructive cutover boundary
  - Byte-stable catalog/data rollback and retry verification
affects: [BC-11 plan 05, phase verification]
requirements-completed: [SYNC-02, SYNC-03]
completed: 2026-07-22
status: complete
---

# Phase 11 Plan 04: Transactional Cutover Proof Summary

The v1.11 cutover now has executable, isolated PostgreSQL proof for supported starting catalogs and injected transaction failures.

## Accomplishments

- Added a test-visible, internal cutover-stage probe. Production `EnsureAsync` always passes no probe and follows the unchanged runtime path.
- Added a guarded scratch-database harness that creates random catalogs through the maintenance database, snapshots public/v11 table definitions and rows, and terminates connections only to its own generated database during disposal.
- Added coverage for legacy-only migration, additive reruns, fresh users-only setup, completed-public no-op behavior, and invalid partial catalogs.
- Added rollback/retry proof for exceptions after schema application, type seeding, legacy migration, dropping legacy figures, and the first public-table promotion.

## Verification

- `dotnet test BlazorCanvas.sln --nologo --filter "FullyQualifiedName~V11CutoverTests"` — 12 passed.
- `dotnet build BlazorCanvas.sln --nologo -v q` — passed with 0 warnings and 0 errors.
- `dotnet test BlazorCanvas.sln --nologo --no-build` — 286 passed, 0 failed, 0 skipped.

## Deviations

Fresh users-only cutover correctly creates final storage and seeds types but does not create a canvas. Canvases are intentionally owner-lazy through `CanvasRepository`; the test asserts this rather than introducing a duplicate creation path.

## Next Phase Readiness

Plan 05 can use the final-public scratch-catalog basis for persisted multi-circuit sync and reload-convergence proof.
