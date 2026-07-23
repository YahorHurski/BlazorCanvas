using BlazorCanvas.Data;
using BlazorCanvas.Data.V11;
using BlazorCanvas.Shapes;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace BlazorCanvas.Tests.Database;

/// <summary>
/// Shared xUnit fixture for the "Database" test collection. Connects to the live Compose
/// PostgreSQL container (D-27 — this development machine publishes it on host port 5433
/// instead of the D-27-documented 5432, because a native postgresql-x64-18 Windows service
/// permanently occupies 5432; see 01-03-SUMMARY.md's deviation record), applies pending
/// migrations once on startup, and provides helpers that INSERT raw values through the
/// DbContext while deliberately bypassing <see cref="MinSizeGuard"/> and
/// <see cref="Normalisation"/> — the whole point of this test suite is that PostgreSQL itself
/// is the thing being exercised, not the C# guard.
///
/// If PostgreSQL is unreachable, <see cref="InitializeAsync"/> throws rather than letting the
/// suite silently skip — a skipped database suite is exactly how "the database refuses illegal
/// rows" (ROADMAP success criterion 3) would become an unverified claim nobody ever checked.
/// </summary>
public class DatabaseFixture : IAsyncLifetime
{
    /// <summary>
    /// Read from BLAZORCANVAS_TEST_CONNECTION if set (so the same tests can target a different
    /// database in CI later), otherwise the literal Compose connection string for this
    /// development machine (port 5433 — see the D-27 deviation note above).
    /// </summary>
    public string ConnectionString { get; } =
        Environment.GetEnvironmentVariable("BLAZORCANVAS_TEST_CONNECTION")
        ?? "Host=localhost;Port=5433;Database=canvas;Username=postgres;Password=postgres";

    /// <summary>
    /// Shared data source for v11's parameterised Npgsql tests and repository tests.
    /// </summary>
    public NpgsqlDataSource DataSource { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        try
        {
            await using var context = CreateContext();
            await context.Database.MigrateAsync();
            DataSource = NpgsqlDataSource.Create(ConnectionString);
            await V11Cutover.EnsureAsync(DataSource, DefaultShapes.CreateRegistry());
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                "The BlazorCanvas.Tests.Database suite could not migrate or apply the v11 schema. " +
                "Run 'docker compose up -d' from the repository root and retry. " +
                $"Connection string used: \"{ConnectionString}\". " +
                $"Underlying error: {ex.GetType().Name}: {ex.Message}",
                ex);
        }
    }

    public async Task DisposeAsync()
    {
        if (DataSource is not null)
        {
            await DataSource.DisposeAsync();
        }
    }

    public CanvasDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<CanvasDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;
        return new CanvasDbContext(options);
    }

    /// <summary>
    /// Opens a v11 connection from the shared data source.
    /// </summary>
    public async Task<NpgsqlConnection> OpenV11ConnectionAsync() =>
        await DataSource.OpenConnectionAsync();

    /// <summary>
    /// Creates a v11 canvas with schema defaults for the supplied existing owner.
    /// </summary>
    public static async Task<Guid> CreateTestCanvasAsync(NpgsqlConnection connection, int ownerId)
    {
        var canvasId = Guid.NewGuid();
        await using var command = new NpgsqlCommand(
            "INSERT INTO public.canvases (id, owner_id) VALUES (@id, @ownerId)", connection);
        command.Parameters.AddWithValue("id", canvasId);
        command.Parameters.AddWithValue("ownerId", ownerId);
        await command.ExecuteNonQueryAsync();
        return canvasId;
    }

    /// <summary>
    /// Creates a throwaway user row and returns its id. Every figure needs a valid user_id
    /// (the FK is NOT NULL) — the username itself is never asserted on, only its existence.
    /// </summary>
    public static async Task<int> CreateTestUserAsync(CanvasDbContext context)
    {
        var user = new User
        {
            Username = $"db-test-{Guid.NewGuid():N}",
            Password = "irrelevant-to-this-suite",
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();
        return user.Id;
    }

}

/// <summary>
/// Ties the "Database" collection name to <see cref="DatabaseFixture"/>, so every test class in
/// this namespace runs sequentially against the same live PostgreSQL container (no two Database
/// tests ever run concurrently against it).
/// </summary>
[CollectionDefinition("Database")]
public class DatabaseCollection : ICollectionFixture<DatabaseFixture>
{
    // Marker class only — carries no code of its own.
}
