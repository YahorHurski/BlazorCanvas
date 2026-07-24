using System.Diagnostics;
using System.Text.Json;
using BlazorCanvas.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace BlazorCanvas.Tests.Migrations;

public sealed class MigrationRoundTripTests
{
    private const string AdminConnectionString =
        "Host=localhost;Port=5433;Database=postgres;Username=postgres;Password=postgres";

    [Fact]
    public async Task AnchorGeometryRewrite_PreservesEveryFixtureFigure()
    {
        var repoRoot = FindRepoRoot();
        var databaseName = "canvas_migtest_" + Guid.NewGuid().ToString("N");
        var databaseConnectionString =
            $"Host=localhost;Port=5433;Database={databaseName};Username=postgres;Password=postgres";

        try
        {
            await CreateDatabaseAsync(databaseName);
            await SeedFixtureAsync(databaseConnectionString, Path.Combine(repoRoot, "tests", "BlazorCanvas.Tests", "Fixtures", "v1.1-pre-rewrite.sql"));

            await using (var context = CreateContext(databaseConnectionString))
            {
                await context.Database.MigrateAsync();
            }

            await AssertMigratedRowsAsync(
                databaseConnectionString,
                Path.Combine(repoRoot, "tests", "BlazorCanvas.Tests", "Fixtures", "v1.1-pre-rewrite-MANIFEST.md"));
        }
        finally
        {
            await DropDatabaseAsync(databaseName);
        }
    }

    private static CanvasDbContext CreateContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder<CanvasDbContext>()
            .UseNpgsql(connectionString)
            .Options;
        return new CanvasDbContext(options);
    }

    private static async Task CreateDatabaseAsync(string databaseName)
    {
        await using var conn = new NpgsqlConnection(AdminConnectionString);
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand($"""CREATE DATABASE "{databaseName}";""", conn);
        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task DropDatabaseAsync(string databaseName)
    {
        await using var conn = new NpgsqlConnection(AdminConnectionString);
        await conn.OpenAsync();

        await using (var terminate = new NpgsqlCommand(
            """
            SELECT pg_terminate_backend(pid)
            FROM pg_stat_activity
            WHERE datname = @databaseName
              AND pid <> pg_backend_pid();
            """,
            conn))
        {
            terminate.Parameters.AddWithValue("databaseName", databaseName);
            await terminate.ExecuteNonQueryAsync();
        }

        await using var drop = new NpgsqlCommand($"""DROP DATABASE IF EXISTS "{databaseName}";""", conn);
        await drop.ExecuteNonQueryAsync();
    }

    private static async Task SeedFixtureAsync(string connectionString, string fixturePath)
    {
        var sql = string.Join(
            Environment.NewLine,
            File.ReadLines(fixturePath).Where(line => !line.StartsWith('\\')));

        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand(sql, conn)
        {
            CommandTimeout = 120,
        };
        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task AssertMigratedRowsAsync(string connectionString, string manifestPath)
    {
        var expected = FixtureManifest.Load(manifestPath);
        Assert.NotEmpty(expected);
        Assert.Equal(new[] { "circle", "line", "rectangle", "triangle" }, expected.Select(r => r.Type).Distinct().Order());
        Assert.Contains(expected, r => r.Type == "line" && r.Geometry["dx"] > 0 && r.Geometry["dy"] > 0);

        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync();

        await using (var history = new NpgsqlCommand(
            """SELECT count(*) FROM "__EFMigrationsHistory" WHERE "MigrationId" LIKE '%AnchorGeometryRewrite';""",
            conn))
        {
            Assert.Equal(1L, (long)(await history.ExecuteScalarAsync())!);
        }

        var actual = new List<MigratedFigure>();
        await using (var cmd = new NpgsqlCommand(
            "SELECT id, type, x, y, geometry::text, z FROM figures ORDER BY z, id",
            conn))
        await using (var reader = await cmd.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                actual.Add(new MigratedFigure(
                    reader.GetGuid(0),
                    reader.GetString(1),
                    reader.GetInt32(2),
                    reader.GetInt32(3),
                    ParseGeometry(reader.GetString(4)),
                    reader.GetDecimal(5)));
            }
        }

        Assert.Equal(expected.Count, actual.Count);

        for (var i = 0; i < expected.Count; i++)
        {
            var e = expected[i];
            var a = actual[i];
            Assert.NotEqual(Guid.Empty, a.Id);
            Assert.Equal(e.Type, a.Type);
            Assert.Equal(e.X, a.X);
            Assert.Equal(e.Y, a.Y);
            Assert.Equal(e.Geometry, a.Geometry);
            Assert.Equal(e.Z, a.Z);

            if (e.Type == "circle")
            {
                var r = (e.OldX2 - e.OldX1) / 2;
                Assert.Equal(0, (e.OldX2 - e.OldX1) % 2);
                Assert.Equal(r, e.Geometry["r"]);
                Assert.Equal(e.OldX1 + r, a.X);
                Assert.Equal(e.OldY1 + r, a.Y);
            }
        }
    }

    private static IReadOnlyDictionary<string, int> ParseGeometry(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.EnumerateObject()
            .ToDictionary(p => p.Name, p => p.Value.GetInt32(), StringComparer.Ordinal);
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "docker-compose.yml")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName
            ?? throw new InvalidOperationException("Could not locate docker-compose.yml above " + AppContext.BaseDirectory);
    }

    private sealed record MigratedFigure(
        Guid Id,
        string Type,
        int X,
        int Y,
        IReadOnlyDictionary<string, int> Geometry,
        decimal Z);
}
