---
phase: BC-12-regression-verification
slug: regression-verification
status: verified
threats_open: 0
asvs_level: 1
created: 2026-07-22
updated: 2026-07-22
---

# Phase BC-12 - Security

Per-phase security contract: threat register, accepted risks, and audit trail.

## Trust Boundaries

| Boundary | Description | Data Crossing |
|----------|-------------|---------------|
| Operator browser to local app | The human drives pointer, selection, drawing, dragging, deletion, and visual comparison in the app UI. | Pointer events, disposable account session, user-observed canvas state |
| Window A to notifier to Window B | Two normal same-profile browser windows share one disposable-user session and one app process for sync observation. | Committed figure create/delete/position messages |
| Operator to local database | Test data is created and cleaned up through UI-only paths. | Disposable canvas figures and account records |
| Local pointer gesture to circuit/browser-local state | Browser pointer input changes ephemeral preview state until the existing gateway validates a completed gesture. | In-progress preview geometry, not persisted data |
| Circuit state to cross-tab notifier | Only persisted canonical figures may cross tabs; preview geometry remains local. | Committed sync messages only |

## Threat Register

| Threat ID | Category | Component | Severity | Disposition | Mitigation | Status |
|-----------|----------|-----------|----------|-------------|------------|--------|
| 12-01:T-12-01 | Tampering | Acceptance data | medium | mitigate | Fresh disposable account, UI-only setup and cleanup, no SQL, no volume reset, and no real-account reuse were required by `12-01-PLAN.md`; UAT completed with no issues. | closed |
| 12-01:T-12-02 | Spoofing | Second window/session | medium | mitigate | The procedure requires normal same-profile windows, shared-session confirmation, one local app process, and no private windows; UAT completed the two-window checks. | closed |
| 12-01:T-12-03 | Repudiation | Acceptance outcome | low | accept | Summary/UAT artifacts record the failed and passed human outcomes, preflight evidence, and no password. | closed |
| 12-01:T-12-04 | Denial of service | Local app process | low | accept | Preflight build/test/container/certificate checks and retained process logs support diagnosis; no blocking DoS threat remains. | closed |
| 12-01:T-12-SC | Tampering | Dependency installation | low | accept | Acceptance plan installed no packages and made no product changes. | closed |
| 12-02:T-12-01 | Tampering | `Home.razor` draw handlers | medium | mitigate | `CanvasInteractionCoordinator.DrawAsync` remains the validation, persistence, and publication boundary; preview state has no repository or notifier dependency. | closed |
| 12-02:T-12-02 | Information disclosure | `CanvasSyncNotifier` cross-tab delivery | medium | mitigate | Regression tests and source contract show preview publication is absent and creation occurs only after completed draw commit; UAT confirmed the second tab stays unchanged until release. | closed |
| 12-02:T-12-03 | Denial of service | Pointer-move render path | low | accept | The final fix uses browser-local SVG preview updates without polling, packages, transport changes, or background work. | closed |
| 12-02:T-12-SC | Tampering | Dependency installation | high | mitigate | `12-02-SUMMARY.md` records `tech-stack.added: []`; the fix used existing C#, Razor, JavaScript, and xUnit surfaces only. | closed |

## Accepted Risks Log

| Risk ID | Threat Ref | Rationale | Accepted By | Date |
|---------|------------|-----------|-------------|------|
| AR-12-01 | 12-01:T-12-03 | Human acceptance evidence is procedural by design for REG-01; summaries and UAT preserve the observable outcome without recording secrets. | GSD workflow | 2026-07-22 |
| AR-12-02 | 12-01:T-12-04 | Local acceptance host availability risk is bounded to a development verification run and covered by retained logs. | GSD workflow | 2026-07-22 |
| AR-12-03 | 12-01:T-12-SC | Package-install tampering risk is closed by the acceptance-only scope and no dependency installation. | GSD workflow | 2026-07-22 |
| AR-12-04 | 12-02:T-12-03 | Pointer-move preview load is bounded to local SVG DOM updates and no new transport/background mechanism. | GSD workflow | 2026-07-22 |

## Security Audit Trail

| Audit Date | Threats Total | Closed | Open | Run By |
|------------|---------------|--------|------|--------|
| 2026-07-22 | 9 | 9 | 0 | Codex / gsd-secure-phase |

## Sign-Off

- [x] All threats have a disposition (mitigate / accept / transfer)
- [x] Accepted risks documented in Accepted Risks Log
- [x] `threats_open: 0` confirmed
- [x] `status: verified` set in frontmatter

**Approval:** verified 2026-07-22
