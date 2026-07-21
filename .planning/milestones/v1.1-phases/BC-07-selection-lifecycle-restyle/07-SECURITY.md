---
phase: BC-07
slug: selection-lifecycle-restyle
status: verified
threats_open: 0
asvs_level: 1
created: 2026-07-21
---

# Phase BC-07 — Security

> Per-phase security contract: threat register, accepted risks, and audit trail.

---

## Trust Boundaries

| Boundary | Description | Data Crossing |
|----------|-------------|---------------|
| Browser → Blazor Server circuit | Pointer and click events arrive over SignalR; this phase adds no new inbound message type. | Pointer coordinates and button state |
| Database → SVG render | Stored figure type and integer coordinates are rendered in the selection overlay. | Figure type and coordinates |

---

## Threat Register

| Threat ID | Category | Component | Severity | Disposition | Mitigation | Status |
|-----------|----------|-----------|----------|-------------|------------|--------|
| T-07-01 | Tampering (markup injection) | `SelectionTrace.razor` | low | mitigate | Figure type is narrowed through `FigureTypeNames.Parse`; coordinates are integers; circles and triangles use typed geometry helpers and Razor attribute binding. | closed |
| T-07-02 | Information disclosure | Local selection state | low | mitigate | `selectedId` remains local UI state; no `SyncMessage` or database write carries selection. | closed |
| T-07-03 | Denial of service | Per-render SVG overlay | low | accept | Exactly one inert overlay is rendered only while a figure is selected; `pointer-events:none` prevents input interception. | closed |
| T-07-SC | Tampering (supply chain) | Package installation | low | accept | The phase added no packages or dependency changes. | closed |

*Status: open · closed · open — below high threshold (non-blocking)*

---

## Accepted Risks Log

| Risk ID | Threat Ref | Rationale | Accepted By | Date |
|---------|------------|-----------|-------------|------|
| AR-07-01 | T-07-03 | A single temporary inert SVG group has bounded rendering cost. | Phase threat model | 2026-07-21 |
| AR-07-02 | T-07-SC | No dependency installation or package surface change occurred. | Phase threat model | 2026-07-21 |

---

## Security Audit Trail

| Audit Date | Threats Total | Closed | Open | Run By |
|------------|---------------|--------|------|--------|
| 2026-07-21 | 4 | 4 | 0 | GSD secure-phase L1 workflow |

---

## Sign-Off

- [x] All threats have a disposition (mitigate / accept / transfer)
- [x] Accepted risks documented in Accepted Risks Log
- [x] `threats_open: 0` confirmed
- [x] `status: verified` set in frontmatter

**Approval:** verified 2026-07-21
