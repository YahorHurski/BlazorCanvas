using BlazorCanvas.Data.V11;
using BlazorCanvas.Shapes;
using BlazorCanvas.Tests.Database;
using Npgsql;

namespace BlazorCanvas.Tests.Database.V11;

[Collection("Database")]
public class ZCollisionRetryTests
{
    private const string ZUniqueConstraint = "z_unique_per_canvas";
    private readonly DatabaseFixture _fixture;

    public ZCollisionRetryTests(DatabaseFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task InsertAsync_BlocksOnTheForcedCollisionThenCompletesAfterCommit()
    {
        var canvasId = await CreateCanvasAsync();
        var repository = CreateRepository();
        var first = await repository.InsertAsync(canvasId, CreateInput(), 0m, 0m);

        await using var connectionA = await _fixture.OpenV11ConnectionAsync();
        await using var transaction = await connectionA.BeginTransactionAsync();
        var competingId = await InsertRawAsync(connectionA, transaction, canvasId, 2m);
        var retryingInsert = repository.InsertAsync(canvasId, CreateInput(), 0m, 0m);

        await Task.Delay(100);
        Assert.False(retryingInsert.IsCompleted);
        await transaction.CommitAsync();
        var retried = await retryingInsert;

        var rows = await repository.LoadAsync(canvasId);
        Assert.Equal(new[] { 1m, 2m, 3m }, rows.Select(row => row.Z));
        Assert.Equal(3, rows.Count);
        Assert.Equal(3, rows.Select(row => row.Id).Distinct().Count());
        Assert.Contains(rows, row => row.Id == first.Id);
        Assert.Contains(rows, row => row.Id == competingId);
        Assert.Contains(rows, row => row.Id == retried.Id);
        // "Both figures present" matters: the failure prevented here is a draw silently vanishing.
    }

    [Fact]
    public async Task ForcedCollision_FailedAttemptAddsExactlyOneRetriedRow()
    {
        var canvasId = await CreateCanvasAsync();
        var repository = CreateRepository();
        await repository.InsertAsync(canvasId, CreateInput(), 0m, 0m);

        await using var connectionA = await _fixture.OpenV11ConnectionAsync();
        await using var transaction = await connectionA.BeginTransactionAsync();
        await InsertRawAsync(connectionA, transaction, canvasId, 2m);
        var retryingInsert = repository.InsertAsync(canvasId, CreateInput(), 0m, 0m);
        await Task.Delay(100);
        Assert.False(retryingInsert.IsCompleted);
        await transaction.CommitAsync();
        await retryingInsert;

        // MODEL-05 idempotency: the 23505 attempt wrote nothing; only its retry added one row.
        Assert.Equal(3, (await repository.LoadAsync(canvasId)).Count);
    }

    [Fact]
    public async Task InsertWithIdAndZAsync_DuplicateIdIsANoOpInsteadOfARetry()
    {
        var canvasId = await CreateCanvasAsync();
        var repository = CreateRepository();
        var id = Guid.NewGuid();

        Assert.True(await repository.InsertWithIdAndZAsync(id, canvasId, CreateInput(), 0m, 0m, 1m));
        Assert.False(await repository.InsertWithIdAndZAsync(id, canvasId, CreateInput(), 0m, 0m, 1m));
        Assert.Single(await repository.LoadAsync(canvasId));
    }

    [Fact]
    public async Task InsertWithIdAndZAsync_ZCollisionPropagatesInsteadOfRelocating()
    {
        var canvasId = await CreateCanvasAsync();
        var repository = CreateRepository();
        Assert.True(await repository.InsertWithIdAndZAsync(Guid.NewGuid(), canvasId, CreateInput(), 0m, 0m, 1m));

        var exception = await Assert.ThrowsAsync<PostgresException>(() =>
            repository.InsertWithIdAndZAsync(Guid.NewGuid(), canvasId, CreateInput(), 0m, 0m, 1m));

        // A blanket PostgresException retry would swallow this data error and alter legacy stacking.
        Assert.Equal(PostgresErrorCodes.UniqueViolation, exception.SqlState);
        Assert.Equal(ZUniqueConstraint, exception.ConstraintName);
    }

    [Fact]
    public async Task InsertAsync_RetryTerminatesAfterAContendedRangeIsCommitted()
    {
        var canvasId = await CreateCanvasAsync();
        var repository = CreateRepository();
        await repository.InsertAsync(canvasId, CreateInput(), 0m, 0m);

        await using var connectionA = await _fixture.OpenV11ConnectionAsync();
        await using var transaction = await connectionA.BeginTransactionAsync();
        for (var z = 2m; z <= 7m; z++)
        {
            await InsertRawAsync(connectionA, transaction, canvasId, z);
        }

        var inserting = repository.InsertAsync(canvasId, CreateInput(), 0m, 0m);
        await Task.Delay(100);
        Assert.False(inserting.IsCompleted);
        await transaction.CommitAsync();

        var finished = await Task.WhenAny(inserting, Task.Delay(TimeSpan.FromSeconds(5)));
        Assert.Same(inserting, finished);
        try
        {
            await inserting;
        }
        catch (PostgresException exception)
        {
            Assert.Equal(PostgresErrorCodes.UniqueViolation, exception.SqlState);
        }

        // The retry is capped at five; an unbounded loop would turn contention into a hung circuit.
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

    private static ValidatedFigureInput CreateInput()
    {
        var gateway = new FigureInputGateway(DefaultShapes.CreateRegistry());
        Assert.True(gateway.TryValidate("rectangle", "{\"w\":20,\"h\":10}", null, out var input));
        return input!;
    }

    private static async Task<Guid> InsertRawAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid canvasId,
        decimal z)
    {
        const string sql = """
            INSERT INTO v11.figures (id, canvas_id, type, x, y, rotation, geometry, style, z, bbox_x, bbox_y, bbox_w, bbox_h)
            VALUES (@id, @canvas_id, 'rectangle', 0, 0, 0, '{"w":20,"h":10}'::jsonb, '{}'::jsonb, @z, 0, 0, 20, 10)
            """;
        var id = Guid.NewGuid();
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("id", id);
        command.Parameters.AddWithValue("canvas_id", canvasId);
        command.Parameters.AddWithValue("z", z);
        await command.ExecuteNonQueryAsync();
        return id;
    }
}
