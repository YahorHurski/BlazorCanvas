---
phase: BC-11-renderer-sync-cutover
plan: 03
status: complete
completed: 2026-07-22
requirements-completed: [TEST-02]
---

# Phase 11 Plan 03: Cutover Summary

The runtime now starts through `V11Cutover`, which serializes catalog probing and performs legacy replay, schema promotion, and cleanup in one PostgreSQL transaction. Runtime repositories use only promoted public tables.

## Accomplishments

- Added the guarded public-schema cutover and moved migration-only DDL/replay helpers under `Data/V11/Transition`.
- Removed the retired EF figure entity/store, old geometry helpers, legacy bootstrap, and their obsolete tests.
- Simplified the EF migration model to users only; fresh storage is provisioned by cutover.
- Rebasing removed the outdated legacy schema assertions. The focused cutover tests and full solution suite pass.

## Verification

- `dotnet build BlazorCanvas.sln --nologo -v q` — passed with 0 warnings and 0 errors.
- `dotnet test BlazorCanvas.sln --nologo --filter "FullyQualifiedName~V11CutoverTests"` — 2 passed.
- `dotnet test BlazorCanvas.sln --nologo` — 276 passed, 0 failed, 0 skipped.

## Task Commits

1. `663c94a` — guarded transactional v1.11 schema promotion.
2. This summary's commit — removal and final verification work.
