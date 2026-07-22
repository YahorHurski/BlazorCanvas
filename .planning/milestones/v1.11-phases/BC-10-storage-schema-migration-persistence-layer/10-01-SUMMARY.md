---
phase: BC-10-storage-schema-migration-persistence-layer
plan: "01"
subsystem: database
tags: [postgresql, npgsql, schema, jsonb, xunit]
requires:
  - phase: BC-09-shape-registry-validation-gateway
    provides: ShapeRegistry and canonical default shape names
provides:
  - Idempotent isolated v11 PostgreSQL storage schema
  - Registry-backed, conflict-safe figure-type seeding
  - Live catalog assertions for the storage model
affects: [BC-10-02, BC-10-03, BC-10-04, BC-10-05, BC-11]
tech-stack:
  added: []
  patterns: [single DDL definition, schema-qualified PostgreSQL SQL, catalog-driven integration tests]
key-files:
  created:
    - src/BlazorCanvas/Data/V11/V11Schema.cs
    - tests/BlazorCanvas.Tests/Database/V11/V11SchemaShapeTests.cs
  modified:
    - src/BlazorCanvas/Shapes/Bbox.cs
    - tests/BlazorCanvas.Tests/Database/DatabaseFixture.cs
    - tests/BlazorCanvas.Tests/Database/SchemaShapeTests.cs
key-decisions:
  - "Phase 10 keeps the new tables in v11 so public.figures and the running application stay untouched."
  - "bbox_* stores local geometry extent, making a move an x/y-only write."
  - "figure_types is data seeded from ShapeRegistry, with a parameterised ON CONFLICT no-op for concurrent callers."
patterns-established:
  - "New v11 SQL is schema-qualified and never changes search_path."
  - "Storage schema claims are proved from information_schema and pg_catalog against the live database."
requirements-completed: [MODEL-01, MODEL-02, MODEL-03]
coverage:
  - id: D1
    description: Idempotent v11 schema and registry-backed type seeding
    requirement: MODEL-03
    verification:
      - kind: integration
        ref: tests/BlazorCanvas.Tests/Database/V11/V11SchemaShapeTests.cs#ApplyAndSeed_IsIdempotentAndConcurrent
        status: pass
    human_judgment: false
  - id: D2
    description: Position/shape split, decimal boundaries, and local bbox catalog shape
    requirement: MODEL-01
    verification:
      - kind: integration
        ref: tests/BlazorCanvas.Tests/Database/V11/V11SchemaShapeTests.cs#Figures_ColumnCatalogTypesAreExact
        status: pass
    human_judgment: false
  - id: D3
    description: Canvas, figure type, index, constraint, and legacy isolation guarantees
    requirement: MODEL-02
    verification:
      - kind: integration
        ref: dotnet test BlazorCanvas.sln --nologo
        status: pass
    human_judgment: false
duration: 28min
completed: 2026-07-22
status: complete
---

# Phase BC-10 Plan 01: Storage Schema Migration & Persistence Layer Summary

**An additive, idempotent v11 PostgreSQL schema now holds canvases, data-driven figure types, and JSON-backed figures while the public legacy model remains intact.**

## Performance

- **Duration:** 28 min
- **Tasks:** 3/3
- **Files modified:** 5
- **Verification:** 29 v11 catalog assertions, 68 database-suite assertions, and 1,062 full-suite assertions passed.

## Accomplishments

- Added `V11Schema` as the sole v1.11 storage DDL definition, including idempotent application and parameterised, concurrent-safe registry seeding.
- Applied and proved the live `v11` schema from PostgreSQL catalogs: three new tables, four seeded types, decimal coordinates, JSON shape/style, constraints, indexes, and public-schema isolation.
- Updated the database fixture and five legacy catalog queries so existing tests remain explicitly tied to `public` after `v11.figures` exists.

## Exact v11 DDL Applied

```sql
CREATE SCHEMA IF NOT EXISTS v11;

CREATE TABLE IF NOT EXISTS v11.canvases (
    id uuid PRIMARY KEY,
    owner_id integer NOT NULL REFERENCES public.users(id) ON DELETE CASCADE,
    name text NOT NULL DEFAULT 'Canvas',
    width integer NOT NULL DEFAULT 1472,
    height integer NOT NULL DEFAULT 828,
    background text NOT NULL DEFAULT '#FFFFFF',
    created_at timestamptz NOT NULL DEFAULT now()
);
CREATE INDEX IF NOT EXISTS ix_canvases_owner ON v11.canvases (owner_id);

CREATE TABLE IF NOT EXISTS v11.figure_types (
    name text PRIMARY KEY
);

CREATE TABLE IF NOT EXISTS v11.figures (
    id uuid PRIMARY KEY,
    canvas_id uuid NOT NULL REFERENCES v11.canvases(id) ON DELETE CASCADE,
    type text NOT NULL REFERENCES v11.figure_types(name),
    x numeric(12,3) NOT NULL,
    y numeric(12,3) NOT NULL,
    rotation numeric(7,3) NOT NULL DEFAULT 0,
    geometry jsonb NOT NULL,
    style jsonb NOT NULL DEFAULT '{}',
    z numeric NOT NULL,
    bbox_x double precision NOT NULL,
    bbox_y double precision NOT NULL,
    bbox_w double precision NOT NULL,
    bbox_h double precision NOT NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT z_unique_per_canvas UNIQUE (canvas_id, z),
    CONSTRAINT style_is_object CHECK (jsonb_typeof(style) = 'object'),
    CONSTRAINT geometry_is_object CHECK (jsonb_typeof(geometry) = 'object'),
    CONSTRAINT bbox_is_positive CHECK (bbox_w >= 0 AND bbox_h >= 0)
);
CREATE INDEX IF NOT EXISTS ix_figures_canvas_z ON v11.figures (canvas_id, z);
COMMENT ON TABLE v11.figures IS
    'x, y, rotation locate the figure; geometry is local shape data from (0,0); bbox_* is a local, stroke-excluding cache of geometry alone. A move writes x and y only.';
```

`bbox_x/y/w/h` are LOCAL geometry extent, not canvas-absolute coordinates. That keeps the cache a pure function of `geometry`, so moving a figure writes only `x` and `y`; readers derive an absolute extent by adding position and stroke expansion.

## Schema Transition Notes

- This plan deliberately does **not** wire `V11Schema.ApplyAndSeedAsync` into `Program.cs`. Phase 11 must add that startup call.
- Phase 11's schema cutover is: `DROP TABLE public.figures`; `ALTER TABLE v11.<t> SET SCHEMA public` for each new table; then `DROP SCHEMA v11`.

## Legacy Schema Query Edits

`SchemaShapeTests.cs` gained exactly these five SQL precision edits:

1. `Figures_HasExactlyFourNamedCheckConstraints`: `'figures'::regclass` → `'public.figures'::regclass`.
2. `FiguresTable_HasCommentDocumentingTheCircleConvention`: `'figures'::regclass` → `'public.figures'::regclass`.
3. `IndexOnFiguresUserId_Exists`: added `schemaname = 'public'`.
4. `UsersUsername_HasAUniqueConstraint`: joined `pg_namespace` and filtered `n.nspname = 'public'`.
5. `FiguresUserId_ForeignKey_HasCascadeDeleteRule`: added `tc.table_schema = 'public'`, avoiding unordered selection from v11's two FK rows.

## Task Commits

1. **Task 1: V11Schema — the single DDL definition and its idempotent applier** — `6b48548`
2. **Task 2: Extend the database fixture and make existing schema assertions schema-precise** — `9a3e900`
3. **Task 3: Push the schema to the live database and assert its shape from PostgreSQL catalogs** — `6b707e9`

## Files Created/Modified

- `src/BlazorCanvas/Data/V11/V11Schema.cs` — v11 DDL, applier, and type seeder.
- `src/BlazorCanvas/Shapes/Bbox.cs` — corrected local-cache documentation.
- `tests/BlazorCanvas.Tests/Database/DatabaseFixture.cs` — v11 setup, data source, and canvas helper.
- `tests/BlazorCanvas.Tests/Database/SchemaShapeTests.cs` — schema-qualified legacy catalog SQL.
- `tests/BlazorCanvas.Tests/Database/V11/V11SchemaShapeTests.cs` — catalog and transactional behavioural coverage.

## Decisions Made

- The coexistence schema is `v11`; no DDL changes `search_path` or writes to legacy tables.
- `ix_canvases_owner` is intentionally non-unique: multi-canvas remains open, so one canvas per owner is a migration invariant.
- Type equality remains PostgreSQL text/FK equality; the tests prove `Circle` and `circle ` are rejected while `circle` succeeds.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Test assertion] Corrected catalog expectations for PostgreSQL metadata.**

- **Found during:** Task 3
- **Issue:** `information_schema` reports precision 53 for `double precision`; only unconstrained `numeric z` must report NULL precision. The plan's cascade requirement applies to `canvas_id`, whereas `type` deliberately uses the default delete rule.
- **Fix:** Limited precision assertions to numeric columns and asserted exactly one cascading v11 foreign key.
- **Files modified:** `tests/BlazorCanvas.Tests/Database/V11/V11SchemaShapeTests.cs`
- **Verification:** 29/29 v11 assertions passed.
- **Committed in:** `6b707e9`

**Total deviations:** 1 auto-fixed (Rule 1)

## Issues Encountered

None beyond the corrected catalog-test expectations above.

## User Setup Required

None.

## Next Phase Readiness

Plans 10-02 through 10-05 can use the live, additive v11 schema through `V11Schema` and the fixture's `NpgsqlDataSource`. The running application continues to use the untouched public model until Phase 11.

## Self-Check: PASSED

- All five planned files exist and are committed in `6b48548`, `9a3e900`, and `6b707e9`.
- The full solution test command passed with 1,062 tests.
- The live `canvas` database returned `canvases`, `figure_types`, `figures` for schema `v11`, and `4` for `v11.figure_types`.
