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

    [Theory]
    [MemberData(nameof(Matrix))]
    public async Task GuardVerdict_MatchesDatabaseVerdict(FigureType type, Box box, string label)
    {
        var guardSaysDrawable = MinSizeGuard.IsDrawable(type, box);
        var attempt = await _fixture.TryInsertFigureAsync(type, box);

        Assert.True(
            guardSaysDrawable == attempt.Succeeded,
            $"D-50 disagreement: type={type}, box={box} ({label}). " +
            $"MinSizeGuard.IsDrawable => {guardSaysDrawable}; database INSERT => " +
            $"{(attempt.Succeeded ? "succeeded" : $"rejected ({attempt.Error?.ConstraintName})")}. " +
            "Resolve in favour of CONSTRAINT-schema — the DDL is authoritative over both.");
    }

    [Fact]
    public void HorizontalLine_AndZeroHeightRectangle_SameBoxOppositeVerdicts()
    {
        // The decisive pair (D-50 vs D-23, retracted): a SHARED guard cannot accept the line
        // while also rejecting the rectangle for the exact same box.
        Assert.True(MinSizeGuard.IsDrawable(FigureType.Line, ZeroHeight));
        Assert.False(MinSizeGuard.IsDrawable(FigureType.Rectangle, ZeroHeight));
    }

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
        var insertedIds = new List<int>();

        await using (var context = _fixture.CreateContext())
        {
            userId = await DatabaseFixture.CreateTestUserAsync(context);
            var figures = new[]
            {
                new Figure { UserId = userId, Type = "rectangle", X1 = 10, Y1 = 10, X2 = 20, Y2 = 20 },
                new Figure { UserId = userId, Type = "rectangle", X1 = 30, Y1 = 30, X2 = 40, Y2 = 40 },
                new Figure { UserId = userId, Type = "rectangle", X1 = 50, Y1 = 50, X2 = 60, Y2 = 60 },
            };
            context.Figures.AddRange(figures);
            await context.SaveChangesAsync();
            insertedIds.AddRange(figures.Select(f => f.Id));
        }

        Assert.Equal(3, insertedIds.Count);
        Assert.True(
            insertedIds[0] < insertedIds[1] && insertedIds[1] < insertedIds[2],
            "Expected ids to be assigned in insertion order (D-39 — the sequential id is the z-order).");

        // The shell action: tear the container down WITHOUT -v (the named volume surviving is
        // exactly the property under test — CONSTRAINT-env, T-BC01-13), then bring it back up.
        await RunDockerComposeAsync(repoRoot, "down");
        await RunDockerComposeAsync(repoRoot, "up", "-d", "--wait");
        NpgsqlConnection.ClearAllPools();
        await WaitForDatabaseAsync();

        await using var verifyContext = _fixture.CreateContext();
        var survivingIds = await verifyContext.Figures
            .Where(f => f.UserId == userId)
            .OrderBy(f => f.Id)
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
