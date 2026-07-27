using Npgsql;

namespace BlazorCanvas.Tests.Database;

/// <summary>
/// This file no longer proves "the database refuses an illegal geometry" — D-59 removed those
/// CHECKs, and STOR-03 moved geometry well-formedness entirely into code (MinSizeGuard). What it
/// still proves: the retained <c>figures_type_is_known</c> whitelist still discriminates a
/// legitimate type literal from an unknown or miscased one, and that well-formed anchor+geometry
/// rows insert. Every test constructs a Figure with literal anchor + geometry JSON (via
/// <see cref="DatabaseFixture.TryInsertRawFigureAsync"/>) and calls SaveChanges directly — never
/// routing through MinSizeGuard or Normalisation.
/// </summary>
[Collection("Database")]
public class CheckConstraintTests
{
    private readonly DatabaseFixture _fixture;

    public CheckConstraintTests(DatabaseFixture fixture) => _fixture = fixture;

    public static IEnumerable<object[]> RejectedCases()
    {
        // D-59 removes geometry CHECKs; Phase 10/STOR-03 re-expresses geometry guards in code.
        // The type whitelist CHECK is deliberately retained through and after the backfill
        // (D-59 item 8).
        yield return new object[] { "oval", 0, 0, """{"w":10,"h":10}""", "figures_type_is_known", "unknown type literal" };
        yield return new object[] { "Circle", 0, 0, """{"r":10}""", "figures_type_is_known", "PascalCase type literal (accidental Enum.ToString())" };
    }

    [Theory]
    [MemberData(nameof(RejectedCases))]
    public async Task IllegalRow_IsRejectedByNamedConstraint(
        string type, int x, int y, string geometry, string expectedConstraint, string caseLabel)
    {
        var attempt = await _fixture.TryInsertRawFigureAsync(type, x, y, geometry);

        Assert.False(
            attempt.Succeeded,
            $"[{caseLabel}] expected the database to REJECT ('{type}', {x},{y}, {geometry}), but the INSERT succeeded.");
        Assert.NotNull(attempt.Error);
        Assert.Equal(PostgresErrorCodes.CheckViolation, attempt.Error!.SqlState);
        Assert.Equal(expectedConstraint, attempt.Error.ConstraintName);
    }

    public static IEnumerable<object[]> AcceptedLineCases()
    {
        // A rejected draw is silent (D-50), but these are LEGAL figures — a test that rejects
        // them has misread D-50.
        yield return new object[] { 10, 10, """{"dx":80,"dy":0}""", "horizontal line (zero height)" };
        yield return new object[] { 10, 10, """{"dx":0,"dy":80}""", "vertical line (zero width)" };
        yield return new object[] { 0, 100, """{"dx":100,"dy":-100}""", "up-and-right diagonal (legal for a line)" };
    }

    [Theory]
    [MemberData(nameof(AcceptedLineCases))]
    public async Task LegalLine_IsAccepted(int x, int y, string geometry, string caseLabel)
    {
        var attempt = await _fixture.TryInsertRawFigureAsync("line", x, y, geometry);

        Assert.True(
            attempt.Succeeded,
            $"[{caseLabel}] expected ('line', {x},{y}, {geometry}) to INSERT successfully, " +
            $"but it was rejected by {attempt.Error?.ConstraintName} ({attempt.Error?.MessageText}).");
    }

    public static IEnumerable<object[]> ValidFigureCases()
    {
        yield return new object[] { "circle", 5, 5, """{"r":5}""", "well-formed circle" };
        yield return new object[] { "rectangle", 10, 10, """{"w":80,"h":80}""", "well-formed rectangle" };
        yield return new object[] { "triangle", 10, 10, """{"w":80,"h":80}""", "well-formed triangle" };
    }

    [Theory]
    [MemberData(nameof(ValidFigureCases))]
    public async Task WellFormedFigure_InsertsSuccessfully(
        string type, int x, int y, string geometry, string caseLabel)
    {
        // Proves the CHECKs discriminate rather than blanket-refusing every row.
        var attempt = await _fixture.TryInsertRawFigureAsync(type, x, y, geometry);

        Assert.True(
            attempt.Succeeded,
            $"[{caseLabel}] expected ('{type}', {x},{y}, {geometry}) to INSERT successfully, " +
            $"but it was rejected by {attempt.Error?.ConstraintName} ({attempt.Error?.MessageText}).");
    }
}
