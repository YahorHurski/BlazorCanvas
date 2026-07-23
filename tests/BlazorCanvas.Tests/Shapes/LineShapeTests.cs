using System.Text.Json;
using BlazorCanvas.Shapes;

namespace BlazorCanvas.Tests.Shapes;

public class LineShapeTests
{
    private readonly LineShape _subject = new();

    public static IEnumerable<object[]> InvalidJson()
    {
        yield return new object[] { "{}" };
        yield return new object[] { "{\"points\":[]}" };
        yield return new object[] { "{\"points\":[[0,0]]}" };
        yield return new object[] { "{\"points\":[[0,0],[1,1],[2,2]]}" };
        yield return new object[] { "{\"points\":[[0],[1,1]]}" };
        yield return new object[] { "{\"points\":[[0,\"a\"],[1,1]]}" };
        yield return new object[] { "{\"points\":\"nope\"}" };
        yield return new object[] { "[]" };
        yield return new object[] { "1" };
        yield return new object[] { "{\"points\":[[1e400,0],[1,1]]}" };
    }

    [Theory]
    [MemberData(nameof(InvalidJson))]
    public void TryParseGeometry_InvalidShape_ReturnsFalseWithNullGeometry(string raw)
    {
        using var document = JsonDocument.Parse(raw);
        var parsed = _subject.TryParseGeometry(document.RootElement, out var geometry);
        Assert.False(parsed);
        Assert.Null(geometry);
    }

    [Fact]
    public void TryParseGeometry_OrderedPoints_PreservesInputOrder()
    {
        using var document = JsonDocument.Parse("{\"points\":[[0,0],[100,40]]}");
        Assert.True(_subject.TryParseGeometry(document.RootElement, out var geometry));
        Assert.Equal(new[] { new LocalPoint(0, 0), new LocalPoint(100, 40) }, Assert.IsType<LineGeometry>(geometry).Points);
    }

    [Fact]
    public void ToJson_UpAndRightDiagonal_RoundTripsInDrawOrder()
    {
        var expected = new[] { new LocalPoint(0, 160), new LocalPoint(200, 0) };
        var json = _subject.ToJson(new LineGeometry(expected));
        Assert.Equal("{\"points\":[[0,160],[200,0]]}", json);
        using var document = JsonDocument.Parse(json);
        Assert.True(_subject.TryParseGeometry(document.RootElement, out var parsed));
        var actual = Assert.IsType<LineGeometry>(parsed).Points;
        Assert.Equal(expected, actual);
        Assert.NotEqual(new[] { new LocalPoint(0, 0), new LocalPoint(200, 160) }, actual);
    }

    [Fact]
    public void IsDrawable_IdenticalPoints_ReturnsFalse() => Assert.False(_subject.IsDrawable(new LineGeometry([new(1, 1), new(1, 1)])));

    [Theory]
    [InlineData(0, 0, 10, 0)]
    [InlineData(0, 0, 0, 10)]
    public void IsDrawable_HorizontalOrVerticalLine_ReturnsTrue(double x0, double y0, double x1, double y1) =>
        Assert.True(_subject.IsDrawable(new LineGeometry([new(x0, y0), new(x1, y1)])));

    [Fact]
    public void BoundsOf_HorizontalLine_HasZeroHeight() => Assert.Equal(new Bbox(1, 2, 9, 0), _subject.BoundsOf(new LineGeometry([new(10, 2), new(1, 2)])));

    [Fact]
    public void BoundsOf_VerticalLine_HasZeroWidth() => Assert.Equal(new Bbox(1, 2, 0, 9), _subject.BoundsOf(new LineGeometry([new(1, 11), new(1, 2)])));

    [Fact]
    public void FromGesture_UpAndRightLine_PreservesAbsoluteEndpoints()
    {
        var placement = _subject.FromGesture(new CanvasPoint(10, 100), new CanvasPoint(200, 20));
        var line = Assert.IsType<LineGeometry>(placement.Geometry);
        Assert.Equal(new LocalPoint(10, 100), new LocalPoint(placement.X + line.Points[0].X, placement.Y + line.Points[0].Y));
        Assert.Equal(new LocalPoint(200, 20), new LocalPoint(placement.X + line.Points[1].X, placement.Y + line.Points[1].Y));
    }

    [Fact]
    public void FromGesture_OutOfBoundsPoints_ClampsToInclusiveCanvasBounds()
    {
        var placement = _subject.FromGesture(new CanvasPoint(-10, -10), new CanvasPoint(2000, 1000));
        Assert.Equal(0, placement.X);
        Assert.Equal(0, placement.Y);
        Assert.Equal(new[] { new LocalPoint(0, 0), new LocalPoint(1472, 828) }, Assert.IsType<LineGeometry>(placement.Geometry).Points);
    }
}
