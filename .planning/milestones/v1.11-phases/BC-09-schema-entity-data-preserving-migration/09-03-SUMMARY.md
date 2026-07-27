---
phase: 09-schema-entity-data-preserving-migration
plan: 03
subsystem: data
tags: [ef-core, postgres, jsonb, blazor, sync]
requires:
  - phase: 09-01
    provides: GeometryCodec
provides:
  - Production model compiled on anchor+geometry storage
  - Guid figure ids through store, sync, and Home.razor
affects: [migrations, tests, sync, canvas]
tech-stack:
  added: []
  patterns:
    - Figure Box conversion at persistence and broadcast boundaries
key-files:
  created: []
  modified:
    - src/BlazorCanvas/Data/Figure.cs
    - src/BlazorCanvas/Data/CanvasDbContext.cs
    - src/BlazorCanvas/Data/FigureStore.cs
    - src/BlazorCanvas/Sync/SyncMessage.cs
    - src/BlazorCanvas/Components/Pages/Home.razor
key-decisions:
  - "Figure.Geometry remains a string mapped to jsonb so compact GeometryCodec JSON is stored directly."
  - "FigureStore.UpdateAsync reads the existing figure type before encoding the replacement box, preserving the public update signature."
patterns-established:
  - "Render/sync still use Box payloads in Phase 9; storage conversion happens through GeometryCodec."
requirements-completed: [STOR-01]
coverage:
  - id: D1
    description: "The production app compiles on Guid id, x/y anchor, geometry jsonb, and z storage fields."
    requirement: STOR-01
    verification:
      - kind: other
        ref: "dotnet build src/BlazorCanvas/BlazorCanvas.csproj"
        status: pass
    human_judgment: false
duration: 18 min
completed: 2026-07-23
status: complete
---

# Phase 09 Plan 03: Production Model Summary

**The app project now compiles on Guid figures stored as anchor coordinates plus geometry JSON.**

## Performance

- **Duration:** 18 min
- **Started:** 2026-07-23T18:52:00Z
- **Completed:** 2026-07-23T19:10:00Z
- **Tasks:** 3
- **Files modified:** 5

## Accomplishments

- Reworked `Figure` and `CanvasDbContext` to the D-59 model: Guid `id`, integer `x/y`, `geometry` jsonb, numeric `z`, type whitelist CHECK, and `(user_id, z)` index.
- Adapted `FigureStore` to load by `z, id`, append z on insert, and encode updates through `GeometryCodec`.
- Retyped `SyncMessage` and `Home.razor` to Guid ids while preserving the current box-shaped sync payload and current draw/drag/delete control flow.

## Task Commits

1. **Task 1: Rewrite the Figure entity and CanvasDbContext figures mapping** - `75dd163` (feat)
2. **Task 2: Adapt FigureStore and SyncMessage** - `b6c5d05` (feat)
3. **Task 3: Adapt Home.razor** - `19f30f2` (feat)

**Plan metadata:** committed separately by GSD close-out.

## Files Created/Modified

- `src/BlazorCanvas/Data/Figure.cs` - New anchor+geometry entity fields.
- `src/BlazorCanvas/Data/CanvasDbContext.cs` - D-59 EF mapping.
- `src/BlazorCanvas/Data/FigureStore.cs` - Codec-based persistence and z ordering.
- `src/BlazorCanvas/Sync/SyncMessage.cs` - Guid ids with retained box payload.
- `src/BlazorCanvas/Components/Pages/Home.razor` - Guid state and codec conversion helpers.

## Decisions Made

- Kept Phase 9 sync messages box-shaped as planned; Phase 10 owns the anchor+geometry payload rework.

## Deviations from Plan

None - plan executed exactly as written.

**Total deviations:** 0 auto-fixed. **Impact on plan:** None.

## Issues Encountered

None.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

Ready for 09-04 to generate and hand-correct the EF migration against this model.

---
*Phase: 09-schema-entity-data-preserving-migration*
*Completed: 2026-07-23*
