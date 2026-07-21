---
phase: BC-08-architecture-constraint-cleanup
plan: "01"
subsystem: documentation
tags: [adr, constraints, policy, verification]
requires:
  - phase: BC-07-selection-lifecycle-restyle
    provides: Completed v1.1 selection work with no JavaScript or interop change
provides:
  - ADR-aligned derived runtime constraint with the retired JavaScript restriction removed
  - Classified policy audit and documentation-only regression evidence
affects: [future-planning, architecture-constraints]
tech-stack:
  added: []
  patterns: [authoritative-ADR reconciliation, documentation-only diff allowlist]
key-files:
  created: [.planning/phases/BC-08-architecture-constraint-cleanup/08-01-SUMMARY.md]
  modified: [docs/DECISIONS.md, .planning/intel/constraints.md]
key-decisions:
  - "The former application-authored JavaScript prohibition is retired; JavaScript or interop needs a later affirmative decision."
  - "D-06, D-18, D-33, D-37, and D-57 retain their independent product or MVP-simplicity rationales."
patterns-established:
  - "Derived constraints must cite and agree with the authoritative ADR, while historical policy evidence remains explicitly contextualized."
requirements-completed: [ARCH-01]
coverage:
  - id: D1
    description: "The authoritative ADR, project summary, and derived constraint describe the retired JavaScript restriction as permissive without altering the application surface."
    requirement: ARCH-01
    verification:
      - kind: other
        ref: "repository policy-family audit plus documentation-only diff allowlist"
        status: pass
      - kind: other
        ref: "dotnet build BlazorCanvas.sln; dotnet test BlazorCanvas.sln --nologo"
        status: pass
    human_judgment: false
duration: 0min
completed: 2026-07-21
status: complete
---

# Phase BC-08 Plan 01: Architecture Constraint Cleanup Summary

**The derived runtime constraint now mirrors the ADR's permissive retired-JavaScript policy, with no application-surface change.**

## Performance

- **Duration:** 15min
- **Started:** 2026-07-21T12:55:00Z
- **Completed:** 2026-07-21T13:10:06Z
- **Tasks:** 2
- **Files modified:** 3

## Accomplishments

- Replaced the stale `CONSTRAINT-env` hard prohibition with the ADR-aligned retired, permissive policy and documentation-only scope fence.
- Corrected D-11's residual claim that D-06 rules out JS interop; its rejection now rests on duplicate synchronization paths and no product benefit.
- Proved the cleanup remains documentation-only through a repository policy audit, scoped-diff gate, clean build, and full test suite.

## Task Commits

Each implementation task was committed atomically:

1. **Task 1: Reconcile active policy artifacts with the ADR amendment** - `8fe79fc` (docs)
2. **Task 2: Run the classified policy audit and documentation-only regression gates** - verification only; no file changes

## Files Created/Modified

- `docs/DECISIONS.md` - removes the remaining active D-11 interop prohibition rationale.
- `.planning/intel/constraints.md` - replaces the contradictory `CONSTRAINT-env` rule with the current permissive policy.
- `.planning/phases/BC-08-architecture-constraint-cleanup/08-01-SUMMARY.md` - records audit classifications and close-out evidence.

## Policy Audit Classifications

Command executed:

```powershell
rg -n -i --hidden --no-ignore --glob '!.git/**' --glob '!**/bin/**' --glob '!**/obj/**' --glob '!.vs/**' 'no hand-authored JavaScript|no JS|hand-authored|no application-authored JavaScript|no JavaScript anywhere' .
```

Every hit was inspected in context and classified as follows:

| Match group | Classification | Outcome |
|---|---|---|
| `docs/DECISIONS.md`, `.planning/PROJECT.md`, `.planning/intel/constraints.md`, `.planning/STATE.md`, `.planning/intel/{SYNTHESIS,decisions,requirements}.md` | Permissive, retired, or superseded | Retained; no active prohibition remains. |
| `.planning/REQUIREMENTS.md`, `.planning/ROADMAP.md`, and Phase BC-08 plan/research artifacts | Current phase requirement or audit evidence | Retained; they describe the retirement and verification work, not an active restriction. |
| `.planning/backlog/v1.2-figures-and-toolbar.md` and Phase BC-07 plan artifacts | Permissive future or phase-scope wording | Retained; they explicitly permit JS/interop while adding none in their scoped phase. |
| `.planning/RETROSPECTIVE.md`, `.planning/milestones/v1.0-*`, and archived v1.0 phase artifacts | Historical or superseded evidence | Retained as immutable v1.0 context. |
| Archived UI/spec references to hand-authored SVG, Razor, or CSS | Unrelated lexical match | Retained; they make no JavaScript-policy assertion. |

The one active contradiction found during the audit was D-11's sentence that D-06 rules out JS interop. It was corrected in the scoped ADR artifact before the audit passed.

## Verification Evidence

1. Policy audit above: passed; every retained match is permissive, historical, superseded, phase-scoped, or unrelated to the policy.
2. Source/test policy check: `git grep -n -i -E 'no hand-authored JavaScript|no JS|hand-authored' -- src tests` returned no matches.
3. Documentation-only scope gate before Task 1 commit: changed paths were only `docs/DECISIONS.md` and `.planning/intel/constraints.md`; no `src/`, `tests/`, `wwwroot/`, `.cs`, `.razor`, `.csproj`, or `.js` paths were present. The post-task `git diff --name-only` was clean.
4. `dotnet build BlazorCanvas.sln`: passed with 0 warnings and 0 errors.
5. `dotnet test BlazorCanvas.sln --nologo`: passed, 405 passed / 0 failed / 0 skipped.

## Decisions Made

- Kept JavaScript/interop permissive rather than prescriptive: a future product decision must select any concrete use.
- Preserved the fixed canvas, toolbar Delete, drag termination, and draw-completion behaviour for their independent MVP or behavioural rationales.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Documentation defect] Removed D-11's obsolete D-06 interop prohibition**
- **Found during:** Task 1 policy reconciliation
- **Issue:** The authoritative ADR still said client-side tab sync was rejected because D-06 ruled out JS interop.
- **Fix:** Retained the rejection but grounded it in duplicate synchronization paths and lack of product benefit.
- **Files modified:** `docs/DECISIONS.md`
- **Verification:** Policy audit found no remaining active retired-policy contradiction.
- **Committed in:** `8fe79fc`

---

**Total deviations:** 1 auto-fixed documentation defect.
**Impact on plan:** Necessary to satisfy ARCH-01's repository-wide active-policy consistency; no scope expansion beyond the approved documentation artifacts.

## Issues Encountered

- The sandbox initially could not read the user NuGet configuration; the verification command was rerun with the required local access.
- Database integration tests initially failed because the existing Docker Desktop/PostgreSQL service was stopped. Docker Desktop and the repository's existing Compose container were started, then the exact test command passed.
- The legacy `state.advance-plan` handler could not parse the existing `Plan: Not started` field; all supported state handlers ran, then the affected current-position and decision labels were reconciled in `STATE.md`.

## User Setup Required

None - the existing local PostgreSQL test dependency was started for verification only.

## Next Phase Readiness

- ARCH-01 has reproducible documentation-only and regression evidence.
- v1.1 is ready for milestone-level verification or shipping review.

## Self-Check: PASSED

- Found `.planning/phases/BC-08-architecture-constraint-cleanup/08-01-SUMMARY.md`.
- Found Task 1 commit `8fe79fc` in repository history.

---
*Phase: BC-08-architecture-constraint-cleanup*
*Completed: 2026-07-21*
