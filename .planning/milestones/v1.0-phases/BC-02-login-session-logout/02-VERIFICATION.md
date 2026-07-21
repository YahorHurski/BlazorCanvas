---
phase: 02-login-session-logout
verified: 2026-07-15T22:00:00Z
status: passed
score: 10/10 must-haves verified
behavior_unverified: 0
overrides_applied: 0
---

# Phase 2: Login, Session & Logout Verification Report

**Phase Goal:** A user can identify themselves, and the app knows whose canvas to load — across every tab in the browser and across F5.
**Verified:** 2026-07-15T22:00:00Z
**Status:** passed
**Re-verification:** No — initial verification

## Goal Achievement

### Observable Truths (merged from ROADMAP Success Criteria + all 3 plans' must_haves.truths)

| # | Truth | Status | Evidence |
|---|-------|--------|----------|
| 1 | Blank pass-through app shell, no Bootstrap/NavMenu/demo chrome (02-01) | VERIFIED | `MainLayout.razor` = `@Body` + `#blazor-error-ui` only (no `NavMenu`/`sidebar`); `wwwroot/lib/bootstrap` deleted; `App.razor` has no Bootstrap `<link>`; `NavMenu.razor`, `Counter.razor`, `Weather.razor` deleted; `NotFound.razor`/`Error.razor` retained |
| 2 | Page has zero margin, 48px toolbar sits flush at (0,0) (D-43) | VERIFIED | `app.css` contains `html, body { margin: 0; }`; `Home.razor.css` `.toolbar { height: 48px; width: 100%; }` with no border |
| 3 | Unauthenticated GET to a protected page redirects to `/login` via cookie middleware `LoginPath`, not the Router (AUTH-02, D-51) | VERIFIED (code) + human-confirmed | `Program.cs` L21 `options.LoginPath = "/login"`; `Home.razor` carries `@attribute [Authorize]` with no `AuthorizeRouteView`/custom redirect in `Routes.razor`; human-verify checkpoint step 2 approved |
| 4 | Auth scheme issues a session cookie (IsPersistent stays default false — dies with browser, survives F5, shared across tabs) (AUTH-02, D-26) | VERIFIED (code) + human-confirmed | `Login.razor` `SignInAsync(...)` call has no `AuthenticationProperties`/`IsPersistent` argument at all — comment explicitly confirms default false; human-verify steps 4, 5, 8 approved |
| 5 | `POST /logout` validates antiforgery, clears cookie via `SignOutAsync`, local-redirects to `/login` (AUTH-03, D-25/D-56) | VERIFIED (code) + human-confirmed | `Program.cs` L76-86: `ValidateRequestAsync` precedes `SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme)` precedes `Results.LocalRedirect("/login")` (literal, not variable); human-verify step 7 approved |
| 6 | Username reduced to one canonical form (trim + lowercase-invariant) by exactly one function (AUTH-01, D-44) | VERIFIED | `UsernameNormalizer.Normalize` = `(username ?? "").Trim().ToLowerInvariant()`; called at both the lookup (`Login.razor` L63) and the INSERT (username field set from the same normalized variable); 6/6 unit tests pass (`UsernameNormalizerTests.cs`) |
| 7 | New username at `/login` creates account + lands on canvas at `/`; existing+correct password lands on same account; wrong password shows error; no Register page (AUTH-01, D-17) | VERIFIED (code) + human-confirmed | `Login.razor` create-on-unknown path (`SingleOrDefaultAsync` → insert if null, race-safe `UniqueViolation` catch) → `SignInAsync` → `Nav.NavigateTo("/")`; no `Register.razor` file exists anywhere in `Components/Pages/`; human-verify step 3 approved |
| 8 | Three exact error variants + ReturnUrl-gated informational banner (AUTH-01, UI-SPEC) | VERIFIED | Exact strings present: `"Wrong password. Try again."`, `"Username is required."`, `"Password is required."`, `"Please log in to continue."`; banner gated on `Query["ReturnUrl"]` presence only, raw value never passed to `NavigateTo` |
| 9 | Authenticated canvas shell reads `user_id` from cookie claim via cascading `AuthenticationState`, no DB lookup on load (AUTH-02, D-51) | VERIFIED | `Home.razor`: `@rendermode InteractiveServer` + `@attribute [Authorize]`; `AuthStateTask` → `state.User.FindFirst("user_id")`; no `CanvasDbContext`/`@inject ... Db` anywhere in the file — grep confirms zero matches |
| 10 | Right-aligned Logout form in 48px toolbar posts to `POST /logout`, returns to `/login`; different user lands on separate canvas (AUTH-03, D-56) | VERIFIED (code) + human-confirmed | `Home.razor`: `<form method="post" action="/logout">` with `token.FormFieldName`-sourced hidden antiforgery input, `margin-left: auto` in `.logout-form` CSS pushing it right; human-verify step 7 approved (per-user separation) |

**Score:** 10/10 truths verified (0 present-but-behavior-unverified)

### Required Artifacts

| Artifact | Expected | Status | Details |
|----------|----------|--------|---------|
| `src/BlazorCanvas/Auth/UsernameNormalizer.cs` | `public static string Normalize(string?)` | VERIFIED | Exists, single-line body, XML-doc citing D-44, `ToLowerInvariant` count = 1 |
| `src/BlazorCanvas/Program.cs` | AddAuthentication/AddCookie/AddAuthorization/AddCascadingAuthenticationState/UseAuthentication/UseAuthorization before UseAntiforgery/MapPost(/logout) | VERIFIED | All present; line-order confirmed via awk (67, 68 < 70) |
| `tests/BlazorCanvas.Tests/Auth/UsernameNormalizerTests.cs` | passing unit tests | VERIFIED | 6 `[InlineData]` cases, all pass (confirmed by direct `dotnet test` run in this verification, not just SUMMARY claim) |
| `src/BlazorCanvas/Components/Pages/Login.razor` (+ `.css`) | static-SSR login page + handler | VERIFIED | No render-mode directive; handler matches spec exactly; CSS transcribes locked UI-SPEC tokens (`#1D4ED8`, `#8B939E`, `#B91C1C`, `#DCE0E5`) |
| `src/BlazorCanvas/Components/Pages/Home.razor` (+ `.css`) | InteractiveServer `[Authorize]` shell + 48px toolbar + logout form | VERIFIED | Directives present, claim-only read, toolbar CSS matches (48px, `#DCE0E5`, no border) |
| `src/BlazorCanvas/Components/_Imports.razor` | auth `@using` directives | VERIFIED | `Microsoft.AspNetCore.Authorization` and `Microsoft.AspNetCore.Components.Authorization` both present |
| `src/BlazorCanvas/Components/Layout/MainLayout.razor` (+ `.css`) | gutted to `@Body` + error-ui | VERIFIED | Matches exactly; CSS contains only `#blazor-error-ui` rules |
| `src/BlazorCanvas/Components/App.razor` | no Bootstrap link | VERIFIED | Only `app.css` and `BlazorCanvas.styles.css` links remain |
| `src/BlazorCanvas/wwwroot/app.css` | framework essentials + margin:0, no `bs-` refs | VERIFIED | Confirmed by direct read; no Bootstrap-derived rules remain |

### Key Link Verification

| From | To | Via | Status | Details |
|------|----|----|--------|---------|
| `Login.razor` `SignInAsync` | `Home.razor` `FindFirst` | `Claim("user_id", ...)` string match | WIRED | Both sides use the literal string `"user_id"` — grep-confirmed exact match |
| `Program.cs` `AddCookie` scheme | `Login.razor`/`Program.cs` sign-in/sign-out | `CookieAuthenticationDefaults.AuthenticationScheme` | WIRED | Same constant used in all three call sites (`AddAuthentication`, `SignInAsync`, `SignOutAsync`) |
| `Program.cs` middleware order | Auth → Authz → Antiforgery | line-number ordering | WIRED | `UseAuthentication()` (L67) → `UseAuthorization()` (L68) → `UseAntiforgery()` (L70) |
| `options.LoginPath = "/login"` | `Login.razor`'s `ReturnUrl`-gated banner | query-string signal | WIRED | `LoginPath` config produces the `?ReturnUrl=` query param `Login.razor`'s `OnInitialized` reads |
| Logout `<form>` hidden antiforgery input | `POST /logout` `ValidateRequestAsync` | manual token via `AntiforgeryStateProvider.GetAntiforgeryToken()` | WIRED | `Home.razor` renders `token.FormFieldName`/`token.Value`; endpoint validates before sign-out |
| `MainLayout.razor` | `NavMenu.razor` deletion | no reference | WIRED (verified absent) | `grep -c 'NavMenu\|sidebar'` on `MainLayout.razor` = 0; file deleted |

### Requirements Coverage

| Requirement | Source Plan(s) | Description | Status | Evidence |
|---|---|---|---|---|
| AUTH-01 | 02-01, 02-02, 02-03 | Login form, create-on-unknown, plaintext compare, normalize | SATISFIED | `Login.razor` handler, `UsernameNormalizer`, unit tests, human-verify steps 3+6 |
| AUTH-02 | 02-02, 02-03 | Session cookie, LoginPath redirect, claim read no DB lookup | SATISFIED | `Program.cs` cookie config + middleware order, `Home.razor` claim-only read, human-verify steps 2,4,5,8 |
| AUTH-03 | 02-01, 02-02, 02-03 | Logout form → POST /logout, CSRF-protected, per-user separation | SATISFIED | `Home.razor` logout form, `Program.cs` `/logout` endpoint, human-verify step 7 |

No orphaned requirements: REQUIREMENTS.md maps exactly AUTH-01/02/03 to Phase 2, and all three appear in at least one plan's `requirements` frontmatter field. All three are marked `[x]` and "Complete" in REQUIREMENTS.md's traceability table (lines 22-38, 175-177).

### Anti-Patterns Found

None. Scanned all 11 files touched across the three plans (`Program.cs`, `UsernameNormalizer.cs`, `Login.razor`, `Login.razor.css`, `Home.razor`, `Home.razor.css`, `_Imports.razor`, `App.razor`, `MainLayout.razor`, `MainLayout.razor.css`, `app.css`) for `TBD|FIXME|XXX|TODO|HACK|PLACEHOLDER|placeholder|coming soon|not yet implemented` — zero matches. No empty-implementation patterns (`return null`, `=> {}`), no hardcoded-empty stub props found in the reviewed files.

### Behavioral Spot-Checks

| Behavior | Command | Result | Status |
|---|---|---|---|
| UsernameNormalizer unit tests exist and pass | `dotnet test tests/BlazorCanvas.Tests/BlazorCanvas.Tests.csproj --no-build --filter "FullyQualifiedName!~Database"` | 91/91 passed (includes the 6 UsernameNormalizer cases) | PASS |
| Middleware ordering (Auth → Authz → Antiforgery) | `awk` line-number check on `Program.cs` | auth:67 authz:68 antiforgery:70 → order_ok:1 | PASS |
| All 8 SUMMARY-cited commits exist in git history | `git cat-file -t <hash>` for all 8 hashes | all returned `commit` | PASS |

Full solution build was NOT re-run in this verification because the app is currently running under a locked `BlazorCanvas.exe` (per environmental context) — relied on the documented green build immediately preceding this verification, corroborated by the successful `dotnet test --no-build` run above (which requires the DLL to have built cleanly).

### Human Verification Required

None — all interactive/runtime criteria (F5 persistence, second-tab auth, browser-close session expiry, unauth redirect + banner, D-44 same-account normalization, three error variants, logout + per-user separation) were already executed by the human at the blocking checkpoint in 02-03 and approved ("approved" — all 8 checks passed), per the environmental context provided for this verification. No additional human verification items were identified beyond what that checkpoint already covered.

### Gaps Summary

No gaps found. All must_haves truths, artifacts, and key_links from all three plans (02-01, 02-02, 02-03) are present, substantive, and wired in the codebase — not merely claimed in SUMMARY.md. All three requirement IDs (AUTH-01, AUTH-02, AUTH-03) are satisfied with both static-code evidence and human-confirmed runtime behavior. The phase goal — "A user can identify themselves, and the app knows whose canvas to load — across every tab in the browser and across F5" — is achieved: cookie-based session auth is wired end-to-end from `/login` through the `[Authorize]` canvas shell to `POST /logout`, with the claim-based, DB-lookup-free identity read that Phase 3 depends on.

---

_Verified: 2026-07-15T22:00:00Z_
_Verifier: Claude (gsd-verifier)_
