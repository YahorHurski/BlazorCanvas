---
phase: 02-login-session-logout
plan: 02
subsystem: auth
tags: [cookie-auth, aspnet-core, blazor-server, antiforgery, csrf, minimal-api, xunit, tdd]

# Dependency graph
requires:
  - phase: BC-01
    provides: CanvasDbContext / users table (case-sensitive UNIQUE index on username), Normalisation.cs convention (single-source static-utility pattern)
provides:
  - Cookie-authentication backbone (AddAuthentication/AddCookie, LoginPath=/login)
  - AddAuthorization() + AddCascadingAuthenticationState() service registration
  - Correct middleware order: UseAuthentication -> UseAuthorization -> UseAntiforgery
  - POST /logout minimal-API endpoint (antiforgery-validated, SignOutAsync, LocalRedirect)
  - UsernameNormalizer.Normalize(string?) - single source of the D-44 trim+lowercase rule
affects: [02-03 (login page + toolbar/logout form), any future phase touching Program.cs middleware order]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Auth/ namespace mirrors Geometry/'s convention: static class, one static method, XML-doc citing the ADR it enforces, no DI/instance state"
    - "Minimal-API endpoints require explicit IAntiforgery.ValidateRequestAsync - not auto-protected like EditForm/Razor Components endpoints"

key-files:
  created:
    - src/BlazorCanvas/Auth/UsernameNormalizer.cs
    - tests/BlazorCanvas.Tests/Auth/UsernameNormalizerTests.cs
  modified:
    - src/BlazorCanvas/Program.cs

key-decisions:
  - "IsPersistent is left at its default (false) at sign-in time (02-03 will not change this) so the cookie is a true session cookie per D-26 - ExpireTimeSpan=365d only bounds server-side ticket validity"
  - "UseAuthentication()/UseAuthorization() inserted directly before the pre-existing UseAntiforgery() call, not at the top of the pipeline, matching RESEARCH Pitfall 4's exact ordering requirement"
  - "POST /logout uses Results.LocalRedirect (never Results.Redirect with a caller-supplied target) to make an open redirect structurally impossible"

patterns-established:
  - "Single-source normalizer pattern (Auth/UsernameNormalizer.cs) mirrors Geometry/Normalisation.cs - both are static, pure, ADR-citing utility classes with a one-line body"

requirements-completed: [AUTH-01, AUTH-02, AUTH-03]

coverage:
  - id: D1
    description: "UsernameNormalizer.Normalize(string?) reduces a username to trim+lowercase-invariant, null-safe"
    requirement: "AUTH-01"
    verification:
      - kind: unit
        ref: "tests/BlazorCanvas.Tests/Auth/UsernameNormalizerTests.cs#Normalize_ProducesCanonicalForm"
        status: pass
    human_judgment: false
  - id: D2
    description: "Cookie authentication, authorization, and cascading auth state registered in Program.cs; LoginPath=/login"
    requirement: "AUTH-02"
    verification:
      - kind: other
        ref: "dotnet build BlazorCanvas.sln -c Debug (grep-verified: AddCookie/LoginPath/AddCascadingAuthenticationState present)"
        status: pass
    human_judgment: true
    rationale: "Build success and static grep confirm the code is present and compiles, but the actual redirect-to-/login behavior on an unauthenticated request can only be confirmed once 02-03 adds a protected page to test against - no [Authorize] page exists yet in this plan's scope."
  - id: D3
    description: "UseAuthentication/UseAuthorization run before UseAntiforgery in the middleware pipeline (RESEARCH Pitfall 4)"
    requirement: "AUTH-02"
    verification:
      - kind: other
        ref: "awk line-number ordering check against src/BlazorCanvas/Program.cs (a=67 < c=70, b=68 < c=70)"
        status: pass
    human_judgment: false
  - id: D4
    description: "POST /logout validates antiforgery, calls SignOutAsync, and LocalRedirects to /login"
    requirement: "AUTH-03"
    verification:
      - kind: other
        ref: "dotnet build BlazorCanvas.sln -c Debug (grep-verified: MapPost/ValidateRequestAsync/SignOutAsync/LocalRedirect ordering)"
        status: pass
    human_judgment: true
    rationale: "Build and static ordering checks confirm the code compiles and calls occur in the right order, but there is no logged-in session or logout form yet (02-03) to drive the endpoint end-to-end and observe an actual 302 + cleared cookie."

# Metrics
duration: 20min
completed: 2026-07-15
status: complete
---

# Phase 2 Plan 2: Login, Session & Logout Summary

**Cookie-authentication backbone wired in Program.cs (AddCookie/LoginPath, AddAuthorization, AddCascadingAuthenticationState, correct Auth->Authz->Antiforgery middleware order), a CSRF-protected POST /logout endpoint, and the single-source UsernameNormalizer (D-44) built TDD-first.**

## Performance

- **Duration:** 20 min
- **Started:** 2026-07-15T19:00:00Z
- **Completed:** 2026-07-15T19:20:00Z
- **Tasks:** 3 completed (Task 1 produced 2 commits: RED + GREEN)
- **Files modified:** 3 (2 created, 1 modified)

## Accomplishments
- `UsernameNormalizer.Normalize(string?)` is now the single source of the trim + lowercase-invariant username rule (D-44), built RED-first (test committed before the implementation existed, confirmed to fail to compile) then GREEN (implementation added, all 6 cases pass)
- `Program.cs` registers `AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(...)` with `LoginPath = "/login"`, plus `AddAuthorization()` and `AddCascadingAuthenticationState()`
- `UseAuthentication()`/`UseAuthorization()` inserted directly before the pre-existing `UseAntiforgery()` call, satisfying RESEARCH's Pitfall 4 ordering requirement
- `POST /logout` minimal-API endpoint added: `IAntiforgery.ValidateRequestAsync` -> `SignOutAsync` -> `Results.LocalRedirect("/login")` — CSRF-protected and open-redirect-safe by construction

## Task Commits

Each task was committed atomically:

1. **Task 1: UsernameNormalizer** - `5683fcf` (test, RED) + `111b2f2` (feat, GREEN)
2. **Task 2: Cookie-auth services + middleware ordering** - `a6b1d4c` (feat)
3. **Task 3: POST /logout endpoint** - `307c7d9` (feat)

**Plan metadata:** committed separately after this SUMMARY (see below)

_Note: Task 1 was `tdd="true"` - test committed first, confirmed to fail (BlazorCanvas.Auth namespace did not exist), then the implementation was restored and committed once all 6 cases passed._

## Files Created/Modified
- `src/BlazorCanvas/Auth/UsernameNormalizer.cs` - single static `Normalize(string?)` method: `(username ?? "").Trim().ToLowerInvariant()`, mirrors `Geometry/Normalisation.cs`'s convention
- `tests/BlazorCanvas.Tests/Auth/UsernameNormalizerTests.cs` - xUnit `[Theory]`/`[InlineData]` covering every behavior-block case (upper/lower/whitespace/null/whitespace-only)
- `src/BlazorCanvas/Program.cs` - cookie auth registration + middleware ordering + `/logout` endpoint

## Decisions Made
- `IsPersistent` intentionally left at its default `false` — `ExpireTimeSpan = TimeSpan.FromDays(365)` only bounds the encrypted ticket's server-side validity, not cookie persistence; the actual sign-in call (02-03) is what determines the cookie's lifetime, per D-26
- `UseAuthentication()`/`UseAuthorization()` placed immediately above the existing `UseAntiforgery()` call (not at the very top of the pipeline) to keep the diff minimal and the ordering requirement unambiguous
- `Results.LocalRedirect("/login")` used verbatim (a compile-time-fixed literal, never a variable) so the logout endpoint has no open-redirect surface at all, not just a validated one

## Deviations from Plan

None — plan executed exactly as written. All Program.cs additions match the plan's specified code verbatim (including the RESEARCH-cited comments); `UsernameNormalizer` matches the specified `(username ?? "").Trim().ToLowerInvariant()` body exactly.

## Issues Encountered

None for the plan's own scope. During Task 3's final full-suite verification (`dotnet test` with no filter, run beyond what the plan's `<verify>` block required), 68 pre-existing `BlazorCanvas.Tests.Database.CheckConstraintTests` cases from Phase BC-01 failed with `NpgsqlException: Failed to connect to 127.0.0.1:5433` — the local Docker Postgres container is not currently running on this dev machine. This is unrelated to any file this plan touches (no `Auth/` or `Program.cs` code path is exercised by those tests) and is logged in `.planning/phases/BC-02-login-session-logout/deferred-items.md` per the executor's scope-boundary rule, not fixed. The plan's own scoped verification (`dotnet test --filter "FullyQualifiedName~UsernameNormalizer"`, 6/6 pass) and `dotnet build BlazorCanvas.sln` (success) both pass cleanly.

## User Setup Required

None — no external service configuration required. (Note: bringing up the local Postgres container via `docker compose up -d` is required to run the pre-existing BC-01 database integration test suite, but that is an environment-state item unrelated to this plan's deliverables — see Issues Encountered above.)

## Next Phase Readiness

The cookie-auth backbone that 02-03 depends on is fully live: `LoginPath="/login"` will redirect an unauthenticated request the moment `[Authorize]` appears on `Home.razor` (02-03); `AddCascadingAuthenticationState()` makes `Task<AuthenticationState>` available to inject in `Home.razor`'s `@code` block with no DB lookup; `POST /logout` is ready for the toolbar's plain `<form>` to target; `UsernameNormalizer.Normalize` is ready for the login handler to call on every read and write path. No blockers for 02-03.

---
*Phase: 02-login-session-logout*
*Completed: 2026-07-15*

## Self-Check: PASSED

All created files verified present on disk (`src/BlazorCanvas/Auth/UsernameNormalizer.cs`,
`tests/BlazorCanvas.Tests/Auth/UsernameNormalizerTests.cs`, `src/BlazorCanvas/Program.cs`,
`02-02-SUMMARY.md`) and all 5 commits (`5683fcf`, `111b2f2`, `a6b1d4c`, `307c7d9`, `0ef0f1e`) verified
present in `git log`.
