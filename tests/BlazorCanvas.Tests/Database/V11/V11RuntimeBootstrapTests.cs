using BlazorCanvas.Data.V11;
using BlazorCanvas.Shapes;
using BlazorCanvas.Tests.Database;
using Npgsql;
using System.Reflection;

namespace BlazorCanvas.Tests.Database.V11;

[Collection("Database")]
public class V11RuntimeBootstrapTests
{
    private readonly DatabaseFixture _fixture;

    public V11RuntimeBootstrapTests(DatabaseFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task EnsureAsync_FirstRunMigratesAndKeepsTheLegacyTable()
    {
        await using var database = await CreateLegacyDatabaseAsync("rectangle");

        await V11RuntimeBootstrap.EnsureAsync(database.DataSource, DefaultShapes.CreateRegistry());

        await using var connection = await database.OpenAsync();
        Assert.True(await HasTableAsync(connection, "public.figures"));
        Assert.Equal(1L, await ScalarAsync<long>(connection, "SELECT count(*) FROM v11.canvases"));
        Assert.Equal(1L, await ScalarAsync<long>(connection, "SELECT count(*) FROM v11.figures"));
        Assert.Equal(4L, await ScalarAsync<long>(connection, "SELECT count(*) FROM v11.figure_types"));
    }

    [Fact]
    public async Task EnsureAsync_RerunIsIdempotentForFiguresAndFigurelessCanvases()
    {
        await using var database = await CreateLegacyDatabaseAsync(figureType: null);

        await V11RuntimeBootstrap.EnsureAsync(database.DataSource, DefaultShapes.CreateRegistry());
        await using var connection = await database.OpenAsync();
        var firstCanvasCount = await ScalarAsync<long>(connection, "SELECT count(*) FROM v11.canvases");
        var firstFigureCount = await ScalarAsync<long>(connection, "SELECT count(*) FROM v11.figures");

        await V11RuntimeBootstrap.EnsureAsync(database.DataSource, DefaultShapes.CreateRegistry());

        Assert.Equal(firstCanvasCount, await ScalarAsync<long>(connection, "SELECT count(*) FROM v11.canvases"));
        Assert.Equal(firstFigureCount, await ScalarAsync<long>(connection, "SELECT count(*) FROM v11.figures"));
        Assert.Equal(1L, firstCanvasCount);
        Assert.Equal(0L, firstFigureCount);
    }

    [Fact]
    public async Task EnsureAsync_PropagatesAnInvalidLegacyCatalogFailure()
    {
        await using var database = await CreateLegacyDatabaseAsync("rectangle");
        var incompleteRegistry = new ShapeRegistry();
        incompleteRegistry.Register(new LineShape());

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            V11RuntimeBootstrap.EnsureAsync(database.DataSource, incompleteRegistry));
    }

    [Fact]
    public async Task EnsureAsync_RejectsAStoreWithoutTheEfUsersSchema()
    {
        await using var database = await V11MigrationReplayFixture.CreateFreshAsync(
            AdminConnectionString(),
            $"{V11MigrationReplayFixture.ScratchPrefix}{Guid.NewGuid():N}",
            _ => Task.CompletedTask);
        await using var connection = await database.OpenAsync();
        await using (var command = new NpgsqlCommand("DROP TABLE public.figures; DROP TABLE public.users;", connection))
        {
            await command.ExecuteNonQueryAsync();
        }

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            V11RuntimeBootstrap.EnsureAsync(database.DataSource, DefaultShapes.CreateRegistry()));

        Assert.Contains("users schema", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Program_BootstrapsThePooledV11GraphBeforeComponentRoutes()
    {
        var program = File.ReadAllText(FindRepositoryFile("src", "BlazorCanvas", "Program.cs"));
        var efMigration = program.IndexOf("db.Database.Migrate()", StringComparison.Ordinal);
        var bootstrap = program.IndexOf("await V11RuntimeBootstrap.EnsureAsync", StringComparison.Ordinal);
        var componentRoutes = program.IndexOf("app.MapRazorComponents", StringComparison.Ordinal);

        Assert.True(efMigration >= 0, "Program.cs must migrate the EF users schema.");
        Assert.True(bootstrap > efMigration, "The v11 bootstrap must follow EF user migration.");
        Assert.True(componentRoutes > bootstrap, "The v11 bootstrap must precede interactive component routes.");
        Assert.Equal(1, CountOccurrences(program, "AddSingleton<NpgsqlDataSource>"));
        Assert.Contains("NpgsqlDataSource.Create(", program, StringComparison.Ordinal);
        Assert.Contains("AddSingleton(DefaultShapes.CreateRegistry())", program, StringComparison.Ordinal);
        Assert.Contains("AddScoped<FigureInputGateway>()", program, StringComparison.Ordinal);
        Assert.Contains("AddScoped<FigureRepository>()", program, StringComparison.Ordinal);
        Assert.Contains("AddScoped<CanvasRepository>()", program, StringComparison.Ordinal);
    }

    [Fact]
    public void FigureRepository_StillAllowsOnlyValidatedInputsForWrites()
    {
        var methods = typeof(FigureRepository).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

        Assert.DoesNotContain(methods.SelectMany(method => method.GetParameters()), parameter =>
            parameter.ParameterType == typeof(string));
        Assert.Contains(typeof(FigureRepository).GetMethod(nameof(FigureRepository.InsertAsync))!.GetParameters(), parameter =>
            parameter.ParameterType == typeof(ValidatedFigureInput));
    }

    private async Task<V11MigrationReplayFixture.FreshMigrationDatabase> CreateLegacyDatabaseAsync(string? figureType)
    {
        return await V11MigrationReplayFixture.CreateFreshAsync(
            AdminConnectionString(),
            $"{V11MigrationReplayFixture.ScratchPrefix}{Guid.NewGuid():N}",
            async connection =>
            {
                await using (var user = new NpgsqlCommand(
                    "INSERT INTO public.users (id, username, password) VALUES (1, 'bootstrap-user', 'irrelevant')", connection))
                {
                    await user.ExecuteNonQueryAsync();
                }

                if (figureType is not null)
                {
                    await using var figure = new NpgsqlCommand(
                        "INSERT INTO public.figures (id, user_id, type, x1, y1, x2, y2) VALUES (1, 1, @type, 0, 0, 20, 10)", connection);
                    figure.Parameters.AddWithValue("type", figureType);
                    await figure.ExecuteNonQueryAsync();
                }
            });
    }

    private string AdminConnectionString()
    {
        var builder = new NpgsqlConnectionStringBuilder(_fixture.ConnectionString) { Database = "postgres" };
        return builder.ConnectionString;
    }

    private static async Task<bool> HasTableAsync(NpgsqlConnection connection, string name)
    {
        await using var command = new NpgsqlCommand("SELECT to_regclass(@name) IS NOT NULL", connection);
        command.Parameters.AddWithValue("name", name);
        return (bool)(await command.ExecuteScalarAsync())!;
    }

    private static async Task<T> ScalarAsync<T>(NpgsqlConnection connection, string sql)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        return (T)(await command.ExecuteScalarAsync())!;
    }

    private static string FindRepositoryFile(params string[] segments)
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            var path = Path.Combine([directory.FullName, .. segments]);
            if (File.Exists(path))
            {
                return path;
            }
        }

        throw new FileNotFoundException($"Could not locate repository file '{Path.Combine(segments)}'.");
    }

    private static int CountOccurrences(string value, string substring)
    {
        var count = 0;
        var position = 0;
        while ((position = value.IndexOf(substring, position, StringComparison.Ordinal)) >= 0)
        {
            count++;
            position += substring.Length;
        }

        return count;
    }
}
