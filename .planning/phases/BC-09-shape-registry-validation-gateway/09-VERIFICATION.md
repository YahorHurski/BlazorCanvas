---
phase: BC-09-shape-registry-validation-gateway
verified: 2026-07-22T00:00:00Z
status: human_needed
score: 45/45 must-haves verified
behavior_unverified: 0
overrides_applied: 0
human_verification:
  - test: "Confirm the Phase 9 source-boundary review accepts the three judgment-tier prohibitions."
    expected: "No type-specific rule is deliberately placed outside an IShapeDefinition implementation; Phase 9 adds no visible rejection UI; and no rejection path logs, echoes, or rebroadcasts raw client input."
    why_human: "The plans intentionally mark these as judgment-tier prohibitions without deterministic enforcement descriptors. Automated source and test evidence is clean, but the verifier must not silently mark them green."
---

# Phase BC-09: Shape Registry & Validation Gateway Verification Report

**Phase Goal:** All type-specific figure logic and client-supplied JSON validation live behind pure-C# `IShapeDefinition` and a single validation gateway, proven by unit tests with no database involvement and no running-app change.

**Verified:** 2026-07-22

**Status:** human_needed

**Re-verification:** No — initial verification. The post-audit commit `46c23aa` was included.

## Goal Achievement

### Observable Truths

| # | Truth | Status | Evidence |
| --- | --- | --- | --- |
| 1 | Four shipped types use one `IShapeDefinition` contract for parse, drawability, bounds, and gestures. | VERIFIED | `IShapeDefinition.cs` defines the six-member contract; `LineShape`, `RectangleShape`, `CircleShape`, and `TriangleShape` each implement it. The 196-case v1.1 gesture grid and all shape tests pass. |
| 2 | Line and triangle retain point-list geometry rather than becoming bbox-derived shapes. | VERIFIED | `LineGeometry` and `TriangleGeometry` hold `IReadOnlyList<LocalPoint>`; `PointListPrimacyTests` proves same-bounds/different-JSON pairs and downward/sideways triangles. |
| 3 | A fifth, unshipped type works through the registry without production or schema changes. | VERIFIED | Test-only `PentagonShape` is registered and exercised only as `IShapeDefinition`; the isolation test proves fresh default registries still have four definitions. |
| 4 | Hostile geometry is silently rejected before bounds computation. | VERIFIED | `FigureInputGateway.TryValidate` parses, calls `TryParseGeometry`, then its shared helper checks `IsDrawable` before `BoundsOf`; hostile-input tests and the v1.1 compatibility-floor cases pass. |
| 5 | Hostile style is bounded/replaced and never re-emitted verbatim. | VERIFIED | `FigureStyle.Sanitised`, `StyleGateway`, and gateway hostile-style tests enforce the fixed hex allowlist, finite-before-clamp values, and four-key canonical JSON. |
| 6 | Geometry helper and registry foundational invariants hold. | VERIFIED | Finite JSON parsing, invariant writer output, ordinal lookup, duplicate rejection, and ordered registration are covered by `GeometryModelTests` and `ShapeRegistryTests`. |
| 7 | The one-shot v1.1 fixture is immutable, redacted, reproducible, and ready for Phase 10. | VERIFIED | SHA-256 is `80FB2335AAE717DA3E6210639A976E796D6F9B9CAD0FD1E715B12ED90C43CE22`; direct restore produced 708 users, 795 figures, four checks, one password value, and eight curated rows. |

**Score:** 45/45 plan and roadmap must-have truths verified (0 present-but-behavior-unverified).

### Required Artifacts

| Artifact | Expected | Status | Details |
| --- | --- | --- | --- |
| `src/BlazorCanvas/Shapes/IShapeDefinition.cs` | Central typed shape contract | VERIFIED | Six required members, with no resize surface. |
| `src/BlazorCanvas/Shapes/*Shape.cs` and `DefaultShapes.cs` | Four isolated definitions and canonical registry | VERIFIED | All substantive, compiled, and covered by the gesture-equivalence and definition tests. |
| `src/BlazorCanvas/Shapes/FigureStyle.cs`, `StyleGateway.cs` | Typed style sanitisation and canonical JSON | VERIFIED | Generated allowlist regex, finite-before-clamp handling, MaxDepth 32 parsing, fixed four-key output. |
| `src/BlazorCanvas/Shapes/FigureInputGateway.cs` | Single record-validation gateway | VERIFIED | Shared post-parse helper owns drawability, bounds, sanitisation, and serialisation; no logging or error-channel API. |
| `src/BlazorCanvas/Shapes/ValidatedFigureInput.cs` | Read-only persistence-safe result | VERIFIED | Post-audit `46c23aa` changed the public positional constructor to an internal constructor and added a passing public-constructor guard test. |
| `tests/BlazorCanvas.Tests/Shapes/*.cs` | Isolated unit proof | VERIFIED | 628 shape tests passed without a database test fixture or database collection. |
| `tests/BlazorCanvas.Tests/Fixtures/v1.1-pre-rewrite.sql` and manifest | Pre-rewrite migration subject | VERIFIED | Tracked, LF-only, checksummed, restorable, and copied to test output by the test project. |

### Key Link Verification

| From | To | Via | Status | Details |
| --- | --- | --- | --- | --- |
| `DefaultShapes.CreateRegistry` | shipped definitions | ordered `Register` calls | WIRED | Registration order is `line`, `rectangle`, `circle`, `triangle`; parity tests compare it to the v1.1 mapping. |
| `FigureInputGateway` | shape registry/style gateway | exact lookup, typed parse, shared `TryCreateValidatedInput` | WIRED | Canonical type comes from `definition.Name`; geometry/style text is re-serialised from typed records. |
| `ValidatedFigureInput.Bounds` | future Phase 10 bbox persistence | documented return contract | WIRED AT PHASE BOUNDARY | Phase 10 is explicitly responsible for persistence; BC-09 correctly leaves the running app unchanged. |
| fixture SQL | Phase 10 replay test | test-project `None Update` copy rule | WIRED | The SQL fixture is copied to output and its manifest gives expected conversion values and z-order. |

### Behavioral Spot-Checks

| Behavior | Command | Result | Status |
| --- | --- | --- | --- |
| Shape-only, database-free suite | `dotnet test --nologo --filter "FullyQualifiedName~BlazorCanvas.Tests.Shapes"` | 628 passed, 0 failed | PASS |
| Post-audit gateway-boundary guard | named `ValidatedFigureInput_HasNoPublicConstructor_AndGatewayResultsRemainReadable` test | 1 passed, 0 failed | PASS |
| Full workspace regression | `dotnet test --nologo` | 1,033 passed, 0 failed | PASS |

### Fixture Restore Proof

| Check | Result | Status |
| --- | --- | --- |
| Restore with `psql -v ON_ERROR_STOP=1` into fresh `canvas_verify_bc09` | Completed without SQL errors | PASS |
| Restored counts | 708 users; 795 figures; eight fixture-user rows | PASS |
| Pre-rewrite schema | Four `figures` CHECK constraints present | PASS |
| Password redaction | One distinct password value | PASS |
| Integrity | Manifest SHA-256 matched and file contained zero CR bytes | PASS |

### Requirements Coverage

| Requirement | Source Plans | Status | Evidence |
| --- | --- | --- | --- |
| SHAPE-01 | 09-01, 09-04, 09-05 | SATISFIED | Central interface, four implementations, default registry, and exact gesture-grid tests. |
| SHAPE-02 | 09-01, 09-04, 09-05 | SATISFIED | Ordered point-list records plus same-bounds/different-JSON and non-upright-triangle tests. |
| SHAPE-03 | 09-04, 09-05 | SATISFIED | Test-only pentagon registers and round-trips through `IShapeDefinition` without production/schema changes. |
| VALID-01 | 09-02, 09-06 | SATISFIED | Typed-record re-serialisation removes unknown geometry/style keys and hostile values. |
| VALID-02 | 09-06 | SATISFIED | Invalid geometry returns false/null silently; legal horizontal and vertical v1.1 lines remain valid. |
| VALID-03 | 09-02, 09-06 | SATISFIED | Style allowlist, finite-value replacement, clamps, and hostile payload tests. |

### Anti-Patterns and Review Notes

| File | Severity | Finding | Impact |
| --- | --- | --- | --- |
| `src/BlazorCanvas/Shapes/ShapeRegistry.cs` | WARNING | `All` and `Names` expose the concrete mutable `List` instances through `IReadOnlyList`; an intentional downcast can desynchronise visible ordering from the lookup dictionary. | Does not fail the phase goal or existing tests, but returning read-only wrappers/snapshots would preserve the registry's registration-only mutation invariant. |
| `tests/BlazorCanvas.Tests/Shapes/V11GestureEquivalenceTests.cs` | WARNING | The line-grid comparison sorts both endpoint arrays, so it proves the endpoint set but not stored endpoint order. | Point-order round trips are independently covered by line and primacy tests; this equivalence test should not be relied on as an order-preservation guard. |

No `TBD`, `FIXME`, `XXX`, `TODO`, placeholder, logging, database-fixture, data-layer, UI, or renderer references were found in the new Shapes production/test scope. No Phase 9 UI surface changed.

### Human Verification Required

### 1. Judgment-tier prohibition acceptance

**Test:** Review the Phase 9 source boundary and accept the three judgment-tier prohibitions recorded in plans 09-04 and 09-06.

**Expected:** You agree that Phase 9 has no intentional type-specific rule outside `IShapeDefinition`, no visible rejection UI, and no raw-client-payload logging/echo/rebroadcast path.

**Why human:** The plans deliberately declare these as judgment-tier prohibitions with no deterministic enforcement descriptors. Static and unit-test evidence is clean, but the verification policy requires explicit human acceptance rather than a silent automated pass.

## Gaps Summary

No goal-blocking gaps were found. The only remaining gate is the explicit human resolution of the judgment-tier prohibitions; the fixture approval checkpoint has already been completed.

---

_Verified: 2026-07-22_

_Verifier: the agent (gsd-verifier)_
