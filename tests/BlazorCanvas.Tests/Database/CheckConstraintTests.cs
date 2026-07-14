using Npgsql;

namespace BlazorCanvas.Tests.Database;

/// <summary>
/// ROADMAP success criterion 3: the database itself refuses an illegal row. Every test here
/// constructs a Figure with literal coordinates (via
/// <see cref="DatabaseFixture.TryInsertRawFigureAsync"/>) and calls SaveChanges directly —
/// never routing through MinSizeGuard or Normalisation. The rejection under test belongs to
/// PostgreSQL, not to the C# guard: routing through the guard first would prove only that the
/// guard agrees with itself, not that the database enforces anything.
/// </summary>
[Collection("Database")]
public class CheckConstraintTests
{
    private readonly DatabaseFixture _fixture;

    public CheckConstraintTests(DatabaseFixture fixture) => _fixture = fixture;

    public static IEnumerable<object[]> RejectedCases()
    {
        // circle_is_a_circle (D-22): rejects a non-square or odd-sided circle
        yield return new object[] { "circle", 0, 0, 10, 8, "circle_is_a_circle", "non-square circle (would render as an oval)" };
        yield return new object[] { "circle", 0, 0, 9, 9, "circle_is_a_circle", "odd-sided square circle" };
        yield return new object[] { "circle", 10, 10, 10, 10, "circle_is_a_circle", "zero-radius circle" };
        yield return new object[] { "circle", 10, 10, 0, 0, "circle_is_a_circle", "un-normalised / negative-side circle" };

        // box_is_a_box (D-23/D-41): rejects a zero-area rectangle
        yield return new object[] { "rectangle", 10, 10, 90, 10, "box_is_a_box", "zero-height rectangle" };
        yield return new object[] { "rectangle", 10, 10, 10, 90, "box_is_a_box", "zero-width rectangle" };
        yield return new object[] { "rectangle", 10, 10, 10, 10, "box_is_a_box", "zero-area rectangle" };

        // box_is_a_box (D-21): a triangle is derived from the same box as a rectangle
        yield return new object[] { "triangle", 10, 10, 90, 10, "box_is_a_box", "zero-height triangle" };
        yield return new object[] { "triangle", 10, 10, 10, 90, "box_is_a_box", "zero-width triangle" };
        yield return new object[] { "triangle", 10, 10, 10, 10, "box_is_a_box", "zero-area triangle" };

        // line_is_a_line: rejects a zero-length line, and an un-normalised one
        yield return new object[] { "line", 10, 10, 10, 10, "line_is_a_line", "zero-length line" };
        yield return new object[] { "line", 90, 10, 10, 10, "line_is_a_line", "un-normalised line (x2 < x1)" };

        // figures_type_is_known (D-46)
        yield return new object[] { "oval", 0, 0, 10, 10, "figures_type_is_known", "unknown type literal" };
        yield return new object[] { "Circle", 0, 0, 10, 10, "figures_type_is_known", "PascalCase type literal (accidental Enum.ToString())" };
    }

    [Theory]
    [MemberData(nameof(RejectedCases))]
    public async Task IllegalRow_IsRejectedByNamedConstraint(
        string type, int x1, int y1, int x2, int y2, string expectedConstraint, string caseLabel)
    {
        var attempt = await _fixture.TryInsertRawFigureAsync(type, x1, y1, x2, y2);

        Assert.False(
            attempt.Succeeded,
            $"[{caseLabel}] expected the database to REJECT ('{type}', {x1},{y1}, {x2},{y2}), but the INSERT succeeded.");
        Assert.NotNull(attempt.Error);
        Assert.Equal(PostgresErrorCodes.CheckViolation, attempt.Error!.SqlState);
        Assert.Equal(expectedConstraint, attempt.Error.ConstraintName);
    }

    public static IEnumerable<object[]> AcceptedLineCases()
    {
        // A rejected draw is silent (D-50), but these are LEGAL figures — a test that rejects
        // them has misread D-50.
        yield return new object[] { 10, 10, 90, 10, "horizontal line (zero height)" };
        yield return new object[] { 10, 10, 10, 90, "vertical line (zero width)" };
        yield return new object[] { 0, 100, 100, 0, "up-and-right diagonal (y2 < y1 — legal for a line)" };
    }

    [Theory]
    [MemberData(nameof(AcceptedLineCases))]
    public async Task LegalLine_IsAccepted(int x1, int y1, int x2, int y2, string caseLabel)
    {
        var attempt = await _fixture.TryInsertRawFigureAsync("line", x1, y1, x2, y2);

        Assert.True(
            attempt.Succeeded,
            $"[{caseLabel}] expected ('line', {x1},{y1}, {x2},{y2}) to INSERT successfully, " +
            $"but it was rejected by {attempt.Error?.ConstraintName} ({attempt.Error?.MessageText}).");
    }

    public static IEnumerable<object[]> ValidFigureCases()
    {
        yield return new object[] { "circle", 0, 0, 10, 10, "well-formed circle" };
        yield return new object[] { "rectangle", 10, 10, 90, 90, "well-formed rectangle" };
        yield return new object[] { "triangle", 10, 10, 90, 90, "well-formed triangle" };
    }

    [Theory]
    [MemberData(nameof(ValidFigureCases))]
    public async Task WellFormedFigure_InsertsSuccessfully(
        string type, int x1, int y1, int x2, int y2, string caseLabel)
    {
        // Proves the CHECKs discriminate rather than blanket-refusing every row.
        var attempt = await _fixture.TryInsertRawFigureAsync(type, x1, y1, x2, y2);

        Assert.True(
            attempt.Succeeded,
            $"[{caseLabel}] expected ('{type}', {x1},{y1}, {x2},{y2}) to INSERT successfully, " +
            $"but it was rejected by {attempt.Error?.ConstraintName} ({attempt.Error?.MessageText}).");
    }
}
