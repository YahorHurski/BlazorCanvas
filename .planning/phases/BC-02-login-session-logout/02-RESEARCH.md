# Phase 2: Login, Session & Logout - Research

**Researched:** 2026-07-15
**Domain:** ASP.NET Core / Blazor Web App cookie authentication, spanning the static-SSR / InteractiveServer render-mode boundary
**Confidence:** HIGH (framework mechanics, confirmed against Microsoft Learn docs for `aspnetcore-10.0` and against the actual Phase 1 codebase) / MEDIUM (a few UX-adjacent implementation choices left as recommendations, not ADR-locked)

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|-------------------|
| AUTH-01 | `/login` static-SSR form. Existing username + correct password loads account; wrong password errors; unknown username creates account + empty canvas. No Register page. Usernames trimmed/lowercased/UNIQUE/non-empty; passwords non-empty, plaintext. `SignInAsync` + redirect on success. | "The login handler" (Code Examples), "Case-insensitive username" (Common Pitfalls), Package/schema confirmation against Phase 1 code |
| AUTH-02 | Session cookie (no expiry — dies with browser, survives F5, shared across tabs). `user_id` cookie claim, no DB lookup on canvas page load. Unauthenticated `/` redirects to `/login`. | "Cookie configuration for a session cookie" (Code Examples), "Reading the claim with no DB lookup" (Code Examples), "The LoginPath redirect, not AuthorizeRouteView" (Architecture Patterns + Common Pitfalls) |
| AUTH-03 | Right-aligned Logout `<form method="post" action="/logout">` in the 48px toolbar; posts to a real endpoint, not an interactive button. | "The logout endpoint" (Code Examples), "Antiforgery on the plain logout form" (Common Pitfalls) |
</phase_requirements>

## Summary

This phase is entirely framework plumbing — there is no new domain logic beyond a username/password
lookup already fully specified by the ADRs. The one substantive engineering problem is the **render-mode
seam**: `/login` must be **static SSR** because only a static-SSR request/response cycle has a live
`HttpContext` that `SignInAsync` can write a `Set-Cookie` header into; the canvas at `/` is
**InteractiveServer** and reads the resulting cookie's claim over its long-lived SignalR circuit, which
never gets a fresh `HttpContext` per interaction. Confirmed directly against the .NET 10 docs and Phase
1's actual `Program.cs`/`Home.razor`: this is a plain `Microsoft.NET.Sdk.Web` Blazor Web App with
`AddInteractiveServerComponents()` / `AddInteractiveServerRenderMode()` already wired, and it currently
has **zero** authentication code — `Home.razor` at `/` has no `@rendermode` (so it is implicitly static
SSR today) and no `[Authorize]`. Phase 2 must add: cookie-auth middleware in `Program.cs`; give `Home.razor`
`@rendermode InteractiveServer` + `@attribute [Authorize]` (making it the interactive, protected canvas
shell Phase 3 will fill in); replace `Home.razor`'s placeholder markup with the toolbar strip; add a
`/login` static-SSR page; and add a `POST /logout` minimal-API endpoint.

Everything needed is already **in-box** in the ASP.NET Core / Blazor shared framework — cookie
authentication (`Microsoft.AspNetCore.Authentication.Cookies`), antiforgery, and
`Microsoft.AspNetCore.Components.Authorization` all ship with `Microsoft.NET.Sdk.Web` and are already
referenced transitively. **No new NuGet packages are required for this phase.**

The single biggest correctness risk is *not* the sign-in mechanics — it's assuming the Blazor Router's
`AuthorizeRouteView`/`<NotAuthorized>` pattern is what redirects an unauthenticated `/` visitor to
`/login`. It is not, for the initial-request case this phase cares about. Per Microsoft's own docs:
*"Razor components of Blazor Web Apps never display `<NotAuthorized>` content when authorization fails
server-side during static server-side rendering (static SSR)... use server-side techniques, such as
configuring `CookieAuthenticationOptions.LoginPath`."* Since every Blazor Web App page — including ones
destined for InteractiveServer — is first rendered through a static-SSR prerender pass on the initial
HTTP GET, `[Authorize]` on `Home.razor` is enforced by the ordinary ASP.NET Core authorization
middleware against `CookieAuthenticationOptions.LoginPath`, exactly as it would be for a Razor Pages or
MVC app — no client-side circuit ever spins up for a rejected visitor. This is also the mechanism that
gives the UI-SPEC's redirect-reason banner its signal for free: the cookie handler appends the default
`?ReturnUrl=%2F` query parameter to the redirect, and `/login` can gate the banner on that parameter's
presence with zero custom code.

**Primary recommendation:** Configure `AddAuthentication().AddCookie(options => { options.LoginPath =
"/login"; ... })` with `app.UseAuthentication(); app.UseAuthorization();` before `app.UseAntiforgery();`
in `Program.cs`; build `/login` as a static-SSR `EditForm` component that does the trim/lowercase/lookup/
create-or-compare/`SignInAsync` dance directly (using the `[CascadingParameter] HttpContext?` that is
valid in static SSR), never setting `AuthenticationProperties.IsPersistent = true` (the default `false`
is exactly D-26's "dies with the browser" session cookie); mark `Home.razor` `@rendermode
InteractiveServer` + `[Authorize]` and read `user_id` off the cascading `AuthenticationState`, never
touching the database; and add a `MapPost("/logout", ...)` minimal-API endpoint that calls
`SignOutAsync` and redirects to `/login`.

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| Username/password validation, user create-on-unknown | Frontend Server (static SSR) | Database | The `/login` static-SSR component owns the request/response cycle needed to call `SignInAsync`; it delegates the actual EF Core read/write to the database tier. No API layer exists (D-07). |
| Session cookie issuance (`SignInAsync`) | Frontend Server (static SSR) | — | Only a live `HttpContext` (available during static SSR, not during an established InteractiveServer circuit) can write `Set-Cookie`. This is the entire reason D-34 exists. |
| Session cookie validation on every request | ASP.NET Core middleware (`UseAuthentication`/`UseAuthorization`) | — | Framework-owned; runs before any Razor Component renders, for both static SSR and the InteractiveServer prerender pass. |
| `user_id` claim read on canvas page load | Frontend Server (InteractiveServer circuit) | — | Read from the cascading `AuthenticationState` already carried by the circuit's `HttpContext.User` at connection time — explicitly **not** a database read (D-51). |
| Session cookie clearing (`SignOutAsync`) | Frontend Server (plain endpoint) | — | Same constraint as sign-in: a plain `MapPost` endpoint gets a live `HttpContext`; an interactive circuit does not. |
| Unauthenticated-visitor redirect to `/login` | ASP.NET Core middleware (`CookieAuthenticationOptions.LoginPath`) | — | Not the Blazor Router's `AuthorizeRouteView`/`NotAuthorized` — that mechanism is inert for the static-SSR-prerender case this requirement describes. |
| Toolbar strip layout / Logout button | Browser / Client (markup only, zero JS) | Frontend Server (renders the markup) | Pure CSS + native `<form>` semantics; no interactivity is required for the plain logout POST. |

## Standard Stack

### Core

| Component | Version | Purpose | Why Standard |
|-----------|---------|---------|---------------|
| `Microsoft.AspNetCore.Authentication.Cookies` (in-box, part of `Microsoft.AspNetCore.App` shared framework) | Ships with .NET 10 SDK (verified: `dotnet --list-sdks` shows `10.0.301` installed per D-28) | Cookie-based sign-in/out, session cookie semantics | This *is* "cookie plumbing, not ASP.NET Core Identity" (D-34) — the exact minimal mechanism the ADR calls for. `[CITED: learn.microsoft.com/aspnet/core/security/authentication/cookie]` |
| `Microsoft.AspNetCore.Components.Authorization` (in-box) | Ships with .NET 10 SDK | `AuthenticationStateProvider`, `[CascadingParameter] Task<AuthenticationState>`, `[Authorize]`, `AuthorizeRouteView` | Standard Blazor auth-state plumbing; no reason to hand-roll. `[CITED: learn.microsoft.com/aspnet/core/blazor/security/authentication-state]` |
| Built-in Antiforgery (`AddRazorComponents()` wires it automatically; `app.UseAntiforgery()` already present in Phase 1's `Program.cs`) | Ships with .NET 10 SDK | CSRF protection for form posts | Already present in the scaffolded `Program.cs` — Phase 2 must respect its middleware-ordering requirement (after `UseAuthentication`/`UseAuthorization`), not add a package. `[CITED: learn.microsoft.com/aspnet/core/blazor/forms]` |

### Supporting

None. Everything this phase needs — `ClaimsIdentity`, `ClaimsPrincipal`, `AuthenticationProperties`,
`CookieAuthenticationDefaults`, `EditForm`, `[SupplyParameterFromForm]`, `AntiforgeryStateProvider` — is
part of the base class library / Blazor framework already referenced by the existing
`Microsoft.NET.Sdk.Web` project. `[VERIFIED: codebase — src/BlazorCanvas/BlazorCanvas.csproj]`.

### Alternatives Considered

| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| Cookie auth (this phase) | ASP.NET Core Identity | Rejected by D-08 — full account system, hashing, reset flows; none of it wanted for plaintext-password MinVP. |
| Cookie auth (this phase) | JWT in `localStorage` + manual header attach | Requires JavaScript to read/attach the token on every SignalR message; violates the no-JS constraint outright. |
| `EditForm` static-SSR sign-in (this phase) | Hand-written `POST /login` minimal-API endpoint reading raw form fields | Considered and rejected in D-34 itself ("less framework machinery... sacrifices the well-trodden path for no real saving"). The `EditForm` approach also gets antiforgery, model binding, and native HTML `required` validation for free. |

**Installation:** None. No `dotnet add package` commands are needed for this phase.

**Version verification:** `Microsoft.EntityFrameworkCore.Design 10.0.10` and
`Npgsql.EntityFrameworkCore.PostgreSQL 10.0.3` are already pinned in
`src/BlazorCanvas/BlazorCanvas.csproj` from Phase 1 `[VERIFIED: codebase]`. Nothing in Phase 2 changes
these. The cookie-auth and antiforgery APIs used below are part of the .NET 10 shared framework, not a
versioned NuGet package — there is no separate version to pin or drift.

## Package Legitimacy Audit

**Not applicable — this phase installs no external packages.** Cookie authentication, antiforgery, and
`Microsoft.AspNetCore.Components.Authorization` all ship in-box with the `Microsoft.NET.Sdk.Web` /
`Microsoft.AspNetCore.App` shared framework the project already targets (`net10.0`). No `PackageReference`
additions are required, so the Package Legitimacy Gate has nothing to check.

## Architecture Patterns

### System Architecture Diagram

```
                     Unauthenticated GET /                Authenticated GET /
                              │                                    │
                              ▼                                    ▼
                  ┌─────────────────────────────────────────────────────────┐
                  │  ASP.NET Core routing + [Authorize] on Home.razor's      │
                  │  endpoint (CookieAuthenticationOptions.LoginPath check)  │
                  └───────────────┬───────────────────────┬───────────────┘
                    no valid cookie                 valid cookie
                                  │                         │
                                  ▼                         ▼
                    302 → /login?ReturnUrl=%2F   Static-SSR prerender of Home.razor,
                                  │                then upgrade to InteractiveServer
                                  ▼               circuit (SignalR). AuthenticationState
              ┌───────────────────────────────┐   cascades user_id claim — NO DB read.
              │  /login (static SSR, no        │              │
              │  @rendermode)                   │              ▼
              │  - Reads ReturnUrl → shows      │   Canvas shell renders: 48px toolbar
              │    "Please log in to continue." │   (Logout form) + grey area below
              │  - EditForm posts to itself     │   (Phase 3 fills with the SVG canvas)
              └───────────────┬────────────────┘
                               │ EditForm OnValidSubmit (still static SSR — same request)
                               ▼
              ┌────────────────────────────────────────┐
              │  Login handler (runs server-side,       │
              │  cascading HttpContext still live):     │
              │  1. Normalise username (trim+lowercase) │
              │  2. SELECT users WHERE username = @u    │
              │  3a. Not found → INSERT, catch unique   │
              │      race → treat as existing           │
              │  3b. Found → compare plaintext password │
              │  4. HttpContext.SignInAsync(cookie,      │
              │     claim "user_id" = user.Id)          │
              │  5. NavigationManager.NavigateTo("/")    │──▶ back to top: authenticated GET /
              └──────────────────┬───────────────────────┘
                                  ▼
                        PostgreSQL `users` table
                        (Phase 1 schema, unchanged)

  Separately, at any time from the authenticated canvas shell's InteractiveServer circuit:

  ┌────────────────────────────────┐        POST /logout          ┌───────────────────────────┐
  │ Toolbar's plain <form           │ ───── (real browser nav, ──▶ │ MapPost("/logout", ...)    │
  │ method="post" action="/logout"> │        not a SignalR call)   │ - SignOutAsync              │
  │ (native HTML, zero JS,          │                               │ - Redirects to /login       │
  │  antiforgery token included)    │ ◀──────── 302 → /login ────── └───────────────────────────┘
  └──────────────────────────────────┘
```

### Recommended Project Structure

```
src/BlazorCanvas/
├── Components/
│   ├── Pages/
│   │   ├── Home.razor          # → becomes the authenticated canvas shell:
│   │   │                       #   @rendermode InteractiveServer, @attribute [Authorize]
│   │   │                       #   renders the 48px toolbar (Logout) + empty grey body
│   │   ├── Home.razor.css      # toolbar strip styling per 02-UI-SPEC.md
│   │   └── Login.razor         # NEW — static SSR (no @rendermode), the login card
│   ├── Login.razor.css         # NEW — card styling per 02-UI-SPEC.md
│   └── Layout/
│       └── MainLayout.razor    # MUST be gutted of Bootstrap/NavMenu — see Open Questions
├── Data/
│   ├── User.cs                 # unchanged (Phase 1)
│   └── CanvasDbContext.cs      # unchanged (Phase 1) — no schema change this phase
├── Auth/                       # NEW (suggested) — small, focused, not framework ceremony
│   └── UsernameNormalizer.cs   # single source of the trim+lowercase rule (D-44)
└── Program.cs                  # add AddAuthentication/AddCookie/AddAuthorization/
                                 # AddCascadingAuthenticationState, UseAuthentication/
                                 # UseAuthorization, MapPost("/logout", ...)
```

### Pattern 1: Static-SSR sign-in via `EditForm` posting to itself

**What:** `/login` has no `@rendermode` directive (so it renders as static SSR, matching D-34), and uses
`<EditForm Model="Input" FormName="login" OnValidSubmit="LoginAsync">` with
`[SupplyParameterFromForm] private LoginFormModel? Input { get; set; }`. Because the request never
becomes an interactive circuit, the component's `LoginAsync` handler runs as an ordinary server-side POST
handler with a still-live `HttpContext`, and `EditForm` auto-adds the antiforgery token.

**When to use:** Any Blazor Web App form that must call `HttpContext`-dependent APIs (`SignInAsync`,
`SignOutAsync`, setting headers/cookies) — this is the officially documented approach used by the .NET
8–10 Blazor Web App Identity project template's own `Login.razor`.

**Example:**
```csharp
// Source: learn.microsoft.com/aspnet/core/blazor/forms (aspnetcore-10.0) + cookie.md, synthesised
// for this project's plaintext-password check (no ASP.NET Core Identity).
@page "/login"
@using System.ComponentModel.DataAnnotations
@using System.Security.Claims
@using Microsoft.AspNetCore.Authentication
@using Microsoft.AspNetCore.Authentication.Cookies
@using Microsoft.EntityFrameworkCore
@using BlazorCanvas.Data
@inject CanvasDbContext Db
@inject NavigationManager Nav

<EditForm Model="Input" FormName="login" OnValidSubmit="LoginAsync">
    <DataAnnotationsValidator />
    <!-- username / password InputText fields per 02-UI-SPEC.md -->
    @if (errorMessage is not null)
    {
        <p class="error">@errorMessage</p>
    }
    <button type="submit">Log in</button>
</EditForm>

@code {
    [CascadingParameter] private HttpContext? HttpContext { get; set; }
    [SupplyParameterFromForm] private LoginFormModel? Input { get; set; }

    private string? errorMessage;
    private bool showRedirectBanner;

    protected override void OnInitialized()
    {
        Input ??= new();
        // D-51's "redirect-reason" signal: the cookie handler's default ReturnUrlParameter.
        showRedirectBanner = !string.IsNullOrEmpty(HttpContext?.Request.Query["ReturnUrl"]);
    }

    private async Task LoginAsync()
    {
        var username = (Input!.Username ?? "").Trim().ToLowerInvariant();
        if (username.Length == 0) { errorMessage = "Username is required."; return; }
        if (string.IsNullOrEmpty(Input.Password)) { errorMessage = "Password is required."; return; }

        var user = await Db.Users.SingleOrDefaultAsync(u => u.Username == username);
        if (user is null)
        {
            user = new User { Username = username, Password = Input.Password };
            Db.Users.Add(user);
            try
            {
                await Db.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when (IsUniqueViolation(ex))
            {
                // Two tabs raced to create the same new username (D-44's UNIQUE index won).
                Db.Entry(user).State = EntityState.Detached;
                user = await Db.Users.SingleAsync(u => u.Username == username);
                if (user.Password != Input.Password) { errorMessage = "Wrong password. Try again."; return; }
            }
        }
        else if (user.Password != Input.Password)
        {
            errorMessage = "Wrong password. Try again.";
            return;
        }

        var identity = new ClaimsIdentity(
            [new Claim("user_id", user.Id.ToString())],
            CookieAuthenticationDefaults.AuthenticationScheme);

        await HttpContext!.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity));
        // IsPersistent left at its default (false) — a session cookie, per D-26.

        Nav.NavigateTo("/");
    }

    private static bool IsUniqueViolation(DbUpdateException ex) =>
        ex.InnerException is Npgsql.PostgresException { SqlState: Npgsql.PostgresErrorCodes.UniqueViolation };

    private class LoginFormModel
    {
        public string? Username { get; set; }
        public string? Password { get; set; }
    }
}
```
`[CITED: learn.microsoft.com/aspnet/core/blazor/forms]` for the `EditForm`/`[SupplyParameterFromForm]`
shape and antiforgery auto-wiring; `[CITED: learn.microsoft.com/aspnet/core/security/authentication/cookie]`
for the `SignInAsync`/`AuthenticationProperties.IsPersistent` semantics;
`[CITED: learn.microsoft.com/aspnet/core/blazor/components/httpcontext]` for the
`[CascadingParameter] HttpContext?` availability in static SSR; the login-flow control logic itself
(create-on-unknown, unique-violation race handling) is `[ASSUMED]` — it is this project's own synthesis
of D-17/D-44 against the Phase 1 schema, not lifted from a Microsoft sample, and should be reviewed by
the planner/executor rather than treated as a verified external pattern.

### Pattern 2: Reading `user_id` on the InteractiveServer canvas with no DB lookup

**What:** `Home.razor` gets `@rendermode InteractiveServer` and `@attribute [Authorize]`. Inside, a
cascading `Task<AuthenticationState>` (provided automatically once `AddCascadingAuthenticationState()` is
registered — no `<CascadingAuthenticationState>` wrapper needed for Blazor Web Apps `.NET 8+`) carries
the `ClaimsPrincipal` that was already resolved from the cookie when the circuit's SignalR connection was
established. Reading the claim is synchronous once the cascading task completes; it never touches EF Core.

**When to use:** Exactly this case — any InteractiveServer page that needs the signed-in identity without
paying for a database round trip on every page load.

**Example:**
```csharp
// Source: learn.microsoft.com/aspnet/core/blazor/security/authentication-state (aspnetcore-10.0),
// adapted to read this project's custom "user_id" claim instead of ClaimTypes.Name.
@page "/"
@rendermode InteractiveServer
@attribute [Authorize]

@code {
    [CascadingParameter] private Task<AuthenticationState>? AuthStateTask { get; set; }

    private int userId;

    protected override async Task OnInitializedAsync()
    {
        var state = await AuthStateTask!;
        userId = int.Parse(state.User.FindFirst("user_id")!.Value);
        // No `Db.Users.FindAsync(userId)` here — D-51 forbids it. Figure queries (Phase 3)
        // use `userId` directly: `WHERE user_id = @userId`.
    }
}
```
`[CITED: learn.microsoft.com/aspnet/core/blazor/security/authentication-state]`

### Pattern 3: The plain, non-interactive Logout form

**What:** A genuine HTML `<form method="post" action="/logout">` rendered *inside* the InteractiveServer
canvas shell, but with **no** `@onsubmit`/`@formname` — it is not Blazor's "enhanced form" mechanism at
all, just an ordinary browser navigation the way the UI-SPEC and D-51/D-56 require ("a real HTTP
round-trip"). Because it targets a route outside the Razor Components endpoint (`/logout`, a minimal
API), Blazor's automatic per-`EditForm` antiforgery wiring does not apply; add the token manually.

**Example:**
```razor
@* Source: pattern synthesised from learn.microsoft.com/aspnet/core/blazor/forms's manual
   <AntiforgeryToken /> guidance (for plain <form> elements) plus the additional-scenarios.md
   XSRF-token-on-a-logout-form precedent (§"For an XSRF token passed to a component"). *@
@inject Microsoft.AspNetCore.Components.Forms.AntiforgeryStateProvider Antiforgery

@{ var token = Antiforgery.GetAntiforgeryToken(); }

<form method="post" action="/logout">
    <input type="hidden" name="@token!.Name" value="@token.Value" />
    <button type="submit" aria-label="Log out">...</button>
</form>
```
```csharp
// Program.cs
app.MapPost("/logout", async (HttpContext context, IAntiforgery antiforgery) =>
{
    await antiforgery.ValidateRequestAsync(context);
    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.LocalRedirect("/login");
});
```
`[CITED: learn.microsoft.com/aspnet/core/blazor/forms]` for `<AntiforgeryToken />`/manual antiforgery on
plain forms; `[CITED: learn.microsoft.com/aspnet/core/security/authentication/cookie]` for `SignOutAsync`;
the minimal-API `IAntiforgery.ValidateRequestAsync` wiring is `[ASSUMED]` (well-established public API,
not fetched from an official minimal-API-specific antiforgery doc page this session) — flagged in the
Assumptions Log because minimal API endpoints, unlike Razor Components endpoints, are **not**
antiforgery-protected by default, so omitting this line silently drops CSRF protection on `/logout`
rather than failing loudly.

### Anti-Patterns to Avoid

- **Relying on `AuthorizeRouteView`'s `<NotAuthorized>` template to redirect `/` to `/login`.** This
  template only fires for the Blazor Router's *client-side* navigation inside an already-established
  interactive circuit. For the initial HTTP GET to a protected page — the case AUTH-02 actually
  describes — authorization is decided by ASP.NET Core's routing/authorization middleware before any
  component renders; the redirect is `CookieAuthenticationOptions.LoginPath`, full stop.
  `[CITED: learn.microsoft.com/aspnet/core/blazor/security]`
- **Putting `[Authorize]` only on `Home.razor` without registering `builder.Services.AddAuthorization()`.**
  `[Authorize]` is inert without the authorization service registration; the page renders as if
  unprotected, silently.
- **Setting `AuthenticationProperties.IsPersistent = true`.** This produces a cookie with an `Expires`
  attribute — a persistent cookie that survives browser close, which is exactly D-26's *rejected*
  alternative. Leave `IsPersistent` at its default `false`.
- **Doing a DB lookup for the user on every canvas page load "just to be safe."** D-51 explicitly forbids
  this; it also silently reintroduces the cost the ADR spent effort eliminating.
- **Using `EditForm.OnSubmit` and manually calling `EditContext.Validate()`, or skipping
  `DataAnnotationsValidator`/`required`, for the login form.** The UI-SPEC's three server-side error
  variants (empty username / empty password / wrong password) must be reachable even with the browser's
  native `required` bypassed — this is exactly the point of D-45's "server-side validation is mandatory,
  not optional" note reproduced in the UI-SPEC's Accessibility section. Custom checks (as in Pattern 1
  above) rather than relying purely on `[Required]` attributes fit the three-specific-messages
  requirement better than generic data-annotation error text.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Session cookie issuance/validation | A custom `Set-Cookie` header + a hand-rolled middleware that reads it back | `AddAuthentication().AddCookie()` + `SignInAsync`/`SignOutAsync` | Handles encryption (Data Protection), `SameSite`, expiry semantics, and the `[Authorize]`/`LoginPath` integration for free. D-34 explicitly considered and rejected the hand-rolled version. |
| CSRF protection on form posts | A manually generated random token in a hidden field, checked by a custom filter | Blazor's built-in antiforgery (`AddRazorComponents()` + `UseAntiforgery()` + `<AntiforgeryToken />` for plain forms) | Already wired for the `EditForm` case in Phase 1's scaffolded `Program.cs`; the plain-form case just needs the one extra `<AntiforgeryToken />`/`AntiforgeryStateProvider` line, not a new subsystem. |
| Reading "who is logged in" inside an interactive component | A custom `CircuitHandler` that captures `HttpContext.User` into a singleton (this *is* a documented advanced pattern, but not needed here) | The framework's cascading `Task<AuthenticationState>` | The advanced `CircuitHandler`/`UserService` pattern documented in `additional-scenarios.md` exists for cases needing the user in a *non-component* service (e.g. a `DelegatingHandler`). This app's canvas reads the claim directly inside the one page component that needs it — the simpler cascading-parameter pattern is sufficient and is what D-51 describes. |

**Key insight:** Every mechanism this phase needs is a named, documented ASP.NET Core / Blazor feature,
not a gap to fill with custom code. The only genuinely custom logic is the ~20-line create-or-compare
login handler (Pattern 1) and the username normalisation helper — both pure business logic already fully
specified by D-17/D-44, not framework plumbing.

## Common Pitfalls

### Pitfall 1: Assuming `/login` needs `@rendermode` at all
**What goes wrong:** Adding `@rendermode InteractiveServer` (or any render mode) to `Login.razor` "for
consistency" breaks `SignInAsync` — the component would prerender statically, spin up a circuit, and by
the time `LoginAsync` runs interactively, the HTTP response has already started and `HttpContext` is
`null` (per the cascading-parameter contract: *"The value is `null` during interactive rendering."*).
**Why it happens:** every other page in the eventual app (`/`) *does* need InteractiveServer, so it's an
easy copy-paste mistake.
**How to avoid:** `Login.razor` has **no `@rendermode` directive at all** — omitting it is what makes it
static SSR, matching D-34 exactly.
**Warning signs:** `NullReferenceException` on `HttpContext!.SignInAsync(...)`, or (per the docs'
"Don't set or modify headers after the response starts" section) a `System.InvalidOperationException:
'Headers are read-only, response has already started.'` if streaming rendering was also mistakenly
enabled. `[CITED: learn.microsoft.com/aspnet/core/blazor/components/httpcontext]`

### Pitfall 2: Case-insensitive username — an app-layer rule, not a DB-layer one
**What goes wrong:** Trusting Postgres's `UNIQUE` index on `username` to be case-insensitive by itself.
It is not — Postgres's default collation is case-sensitive, so `"Egor"` and `"egor"` would insert as two
distinct rows, each satisfying the index.
**Why it happens:** the ADR (D-44) and the canonical schema comment both call this out explicitly, and
Phase 1's actual `CanvasDbContext.cs` confirms the index is a plain `entity.HasIndex(u =>
u.Username).IsUnique();` with **no** `citext` extension and **no** case-insensitive collation configured
— case-insensitivity is 100% an application responsibility this phase must implement.
**How to avoid:** Every write and every lookup path normalises with `.Trim().ToLowerInvariant()` **before**
touching the database (Pattern 1 above does this at the single entry point — do not duplicate the rule
elsewhere).
`[VERIFIED: codebase — src/BlazorCanvas/Data/CanvasDbContext.cs, lines 33-39]`
**Warning signs:** a returning user who typed a capital letter once lands on a brand-new empty canvas —
this reads to the user as "my work vanished" (the ADR's own words for this exact failure mode).

### Pitfall 3: The create-on-unknown-username race
**What goes wrong:** two browser tabs submit the same brand-new username at nearly the same instant. Both
do a `SELECT` that finds nothing, both attempt an `INSERT`; Postgres's `UNIQUE` constraint on `username`
lets exactly one succeed and throws a unique-violation (`SqlState 23505`) on the other. If that exception
is left uncaught, the second tab's user gets a raw 500 / "Could not save" error for what should be a
completely ordinary "log in to the account that already exists" outcome.
**Why it happens:** the SELECT-then-INSERT sequence is not atomic; nothing in the ADR calls this race out
explicitly (it is an inference from D-44's UNIQUE constraint + D-17's create-on-unknown flow), so it is
easy to miss in a first pass.
**How to avoid:** catch `DbUpdateException` wrapping an `Npgsql.PostgresException` with
`SqlState == PostgresErrorCodes.UniqueViolation`, re-query the now-existing row, and fall through to the
normal password-compare path (Pattern 1). `[ASSUMED]` — this specific race and its handling is this
project's own synthesis from the schema + ADR, not lifted from an official sample; flagged in the
Assumptions Log.
**Warning signs:** intermittent "Could not save — is the database running?" errors (D-45's generic
message) specifically when two tabs race to register the *same brand-new* username — reproducible only
under concurrent load, easy to miss in solo manual testing.

### Pitfall 4: Antiforgery ordering and the plain logout `<form>`
**What goes wrong:** (a) placing `app.UseAntiforgery()` *before* `app.UseAuthentication()`/
`app.UseAuthorization()` in `Program.cs` — the docs are explicit that antiforgery must come *after* both;
(b) forgetting that a **plain** `<form>` (not `EditForm`) does **not** get an antiforgery token added
automatically — only `EditForm` instances do — so the Logout form (which D-34/D-56 require to be a plain
form, not an interactive component) needs `<AntiforgeryToken />`/`AntiforgeryStateProvider` added by
hand; (c) forgetting that a **minimal API** endpoint like `MapPost("/logout", ...)` is not automatically
antiforgery-protected the way a Razor Components endpoint is, so a token present in the form but never
validated server-side achieves nothing.
**Why it happens:** the two form mechanisms (`EditForm` vs. plain `<form>`) look almost identical in
markup but have different automatic-protection guarantees; this is precisely the kind of "silent wherever
the framework was in the room" gap the ROADMAP calls out for this phase.
**How to avoid:** `UseAuthentication(); UseAuthorization(); UseAntiforgery();` in that order (already
partially true — Phase 1's `Program.cs` already calls `UseAntiforgery()` near the end; this phase must
insert the two new calls *before* it, not after); add `<AntiforgeryToken />`-equivalent markup to the
Logout form; validate with `IAntiforgery.ValidateRequestAsync` inside the `/logout` handler.
**Warning signs:** logout silently works even with a forged/absent token (protection never wired), or —
opposite failure — a `400 Bad Request` on every legitimate logout click (ordering wrong).
`[CITED: learn.microsoft.com/aspnet/core/blazor/forms]`

### Pitfall 5: Dropping the Bootstrap/`MainLayout` scaffold — a genuinely open, cross-phase question
**What goes wrong:** `MainLayout.razor` (unchanged since `dotnet new blazor`) still renders a
Bootstrap sidebar + `NavMenu` with Home/Counter/Weather demo links, and `wwwroot/app.css` still pulls in
`lib/bootstrap`. Neither `02-UI-SPEC.md` nor `03-UI-SPEC.md` (Phase 3, which this phase's spec explicitly
inherits its design-system base from) want any of that chrome on `/login` or on the canvas shell — but
this is flagged as *unresolved* in both UI-SPECs' Open Questions, not locked by an ADR.
**Why it happens:** it's leftover scaffold, not something anyone explicitly decided to keep or remove.
**How to avoid:** this is a planning decision, not a research one — see Open Questions below. The
research finding is only that whichever direction is chosen, `Login.razor` (no toolbar, centered card)
and `Home.razor` (48px toolbar, no sidebar) need **different** effective layouts, so a single shared
`MainLayout` with a sidebar cannot serve both without modification.
**Warning signs:** the login card renders next to a Bootstrap sidebar with "Counter"/"Weather" links —
visibly wrong against the UI-SPEC's "no sidebar/nav" requirement.

## Code Examples

### `Program.cs` — the full set of additions this phase makes

```csharp
// Source: synthesised from learn.microsoft.com/aspnet/core/security/authentication/cookie and
// learn.microsoft.com/aspnet/core/blazor/security/authentication-state, both aspnetcore-10.0.
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

// ... existing builder.Services.AddRazorComponents().AddInteractiveServerComponents(); ...
// ... existing builder.Services.AddDbContext<CanvasDbContext>(...); ...

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        // Session cookie (D-26): IsPersistent stays false at sign-in time (Pattern 1), so no
        // Expires/Max-Age is ever written to the cookie — it dies with the browser. ExpireTimeSpan
        // below only bounds the encrypted ticket's server-side validity; it is not what makes the
        // cookie a "session" cookie.
        options.ExpireTimeSpan = TimeSpan.FromDays(365);
    });
builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();

var app = builder.Build();

// ... existing migration-apply block ...
// ... existing app.UseExceptionHandler / UseHsts / UseStatusCodePagesWithReExecute / UseHttpsRedirection ...

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();   // must stay AFTER UseAuthentication/UseAuthorization (already true here)

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapPost("/logout", async (HttpContext context, IAntiforgery antiforgery) =>
{
    await antiforgery.ValidateRequestAsync(context);
    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.LocalRedirect("/login");
});

app.Run();
```

## State of the Art

Nothing in this phase has an "old approach" — cookie authentication in Blazor Web Apps is a .NET 8+
concept (the render-mode split described in D-34 didn't exist before Blazor Web Apps unified
Server/WebAssembly in .NET 8), and .NET 10 makes no breaking changes to the APIs used here relative to
.NET 8/9. One .NET 10-specific note surfaced during research: starting with ASP.NET Core 10, *known API
endpoints* (a new concept) no longer redirect to a login page on 401 — they return raw status codes
instead. This does **not** affect this phase: `Home.razor`'s protected page is a Razor Components
endpoint, not a "known API" endpoint, so the classic `LoginPath` redirect behavior this research relies on
is unaffected. `[CITED: learn.microsoft.com/aspnet/core/security/authentication/cookie]`

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|----------------|
| A1 | The create-on-unknown-username race (two tabs, same new username) should be handled by catching a unique-violation `DbUpdateException` and re-querying, rather than e.g. a `SELECT ... FOR UPDATE` or a serializable transaction. | Common Pitfalls #3, Pattern 1 | Low — this is standard optimistic-insert-and-catch practice for Postgres+EF Core, but it is this project's own synthesis, not verified against an official sample this session. If wrong, the fallback failure mode is a rare, reproducible-under-load 500 error, not silent data corruption. |
| A2 | Minimal-API endpoints (`MapPost("/logout", ...)`) require an explicit `IAntiforgery.ValidateRequestAsync` call to get CSRF protection — they are not automatically covered by `UseAntiforgery()` middleware the way Razor Components/`EditForm` endpoints are. | Pattern 3, Common Pitfalls #4 | Low-medium — if this project's minimal-API antiforgery behavior differs from what's assumed, `/logout` could either (a) silently lack CSRF protection (low severity: forcing a logout is a minor annoyance, not data loss) or (b) reject every legitimate logout with 400 if validation is wired but the token isn't actually reaching the form correctly. Either way it is easily caught by manual testing (click Logout, confirm success) before phase completion. |
| A3 | The Bootstrap/`MainLayout` scaffold should be dropped for both `/login` and the authenticated shell (per both UI-SPECs' "proposed: yes" note) rather than kept and worked around. | Common Pitfalls #5, Open Questions | Low — purely cosmetic; both UI-SPECs already lean toward removal, this just needs final sign-off before the planner commits to a specific `MainLayout`/`@layout` restructuring. |

**If this table is empty:** N/A — see rows above.

## Open Questions

1. **Drop the Bootstrap/`MainLayout` scaffold — carried over from Phase 3's spec, still unresolved.**
   - What we know: both `02-UI-SPEC.md` and `03-UI-SPEC.md` flag this as their one open item, and both
     lean "yes, drop it, build from tokens in dedicated `*.razor.css` files."
   - What's unclear: the exact restructuring — does `MainLayout.razor` become an empty pass-through
     (`@Body` only, no sidebar), with `Login.razor` and `Home.razor` each owning their own chrome via
     `*.razor.css`? Or does `/login` opt out of `MainLayout` entirely via a second, minimal layout
     component?
   - Recommendation: since this app has exactly two pages and neither wants a sidebar, the simplest fix
     is to gut `MainLayout.razor` down to `@Body` with no sidebar/`NavMenu`/Bootstrap link, and let each
     page's own component (toolbar strip for `Home.razor`, centered card for `Login.razor`) supply 100%
     of its own visual chrome via component-scoped CSS. This keeps one layout, matching "same app, same
     shell" language in `02-UI-SPEC.md`. Confirm with the user before the plan locks this in — it is
     UI-layer, not ADR-locked.

2. **Should a successful login honor `ReturnUrl`, or always redirect to `/`?**
   - What we know: D-51's routes table lists exactly one protected page (`/`); there is no other
     protected destination in this app.
   - What's unclear: nothing functionally — `ReturnUrl` can only ever be `/` in practice, since `/` is
     the only `[Authorize]`-protected route this app has.
   - Recommendation: always redirect to `/` after a successful login (as in Pattern 1's
     `Nav.NavigateTo("/")`), and use `ReturnUrl`'s mere *presence* only for the redirect-reason banner,
     never for its value. Simpler, and avoids open-redirect concerns from an unvalidated `ReturnUrl`
     value entirely.

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|--------------|-----------|---------|----------|
| .NET 10 SDK | Building/running the app | ✓ | 10.0.301 (per D-28, verified in Phase 1) | — |
| PostgreSQL 17 (Docker Compose) | `users` table read/write during login | ✓ | Running via `docker-compose.yml`, port 5433 on this dev machine (Phase 1 deviation, user-approved) | — |
| Cookie auth / antiforgery / `Microsoft.AspNetCore.Components.Authorization` | This entire phase | ✓ | In-box with `net10.0` shared framework | — |

**Missing dependencies with no fallback:** None.
**Missing dependencies with fallback:** None — this phase introduces no new external dependency.

## Security Domain

### Applicable ASVS Categories

| ASVS Category | Applies | Standard Control |
|----------------|---------|--------------------|
| V2 Authentication | Yes, narrowly | Cookie-based session identification per D-26/D-34. **Password hashing/salting is explicitly out of scope** — D-08 locks plaintext passwords as a deliberate, accepted non-security choice for this throwaway learning project. Do not add hashing; it would silently contradict a locked decision. |
| V3 Session Management | Yes | ASP.NET Core cookie authentication's built-in session cookie (`IsPersistent = false`), Data-Protection-encrypted ticket. No custom session-token scheme. |
| V4 Access Control | Yes | `[Authorize]` on `Home.razor` + `CookieAuthenticationOptions.LoginPath`; one user's `user_id` claim scopes all figure queries in later phases — enforced at the query level (Phase 3+), not this phase's concern beyond establishing the claim. |
| V5 Input Validation | Yes | Server-side validation of username (non-empty after trim) and password (non-empty) in the login handler — required regardless of native HTML `required`, per D-45's "server-side validation is mandatory" and the UI-SPEC's Accessibility section. |
| V6 Cryptography | **Explicitly N/A** | D-08 locks plaintext password storage/comparison as a deliberate, recorded exception. Do not introduce hashing, salting, or any cryptographic password handling — doing so would silently override a locked ADR decision the plan must not re-litigate. |
| V13 CSRF (cross-cutting with V4 in ASVS 5.0) | Yes | Blazor's built-in antiforgery for the `EditForm`-based `/login` (automatic) and manual `IAntiforgery.ValidateRequestAsync` for the plain `/logout` form (Pattern 3 / Common Pitfall 4). |

### Known Threat Patterns for this stack

| Pattern | STRIDE | Standard Mitigation |
|---------|--------|-----------------------|
| CSRF on `POST /logout` (forcing a victim's browser to log them out via a forged cross-site request) | Spoofing / Tampering | Antiforgery token validated server-side (Pattern 3). Low severity here (logout is non-destructive per D-25), but cheap to close. |
| Username enumeration via distinct "wrong password" vs. "unknown username" error copy | Information Disclosure | **Explicitly accepted, not mitigated** — D-17/UI-SPEC's Copywriting Contract *requires* distinct messages ("Wrong password. Try again." vs. silent account creation), and D-08 already treats this app as having no real security boundary to protect. Do not "fix" this by unifying error messages; it would contradict the locked UI-SPEC. |
| Open redirect via an unvalidated `ReturnUrl` | Tampering | Not exploitable in this app's design — see Open Question 2: `ReturnUrl`'s value is never used for navigation, only its presence is checked. If a future phase ever changes this, `ReturnUrl` must be validated as a local path before use. |
| Session fixation / cookie theft | Elevation of Privilege | Standard ASP.NET Core cookie auth defaults (HttpOnly, Data-Protection-encrypted) apply unchanged; no custom cookie handling is introduced that would weaken this. |

## Sources

### Primary (HIGH confidence)
- [ASP.NET Core Blazor authentication and authorization (aspnetcore-10.0)](https://learn.microsoft.com/en-us/aspnet/core/blazor/security/?view=aspnetcore-10.0) — `[Authorize]` on `@page` components, static-SSR `<NotAuthorized>` limitation, `LoginPath` redirect behavior, antiforgery summary
- [ASP.NET Core Blazor authentication state (aspnetcore-10.0)](https://learn.microsoft.com/en-us/aspnet/core/blazor/security/authentication-state?view=aspnetcore-10.0) — `AddCascadingAuthenticationState()`, `AuthorizeRouteView`, `AuthenticationStateProvider`
- [ASP.NET Core Blazor forms overview (aspnetcore-10.0)](https://learn.microsoft.com/en-us/aspnet/core/blazor/forms/?view=aspnetcore-10.0) — `EditForm`/`[SupplyParameterFromForm]`/`FormName`, antiforgery for `EditForm` vs. plain `<form>`, middleware ordering (`UseAntiforgery` after `UseAuthentication`/`UseAuthorization`)
- [Use cookie authentication without ASP.NET Core Identity (aspnetcore-10.0)](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/cookie?view=aspnetcore-10.0) — `AddAuthentication().AddCookie()`, `SignInAsync`/`SignOutAsync`, `AuthenticationProperties.IsPersistent` session-vs-persistent semantics, .NET 10's "known API endpoints" 401 note
- [IHttpContextAccessor/HttpContext in ASP.NET Core Blazor apps (aspnetcore-10.0)](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/httpcontext?view=aspnetcore-10.0) — `[CascadingParameter] HttpContext?` validity during static SSR, `null` during interactive rendering, header-write timing error
- Phase 1 codebase, read directly this session: `src/BlazorCanvas/Program.cs`, `Data/CanvasDbContext.cs`, `Data/User.cs`, `Components/App.razor`, `Components/Routes.razor`, `Components/Pages/Home.razor`, `Components/Layout/MainLayout.razor`, `BlazorCanvas.csproj`

### Secondary (MEDIUM confidence)
- [ASP.NET Core server-side and Blazor Web App additional security scenarios (aspnetcore-10.0)](https://learn.microsoft.com/en-us/aspnet/core/blazor/security/additional-scenarios?view=aspnetcore-10.0) — `CircuitHandler`/`UserService` pattern (considered, not needed for this phase), XSRF-token-on-a-logout-form precedent
- WebSearch-surfaced community examples of minimal-API `IAntiforgery.ValidateRequestAsync` usage, cross-checked against the official antiforgery docs' general guidance but not fetched from an official minimal-API-specific page this session (see Assumption A2)

### Tertiary (LOW confidence)
- None retained — where a WebSearch result could not be corroborated against an official Microsoft Learn page, it was either dropped or explicitly tagged `[ASSUMED]` in the text above (see Assumptions Log).

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — everything is in-box, directly confirmed against Phase 1's actual `.csproj` and official docs; no version-drift risk since these are shared-framework APIs, not floating NuGet packages.
- Architecture (render-mode boundary, `LoginPath` redirect mechanism): HIGH — the single most important and most easily-gotten-wrong fact of this phase (that `AuthorizeRouteView`'s `NotAuthorized` template does *not* drive the `/` → `/login` redirect) is directly quoted from Microsoft's own docs, not inferred.
- Login/create-race handling logic: MEDIUM — sound engineering practice, but this project's own synthesis rather than a verified official pattern; flagged in Assumptions Log (A1).
- Pitfalls: HIGH — each pitfall traces either to an explicit doc statement (antiforgery ordering, static-SSR `HttpContext` nullability) or to a directly-read fact in the Phase 1 codebase (the case-sensitive DB index).

**Research date:** 2026-07-15
**Valid until:** 30 days (stable ASP.NET Core 10 APIs; re-verify if the project's `net10.0` TFM or the referenced NuGet package versions change before Phase 2 executes)

## RESEARCH COMPLETE
