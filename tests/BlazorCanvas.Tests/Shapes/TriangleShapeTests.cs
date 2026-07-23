using System.Text.Json;
using BlazorCanvas.Shapes;

namespace BlazorCanvas.Tests.Shapes;

public class TriangleShapeTests
{
    private readonly TriangleShape _subject = new();

    public static IEnumerable<object[]> InvalidJson()
    {
        yield return new object[] { "{}" };
        yield return new object[] { "{\"points\":[]}" };
        yield return new object[] { "{\"points\":[[0,0]]}" };
        yield return new object[] { "{\"points\":[[0,0],[1,1],[2,2],[3,3]]}" };
        yield return new object[] { "{\"points\":[[0],[1,1],[2,2]]}" };
        yield return new object[] { "{\"points\":[[0,\"a\"],[1,1],[2,2]]}" };
        yield return new object[] { "{\"points\":\"nope\"}" };
        yield return new object[] { "[]" };
        yield return new object[] { "1" };
        yield return new object[] { "{\"points\":[[1e400,0],[1,1],[2,2]]}" };
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
    public void TryParseGeometry_OrderedPoints_PreservesInputOrder()
    {
        using var document = JsonDocument.Parse("{\"points\":[[0,0],[100,40],[25,120]]}");
        Assert.True(_subject.TryParseGeometry(document.RootElement, out var geometry));
        Assert.Equal(new[] { new LocalPoint(0, 0), new LocalPoint(100, 40), new LocalPoint(25, 120) }, Assert.IsType<TriangleGeometry>(geometry).Points);
    }

    [Theory]
    [InlineData(0, 0, 0, 0, 1, 1)]
    [InlineData(0, 0, 50, 40, 100, 80)]
    public void IsDrawable_DegenerateVertices_ReturnsFalse(double x0, double y0, double x1, double y1, double x2, double y2) =>
        Assert.False(_subject.IsDrawable(new TriangleGeometry([new(x0, y0), new(x1, y1), new(x2, y2)])));

    [Fact]
    public void IsDrawable_NormalTriangle_ReturnsTrue() => Assert.True(_subject.IsDrawable(new TriangleGeometry([new(10, 0), new(0, 20), new(20, 20)])));

    [Fact]
    public void BoundsOf_FractionalApex_DoesNotRoundAndIsStable()
    {
        var geometry = new TriangleGeometry([new(13.5, 0), new(-2, 30), new(25, 30)]);
        var first = _subject.BoundsOf(geometry);
        Assert.Equal(new Bbox(-2, 0, 27, 30), first);
        Assert.Equal(first, _subject.BoundsOf(geometry));
    }

    [Fact]
    public void FromGesture_DownwardGesture_UsesUpwardIsoscelesVertexFormula()
    {
        var placement = _subject.FromGesture(new CanvasPoint(100, 20), new CanvasPoint(20, 100));
        Assert.Equal(20, placement.X);
        Assert.Equal(20, placement.Y);
        Assert.Equal(new[] { new LocalPoint(40, 0), new LocalPoint(0, 80), new LocalPoint(80, 80) }, Assert.IsType<TriangleGeometry>(placement.Geometry).Points);
    }

    [Fact]
    public void FromGesture_OutOfBoundsPoints_ClampsToInclusiveCanvasBounds()
    {
        var placement = _subject.FromGesture(new CanvasPoint(-10, -10), new CanvasPoint(2000, 1000));
        Assert.Equal(0, placement.X);
        Assert.Equal(0, placement.Y);
        Assert.Equal(new[] { new LocalPoint(736, 0), new LocalPoint(0, 828), new LocalPoint(1472, 828) }, Assert.IsType<TriangleGeometry>(placement.Geometry).Points);
    }
}
