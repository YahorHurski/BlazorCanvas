---
phase: 09-schema-entity-data-preserving-migration
status: passed
verified: 2026-07-23
requirements: [STOR-01, MIG-01, MIG-02]
---

# Phase 09 Verification

## Verdict

PASSED - Phase 9 achieved its goal: the production model and EF migration now use D-59 anchor
`x,y` + `geometry jsonb` storage, and the immutable v1.1 fixture round-trip proves existing
figures migrate without row loss or geometry/z drift.

## Requirement Traceability

| Requirement | Status | Evidence |
| --- | --- | --- |
| STOR-01 | Passed | `scripts/verify-live-schema.sh` verified uuid `id`, integer `x/y`, jsonb `geometry`, numeric `z`, type whitelist CHECK only, `(user_id,z)` index, no `created_at`, and no geometry CHECK against migrated Postgres database `canvas_phase09`. `FigureStore.LoadAsync` orders by `Z` then `Id`. |
| MIG-01 | Passed | `20260723180111_AnchorGeometryRewrite` adds target columns, backfills from old bbox/id before dropping them, assigns `z = old id`, and fails loudly on Down. |
| MIG-02 | Passed | `MigrationRoundTripTests` seeds the immutable SQL fixture into a disposable Postgres DB, runs the real EF migration, and compares all 795 migrated rows to the D-59 manifest. |

## Success Criteria

1. Live `figures` table matches THE SCHEMA: Passed.
   Evidence: `CANVAS_DB=canvas_phase09 scripts/verify-live-schema.sh` exited 0 when run inside `canvas-postgres`.

2. Figures load in `z, id` order: Passed.
   Evidence: `FigureStore.LoadAsync` uses `.OrderBy(f => f.Z).ThenBy(f => f.Id)` and `FigureStoreTests.LoadAsync_ReturnsFiguresInCreationOrder` asserts appended z order.

3. EF migration preserves pre-existing figures: Passed.
   Evidence: `MigrationRoundTripTests.AnchorGeometryRewrite_PreservesEveryFixtureFigure`.

4. Automated round-trip test compares migrated rows to manifest for all four types: Passed.
   Evidence: `dotnet test tests/BlazorCanvas.Tests/BlazorCanvas.Tests.csproj --filter FullyQualifiedName~MigrationRoundTrip` passed 1/1.

## Automated Checks

- `dotnet build BlazorCanvas.sln` - passed, 0 warnings, 0 errors.
- `dotnet test tests/BlazorCanvas.Tests/BlazorCanvas.Tests.csproj --filter FullyQualifiedName~MigrationRoundTrip` - passed, 1/1.
- `BLAZORCANVAS_TEST_CONNECTION=Host=localhost;Port=5433;Database=canvas_phase09;Username=postgres;Password=postgres dotnet test tests/BlazorCanvas.Tests/BlazorCanvas.Tests.csproj` - passed, 374/374.
- `scripts/verify-live-schema.sh` - passed inside `canvas-postgres` with `CANVAS_DB=canvas_phase09 CANVAS_DB_PORT=5432`.

## Notes

- The default Compose `canvas` database was not reset because it already contained an unrelated future multi-canvas schema. Verification used isolated database `canvas_phase09` on the same Compose Postgres server to avoid destructive data loss.
- No human verification is required for this phase; all success criteria are covered by automated database/schema/unit tests.
