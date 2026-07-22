using System.Text.Json;
using BlazorCanvas.Shapes;

namespace BlazorCanvas.Tests.Shapes;

public class Star5ShapeTests
{
    private readonly Star5Shape _subject = new();

    public static IEnumerable<object[]> InvalidJson()
    {
        yield return new object[] { "{}" };
        yield return new object[] { "{\"points\":[[0,0],[1,1],[2,2],[3,3],[4,4],[5,5],[6,6],[7,7],[8,8],[9,9]]}" };
        yield return new object[] { "{\"points\":[],\"innerRatio\":0.382}" };
        yield return new object[] { "{\"points\":[[0,0]],\"innerRatio\":0.382}" };
        yield return new object[] { "{\"points\":[[0,0],[1,1],[2,2],[3,3],[4,4],[5,5],[6,6],[7,7],[8,8],[9,9],[10,10]],\"innerRatio\":0.382}" };
        yield return new object[] { "{\"points\":\"nope\",\"innerRatio\":0.382}" };
        yield return new object[] { "{\"points\":[[0],[1,1],[2,2],[3,3],[4,4],[5,5],[6,6],[7,7],[8,8],[9,9]],\"innerRatio\":0.382}" };
        yield return new object[] { "{\"points\":[[0,\"a\"],[1,1],[2,2],[3,3],[4,4],[5,5],[6,6],[7,7],[8,8],[9,9]],\"innerRatio\":0.382}" };
        yield return new object[] { "{\"points\":[[1e400,0],[1,1],[2,2],[3,3],[4,4],[5,5],[6,6],[7,7],[8,8],[9,9]],\"innerRatio\":0.382}" };
        yield return new object[] { "{\"points\":[[0,0],[1,1],[2,2],[3,3],[4,4],[5,5],[6,6],[7,7],[8,8],[9,9]],\"innerRatio\":\"0.382\"}" };
        yield return new object[] { "{\"points\":[[0,0],[1,1],[2,2],[3,3],[4,4],[5,5],[6,6],[7,7],[8,8],[9,9]],\"innerRatio\":0}" };
        yield return new object[] { "{\"points\":[[0,0],[1,1],[2,2],[3,3],[4,4],[5,5],[6,6],[7,7],[8,8],[9,9]],\"innerRatio\":-0.382}" };
        yield return new object[] { "{\"points\":[[0,0],[1,1],[2,2],[3,3],[4,4],[5,5],[6,6],[7,7],[8,8],[9,9]],\"innerRatio\":1e400}" };
        yield return new object[] { "[]" };
        yield return new object[] { "1" };
    }

    [Theory]
    [MemberData(nameof(InvalidJson))]
    public void TryParseGeometry_InvalidShape_ReturnsFalseWithNullGeometry(string raw)
    {
        using var document = JsonDocument.Parse(raw);
        Assert.False(_subject.TryParseGeometry(document.RootElement, out var geometry));
        Assert.Null(geometry);
    }

    [Fact]
    public void TryParseGeometry_OrderedPointsAndPositiveRatio_PreservesInput()
    {
        const string json = "{\"points\":[[50,0],[57.64,30.9],[100,50],[57.64,69.1],[50,100],[42.36,69.1],[0,50],[42.36,30.9],[50,0.5],[51,30]],\"innerRatio\":0.5}";
        using var document = JsonDocument.Parse(json);

        Assert.True(_subject.TryParseGeometry(document.RootElement, out var geometry));

        var star = Assert.IsType<Star5Geometry>(geometry);
        Assert.Equal(0.5, star.InnerRatio);
        Assert.Equal(
            new[]
            {
                new LocalPoint(50, 0),
                new LocalPoint(57.64, 30.9),
                new LocalPoint(100, 50),
                new LocalPoint(57.64, 69.1),
                new LocalPoint(50, 100),
                new LocalPoint(42.36, 69.1),
                new LocalPoint(0, 50),
                new LocalPoint(42.36, 30.9),
                new LocalPoint(50, 0.5),
                new LocalPoint(51, 30)
            },
            star.Points);
    }

    [Fact]
    public void ToJson_ValidGeometry_RoundTripsCanonicalJsonByteForByte()
    {
        const string json = "{\"points\":[[50,0],[57.64,30.9],[100,50],[57.64,69.1],[50,100],[42.36,69.1],[0,50],[42.36,30.9],[50,0.5],[51,30]],\"innerRatio\":0.5}";
        using var document = JsonDocument.Parse(json);
        Assert.True(_subject.TryParseGeometry(document.RootElement, out var parsed));

        Assert.Equal(json, _subject.ToJson(parsed));
    }

    [Fact]
    public void BoundsOf_UsesPointListOnly()
    {
        var points = new[]
        {
            new LocalPoint(50, 0),
            new LocalPoint(60, 30),
            new LocalPoint(100, 50),
            new LocalPoint(60, 70),
            new LocalPoint(50, 100),
            new LocalPoint(40, 70),
            new LocalPoint(0, 50),
            new LocalPoint(40, 30),
            new LocalPoint(50, 10),
            new LocalPoint(55, 30)
        };

        var original = _subject.BoundsOf(new Star5Geometry(points, 0.382));
        var changedRatio = _subject.BoundsOf(new Star5Geometry(points, 0.9));

        Assert.Equal(new Bbox(0, 0, 100, 100), original);
        Assert.Equal(original, changedRatio);
    }

    [Fact]
    public void FromGesture_CornerToCorner_CreatesPointUpTenPointStretchableStar()
    {
        var placement = _subject.FromGesture(new CanvasPoint(10, 20), new CanvasPoint(210, 120));

        Assert.Equal(10, placement.X);
        Assert.Equal(20, placement.Y);
        var star = Assert.IsType<Star5Geometry>(placement.Geometry);
        Assert.Equal(Star5Shape.DefaultInnerRatio, star.InnerRatio);
        AssertStarPoints(ExpectedStarPoints(200, 100, Star5Shape.DefaultInnerRatio), star.Points);
        Assert.Equal(new Bbox(0, 0, 200, 100), _subject.BoundsOf(star));
    }

    [Fact]
    public void FromGesture_ReversedCorners_ProducesSamePlacementAndPoints()
    {
        var downRight = _subject.FromGesture(new CanvasPoint(10, 20), new CanvasPoint(210, 120));
        var upLeft = _subject.FromGesture(new CanvasPoint(210, 120), new CanvasPoint(10, 20));

        Assert.Equal(downRight.X, upLeft.X);
        Assert.Equal(downRight.Y, upLeft.Y);
        AssertStarPoints(
            Assert.IsType<Star5Geometry>(downRight.Geometry).Points,
            Assert.IsType<Star5Geometry>(upLeft.Geometry).Points);
    }

    [Fact]
    public void FromGesture_OutOfBoundsPoints_RoundsAwayFromZeroAndClampsToInclusiveCanvasBounds()
    {
        var placement = _subject.FromGesture(new CanvasPoint(-10.5, -10.5), new CanvasPoint(1471.5, 827.5));

        Assert.Equal(0, placement.X);
        Assert.Equal(0, placement.Y);
        var star = Assert.IsType<Star5Geometry>(placement.Geometry);
        AssertStarPoints(ExpectedStarPoints(1472, 828, Star5Shape.DefaultInnerRatio), star.Points);
        Assert.Equal(new Bbox(0, 0, 1472, 828), _subject.BoundsOf(star));
    }

    [Fact]
    public void IsDrawable_RequiresTenFiniteDistinctNonZeroAreaPointsWithPositiveExtents()
    {
        Assert.True(_subject.IsDrawable(new Star5Geometry(ExpectedStarPoints(100, 80, Star5Shape.DefaultInnerRatio), Star5Shape.DefaultInnerRatio)));
        Assert.False(_subject.IsDrawable(new Star5Geometry(ExpectedStarPoints(0, 80, Star5Shape.DefaultInnerRatio), Star5Shape.DefaultInnerRatio)));
        Assert.False(_subject.IsDrawable(new Star5Geometry(ExpectedStarPoints(100, 0, Star5Shape.DefaultInnerRatio), Star5Shape.DefaultInnerRatio)));
        Assert.False(_subject.IsDrawable(new Star5Geometry(Enumerable.Repeat(new LocalPoint(1, 1), 10).ToArray(), Star5Shape.DefaultInnerRatio)));
        Assert.False(_subject.IsDrawable(new Star5Geometry([.. ExpectedStarPoints(100, 80, Star5Shape.DefaultInnerRatio).Take(9), new LocalPoint(double.PositiveInfinity, 1)], Star5Shape.DefaultInnerRatio)));
    }

    [Fact]
    public void IsDrawable_TEST04RejectsExactZeroExtentsAndAcceptsOneUnitSliver()
    {
        // TEST-04 / D-70 / D-71: the unit boundary matches the gateway's zero-extent threshold.
        Assert.False(_subject.IsDrawable(new Star5Geometry(ExpectedStarPoints(0, 80, Star5Shape.DefaultInnerRatio), Star5Shape.DefaultInnerRatio)));
        Assert.False(_subject.IsDrawable(new Star5Geometry(ExpectedStarPoints(100, 0, Star5Shape.DefaultInnerRatio), Star5Shape.DefaultInnerRatio)));
        Assert.True(_subject.IsDrawable(new Star5Geometry(ExpectedStarPoints(1, 80, Star5Shape.DefaultInnerRatio), Star5Shape.DefaultInnerRatio)));
    }

    [Fact]
    public void DefaultRegistry_ContainsStar5AfterTriangle()
    {
        Assert.Equal(new[] { "line", "rectangle", "circle", "triangle", "star5" }, DefaultShapes.CreateRegistry().Names);
    }

    private static IReadOnlyList<LocalPoint> ExpectedStarPoints(double width, double height, double innerRatio)
    {
        var radiusX = width / 2;
        var radiusY = height / 2;

        var rawPoints = Enumerable.Range(0, 10)
            .Select(index =>
            {
                var theta = (-Math.PI / 2) + (index * (Math.PI / 5));
                var scale = index % 2 == 0 ? 1.0 : innerRatio;
                return new LocalPoint(
                    radiusX + (radiusX * scale * Math.Cos(theta)),
                    radiusY + (radiusY * scale * Math.Sin(theta)));
            })
            .ToArray();
        var minX = rawPoints.Min(point => point.X);
        var minY = rawPoints.Min(point => point.Y);
        var rawWidth = rawPoints.Max(point => point.X) - minX;
        var rawHeight = rawPoints.Max(point => point.Y) - minY;
        return rawPoints
            .Select(point => new LocalPoint(
                rawWidth == 0 ? 0 : ((point.X - minX) / rawWidth) * width,
                rawHeight == 0 ? 0 : ((point.Y - minY) / rawHeight) * height))
            .ToArray();
    }

    private static void AssertStarPoints(IReadOnlyList<LocalPoint> expected, IReadOnlyList<LocalPoint> actual)
    {
        Assert.Equal(expected.Count, actual.Count);
        for (var index = 0; index < expected.Count; index++)
        {
            Assert.Equal(expected[index].X, actual[index].X, 12);
            Assert.Equal(expected[index].Y, actual[index].Y, 12);
        }
    }
}
