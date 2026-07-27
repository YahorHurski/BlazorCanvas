---
phase: 09-schema-entity-data-preserving-migration
plan: 02
subsystem: testing
tags: [migration, fixture, postgres, manifest]
requires: []
provides:
  - Immutable pre-rewrite SQL fixture
  - D-59 expected migration manifest for every fixture figure
affects: [BC-09, migration-round-trip]
tech-stack:
  added: []
  patterns:
    - Immutable fixture plus independently derived manifest for migration proofs
key-files:
  created:
    - tests/BlazorCanvas.Tests/Fixtures/v1.1-pre-rewrite.sql
    - tests/BlazorCanvas.Tests/Fixtures/v1.1-pre-rewrite-MANIFEST.md
  modified: []
key-decisions:
  - "The manifest was re-derived from this branch's D-59 geometry shapes instead of copied from de89dcd."
patterns-established:
  - "Fixture expectations are keyed by old id, with z equal to old id."
requirements-completed: [MIG-02]
coverage:
  - id: D1
    description: "The immutable SQL fixture is byte-identical to de89dcd and contains all four figure types plus down-right line probes."
    requirement: MIG-02
    verification:
      - kind: other
        ref: "git hash-object comparison against git show de89dcd:tests/BlazorCanvas.Tests/Fixtures/v1.1-pre-rewrite.sql"
        status: pass
    human_judgment: false
  - id: D2
    description: "The manifest lists expected D-59 anchor, geometry JSON, and z for every fixture figure row."
    requirement: MIG-02
    verification:
      - kind: other
        ref: "manifest row count 795 equals fixture figure row count 795"
        status: pass
    human_judgment: false
duration: 12 min
completed: 2026-07-23
status: complete
---

# Phase 09 Plan 02: Fixture Manifest Summary

**The immutable v1.1 SQL fixture is restored and every row has D-59 expected migration values.**

## Performance

- **Duration:** 12 min
- **Started:** 2026-07-23T18:40:00Z
- **Completed:** 2026-07-23T18:52:00Z
- **Tasks:** 2
- **Files modified:** 2

## Accomplishments

- Restored `v1.1-pre-rewrite.sql` byte-identical from commit `de89dcd`.
- Confirmed the fixture contains 795 figures: 200 lines, 350 rectangles, 118 circles, and 127 triangles.
- Re-derived a full 795-row manifest using this branch's D-59 shapes: rectangle/triangle `{w,h}`, circle centre anchor `{r}`, line `{dx,dy}`, and `z = old id`.

## Task Commits

1. **Task 1: Port the immutable v1.1-pre-rewrite.sql fixture from commit de89dcd** - `1fad020` (test)
2. **Task 2: Re-derive the expected post-migration MANIFEST against this branch's per-type shapes** - `e5d97d2` (test)

**Plan metadata:** committed separately by GSD close-out.

## Files Created/Modified

- `tests/BlazorCanvas.Tests/Fixtures/v1.1-pre-rewrite.sql` - Immutable old-schema SQL snapshot.
- `tests/BlazorCanvas.Tests/Fixtures/v1.1-pre-rewrite-MANIFEST.md` - Expected D-59 migrated values for every figure row.

## Decisions Made

- Sorted manifest rows by old id so the table also pins the intended `ORDER BY z, id` sequence.

## Deviations from Plan

None - plan executed exactly as written.

**Total deviations:** 0 auto-fixed. **Impact on plan:** None.

## Issues Encountered

None.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

Ready for 09-06 to seed the immutable fixture and assert the real migration against the manifest.

---
*Phase: 09-schema-entity-data-preserving-migration*
*Completed: 2026-07-23*
