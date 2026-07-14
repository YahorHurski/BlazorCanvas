using BlazorCanvas.Components;
using BlazorCanvas.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddDbContext<CanvasDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Canvas")));

var app = builder.Build();

// Apply migrations automatically at startup (D-42). The container may still be starting when
// `dotnet run` fires, so retry on connection failure; let any other exception propagate — a
// migration failure (e.g. a CHECK constraint that cannot be created) must fail loudly at boot,
// never leave the app running with a half-built schema.
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

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
