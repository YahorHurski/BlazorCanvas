using System.Text;
using BlazorCanvas.Data.V11;
using BlazorCanvas.Data.V11.Transition;
using BlazorCanvas.Shapes;
using Npgsql;

namespace BlazorCanvas.Tests.Database.V11;

/// <summary>Owns a randomly named catalog for cutover tests; it never alters DatabaseFixture's canvas catalog.</summary>
internal sealed class V11CutoverScratchDatabase : IAsyncDisposable
{
    private readonly NpgsqlConnectionStringBuilder connectionString;
    private readonly NpgsqlConnectionStringBuilder maintenanceConnectionString;

    private V11CutoverScratchDatabase(NpgsqlConnectionStringBuilder connectionString)
    {
        this.connectionString = connectionString;
        maintenanceConnectionString = new NpgsqlConnectionStringBuilder(connectionString.ConnectionString) { Database = "postgres", Pooling = false };
        DataSource = NpgsqlDataSource.Create(connectionString.ConnectionString);
    }

    public NpgsqlDataSource DataSource { get; }

    public static async Task<V11CutoverScratchDatabase> CreateAsync(string fixtureConnectionString)
    {
        var databaseName = $"canvas_cutover_{Guid.NewGuid():N}";
        var connectionString = new NpgsqlConnectionStringBuilder(fixtureConnectionString) { Database = databaseName, Pooling = false };
        var database = new V11CutoverScratchDatabase(connectionString);
        await using var maintenance = new NpgsqlConnection(database.maintenanceConnectionString.ConnectionString);
        await maintenance.OpenAsync();
        await using var create = new NpgsqlCommand($"CREATE DATABASE \"{databaseName}\"", maintenance);
        await create.ExecuteNonQueryAsync();
        return database;
    }

    public async Task SetupFreshUsersOnlyAsync()
    {
        await ExecuteAsync("""
            CREATE TABLE public.users (
              id integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
              username text NOT NULL UNIQUE,
              password text NOT NULL);
            INSERT INTO public.users (username, password) VALUES ('cutover-user', 'test');
            """);
    }

    public async Task SetupLegacyOnlyAsync()
    {
        await SetupFreshUsersOnlyAsync();
        await ExecuteAsync("""
            CREATE TABLE public.figures (
              id integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
              user_id integer NOT NULL REFERENCES public.users(id) ON DELETE CASCADE,
              type text NOT NULL, x1 integer NOT NULL, y1 integer NOT NULL,
              x2 integer NOT NULL, y2 integer NOT NULL,
              CONSTRAINT legacy_type_check CHECK (type IN ('rectangle', 'line', 'circle', 'triangle')));
            INSERT INTO public.figures (user_id, type, x1, y1, x2, y2)
            VALUES (1, 'rectangle', 10, 20, 60, 80), (1, 'line', 100, 140, 170, 110);
            """);
    }

    public async Task SetupAdditiveAsync()
    {
        await SetupLegacyOnlyAsync();
        await using var connection = await DataSource.OpenConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await V11Schema.ApplyAsync(connection, transaction);
        await V11Schema.SeedFigureTypesAsync(connection, transaction, DefaultShapes.CreateRegistry());
        await V11DataMigration.RunAsync(connection, transaction, DefaultShapes.CreateRegistry());
        await transaction.CommitAsync();
    }

    public async Task SetupCompletedPublicAsync()
    {
        await SetupFreshUsersOnlyAsync();
        await V11Cutover.EnsureAsync(DataSource, DefaultShapes.CreateRegistry());
    }

    public async Task SetupInvalidAsync()
    {
        await SetupFreshUsersOnlyAsync();
        await ExecuteAsync("CREATE SCHEMA v11; CREATE TABLE v11.canvases (id uuid PRIMARY KEY);");
    }

    /// <summary>Stable whole-application catalog/data representation suitable for rollback equality checks.</summary>
    public async Task<string> SnapshotAsync()
    {
        await using var connection = await DataSource.OpenConnectionAsync();
        const string tablesSql = """
            SELECT table_schema, table_name
            FROM information_schema.tables
            WHERE table_type = 'BASE TABLE' AND table_schema IN ('public', 'v11')
            ORDER BY table_schema, table_name
            """;
        var tables = new List<(string Schema, string Table)>();
        await using (var tablesCommand = new NpgsqlCommand(tablesSql, connection))
        await using (var reader = await tablesCommand.ExecuteReaderAsync())
            while (await reader.ReadAsync()) tables.Add((reader.GetString(0), reader.GetString(1)));

        var result = new StringBuilder();
        foreach (var (schema, table) in tables)
        {
            result.AppendLine($"TABLE {schema}.{table}");
            await AppendRowsAsync(result, connection, """
                SELECT column_name, data_type, udt_name, is_nullable, column_default
                FROM information_schema.columns WHERE table_schema = @schema AND table_name = @table
                ORDER BY ordinal_position
                """, schema, table);
            await AppendRowsAsync(result, connection, """
                SELECT pg_get_constraintdef(c.oid, true)
                FROM pg_constraint c JOIN pg_class r ON r.oid = c.conrelid JOIN pg_namespace n ON n.oid = r.relnamespace
                WHERE n.nspname = @schema AND r.relname = @table ORDER BY c.conname
                """, schema, table);
            await AppendRowsAsync(result, connection, """
                SELECT indexdef FROM pg_indexes WHERE schemaname = @schema AND tablename = @table ORDER BY indexname
                """, schema, table);
            await using var dataCommand = new NpgsqlCommand($"SELECT row_to_json(t)::text FROM (SELECT * FROM \"{schema}\".\"{table}\") t ORDER BY row_to_json(t)::text", connection);
            await using var data = await dataCommand.ExecuteReaderAsync();
            while (await data.ReadAsync()) result.AppendLine(data.GetString(0));
        }
        return result.ToString();
    }

    public async ValueTask DisposeAsync()
    {
        await DataSource.DisposeAsync();
        await using var maintenance = new NpgsqlConnection(maintenanceConnectionString.ConnectionString);
        await maintenance.OpenAsync();
        await using (var terminate = new NpgsqlCommand("SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = @database AND pid <> pg_backend_pid()", maintenance))
        {
            terminate.Parameters.AddWithValue("database", connectionString.Database!);
            await terminate.ExecuteNonQueryAsync();
        }
        await using var drop = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{connectionString.Database}\"", maintenance);
        await drop.ExecuteNonQueryAsync();
    }

    private async Task ExecuteAsync(string sql)
    {
        await using var connection = await DataSource.OpenConnectionAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task AppendRowsAsync(StringBuilder result, NpgsqlConnection connection, string sql, string schema, string table)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("schema", schema);
        command.Parameters.AddWithValue("table", table);
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            for (var column = 0; column < reader.FieldCount; column++) result.Append(reader.IsDBNull(column) ? "<null>" : reader.GetValue(column)).Append('\t');
            result.AppendLine();
        }
    }
}
