using BlazorCanvas.Data.V11;
using BlazorCanvas.Shapes;
using Npgsql;

namespace BlazorCanvas.Tests.Database.V11;

[Collection("Database")]
public class V11CutoverTests
{
    private readonly DatabaseFixture fixture;
    public V11CutoverTests(DatabaseFixture fixture) => this.fixture = fixture;

    [Fact]
    public async Task ScratchDatabase_CanBeCreatedAndDisposedWithoutTouchingTheSharedCatalog()
    {
        await using var scratch = await V11CutoverScratchDatabase.CreateAsync(fixture.ConnectionString);
        await scratch.SetupFreshUsersOnlyAsync();
        Assert.Contains("TABLE public.users", await scratch.SnapshotAsync());
    }

    [Fact]
    public async Task LegacyOnlyCatalog_MigratesAndPromotesRepresentativeRows()
    {
        await using var scratch = await V11CutoverScratchDatabase.CreateAsync(fixture.ConnectionString);
        await scratch.SetupLegacyOnlyAsync();

        await V11Cutover.EnsureAsync(scratch.DataSource, DefaultShapes.CreateRegistry());

        await AssertFinalPublicCatalogAsync(scratch.DataSource);
        await using var connection = await scratch.DataSource.OpenConnectionAsync();
        await using var command = new NpgsqlCommand("SELECT id, canvas_id, x, y, geometry::text, style::text, z FROM public.figures ORDER BY z", connection);
        await using var rows = await command.ExecuteReaderAsync();
        Assert.True(await rows.ReadAsync());
        Assert.Equal(V11DeterministicId.ForFigure(1), rows.GetGuid(0));
        Assert.Equal(V11DeterministicId.ForCanvas(1), rows.GetGuid(1));
        Assert.Equal(10m, rows.GetDecimal(2));
        Assert.Equal(20m, rows.GetDecimal(3));
        Assert.Contains("50", rows.GetString(4));
        Assert.Contains("fill", rows.GetString(5));
        Assert.Equal(1m, rows.GetDecimal(6));
        Assert.True(await rows.ReadAsync());
        Assert.Equal(V11DeterministicId.ForFigure(2), rows.GetGuid(0));
        Assert.Equal(2m, rows.GetDecimal(6));
        Assert.False(await rows.ReadAsync());
    }

    [Fact]
    public async Task AdditiveCatalog_RerunsWithoutDuplicateRows()
    {
        await using var scratch = await V11CutoverScratchDatabase.CreateAsync(fixture.ConnectionString);
        await scratch.SetupAdditiveAsync();

        await V11Cutover.EnsureAsync(scratch.DataSource, DefaultShapes.CreateRegistry());

        await AssertFinalPublicCatalogAsync(scratch.DataSource);
        await using var connection = await scratch.DataSource.OpenConnectionAsync();
        Assert.Equal(1L, await ScalarAsync<long>(connection, "SELECT count(*) FROM public.canvases"));
        Assert.Equal(4L, await ScalarAsync<long>(connection, "SELECT count(*) FROM public.figure_types"));
        Assert.Equal(2L, await ScalarAsync<long>(connection, "SELECT count(*) FROM public.figures"));
    }

    [Fact]
    public async Task FreshUsersOnlyCatalog_InstallsFinalPublicStorageWithoutLegacyFigures()
    {
        await using var scratch = await V11CutoverScratchDatabase.CreateAsync(fixture.ConnectionString);
        await scratch.SetupFreshUsersOnlyAsync();

        await V11Cutover.EnsureAsync(scratch.DataSource, DefaultShapes.CreateRegistry());

        await AssertFinalPublicCatalogAsync(scratch.DataSource);
        await using var connection = await scratch.DataSource.OpenConnectionAsync();
        Assert.Equal(0L, await ScalarAsync<long>(connection, "SELECT count(*) FROM public.canvases"));
        Assert.Equal(4L, await ScalarAsync<long>(connection, "SELECT count(*) FROM public.figure_types"));
        Assert.Equal(0L, await ScalarAsync<long>(connection, "SELECT count(*) FROM public.figures"));
    }

    [Fact]
    public async Task CompletedPublicCatalog_NormalEnsureIsAnExactNoOp()
    {
        await using var scratch = await V11CutoverScratchDatabase.CreateAsync(fixture.ConnectionString);
        await scratch.SetupCompletedPublicAsync();
        var before = await scratch.SnapshotAsync();

        await V11Cutover.EnsureAsync(scratch.DataSource, DefaultShapes.CreateRegistry());

        Assert.Equal(before, await scratch.SnapshotAsync());
        await AssertFinalPublicCatalogAsync(scratch.DataSource);
    }

    [Fact]
    public async Task InvalidPartialCatalog_IsRejectedBeforePromotion()
    {
        await using var scratch = await V11CutoverScratchDatabase.CreateAsync(fixture.ConnectionString);
        await scratch.SetupInvalidAsync();
        var before = await scratch.SnapshotAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() => V11Cutover.EnsureAsync(scratch.DataSource, DefaultShapes.CreateRegistry()));

        Assert.Equal(before, await scratch.SnapshotAsync());
    }

    [Theory]
    [InlineData("AfterSchemaApply", false)]
    [InlineData("AfterTypeSeed", false)]
    [InlineData("AfterMigration", true)]
    [InlineData("AfterDropPublicFigures", false)]
    [InlineData("AfterPromoteCanvases", false)]
    public async Task InjectedTransactionFailures_RollBackEveryCatalogAndRemainRetryable(string stageName, bool legacy)
    {
        var stage = Enum.Parse<V11CutoverStage>(stageName);
        await using var scratch = await V11CutoverScratchDatabase.CreateAsync(fixture.ConnectionString);
        if (legacy) await scratch.SetupLegacyOnlyAsync(); else await scratch.SetupFreshUsersOnlyAsync();
        var before = await scratch.SnapshotAsync();

        await Assert.ThrowsAsync<CutoverProbeException>(() => V11Cutover.EnsureAsync(
            scratch.DataSource,
            DefaultShapes.CreateRegistry(),
            (current, _) => current == stage ? Task.FromException(new CutoverProbeException()) : Task.CompletedTask));

        Assert.Equal(before, await scratch.SnapshotAsync());
        await V11Cutover.EnsureAsync(scratch.DataSource, DefaultShapes.CreateRegistry());
        await AssertFinalPublicCatalogAsync(scratch.DataSource);
    }

    [Fact]
    public void Program_InvokesCutoverBeforeComponentRoutes()
    {
        var program = File.ReadAllText(Find("src", "BlazorCanvas", "Program.cs"));
        Assert.True(program.IndexOf("await V11Cutover.EnsureAsync", StringComparison.Ordinal) < program.IndexOf("app.MapRazorComponents", StringComparison.Ordinal));
    }

    private static async Task AssertFinalPublicCatalogAsync(NpgsqlDataSource dataSource)
    {
        await using var connection = await dataSource.OpenConnectionAsync();
        await AssertColumnsAsync(connection, "canvases", ["id", "owner_id", "name", "width", "height", "background", "created_at"]);
        await AssertColumnsAsync(connection, "figure_types", ["name"]);
        await AssertColumnsAsync(connection, "figures", ["id", "canvas_id", "type", "x", "y", "rotation", "geometry", "style", "z", "bbox_x", "bbox_y", "bbox_w", "bbox_h", "created_at"]);
        Assert.Equal("uuid", await ScalarAsync<string>(connection, "SELECT data_type FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'figures' AND column_name = 'id'"));
        Assert.False(await ScalarAsync<bool>(connection, "SELECT to_regnamespace('v11') IS NOT NULL"));
        Assert.False(await ScalarAsync<bool>(connection, "SELECT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'figures' AND column_name IN ('x1', 'y1', 'x2', 'y2'))"));
        Assert.False(await ScalarAsync<bool>(connection, "SELECT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'legacy_type_check')"));
    }

    private static async Task AssertColumnsAsync(NpgsqlConnection connection, string table, string[] expected)
    {
        await using var command = new NpgsqlCommand("SELECT column_name FROM information_schema.columns WHERE table_schema = 'public' AND table_name = @table ORDER BY ordinal_position", connection);
        command.Parameters.AddWithValue("table", table);
        var actual = new List<string>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync()) actual.Add(reader.GetString(0));
        Assert.Equal(expected, actual);
    }

    private static async Task<T> ScalarAsync<T>(NpgsqlConnection connection, string sql)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        return (T)(await command.ExecuteScalarAsync())!;
    }

    private static string Find(params string[] segments)
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        { var path = Path.Combine([directory.FullName, .. segments]); if (File.Exists(path)) return path; }
        throw new FileNotFoundException();
    }

    private sealed class CutoverProbeException : Exception;
}
