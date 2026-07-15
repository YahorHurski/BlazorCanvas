---
phase: BC-01-database-schema-geometry-core
plan: 06
subsystem: database
tags: [efcore, npgsql, design-time-tooling, dotnet-ef, dbcontextfactory]

# Dependency graph
requires:
  - phase: BC-01-database-schema-geometry-core (plan 01-03)
    provides: CanvasDbContextFactory, the D-27 port-5433 Docker Postgres setup, and appsettings.Development.json
provides:
  - CanvasDbContextFactory throws an actionable InvalidOperationException when ConnectionStrings:Canvas cannot be resolved, instead of silently guessing a hardcoded localhost:5432 connection string
affects: [any future phase that runs dotnet ef migrations add/update — the design-time tooling path]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Design-time factories fail loudly on missing config rather than falling back to a guessed connection string"

key-files:
  created: []
  modified:
    - src/BlazorCanvas/Data/CanvasDbContextFactory.cs

key-decisions:
  - "Removed the hardcoded Host=localhost;Port=5432;...;Username=postgres;Password=postgres fallback entirely — a config miss now throws instead of guessing"
  - "Added .AddEnvironmentVariables() to the ConfigurationBuilder chain so the ConnectionStrings__Canvas escape hatch named in the exception message actually works"
  - "Program.cs, docker-compose.yml, and appsettings.*.json left untouched — this is a design-time-only fix, the runtime DI registration was already correct"

patterns-established:
  - "Design-time DbContext factories must fail loudly (throw) on unresolved configuration, never fall back to a guessed connection value — especially on a host where multiple Postgres instances can coexist on different ports"

requirements-completed: [DATA-02]

coverage:
  - id: D1
    description: "CanvasDbContextFactory.CreateDbContext throws an actionable InvalidOperationException when ConnectionStrings:Canvas is unresolved, instead of falling back to a hardcoded localhost:5432 connection string (closes CR-03)"
    requirement: "DATA-02"
    verification:
      - kind: other
        ref: "dotnet build BlazorCanvas.sln (0 warnings, 0 errors); grep -c 'throw new InvalidOperationException' src/BlazorCanvas/Data/CanvasDbContextFactory.cs == 1; manual reproduction with appsettings.Development.json temporarily removed reproduced the throw with the exact actionable message, then file was restored byte-for-byte"
        status: pass
    human_judgment: false
  - id: D2
    description: "Regression: dotnet ef migrations list run from src/BlazorCanvas/ still succeeds against the port-5433 container (D-27 preserved), and the full test suite (153 tests) stays green with Program.cs byte-for-byte unchanged"
    requirement: "DATA-02"
    verification:
      - kind: other
        ref: "dotnet ef migrations list (from src/BlazorCanvas/) exit 0; dotnet test BlazorCanvas.sln — 153 passed, 0 failed; git diff --name-only lists only CanvasDbContextFactory.cs"
        status: pass
    human_judgment: false

duration: 5min
completed: 2026-07-15
status: complete
---

# Phase BC-01 Plan 06: Design-Time Factory Fails Loudly (CR-03 Closure) Summary

**CanvasDbContextFactory.CreateDbContext now throws an actionable InvalidOperationException instead of silently falling back to a hardcoded localhost:5432 connection string when ConnectionStrings:Canvas is unresolved.**

## Performance

- **Duration:** 5 min
- **Started:** 2026-07-15T07:54:48Z
- **Completed:** 2026-07-15T07:59:42Z
- **Tasks:** 1 completed
- **Files modified:** 1

## Accomplishments
- Closed CR-03 (01-REVIEW.md): the design-time factory no longer guesses a connection string when config is missing — it throws, naming the fix (`dotnet ef` from `src/BlazorCanvas/`, or set `ConnectionStrings__Canvas`) and warning explicitly that port 5432 on this machine belongs to a different PostgreSQL server (the native `postgresql-x64-18` service, D-27).
- Removed the hardcoded `Username=postgres;Password=postgres` credential literal that shipped with the old fallback (the secondary Information Disclosure concern noted in CR-03 / threat T-0106-02).
- Added `.AddEnvironmentVariables()` to the configuration chain so the `ConnectionStrings__Canvas` environment-variable escape hatch named in the exception message is actually functional.
- Proved the throw path fires correctly and the message is exactly as intended, by temporarily removing `appsettings.Development.json`, re-running the design-time command, observing the exact actionable message, then restoring the file byte-for-byte (verified via `cat`).
- Confirmed `Program.cs` is untouched (`git diff --name-only` for this plan lists only `CanvasDbContextFactory.cs`) and the full suite (153 tests) stays green.

## Task Commits

Each task was committed atomically:

1. **Task 1: Make the design-time factory fail loudly instead of guessing a connection string (CR-03)** - `ae9d772` (fix)

**Plan metadata:** (this commit)

## Files Created/Modified
- `src/BlazorCanvas/Data/CanvasDbContextFactory.cs` - Replaced the hardcoded `??` fallback connection string with a `throw new InvalidOperationException(...)`; added `.AddEnvironmentVariables()` to the config chain.

## Decisions Made
- Kept `SetBasePath(Directory.GetCurrentDirectory())` and both `AddJsonFile(..., optional: true)` calls exactly as the plan specified — the point of CR-03 is that the reachable "config not found" path now throws rather than falls back, not that the base-path strategy changes.
- No new hardcoded connection value was introduced anywhere as a replacement — the only "value" left in the file is the exception message text.

## Deviations from Plan

### Auto-fixed Issues

None - the code change matched the plan's drafted fix exactly (01-REVIEW.md CR-03, lines 160-185), adapted only to keep `SetBasePath(Directory.GetCurrentDirectory())` per the plan's explicit instruction (the review's illustrative snippet showed an alternate `Path.GetDirectoryName(...Assembly.Location)` base path, which the plan text did not request and which was not used).

---

**Total deviations:** 0 auto-fixed. No scope creep — the change is confined to the single file named in `files_modified`.

## Issues Encountered

**The plan's literal repo-root reproduction command did not fail as the acceptance criteria predicted, for a benign reason.** Running `dotnet ef migrations list --project src/BlazorCanvas --startup-project src/BlazorCanvas` from the repository root exited 0, not non-zero. Root cause investigated with a temporary diagnostic (`Console.Error.WriteLine(Directory.GetCurrentDirectory())` inside `CreateDbContext`, removed before committing): the installed `dotnet-ef` 10.0.5 tool's design-time operation executor anchors its working directory to the `--project` directory (`src/BlazorCanvas`) before invoking `IDesignTimeDbContextFactory.CreateDbContext` — this is EF Core 3.0+ tooling behavior, not a defect in this fix. Because of that, `Directory.GetCurrentDirectory()` inside the factory resolves to the project directory (where `appsettings.Development.json` lives) regardless of the invoking shell's CWD, whenever `--project`/`--startup-project` are supplied explicitly.

This does not weaken the fix: the throw path is real and correctly wired — it was proven directly by temporarily removing `appsettings.Development.json` (the actual source of truth for the connection string) and re-running the same command, which produced the exact actionable message, then restoring the file unchanged. The scenario CR-03 actually protects against (a machine or CI invocation where neither `appsettings.json` nor `appsettings.Development.json` nor `ConnectionStrings__Canvas` can be found) still throws loudly instead of silently targeting the wrong Postgres server. The specific repro recipe in the plan's acceptance criteria (cd to repo root, keep both `--project` flags) is simply not the way to trigger a "config truly absent" state with this tool version — direct removal of the config source is. This is noted here rather than silently declared "criterion met" since the literal command in the acceptance criteria did not reproduce the documented outcome.

Also note: bare `dotnet ef migrations list` with no `--project` flag at all, run from the repo root, already fails loudly today ("No project was found. Change the current working directory or use the --project option.") — a separate, pre-existing fail-loud guard from the `dotnet-ef` CLI itself, unrelated to this fix.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness
- CR-03 closed; all three Critical defects from 01-REVIEW.md (CR-01, CR-02, CR-03) are now resolved across plans 01-05 and 01-06.
- No blockers for BC-02 or subsequent phases.

---
*Phase: BC-01-database-schema-geometry-core*
*Completed: 2026-07-15*

## Self-Check: PASSED

- FOUND: src/BlazorCanvas/Data/CanvasDbContextFactory.cs
- FOUND: .planning/phases/BC-01-database-schema-geometry-core/01-06-SUMMARY.md
- FOUND: ae9d772 (Task 1 commit)
- FOUND: 16eeb07 (SUMMARY commit)
