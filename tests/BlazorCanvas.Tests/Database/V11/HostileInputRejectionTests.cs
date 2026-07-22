using System.Reflection;
using System.Text.Json;
using BlazorCanvas.Data.V11;
using BlazorCanvas.Shapes;
using BlazorCanvas.Tests.Database;
using BlazorCanvas.Tests.Shapes;
using Npgsql;

namespace BlazorCanvas.Tests.Database.V11;

[Collection("Database")]
public class HostileInputRejectionTests
{
    private readonly DatabaseFixture _fixture;

    public HostileInputRejectionTests(DatabaseFixture fixture) => _fixture = fixture;

    // 09-06 proves the gateway returns false. This suite proves the rejected payload has no path
    // into PostgreSQL, where D-60 deliberately leaves geometry without CHECK constraints.
    [Theory]
    [MemberData(nameof(HostileGeometryCases))]
    public async Task HostileGeometryNeverBecomesARow(string? typeName, string? geometryJson)
    {
        var canvasId = await CreateCanvasAsync();
        var gateway = CreateGateway();

        Assert.False(gateway.TryValidate(typeName, geometryJson, null, out var input));
        Assert.Null(input);
        // There is intentionally no repository call after rejection: the canvas count is the
        // consequence assertion that proves the hostile corpus did not become stored data.
        Assert.Equal(0, await CountFiguresForCanvasAsync(canvasId));
    }

    [Theory]
    [MemberData(nameof(HostileStyleCases))]
    public async Task HostileStyleIsSanitisedInTheValuePostgreSqlActuallyStores(string styleJson, string hostileValue, FigureStyle expected)
    {
        var canvasId = await CreateCanvasAsync();
        var gateway = CreateGateway();
        Assert.True(gateway.TryValidate("circle", "{\"r\":50}", styleJson, out var input));

        var inserted = await CreateRepository().InsertAsync(canvasId, input!, 10m, 20m);
        var stored = await ReadJsonColumnAsync(inserted.Id, "style");

        // Reading PostgreSQL, rather than the in-memory record, is the point of this boundary test.
        Assert.DoesNotContain(JsonSerializer.Serialize(hostileValue), stored, StringComparison.Ordinal);
        Assert.Equal(expected, StyleGateway.Parse(stored));
        using var document = JsonDocument.Parse(stored);
        Assert.Equal(
            new[] { "stroke", "stroke_width", "fill", "opacity" }.OrderBy(name => name),
            document.RootElement.EnumerateObject().Select(property => property.Name).OrderBy(name => name));
    }

    [Theory]
    [MemberData(nameof(HostileExtraContentCases))]
    public async Task StoredGeometryIsOnlyTheCanonicalValidatedValue(string typeName, string geometryJson, string expectedJson, string[] hostileSubstrings)
    {
        var canvasId = await CreateCanvasAsync();
        var gateway = CreateGateway();
        Assert.True(gateway.TryValidate(typeName, geometryJson, "{\"stroke\":\"#000000\\\" onload=\\\"alert(1)\"}", out var input));

        var inserted = await CreateRepository().InsertAsync(canvasId, input!, 0m, 0m);
        await V11JsonAssertions.AssertJsonbEqualAsync(_fixture, inserted.Id, "geometry", expectedJson);
        var stored = await ReadJsonColumnAsync(inserted.Id, "geometry");
        foreach (var hostile in hostileSubstrings)
        {
            Assert.DoesNotContain(hostile, stored, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void RepositoryCannotBeReachedWithRawGeometryOrStyleText()
    {
        var publicMethods = typeof(FigureRepository).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        Assert.DoesNotContain(publicMethods.SelectMany(method => method.GetParameters()), parameter => parameter.ParameterType == typeof(string));
        Assert.Contains(
            typeof(FigureRepository).GetMethod(nameof(FigureRepository.InsertAsync))!.GetParameters(),
            parameter => parameter.ParameterType == typeof(ValidatedFigureInput));
        Assert.Empty(typeof(ValidatedFigureInput).GetConstructors(BindingFlags.Public | BindingFlags.Instance));

        // A future raw-text write path must route through FigureInputGateway, never add an overload.
    }

    [Fact]
    public async Task DatabaseGuaranteesAndTheDeliberateD60GapAreBothPinned()
    {
        var canvasId = await CreateCanvasAsync();
        await using (var connection = await _fixture.OpenV11ConnectionAsync())
        await using (var transaction = await connection.BeginTransactionAsync())
        {
            // This deliberately asserts D-60's accepted weakness: geometry correctness is now entirely
            // the gateway's responsibility, so a raw negative-radius JSON object reaches PostgreSQL.
            await ExecuteRawInsertAsync(connection, transaction, canvasId, "circle", "{\"r\":-5}", "{}", Guid.NewGuid());
            Assert.False(CreateGateway().TryValidate("circle", "{\"r\":-5}", null, out _));
            // The transaction is never committed, so the deliberate invalid geometry cannot leak.
        }

        var styleException = await AssertConstraintRefusalAsync(canvasId, "circle", "{\"r\":5}", "[]");
        Assert.Equal(PostgresErrorCodes.CheckViolation, styleException.SqlState);
        Assert.Equal("style_is_object", styleException.ConstraintName);

        var geometryException = await AssertConstraintRefusalAsync(canvasId, "circle", "[]", "{}");
        Assert.Equal(PostgresErrorCodes.CheckViolation, geometryException.SqlState);
        Assert.Equal("geometry_is_object", geometryException.ConstraintName);
    }

    [Fact]
    public void HostileCorporaCannotBecomeVacuous()
    {
        Assert.True(HostileGeometryCases().Count() >= 25);
        Assert.True(HostileStyleCases().Count() >= 10);
    }

    public static IEnumerable<object?[]> HostileGeometryCases() => FigureInputGatewayTests.HostileGeometryCases();

    public static IEnumerable<object[]> HostileExtraContentCases() => FigureInputGatewayTests.HostileExtraContentCases();

    public static IEnumerable<object[]> HostileStyleCases()
    {
        foreach (var testCase in FigureInputGatewayTests.HostileStyleCases())
        {
            yield return testCase;
        }

        // These complete the 09-02 hostile style corpus with its opacity and markup cases.
        yield return ["{\"opacity\":-0.1}", "-0.1", new FigureStyle(Opacity: 0)];
        yield return ["{\"stroke\":\"#000\\\" /><script>x</script>\"}", "#000\" /><script>x</script>", new FigureStyle()];
    }

    private async Task ExecuteRawInsertAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid canvasId,
        string type,
        string geometry,
        string style,
        Guid id)
    {
        const string sql = """
            INSERT INTO v11.figures (id, canvas_id, type, x, y, rotation, geometry, style, z, bbox_x, bbox_y, bbox_w, bbox_h)
            VALUES (@id, @canvas, @type, 0, 0, 0, @geometry::jsonb, @style::jsonb, 1, 0, 0, 10, 10)
            """;
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("id", id);
        command.Parameters.AddWithValue("canvas", canvasId);
        command.Parameters.AddWithValue("type", type);
        command.Parameters.AddWithValue("geometry", geometry);
        command.Parameters.AddWithValue("style", style);
        await command.ExecuteNonQueryAsync();
    }

    private async Task<PostgresException> AssertConstraintRefusalAsync(Guid canvasId, string type, string geometry, string style)
    {
        await using var connection = await _fixture.OpenV11ConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        return await Assert.ThrowsAsync<PostgresException>(() =>
            ExecuteRawInsertAsync(connection, transaction, canvasId, type, geometry, style, Guid.NewGuid()));
    }

    private async Task<string> ReadJsonColumnAsync(Guid figureId, string column)
    {
        if (column is not ("geometry" or "style"))
        {
            throw new ArgumentOutOfRangeException(nameof(column));
        }

        await using var connection = await _fixture.OpenV11ConnectionAsync();
        await using var command = new NpgsqlCommand($"SELECT {column}::text FROM v11.figures WHERE id = @id", connection);
        command.Parameters.AddWithValue("id", figureId);
        return (string)(await command.ExecuteScalarAsync())!;
    }

    private async Task<int> CountFiguresForCanvasAsync(Guid canvasId)
    {
        await using var connection = await _fixture.OpenV11ConnectionAsync();
        await using var command = new NpgsqlCommand("SELECT count(*) FROM v11.figures WHERE canvas_id = @canvas", connection);
        command.Parameters.AddWithValue("canvas", canvasId);
        return Convert.ToInt32(await command.ExecuteScalarAsync());
    }

    private FigureRepository CreateRepository() => new(_fixture.DataSource);

    private async Task<Guid> CreateCanvasAsync()
    {
        int ownerId;
        await using (var context = _fixture.CreateContext())
        {
            ownerId = await DatabaseFixture.CreateTestUserAsync(context);
        }

        await using var connection = await _fixture.OpenV11ConnectionAsync();
        return await DatabaseFixture.CreateTestCanvasAsync(connection, ownerId);
    }

    private static FigureInputGateway CreateGateway() => new(DefaultShapes.CreateRegistry());
}
