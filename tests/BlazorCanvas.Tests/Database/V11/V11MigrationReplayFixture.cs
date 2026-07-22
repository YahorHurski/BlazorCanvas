using System.Security.Cryptography;
using System.Text.RegularExpressions;
using BlazorCanvas.Data.V11;
using BlazorCanvas.Shapes;
using Npgsql;

namespace BlazorCanvas.Tests.Database.V11;

/// <summary>
/// Restores a full database dump only into a run-owned scratch database. A dump sent to the wrong
/// database destroys a developer's data, so every lifecycle operation is guarded by its name.
/// </summary>
public sealed class V11MigrationReplayFixture : IAsyncLifetime
{
    public const string ScratchPrefix = "canvas_v11_replay_";
    private const string FixtureHash = "80FB2335AAE717DA3E6210639A976E796D6F9B9CAD0FD1E715B12ED90C43CE22";
    private const string LegacyDdl = """
        CREATE TABLE public.users (
            id integer PRIMARY KEY,
            username text NOT NULL,
            password text NOT NULL
        );
        CREATE TABLE public.figures (
            id integer NOT NULL,
            user_id integer NOT NULL,
            type text NOT NULL,
            x1 integer NOT NULL,
            y1 integer NOT NULL,
            x2 integer NOT NULL,
            y2 integer NOT NULL,
            CONSTRAINT box_is_a_box CHECK (((type <> ALL (ARRAY['rectangle'::text, 'triangle'::text])) OR ((x2 > x1) AND (y2 > y1)))),
            CONSTRAINT circle_is_a_circle CHECK (((type <> 'circle'::text) OR (((x2 - x1) = (y2 - y1)) AND (x2 > x1) AND (((x2 - x1) % 2) = 0)))),
            CONSTRAINT figures_type_is_known CHECK ((type = ANY (ARRAY['line'::text, 'rectangle'::text, 'circle'::text, 'triangle'::text]))),
            CONSTRAINT line_is_a_line CHECK (((type <> 'line'::text) OR ((x2 >= x1) AND ((x2 > x1) OR (y2 <> y1)))))
        );
        """;

    private readonly string _sourceConnectionString =
        Environment.GetEnvironmentVariable("BLAZORCANVAS_TEST_CONNECTION")
        ?? "Host=localhost;Port=5433;Database=canvas;Username=postgres;Password=postgres";
    private string _adminConnectionString = null!;

    public string ScratchDatabaseName { get; private set; } = null!;

    public string ConnectionString { get; private set; } = null!;

    public NpgsqlDataSource DataSource { get; private set; } = null!;

    public V11MigrationReport Report { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        ScratchDatabaseName = $"{ScratchPrefix}{Guid.NewGuid():N}";
        _adminConnectionString = BuildAdminConnectionString(_sourceConnectionString);
        EnsureSafeScratchName(ScratchDatabaseName, _sourceConnectionString);

        await CreateDatabaseAsync(_adminConnectionString, ScratchDatabaseName);
        ConnectionString = WithDatabase(_sourceConnectionString, ScratchDatabaseName);
        DataSource = NpgsqlDataSource.Create(ConnectionString);

        try
        {
            var fixturePath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "v1.1-pre-rewrite.sql");
            var dump = await File.ReadAllTextAsync(fixturePath);
            var actualHash = Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(fixturePath)));
            if (!string.Equals(FixtureHash, actualHash, StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"Fixture SHA-256 mismatch. Expected {FixtureHash}; actual {actualHash}.");
            }

            // \restrict and \unrestrict are psql meta-commands, not SQL. Driver-only execution
            // avoids a psql/docker dependency and sends every remaining SQL statement verbatim.
            var sql = string.Join('\n', dump.Split('\n').Where(line => !line.TrimStart().StartsWith('\\')));
            await using var connection = await OpenAsync();
            await using var command = new NpgsqlCommand(sql, connection);
            await command.ExecuteNonQueryAsync();

            await AssertRestoreAsync(connection);
            Report = await V11DataMigration.RunAsync(connection, DefaultShapes.CreateRegistry());
        }
        catch
        {
            await DisposeAsync();
            throw;
        }
    }

    public async Task DisposeAsync()
    {
        try
        {
            if (DataSource is not null)
            {
                await DataSource.DisposeAsync();
            }
        }
        finally
        {
            if (!string.IsNullOrWhiteSpace(ScratchDatabaseName))
            {
                // WITH (FORCE) makes cleanup reliable even if a failed assertion left a connection alive.
                await DropDatabaseAsync(_adminConnectionString ?? BuildAdminConnectionString(_sourceConnectionString), ScratchDatabaseName);
            }
        }
    }

    public async Task<NpgsqlConnection> OpenAsync() => await DataSource.OpenConnectionAsync();

    public static async Task<V11MigrationReport> MigrateFreshAsync(
        string adminConnectionString,
        string scratchName,
        Func<NpgsqlConnection, Task> seed)
    {
        await using var database = await CreateFreshAsync(adminConnectionString, scratchName, seed);
        return await V11DataMigration.RunAsync(database.DataSource, DefaultShapes.CreateRegistry());
    }

    /// <summary>
    /// Creates a separately guarded legacy-only database for focused migration cases. Tests dispose
    /// it in a finally block, so no canvas_v11_replay_ database survives a failed assertion.
    /// </summary>
    public static async Task<FreshMigrationDatabase> CreateFreshAsync(
        string adminConnectionString,
        string scratchName,
        Func<NpgsqlConnection, Task> seed)
    {
        ArgumentNullException.ThrowIfNull(seed);
        EnsureSafeScratchName(scratchName, adminConnectionString);
        await CreateDatabaseAsync(adminConnectionString, scratchName);
        var connectionString = WithDatabase(adminConnectionString, scratchName);
        var dataSource = NpgsqlDataSource.Create(connectionString);
        try
        {
            await using var connection = await dataSource.OpenConnectionAsync();
            await using (var ddl = new NpgsqlCommand(LegacyDdl, connection))
            {
                await ddl.ExecuteNonQueryAsync();
            }

            await seed(connection);
            return new FreshMigrationDatabase(adminConnectionString, scratchName, dataSource);
        }
        catch
        {
            await dataSource.DisposeAsync();
            await DropDatabaseAsync(adminConnectionString, scratchName);
            throw;
        }
    }

    private static async Task AssertRestoreAsync(NpgsqlConnection connection)
    {
        const string sql = """
            SELECT (SELECT count(*) FROM public.users), (SELECT count(*) FROM public.figures),
                EXISTS (SELECT 1 FROM public.users WHERE id = 3561),
                (SELECT count(*) FROM public.figures WHERE id BETWEEN 3860 AND 3867)
            """;
        await using var command = new NpgsqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        Assert.Equal(708L, reader.GetInt64(0));
        Assert.Equal(795L, reader.GetInt64(1));
        Assert.True(reader.GetBoolean(2));
        Assert.Equal(8L, reader.GetInt64(3));
    }

    private static string BuildAdminConnectionString(string connectionString)
    {
        var builder = new NpgsqlConnectionStringBuilder(connectionString) { Database = "postgres" };
        if (string.Equals(builder.Database, "canvas", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("The admin connection must never target canvas.");
        }

        return builder.ConnectionString;
    }

    private static string WithDatabase(string connectionString, string databaseName)
    {
        var builder = new NpgsqlConnectionStringBuilder(connectionString) { Database = databaseName };
        return builder.ConnectionString;
    }

    private static void EnsureSafeScratchName(string scratchName, string sourceConnectionString)
    {
        if (!scratchName.StartsWith(ScratchPrefix, StringComparison.Ordinal)
            || !Regex.IsMatch(scratchName, $"^{ScratchPrefix}[0-9a-f]{{32}}$", RegexOptions.CultureInvariant)
            || string.Equals(scratchName, new NpgsqlConnectionStringBuilder(sourceConnectionString).Database, StringComparison.OrdinalIgnoreCase)
            || string.Equals(scratchName, "canvas", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("The scratch database name is unsafe; it must use the guarded canvas_v11_replay_ prefix and never equal canvas.");
        }
    }

    private static async Task CreateDatabaseAsync(string adminConnectionString, string scratchName)
    {
        EnsureSafeScratchName(scratchName, adminConnectionString);
        await using var connection = new NpgsqlConnection(adminConnectionString);
        await connection.OpenAsync();
        // The GUID-derived identifier has passed the fixed-prefix guard, so quoting it cannot inject SQL.
        await using var command = new NpgsqlCommand($"CREATE DATABASE \"{scratchName}\"", connection);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task DropDatabaseAsync(string adminConnectionString, string scratchName)
    {
        EnsureSafeScratchName(scratchName, adminConnectionString);
        await using var connection = new NpgsqlConnection(adminConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{scratchName}\" WITH (FORCE)", connection);
        await command.ExecuteNonQueryAsync();
    }

    public sealed class FreshMigrationDatabase : IAsyncDisposable
    {
        private readonly string _adminConnectionString;

        internal FreshMigrationDatabase(string adminConnectionString, string name, NpgsqlDataSource dataSource)
        {
            _adminConnectionString = adminConnectionString;
            ScratchDatabaseName = name;
            DataSource = dataSource;
        }

        public string ScratchDatabaseName { get; }

        public NpgsqlDataSource DataSource { get; }

        public async Task<NpgsqlConnection> OpenAsync() => await DataSource.OpenConnectionAsync();

        public async ValueTask DisposeAsync()
        {
            try
            {
                await DataSource.DisposeAsync();
            }
            finally
            {
                await DropDatabaseAsync(_adminConnectionString, ScratchDatabaseName);
            }
        }
    }
}
