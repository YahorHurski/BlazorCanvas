using BlazorCanvas.Data;
using BlazorCanvas.Geometry;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace BlazorCanvas.Tests.Database;

/// <summary>
/// Shared xUnit fixture for the "Database" test collection. Connects to the live Compose
/// PostgreSQL container (D-27 — this development machine publishes it on host port 5433
/// instead of the D-27-documented 5432, because a native postgresql-x64-18 Windows service
/// permanently occupies 5432; see 01-03-SUMMARY.md's deviation record), applies pending
/// migrations once on startup, and provides a raw-insert helper that hands PostgreSQL an
/// anchor and a geometry JSON string DIRECTLY, bypassing <see cref="MinSizeGuard"/>,
/// <see cref="Normalisation"/> and <see cref="GeometryCodec"/> entirely — the whole point of
/// this test suite is that PostgreSQL itself is the thing being exercised, not the C# guard.
/// D-59 removed the geometry CHECK constraints this fixture used to help probe; the only
/// constraint remaining on <c>figures</c> is the <c>type</c> whitelist (D-46, D-59 item 9).
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

    public async Task InitializeAsync()
    {
        try
        {
            await using var context = CreateContext();
            await context.Database.MigrateAsync();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                "The BlazorCanvas.Tests.Database suite could not reach PostgreSQL. " +
                "Run 'docker compose up -d' from the repository root and retry. " +
                $"Connection string used: \"{ConnectionString}\". " +
                $"Underlying error: {ex.GetType().Name}: {ex.Message}",
                ex);
        }
    }

    public Task DisposeAsync() => Task.CompletedTask;

    public CanvasDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<CanvasDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;
        return new CanvasDbContext(options);
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

    /// <summary>
    /// Attempts to INSERT one figure with the given RAW type literal, anchor and geometry JSON
    /// string, through a brand-new DbContext inside its own transaction that is always rolled
    /// back — so the suite never accumulates rows regardless of whether the INSERT succeeds.
    /// Values are passed exactly as given, with no re-encoding through
    /// <see cref="GeometryCodec"/> and no pass through <see cref="MinSizeGuard"/> or
    /// <see cref="Normalisation"/> — so a test here can hand PostgreSQL a value the C# path
    /// would never produce, which is the whole reason this helper exists.
    /// </summary>
    public async Task<InsertAttempt> TryInsertRawFigureAsync(
        string typeLiteral, int x, int y, string geometry)
    {
        await using var context = CreateContext();
        await using var transaction = await context.Database.BeginTransactionAsync();

        var userId = await CreateTestUserAsync(context);

        context.Figures.Add(new Figure
        {
            UserId = userId,
            Type = typeLiteral,
            X = x,
            Y = y,
            Geometry = geometry,
            Z = 1m,
        });

        try
        {
            await context.SaveChangesAsync();
            return InsertAttempt.Success();
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException pg
            && pg.SqlState == PostgresErrorCodes.CheckViolation)
        {
            return InsertAttempt.Rejected(pg);
        }
        // The transaction is never committed above, so disposing it here rolls back either way.
    }

    /// <summary>
    /// Convenience overload for the type-whitelist round-trip test in
    /// TypeWhitelistAndPersistenceTests: only the type literal matters there, so the anchor is
    /// a fixed (0,0) and the geometry a fixed well-formed literal.
    /// </summary>
    public Task<InsertAttempt> TryInsertFigureAsync(FigureType type, string geometry) =>
        TryInsertRawFigureAsync(FigureTypeNames.ToDbValue(type), 0, 0, geometry);
}

/// <summary>The outcome of one INSERT attempt against the live database.</summary>
public sealed class InsertAttempt
{
    public bool Succeeded { get; }

    public PostgresException? Error { get; }

    private InsertAttempt(bool succeeded, PostgresException? error)
    {
        Succeeded = succeeded;
        Error = error;
    }

    public static InsertAttempt Success() => new(true, null);

    public static InsertAttempt Rejected(PostgresException error) => new(false, error);
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
