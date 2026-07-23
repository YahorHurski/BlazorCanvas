---
phase: BC-09-shape-registry-validation-gateway
plan: 02
subsystem: input-validation
tags: [csharp, system-text-json, regex, xunit, svg-security]
requires:
  - phase: BC-09 plan 01
    provides: GeometryJson's byte-stable Utf8JsonWriter serialisation helper
provides:
  - Typed, idempotently sanitised FigureStyle values
  - Never-throwing client style JSON parsing and four-key serialisation
  - Hostile-style regression coverage for D-66 bounds and allowlist rules
affects: [BC-09 plans 06, BC-10 persistence, BC-11 rendering]
tech-stack:
  added: []
  patterns:
    - Parse untrusted JSON into a typed record, sanitise it, then serialise only literal known keys
    - Replace non-finite doubles before clamping to prevent NaN from crossing an SVG trust boundary
key-files:
  created:
    - src/BlazorCanvas/Shapes/FigureStyle.cs
    - src/BlazorCanvas/Shapes/StyleGateway.cs
    - tests/BlazorCanvas.Tests/Shapes/StyleGatewayTests.cs
  modified: []
key-decisions:
  - "GeneratedRegex supplies one cached, anchored fixed-width colour allowlist shared by both style colours."
  - "Style JSON is emitted only from a sanitised FigureStyle using four fixed literal keys and order."
requirements-completed: [VALID-01, VALID-03]
coverage:
  - id: D1
    description: "FigureStyle rejects hostile colours and bounds finite and non-finite numeric style values before they can reach SVG attributes."
    requirement: VALID-03
    verification:
      - kind: unit
        ref: "tests/BlazorCanvas.Tests/Shapes/StyleGatewayTests.cs#Sanitised_HostileStroke_ReplacesItAndNeverSerialisesIt"
        status: pass
      - kind: unit
        ref: "tests/BlazorCanvas.Tests/Shapes/StyleGatewayTests.cs#Sanitised_NonFiniteStrokeWidth_ReplacesItWithTheDefaultBeforeClamping"
        status: pass
    human_judgment: false
  - id: D2
    description: "StyleGateway drops unknown client keys and writes exactly four known fields from the typed record."
    requirement: VALID-01
    verification:
      - kind: unit
        ref: "tests/BlazorCanvas.Tests/Shapes/StyleGatewayTests.cs#Parse_UnknownHostileKeys_DropsThemBeforeSerialisation"
        status: pass
      - kind: unit
        ref: "tests/BlazorCanvas.Tests/Shapes/StyleGatewayTests.cs#ToJson_AnyInput_WritesExactlyTheFourKnownPropertiesInFixedOrder"
        status: pass
    human_judgment: false
duration: 4min
completed: 2026-07-21
status: complete
---

# Phase BC-09 Plan 02: Style Validation Gateway Summary

**A typed style boundary now converts hostile browser JSON into bounded, allowlisted values and emits only a four-key canonical payload.**

## Performance

- **Duration:** 4 min
- **Started:** 2026-07-21T21:32:21Z
- **Completed:** 2026-07-21T21:36:14Z
- **Tasks:** 2/2
- **Files modified:** 3
- **Test baseline:** 441 passing
- **Final full suite:** 489 passing (48 tests added)

## Accomplishments

- Added an idempotent `FigureStyle` record whose defaults precisely match the existing committed SVG appearance, whose colours are allowlisted, and whose finite numeric values are bounded.
- Added a never-throwing `StyleGateway` that defensively parses client JSON and serialises only `stroke`, `stroke_width`, `fill`, and `opacity` in byte-stable order.
- Added 48 focused tests for hostile colour payloads, malformed and overly deep JSON, unknown-key removal, bounds, non-finite values, output order, and idempotence.

## Task Commits

1. **Task 1: FigureStyle record with the D-66 sanitising rules** - `de62471` (feat)
2. **Task 2: StyleGateway parse/serialise, and the hostile-style test suite** - `e01a54f` (feat)

## Files Created/Modified

- `src/BlazorCanvas/Shapes/FigureStyle.cs` - Typed D-66 style defaults, colour allowlist, and finite-before-clamp sanitisation.
- `src/BlazorCanvas/Shapes/StyleGateway.cs` - Defensive JSON parser and fixed-order canonical writer.
- `tests/BlazorCanvas.Tests/Shapes/StyleGatewayTests.cs` - Security and boundary regression coverage for the style trust boundary.

## Decisions Made

- Used a source-generated, culture-invariant regex because net10.0 supports it and it provides one cached static matcher for the fixed-width allowlist.
- Reused `GeometryJson.Serialise` so style output follows the established UTF-8, minified, invariant writer pattern without editing 09-01-owned code.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Test correctness] Assert complete hostile JSON values rather than ambiguous substrings**
- **Found during:** Task 2 (StyleGateway parse/serialise, and the hostile-style test suite)
- **Issue:** The requested raw-substring assertion is false for the hostile `000000`, which is naturally contained in the safe default `#000000`, and for the empty string, which is contained in every .NET string.
- **Fix:** Asserted that the hostile value's complete JSON string literal is absent from the canonical output, proving it was not stored or re-emitted while avoiding false positives from safe defaults.
- **Files modified:** `tests/BlazorCanvas.Tests/Shapes/StyleGatewayTests.cs`
- **Verification:** Focused suite passed 48/48; full suite passed 489/489.
- **Committed in:** `e01a54f`

---

**Total deviations:** 1 auto-fixed (Rule 1 test correctness).
**Impact on plan:** The replacement makes the test executable while preserving and precisely testing the intended raw-value non-persistence guarantee.

## Issues Encountered

- The initial focused run confirmed the specified substring edge case; it was resolved through the Rule 1 assertion correction above.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

- Plan 09-06 can compose this style boundary with the geometry gateway, and Phase 10 can store its canonical output.
- No blockers. The existing renderer, components, data layer, and legacy test directories remain untouched.

## Verification

- `dotnet build src/BlazorCanvas/BlazorCanvas.csproj --nologo -v q` — passed with 0 warnings and 0 errors.
- `dotnet test --nologo --filter "FullyQualifiedName~BlazorCanvas.Tests.Shapes.StyleGatewayTests"` — passed 48/48.
- `dotnet test --nologo` — passed 489/489.
- Source and diff scope checks — passed; no legacy geometry, components, data, or existing test directories changed.

## Self-Check: PASSED

---
*Phase: BC-09-shape-registry-validation-gateway*
*Completed: 2026-07-21*
