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
    public async Task EnsureAsync_IsRestartSafeForTheCompletedPublicCatalog()
    {
        await V11Cutover.EnsureAsync(fixture.DataSource, DefaultShapes.CreateRegistry());
        await using var connection = await fixture.OpenV11ConnectionAsync();
        await using var command = new NpgsqlCommand("SELECT to_regnamespace('v11') IS NULL AND to_regclass('public.figures') IS NOT NULL", connection);
        Assert.True((bool)(await command.ExecuteScalarAsync())!);
    }

    [Fact]
    public void Program_InvokesCutoverBeforeComponentRoutes()
    {
        var program = File.ReadAllText(Find("src", "BlazorCanvas", "Program.cs"));
        Assert.True(program.IndexOf("await V11Cutover.EnsureAsync", StringComparison.Ordinal) < program.IndexOf("app.MapRazorComponents", StringComparison.Ordinal));
    }

    private static string Find(params string[] segments)
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        { var path = Path.Combine([directory.FullName, .. segments]); if (File.Exists(path)) return path; }
        throw new FileNotFoundException();
    }
}
