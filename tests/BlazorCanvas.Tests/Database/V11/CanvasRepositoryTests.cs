using BlazorCanvas.Data.V11;
using BlazorCanvas.Tests.Database;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace BlazorCanvas.Tests.Database.V11;

[Collection("Database")]
public class CanvasRepositoryTests
{
    private readonly DatabaseFixture _fixture;

    public CanvasRepositoryTests(DatabaseFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task EnsureForOwnerAsync_ReturnsTheMigratedDeterministicCanvas()
    {
        var ownerId = await CreateOwnerAsync();
        var id = V11DeterministicId.ForCanvas(ownerId);
        await InsertCanvasAsync(id, ownerId, "Migrated", 100, 200, "#000000");

        var canvas = await CreateRepository().EnsureForOwnerAsync(ownerId);

        Assert.Equal(id, canvas.Id);
        Assert.Equal(ownerId, canvas.OwnerId);
        Assert.Equal("Migrated", canvas.Name);
        Assert.Equal(100, canvas.Width);
        Assert.Equal(200, canvas.Height);
        Assert.Equal("#000000", canvas.Background);
    }

    [Fact]
    public async Task EnsureForOwnerAsync_CreatesOneDefaultFigurelessCanvas()
    {
        var ownerId = await CreateOwnerAsync();

        var canvas = await CreateRepository().EnsureForOwnerAsync(ownerId);

        Assert.Equal(V11DeterministicId.ForCanvas(ownerId), canvas.Id);
        Assert.Equal(ownerId, canvas.OwnerId);
        Assert.Equal(1472, canvas.Width);
        Assert.Equal(828, canvas.Height);
        Assert.Equal("Canvas", canvas.Name);
        Assert.Equal("#FFFFFF", canvas.Background);
        Assert.Equal(0L, await CountFiguresAsync(canvas.Id));
    }

    [Fact]
    public async Task EnsureForOwnerAsync_RepeatedAndConcurrentCallsRemainOneCanvas()
    {
        var ownerId = await CreateOwnerAsync();
        var repository = CreateRepository();

        var canvases = await Task.WhenAll(Enumerable.Range(0, 16)
            .Select(_ => repository.EnsureForOwnerAsync(ownerId)));

        Assert.All(canvases, canvas => Assert.Equal(V11DeterministicId.ForCanvas(ownerId), canvas.Id));
        Assert.Equal(1L, await CountCanvasesAsync(ownerId));
    }

    [Fact]
    public async Task EnsureForOwnerAsync_NeverReturnsAnotherOwnersCanvas()
    {
        var firstOwner = await CreateOwnerAsync();
        var secondOwner = await CreateOwnerAsync();
        var repository = CreateRepository();

        var first = await repository.EnsureForOwnerAsync(firstOwner);
        var second = await repository.EnsureForOwnerAsync(secondOwner);

        Assert.NotEqual(first.Id, second.Id);
        Assert.Equal(firstOwner, first.OwnerId);
        Assert.Equal(secondOwner, second.OwnerId);
    }

    [Fact]
    public async Task EnsureForOwnerAsync_PropagatesTheMissingOwnerForeignKeyFailure()
    {
        var exception = await Assert.ThrowsAsync<PostgresException>(() =>
            CreateRepository().EnsureForOwnerAsync(int.MaxValue));

        Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, exception.SqlState);
    }

    private CanvasRepository CreateRepository() => new(_fixture.DataSource);

    private async Task<int> CreateOwnerAsync()
    {
        await using var context = _fixture.CreateContext();
        return await DatabaseFixture.CreateTestUserAsync(context);
    }

    private async Task InsertCanvasAsync(Guid id, int ownerId, string name, int width, int height, string background)
    {
        const string sql = """
            INSERT INTO public.canvases (id, owner_id, name, width, height, background)
            VALUES (@id, @owner_id, @name, @width, @height, @background)
            """;
        await using var connection = await _fixture.OpenV11ConnectionAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("id", id);
        command.Parameters.AddWithValue("owner_id", ownerId);
        command.Parameters.AddWithValue("name", name);
        command.Parameters.AddWithValue("width", width);
        command.Parameters.AddWithValue("height", height);
        command.Parameters.AddWithValue("background", background);
        await command.ExecuteNonQueryAsync();
    }

    private async Task<long> CountCanvasesAsync(int ownerId)
    {
        await using var connection = await _fixture.OpenV11ConnectionAsync();
        await using var command = new NpgsqlCommand("SELECT count(*) FROM public.canvases WHERE owner_id = @owner_id", connection);
        command.Parameters.AddWithValue("owner_id", ownerId);
        return (long)(await command.ExecuteScalarAsync())!;
    }

    private async Task<long> CountFiguresAsync(Guid canvasId)
    {
        await using var connection = await _fixture.OpenV11ConnectionAsync();
        await using var command = new NpgsqlCommand("SELECT count(*) FROM public.figures WHERE canvas_id = @canvas_id", connection);
        command.Parameters.AddWithValue("canvas_id", canvasId);
        return (long)(await command.ExecuteScalarAsync())!;
    }
}
