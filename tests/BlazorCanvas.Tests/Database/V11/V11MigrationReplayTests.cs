using System.Text.Json;
using BlazorCanvas.Data.V11;
using BlazorCanvas.Shapes;
using Npgsql;

namespace BlazorCanvas.Tests.Database.V11;

[Collection("Database")]
public sealed class V11MigrationReplayTests(V11MigrationReplayFixture fixture) : IClassFixture<V11MigrationReplayFixture>
{
    private readonly ShapeRegistry _registry = DefaultShapes.CreateRegistry();

    [Fact]
    public void Report_AccountsForEveryLegacyRow()
    {
        Assert.Equal(708, fixture.Report.UsersSeen);
        Assert.Equal(708, fixture.Report.CanvasesCreated);
        Assert.Equal(795, fixture.Report.FiguresSeen);
        Assert.Equal(795, fixture.Report.FiguresInserted);
        Assert.Equal(0, fixture.Report.FiguresAlreadyPresent);
    }

    [Fact]
    public async Task Counts_AreOneCanvasPerUserAndEveryFigure()
    {
        Assert.Equal(708L, await ScalarAsync<long>("SELECT count(*) FROM v11.canvases"));
        Assert.Equal(795L, await ScalarAsync<long>("SELECT count(*) FROM v11.figures"));
    }

    [Fact]
    public async Task Canvases_HaveDistinctOwners()
    {
        Assert.Equal(708L, await ScalarAsync<long>("SELECT count(DISTINCT owner_id) FROM v11.canvases"));
    }

    [Fact]
    public async Task Canvases_UseTheRequiredDimensions()
    {
        Assert.Equal(0L, await ScalarAsync<long>("SELECT count(*) FROM v11.canvases WHERE width <> 1472 OR height <> 828"));
    }

    [Fact]
    public async Task Canvases_UseTheRequiredNameAndBackground()
    {
        Assert.Equal(0L, await ScalarAsync<long>("SELECT count(*) FROM v11.canvases WHERE name <> 'Canvas' OR background <> '#FFFFFF'"));
    }

    [Fact]
    public async Task FigurelessUsers_StillReceiveTheir173Canvases()
    {
        Assert.Equal(535L, await ScalarAsync<long>("SELECT count(DISTINCT canvas_id) FROM v11.figures"));
        // This is correct, not a defect: a user who never drew still receives a canvas.
        Assert.Equal(173L, await ScalarAsync<long>("""
            SELECT count(*) FROM v11.canvases c
            LEFT JOIN v11.figures f ON f.canvas_id = c.id
            WHERE f.id IS NULL
            """));
    }

    [Theory]
    [MemberData(nameof(TabulatedRows))]
    public async Task TabulatedRows_PreserveLiteralRenderedVertices(TabulatedRow expected)
    {
        var id = V11DeterministicId.ForFigure(expected.OldId);
        var row = await ReadFigureAsync(id);
        Assert.Equal(expected.X, row.X);
        Assert.Equal(expected.Y, row.Y);
        await V11JsonAssertions.AssertJsonbEqualAsync(fixture.DataSource, id, "geometry", expected.GeometryJson);
        Assert.Equal((decimal)expected.OldId, row.Z);
        Assert.Equal(0m, row.Rotation);

        var definition = _registry.Get(row.Type);
        Assert.True(definition.TryParseGeometry(JsonDocument.Parse(row.GeometryJson).RootElement, out var geometry));
        var bounds = definition.BoundsOf(geometry);
        Assert.Equal(bounds.X, row.BboxX);
        Assert.Equal(bounds.Y, row.BboxY);
        Assert.Equal(bounds.W, row.BboxW);
        Assert.Equal(bounds.H, row.BboxH);

        // These values are literal transcriptions from the manifest and old renderer, never a
        // re-computation of the migration formula: otherwise this test could merely agree with itself.
        var vertices = AbsoluteVertices(row.X, row.Y, geometry);
        Assert.Equal(expected.Vertices, vertices);
        if (expected.CircleRadius is double radius)
        {
            Assert.Equal(radius, Assert.IsType<CircleGeometry>(geometry).R);
        }
    }

    [Fact]
    public async Task EveryLegacyFigure_MatchesAnIndependentConversion()
    {
        await using var connection = await fixture.OpenAsync();
        await using var command = new NpgsqlCommand("SELECT id, user_id, type, x1, y1, x2, y2 FROM public.figures ORDER BY id", connection);
        await using var reader = await command.ExecuteReaderAsync();
        var legacyRows = new List<LegacyFigureRow>();
        while (await reader.ReadAsync())
        {
            legacyRows.Add(new LegacyFigureRow(reader.GetInt32(0), reader.GetInt32(1), reader.GetString(2), reader.GetInt32(3), reader.GetInt32(4), reader.GetInt32(5), reader.GetInt32(6)));
        }

        var count = 0;
        foreach (var legacy in legacyRows)
        {
            var converted = LegacyFigureConversion.Convert(legacy);
            var migrated = await ReadFigureAsync(V11DeterministicId.ForFigure(legacy.Id));
            Assert.Equal(V11DeterministicId.ForFigure(legacy.Id), migrated.Id);
            Assert.Equal(V11DeterministicId.ForCanvas(legacy.UserId), migrated.CanvasId);
            Assert.Equal(converted.X, migrated.X);
            Assert.Equal(converted.Y, migrated.Y);
            Assert.Equal((decimal)legacy.Id, migrated.Z);
            await V11JsonAssertions.AssertJsonbEqualAsync(
                fixture.DataSource,
                migrated.Id,
                "geometry",
                _registry.Get(legacy.Type).ToJson(converted.Geometry));
            count++;
        }

        Assert.Equal(795, count); // Non-vacuity guard: a query returning nothing cannot pass silently.
    }

    [Fact]
    public async Task EveryCanvas_PreservesTheOldIdStackingOrder()
    {
        await using var connection = await fixture.OpenAsync();
        await using var users = new NpgsqlCommand("SELECT DISTINCT user_id FROM public.figures ORDER BY user_id", connection);
        await using var reader = await users.ExecuteReaderAsync();
        var userIds = new List<int>();
        while (await reader.ReadAsync()) userIds.Add(reader.GetInt32(0));
        await reader.DisposeAsync();

        foreach (var userId in userIds)
        {
            var oldIds = await ReadIntListAsync(connection, "SELECT id FROM public.figures WHERE user_id = @id ORDER BY id", userId);
            var actual = await ReadGuidListAsync(connection, "SELECT id FROM v11.figures WHERE canvas_id = @id ORDER BY z", V11DeterministicId.ForCanvas(userId));
            Assert.Equal(oldIds.Select(V11DeterministicId.ForFigure), actual);
        }
    }

    [Fact]
    public async Task EveryCanvas_HasATotalZOrder()
    {
        Assert.Equal(0L, await ScalarAsync<long>("""
            SELECT count(*) FROM (
                SELECT canvas_id FROM v11.figures GROUP BY canvas_id HAVING count(DISTINCT z) <> count(*)
            ) collisions
            """));
    }

    [Fact]
    public async Task OverlappingFixtureRows_ReturnInTheirOriginalSequence()
    {
        var expected = new[] { 3864, 3865, 3866, 3867 }.Select(V11DeterministicId.ForFigure);
        var actual = await ReadGuidListAsync(
            await fixture.OpenAsync(),
            "SELECT id FROM v11.figures WHERE canvas_id = @id AND z BETWEEN 3864 AND 3867 ORDER BY z",
            V11DeterministicId.ForCanvas(3561));
        Assert.Equal(expected, actual);
    }

    [Fact]
    public async Task EveryFigure_UsesTheFixedStyleComparedAsJsonb()
    {
        // jsonb, not text: PostgreSQL changes object-key order, which is unrelated to the data.
        Assert.Equal(0L, await ScalarAsync<long>("""
            SELECT count(*) FROM v11.figures
            WHERE style <> '{"stroke":"#000000","stroke_width":2,"fill":"#FFFFFF","opacity":1}'::jsonb
            """));
        Assert.Equal(0L, await ScalarAsync<long>("SELECT count(*) FROM v11.figures WHERE style = '{}'::jsonb"));
    }

    [Fact]
    public async Task EveryFigure_ExposesEachFixedStyleValue()
    {
        Assert.Equal(0L, await ScalarAsync<long>("""
            SELECT count(*) FROM v11.figures
            WHERE style->>'stroke' <> '#000000' OR style->>'stroke_width' <> '2'
               OR style->>'fill' <> '#FFFFFF' OR style->>'opacity' <> '1'
            """));
    }

    [Fact]
    public async Task EveryFigure_HasZeroRotation()
    {
        Assert.Equal(0L, await ScalarAsync<long>("SELECT count(*) FROM v11.figures WHERE rotation <> 0"));
    }

    [Fact]
    public async Task EveryFigure_BboxEqualsAFreshGeometryRecompute()
    {
        await using var connection = await fixture.OpenAsync();
        await using var command = new NpgsqlCommand("SELECT type, geometry::text, bbox_x, bbox_y, bbox_w, bbox_h FROM v11.figures", connection);
        await using var reader = await command.ExecuteReaderAsync();
        var count = 0;
        while (await reader.ReadAsync())
        {
            var definition = _registry.Get(reader.GetString(0));
            using var document = JsonDocument.Parse(reader.GetString(1));
            Assert.True(definition.TryParseGeometry(document.RootElement, out var geometry));
            var bounds = definition.BoundsOf(geometry);
            // Exact by design: both values come from the same parsed doubles, so tolerance hides defects.
            Assert.Equal(bounds.X, reader.GetDouble(2));
            Assert.Equal(bounds.Y, reader.GetDouble(3));
            Assert.Equal(bounds.W, reader.GetDouble(4));
            Assert.Equal(bounds.H, reader.GetDouble(5));
            count++;
        }

        Assert.Equal(795, count);
    }

    [Fact]
    public async Task SecondRun_IsIdempotentAndChangesNoCounts()
    {
        var before = await ScalarAsync<long>("SELECT count(*) FROM v11.figures");
        var report = await V11DataMigration.RunAsync(fixture.DataSource, _registry);
        Assert.Equal(0, report.CanvasesCreated);
        Assert.Equal(0, report.FiguresInserted);
        Assert.Equal(795, report.FiguresAlreadyPresent);
        Assert.Equal(before, await ScalarAsync<long>("SELECT count(*) FROM v11.figures"));
    }

    [Fact]
    public async Task EmptyLegacyFigures_StillCreateCanvases()
    {
        var report = await V11MigrationReplayFixture.MigrateFreshAsync(AdminConnectionString(), ScratchName(), async connection =>
        {
            await using var command = new NpgsqlCommand("INSERT INTO public.users (id, username, password) VALUES (1, 'a', 'x'), (2, 'b', 'x'), (3, 'c', 'x')", connection);
            await command.ExecuteNonQueryAsync();
        });
        Assert.Equal(3, report.UsersSeen);
        Assert.Equal(3, report.CanvasesCreated);
        Assert.Equal(0, report.FiguresSeen);
    }

    [Fact]
    public async Task EmptyLegacyDatabase_MigratesToNothing()
    {
        var report = await V11MigrationReplayFixture.MigrateFreshAsync(AdminConnectionString(), ScratchName(), _ => Task.CompletedTask);
        Assert.Equal(0, report.UsersSeen);
        Assert.Equal(0, report.CanvasesCreated);
        Assert.Equal(0, report.FiguresSeen);
    }

    [Fact]
    public async Task InvalidLegacyFigure_AbortsAndRollsBackAllData()
    {
        await using var database = await V11MigrationReplayFixture.CreateFreshAsync(AdminConnectionString(), ScratchName(), async connection =>
        {
            await using var command = new NpgsqlCommand("""
                ALTER TABLE public.figures DROP CONSTRAINT figures_type_is_known;
                INSERT INTO public.users (id, username, password) VALUES (1, 'a', 'x');
                INSERT INTO public.figures (id, user_id, type, x1, y1, x2, y2) VALUES (1, 1, 'unknown', 123, 456, 789, 1000);
                """, connection);
            await command.ExecuteNonQueryAsync();
        });

        var exception = await Assert.ThrowsAnyAsync<Exception>(() => V11DataMigration.RunAsync(database.DataSource, _registry));
        Assert.DoesNotContain("123", exception.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("456", exception.Message, StringComparison.Ordinal);
        await using var connection = await database.OpenAsync();
        Assert.Equal(0L, await ScalarAsync<long>(connection, "SELECT count(*) FROM v11.figures"));
        Assert.Equal(0L, await ScalarAsync<long>(connection, "SELECT count(*) FROM v11.canvases"));
    }

    [Fact]
    public async Task Migration_LeavesTheOldTablesAndConstraintsIntact()
    {
        Assert.Equal(795L, await ScalarAsync<long>("SELECT count(*) FROM public.figures"));
        Assert.Equal(708L, await ScalarAsync<long>("SELECT count(*) FROM public.users"));
        Assert.Equal(7L, await ScalarAsync<long>("SELECT count(*) FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'figures'"));
        Assert.Equal(4L, await ScalarAsync<long>("SELECT count(*) FROM pg_constraint WHERE conrelid = 'public.figures'::regclass AND contype = 'c'"));
    }

    public static IEnumerable<object[]> TabulatedRows =>
    [
        [new TabulatedRow(3860, "line", 100, 100, "{\"points\":[[0,0],[200,160]]}", [(100, 100), (300, 260)], null)],
        [new TabulatedRow(3861, "line", 400, 140, "{\"points\":[[0,160],[200,0]]}", [(400, 300), (600, 140)], null)],
        [new TabulatedRow(3862, "line", 100, 400, "{\"points\":[[0,0],[300,0]]}", [(100, 400), (400, 400)], null)],
        [new TabulatedRow(3863, "line", 700, 100, "{\"points\":[[0,0],[0,300]]}", [(700, 100), (700, 400)], null)],
        [new TabulatedRow(3864, "rectangle", 200, 200, "{\"w\":300,\"h\":180}", [(200, 200), (500, 200), (500, 380), (200, 380)], null)],
        [new TabulatedRow(3865, "circle", 300, 250, "{\"r\":100}", [(400, 350)], 100)],
        [new TabulatedRow(3866, "triangle", 250, 300, "{\"points\":[[100,0],[0,200],[200,200]]}", [(350, 300), (250, 500), (450, 500)], null)],
        [new TabulatedRow(3867, "rectangle", 280, 230, "{\"w\":240,\"h\":240}", [(280, 230), (520, 230), (520, 470), (280, 470)], null)],
    ];

    private async Task<FigureRecord> ReadFigureAsync(Guid id)
    {
        await using var connection = await fixture.OpenAsync();
        await using var command = new NpgsqlCommand("""
            SELECT id, canvas_id, type, x, y, rotation, geometry::text, z, bbox_x, bbox_y, bbox_w, bbox_h
            FROM v11.figures WHERE id = @id
            """, connection);
        command.Parameters.AddWithValue("id", id);
        await using var reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        return new FigureRecord(reader.GetGuid(0), reader.GetGuid(1), reader.GetString(2), reader.GetFieldValue<decimal>(3), reader.GetFieldValue<decimal>(4), reader.GetFieldValue<decimal>(5), reader.GetString(6), reader.GetFieldValue<decimal>(7), reader.GetDouble(8), reader.GetDouble(9), reader.GetDouble(10), reader.GetDouble(11));
    }

    private async Task<T> ScalarAsync<T>(string sql) { await using var c = await fixture.OpenAsync(); return await ScalarAsync<T>(c, sql); }

    private static async Task<T> ScalarAsync<T>(NpgsqlConnection connection, string sql)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        return (T)(await command.ExecuteScalarAsync())!;
    }

    private static async Task<List<int>> ReadIntListAsync(NpgsqlConnection connection, string sql, int id)
    {
        await using var command = new NpgsqlCommand(sql, connection); command.Parameters.AddWithValue("id", id);
        await using var reader = await command.ExecuteReaderAsync(); var result = new List<int>(); while (await reader.ReadAsync()) result.Add(reader.GetInt32(0)); return result;
    }

    private static async Task<List<Guid>> ReadGuidListAsync(NpgsqlConnection connection, string sql, Guid id)
    {
        await using var command = new NpgsqlCommand(sql, connection); command.Parameters.AddWithValue("id", id);
        await using var reader = await command.ExecuteReaderAsync(); var result = new List<Guid>(); while (await reader.ReadAsync()) result.Add(reader.GetGuid(0)); return result;
    }

    private static IReadOnlyList<(double X, double Y)> AbsoluteVertices(decimal x, decimal y, IFigureGeometry geometry) => geometry switch
    {
        LineGeometry line => line.Points.Select(p => ((double)x + p.X, (double)y + p.Y)).ToList(),
        TriangleGeometry triangle => triangle.Points.Select(p => ((double)x + p.X, (double)y + p.Y)).ToList(),
        RectangleGeometry rectangle => [((double)x, (double)y), ((double)x + rectangle.W, (double)y), ((double)x + rectangle.W, (double)y + rectangle.H), ((double)x, (double)y + rectangle.H)],
        CircleGeometry circle => [((double)x + circle.R, (double)y + circle.R)],
        _ => throw new ArgumentOutOfRangeException(nameof(geometry)),
    };

    private static string AdminConnectionString() => new NpgsqlConnectionStringBuilder(Environment.GetEnvironmentVariable("BLAZORCANVAS_TEST_CONNECTION") ?? "Host=localhost;Port=5433;Database=canvas;Username=postgres;Password=postgres") { Database = "postgres" }.ConnectionString;
    private static string ScratchName() => $"{V11MigrationReplayFixture.ScratchPrefix}{Guid.NewGuid():N}";

    public sealed record TabulatedRow(int OldId, string Type, decimal X, decimal Y, string GeometryJson, IReadOnlyList<(double X, double Y)> Vertices, double? CircleRadius);
    private sealed record FigureRecord(Guid Id, Guid CanvasId, string Type, decimal X, decimal Y, decimal Rotation, string GeometryJson, decimal Z, double BboxX, double BboxY, double BboxW, double BboxH);
}
