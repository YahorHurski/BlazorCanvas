using System.Reflection;
using BlazorCanvas.Data.V11;
using BlazorCanvas.Shapes;
using BlazorCanvas.Tests.Database;
using BlazorCanvas.Tests.Shapes;
using Npgsql;

namespace BlazorCanvas.Tests.Database.V11;

[Collection("Database")]
public class FigureRepositoryTests
{
    private readonly DatabaseFixture _fixture;

    public FigureRepositoryTests(DatabaseFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task MoveAsync_WritesOnlyPositionColumns()
    {
        var canvasId = await CreateCanvasAsync();
        var repository = CreateRepository();
        var inserted = await repository.InsertAsync(canvasId, CreateInput(), 10m, 20m, 15m);
        var before = await ReadSnapshotAsync(inserted.Id);

        Assert.Equal(1, await repository.MoveAsync(canvasId, inserted.Id, 47.125m, -12.875m));
        var after = await ReadSnapshotAsync(inserted.Id);

        // This is the observational half of MODEL-01: the pinned SQL in the repository supplies
        // the other half. These eleven fields must remain byte-for-byte observationally unchanged.
        Assert.Equal(47.125m, after.X);
        Assert.Equal(-12.875m, after.Y);
        Assert.Equal(before.Type, after.Type);
        await V11JsonAssertions.AssertJsonbEqualAsync(_fixture, inserted.Id, "geometry", before.GeometryJson);
        await V11JsonAssertions.AssertJsonbEqualAsync(_fixture, inserted.Id, "style", before.StyleJson);
        Assert.Equal(before.Z, after.Z);
        Assert.Equal(before.Rotation, after.Rotation);
        Assert.Equal(before.CreatedAt, after.CreatedAt);
        Assert.Equal(before.BboxX, after.BboxX);
        Assert.Equal(before.BboxY, after.BboxY);
        Assert.Equal(before.BboxW, after.BboxW);
        Assert.Equal(before.BboxH, after.BboxH);
    }

    [Theory]
    [InlineData("line")]
    [InlineData("rectangle")]
    [InlineData("circle")]
    [InlineData("triangle")]
    [InlineData("pentagon")]
    public async Task MoveAsync_IsTypeBlindForEveryRegisteredShape(string type)
    {
        var canvasId = await CreateCanvasAsync();
        var repository = CreateRepository();
        if (type == "pentagon")
        {
            await EnsureFigureTypeAsync(type);
        }

        var inserted = await repository.InsertAsync(canvasId, CreateInput(type), 10m, 20m);

        Assert.Equal(1, await repository.MoveAsync(canvasId, inserted.Id, 47.5m, 7.75m));
        var moved = Assert.Single(await repository.LoadAsync(canvasId));

        // The five-vertex pentagon takes the identical call: D-22's promise is not four-shape-only.
        Assert.Equal(47.5m, moved.X);
        Assert.Equal(7.75m, moved.Y);
    }

    [Fact]
    public async Task InsertAsync_ReturnsPreGeneratedUuidPresentInTheDatabase()
    {
        var canvasId = await CreateCanvasAsync();
        var inserted = await CreateRepository().InsertAsync(canvasId, CreateInput(), 0m, 0m);

        Assert.NotEqual(Guid.Empty, inserted.Id);
        Assert.Contains(await CreateRepository().LoadAsync(canvasId), row => row.Id == inserted.Id);
    }

    [Fact]
    public async Task InsertAsync_IdenticalDrawsCreateDistinctRowsAndIds()
    {
        var canvasId = await CreateCanvasAsync();
        var repository = CreateRepository();

        var first = await repository.InsertAsync(canvasId, CreateInput(), 0m, 0m);
        var second = await repository.InsertAsync(canvasId, CreateInput(), 0m, 0m);

        Assert.NotEqual(first.Id, second.Id);
        Assert.Equal(2, (await repository.LoadAsync(canvasId)).Count);
    }

    [Fact]
    public async Task InsertWithIdAndZAsync_IsIdempotentForAnExistingId()
    {
        var canvasId = await CreateCanvasAsync();
        var repository = CreateRepository();
        var id = Guid.NewGuid();

        Assert.True(await repository.InsertWithIdAndZAsync(id, canvasId, CreateInput(), 0m, 0m, 1m));
        Assert.False(await repository.InsertWithIdAndZAsync(id, canvasId, CreateInput(), 0m, 0m, 1m));
        Assert.Single(await repository.LoadAsync(canvasId));
    }

    [Fact]
    public async Task InsertAsync_ConcurrentDrawsKeepEveryUuidAndLayerDistinct()
    {
        var canvasId = await CreateCanvasAsync();
        var repository = CreateRepository();

        var inserted = await Task.WhenAll(Enumerable.Range(0, 32)
            .Select(async index =>
            {
                // The five-attempt retry is for brief tab races, not an artificial 32-way barrier.
                // The deterministic two-connection suite separately forces the actual collision.
                await Task.Delay(TimeSpan.FromMilliseconds(index * 10));
                return await repository.InsertAsync(canvasId, CreateInput(), 0m, 0m);
            }));
        var loaded = await repository.LoadAsync(canvasId);

        Assert.Equal(32, loaded.Count);
        Assert.Equal(32, inserted.Select(row => row.Id).Distinct().Count());
        Assert.Equal(32, loaded.Select(row => row.Id).Distinct().Count());
        Assert.Equal(32, loaded.Select(row => row.Z).Distinct().Count());
    }

    [Fact]
    public async Task InsertAsync_ReusesTopLayerAfterItsFigureIsDeleted()
    {
        var canvasId = await CreateCanvasAsync();
        var repository = CreateRepository();
        var first = await repository.InsertAsync(canvasId, CreateInput(), 0m, 0m);
        var second = await repository.InsertAsync(canvasId, CreateInput(), 0m, 0m);

        Assert.Equal(1m, first.Z);
        Assert.Equal(2m, second.Z);
        Assert.Equal(1, await repository.DeleteAsync(canvasId, second.Id));
        Assert.Equal(2m, (await repository.InsertAsync(canvasId, CreateInput(), 0m, 0m)).Z);
    }

    [Fact]
    public async Task InsertWithIdAndZAsync_PreservesSixtyExactMidpointSubdivisions()
    {
        var canvasId = await CreateCanvasAsync();
        var repository = CreateRepository();
        Assert.True(await repository.InsertWithIdAndZAsync(Guid.NewGuid(), canvasId, CreateInput(), 0m, 0m, 1m));
        Assert.True(await repository.InsertWithIdAndZAsync(Guid.NewGuid(), canvasId, CreateInput(), 0m, 0m, 2m));

        var expected = new List<decimal> { 1m, 2m };
        var z = 1m;
        for (var index = 0; index < 60; index++)
        {
            z = (z + 2m) / 2m;
            Assert.True(await repository.InsertWithIdAndZAsync(Guid.NewGuid(), canvasId, CreateInput(), 0m, 0m, z));
            expected.Add(z);
        }

        var loaded = await repository.LoadAsync(canvasId);
        // D-63 chose numeric instead of double so these sixty layer subdivisions remain exact.
        Assert.Equal(62, loaded.Count);
        Assert.Equal(62, loaded.Select(row => row.Z).Distinct().Count());
        Assert.Equal(expected.OrderBy(value => value), loaded.Select(row => row.Z));
        Assert.All(expected, value => Assert.Contains(value, loaded.Select(row => row.Z)));
    }

    [Fact]
    public async Task LoadAsync_OrdersExplicitLayersAscendingRegardlessOfInsertOrder()
    {
        var canvasId = await CreateCanvasAsync();
        var repository = CreateRepository();
        foreach (var z in new[] { 8m, 1m, 4m, 2m })
        {
            Assert.True(await repository.InsertWithIdAndZAsync(Guid.NewGuid(), canvasId, CreateInput(), 0m, 0m, z));
        }

        Assert.Equal(new[] { 1m, 2m, 4m, 8m }, (await repository.LoadAsync(canvasId)).Select(row => row.Z));
    }

    [Fact]
    public async Task LoadAsync_DoesNotReturnAnotherCanvassFigure()
    {
        var firstCanvas = await CreateCanvasAsync();
        var secondCanvas = await CreateCanvasAsync();
        await CreateRepository().InsertAsync(firstCanvas, CreateInput(), 0m, 0m);

        Assert.Empty(await CreateRepository().LoadAsync(secondCanvas));
    }

    [Fact]
    public async Task MoveAndDeleteAsync_RespectCanvasOwnership()
    {
        var firstCanvas = await CreateCanvasAsync();
        var secondCanvas = await CreateCanvasAsync();
        var repository = CreateRepository();
        var figure = await repository.InsertAsync(firstCanvas, CreateInput(), 11m, 12m);

        // Migrated UUIDs are predictable by construction; canvas_id is the actual ownership guard.
        Assert.Equal(0, await repository.MoveAsync(secondCanvas, figure.Id, 99m, 99m));
        Assert.Equal(0, await repository.DeleteAsync(secondCanvas, figure.Id));
        var unchanged = Assert.Single(await repository.LoadAsync(firstCanvas));
        Assert.Equal(11m, unchanged.X);
        Assert.Equal(12m, unchanged.Y);
    }

    [Fact]
    public async Task MoveAndDeleteAsync_ReturnZeroForMissingFigureWithoutResurrection()
    {
        var canvasId = await CreateCanvasAsync();
        var repository = CreateRepository();
        var missing = Guid.NewGuid();

        Assert.Equal(0, await repository.MoveAsync(canvasId, missing, 1m, 1m));
        Assert.Equal(0, await repository.DeleteAsync(canvasId, missing));
        Assert.Empty(await repository.LoadAsync(canvasId));
    }

    [Fact]
    public async Task DeleteAsync_RemovesOnlyTheRequestedFigure()
    {
        var canvasId = await CreateCanvasAsync();
        var repository = CreateRepository();
        var deleted = await repository.InsertAsync(canvasId, CreateInput(), 0m, 0m);
        var kept = await repository.InsertAsync(canvasId, CreateInput("line"), 0m, 0m);

        Assert.Equal(1, await repository.DeleteAsync(canvasId, deleted.Id));
        var remaining = Assert.Single(await repository.LoadAsync(canvasId));
        Assert.Equal(kept.Id, remaining.Id);
    }

    [Fact]
    public async Task LoadAsync_ReturnsAnEmptyNonNullCollectionForANewCanvas()
    {
        Assert.Empty(await CreateRepository().LoadAsync(await CreateCanvasAsync()));
    }

    [Fact]
    public async Task InsertAsync_WritesLocalBoundsFromTheValidatedInput()
    {
        var canvasId = await CreateCanvasAsync();
        var input = CreateInput("rectangle");
        var inserted = await CreateRepository().InsertAsync(canvasId, input, 100m, 200m);

        Assert.Equal(input.Bounds.X, inserted.BboxX);
        Assert.Equal(input.Bounds.Y, inserted.BboxY);
        Assert.Equal(input.Bounds.W, inserted.BboxW);
        Assert.Equal(input.Bounds.H, inserted.BboxH);
    }

    [Fact]
    public async Task InsertAsync_PreservesRotationWithoutATypeBranch()
    {
        var canvasId = await CreateCanvasAsync();
        var inserted = await CreateRepository().InsertAsync(canvasId, CreateInput("triangle"), 0m, 0m, 17.5m);

        Assert.Equal(17.5m, inserted.Rotation);
    }

    [Fact]
    public async Task ExplicitZCollision_PropagatesRatherThanRelocatingTheFigure()
    {
        var canvasId = await CreateCanvasAsync();
        var repository = CreateRepository();
        Assert.True(await repository.InsertWithIdAndZAsync(Guid.NewGuid(), canvasId, CreateInput(), 0m, 0m, 1m));

        var exception = await Assert.ThrowsAsync<PostgresException>(() =>
            repository.InsertWithIdAndZAsync(Guid.NewGuid(), canvasId, CreateInput(), 0m, 0m, 1m));
        Assert.Equal(PostgresErrorCodes.UniqueViolation, exception.SqlState);
        Assert.Equal(ZUniqueConstraint, exception.ConstraintName);
    }

    [Fact]
    public void PublicRepositorySurface_TakesNoRawGeometryOrStyleStrings()
    {
        var methods = typeof(FigureRepository).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

        Assert.Equal(5, methods.Length);
        Assert.All(methods, method => Assert.Contains(method.GetParameters(), parameter => parameter.ParameterType == typeof(Guid)
            && parameter.Name == "canvasId"));
        Assert.DoesNotContain(methods.SelectMany(method => method.GetParameters()), parameter => parameter.ParameterType == typeof(string));
        Assert.Contains(typeof(FigureRepository).GetMethod(nameof(FigureRepository.InsertAsync))!.GetParameters(),
            parameter => parameter.ParameterType == typeof(ValidatedFigureInput));
        // This is 09-06 enforced by the type system rather than by a convention around raw JSON.
    }

    [Fact]
    public async Task LoadAsync_ReturnsTheFullStoredRowProjection()
    {
        var canvasId = await CreateCanvasAsync();
        var inserted = await CreateRepository().InsertAsync(canvasId, CreateInput("circle"), 3m, 4m, 5m);
        var loaded = Assert.Single(await CreateRepository().LoadAsync(canvasId));

        Assert.Equal(inserted, loaded);
    }

    [Fact]
    public async Task LoadAsync_ReturnsTheNamedCanvasIdOnEveryRow()
    {
        var canvasId = await CreateCanvasAsync();
        var repository = CreateRepository();
        await repository.InsertAsync(canvasId, CreateInput(), 0m, 0m);
        await repository.InsertAsync(canvasId, CreateInput("line"), 0m, 0m);

        Assert.All(await repository.LoadAsync(canvasId), row => Assert.Equal(canvasId, row.CanvasId));
    }

    [Fact]
    public async Task InsertWithIdAndZAsync_ReturnsFalseWithoutAddingARowForDuplicateId()
    {
        var canvasId = await CreateCanvasAsync();
        var repository = CreateRepository();
        var id = Guid.NewGuid();
        Assert.True(await repository.InsertWithIdAndZAsync(id, canvasId, CreateInput(), 0m, 0m, 1m));

        Assert.False(await repository.InsertWithIdAndZAsync(id, canvasId, CreateInput(), 100m, 100m, 2m));
        var row = Assert.Single(await repository.LoadAsync(canvasId));
        Assert.Equal(1m, row.Z);
        Assert.Equal(0m, row.X);
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

    private static ValidatedFigureInput CreateInput(string type = "rectangle")
    {
        var registry = DefaultShapes.CreateRegistry();
        if (type == "pentagon")
        {
            registry.Register(new PentagonShape());
        }

        var gateway = new FigureInputGateway(registry);
        var geometry = type switch
        {
            "line" => "{\"points\":[[0,0],[20,10]]}",
            "rectangle" => "{\"w\":20,\"h\":10}",
            "circle" => "{\"r\":10}",
            "triangle" => "{\"points\":[[10,0],[0,20],[20,20]]}",
            "pentagon" => "{\"points\":[[0,0],[10,0],[12,8],[5,12],[-2,8]]}",
            _ => throw new ArgumentOutOfRangeException(nameof(type)),
        };

        Assert.True(gateway.TryValidate(type, geometry, null, out var input));
        return input!;
    }

    private async Task<FigureSnapshot> ReadSnapshotAsync(Guid figureId)
    {
        const string sql = """
            SELECT type, x, y, rotation, geometry::text, style::text, z, created_at, bbox_x, bbox_y, bbox_w, bbox_h
            FROM v11.figures WHERE id = @id
            """;
        await using var connection = await _fixture.OpenV11ConnectionAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("id", figureId);
        await using var reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        return new FigureSnapshot(
            reader.GetString(0),
            reader.GetFieldValue<decimal>(1),
            reader.GetFieldValue<decimal>(2),
            reader.GetFieldValue<decimal>(3),
            reader.GetString(4),
            reader.GetString(5),
            reader.GetFieldValue<decimal>(6),
            reader.GetFieldValue<DateTimeOffset>(7),
            reader.GetDouble(8),
            reader.GetDouble(9),
            reader.GetDouble(10),
            reader.GetDouble(11));
    }

    private async Task EnsureFigureTypeAsync(string type)
    {
        const string sql = "INSERT INTO v11.figure_types (name) VALUES (@name) ON CONFLICT (name) DO NOTHING";
        await using var connection = await _fixture.OpenV11ConnectionAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("name", type);
        await command.ExecuteNonQueryAsync();
    }

    private sealed record FigureSnapshot(
        string Type,
        decimal X,
        decimal Y,
        decimal Rotation,
        string GeometryJson,
        string StyleJson,
        decimal Z,
        DateTimeOffset CreatedAt,
        double BboxX,
        double BboxY,
        double BboxW,
        double BboxH);

    private const string ZUniqueConstraint = "z_unique_per_canvas";
}

/// <summary>
/// v11 JSON assertion discipline: jsonb sorts object keys (length, then bytes), so canonical JSON
/// text is not comparable as a string. Arrays such as geometry points retain order and remain
/// load-bearing; callers compare those individual elements when that order is the behaviour under test.
/// </summary>
internal static class V11JsonAssertions
{
    public static async Task AssertJsonbEqualAsync(
        DatabaseFixture fixture,
        Guid figureId,
        string column,
        string expectedJson)
    {
        if (column is not ("geometry" or "style"))
        {
            throw new ArgumentOutOfRangeException(nameof(column));
        }

        var sql = $"SELECT {column} = @expected::jsonb FROM v11.figures WHERE id = @id";
        await using var connection = await fixture.OpenV11ConnectionAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("expected", expectedJson);
        command.Parameters.AddWithValue("id", figureId);
        Assert.True((bool)(await command.ExecuteScalarAsync())!);
    }
}
