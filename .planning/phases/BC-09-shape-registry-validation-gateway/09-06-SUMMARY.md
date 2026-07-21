---
phase: BC-09-shape-registry-validation-gateway
plan: 06
subsystem: shape-validation
tags: [csharp, shapes, validation, json, security, xunit]
requires:
  - phase: BC-09-02
    provides: Sanitised FigureStyle parsing and canonical style JSON
  - phase: BC-09-04
    provides: Shape definitions, drawability rules, bounds, and gestures
provides:
  - A single FigureInputGateway for client-supplied type, geometry, and style
  - Canonical persistence-safe JSON generated only from validated typed records
  - Hostile-input, compatibility-floor, and gesture-parity test coverage
affects: [BC-10, BC-11, persistence, sync, renderer]
tech-stack:
  added: []
  patterns:
    - Resolve, parse, validate, calculate bounds, sanitise, then serialise through one gateway
    - Reject hostile geometry silently while replacing hostile style with fixed safe values
key-files:
  created:
    - src/BlazorCanvas/Shapes/ValidatedFigureInput.cs
    - src/BlazorCanvas/Shapes/FigureInputGateway.cs
    - tests/BlazorCanvas.Tests/Shapes/FigureInputGatewayTests.cs
  modified: []
key-decisions:
  - "The registry definition's literal is persisted; caller-supplied type text is never copied."
  - "Bounds are calculated only after successful parsing and drawability checks, with finite non-negative post-conditions."
  - "Phase 10 persistence and Phase 11 sync must write only through FigureInputGateway; another write path reopens VALID-01/02/03."
patterns-established:
  - "Every client JSON payload crosses one typed-record validation and canonicalisation boundary before persistence."
requirements-completed: [VALID-01, VALID-02, VALID-03]
coverage:
  - id: D1
    description: "FigureInputGateway rejects hostile geometry silently before bounds and returns canonical typed-record JSON."
    requirement: VALID-01
    verification:
      - kind: unit
        ref: "tests/BlazorCanvas.Tests/Shapes/FigureInputGatewayTests.cs#TryValidate_HostileGeometry_ReturnsFalseWithNoResult"
        status: pass
    human_judgment: false
  - id: D2
    description: "The v1.1 compatibility floor retains legal horizontal and vertical lines plus every documented conversion shape."
    requirement: VALID-02
    verification:
      - kind: unit
        ref: "tests/BlazorCanvas.Tests/Shapes/FigureInputGatewayTests.cs#TryValidate_V11LegalConvertedGeometry_IsNeverRejected"
        status: pass
    human_judgment: false
  - id: D3
    description: "Hostile style is clamped or replaced and its raw value is absent from canonical style JSON."
    requirement: VALID-03
    verification:
      - kind: unit
        ref: "tests/BlazorCanvas.Tests/Shapes/FigureInputGatewayTests.cs#TryValidate_HostileStyle_ClampsAndDoesNotReEmitTheHostileValue"
        status: pass
    human_judgment: false
duration: 15min
completed: 2026-07-22
status: complete
---

# Phase BC-09 Plan 06: Figure Input Gateway Summary

**A single registry-backed gateway now rejects unsafe figure geometry, sanitises all style input, and returns only canonical JSON re-serialised from typed records.**

## Performance

- **Duration:** 15 min
- **Tasks:** 2/2
- **Files modified:** 3
- **Verification:** gateway suite 65/65 and full suite 1,032/1,032 passed with zero failures.

## Accomplishments

- Added `ValidatedFigureInput` as the persistence-safe result that carries canonical geometry/style JSON and the authoritative local bounding box.
- Added `FigureInputGateway`, which resolves exact registry types, rejects malformed or undrawable geometry silently, calculates bounds only after drawability, and uses one shared path for wire and gesture input.
- Added hostile-input coverage for parser limits, geometry rejection, style sanitisation, canonical output, v1.1 conversion compatibility, silence, and gesture parity.

## Task Commits

1. **Task 1: ValidatedFigureInput and the FigureInputGateway** — `01dfa11` (feat)
2. **Task 2: The hostile-input test suite for the gateway** — `b6d723b` (test)

## Files Created

- `src/BlazorCanvas/Shapes/ValidatedFigureInput.cs` — validated persistence result and bbox cache contract.
- `src/BlazorCanvas/Shapes/FigureInputGateway.cs` — application-wide validation, sanitisation, and canonicalisation boundary.
- `tests/BlazorCanvas.Tests/Shapes/FigureInputGatewayTests.cs` — hostile, compatibility, silence, and parity coverage.

## Decisions Made

- Stored type literals come from the resolved registry definition, never a client string.
- A horizontal or vertical line remains valid even when its bounds have a zero dimension; only coincident endpoints are rejected.
- Phase 10's persistence layer and Phase 11's sync receiver must write only through `FigureInputGateway`; a second write path silently reopens VALID-01, VALID-02, and VALID-03.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Test correctness] Excluded inherited `object.ToString` from the failure-reason reflection check.**
- **Found during:** Task 2
- **Issue:** The first reflection assertion treated the inherited `object.ToString()` as a gateway failure-reason API.
- **Fix:** Restricted the assertion to methods declared by `FigureInputGateway` itself.
- **Files modified:** `tests/BlazorCanvas.Tests/Shapes/FigureInputGatewayTests.cs`
- **Verification:** Focused suite passed 65/65; full suite passed 1,032/1,032.
- **Committed in:** `b6d723b`

---

**Total deviations:** 1 auto-fixed (Rule 1 - test correctness).
**Impact on plan:** The correction makes the public-surface assertion precise without changing production behaviour or scope.

## Issues Encountered

None beyond the reflection-test false positive resolved above.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

Phase 9 is complete. Phase 10 can make persistence writes through `FigureInputGateway`, and Phase 11 can use its canonical output for sync and rendering without reopening the geometry/style trust boundary.

## Self-Check: PASSED

- All three planned files exist and task commits `01dfa11` and `b6d723b` exist in history.
- The changed-code range contains exactly the three planned source/test files.

---
*Phase: BC-09-shape-registry-validation-gateway*
*Completed: 2026-07-22*
