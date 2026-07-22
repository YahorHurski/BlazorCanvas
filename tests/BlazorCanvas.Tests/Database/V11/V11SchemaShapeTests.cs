using BlazorCanvas.Data.V11;
using BlazorCanvas.Geometry;
using BlazorCanvas.Shapes;
using BlazorCanvas.Tests.Shapes;
using Npgsql;
using NpgsqlTypes;

namespace BlazorCanvas.Tests.Database.V11;

/// <summary>
/// Proves the live v11 database objects from PostgreSQL's catalogs rather than from a C# model.
/// </summary>
[Collection("Database")]
public class V11SchemaShapeTests
{
    private readonly DatabaseFixture _fixture;

    public V11SchemaShapeTests(DatabaseFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task V11Schema_ContainsExactlyTheThreeModelTables()
    {
        await using var connection = await _fixture.OpenV11ConnectionAsync();
        var tables = await StringsAsync(connection, """
            SELECT table_name FROM information_schema.tables
            WHERE table_schema = 'v11' AND table_type = 'BASE TABLE' ORDER BY table_name
            """);

        Assert.Equal(new[] { "canvases", "figure_types", "figures" }, tables);
    }

    [Fact]
    public async Task V11Schema_AndPublicUsersExist()
    {
        await using var connection = await _fixture.OpenV11ConnectionAsync();
        Assert.Equal(1L, await CountAsync(connection, "SELECT count(*) FROM information_schema.schemata WHERE schema_name = 'v11'"));
        Assert.Equal(1L, await CountAsync(connection, "SELECT count(*) FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'users'"));
    }

    [Fact]
    public async Task Figures_HasExactlyTheRequiredColumnsInOrder()
    {
        await using var connection = await _fixture.OpenV11ConnectionAsync();
        var columns = await StringsAsync(connection, """
            SELECT column_name FROM information_schema.columns
            WHERE table_schema = 'v11' AND table_name = 'figures' ORDER BY ordinal_position
            """);

        Assert.Equal(new[]
        {
            "id", "canvas_id", "type", "x", "y", "rotation", "geometry", "style", "z",
            "bbox_x", "bbox_y", "bbox_w", "bbox_h", "created_at",
        }, columns);
    }

    [Theory]
    [InlineData("id", "uuid", null, null)]
    [InlineData("canvas_id", "uuid", null, null)]
    [InlineData("x", "numeric", 12, 3)]
    [InlineData("y", "numeric", 12, 3)]
    [InlineData("rotation", "numeric", 7, 3)]
    [InlineData("geometry", "jsonb", null, null)]
    [InlineData("style", "jsonb", null, null)]
    [InlineData("z", "numeric", null, null)]
    [InlineData("bbox_x", "double precision", null, null)]
    [InlineData("bbox_y", "double precision", null, null)]
    [InlineData("bbox_w", "double precision", null, null)]
    [InlineData("bbox_h", "double precision", null, null)]
    [InlineData("created_at", "timestamp with time zone", null, null)]
    public async Task Figures_ColumnCatalogTypesAreExact(string column, string type, int? precision, int? scale)
    {
        await using var connection = await _fixture.OpenV11ConnectionAsync();
        await using var command = new NpgsqlCommand("""
            SELECT data_type, numeric_precision, numeric_scale, is_nullable
            FROM information_schema.columns
            WHERE table_schema = 'v11' AND table_name = 'figures' AND column_name = @column
            """, connection);
        command.Parameters.AddWithValue("column", column);
        await using var reader = await command.ExecuteReaderAsync();

        Assert.True(await reader.ReadAsync());
        Assert.Equal(type, reader.GetString(0));
        if (column == "z")
        {
            Assert.True(reader.IsDBNull(1));
        }
        else if (type == "numeric")
        {
            Assert.Equal(precision, reader.IsDBNull(1) ? null : reader.GetInt32(1));
            Assert.Equal(scale, reader.IsDBNull(2) ? null : reader.GetInt32(2));
        }
        Assert.Equal("NO", reader.GetString(3));
    }

    [Fact]
    public async Task Canvases_HasDefaultsAndNoDeferredColumn()
    {
        await using var connection = await _fixture.OpenV11ConnectionAsync();
        var columns = await StringsAsync(connection, """
            SELECT column_name FROM information_schema.columns
            WHERE table_schema = 'v11' AND table_name = 'canvases' ORDER BY ordinal_position
            """);
        Assert.Equal(new[] { "id", "owner_id", "name", "width", "height", "background", "created_at" }, columns);

        Assert.Contains("1472", await ScalarStringAsync(connection, "SELECT column_default FROM information_schema.columns WHERE table_schema='v11' AND table_name='canvases' AND column_name='width'"));
        Assert.Contains("828", await ScalarStringAsync(connection, "SELECT column_default FROM information_schema.columns WHERE table_schema='v11' AND table_name='canvases' AND column_name='height'"));
        Assert.Contains("Canvas", await ScalarStringAsync(connection, "SELECT column_default FROM information_schema.columns WHERE table_schema='v11' AND table_name='canvases' AND column_name='name'"));
        Assert.Contains("#FFFFFF", await ScalarStringAsync(connection, "SELECT column_default FROM information_schema.columns WHERE table_schema='v11' AND table_name='canvases' AND column_name='background'"));
        Assert.DoesNotContain("updated_at", columns);
    }

    [Fact]
    public async Task FigureTypes_IsAOneColumnPrimaryKeyTable()
    {
        await using var connection = await _fixture.OpenV11ConnectionAsync();
        Assert.Equal(new[] { "name" }, await StringsAsync(connection, "SELECT column_name FROM information_schema.columns WHERE table_schema='v11' AND table_name='figure_types' ORDER BY ordinal_position"));
        Assert.Equal("figure_types_pkey", await ScalarStringAsync(connection, "SELECT conname FROM pg_constraint WHERE conrelid='v11.figure_types'::regclass AND contype='p'"));
    }

    [Fact]
    public async Task Figures_HasNamedConstraintsAndForeignKeys()
    {
        await using var connection = await _fixture.OpenV11ConnectionAsync();
        var constraints = await StringsAsync(connection, "SELECT conname FROM pg_constraint WHERE conrelid='v11.figures'::regclass ORDER BY conname");
        Assert.Contains("z_unique_per_canvas", constraints);
        Assert.Contains("style_is_object", constraints);
        Assert.Contains("geometry_is_object", constraints);
        Assert.Contains("bbox_is_positive", constraints);

        var foreignKeys = await StringsAsync(connection, "SELECT confrelid::regclass::text FROM pg_constraint WHERE conrelid='v11.figures'::regclass AND contype='f' ORDER BY confrelid::regclass::text");
        Assert.Equal(new[] { "v11.canvases", "v11.figure_types" }, foreignKeys);
        Assert.Equal(1L, await CountAsync(connection, "SELECT count(*) FROM pg_constraint WHERE conrelid='v11.figures'::regclass AND contype='f' AND confdeltype='c'"));
    }

    [Fact]
    public async Task V11Indexes_ArePresentAndCanvasOwnerIsNotUnique()
    {
        await using var connection = await _fixture.OpenV11ConnectionAsync();
        Assert.Contains("CREATE INDEX ix_figures_canvas_z ON v11.figures USING btree (canvas_id, z)", await ScalarStringAsync(connection, "SELECT indexdef FROM pg_indexes WHERE schemaname='v11' AND tablename='figures' AND indexname='ix_figures_canvas_z'"));
        Assert.DoesNotContain("UNIQUE", await ScalarStringAsync(connection, "SELECT indexdef FROM pg_indexes WHERE schemaname='v11' AND tablename='canvases' AND indexname='ix_canvases_owner'"));
    }

    [Fact]
    public async Task Figures_TypeHasNoEnumeratingCheckConstraint()
    {
        await using var connection = await _fixture.OpenV11ConnectionAsync();
        var checks = await StringsAsync(connection, "SELECT pg_get_constraintdef(oid) FROM pg_constraint WHERE conrelid='v11.figures'::regclass AND contype='c'");
        foreach (var check in checks)
        {
            Assert.DoesNotContain("line", check, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("rectangle", check, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("circle", check, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("triangle", check, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task NewFigureType_RoundTripsWithOnlyInsertsAndSelect()
    {
        await using var connection = await _fixture.OpenV11ConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        var ownerId = await CreateUserAsync(connection, transaction);
        var canvasId = await CreateCanvasAsync(connection, transaction, ownerId);
        var pentagon = new PentagonShape();
        var placement = pentagon.FromGesture(new CanvasPoint(0, 0), new CanvasPoint(100, 100));

        // This uses exactly two INSERT statements and one SELECT after seeding: no DDL is executed.
        await ExecuteAsync(connection, transaction, "INSERT INTO v11.figure_types (name) VALUES ('pentagon')");
        await InsertFigureAsync(connection, transaction, canvasId, "pentagon", 0m, pentagon.ToJson(placement.Geometry), 0m);
        Assert.Equal("pentagon", await ScalarStringAsync(connection, "SELECT type FROM v11.figures WHERE canvas_id=@canvas", transaction, ("canvas", canvasId)));
    }

    [Fact]
    public async Task ApplyAndSeed_IsIdempotentAndConcurrent()
    {
        var registry = DefaultShapes.CreateRegistry();
        await V11Schema.ApplyAndSeedAsync(_fixture.ConnectionString, registry);
        await using var first = new NpgsqlConnection(_fixture.ConnectionString);
        await using var second = new NpgsqlConnection(_fixture.ConnectionString);
        await Task.WhenAll(first.OpenAsync(), second.OpenAsync());
        await Task.WhenAll(V11Schema.SeedFigureTypesAsync(first, registry), V11Schema.SeedFigureTypesAsync(second, registry));
        Assert.Equal(4L, await CountAsync(first, "SELECT count(*) FROM v11.figure_types"));
    }

    [Fact]
    public async Task TypeEquality_IsByteExact()
    {
        await AssertFigureInsertSqlStateAsync("circle", PostgresErrorCodes.ForeignKeyViolation, shouldSucceed: true);
        await AssertFigureInsertSqlStateAsync("Circle", PostgresErrorCodes.ForeignKeyViolation, shouldSucceed: false);
        await AssertFigureInsertSqlStateAsync("circle ", PostgresErrorCodes.ForeignKeyViolation, shouldSucceed: false);
    }

    [Fact]
    public async Task NumericBoundary_AcceptsMaximumAndRejectsOverflow()
    {
        await using (var connection = await _fixture.OpenV11ConnectionAsync())
        await using (var transaction = await connection.BeginTransactionAsync())
        {
            var ownerId = await CreateUserAsync(connection, transaction);
            var canvasId = await CreateCanvasAsync(connection, transaction, ownerId);
            await InsertFigureAsync(connection, transaction, canvasId, "circle", 999999999.999m, "{}", 0m);
            Assert.Equal(999999999.999m, await ScalarDecimalAsync(connection, "SELECT x FROM v11.figures WHERE canvas_id=@canvas", transaction, ("canvas", canvasId)));
        }

        await using var overflowConnection = await _fixture.OpenV11ConnectionAsync();
        await using var overflowTransaction = await overflowConnection.BeginTransactionAsync();
        var overflowOwnerId = await CreateUserAsync(overflowConnection, overflowTransaction);
        var overflowCanvasId = await CreateCanvasAsync(overflowConnection, overflowTransaction, overflowOwnerId);
        var error = await Assert.ThrowsAsync<PostgresException>(() => InsertFigureAsync(overflowConnection, overflowTransaction, overflowCanvasId, "circle", 1000000000m, "{}", 0m));
        Assert.Equal("22003", error.SqlState);
    }

    [Fact]
    public async Task NumericScale_RoundsAsExactDecimal()
    {
        await using var connection = await _fixture.OpenV11ConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        var ownerId = await CreateUserAsync(connection, transaction);
        var canvasId = await CreateCanvasAsync(connection, transaction, ownerId);
        await InsertFigureAsync(connection, transaction, canvasId, "circle", 0.0005m, "{}", 0m);
        await InsertFigureAsync(connection, transaction, canvasId, "circle", 0.0004m, "{}", 1m);
        var values = await DecimalsAsync(connection, "SELECT x FROM v11.figures WHERE canvas_id=@canvas ORDER BY z", transaction, ("canvas", canvasId));
        Assert.Equal(new[] { 0.001m, 0.000m }, values);
    }

    [Fact]
    public async Task EmptyCanvas_IsAReadableFirstClassState()
    {
        await using var connection = await _fixture.OpenV11ConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        var ownerId = await CreateUserAsync(connection, transaction);
        var canvasId = await CreateCanvasAsync(connection, transaction, ownerId);
        Assert.Equal(canvasId, await ScalarGuidAsync(connection, "SELECT id FROM v11.canvases WHERE id=@canvas", transaction, ("canvas", canvasId)));
        Assert.Equal(0L, await CountAsync(connection, "SELECT count(*) FROM v11.figures WHERE canvas_id=@canvas", transaction, ("canvas", canvasId)));
    }

    [Fact]
    public async Task UnqualifiedFigures_StillResolvesToPublic()
    {
        await using var connection = await _fixture.OpenV11ConnectionAsync();
        Assert.Equal("figures", await ScalarStringAsync(connection, "SELECT 'figures'::regclass::text"));
        Assert.Equal("public", await ScalarStringAsync(connection, """
            SELECT n.nspname FROM pg_class c JOIN pg_namespace n ON n.oid = c.relnamespace
            WHERE c.oid = 'figures'::regclass
            """));
    }

    [Fact]
    public async Task LegacyPublicModel_RemainsUntouched()
    {
        await using var connection = await _fixture.OpenV11ConnectionAsync();
        Assert.Equal(7L, await CountAsync(connection, "SELECT count(*) FROM information_schema.columns WHERE table_schema='public' AND table_name='figures'"));
        Assert.Equal(4L, await CountAsync(connection, "SELECT count(*) FROM pg_constraint WHERE conrelid='public.figures'::regclass AND contype='c'"));
        Assert.Equal(3L, await CountAsync(connection, "SELECT count(*) FROM information_schema.columns WHERE table_schema='public' AND table_name='users'"));
        Assert.Equal("20260714212457_InitialSchema", await ScalarStringAsync(connection, "SELECT \"MigrationId\" FROM public.\"__EFMigrationsHistory\""));
    }

    private static async Task<int> CreateUserAsync(NpgsqlConnection connection, NpgsqlTransaction transaction)
    {
        await using var command = new NpgsqlCommand("INSERT INTO public.users (username, password) VALUES (@username, 'test') RETURNING id", connection, transaction);
        command.Parameters.AddWithValue("username", $"v11-test-{Guid.NewGuid():N}");
        return (int)(await command.ExecuteScalarAsync())!;
    }

    private static async Task<Guid> CreateCanvasAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, int ownerId)
    {
        var id = Guid.NewGuid();
        await using var command = new NpgsqlCommand("INSERT INTO v11.canvases (id, owner_id) VALUES (@id, @ownerId)", connection, transaction);
        command.Parameters.AddWithValue("id", id);
        command.Parameters.AddWithValue("ownerId", ownerId);
        await command.ExecuteNonQueryAsync();
        return id;
    }

    private static async Task InsertFigureAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, Guid canvasId, string type, decimal x, string geometry, decimal z)
    {
        await using var command = new NpgsqlCommand("""
            INSERT INTO v11.figures (id, canvas_id, type, x, y, geometry, z, bbox_x, bbox_y, bbox_w, bbox_h)
            VALUES (@id, @canvas, @type, @x, 0, @geometry, @z, 0, 0, 0, 0)
            """, connection, transaction);
        command.Parameters.AddWithValue("id", Guid.NewGuid());
        command.Parameters.AddWithValue("canvas", canvasId);
        command.Parameters.AddWithValue("type", type);
        command.Parameters.AddWithValue("x", NpgsqlDbType.Numeric, x);
        command.Parameters.AddWithValue("geometry", NpgsqlDbType.Jsonb, geometry);
        command.Parameters.AddWithValue("z", NpgsqlDbType.Numeric, z);
        await command.ExecuteNonQueryAsync();
    }

    private async Task AssertFigureInsertSqlStateAsync(string type, string expectedSqlState, bool shouldSucceed)
    {
        await using var connection = await _fixture.OpenV11ConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        var ownerId = await CreateUserAsync(connection, transaction);
        var canvasId = await CreateCanvasAsync(connection, transaction, ownerId);
        if (shouldSucceed)
        {
            await InsertFigureAsync(connection, transaction, canvasId, type, 0m, "{}", 0m);
            return;
        }

        var error = await Assert.ThrowsAsync<PostgresException>(() => InsertFigureAsync(connection, transaction, canvasId, type, 0m, "{}", 0m));
        Assert.Equal(expectedSqlState, error.SqlState);
    }

    private static async Task ExecuteAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, string sql)
    {
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<List<string>> StringsAsync(NpgsqlConnection connection, string sql)
    {
        var result = new List<string>();
        await using var command = new NpgsqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync()) result.Add(reader.GetString(0));
        return result;
    }

    private static async Task<long> CountAsync(NpgsqlConnection connection, string sql, NpgsqlTransaction? transaction = null, params (string Name, object Value)[] parameters) =>
        Convert.ToInt64(await ScalarAsync(connection, sql, transaction, parameters));

    private static async Task<string> ScalarStringAsync(NpgsqlConnection connection, string sql, NpgsqlTransaction? transaction = null, params (string Name, object Value)[] parameters) =>
        (string)(await ScalarAsync(connection, sql, transaction, parameters))!;

    private static async Task<decimal> ScalarDecimalAsync(NpgsqlConnection connection, string sql, NpgsqlTransaction transaction, params (string Name, object Value)[] parameters) =>
        (decimal)(await ScalarAsync(connection, sql, transaction, parameters))!;

    private static async Task<Guid> ScalarGuidAsync(NpgsqlConnection connection, string sql, NpgsqlTransaction transaction, params (string Name, object Value)[] parameters) =>
        (Guid)(await ScalarAsync(connection, sql, transaction, parameters))!;

    private static async Task<object?> ScalarAsync(NpgsqlConnection connection, string sql, NpgsqlTransaction? transaction, params (string Name, object Value)[] parameters)
    {
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        foreach (var (name, value) in parameters) command.Parameters.AddWithValue(name, value);
        return await command.ExecuteScalarAsync();
    }

    private static async Task<List<decimal>> DecimalsAsync(NpgsqlConnection connection, string sql, NpgsqlTransaction transaction, params (string Name, object Value)[] parameters)
    {
        var result = new List<decimal>();
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        foreach (var (name, value) in parameters) command.Parameters.AddWithValue(name, value);
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync()) result.Add(reader.GetDecimal(0));
        return result;
    }
}
