---
phase: BC-02-login-session-logout
plan: 01
subsystem: ui
tags: [blazor, razor, css, scaffold-removal]

# Dependency graph
requires: []
provides:
  - "Bootstrap-free app shell (`App.razor`/`app.css`), no bundled Bootstrap CSS/JS in `wwwroot`"
  - "A blank pass-through `MainLayout` (`@Body` + `#blazor-error-ui` only)"
  - "`html, body { margin: 0; }` page reset (D-43 baseline for the 48px toolbar in 02-03)"
  - "Demo scaffold removed: NavMenu, Counter, Weather deleted; NotFound/Error retained"
affects: [02-02, 02-03]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Each page owns 100% of its own chrome via component-scoped `*.razor.css` — no shared sidebar/nav layout"

key-files:
  created: []
  modified:
    - src/BlazorCanvas/Components/App.razor
    - src/BlazorCanvas/wwwroot/app.css
    - src/BlazorCanvas/Components/Layout/MainLayout.razor
    - src/BlazorCanvas/Components/Layout/MainLayout.razor.css

key-decisions:
  - "app.css's old `html, body { font-family: ... }` rule (Bootstrap-era default) was replaced outright by the plan's `margin: 0` reset rather than merged/kept, since the plan's exhaustive keep-list did not include it and 02-UI-SPEC.md defines its own system-UI font stack for the login/toolbar surfaces built in 02-03."

requirements-completed: [AUTH-01, AUTH-03]

coverage:
  - id: D1
    description: "Bootstrap fully removed: wwwroot/lib/bootstrap bundle deleted, stylesheet link removed from App.razor, app.css stripped of every Bootstrap-derived rule (btn-primary, btn:focus, content, darker-border-checkbox, form-floating placeholder rules)"
    requirement: "AUTH-01"
    verification:
      - kind: other
        ref: "grep -c 'bootstrap' src/BlazorCanvas/Components/App.razor == 0; grep -c 'bs-' src/BlazorCanvas/wwwroot/app.css == 0; test ! -d src/BlazorCanvas/wwwroot/lib/bootstrap"
        status: pass
    human_judgment: false
  - id: D2
    description: "html, body { margin: 0; } page reset added to app.css, establishing the zero-margin baseline D-43's 48px toolbar depends on"
    requirement: "AUTH-03"
    verification:
      - kind: other
        ref: "grep -c 'margin: 0' src/BlazorCanvas/wwwroot/app.css == 1"
        status: pass
    human_judgment: false
  - id: D3
    description: "MainLayout gutted to a blank pass-through (@Body + #blazor-error-ui only); NavMenu/Counter/Weather demo scaffold deleted; NotFound/Error retained"
    requirement: "AUTH-01"
    verification:
      - kind: other
        ref: "grep -c 'NavMenu|sidebar' MainLayout.razor == 0; test ! -f NavMenu.razor/Counter.razor/Weather.razor; test -f NotFound.razor/Error.razor"
        status: pass
    human_judgment: false
  - id: D4
    description: "Solution still compiles (dotnet build BlazorCanvas.sln) after the scaffold strip"
    verification:
      - kind: other
        ref: "dotnet build BlazorCanvas.sln -c Debug --nologo -v q → 0 warnings, 0 errors"
        status: pass
    human_judgment: false

duration: 8min
completed: 2026-07-15
status: complete
---

# Phase BC-02 Plan 01: Drop the default Blazor scaffold Summary

**Stripped the default Blazor Web App template down to a blank pass-through shell — Bootstrap bundle deleted, `MainLayout` reduced to `@Body` + framework error-UI, and the NavMenu/Counter/Weather demo scaffold removed — clearing the way for 02-03's login card and toolbar to own their chrome entirely via `02-UI-SPEC.md` tokens.**

## Performance

- **Duration:** 8 min
- **Started:** 2026-07-15T18:43:00Z
- **Completed:** 2026-07-15T18:48:37Z
- **Tasks:** 2 completed
- **Files modified:** 4 modified, 4 deleted, ~44 Bootstrap bundle files deleted

## Accomplishments
- Removed the bundled `wwwroot/lib/bootstrap` directory and its stylesheet link from `App.razor`
- Reduced `app.css` to Blazor framework essentials (`.blazor-error-boundary`, `.validation-message`, `.valid.modified`, `.invalid`, `h1:focus`) plus a new `html, body { margin: 0; }` page reset (D-43 baseline)
- Gutted `MainLayout.razor` to `@Body` + the `#blazor-error-ui` block; `MainLayout.razor.css` now holds only the `#blazor-error-ui` rules
- Deleted the demo scaffold: `NavMenu.razor`(`.css`), `Counter.razor`, `Weather.razor` — closing the unauthenticated `/counter` and `/weather` demo routes (T-02-101)
- Confirmed `NotFound.razor`/`Error.razor` remain intact (wired to `Program.cs`'s status-code and exception-handler middleware)
- Verified `dotnet build BlazorCanvas.sln` succeeds after every change

## Task Commits

Each task was committed atomically:

1. **Task 1: Remove Bootstrap from the app shell** - `006ca69` (chore)
2. **Task 2: Gut MainLayout to a pass-through and delete the demo pages/nav** - `f86db84` (chore)

**Plan metadata:** (this commit)

## Files Created/Modified
- `src/BlazorCanvas/Components/App.razor` - removed the Bootstrap stylesheet `<link>`; app.css/BlazorCanvas.styles.css links retained
- `src/BlazorCanvas/wwwroot/app.css` - reduced to framework essentials + `html, body { margin: 0; }` reset
- `src/BlazorCanvas/wwwroot/lib/bootstrap/` - deleted (entire bundled Bootstrap CSS/JS directory)
- `src/BlazorCanvas/Components/Layout/MainLayout.razor` - gutted to `@Body` + `#blazor-error-ui`
- `src/BlazorCanvas/Components/Layout/MainLayout.razor.css` - reduced to only `#blazor-error-ui`/`.dismiss` rules
- `src/BlazorCanvas/Components/Layout/NavMenu.razor` / `.css` - deleted
- `src/BlazorCanvas/Components/Pages/Counter.razor` - deleted
- `src/BlazorCanvas/Components/Pages/Weather.razor` - deleted

## Decisions Made
- The pre-existing `html, body { font-family: 'Helvetica Neue', ... }` rule in `app.css` was dropped rather than kept or merged into the new `margin: 0` block. The plan's task action gave an exhaustive keep-list for `app.css` that did not include this rule, and `02-UI-SPEC.md` defines its own system-UI font stack for the surfaces 02-03 builds — carrying the old Bootstrap-era font stack forward would have been dead weight with no spec backing it.

## Deviations from Plan

None - plan executed exactly as written. Both tasks' acceptance criteria (build success, grep checks, file existence/non-existence checks) were verified and passed exactly as specified before each commit.

## Issues Encountered
None.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness
- `App.razor`, `app.css`, and `MainLayout` are now a clean, Bootstrap-free, chrome-free baseline that 02-02 (auth wiring) and 02-03 (Login/Home pages with component-scoped CSS) can build on directly per `02-UI-SPEC.md`.
- The `html, body { margin: 0; }` reset is in place, satisfying D-43's prerequisite for 02-03's full-viewport-width 48px toolbar.
- No blockers.

---
*Phase: BC-02-login-session-logout*
*Completed: 2026-07-15*

## Self-Check: PASSED

- FOUND: .planning/phases/BC-02-login-session-logout/02-01-SUMMARY.md
- FOUND: commit 006ca69
- FOUND: commit f86db84
