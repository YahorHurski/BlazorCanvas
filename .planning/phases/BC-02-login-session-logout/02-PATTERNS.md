# Phase 2: Login, Session & Logout - Pattern Map

**Mapped:** 2026-07-15
**Files analyzed:** 9 (new/modified)
**Analogs found:** 6 exact-or-role / 3 no-analog (pure framework template patterns from RESEARCH.md)

## File Classification

| New/Modified File | Role | Data Flow | Closest Analog | Match Quality |
|--------------------|------|-----------|-----------------|----------------|
| `src/BlazorCanvas/Components/Pages/Login.razor` | component (static-SSR page, form POST) | request-response | `src/BlazorCanvas/Components/Pages/Counter.razor` (only existing `@page` component with `@code`) | role-match (no auth/EditForm analog exists) |
| `src/BlazorCanvas/Components/Pages/Login.razor.css` | component style | — | `src/BlazorCanvas/Components/Layout/MainLayout.razor.css` (only scoped-CSS example in repo) | role-match |
| `src/BlazorCanvas/Components/Pages/Home.razor` (modified) | component (InteractiveServer, `[Authorize]` shell) | request-response | itself (Phase 1 scaffold version) | exact — same file, modify in place |
| `src/BlazorCanvas/Components/Pages/Home.razor.css` | component style (48px toolbar) | — | `src/BlazorCanvas/Components/Layout/MainLayout.razor.css` (`.top-row` 3.5rem strip is the closest existing "horizontal bar" pattern) | role-match |
| `src/BlazorCanvas/Components/Layout/MainLayout.razor` (modified — gut sidebar) | layout | — | itself (Phase 1 scaffold version) | exact — same file, modify in place |
| `src/BlazorCanvas/Program.cs` (modified) | config/startup wiring | request-response | itself (Phase 1 version, already has `AddDbContext`/migration-retry/`UseAntiforgery` pattern to extend) | exact — same file, modify in place |
| `src/BlazorCanvas/Auth/UsernameNormalizer.cs` | utility | transform | `src/BlazorCanvas/Geometry/Normalisation.cs` (small static pure-function class, same "single source of a normalisation rule" shape) | role-match |
| `POST /logout` minimal-API endpoint (added in `Program.cs`) | route (minimal API) | request-response | none — no existing minimal-API endpoint in the codebase (only `MapRazorComponents`/`MapStaticAssets`) | no analog — see below |
| Login/user lookup logic (inline in `Login.razor`'s `@code`, not a separate service per RESEARCH.md's Pattern 1 — no `AuthService`/repository file is proposed) | inline handler, CRUD | CRUD | `src/BlazorCanvas/Data/CanvasDbContext.cs` (the `DbSet<User>` it queries/writes) | role-match (data access shape, not a service-file analog) |

## Pattern Assignments

### `src/BlazorCanvas/Components/Pages/Login.razor` (component, request-response, static SSR)

**Analog:** `src/BlazorCanvas/Components/Pages/Counter.razor` (structure only — the *only* existing
`@page` component with a `@code` block) + RESEARCH.md's Pattern 1 (the authoritative content pattern,
since no auth/form analog exists in this codebase yet).

**Existing page-component shape to copy** (`Counter.razor`, full file, 20 lines):
```razor
@page "/counter"
@rendermode InteractiveServer

<PageTitle>Counter</PageTitle>

<h1>Counter</h1>
<p role="status">Current count: @currentCount</p>
<button class="btn btn-primary" @onclick="IncrementCount">Click me</button>

@code {
    private int currentCount = 0;
    private void IncrementCount() { currentCount++; }
}
```
**Load-bearing deviation (Pitfall 1 in RESEARCH.md):** `Login.razor` must **omit** the `@rendermode`
line entirely — do not copy that line from `Counter.razor`. Everything else about the "`@page` +
`<PageTitle>` + markup + `@code` block" shape is the correct template to follow.

**Core pattern (RESEARCH.md Pattern 1, this project's authoritative login-handler code — no in-repo
analog exists, so this IS the pattern to implement verbatim, adapted to `User`/`CanvasDbContext`):**
```csharp
[CascadingParameter] private HttpContext? HttpContext { get; set; }
[SupplyParameterFromForm] private LoginFormModel? Input { get; set; }

private async Task LoginAsync()
{
    var username = UsernameNormalizer.Normalize(Input!.Username);
    if (username.Length == 0) { errorMessage = "Username is required."; return; }
    if (string.IsNullOrEmpty(Input.Password)) { errorMessage = "Password is required."; return; }

    var user = await Db.Users.SingleOrDefaultAsync(u => u.Username == username);
    if (user is null) { /* create-and-catch-unique-violation, see RESEARCH.md Pattern 1 in full */ }
    else if (user.Password != Input.Password) { errorMessage = "Wrong password. Try again."; return; }

    var identity = new ClaimsIdentity(
        [new Claim("user_id", user.Id.ToString())],
        CookieAuthenticationDefaults.AuthenticationScheme);
    await HttpContext!.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
    Nav.NavigateTo("/");
}
```
This exact handler (full version with the race-condition catch) is in
`.planning/phases/BC-02-login-session-logout/02-RESEARCH.md` under "Pattern 1" (lines ~204-290) — copy
from there, not from any codebase file, since this is genuinely new logic.

**Data-access pattern to copy** (`src/BlazorCanvas/Data/CanvasDbContext.cs` lines 11-21 — how the
existing `DbSet<User>` is exposed and injected):
```csharp
public class CanvasDbContext : DbContext
{
    public CanvasDbContext(DbContextOptions<CanvasDbContext> options) : base(options) { }
    public DbSet<User> Users => Set<User>();
    public DbSet<Figure> Figures => Set<Figure>();
    ...
}
```
`Login.razor` injects this with `@inject CanvasDbContext Db` exactly as RESEARCH.md's Pattern 1 shows;
there is no repository/service indirection anywhere else in the codebase to match, so querying
`Db.Users` directly inside the page component is consistent with the project's existing "no API
layer" (D-07) style.

---

### `src/BlazorCanvas/Components/Pages/Login.razor.css` / `Home.razor.css` (component style, scoped CSS)

**Analog:** `src/BlazorCanvas/Components/Layout/MainLayout.razor.css` — the only `*.razor.css` file in
the repo, demonstrating the scoped-CSS `::deep` convention and media-query breakpoint style used
project-wide.

**Convention to copy** (lines 1-9, 15-22):
```css
.page {
    position: relative;
    display: flex;
    flex-direction: column;
}

.top-row {
    background-color: #f7f7f7;
    border-bottom: 1px solid #d6d5d5;
    justify-content: flex-end;
    height: 3.5rem;
    display: flex;
    align-items: center;
}
```
This `.top-row` block is the closest existing precedent for `Home.razor.css`'s 48px toolbar strip
(`height: 48px`, `display: flex`, `align-items: center`, `margin-left: auto` on the right-aligned
group) — same shape, different token values per 02-UI-SPEC.md (background `#DCE0E5`, no border-bottom
per D-43's "no CSS border" rule — do not copy the `border-bottom` line). All actual colors/spacing must
come from 02-UI-SPEC.md's Color/Spacing tables, not from this scaffold's hardcoded greys.

**`::deep` selector convention** (lines 24-31, for styling child component markup, e.g. inside the
Logout `<form>` if it becomes a nested component):
```css
.top-row ::deep a, .top-row ::deep .btn-link {
    white-space: nowrap;
    margin-left: 1.5rem;
    text-decoration: none;
}
```

---

### `src/BlazorCanvas/Components/Pages/Home.razor` (modified — becomes authenticated canvas shell)

**Analog:** itself, Phase 1 scaffold (full file, 8 lines).

**Current state (to be replaced):**
```razor
@page "/"

<PageTitle>Home</PageTitle>

<h1>Hello, world!</h1>

Welcome to your new app.
```

**Target shape per RESEARCH.md Pattern 2** (the `@rendermode`/`[Authorize]`/cascading-auth-state
pattern — no in-repo analog since no `[Authorize]` page exists yet; this is the authoritative source):
```razor
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
    }
}
```
Note `Counter.razor`'s `@rendermode InteractiveServer` line (already shown above) is the correct
existing precedent for *that one directive* — copy it verbatim; the `[Authorize]` attribute and
cascading-auth-state block have no existing analog and must come from RESEARCH.md Pattern 2.

---

### `src/BlazorCanvas/Components/Layout/MainLayout.razor` (modified — gut sidebar/Bootstrap)

**Analog:** itself, Phase 1 scaffold (full file).

**Current state (to be gutted per RESEARCH.md's Assumption A3 / Open Question 1 — confirm with user
before executing):**
```razor
@inherits LayoutComponentBase

<div class="page">
    <div class="sidebar">
        <NavMenu />
    </div>
    <main>
        <div class="top-row px-4">
            <a href="https://learn.microsoft.com/aspnet/core/" target="_blank">About</a>
        </div>
        <article class="content px-4">
            @Body
        </article>
    </main>
</div>
<div id="blazor-error-ui" data-nosnippet> ... </div>
```
**Recommended target (per RESEARCH.md's own recommendation under Open Question 1):** strip to `@Body`
only — no `.sidebar`, no `<NavMenu />`, no Bootstrap-dependent `.top-row`/`.content` wrapper — leaving
each page (`Login.razor`, `Home.razor`) to supply 100% of its own chrome via its own `*.razor.css`. The
`#blazor-error-ui` block is framework infrastructure (unrelated to Bootstrap/NavMenu) and should be
kept. This is a **planning decision flagged as unresolved by RESEARCH.md** — the planner must confirm
with the user before locking in the exact restructuring shape.

---

### `src/BlazorCanvas/Program.cs` (modified)

**Analog:** itself, Phase 1 version (full file, 58 lines).

**Existing pattern to extend — migration-retry block already present** (lines 21-39), demonstrates the
project's error-handling convention (fail loudly, retry only transient connection errors):
```csharp
{
    const int maxAttempts = 10;
    var delay = TimeSpan.FromSeconds(2);
    for (var attempt = 1; attempt <= maxAttempts; attempt++)
    {
        try
        {
            using var scope = app.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<CanvasDbContext>();
            db.Database.Migrate();
            break;
        }
        catch (NpgsqlException) when (attempt < maxAttempts)
        {
            await Task.Delay(delay);
        }
    }
}
```

**Existing service-registration pattern to extend** (lines 8-13):
```csharp
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddDbContext<CanvasDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Canvas")));
```
Insert the new `AddAuthentication().AddCookie(...)`, `AddAuthorization()`, and
`AddCascadingAuthenticationState()` calls immediately after this block, exactly as RESEARCH.md's
"Program.cs — the full set of additions" code example shows (lines 499-543 of 02-RESEARCH.md).

**Existing middleware-ordering pattern to extend** (lines 41-55 — note `UseAntiforgery()` is already
present and already positioned after the HTTPS/HSTS/status-code middleware; the new
`UseAuthentication()`/`UseAuthorization()` calls must be inserted **before** this existing
`UseAntiforgery()` line, per Pitfall 4):
```csharp
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();   // <-- new UseAuthentication()/UseAuthorization() calls go directly above this line

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();
```
The `MapPost("/logout", ...)` minimal-API endpoint (no existing analog — see "No Analog Found" below)
is added after `MapRazorComponents<App>()...` per RESEARCH.md's code example.

---

### `src/BlazorCanvas/Auth/UsernameNormalizer.cs` (utility, transform)

**Analog:** `src/BlazorCanvas/Geometry/Normalisation.cs` — closest existing "small static pure-function
utility class, single source of one normalisation rule" shape in the codebase (Phase 1's geometry
core).

**Convention to copy** (full file, 27 lines — static class, static method, XML-doc comment naming the
ADR/rule it implements, no DI/instance state):
```csharp
namespace BlazorCanvas.Geometry;

/// <summary>
/// The canonical order on write (D-41). Applied once, before the INSERT, in exactly one place.
/// </summary>
public static class Normalisation
{
    public static Box Normalise(FigureType type, Box b)
    {
        if (type == FigureType.Line)
        {
            var swap = b.X1 > b.X2 || (b.X1 == b.X2 && b.Y1 > b.Y2);
            return swap ? new Box(b.X2, b.Y2, b.X1, b.Y1) : b;
        }

        var x1 = Math.Min(b.X1, b.X2);
        var x2 = Math.Max(b.X1, b.X2);
        var y1 = Math.Min(b.Y1, b.Y2);
        var y2 = Math.Max(b.Y1, b.Y2);
        return new Box(x1, y1, x2, y2);
    }
}
```
Apply this exact shape to `UsernameNormalizer`:
```csharp
namespace BlazorCanvas.Auth;

/// <summary>
/// The single source of the trim+lowercase username rule (D-44). Applied once, before every
/// lookup and every INSERT, in exactly one place — never duplicated inline.
/// </summary>
public static class UsernameNormalizer
{
    public static string Normalize(string? username) =>
        (username ?? "").Trim().ToLowerInvariant();
}
```
This mirrors `Normalisation.Normalise`'s "one static method, one clearly-named rule, XML-doc citing
the decision it enforces" convention exactly — the strongest, most directly-applicable analog found
for this phase.

---

## Shared Patterns

### Cookie authentication registration & middleware ordering
**Source:** RESEARCH.md's "Program.cs — the full set of additions" (no in-repo precedent; this is
genuinely new framework wiring)
**Apply to:** `Program.cs` only, but every other file in this phase (`Login.razor`, `Home.razor`, the
`/logout` endpoint) depends on it being correct.
```csharp
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.ExpireTimeSpan = TimeSpan.FromDays(365); // ticket validity only, not cookie Expires
    });
builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();
```
Ordering constraint (Pitfall 4): `app.UseAuthentication(); app.UseAuthorization();` must be inserted
**before** the codebase's existing `app.UseAntiforgery();` call in `Program.cs` line 51.

### Username normalization (D-44)
**Source:** `src/BlazorCanvas/Auth/UsernameNormalizer.cs` (new, modeled on
`src/BlazorCanvas/Geometry/Normalisation.cs`'s single-static-method convention)
**Apply to:** `Login.razor`'s `LoginAsync` handler — every read and every write path must normalize
through this one function, never duplicate `.Trim().ToLowerInvariant()` inline (per RESEARCH.md
Pitfall 2).

### Plaintext password comparison (D-08, locked)
**Source:** `src/BlazorCanvas/Data/User.cs` lines 4-6 (`Password` doc-comment: "stored and compared in
plaintext — locked and deliberate (D-08)")
**Apply to:** `Login.razor`'s `LoginAsync` — direct `user.Password != Input.Password` string
comparison; no hashing library, no `PasswordHasher<T>`, ever.

### Antiforgery on non-`EditForm` posts
**Source:** RESEARCH.md's Pattern 3 (no in-repo precedent — every existing form-adjacent surface in
the scaffold, e.g. `Counter.razor`'s button, is a Blazor interactive `@onclick`, not a plain HTML
form POST)
**Apply to:** the Logout `<form method="post" action="/logout">` inside `Home.razor` (manual
`<AntiforgeryToken />`-equivalent hidden input via `AntiforgeryStateProvider`) and the `/logout`
minimal-API endpoint (manual `IAntiforgery.ValidateRequestAsync(context)` call — NOT automatically
covered by `UseAntiforgery()` middleware for minimal APIs, per Pitfall 4).

## No Analog Found

Files/constructs with no close match in the codebase — planner should use RESEARCH.md's Code
Examples/Patterns sections directly rather than an in-repo file:

| File / Construct | Role | Data Flow | Reason |
|-------------------|------|-----------|--------|
| `POST /logout` minimal-API endpoint (in `Program.cs`) | route | request-response | No minimal-API endpoint exists anywhere in the codebase — `Program.cs` only has `MapStaticAssets()`/`MapRazorComponents<App>()`. Use RESEARCH.md's Pattern 3 code example verbatim. |
| `Login.razor`'s `EditForm`/`[SupplyParameterFromForm]` login handler | component logic | CRUD + request-response | No form/auth component exists in the scaffold (`Counter.razor` uses `@onclick`, not a form). Use RESEARCH.md's Pattern 1 in full (including the unique-violation race handling) as the authoritative source. |
| `[Authorize]` + cascading `AuthenticationState` read in `Home.razor` | component logic | request-response | No `[Authorize]`-protected page or `AuthenticationState` consumer exists yet. Use RESEARCH.md's Pattern 2 verbatim. |

## Metadata

**Analog search scope:** `src/BlazorCanvas/` (excluding `obj/`/`bin/`), specifically `Program.cs`,
`Data/`, `Components/Pages/`, `Components/Layout/`, `Geometry/`, `.csproj`
**Files scanned:** 16 source `.cs`/`.razor`/`.css` files (all non-generated, non-`obj` files in the
project)
**Pattern extraction date:** 2026-07-15
</content>
</invoke>
