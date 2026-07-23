using System.Diagnostics;
using BlazorCanvas.Data;
using BlazorCanvas.Geometry;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace BlazorCanvas.Tests.Database;

/// <summary>
/// The decisive test of the phase. D-50 says the per-type min-size guard mirrors the three
/// CHECK constraints EXACTLY, so the app can never write a row the database would refuse. Plan
/// 01-02 built the guard; plan 01-03 built the CHECKs; this is the only place the two halves
/// are run against each other over a shared matrix. If they disagree, <c>CONSTRAINT-schema</c>
/// — the DDL — is authoritative over both, and the defect is in the guard or the DbContext,
/// never here.
/// </summary>
[Collection("Database")]
public class GuardMirrorsChecksTests
{
    private readonly DatabaseFixture _fixture;

    public GuardMirrorsChecksTests(DatabaseFixture fixture) => _fixture = fixture;

    // Eight boundary-probing boxes, crossed with all four FigureType members below (32 cases —
    // over the 24-case / 6-shape minimum). The SAME box tested against DIFFERENT types is
    // deliberate: it is what proves the guard is per-type (D-50), not shared (D-23, retracted).
    public static readonly Box WellFormed = new(0, 0, 10, 10); // square, even side, positive both dims
    public static readonly Box ZeroHeight = new(10, 10, 90, 10); // legal (horizontal) line, illegal box/circle
    public static readonly Box ZeroWidth = new(10, 10, 10, 90); // legal (vertical) line, illegal box/circle
    public static readonly Box ZeroArea = new(10, 10, 10, 10); // illegal for every type
    public static readonly Box OddSquare = new(0, 0, 9, 9); // illegal circle only (odd side)
    public static readonly Box NonSquare = new(0, 0, 10, 8); // illegal circle only (not square)
    public static readonly Box Unnormalised = new(10, 10, 0, 0); // x2 < x1 — illegal for every type
    public static readonly Box DiagonalUpRight = new(0, 100, 100, 0); // legal line, illegal box/circle (y2 < y1)

    public static IEnumerable<object[]> Matrix()
    {
        var boxes = new (Box Box, string Label)[]
        {
            (WellFormed, "well-formed"),
            (ZeroHeight, "zero-height"),
            (ZeroWidth, "zero-width"),
            (ZeroArea, "zero-area"),
            (OddSquare, "odd-sided square"),
            (NonSquare, "non-square"),
            (Unnormalised, "unnormalised (x2 < x1)"),
            (DiagonalUpRight, "up-and-right diagonal (y2 < y1)"),
        };

        foreach (var type in Enum.GetValues<FigureType>())
        {
            foreach (var (box, label) in boxes)
            {
                yield return new object[] { type, box, label };
            }
        }
    }

    // D-59 removes the geometry CHECKs this matrix mirrored. Phase 10/STOR-03 re-expresses the
    // per-type guard proof code-side, where geometry JSON is constructed.

    [Theory]
    [InlineData(FigureType.Line)]
    [InlineData(FigureType.Rectangle)]
    [InlineData(FigureType.Circle)]
    [InlineData(FigureType.Triangle)]
    public async Task TypeLiteral_RoundTrips_AgainstFiguresTypeIsKnown(FigureType type)
    {
        // D-46: the four literals FigureTypeNames.ToDbValue produces are exactly the four the
        // figures_type_is_known CHECK accepts. WellFormed is legal for every type, so a
        // rejection here can only mean the literal itself was refused.
        var attempt = await _fixture.TryInsertFigureAsync(type, WellFormed);

        Assert.True(
            attempt.Succeeded,
            $"FigureTypeNames.ToDbValue({type}) = '{FigureTypeNames.ToDbValue(type)}' was rejected " +
            $"by figures_type_is_known: {attempt.Error?.ConstraintName} ({attempt.Error?.MessageText}).");
    }

    /// <summary>
    /// ROADMAP success criterion 1, re-proven with real EF-written data (D-27, D-39): figures
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
