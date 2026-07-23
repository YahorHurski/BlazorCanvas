---
phase: BC-12-regression-verification
plan: "01"
subsystem: regression-verification
tags: [blazor-server, svg-canvas, local-https, manual-acceptance, live-sync]
requires:
  - phase: BC-11-renderer-sync-cutover
    provides: Final-public v1.11 renderer, persistence, and UUID position-sync cutover.
provides:
  - Documented failed REG-01 human acceptance outcome and preserved diagnostic locations.
affects: [REG-01, regression follow-up, canvas drawing preview]
tech-stack:
  added: []
  patterns:
    - Human acceptance failures preserve the first divergent observation without corrective implementation.
key-files:
  created:
    - .planning/phases/BC-12-regression-verification/12-01-SUMMARY.md
  modified: []
key-decisions:
  - "REG-01 is not approved: a local in-progress drawing preview is required in the originating tab, while the second tab must remain commit-only."
patterns-established:
  - "A pre-commit draw preview is local-only and must never be sent through cross-window synchronization."
requirements-completed: []
coverage:
  - id: D1
    description: Human REG-01 acceptance for indistinguishable four-shape canvas behavior and two-window live glide.
    requirement: REG-01
    verification:
      - kind: manual_procedural
        ref: "BC-12 Task 2 human acceptance run on 2026-07-22"
        status: fail
    human_judgment: true
    rationale: "The required visual behavior was directly observed to diverge during drawing."
duration: 15min
completed: 2026-07-22
status: complete
---

# Phase BC-12 Plan 01: Regression Verification Summary

**Automated preflight passed, but REG-01 human acceptance was not approved because the originating tab shows no local in-progress preview while drawing a new figure.**

## Performance

- **Duration:** 15 min
- **Started:** 2026-07-22T15:00:00Z
- **Completed:** 2026-07-22T15:15:55Z
- **Tasks:** 1 automated task completed; 1 human-verification task concluded as failed
- **Files modified:** 1

## Accomplishments

- Docker Compose reached a healthy state; the solution built with zero warnings/errors and all 296 tests passed.
- A single trusted-HTTPS local acceptance host served `https://localhost:7281/login` successfully for the two-window run.
- The first human-visible divergence was captured precisely, without product, schema, migration, test, or database changes.

## Acceptance Outcome

**Result: NOT APPROVED — REG-01 remains incomplete.**

The first failed observation occurred while drawing a new figure in the originating tab:

- **Observed:** no local in-progress preview was visible while the pointer was down; the completed figure appeared only after mouse release.
- **Required:** the originating tab must show the in-progress drawing preview while drawing.
- **Cross-window boundary:** that preview must stay local-only. It must not be broadcast or shown in the second tab, which must receive the figure only after creation commits.
- **Other user report:** the application otherwise worked during the run.

The acceptance script stopped at this first failure. No corrective implementation or re-run was attempted in this phase.

## Preflight and Evidence

Automated preflight passed before the human run:

- `docker compose up -d --wait` — `canvas-postgres` healthy.
- `dotnet build BlazorCanvas.sln --nologo -v q` — 0 warnings, 0 errors.
- `dotnet test BlazorCanvas.sln --nologo` — 296 passed, 0 failed, 0 skipped.
- `dotnet dev-certs https --check --trust` — trusted localhost certificate found.
- One local `dotnet run --project src/BlazorCanvas --launch-profile https --no-build` host returned HTTP 200 for `https://localhost:7281/login`.

Retained logs from this run:

- `C:\Users\EGOR\AppData\Local\Temp\BlazorCanvas-BC12-8bd6ba0d-624c-4392-9261-4b9ecc6fdd59\01-docker-compose.log`
- `C:\Users\EGOR\AppData\Local\Temp\BlazorCanvas-BC12-8bd6ba0d-624c-4392-9261-4b9ecc6fdd59\02-dotnet-build.log`
- `C:\Users\EGOR\AppData\Local\Temp\BlazorCanvas-BC12-8bd6ba0d-624c-4392-9261-4b9ecc6fdd59\03-dotnet-test.log`
- `C:\Users\EGOR\AppData\Local\Temp\BlazorCanvas-BC12-8bd6ba0d-624c-4392-9261-4b9ecc6fdd59\04-dev-certs.log`
- `C:\Users\EGOR\AppData\Local\Temp\BlazorCanvas-BC12-8bd6ba0d-624c-4392-9261-4b9ecc6fdd59\app.stdout.log`
- `C:\Users\EGOR\AppData\Local\Temp\BlazorCanvas-BC12-8bd6ba0d-624c-4392-9261-4b9ecc6fdd59\app.stderr.log`

No screenshot or recording path was supplied with the user report. Preserve any locally captured browser evidence with the failed-run account/figures for the authorized follow-up.

## Task Commits

1. **Task 1: Run automated preflight and start one local acceptance host** — no commit (verification-only; logs are outside the repository).
2. **Task 2: Human-verify the full four-shape and two-window regression script** — no commit (failed acceptance; no implementation authorized).

**Plan metadata:** recorded in the commit containing this summary only.

## Files Created/Modified

- `.planning/phases/BC-12-regression-verification/12-01-SUMMARY.md` — failed human acceptance result, preflight evidence, and follow-up boundary.

## Decisions Made

- Do not treat the v1.11 rewrite as REG-01-approved. An originating-tab drawing preview is a visible requirement.
- Keep preview state local to its originating tab; the second tab receives only committed figure creation.
- Preserve the first failure and do not implement a correction without a separately authorized follow-up plan.

## Deviations from Plan

None — the plan explicitly required stopping at the first failed human observation, preserving evidence, and performing no corrective implementation.

## Issues Encountered

- **Task 2 human acceptance failed:** no in-progress local figure preview appeared during drawing; the figure appeared only after mouse release. This blocks REG-01 approval.

## User Setup Required

None — no external service configuration is required.

## Next Phase Readiness

- A separately authorized follow-up must restore the local-only drawing preview and prove that it is never broadcast to the second tab before commit.
- REG-01 remains incomplete; rerun the full human two-window acceptance script after the follow-up is independently verified.

## Self-Check: PASSED

- This summary exists at the required phase path.
- The preflight logs and acceptance-host logs are retained at the paths above.
- No product, schema, migration, test, or direct database changes were made by this phase.

---
*Phase: BC-12-regression-verification*
*Completed: 2026-07-22*
