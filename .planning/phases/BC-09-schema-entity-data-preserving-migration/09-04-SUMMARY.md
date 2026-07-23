---
phase: 09-schema-entity-data-preserving-migration
plan: 04
subsystem: database
tags: [ef-core, migration, postgres, jsonb, schema]
requires:
  - phase: 09-03
    provides: Anchor+geometry EF model
provides:
  - AnchorGeometryRewrite EF migration
  - Live catalog verification script
affects: [BC-09, database-tests, migration-round-trip]
tech-stack:
  added: []
  patterns:
    - EF migration with hand-written data backfill before destructive column/id swap
key-files:
  created:
    - src/BlazorCanvas/Migrations/20260723180111_AnchorGeometryRewrite.cs
    - src/BlazorCanvas/Migrations/20260723180111_AnchorGeometryRewrite.Designer.cs
    - scripts/verify-live-schema.sh
  modified:
    - src/BlazorCanvas/Migrations/CanvasDbContextModelSnapshot.cs
key-decisions:
  - "Down() fails loudly because the uuid id rewrite is an irreversible data migration."
  - "Live verification used an isolated canvas_phase09 database because the existing canvas database already contained a different future multi-canvas schema."
patterns-established:
  - "Backfill SQL mirrors GeometryCodec formulas and assigns z from the old integer id before id is dropped."
requirements-completed: [STOR-01, MIG-01]
coverage:
  - id: D1
    description: "AnchorGeometryRewrite compiles and backfills x/y/geometry/z before dropping old bbox/id columns."
    requirement: MIG-01
    verification:
      - kind: other
        ref: "dotnet build src/BlazorCanvas/BlazorCanvas.csproj"
        status: pass
    human_judgment: false
  - id: D2
    description: "Live Postgres catalog matches the D-59 figures schema after applying the migration."
    requirement: STOR-01
    verification:
      - kind: integration
        ref: "CANVAS_DB=canvas_phase09 bash scripts/verify-live-schema.sh (executed inside canvas-postgres with port 5432)"
        status: pass
    human_judgment: false
duration: 22 min
completed: 2026-07-23
status: complete
---

# Phase 09 Plan 04: Migration Summary

**The real EF migration rewrites figures to uuid anchor+geometry storage and verifies the live Postgres catalog.**

## Performance

- **Duration:** 22 min
- **Started:** 2026-07-23T19:10:00Z
- **Completed:** 2026-07-23T19:32:00Z
- **Tasks:** 3
- **Files modified:** 4

## Accomplishments

- Generated and hand-corrected `AnchorGeometryRewrite` so it adds nullable target columns, backfills from old `x1,y1,x2,y2,id`, sets target columns not-null, then drops old columns and swaps `id` to uuid.
- Backfill SQL mirrors `GeometryCodec`: rectangle/triangle `{w,h}`, circle centre anchor `{r}`, line signed `{dx,dy}`, and `z = old id`.
- Added `scripts/verify-live-schema.sh` and verified a migrated Compose Postgres database has uuid `id`, integer `x/y`, jsonb `geometry`, numeric `z`, only `figures_type_is_known`, `(user_id,z)` index, no `created_at`, and no geometry CHECK.

## Task Commits

1. **Task 1-2: Generate migration and hand-write backfill** - `2560271` (feat)
2. **Verification fix: tolerate existing index/check naming variants** - `c5d0b33` (fix)
3. **Task 3: Live schema verification script** - `256766a` (test)

**Plan metadata:** committed separately by GSD close-out.

## Files Created/Modified

- `src/BlazorCanvas/Migrations/20260723180111_AnchorGeometryRewrite.cs` - Schema delta, backfill, id swap, irreversible Down.
- `src/BlazorCanvas/Migrations/20260723180111_AnchorGeometryRewrite.Designer.cs` - EF target model.
- `src/BlazorCanvas/Migrations/CanvasDbContextModelSnapshot.cs` - Anchor+geometry model snapshot.
- `scripts/verify-live-schema.sh` - Live catalog assertion script.

## Decisions Made

- Used `DROP ... IF EXISTS` for old index/check cleanup because the local live database had drifted from the expected v1.1 baseline naming/state.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] Existing live database was not the expected v1.1 baseline**
- **Found during:** Task 3 (apply migration to live Postgres)
- **Issue:** The `canvas` database already had a different future multi-canvas schema, so applying this migration there would require destructive reset or manual downgrade.
- **Fix:** Created an isolated `canvas_phase09` database on the same Compose Postgres server, applied the real EF migrations there, and ran the live catalog verification against it.
- **Files modified:** `scripts/verify-live-schema.sh`
- **Verification:** `D-59 schema verified on localhost:5432/canvas_phase09...`
- **Committed in:** `256766a`

**2. [Rule 3 - Blocking] Host bash is unavailable**
- **Found during:** Task 3 (run verification script)
- **Issue:** `bash` resolves to a broken WSL installation and Git Bash is not installed.
- **Fix:** Copied and ran the same script inside the Postgres container, where `bash` and `psql` are available.
- **Files modified:** None
- **Verification:** Script exited 0 inside `canvas-postgres`.
- **Committed in:** N/A

---

**Total deviations:** 2 auto-fixed (2 blocking). **Impact on plan:** The target schema and migration proof are intact; the shared `canvas` database was left untouched to avoid data loss.

## Issues Encountered

- Initial live update failed on missing old index, then on an existing `x` column, revealing the `canvas` database was not the v1.1 baseline.

## User Setup Required

None - Compose Postgres was already running. For host execution of `scripts/verify-live-schema.sh`, install a working Bash or run it inside the Postgres container as done here.

## Next Phase Readiness

Ready for 09-05 to adapt the test suite to the new schema and for 09-06 to run the fixture round-trip proof.

---
*Phase: 09-schema-entity-data-preserving-migration*
*Completed: 2026-07-23*
