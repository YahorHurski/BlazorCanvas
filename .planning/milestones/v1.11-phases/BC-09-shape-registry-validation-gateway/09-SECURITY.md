---
phase: BC-09
slug: shape-registry-validation-gateway
status: verified
threats_open: 0
asvs_level: 1
created: 2026-07-22
---

# Phase BC-09 — Security

> Per-phase security contract: threat register, accepted risks, and audit trail.

## Trust Boundaries

| Boundary | Description | Data Crossing |
|----------|-------------|---------------|
| Client JSON → validation gateway | Geometry and style are untrusted until parsed, validated, and re-serialised. | JSON, type name, gesture coordinates |
| Legacy dump → repository fixture | A local PostgreSQL dump becomes a versioned test input. | User data and figure rows |
| Registry → persistence/sync | Resolved type definitions supply canonical type, geometry, bounds, and style. | Validated figure input |

## Threat Register

| Threat ID | Category | Component | Severity | Disposition | Mitigation | Status |
|-----------|----------|-----------|----------|-------------|------------|--------|
| T-09-01 | Spoofing | Registry lookup | medium | mitigate | Ordinal, no-fallback lookup | closed |
| T-09-02 | Tampering | Registry registration | medium | mitigate | Duplicate names rejected | closed |
| T-09-03 | Tampering | Geometry numbers | high | mitigate | Finite-number gate | closed |
| T-09-04 | Tampering | Geometry points | medium | mitigate | Exact point counts and finite pairs | closed |
| T-09-05 | Tampering | Geometry JSON | medium | mitigate | `Utf8JsonWriter` canonical output | closed |
| T-09-06 | Information Disclosure | Geometry parsing | low | mitigate | Bool Try contract; no client text in errors | closed |
| T-09-SC-01 | Tampering | Plan 01 dependencies | low | accept | No package-manager install | accepted |
| T-09-07 | Tampering | Style colours | high | mitigate | Anchored hex allowlist and defaults | closed |
| T-09-08 | Tampering | Style numerics | high | mitigate | Replace non-finite values before clamp | closed |
| T-09-09 | Tampering | Style JSON | medium | mitigate | Emit exactly four literal keys | closed |
| T-09-10 | Denial of Service | Style parser | medium | mitigate | Depth/comment/trailing-comma limits | closed |
| T-09-11 | Denial of Service | Colour regex | low | mitigate | Fixed-width generated regex | closed |
| T-09-12 | Information Disclosure | Style parsing | low | mitigate | Default-on-failure, no throw | closed |
| T-09-SC-02 | Tampering | Plan 02 dependencies | low | accept | No package-manager install | accepted |
| T-09-13 | Information Disclosure | SQL fixture | high | mitigate | Fixed password placeholder | closed |
| T-09-14 | Tampering | SQL fixture | high | mitigate | Immutable banner and SHA-256 | closed |
| T-09-15 | Tampering | SQL fixture | high | mitigate | Restorability, constraints, and count proof | closed |
| T-09-16 | Repudiation | Fixture provenance | medium | mitigate | Manifest capture metadata | closed |
| T-09-17 | Elevation of Privilege | Fixture SQL | medium | mitigate | Generated SQL without ownership/privileges | closed |
| T-09-18 | Denial of Service | Scratch database | low | accept | Local scratch-database residue accepted | accepted |
| T-09-SC-03 | Tampering | Plan 03 dependencies | low | accept | No package-manager install | accepted |
| T-09-19 | Tampering | Shape gestures | high | mitigate | Round and clamp before shape arithmetic | closed |
| T-09-20 | Tampering | Rectangle/circle extents | high | mitigate | Positive finite parse validation | closed |
| T-09-21 | Tampering | Circle gesture | high | mitigate | Radius floor and edge cap | closed |
| T-09-22 | Tampering | Point order | medium | mitigate | Parse/serialise preserve order | closed |
| T-09-23 | Tampering | Geometry keys | medium | mitigate | Typed serializers emit known keys only | closed |
| T-09-24 | Spoofing | Shape definitions | low | mitigate | Runtime geometry-type guards | closed |
| T-09-SC-04 | Tampering | Plan 04 dependencies | low | accept | No package-manager install | accepted |
| T-09-25 | Elevation of Privilege | Test pentagon | medium | mitigate | Test-only class and fresh registries | closed |
| T-09-26 | Tampering | Equivalence grid | high | mitigate | Exact equality assertions | closed |
| T-09-27 | Tampering | Equivalence coverage | medium | mitigate | Fixed 196-case grid | closed |
| T-09-28 | Repudiation | Temporary tests | low | mitigate | Phase 11 removal documented | closed |
| T-09-SC-05 | Tampering | Plan 05 dependencies | low | accept | No package-manager install | accepted |
| T-09-29 | Tampering | Geometry gateway | high | mitigate | Canonical JSON derives from typed records | closed |
| T-09-30 | Tampering | Style gateway | high | mitigate | Sanitise then canonicalise style | closed |
| T-09-31 | Tampering | Bounds | high | mitigate | Drawability before finite bounds | closed |
| T-09-32 | Spoofing | Type name | medium | mitigate | Resolved definition provides type literal | closed |
| T-09-33 | Information Disclosure | Rejections | medium | mitigate | Bool-only, reason-free public surface | closed |
| T-09-34 | Denial of Service | Geometry parser | medium | mitigate | Strict parser options and failure containment | closed |
| T-09-35 | Tampering | Gateway boundary | high | mitigate | Internal constructor; gateway-only creation | closed |
| T-09-36 | Tampering | Migration compatibility | high | mitigate | v1.1 legal-box compatibility tests | closed |
| T-09-SC-06 | Tampering | Plan 06 dependencies | low | accept | No package-manager install | accepted |

## Accepted Risks Log

| Risk ID | Threat Ref | Rationale | Accepted By | Date |
|---------|------------|-----------|-------------|------|
| AR-09-01 | T-09-SC-01…06 | No NuGet, npm, pip, or cargo packages were added; all dependencies are part of .NET 10 or the existing PostgreSQL tooling. | Phase plans | 2026-07-22 |
| AR-09-02 | T-09-18 | A failed local restore may leave the disposable `canvas_dumpcheck` database; its impact is limited to local development. | Phase plan 09-03 | 2026-07-22 |

## Security Audit Trail

| Audit Date | Threats Total | Closed | Open | Run By |
|------------|---------------|--------|------|--------|
| 2026-07-22 | 42 | 42 | 0 | GSD security auditor; targeted remediation verified |

## Sign-Off

- [x] All threats have a disposition.
- [x] Accepted risks are documented in the Accepted Risks Log.
- [x] `threats_open: 0` confirmed.
- [x] `status: verified` set in frontmatter.

**Approval:** verified 2026-07-22
