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
            .AddEnvironmentVariables()
            .Build();

        // Refuse to guess: a config miss must fail loudly, never fall back to a hardcoded
        // connection string. Port 5432 on this machine is a DIFFERENT PostgreSQL server (the
        // native postgresql-x64-18 service, D-27) and applying migrations to it would corrupt it.
        var connectionString = configuration.GetConnectionString("Canvas")
            ?? throw new InvalidOperationException(
                "ConnectionStrings:Canvas is not configured. Run `dotnet ef` from src/BlazorCanvas/ " +
                "(so appsettings.Development.json is found) or set the ConnectionStrings__Canvas " +
                "environment variable. Refusing to guess a connection string: port 5432 on this " +
                "machine is a DIFFERENT PostgreSQL server and applying migrations to it would corrupt it.");

        var optionsBuilder = new DbContextOptionsBuilder<CanvasDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new CanvasDbContext(optionsBuilder.Options);
    }
}
