---
gsd_state_version: 1.0
milestone: v1.0
milestone_name: milestone
current_phase: 5
current_phase_name: Live Cross-Tab Sync
status: executing
stopped_at: Completed BC-05-01-PLAN.md
last_updated: "2026-07-16T22:01:32.575Z"
last_activity: 2026-07-16
last_activity_desc: Phase BC-04 complete, transitioned to Phase 5
progress:
  total_phases: 5
  completed_phases: 4
  total_plans: 23
  completed_plans: 19
---

# Project State

## Project Reference

See: .planning/PROJECT.md (updated 2026-07-14)

**Core value:** The canvas is always the truth, everywhere at once — what you draw persists instantly,
and every other tab shows it happening live, including a figure gliding in real time as you drag it.
**Current focus:** Phase 5 — Live Cross-Tab Sync

## Current Position

Phase: 5 — Live Cross-Tab Sync
Plan: Not started
Status: Ready to execute
Last activity: 2026-07-16 — Phase BC-04 complete, transitioned to Phase 5
Next: Phase 5 planning

Progress: [████████░░] 83% (Phase 4 complete) / [████████░░] 80% (milestone, 4 of 5 phases)

## Performance Metrics

**Velocity:**

- Total plans completed: 12
- Average duration: —
- Total execution time: —

**By Phase:**

| Phase | Plans | Total | Avg/Plan |
|-------|-------|-------|----------|
| BC-02 | 3 | - | - |
| BC-03 | 5 | - | - |
| BC-04 | 4 | - | - |

**Recent Trend:**

- Last 5 plans: —
- Trend: —

*Updated after each plan completion*
| Phase BC-01 P01 | 5min | 2 tasks | 71 files |
| Phase BC-01 P02 | 7min | 3 tasks | 12 files |
| Phase BC-01 P03 | 15min | 3 tasks | 10 files |
| Phase BC-01 P04 | 30min | 3 tasks | 5 files |
| Phase BC-01 P05 | 15min | 2 tasks | 4 files |
| Phase BC-01 P06 | 5min | 1 tasks | 1 files |
| Phase BC-02 P01 | 8min | 2 tasks | 8 files |
| Phase 02 P02 | 20min | 3 tasks | 3 files |
| Phase 02 P03 | 12min | 2 tasks | 5 files |
| Phase BC-03 P01 | 12min | 2 tasks | 4 files |
| Phase BC-03 P02 | 20min | 3 tasks | 4 files |
| Phase BC-03 P03 | 12min | 3 tasks | 4 files |
| Phase BC-03 P04 | 25min | 2 tasks | 3 files |
**Per-Plan Metrics:**

| Plan | Duration | Tasks | Files |
|------|----------|-------|-------|
| Phase 04 P03 | 50min | 3 tasks | 2 files |
| Phase BC-05 P01 | 35min | 3 tasks | 3 files |

## Accumulated Context

### Decisions

**All 58 ADR decisions (D-01…D-58) are LOCKED.** They are in PROJECT.md `<decisions>`; full text in
`.planning/intel/decisions.md` and `docs/DECISIONS.md`. They are **not open questions** — do not
re-litigate, re-ask, or "improve" them.

The ones most likely to be violated by accident:

- **D-22 (REVISED):** a circle is stored as its **inscribed square**, not centre + rim point. The
  original encoding is REVERSED and dead.

- **D-40:** a `move` broadcast is **UPDATE-ONLY, never insert.** D-11's original "idempotent upsert"
  was a **bug** — it resurrects deleted figures.

- **D-54:** mid-drag, a tab discards **ALL** incoming broadcasts, not just those about the dragged figure.
- **D-50:** the minimum-size guard is **per-type** — a zero-height *line* is legal (it is a horizontal
  line); a zero-height rectangle is not.

- **D-08:** plaintext passwords are **deliberate and locked**. Do not "fix" this.
- **No JavaScript anywhere** — load-bearing, not aesthetic. It is what forced D-18, D-33, D-37, D-57.
- [Phase BC-01]: docker-compose.yml publishes 5432:5432 (not loopback-bound) per explicit user decision D-27
- [Phase BC-01]: .NET 10's dotnet new sln defaults to .slnx -- regenerated with --format sln to satisfy plan requirement
- [Phase BC-01]: ClampDrawRadius rounds distance with MidpointRounding.AwayFromZero (10.5 -> 11) per D-24/D-29
- [Phase BC-01]: MinSizeGuard is a literal per-type transcription of the three CHECK constraints (D-50); horizontal/vertical lines legal, zero-height rectangle/triangle illegal
- [Phase BC-01]: D-27 port deviation (user-approved): Docker container host-published port moved 5432->5433; native postgresql-x64-18 Windows service permanently occupies 5432 on this dev machine. docs/DECISIONS.md D-27 text NOT amended -- open follow-up. — User explicitly chose to move the container port rather than touch the pre-existing native PostgreSQL 18 service
- [Phase BC-01]: GuardMirrorsChecksTests proves MinSizeGuard and the three CHECK constraints agree exactly across a 32-case matrix, in both directions (D-50) -- no disagreement found
- [Phase BC-01]: Volume-persistence proof (ROADMAP criterion 1, re-proven with real data) implemented as a real xUnit test that shells out to docker compose down/up -d --wait via Process.Start, not an external manual script
- [Phase BC-01]: Aligned tests/BlazorCanvas.Tests.csproj EF Core package versions to 10.0.10 to fix a CS1705 compile error surfaced by direct DbContextOptionsBuilder usage in test code (Rule 3 blocking fix, no new package installed)
- [Phase BC-01]: 01-05: The fix stays entirely in C# clamp maths (Movement.ClampDelta, CircleEncoding.ClampDrawRadius) -- no canvas-bounds CHECK constraint added, per locked D-36.
- [Phase BC-01]: Removed the hardcoded Host=localhost;Port=5432;...;Username=postgres;Password=postgres fallback entirely -- CanvasDbContextFactory now throws an actionable InvalidOperationException on missing ConnectionStrings:Canvas instead of guessing (closes CR-03)
- [Phase BC-01]: Added .AddEnvironmentVariables() to CanvasDbContextFactory's ConfigurationBuilder chain so the ConnectionStrings__Canvas escape hatch named in the exception message actually works
- [Phase BC-02]: app.css's old Bootstrap-era font-family rule was dropped, not merged, in favor of the plan's margin:0 reset — 02-UI-SPEC.md defines its own font stack for 02-03's surfaces
- [Phase 02]: IsPersistent left false at sign-in time (02-03) so the cookie stays a true session cookie per D-26 - ExpireTimeSpan=365d only bounds server-side ticket validity
- [Phase 02]: UseAuthentication/UseAuthorization inserted directly before the pre-existing UseAntiforgery() call, matching RESEARCH Pitfall 4's exact ordering requirement
- [Phase 02]: POST /logout uses Results.LocalRedirect (never a caller-supplied target) so an open redirect is structurally impossible
- [Phase 02]: AntiforgeryStateProvider.GetAntiforgeryToken() field name is FormFieldName, not Name as 02-RESEARCH.md's Pattern 3 showed - verified against the installed .NET 10.0.9 assembly and corrected in Home.razor (02-03)
- [Phase BC-03]: DrawGesture never calls MinSizeGuard.IsDrawable -- that decision belongs to the caller (plan 03-05), so a not-yet-drawable gesture can still render a live preview (D-35, D-50)
- [Phase BC-03]: Circle centre/radius is computed from clamped press/cursor points then passed through CircleEncoding only, never Normalisation.Normalise, preserving the even-sided guarantee
- [Phase BC-03]: DbContext lifetime (IDbContextFactory vs scoped) was engineering discretion — docs/DECISIONS.md is silent on it — short-lived per-call contexts chosen to avoid captive-dependency and cross-circuit staleness in the InteractiveServer circuit
- [Phase BC-03]: Test-side IDbContextFactory<CanvasDbContext> adapter is a hand-written nested class over DatabaseFixture.CreateContext(), not a DI/mocking package (D-49 test-project scope cap)
- [Phase BC-03]: [Phase BC-03] Tool is a separate enum from FigureType; Pointer is first so default(Tool) == Tool.Pointer (D-31); no Tool.Delete member since deletion is an action button, not an armable mode (D-33)
- [Phase BC-03]: [Phase BC-03] FigureShape opacity uses a computed OpacityValue property rather than duplicating shape markup per preview state, keeping fill/stroke attribute counts exact while still toggling opacity
- [Phase BC-03]: [Phase BC-03] Toolbar's Logout submit button carries both tool-button and logout-button CSS classes so it inherits base icon-button styling instead of re-declaring border/size rules
- [Phase BC-03]: Canvas surface shape-armed cursor class named 'shape-armed' verbatim, matching plan terminology
- [Phase BC-03]: app.css html,body rule kept strictly additive (background added, margin: 0 untouched) so Phase 2's shipped login page styling is not disturbed

### Pending Todos

None yet.

### Blockers/Concerns

**None. Both items raised at ingest are closed.**

- ✅ **RESOLVED — the D-11 / D-54 contradiction.** `INGEST-CONFLICTS.md` raised one WARNING: D-11's
  checklist item 4 summarised D-54 *backwards*, claiming D-54 had narrowed the mid-drag receive filter
  to only the dragged figure. D-54 decides the **opposite** and lists the narrow filter under
  *Rejected*. **Fixed at source** in `docs/DECISIONS.md` — D-11's item 4 now states the blanket rule
  and matches D-54. The rule is: **mid-drag, a tab discards ALL incoming broadcasts.** Confirmed by
  the user; this was the option they explicitly chose.

- ✅ **RESOLVED — `.planning/config.json`** now exists (`granularity: standard`, `project_code: BC`).

## Deferred Items

Items acknowledged and carried forward from previous milestone close:

| Category | Item | Status | Deferred At |
|----------|------|--------|-------------|
| *(none)* | | | |

## Session Continuity

Last session: 2026-07-16T22:01:31.874Z
Stopped at: Completed BC-05-01-PLAN.md
Resume file: None
