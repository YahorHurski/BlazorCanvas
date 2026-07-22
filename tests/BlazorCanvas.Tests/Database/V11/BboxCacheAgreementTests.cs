using System.Text.Json;
using BlazorCanvas.Data.V11;
using BlazorCanvas.Shapes;
using BlazorCanvas.Tests.Database;
using BlazorCanvas.Tests.Shapes;
using Npgsql;

namespace BlazorCanvas.Tests.Database.V11;

[Collection("Database")]
public class BboxCacheAgreementTests
{
    private readonly DatabaseFixture _fixture;

    public BboxCacheAgreementTests(DatabaseFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task EveryStoredBboxAgreesExactlyWithAFreshGeometryRecompute()
    {
        var seeded = await SeedRepresentativePopulationAsync();

        // This has no WHERE or ORDER BY deliberately: it is a standing invariant for every row
        // in public.figures, including rows written by other tests, rather than a self-check.
        // Exact equality is appropriate because both values come from BoundsOf over the same parsed
        // doubles. Row order is immaterial because the assertion is independently per row.
        var inspection = await InspectTableAsync();
        Assert.Empty(inspection.Mismatches);
        Assert.Equal(await CountFiguresAsync(), inspection.VisitedRows);
        Assert.True(inspection.VisitedRows >= seeded.All.Count);

        Assert.Equal(0d, seeded.Horizontal.BboxH);
        Assert.Equal(0d, seeded.Vertical.BboxW);
        Assert.Empty(await FindMismatchesAsync(seeded.Horizontal.Id));
        Assert.Empty(await FindMismatchesAsync(seeded.Vertical.Id));

        // Equal geometry is not identity: each draw remains an independent stored row.
        var duplicateOne = await CreateRepository().InsertAsync(seeded.CanvasId, CreateInput("rectangle", "{\"w\":20,\"h\":10}"), 30m, 40m);
        var duplicateTwo = await CreateRepository().InsertAsync(seeded.CanvasId, CreateInput("rectangle", "{\"w\":20,\"h\":10}"), 30m, 40m);
        Assert.NotEqual(duplicateOne.Id, duplicateTwo.Id);
        Assert.Equal(duplicateOne.BboxX, duplicateTwo.BboxX);
        Assert.Equal(duplicateOne.BboxY, duplicateTwo.BboxY);
        Assert.Equal(duplicateOne.BboxW, duplicateTwo.BboxW);
        Assert.Equal(duplicateOne.BboxH, duplicateTwo.BboxH);
        Assert.Empty(await FindMismatchesAsync(duplicateOne.Id));
        Assert.Empty(await FindMismatchesAsync(duplicateTwo.Id));

        // A move writes only x and y; a local extent therefore remains a valid cache.
        Assert.Equal(1, await CreateRepository().MoveAsync(seeded.CanvasId, seeded.FarFromOrigin.Id, -500.5m, 900.25m));
        Assert.Empty(await FindMismatchesAsync(seeded.FarFromOrigin.Id));
    }

    [Theory]
    [InlineData("bbox_w")]
    [InlineData("bbox_x")]
    public async Task AgreementGuardDetectsDeliberatelyCorruptedCache(string column)
    {
        var seeded = await SeedRepresentativePopulationAsync();
        await using var connection = await _fixture.OpenV11ConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await using var command = new NpgsqlCommand($"UPDATE public.figures SET {column} = {column} + 1 WHERE id = @id", connection, transaction);
        command.Parameters.AddWithValue("id", seeded.FarFromOrigin.Id);
        Assert.Equal(1, await command.ExecuteNonQueryAsync());

        // A green whole-table run is evidence only because this probe proves the comparison fails.
        var mismatches = await FindMismatchesAsync(seeded.FarFromOrigin.Id, connection, transaction);
        Assert.NotEmpty(mismatches);
        Assert.Contains(mismatches, mismatch => mismatch.Contains(seeded.FarFromOrigin.Id.ToString(), StringComparison.Ordinal)
            && mismatch.Contains(column, StringComparison.Ordinal));
        // Disposing the uncommitted transaction rolls this corruption back.
    }

    [Fact]
    public async Task ZeroExtentLinesAreLegalAndRemainInAgreement()
    {
        var seeded = await SeedRepresentativePopulationAsync();

        Assert.Equal(0d, seeded.Horizontal.BboxH);
        Assert.Equal(0d, seeded.Vertical.BboxW);
        Assert.Empty(await FindMismatchesAsync(seeded.Horizontal.Id));
        Assert.Empty(await FindMismatchesAsync(seeded.Vertical.Id));
    }

    [Fact]
    public async Task NegativeBboxExtentIsRefusedByTheDatabase()
    {
        var canvasId = await CreateCanvasAsync();
        var exception = await Assert.ThrowsAsync<PostgresException>(() =>
            ExecuteRawInsertAsync(canvasId, "INSERT INTO public.figures (id, canvas_id, type, x, y, rotation, geometry, style, z, bbox_x, bbox_y, bbox_w, bbox_h) VALUES (@id, @canvas, 'rectangle', 0, 0, 0, '{\"w\":20,\"h\":10}'::jsonb, '{}'::jsonb, 1, 0, 0, -1, 10)"));

        Assert.Equal(PostgresErrorCodes.CheckViolation, exception.SqlState);
        Assert.Equal("bbox_is_positive", exception.ConstraintName);
    }

    [Fact]
    public async Task EveryBboxColumnIsRequired()
    {
        var canvasId = await CreateCanvasAsync();
        var exception = await Assert.ThrowsAsync<PostgresException>(() =>
            ExecuteRawInsertAsync(canvasId, "INSERT INTO public.figures (id, canvas_id, type, x, y, rotation, geometry, style, z, bbox_x, bbox_y, bbox_w) VALUES (@id, @canvas, 'rectangle', 0, 0, 0, '{\"w\":20,\"h\":10}'::jsonb, '{}'::jsonb, 1, 0, 0, 20)"));

        Assert.Equal(PostgresErrorCodes.NotNullViolation, exception.SqlState);
    }

    private async Task<SeededPopulation> SeedRepresentativePopulationAsync()
    {
        var canvasId = await CreateCanvasAsync();
        var repository = CreateRepository();
        var all = new List<FigureRow>
        {
            await repository.InsertAsync(canvasId, CreateInput("line", "{\"points\":[[0,10],[100,10]]}"), 0m, 0m),
            await repository.InsertAsync(canvasId, CreateInput("line", "{\"points\":[[10,0],[10,100]]}"), 1000m, -1000m),
            await repository.InsertAsync(canvasId, CreateInput("line", "{\"points\":[[0,0],[100,40]]}"), 10m, 20m),
            await repository.InsertAsync(canvasId, CreateInput("rectangle", "{\"w\":1,\"h\":1}"), 0m, 0m),
            await repository.InsertAsync(canvasId, CreateInput("circle", "{\"r\":12.5}"), -250.5m, 400.25m),
            await repository.InsertAsync(canvasId, CreateInput("triangle", "{\"points\":[[50.5,0],[0,80],[101,80]]}"), 42m, 42m),
        };

        return new SeededPopulation(canvasId, all, all[0], all[1], all[4]);
    }

    private async Task<List<string>> FindMismatchesAsync(Guid? onlyId = null, NpgsqlConnection? suppliedConnection = null, NpgsqlTransaction? transaction = null) =>
        (await InspectTableAsync(onlyId, suppliedConnection, transaction)).Mismatches;

    private async Task<TableInspection> InspectTableAsync(Guid? onlyId = null, NpgsqlConnection? suppliedConnection = null, NpgsqlTransaction? transaction = null)
    {
        const string sql = "SELECT id, type, geometry::text, bbox_x, bbox_y, bbox_w, bbox_h FROM public.figures";
        var ownsConnection = suppliedConnection is null;
        var connection = suppliedConnection ?? await _fixture.OpenV11ConnectionAsync();
        try
        {
            await using var command = new NpgsqlCommand(sql, connection, transaction);
            await using var reader = await command.ExecuteReaderAsync();
            var registry = DefaultShapes.CreateRegistry();
            registry.Register(new PentagonShape());
            var mismatches = new List<string>();
            var visitedRows = 0;

            while (await reader.ReadAsync())
            {
                var id = reader.GetGuid(0);
                visitedRows++;
                if (onlyId is Guid expectedId && id != expectedId)
                {
                    continue;
                }

                using var document = JsonDocument.Parse(reader.GetString(2));
                var definition = registry.Get(reader.GetString(1));
                Assert.True(definition.TryParseGeometry(document.RootElement, out var geometry), $"Figure {id} stores unparsable geometry.");
                var bounds = definition.BoundsOf(geometry!);
                AssertEqual(bounds.X, reader.GetDouble(3), id, "bbox_x", mismatches);
                AssertEqual(bounds.Y, reader.GetDouble(4), id, "bbox_y", mismatches);
                AssertEqual(bounds.W, reader.GetDouble(5), id, "bbox_w", mismatches);
                AssertEqual(bounds.H, reader.GetDouble(6), id, "bbox_h", mismatches);
            }

            return new TableInspection(mismatches, visitedRows);
        }
        finally
        {
            if (ownsConnection)
            {
                await connection.DisposeAsync();
            }
        }
    }

    private static void AssertEqual(double expected, double actual, Guid id, string column, List<string> mismatches)
    {
        if (expected != actual)
        {
            mismatches.Add($"Figure {id} has {column} {actual}, but BoundsOf recomputed {expected}.");
        }
    }

    private async Task<int> CountFiguresAsync()
    {
        await using var connection = await _fixture.OpenV11ConnectionAsync();
        await using var command = new NpgsqlCommand("SELECT count(*) FROM public.figures", connection);
        return Convert.ToInt32(await command.ExecuteScalarAsync());
    }

    private async Task ExecuteRawInsertAsync(Guid canvasId, string sql)
    {
        await using var connection = await _fixture.OpenV11ConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("id", Guid.NewGuid());
        command.Parameters.AddWithValue("canvas", canvasId);
        await command.ExecuteNonQueryAsync();
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

    private static ValidatedFigureInput CreateInput(string type, string geometry)
    {
        var gateway = new FigureInputGateway(DefaultShapes.CreateRegistry());
        Assert.True(gateway.TryValidate(type, geometry, null, out var input));
        return input!;
    }

    private sealed record SeededPopulation(Guid CanvasId, List<FigureRow> All, FigureRow Horizontal, FigureRow Vertical, FigureRow FarFromOrigin);

    private sealed record TableInspection(List<string> Mismatches, int VisitedRows);
}
