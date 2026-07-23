---
phase: BC-14-catalog-seed-toolbar-decisions
plan: 03
subsystem: documentation
tags: [decisions, planning-intel, toolbar, star5, architecture]
requires:
  - phase: BC-14-catalog-seed-toolbar-decisions/14-01
    provides: Star5Shape is registered and registry-owned figure_types seed idempotently.
  - phase: BC-14-catalog-seed-toolbar-decisions/14-02
    provides: Tool.Star arms the toolbar star button and maps to star5.
provides:
  - docs/DECISIONS.md records D-70 through D-73 for star5 geometry, storage, catalog seed, and toolbar exposure.
  - Active toolbar guidance is seven controls with star between triangle and delete.
  - PROJECT.md and active planning intel mirror the ARCH-02 amendments without rewriting archived history.
affects: [BC-15-draw-preview-render-persist-star, BC-16-interaction-sync-test-guards, BC-17-regression-verification]
tech-stack:
  added: []
  patterns:
    - Keep docs/DECISIONS.md authoritative and mirror active-only changes into .planning/intel.
    - Use robust stale-count grep gates to prevent old toolbar counts from surviving in active docs.
key-files:
  created: []
  modified:
    - docs/DECISIONS.md
    - .planning/PROJECT.md
    - .planning/intel/decisions.md
    - .planning/intel/requirements.md
    - .planning/intel/constraints.md
key-decisions:
  - "D-70 locks star5 as the fifth stretchable, point-up, corner-to-corner five-pointed star."
  - "D-71 locks star geometry as ten ordered points plus required innerRatio, with points authoritative for render and bbox."
  - "D-72 locks registry-owned figure_types startup seed convergence for completed public catalogs."
  - "D-73 locks the seven-control toolbar order with Star between Triangle and Delete, while Logout remains a POST form outside the count."
patterns-established:
  - "Decision mirrors should cite docs/DECISIONS.md D-numbers and avoid treating historical milestone text as active requirements."
requirements-completed: [ARCH-02]
coverage:
  - id: D1
    description: "Authoritative decision log records D-70 through D-73 and the seven-control toolbar order."
    requirement: ARCH-02
    verification:
      - kind: other
        ref: "rg \"D-70|D-71|D-72|D-73|star5|\\[ pointer \\] \\[ line \\] \\[ rectangle \\] \\[ circle \\] \\[ triangle \\] \\[ star \\] \\[ delete \\]\" docs/DECISIONS.md"
        status: pass
      - kind: other
        ref: "powershell stale six-button gate for docs/DECISIONS.md"
        status: pass
    human_judgment: false
  - id: D2
    description: "PROJECT.md and active planning intel mirror the seven-control toolbar and D-70+ star decisions."
    requirement: ARCH-02
    verification:
      - kind: other
        ref: "rg \"D-70|star5|[pointer] [line] [rectangle] [circle] [triangle] [star] [delete]\" .planning/PROJECT.md .planning/intel/decisions.md .planning/intel/requirements.md .planning/intel/constraints.md"
        status: pass
      - kind: other
        ref: "powershell stale six-button gate for active planning docs"
        status: pass
    human_judgment: false
duration: 3min
completed: 2026-07-22
status: complete
---

# Phase BC-14 Plan 03: Decision Amendment Summary

**Authoritative star5 and seven-control toolbar decisions mirrored into active planning intel**

## Performance

- **Duration:** 3 min
- **Started:** 2026-07-22T20:15:43Z
- **Completed:** 2026-07-22T20:18:51Z
- **Tasks:** 2
- **Files modified:** 5

## Accomplishments

- Added D-70 through D-73 to `docs/DECISIONS.md`, covering star5 geometry, storage format, registry/catalog seed convergence, and the seven-control toolbar.
- Updated active decision-log toolbar guidance so Star sits between Triangle and Delete, Delete remains an action, and Logout remains outside the count as a POST form.
- Mirrored the same active facts into `.planning/PROJECT.md` and `.planning/intel/decisions.md`, `.planning/intel/requirements.md`, and `.planning/intel/constraints.md`.
- Preserved historical shipment statements as history while removing active stale toolbar-count instructions.

## Task Commits

1. **Task 1: Amend authoritative decisions for star5 and seven toolbar buttons** - `29b8d4b` (docs)
2. **Task 2: Mirror toolbar and star decisions into active planning intel** - `4d6601e` (docs)

## Files Created/Modified

- `docs/DECISIONS.md` - Adds D-70 through D-73 and updates active toolbar/logout amendments.
- `.planning/PROJECT.md` - Records the Phase 14 decision amendment in active milestone context while keeping older validation history historical.
- `.planning/intel/decisions.md` - Mirrors D-70 through D-73 and seven-control toolbar guidance.
- `.planning/intel/requirements.md` - Updates `REQ-toolbar` to seven controls with Star between Triangle and Delete.
- `.planning/intel/constraints.md` - Updates the active visual toolbar row to seven controls in the 48px strip.

## Decisions Made

- D-70 names `star5` as the fifth stretchable, point-up, corner-to-corner five-pointed star.
- D-71 makes the ten ordered points authoritative and keeps `innerRatio` required for parse symmetry.
- D-72 records registry-owned idempotent startup seeding for completed public catalogs.
- D-73 records the seven-control toolbar and keeps Logout outside the count with POST antiforgery semantics.

## Verification

- `rg "D-70|D-71|D-72|D-73|star5|\[ pointer \] \[ line \] \[ rectangle \] \[ circle \] \[ triangle \] \[ star \] \[ delete \]" docs/DECISIONS.md` - passed.
- `powershell -NoProfile -Command '$hits = rg -n --pcre2 "(?i)\b(six buttons|six-button|exactly six)\b" docs/DECISIONS.md; if ($LASTEXITCODE -eq 0) { $hits; exit 1 }; if ($LASTEXITCODE -eq 1) { exit 0 }; exit $LASTEXITCODE'` - passed with no matches.
- `rg "D-70|star5|\[pointer\] \[line\] \[rectangle\] \[circle\] \[triangle\] \[star\] \[delete\]|\[ pointer \] \[ line \] \[ rectangle \] \[ circle \] \[ triangle \] \[ star \] \[ delete \]" .planning/PROJECT.md .planning/intel/decisions.md .planning/intel/requirements.md .planning/intel/constraints.md` - passed.
- `powershell -NoProfile -Command '$files = @(".planning/PROJECT.md",".planning/intel/decisions.md",".planning/intel/requirements.md",".planning/intel/constraints.md"); $hits = rg -n --pcre2 "(?i)\b(six buttons|six-button|exactly six)\b" -- $files; if ($LASTEXITCODE -eq 0) { $hits; exit 1 }; if ($LASTEXITCODE -eq 1) { exit 0 }; exit $LASTEXITCODE'` - passed with no matches.
- Stub scan over all modified files for `not available`, `coming soon`, `placeholder`, `TODO`, and `FIXME` returned no matches.

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered

- One local stub-scan command had malformed regex quoting and failed before producing useful output. It was rerun with a simpler literal-pattern scan and returned no matches.

## Known Stubs

None.

## Threat Flags

None. This plan changed documentation and active planning mirrors only; no new network endpoint, auth path, file access pattern, schema change, or runtime trust boundary was introduced.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

Phase 14 is complete. Phase 15 can draw, preview, render, and persist stars against decisions that now match the registered `star5` type, startup catalog seed, and seven-control toolbar.

## Self-Check: PASSED

- Found `.planning/phases/BC-14-catalog-seed-toolbar-decisions/14-03-SUMMARY.md`.
- Found all five modified documentation/intel files.
- Found task commits `29b8d4b` and `4d6601e`.

---
*Phase: BC-14-catalog-seed-toolbar-decisions*
*Completed: 2026-07-22*
