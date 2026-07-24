using System.Diagnostics;
using BlazorCanvas.Data;
using BlazorCanvas.Geometry;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace BlazorCanvas.Tests.Database;

/// <summary>
/// Formerly "the decisive test of the phase" (Phase 1): D-50's per-type min-size guard was run
/// against three database CHECK constraints over a shared matrix. D-59 deleted those CHECKs, and
/// STOR-03 moved the per-type guard proof entirely code-side, into
/// <see cref="Geometry.MinSizeGuard"/> — see <c>MinSizeGuardTests</c>. What survives here, and
/// what this file actually proves:
/// (1) the four literals <see cref="FigureTypeNames.ToDbValue"/> produces are exactly the four
/// the retained <c>figures_type_is_known</c> CHECK accepts (D-46), and
/// (2) figures written through the DbContext survive a full Docker Compose container teardown of
/// the NAMED volume (D-27) — the only place in the suite proving volume persistence.
/// </summary>
[Collection("Database")]
public class TypeWhitelistAndPersistenceTests
{
    private readonly DatabaseFixture _fixture;

    public TypeWhitelistAndPersistenceTests(DatabaseFixture fixture) => _fixture = fixture;

    [Theory]
    [InlineData(FigureType.Line)]
    [InlineData(FigureType.Rectangle)]
    [InlineData(FigureType.Circle)]
    [InlineData(FigureType.Triangle)]
    public async Task TypeLiteral_RoundTrips_AgainstFiguresTypeIsKnown(FigureType type)
    {
        // D-46: the four literals FigureTypeNames.ToDbValue produces are exactly the four the
        // figures_type_is_known CHECK accepts. The geometry content is irrelevant — D-59 left no
        // CHECK on geometry — so a rejection here can only mean the type literal itself was
        // refused.
        var attempt = await _fixture.TryInsertFigureAsync(type, """{"w":10,"h":10}""");

        Assert.True(
            attempt.Succeeded,
            $"FigureTypeNames.ToDbValue({type}) = '{FigureTypeNames.ToDbValue(type)}' was rejected " +
            $"by figures_type_is_known: {attempt.Error?.ConstraintName} ({attempt.Error?.MessageText}).");
    }

    /// <summary>
    /// ROADMAP success criterion 1, re-proven with real EF-written data (D-27, D-59): figures
    /// written through the DbContext survive a full container teardown of the NAMED volume.
    /// Plan 01-01 proved the volume holds with a scratch table; this proves it holds for the
    /// real <c>figures</c> table. The container down/up is a shell action; the surrounding
    /// insert/verify steps are the actual xUnit assertions.
    /// </summary>
    [Fact]
    public async Task FiguresWrittenViaEfCore_SurviveContainerTeardown()
    {
        var repoRoot = FindRepoRoot();
        int userId;
        var insertedIds = new List<Guid>();

        await using (var context = _fixture.CreateContext())
        {
            userId = await DatabaseFixture.CreateTestUserAsync(context);
            var figures = new[]
            {
                CreateFigure(userId, new Box(10, 10, 20, 20), 1m),
                CreateFigure(userId, new Box(30, 30, 40, 40), 2m),
                CreateFigure(userId, new Box(50, 50, 60, 60), 3m),
            };
            context.Figures.AddRange(figures);
            await context.SaveChangesAsync();
            insertedIds.AddRange(figures.Select(f => f.Id));
        }

        Assert.Equal(3, insertedIds.Count);
        Assert.All(insertedIds, id => Assert.NotEqual(Guid.Empty, id));

        // The shell action: tear the container down WITHOUT -v (the named volume surviving is
        // exactly the property under test — CONSTRAINT-env, T-BC01-13), then bring it back up.
        await RunDockerComposeAsync(repoRoot, "down");
        await RunDockerComposeAsync(repoRoot, "up", "-d", "--wait");
        NpgsqlConnection.ClearAllPools();
        await WaitForDatabaseAsync();

        await using var verifyContext = _fixture.CreateContext();
        var survivingIds = await verifyContext.Figures
            .Where(f => f.UserId == userId)
            .OrderBy(f => f.Z)
            .ThenBy(f => f.Id)
            .Select(f => f.Id)
            .ToListAsync();

        Assert.Equal(insertedIds, survivingIds);

        // Clean up now that persistence-across-restart is proven — cascade delete removes the
        // figures with the user (D-46's FK ON DELETE CASCADE).
        var user = await verifyContext.Users.FindAsync(userId);
        if (user is not null)
        {
            verifyContext.Users.Remove(user);
            await verifyContext.SaveChangesAsync();
        }
    }

    private static Figure CreateFigure(int userId, Box box, decimal z)
    {
        var encoded = GeometryCodec.Encode(FigureType.Rectangle, box);
        return new Figure
        {
            UserId = userId,
            Type = "rectangle",
            X = encoded.X,
            Y = encoded.Y,
            Geometry = encoded.Geometry,
            Z = z,
        };
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "docker-compose.yml")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName
            ?? throw new InvalidOperationException(
                "Could not locate docker-compose.yml above " + AppContext.BaseDirectory);
    }

    private static async Task RunDockerComposeAsync(string workingDirectory, params string[] args)
    {
        var psi = new ProcessStartInfo("docker")
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        psi.ArgumentList.Add("compose");
        foreach (var arg in args)
        {
            psi.ArgumentList.Add(arg);
        }

        using var process = Process.Start(psi)
            ?? throw new InvalidOperationException(
                "Failed to start 'docker compose " + string.Join(' ', args) + "'.");

        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        var stdout = await stdoutTask;
        var stderr = await stderrTask;

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"'docker compose {string.Join(' ', args)}' exited {process.ExitCode}.\n{stdout}\n{stderr}");
        }
    }

    private async Task WaitForDatabaseAsync()
    {
        var deadline = DateTime.UtcNow.AddSeconds(30);
        Exception? last = null;
        while (DateTime.UtcNow < deadline)
        {
            try
            {
                await using var conn = new NpgsqlConnection(_fixture.ConnectionString);
                await conn.OpenAsync();
                return;
            }
            catch (Exception ex)
            {
                last = ex;
                await Task.Delay(1000);
            }
        }

        throw new InvalidOperationException(
            "PostgreSQL did not become reachable within 30s after 'docker compose up -d'.", last);
    }
}
