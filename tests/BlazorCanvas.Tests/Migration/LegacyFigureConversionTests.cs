using BlazorCanvas.Data.V11;
using BlazorCanvas.Shapes;

namespace BlazorCanvas.Tests.Migration;

public class LegacyFigureConversionTests
{
    private static readonly ShapeRegistry Registry = DefaultShapes.CreateRegistry();

    // These literal values are the committed fixture contract, transcribed rather than recomputed:
    // a test that repeats the converter formula proves only that the code agrees with itself.
    public static IEnumerable<object[]> ManifestRows()
    {
        yield return Case(3860, "line", 100, 100, 300, 260, 100m, 100m, [new(0, 0), new(200, 160)], 0, null, 0, 0, 200, 160);
        yield return Case(3861, "line", 400, 300, 600, 140, 400m, 140m, [new(0, 160), new(200, 0)], 0, null, 0, 0, 200, 160);
        yield return Case(3862, "line", 100, 400, 400, 400, 100m, 400m, [new(0, 0), new(300, 0)], 0, null, 0, 0, 300, 0);
        yield return Case(3863, "line", 700, 100, 700, 400, 700m, 100m, [new(0, 0), new(0, 300)], 0, null, 0, 0, 0, 300);
        yield return Case(3864, "rectangle", 200, 200, 500, 380, 200m, 200m, null, 300, 180, 0, 0, 300, 180);
        yield return Case(3865, "circle", 300, 250, 500, 450, 300m, 250m, null, 100, null, 0, 0, 200, 200);
        yield return Case(3866, "triangle", 250, 300, 450, 500, 250m, 300m, [new(100, 0), new(0, 200), new(200, 200)], 0, null, 0, 0, 200, 200);
        yield return Case(3867, "rectangle", 280, 230, 520, 470, 280m, 230m, null, 240, 240, 0, 0, 240, 240);
    }

    [Theory]
    [MemberData(nameof(ManifestRows))]
    public void Convert_ManifestRows_MatchesPositionGeometryAndLocalBbox(ManifestCase row)
    {
        var result = LegacyFigureConversion.Convert(row.Type, row.X1, row.Y1, row.X2, row.Y2);

        Assert.Equal(row.X, result.X);
        Assert.Equal(row.Y, result.Y);
        AssertGeometry(row, result.Geometry);
        Assert.Equal(row.Bounds, Registry.Get(row.Type).BoundsOf(result.Geometry));
    }

    [Fact]
    public void Convert_Diagonals_PreserveOriginalPointOrder()
    {
        // D-41's normalisation landmine remains defused by D-60: point order carries the diagonal.
        var upAndRight = Assert.IsType<LineGeometry>(LegacyFigureConversion.Convert("line", 400, 300, 600, 140).Geometry);
        var downAndRight = Assert.IsType<LineGeometry>(LegacyFigureConversion.Convert("line", 100, 100, 300, 260).Geometry);

        Assert.Equal(new[] { new LocalPoint(0, 160), new LocalPoint(200, 0) }, upAndRight.Points);
        Assert.True(upAndRight.Points[1].Y < upAndRight.Points[0].Y);
        Assert.Equal(new[] { new LocalPoint(0, 0), new LocalPoint(200, 160) }, downAndRight.Points);
        Assert.True(downAndRight.Points[1].Y > downAndRight.Points[0].Y);
    }

    [Fact]
    public void Convert_DegenerateButLegalLines_RemainDrawable()
    {
        // Both were legal under v1.1 line_is_a_line; rejecting either would make the row un-migratable.
        var horizontal = Assert.IsType<LineGeometry>(LegacyFigureConversion.Convert("line", 100, 400, 400, 400).Geometry);
        var vertical = Assert.IsType<LineGeometry>(LegacyFigureConversion.Convert("line", 700, 100, 700, 400).Geometry);
        var line = Registry.Get("line");

        Assert.Equal(0, line.BoundsOf(horizontal).H);
        Assert.Equal(0, line.BoundsOf(vertical).W);
        Assert.True(line.IsDrawable(horizontal));
        Assert.True(line.IsDrawable(vertical));
    }

    [Fact]
    public void Convert_OffCanvasRectangle_DoesNotClamp()
    {
        var result = LegacyFigureConversion.Convert("rectangle", 1400, 20, 1600, 40);
        var rectangle = Assert.IsType<RectangleGeometry>(result.Geometry);

        Assert.Equal(1400m, result.X);
        Assert.Equal(200, rectangle.W);
        Assert.Equal(20, rectangle.H);
    }

    [Fact]
    public void Convert_MinimalRectangle_DoesNotRejectOrGrow()
    {
        var rectangle = Assert.IsType<RectangleGeometry>(LegacyFigureConversion.Convert("rectangle", 1, 2, 2, 3).Geometry);
        Assert.Equal(1, rectangle.W);
        Assert.Equal(1, rectangle.H);
    }

    [Fact]
    public void Convert_IdenticalRows_ReturnIndependentButEqualResults()
    {
        var first = LegacyFigureConversion.Convert("rectangle", 10, 20, 30, 40);
        var second = LegacyFigureConversion.Convert("rectangle", 10, 20, 30, 40);

        Assert.Equal(first, second);
        Assert.NotSame(first.Geometry, second.Geometry);
    }

    [Fact]
    public void Convert_MinimalCircle_DoesNotRoundItsRadius()
    {
        var circle = Assert.IsType<CircleGeometry>(LegacyFigureConversion.Convert("circle", 5, 7, 7, 9).Geometry);
        Assert.Equal(1, circle.R);
    }

    [Fact]
    public void Convert_RightToLeftLine_ThrowsInsteadOfSwappingEndpoints() =>
        Assert.Throws<InvalidOperationException>(() => LegacyFigureConversion.Convert("line", 10, 20, 9, 30));

    public static IEnumerable<object?[]> UnknownTypes()
    {
        yield return ["pentagon"];
        yield return ["Circle"];
        yield return [""];
        yield return ["   "];
        yield return [null];
    }

    [Theory]
    [MemberData(nameof(UnknownTypes))]
    public void Convert_UnknownType_ThrowsWithoutEchoingCoordinates(string? type)
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => LegacyFigureConversion.Convert(type!, 123, 456, 789, 987));
        AssertNoCoordinates(exception, 123, 456, 789, 987);
    }

    public static IEnumerable<object[]> ImpossibleRows()
    {
        yield return ["rectangle", 1, 2, 1, 3];
        yield return ["triangle", 1, 4, 3, 3];
        yield return ["circle", 10, 20, 13, 23];
        yield return ["circle", 10, 20, 14, 22];
        yield return ["line", 15, 25, 15, 25];
    }

    [Theory]
    [MemberData(nameof(ImpossibleRows))]
    public void Convert_ImpossibleCoordinates_ThrowsWithoutEchoingCoordinates(string type, int x1, int y1, int x2, int y2)
    {
        var exception = Assert.Throws<InvalidOperationException>(() => LegacyFigureConversion.Convert(type, x1, y1, x2, y2));
        AssertNoCoordinates(exception, x1, y1, x2, y2);
    }

    public static IEnumerable<object[]> ValidLegacyRows()
    {
        // This is the compatibility floor: each case models a v1.1-legal row that must migrate.
        for (var i = 0; i < 20; i++)
        {
            var x = i * 30;
            var y = i * 20;
            var width = (i % 5) + 1;
            var height = (i % 7) + 1;
            yield return ["rectangle", x, y, x + width, y + height];
            yield return ["triangle", x, y, x + width, y + height];

            var side = ((i % 6) + 1) * 2;
            yield return ["circle", x, y, x + side, y + side];

            var lineX2 = x + (i % 5);
            var lineY2 = i % 3 == 0 ? y + 1 : y + (i % 2 == 0 ? -(i % 7) - 1 : (i % 7) + 1);
            yield return ["line", x, y, lineX2, lineY2];
        }
    }

    [Theory]
    [MemberData(nameof(ValidLegacyRows))]
    public void Convert_EveryV11LegalShapeMix_IsDrawableAndHasFiniteNonNegativeBounds(string type, int x1, int y1, int x2, int y2)
    {
        var geometry = LegacyFigureConversion.Convert(type, x1, y1, x2, y2).Geometry;
        var definition = Registry.Get(type);
        var bounds = definition.BoundsOf(geometry);

        Assert.True(definition.IsDrawable(geometry));
        Assert.True(double.IsFinite(bounds.W));
        Assert.True(double.IsFinite(bounds.H));
        Assert.True(bounds.W >= 0);
        Assert.True(bounds.H >= 0);
    }

    private static object[] Case(int id, string type, int x1, int y1, int x2, int y2, decimal x, decimal y, IReadOnlyList<LocalPoint>? points, double primary, double? secondary, double bboxX, double bboxY, double bboxW, double bboxH) =>
        [new ManifestCase(id, type, x1, y1, x2, y2, x, y, points, primary, secondary, new Bbox(bboxX, bboxY, bboxW, bboxH))];

    private static void AssertGeometry(ManifestCase row, IFigureGeometry geometry)
    {
        switch (geometry)
        {
            case LineGeometry line:
                Assert.Equal(row.Points, line.Points);
                break;
            case TriangleGeometry triangle:
                Assert.Equal(row.Points, triangle.Points);
                break;
            case RectangleGeometry rectangle:
                Assert.Equal(row.Primary, rectangle.W);
                Assert.Equal(row.Secondary, rectangle.H);
                break;
            case CircleGeometry circle:
                Assert.Equal(row.Primary, circle.R);
                break;
            default:
                throw new Xunit.Sdk.XunitException("Unexpected geometry type.");
        }
    }

    private static void AssertNoCoordinates(Exception exception, params int[] coordinates)
    {
        foreach (var coordinate in coordinates)
        {
            Assert.DoesNotContain(coordinate.ToString(), exception.Message, StringComparison.Ordinal);
        }
    }

    public sealed record ManifestCase(
        int Id,
        string Type,
        int X1,
        int Y1,
        int X2,
        int Y2,
        decimal X,
        decimal Y,
        IReadOnlyList<LocalPoint>? Points,
        double Primary,
        double? Secondary,
        Bbox Bounds);
}
