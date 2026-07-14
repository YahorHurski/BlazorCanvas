using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace BlazorCanvas.Data;

/// <summary>
/// Design-time factory used only by `dotnet ef` tooling (migrations add / script). At design
/// time `CanvasDbContext` is not yet resolvable from the app's DI container — that registration
/// is added at startup in `Program.cs` (Task 3) — so this factory builds the options directly
/// from `appsettings.Development.json`, reading the same `ConnectionStrings:Canvas` key the
/// running app uses. Never used at runtime.
/// </summary>
public class CanvasDbContextFactory : IDesignTimeDbContextFactory<CanvasDbContext>
{
    public CanvasDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build();

        var connectionString = configuration.GetConnectionString("Canvas")
            ?? "Host=localhost;Port=5432;Database=canvas;Username=postgres;Password=postgres";

        var optionsBuilder = new DbContextOptionsBuilder<CanvasDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new CanvasDbContext(optionsBuilder.Options);
    }
}
