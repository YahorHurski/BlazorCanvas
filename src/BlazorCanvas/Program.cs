using BlazorCanvas.Components;
using BlazorCanvas.Data;
using BlazorCanvas.Data.V11;
using BlazorCanvas.Shapes;
using BlazorCanvas.Sync;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// IDbContextFactory, not a scoped context (T-03-03): the InteractiveServer circuit's DI scope
// lives for the whole circuit (hours, not milliseconds), so a scoped CanvasDbContext would
// accumulate tracked entities for the tab's lifetime and throw "A second operation was started
// on this context instance" the first time two awaited handlers overlap. Every query and write
// now creates and disposes its own short-lived context via this factory. Only the factory is
// registered - registering CanvasDbContext as well would produce a captive-dependency failure
// (a scoped DbContextOptions<CanvasDbContext> alongside the factory's singleton one).
//
// The retry policy is D-52's save-failure boundary: the provider decides what is transient via
// provider metadata, so the app has no hand-written exception classifier. A zero-row
// UPDATE never reaches this strategy because it is not an exception (D-10). Configuring the shared
// factory also makes LoadAsync retry transient failures, which is an accepted D-45 resilience side
// effect rather than a separate read-path feature.
builder.Services.AddDbContextFactory<CanvasDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("Canvas"),
        npgsql => npgsql.EnableRetryOnFailure(
            maxRetryCount: 2,
            maxRetryDelay: TimeSpan.FromMilliseconds(200),
            errorCodesToAdd: null)));

builder.Services.AddScoped<FigureStore>();
// Phase 11 keeps the v1.11 persistence graph independent from the legacy EF FigureStore until
// cutover. NpgsqlDataSource is a singleton pooled transport with no per-user state; ownership is
// enforced by CanvasRepository's owner-derived canvas id and FigureRepository canvas predicates.
builder.Services.AddSingleton<NpgsqlDataSource>(_ => NpgsqlDataSource.Create(
    builder.Configuration.GetConnectionString("Canvas")
    ?? throw new InvalidOperationException("Connection string 'Canvas' is required.")));
builder.Services.AddSingleton(DefaultShapes.CreateRegistry());
builder.Services.AddScoped<FigureInputGateway>();
builder.Services.AddScoped<FigureRepository>();
builder.Services.AddScoped<CanvasRepository>();
// D-11's cross-tab bridge is this Singleton lifetime. Every Blazor Server tab is its own circuit
// and DI scope, so a Scoped notifier would give each tab a private bucket and sync would silently
// never cross tabs. This deliberately differs from Microsoft's single-circuit scoped notifier
// example. The service is safe process-wide because it has no constructor dependencies and stores
// no per-user state outside its user_id-keyed subscriber buckets.
builder.Services.AddSingleton<CanvasSyncNotifier>();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        // Session cookie (D-26): IsPersistent stays false at sign-in time (02-03), so no
        // Expires/Max-Age is ever written to the cookie - it dies with the browser. ExpireTimeSpan
        // below only bounds the encrypted ticket's server-side validity; it is not what makes the
        // cookie a "session" cookie.
        options.ExpireTimeSpan = TimeSpan.FromDays(365);
    });
builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();

var app = builder.Build();

// Apply migrations automatically at startup (D-42). The container may still be starting when
// `dotnet run` fires, so retry on connection failure; let any other exception propagate — a
// migration failure (e.g. a CHECK constraint that cannot be created) must fail loudly at boot,
// never leave the app running with a half-built schema.
{
    const int maxAttempts = 10;
    var delay = TimeSpan.FromSeconds(2);

    var dbContextFactory = app.Services.GetRequiredService<IDbContextFactory<CanvasDbContext>>();

    for (var attempt = 1; attempt <= maxAttempts; attempt++)
    {
        try
        {
            await using var db = dbContextFactory.CreateDbContext();
            db.Database.Migrate();
            break;
        }
        catch (NpgsqlException) when (attempt < maxAttempts)
        {
            await Task.Delay(delay);
        }
    }
}

// This runs after the EF users migration, but before any component route can create an interactive
// circuit. It remains additive: public.figures is retained until 11-03's explicit cutover.
await V11RuntimeBootstrap.EnsureAsync(
    app.Services.GetRequiredService<NpgsqlDataSource>(),
    app.Services.GetRequiredService<ShapeRegistry>());

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapPost("/logout", async (HttpContext context, IAntiforgery antiforgery) =>
{
    // Minimal-API endpoints are NOT automatically antiforgery-protected the way Razor
    // Components/EditForm endpoints are (RESEARCH Pitfall 4 / A2) - validate explicitly, before
    // signing out.
    await antiforgery.ValidateRequestAsync(context);
    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    // Results.LocalRedirect - local path only, never a caller-supplied target - so this endpoint
    // can never be turned into an open redirect.
    return Results.LocalRedirect("/login");
});

app.Run();
