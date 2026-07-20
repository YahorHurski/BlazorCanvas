---
phase: BC-05-live-cross-tab-sync
plan: 02
subsystem: infra
tags: [blazor-server, dependency-injection, ef-core, npgsql, postgres, retry]
requires:
  - phase: BC-05-01
    provides: SyncMessage and CanvasSyncNotifier
provides:
  - CanvasSyncNotifier registered as a process-wide singleton
  - Shared DbContext factory configured with bounded Npgsql transient retries
  - Program.cs comments documenting the load-bearing singleton lifetime and D-52 retry boundary
affects: [BC-05-03, BC-05-04, sync, persistence]
tech-stack:
  added: []
  patterns:
    - DI singleton notifier bridges Blazor Server circuits for one process
    - Npgsql provider execution strategy owns transient retry classification
key-files:
  created:
    - .planning/phases/BC-05-live-cross-tab-sync/05-02-SUMMARY.md
  modified:
    - src/BlazorCanvas/Program.cs
key-decisions:
  - "CanvasSyncNotifier is registered as Singleton, not Scoped, because each browser tab is a separate Blazor Server circuit/DI scope."
  - "The retry policy is provider configuration only: maxRetryCount 2, maxRetryDelay 200ms, errorCodesToAdd null, with no save-path classifier code."
patterns-established:
  - "Register cross-tab sync infrastructure in Program.cs beside the FigureStore service lifetime distinction."
  - "Apply D-52 retry policy once on AddDbContextFactory so FigureStore methods inherit it without local retry loops."
requirements-completed: [SYNC-01, DATA-04]
coverage:
  - id: D1
    description: "CanvasSyncNotifier resolves as one process-wide singleton shared across circuits."
    requirement: SYNC-01
    verification:
      - kind: other
        ref: "dotnet build BlazorCanvas.sln"
        status: pass
      - kind: other
        ref: "source assertions for AddSingleton/AddScoped/AddTransient/using"
        status: pass
      - kind: manual_procedural
        ref: "dotnet run --project src/BlazorCanvas after docker compose restart; Now listening logged"
        status: pass
    human_judgment: false
  - id: D2
    description: "The shared DbContext factory retries transient database failures at most 2 additional times with a 200ms cap."
    requirement: DATA-04
    verification:
      - kind: other
        ref: "dotnet build BlazorCanvas.sln"
        status: pass
      - kind: unit
        ref: "dotnet test BlazorCanvas.sln (405 tests)"
        status: pass
      - kind: other
        ref: "source assertions for EnableRetryOnFailure/maxRetryCount/maxRetryDelay/errorCodesToAdd/connection string"
        status: pass
    human_judgment: false
  - id: D3
    description: "The manual drag-while-database-stopped timing check remains a human-observable interaction gate."
    requirement: DATA-04
    verification: []
    human_judgment: true
    rationale: "The plan's delay proof requires interacting with the running UI while stopping the database container; automated build/test/source/boot gates passed, but no browser interaction was performed in this executor run."
duration: 35min
completed: 2026-07-17
status: complete
---

# Phase BC-05 Plan 02: Program Sync Registration and Retry Policy Summary

**Program.cs now wires the process-wide sync notifier and the bounded Npgsql retry strategy that later cross-tab and rollback plans depend on.**

## Performance

- **Duration:** 35 min
- **Started:** 2026-07-16T23:34:00+02:00
- **Completed:** 2026-07-17T00:08:45+02:00
- **Tasks:** 2
- **Files modified:** 1

## Accomplishments

- Registered `CanvasSyncNotifier` exactly once as a Singleton, beside the existing scoped `FigureStore` registration.
- Configured the existing `AddDbContextFactory<CanvasDbContext>`/`UseNpgsql` call with `EnableRetryOnFailure(maxRetryCount: 2, maxRetryDelay: TimeSpan.FromMilliseconds(200), errorCodesToAdd: null)`.
- Preserved `FigureStore.cs`, package files, JavaScript files, and the startup migration loop unchanged.
- Verified the app boots after a Docker PostgreSQL restart and logs `Now listening on` after migrations run.

## Task Commits

1. **Task 1: Register CanvasSyncNotifier as a Singleton** - `bd1f7be` (feat)
2. **Task 2: Bound transient retries at 2** - `94ad333` (feat)

**Plan metadata:** pending final docs commit

## Files Created/Modified

- `src/BlazorCanvas/Program.cs` - Added `BlazorCanvas.Sync`, the singleton notifier registration, and the provider-level retry configuration/commentary.
- `.planning/phases/BC-05-live-cross-tab-sync/05-02-SUMMARY.md` - Plan execution summary.

## Verification

- `dotnet build BlazorCanvas.sln` - PASS, 0 warnings, 0 errors.
- `dotnet test BlazorCanvas.sln` - PASS, 405 passed, 0 failed, 0 skipped.
- Source assertions - PASS for singleton count, scoped/transient notifier prohibition, existing `FigureStore` scoped count, Sync using, retry count, retry delay, `errorCodesToAdd: null`, and unchanged connection-string source.
- `git diff -- src/BlazorCanvas/Data/FigureStore.cs` - PASS, no output.
- `git diff --stat HEAD -- '*.csproj'` - PASS, no package changes.
- `git diff --stat HEAD -- '*.js'` - PASS, no JavaScript changes.
- Boot gate: `docker compose down && docker compose up -d`, then `dotnet run --project src/BlazorCanvas` - PASS, migrations checked and `Now listening on: http://localhost:5054` logged.

## Decisions Made

- Followed D-11 literally: the notifier lifetime is Singleton because the DI lifetime is the cross-tab bridge.
- Followed D-52 literally: provider retry configuration owns transient classification; no save-path retry loop or classifier was added.
- Kept the startup migration retry loop unchanged because it handles container readiness and complements the mid-session execution strategy.

## Deviations from Plan

### Auto-fixed Issues

None - plan implementation followed the requested scope.

---

**Total deviations:** 0 auto-fixed.
**Impact on plan:** No scope expansion; the only changed source file is `Program.cs`.

## Issues Encountered

- The Windows sandbox helper was unavailable (`codex-windows-sandbox-setup.exe` missing), so file edits and some read-only assertions used approved unsandboxed PowerShell commands.
- The literal `SqlState|IsTransient` recursive gate is already conflicted by pre-existing `src/BlazorCanvas/Components/Pages/Login.razor` username uniqueness handling. This plan added no save-path classifier code and Program.cs contains neither literal.
- The drag/drop retry-delay proof was not exercised through a browser. Automated build/test/source gates and the cold-container boot gate passed.

## User Setup Required

None - no external service configuration required.

## Known Stubs

None.

## Threat Flags

None beyond the plan threat model: the singleton DI boundary and database retry boundary were the intended surfaces and were mitigated as planned.

## Next Phase Readiness

Ready for 05-03. `CanvasSyncNotifier` is available as a singleton and every `FigureStore` call now uses the shared factory retry strategy without modifying `FigureStore.cs`.

## Self-Check: PASSED

- Found `src/BlazorCanvas/Program.cs`.
- Found `.planning/phases/BC-05-live-cross-tab-sync/05-02-SUMMARY.md`.
- Found task commits `bd1f7be` and `94ad333` in git history.
- Verified only `Program.cs` changed in production commits.

---
*Phase: BC-05-live-cross-tab-sync*
*Completed: 2026-07-17*