---
phase: BC-09-shape-registry-validation-gateway
plan: 03
subsystem: database-fixture
tags: [postgres, pg_dump, migration, fixture, redaction, dotnet]
requires:
  - phase: BC-09 plans 01-02
    provides: Additive Phase 9 work that leaves the v1.1 schema intact for this one-shot capture
provides:
  - Immutable redacted v1.1 pre-rewrite PostgreSQL fixture for Phase 10 MIGR-03
  - Proven migration expectations for eight ordered, intentionally varied figure rows
  - Test-output copy rule for the future replay test
affects: [BC-10 migration, MIGR-03 replay test]
tech-stack:
  added: []
  patterns:
    - Generate committed SQL from PostgreSQL and redact password values at their source
    - Seal one-shot migration fixtures with a SHA-256, provenance manifest, restore proof, and human approval
key-files:
  created:
    - tests/BlazorCanvas.Tests/Fixtures/v1.1-pre-rewrite.sql
    - tests/BlazorCanvas.Tests/Fixtures/v1.1-pre-rewrite-MANIFEST.md
  modified:
    - tests/BlazorCanvas.Tests/BlazorCanvas.Tests.csproj
key-decisions:
  - "Captured the entire pre-rewrite database while using a constant server-generated password placeholder in the committed dump."
  - "Seeded eight ordered rows under v11-migration-fixture so Phase 10 can prove every shape conversion, line direction, bbox edge case, and stacking order."
  - "Developer approved the manifest, counts, password redaction, and irreversible capture before it was sealed."
requirements-completed: []
coverage:
  - id: D1
    description: "A LF-only, checksummed v1.1 PostgreSQL fixture restores with the original schema, redacted passwords, and the curated ordered rows."
    verification:
      - kind: integration
        ref: "docker restore into canvas_dumpcheck with psql -v ON_ERROR_STOP=1"
        status: pass
      - kind: other
        ref: "SHA-256 80FB2335AAE717DA3E6210639A976E796D6F9B9CAD0FD1E715B12ED90C43CE22; dotnet test --nologo"
        status: pass
    human_judgment: false
  - id: D2
    description: "The developer reviewed and approved the one-shot fixture, expected migration values, redaction, and live seeded rows."
    verification:
      - kind: manual_procedural
        ref: "Task 3 blocking human-verification checkpoint"
        status: pass
    human_judgment: true
    rationale: "The content of an irreversible historical capture and acceptance of its redaction policy require developer judgment."
duration: 53min
completed: 2026-07-22
status: complete
---

# Phase BC-09 Plan 03: Pre-Rewrite Fixture Summary

**An approved, restore-proven, redacted v1.1 PostgreSQL snapshot now preserves the exact migration subject Phase 10 needs, including ordered edge-case figures and their expected converted geometry.**

## Performance

- **Duration:** 53 min (including the blocking approval checkpoint)
- **Started:** 2026-07-21T21:39:46Z
- **Completed:** 2026-07-21T22:31:21Z
- **Tasks:** 3/3
- **Files modified:** 3
- **Fixture SHA-256:** `80FB2335AAE717DA3E6210639A976E796D6F9B9CAD0FD1E715B12ED90C43CE22`
- **Capture counts:** 708 users; 795 figures; 200 lines, 350 rectangles, 118 circles, 127 triangles
- **Fixture figure IDs:** 3860, 3861, 3862, 3863, 3864, 3865, 3866, 3867

## Accomplishments

- Guarded the live database as pre-rewrite, then seeded `v11-migration-fixture` (user 3561) with four lines, two rectangles, a circle, and a triangle in known z-order.
- Captured the full v1.1 schema and data while redacting every password server-side; restored the SQL into `canvas_dumpcheck` with four CHECK constraints, the expected counts, fixture ordering, and exactly one password value.
- Added the immutable checksum manifest and test-project copy rule, then received explicit developer approval of the irreversible capture.

## Task Commits

1. **Task 1: Verify schema and seed curated fixture rows** - `651abe6` (chore)
2. **Task 2: Assemble, redact, restore-prove, and commit the dump** - `de89dcd` (test)
3. **Task 3: Approve the one-shot fixture before it is sealed** - `1d260c1` (docs)

## Files Created/Modified

- `tests/BlazorCanvas.Tests/Fixtures/v1.1-pre-rewrite.sql` - LF-only, immutable pre-rewrite schema and data snapshot with password placeholders.
- `tests/BlazorCanvas.Tests/Fixtures/v1.1-pre-rewrite-MANIFEST.md` - Provenance, checksum, capture counts, eight-row migration expectations, and Phase 10 replay instructions.
- `tests/BlazorCanvas.Tests/BlazorCanvas.Tests.csproj` - Copies the SQL fixture to test output via `None Update`.

## Decisions Made

- Captured all existing users and figures, not a subset, while redacting passwords as they are emitted by PostgreSQL; D-08 remains unchanged in the application.
- Used the live schema's identity order as the fixture's observable z-order and documented `z = old id` for all eight curated rows.
- Sealed the fixture only after the required developer review approved its expected values, counts, redaction, and seeded live rows.

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered

None.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

- Phase 10 can restore this approved fixture for MIGR-03 and compare the conversion result to the manifest's geometry, bbox, and z-order table.
- No blockers. The capture must not be regenerated after Phase 10 starts.

## Verification

- Restored into a fresh `canvas_dumpcheck` database with `psql -v ON_ERROR_STOP=1` — passed; 708 users, 795 figures, four `figures` CHECK constraints, all eight fixture rows, and one distinct password value.
- `dotnet build tests/BlazorCanvas.Tests/BlazorCanvas.Tests.csproj --nologo -v q` — passed with 0 warnings and 0 errors.
- `dotnet test --nologo --no-build` — passed 489/489.
- SHA-256 and LF check — passed; 0 carriage-return bytes and checksum matches the manifest.
- Blocking human verification — approved by the developer.

## Self-Check: PASSED

- Found both fixture files and the test-project update.
- Confirmed task commits `651abe6`, `de89dcd`, and `1d260c1` exist.

---
*Phase: BC-09-shape-registry-validation-gateway*
*Completed: 2026-07-22*
