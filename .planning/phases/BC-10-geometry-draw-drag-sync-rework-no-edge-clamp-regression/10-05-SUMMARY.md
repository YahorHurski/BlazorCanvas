---
phase: 10-geometry-draw-drag-sync-rework-no-edge-clamp-regression
plan: 05
subsystem: testing
tags: [csharp, xunit, postgresql, geometry]

# Dependency graph
requires:
  - phase: 10-geometry-draw-drag-sync-rework-no-edge-clamp-regression
    plan: 04
    provides: "ShapeRender, anchor+geometry render contract on FigureShape/SelectionTrace/Home.razor; the last box-shaped bridge (dragCurrentBox/dragFigure/FigureBox) gone"
provides:
  - "NormalisationTests — the D-41 line-normalisation landmine asserted end to end, from DrawGesture.Build through GeometryCodec.Encode to the exact stored anchor + geometry JSON, in both gesture directions plus the distinct down-and-right case"
  - "GeometryCodecTests — round-trip theory extended with STOR-04 off-canvas cases (negative anchor, anchor past 1472/828, a line delta carrying off-canvas) and a new theory pinning every serialised geometry member as an integer (no '.', 'e', 'E')"
  - "DatabaseFixture.TryInsertRawFigureAsync(type, x, y, geometry) — a raw anchor+geometry insert helper with no GeometryCodec re-encoding; TryInsertFigureAsync(type, geometry) convenience overload"
  - "CheckConstraintTests and SchemaShapeTests reworked onto anchor+geometry, with two new schema assertions (no CHECK names a retired bounding-box column; geometry/z are NOT NULL)"
  - "TypeWhitelistAndPersistenceTests — renamed from GuardMirrorsChecksTests, dead Matrix/box-field data removed, class doc corrected to what the file actually proves post-D-59"
  - "UnitTest1.cs deleted — no more empty template stub in the suite"
affects: [10-06-verification-checkpoint]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "A database raw-insert test helper takes the model's own storage shape (type literal + anchor + geometry JSON string) with zero re-encoding through the production codec, so a test can hand PostgreSQL a value the C# write path could never produce — the entire reason the helper exists (PA-13)."
    - "Every geometry-bearing test asserts the exact serialised JSON literal or a no-decimal/no-exponent invariant over it (D-20), never a looser predicate — a serialiser change, a member rename, or added whitespace cannot pass silently (T-10-17)."

key-files:
  created:
    - tests/BlazorCanvas.Tests/Database/TypeWhitelistAndPersistenceTests.cs
  modified:
    - tests/BlazorCanvas.Tests/Geometry/NormalisationTests.cs
    - tests/BlazorCanvas.Tests/Geometry/GeometryCodecTests.cs
    - tests/BlazorCanvas.Tests/Database/DatabaseFixture.cs
    - tests/BlazorCanvas.Tests/Database/CheckConstraintTests.cs
    - tests/BlazorCanvas.Tests/Database/SchemaShapeTests.cs
  deleted:
    - tests/BlazorCanvas.Tests/Database/GuardMirrorsChecksTests.cs
    - tests/BlazorCanvas.Tests/UnitTest1.cs

key-decisions:
  - "GuardMirrorsChecksTests.cs's single TryInsertFigureAsync call site was fixed inline in Task 2 (ahead of Task 3's rename) to keep the full solution build green after DatabaseFixture's signature changed — mirrors the same cross-task inline-fix pattern 10-03 documented for Home.razor's receive path."
  - "The convenience overload TryInsertFigureAsync(FigureType, string geometry) fixes the anchor at (0,0) rather than taking one — the only caller (the type-whitelist round-trip test) only cares about the type literal, and D-59 left no CHECK on geometry content to make the anchor value matter."

requirements-completed: [TEST-02, STOR-02, STOR-03]

coverage:
  - id: D1
    description: "The D-41 line-normalisation landmine is asserted end to end: a diagonal drawn up-and-right (press 0,100 -> release 100,0) stores anchor (0,100) with geometry {\"dx\":100,\"dy\":-100}, never the axis-sorted (0,0) origin form; the mirror gesture normalises identically; the down-and-right diagonal stores the distinct positive-dy form"
    requirement: "TEST-02"
    verification:
      - kind: unit
        ref: "tests/BlazorCanvas.Tests/Geometry/NormalisationTests.cs#Landmine_UpAndRightDiagonal_StoresAnchorAtFirstEndpointWithNegativeDy, Landmine_MirrorGesture_ReleasedFirstThenPressed_NormalisesToSameStoredForm, Landmine_DownAndRightDiagonal_StoresPositiveDy_DistinctFromUpAndRightForm"
        status: pass
    human_judgment: false
  - id: D2
    description: "GeometryCodecTests round-trips off-canvas cases legal under STOR-04 (negative anchor, anchor past 1472 on x, anchor past 828 on y, a line delta carrying off-canvas) and asserts no serialised geometry string contains a decimal point or exponent character for any type"
    requirement: "TEST-02"
    verification:
      - kind: unit
        ref: "tests/BlazorCanvas.Tests/Geometry/GeometryCodecTests.cs#EncodeThenDecode_ReturnsOriginalBox, SerialisedGeometry_ContainsNoDecimalPointOrExponent_EveryMemberIsAnInteger"
        status: pass
    human_judgment: false
  - id: D3
    description: "DatabaseFixture's raw-insert helper takes a type literal, an integer anchor and a geometry JSON string with no Box parameter and no re-encoding through GeometryCodec"
    requirement: "STOR-01, TEST-02"
    verification:
      - kind: unit
        ref: "grep -c 'x1' tests/BlazorCanvas.Tests/Database/DatabaseFixture.cs returns 0; dotnet build BlazorCanvas.sln --nologo (0 Warning(s), 0 Error(s))"
        status: pass
    human_judgment: false
  - id: D4
    description: "CheckConstraintTests still asserts the type-whitelist rejects 'oval' and 'Circle' with a check-violation SQL state, and that a horizontal line, a vertical line, an up-and-right diagonal, and well-formed circle/rectangle/triangle rows all INSERT successfully, all expressed as anchor + geometry JSON"
    requirement: "TEST-02"
    verification:
      - kind: unit
        ref: "dotnet test BlazorCanvas.sln --nologo --filter FullyQualifiedName~CheckConstraintTests (22 total incl. SchemaShapeTests, 0 failed)"
        status: pass
    human_judgment: false
  - id: D5
    description: "SchemaShapeTests covers the full D-59 shape against the live catalog including two new assertions: no CHECK constraint on figures mentions a retired bounding-box column name, and geometry/z are both NOT NULL"
    requirement: "TEST-02"
    verification:
      - kind: unit
        ref: "tests/BlazorCanvas.Tests/Database/SchemaShapeTests.cs#Figures_HasNoCheckConstraintReferencingARetiredBoundingBoxColumn, FiguresGeometryAndZ_AreNotNull"
        status: pass
    human_judgment: false
  - id: D6
    description: "GuardMirrorsChecksTests is renamed to TypeWhitelistAndPersistenceTests with its dead eight-box-field/Matrix data removed, keeping both live tests (type-literal round-trip, container-teardown persistence) and the Database collection attribute; UnitTest1.cs (empty template stub) is deleted"
    requirement: "TEST-02"
    verification:
      - kind: unit
        ref: "test ! -f tests/BlazorCanvas.Tests/Database/GuardMirrorsChecksTests.cs; test ! -f tests/BlazorCanvas.Tests/UnitTest1.cs; dotnet test BlazorCanvas.sln --nologo (416/416 pass, 0 skipped)"
        status: pass
    human_judgment: false
  - id: D7
    description: "The full solution builds clean and the full suite is green; the Phase-9 migration round-trip proof (MIG-02) is untouched by this plan"
    requirement: "TEST-02"
    verification:
      - kind: unit
        ref: "dotnet build BlazorCanvas.sln --nologo (0 Warning(s), 0 Error(s)); dotnet test BlazorCanvas.sln --nologo (416/416 pass); dotnet test --filter FullyQualifiedName~MigrationRoundTrip (1/1 pass)"
        status: pass
    human_judgment: false

duration: 25min
completed: 2026-07-24
status: complete
---

# Phase 10 Plan 05: Geometry Draw/Drag/Sync Rework — Test Rework Summary

**The suite is finished with TEST-02: the D-41 line landmine is now proven end to end into stored geometry, the database helpers and schema assertions speak anchor+geometry natively with no bounding-box re-encoding, and the CHECK-mirroring test class is retired to what it still proves after D-59 deleted the CHECKs it used to mirror.**

## Performance

- **Duration:** ~25 min
- **Started:** 2026-07-24 (session start)
- **Completed:** 2026-07-24
- **Tasks:** 3
- **Files modified:** 8 (5 modified, 1 created, 2 deleted)

## Accomplishments
- `NormalisationTests` gained the end-to-end TEST-01/TEST-02 landmine proof: a line gesture driven through `DrawGesture.Build` then `GeometryCodec.Encode` for the up-and-right diagonal (press 0,100 → release 100,0) asserts the exact stored anchor `(0,100)` and geometry `{"dx":100,"dy":-100}`, explicitly rejecting the axis-sorted `(0,0)`-anchored positive-dy form. The mirror gesture (press 100,0 → release 0,100) normalises to the identical stored form; a third down-and-right case (press 0,0 → release 100,100) pins the distinct positive-dy form so a sign error in either direction fails. Every pre-existing `NormalisationTests` case carries over unchanged — the file's test count only grew (D-41, TEST-02)
- `GeometryCodecTests`' round-trip theory gained four STOR-04 off-canvas cases (negative anchor, anchor past 1472 on x, anchor past 828 on y, a line whose delta carries off-canvas) and a new theory asserting every serialised geometry string, for every existing round-trip case, contains no `.`, `e`, or `E` character — pinning D-20's integer-only invariant across the whole type/anchor/extent matrix. All four types already carried an exact-literal assertion (rectangle/triangle `w`/`h`, circle `r`, line `dx`/`dy`); none needed adding
- `DatabaseFixture.TryInsertRawFigureAsync` reshaped to `(string typeLiteral, int x, int y, string geometry)` — no `Box` parameter, no re-encoding through `GeometryCodec`. The private four-integer `EncodeForRawInsert` switch is deleted entirely, so a test can now hand PostgreSQL a value the C# write path could never produce, which is the whole reason the helper exists (PA-13). The convenience overload becomes `TryInsertFigureAsync(FigureType, string geometry)`, fixing the anchor at `(0,0)` since only the type literal matters to its one caller
- `CheckConstraintTests` reworked onto the new helper: the two type-whitelist rejection cases (`oval`, `Circle`) and the accepted-line/well-formed-figure cases are all now expressed as anchor + geometry JSON literals. Its class doc is rewritten to state plainly what the file proves post-D-59 — the type whitelist discriminates, well-formed rows insert — not "the database refuses an illegal geometry"
- `SchemaShapeTests` gained two new live-catalog assertions: no CHECK constraint on `figures` mentions any retired bounding-box column name (`x1`/`y1`/`x2`/`y2`), and `geometry`/`z` are both `NOT NULL` — the model's two strongest remaining database-level guarantees. The class doc's stale "plan 01-03's migration" reference is corrected to "phase BC-09's migration." All twelve pre-existing assertions still pass against the live catalog unchanged
- `GuardMirrorsChecksTests.cs` renamed to `TypeWhitelistAndPersistenceTests.cs`, keeping the `Database` collection attribute and both live tests exactly in intent: the type-literal round-trip against `figures_type_is_known` (D-46), and the container-teardown persistence proof — the only place in the suite proving the named volume survives a full `docker compose down`/`up` (D-27). The eight boundary-probing `Box` fields and the dead `Matrix()` member data (already unconsumed since 09-05 removed the theory that used them) are deleted. The class doc is rewritten to explain the CHECK-mirroring proof moved to `MinSizeGuardTests` after D-59 deleted the CHECKs it used to mirror
- `UnitTest1.cs`, the empty `dotnet new xunit` template stub, is deleted
- Full solution builds clean (0 warnings, 0 errors); full test suite is 416/416 green (up from the 397 baseline at 10-04's close — 19 net new tests: 3 landmine cases, 4 off-canvas round-trip theory instances, 11 integer-only theory instances, 1 net for the geometry/z NOT NULL and retired-column CHECK assertions, minus the removed template stub). `MigrationRoundTrip` filter still passes 1/1 — Phase 9's MIG-02 proof is untouched by this plan

## Task Commits

Each task was committed atomically:

1. **Task 1: The line landmine, asserted through to stored geometry** - `61ddcbe` (test)
2. **Task 2: Database helpers and schema assertions speak anchor+geometry** - `6fe9dcf` (test)
3. **Task 3: Retire the CHECK-mirroring test class, drop the stub, and prove the whole suite green** - `0de9631` (test)

**Plan metadata:** pending (this commit)

## Files Created/Modified
- `tests/BlazorCanvas.Tests/Geometry/NormalisationTests.cs` - Three new end-to-end landmine cases; all pre-existing cases unchanged
- `tests/BlazorCanvas.Tests/Geometry/GeometryCodecTests.cs` - Four new off-canvas round-trip cases; new integer-only-members theory
- `tests/BlazorCanvas.Tests/Database/DatabaseFixture.cs` - Raw-insert helper reshaped to (type, x, y, geometry); private box-encode switch deleted; convenience overload changed
- `tests/BlazorCanvas.Tests/Database/CheckConstraintTests.cs` - Rewritten onto anchor+geometry; class doc corrected
- `tests/BlazorCanvas.Tests/Database/SchemaShapeTests.cs` - Stale doc reference fixed; two new NOT NULL / retired-column assertions
- `tests/BlazorCanvas.Tests/Database/TypeWhitelistAndPersistenceTests.cs` - New; renamed from GuardMirrorsChecksTests.cs with dead data removed and class doc rewritten
- `tests/BlazorCanvas.Tests/Database/GuardMirrorsChecksTests.cs` - Deleted (renamed to TypeWhitelistAndPersistenceTests.cs)
- `tests/BlazorCanvas.Tests/UnitTest1.cs` - Deleted (empty template stub)

## Decisions Made
- Followed planner assumptions PA-12 (rename `GuardMirrorsChecksTests`, don't leave a misleading name in place), PA-13 (the raw-insert helper takes an anchor and a geometry string, no `Box`), and PA-6 (no component-test package added) exactly as written.
- Fixed `GuardMirrorsChecksTests.cs`'s single `TryInsertFigureAsync` call site inline during Task 2 — ahead of Task 3's rename boundary — to keep `dotnet build` green after `DatabaseFixture`'s signature changed. This mirrors the identical cross-task inline-fix pattern 10-03's summary documented for `Home.razor`'s receive path, and Task 3 then cleanly completed the rename and cleanup as planned.
- `TryInsertFigureAsync(FigureType, string geometry)`'s anchor is fixed at `(0,0)` rather than taking a parameter — its only caller (the type-literal round-trip test) never varies the anchor, and D-59 left no database CHECK on geometry content that would make the anchor value matter to that test's outcome.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking issue] Inline compile-fix to `GuardMirrorsChecksTests.cs` during Task 2**
- **Found during:** Task 2
- **Issue:** After `DatabaseFixture.TryInsertFigureAsync` changed signature from `(FigureType, Box)` to `(FigureType, string geometry)`, the sole call site in `GuardMirrorsChecksTests.cs` (a Task 3 file, not yet reworked) failed to compile, breaking Task 2's `dotnet build BlazorCanvas.sln` acceptance criterion.
- **Fix:** Updated the one call site to pass a fixed well-formed geometry literal (`{"w":10,"h":10}`) instead of the retired `WellFormed` Box constant. No other change to that file at this point — its full rename and cleanup happened in Task 3 as planned.
- **Files modified:** `tests/BlazorCanvas.Tests/Database/GuardMirrorsChecksTests.cs`
- **Commit:** `6fe9dcf`

### Verification-tooling notes (not code deviations)

None.

## Issues Encountered
None. All three tasks compiled and passed verification on the first attempt; the full suite, including the container-teardown test, ran green in a single pass.

## User Setup Required
None — the Compose PostgreSQL container was already up and healthy on host port 5433 with the clean, correctly-migrated `canvas` database; no `BLAZORCANVAS_TEST_CONNECTION` override was set (the plan's stale `canvas_phase09` operational note was correctly disregarded, per the environment note). The container-teardown test's `docker compose down`/`up` cycle completed and left the container healthy afterward.

## Next Phase Readiness
- TEST-02 is complete: nothing in the suite asserts against the retired bounding-box model any more. TEST-01's three original silent-failure tests are all accounted for — clamp maths retired with the clamp in 10-01/10-02, the circle round-trip re-expressed as a `{r}` assertion in 10-01, and the line-normalisation landmine carried over and extended to stored geometry in this plan.
- The schema assertions in `SchemaShapeTests` cover the full D-59 shape against the live PostgreSQL catalog, including the two new NOT NULL / retired-column guarantees.
- Full solution builds clean (0 warnings, 0 errors); full test suite is 416/416 green, up from the 397 baseline at 10-04's close.
- No `src/` file was touched by this plan, matching the plan's own `<verification>` constraint — this was a pure test-suite rework.
- The remaining item for Phase 10 is the 10-06 live verification checkpoint: on-screen comparison of all four shapes and the selection trace, plus the live two-tab drag-glide behaviours, carried forward from 10-02/10-03/10-04.

---
*Phase: 10-geometry-draw-drag-sync-rework-no-edge-clamp-regression*
*Completed: 2026-07-24*

## Self-Check: PASSED

All 6 created/reworked test files verified present on disk. Both retired files (`GuardMirrorsChecksTests.cs`, `UnitTest1.cs`) verified absent. All three task commits (`61ddcbe`, `6fe9dcf`, `0de9631`) verified present in git history. Full solution build: 0 warnings, 0 errors. Full test suite: 416/416 passing. `MigrationRoundTrip` filter: 1/1 passing.
