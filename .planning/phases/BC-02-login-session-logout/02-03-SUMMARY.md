---
phase: BC-02-login-session-logout
plan: 03
subsystem: auth
tags: [blazor-server, static-ssr, interactive-server, cookie-auth, antiforgery, ef-core]

# Dependency graph
requires:
  - phase: BC-02 (02-01, 02-02)
    provides: stripped Bootstrap-free shell, cookie authentication scheme registration, UsernameNormalizer, POST /logout endpoint
provides:
  - Static-SSR /login page with create-on-unknown handler, race-safe unique-violation catch, plaintext compare, three error variants, redirect-reason banner
  - InteractiveServer [Authorize] canvas shell at / reading user_id from the cookie claim with no DB lookup
  - 48px toolbar strip with right-aligned, antiforgery-protected Logout form
affects: [BC-03-canvas-drawing]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Static SSR page (no @rendermode) required whenever a component must call HttpContext.SignInAsync — InteractiveServer components cannot set cookies (response already started)"
    - "AuthenticationState claim read only, no EF Core injection, on InteractiveServer pages that must avoid a DB round-trip on load"
    - "Plain <form method=post> (not EditForm) for logout, with a manually rendered antiforgery hidden input from AntiforgeryStateProvider.GetAntiforgeryToken() — plain forms get no automatic token"

key-files:
  created:
    - src/BlazorCanvas/Components/Pages/Login.razor
    - src/BlazorCanvas/Components/Pages/Login.razor.css
  modified:
    - src/BlazorCanvas/Components/Pages/Home.razor
    - src/BlazorCanvas/Components/Pages/Home.razor.css
    - src/BlazorCanvas/Components/_Imports.razor

key-decisions:
  - "AntiforgeryStateProvider.GetAntiforgeryToken()'s hidden-input field name is token.FormFieldName, not token.Name as 02-RESEARCH.md's Pattern 3 showed — verified against the installed .NET 10.0.9 Microsoft.AspNetCore.Components.Forms assembly and corrected in Home.razor"

patterns-established:
  - "Two-render-mode auth split: static SSR for anything writing the auth cookie, InteractiveServer + [Authorize] for everything downstream that only reads the claim"

requirements-completed: [AUTH-01, AUTH-02, AUTH-03]

coverage:
  - id: D1
    description: "Static-SSR /login page: new username creates account + lands on canvas; existing + correct password logs in; wrong password / empty username / empty password show the three exact error strings; Egor/egor normalize to one account"
    requirement: "AUTH-01"
    verification:
      - kind: manual_procedural
        ref: "Human-verify checkpoint (task 3) — steps 3 and 6, approved"
        status: pass
    human_judgment: false
  - id: D2
    description: "Session cookie keeps user logged in across F5 and a second tab; browser close logs the user out; unauthenticated / redirects to /login with the informational banner; canvas reads user_id from the claim with no DB lookup"
    requirement: "AUTH-02"
    verification:
      - kind: manual_procedural
        ref: "Human-verify checkpoint (task 3) — steps 2, 4, 5, 8, approved"
        status: pass
    human_judgment: false
  - id: D3
    description: "Right-aligned Logout form in the 48px toolbar posts to POST /logout, clears the cookie, returns to /login; a different user then lands on their own separate account"
    requirement: "AUTH-03"
    verification:
      - kind: manual_procedural
        ref: "Human-verify checkpoint (task 3) — step 7, approved"
        status: pass
    human_judgment: false

# Metrics
duration: 12min
completed: 2026-07-15
status: complete
---

# Phase BC-02 Plan 03: Login Page, Canvas Shell & Logout Summary

**Static-SSR /login with race-safe create-on-unknown handler, plus an InteractiveServer [Authorize] canvas shell that reads the user_id cookie claim with zero DB lookups and carries a right-aligned, antiforgery-protected Logout form.**

## Performance

- **Duration:** 12 min (a7152ee to 49ffe76)
- **Started:** 2026-07-15T21:17:47+02:00
- **Completed:** 2026-07-15T21:22:05+02:00 (implementation); human-verify approved after
- **Tasks:** 3 (2 auto + 1 checkpoint:human-verify)
- **Files modified:** 5

## Accomplishments
- Static-SSR `/login` page: normalizes username via `UsernameNormalizer`, creates-on-unknown with a race-safe `UniqueViolation` catch, plaintext-compares existing users, signs in with a `Claim("user_id", ...)`, and renders the three exact error strings plus the ReturnUrl-gated "Please log in to continue." banner (never navigating to the raw ReturnUrl value).
- InteractiveServer `[Authorize]` canvas shell at `/`: reads `user_id` from cascading `AuthenticationState` with no `DbContext` injected and no query on load; renders the 48px `#DCE0E5` toolbar strip with a right-aligned, antiforgery-protected Logout form.
- Full BC-02 flow human-verified end-to-end: unauth redirect + banner, new-user create + toolbar, F5 persistence, second-tab auth, D-44 same-account normalization, all three error variants, logout + per-user separation, and browser-close session expiry — all 8 checks approved.

## Task Commits

Each task was committed atomically:

1. **Task 1: The static-SSR /login page and create-or-compare handler** - `a7152ee` (feat)
2. **Task 2: The authenticated canvas shell** - `49ffe76` (feat)
3. **Task 3: checkpoint:human-verify** - approved by human ("approved" — all 8 interactive checks passed), no code commit (verification-only task)

**Plan metadata:** (this commit, following SUMMARY.md write)

## Files Created/Modified
- `src/BlazorCanvas/Components/Pages/Login.razor` - static-SSR EditForm login page + create-or-compare handler
- `src/BlazorCanvas/Components/Pages/Login.razor.css` - login card styled from 02-UI-SPEC locked tokens
- `src/BlazorCanvas/Components/Pages/Home.razor` - InteractiveServer `[Authorize]` shell + 48px toolbar + logout form (rewritten from hello-world placeholder)
- `src/BlazorCanvas/Components/Pages/Home.razor.css` - toolbar strip styled from 02-UI-SPEC locked tokens
- `src/BlazorCanvas/Components/_Imports.razor` - added `@using Microsoft.AspNetCore.Authorization` and `@using Microsoft.AspNetCore.Components.Authorization`

## Decisions Made
- IsPersistent left unset at sign-in (session cookie, D-26) — already established in 02-02, reconfirmed here.
- Claim key `"user_id"` matched exactly between Login.razor's write and Home.razor's `FindFirst("user_id")` read — the key_link the plan called out as load-bearing.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] AntiforgeryStateProvider token field name corrected from `Name` to `FormFieldName`**
- **Found during:** Task 2 (Home.razor logout form)
- **Issue:** 02-RESEARCH.md's Pattern 3 example showed the antiforgery hidden input built as `token.Name`, but the installed .NET 10.0.9 `Microsoft.AspNetCore.Components.Forms.AntiforgeryStateProvider`/`AntiforgeryRequestToken` API surface exposes the field name as `FormFieldName`, not `Name`. Using `Name` would have failed to compile (or silently emitted the wrong hidden-input name, breaking antiforgery validation on `POST /logout`).
- **Fix:** Rendered the hidden input as `<input type="hidden" name="@token!.FormFieldName" value="@token.Value" />` in Home.razor, matching the real assembly.
- **Files modified:** `src/BlazorCanvas/Components/Pages/Home.razor`
- **Verification:** Confirmed directly against the installed .NET 10.0.9 assembly; `dotnet build BlazorCanvas.sln` succeeds; logout flow verified end-to-end in the task 3 human-verify checkpoint (step 7, approved).
- **Committed in:** `49ffe76` (Task 2 commit)

---

**Total deviations:** 1 auto-fixed (1 bug fix)
**Impact on plan:** Necessary correctness fix against the real framework API; no scope creep. All other behavior matches 02-RESEARCH.md's patterns exactly.

## Issues Encountered
None beyond the FormFieldName correction above.

## User Setup Required
None - no external service configuration required. (PostgreSQL via `docker compose up -d` was already required by prior phases/plans.)

## Next Phase Readiness
- BC-02 (Login, Session & Logout) is fully complete: all three plans executed, all 8 interactive human-verify checks approved, all five Phase 2 ROADMAP success criteria hold end-to-end.
- Phase 3 (The Canvas & Drawing) can now build on an authenticated `/` shell that already has the 48px toolbar strip reserving an 8px left inset and a `user_id` available via the claim — no blockers.

---
*Phase: BC-02-login-session-logout*
*Completed: 2026-07-15*

## Self-Check: PASSED

- FOUND: `.planning/phases/BC-02-login-session-logout/02-03-SUMMARY.md`
- FOUND: commit `a7152ee` (Task 1 — static-SSR /login page)
- FOUND: commit `49ffe76` (Task 2 — InteractiveServer canvas shell)
- FOUND: all 5 declared files on disk (Login.razor, Login.razor.css, Home.razor, Home.razor.css, _Imports.razor)
- FOUND: `dotnet build BlazorCanvas.sln -c Debug --nologo -v q` succeeds, 0 warnings, 0 errors
