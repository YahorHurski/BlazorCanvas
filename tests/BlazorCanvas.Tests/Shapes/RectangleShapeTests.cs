using System.Text.Json;
using BlazorCanvas.Shapes;

namespace BlazorCanvas.Tests.Shapes;

public class RectangleShapeTests
{
    private readonly RectangleShape _subject = new();

    public static IEnumerable<object[]> InvalidJson()
    {
        yield return new object[] { "{}" };
        yield return new object[] { "{\"w\":10}" };
        yield return new object[] { "{\"w\":\"10\",\"h\":10}" };
        yield return new object[] { "{\"w\":0,\"h\":10}" };
        yield return new object[] { "{\"w\":-1,\"h\":10}" };
        yield return new object[] { "{\"w\":1e400,\"h\":10}" };
        yield return new object[] { "[]" };
    }

    [Theory]
    [MemberData(nameof(InvalidJson))]
    public void TryParseGeometry_InvalidExtent_ReturnsFalseWithNullGeometry(string raw)
    {
        using var document = JsonDocument.Parse(raw);
        Assert.False(_subject.TryParseGeometry(document.RootElement, out var geometry));
        Assert.Null(geometry);
    }

    [Fact]
    public void TryParseGeometry_FinitePositiveExtent_PreservesExactValue()
    {
        using var document = JsonDocument.Parse("{\"w\":12.5,\"h\":3.25}");
        Assert.True(_subject.TryParseGeometry(document.RootElement, out var geometry));
        Assert.Equal(new RectangleGeometry(12.5, 3.25), Assert.IsType<RectangleGeometry>(geometry));
    }

    [Fact]
    public void ToJson_ValidGeometry_RoundTripsCanonicalJson()
    {
        var geometry = new RectangleGeometry(300, 180);
        var json = _subject.ToJson(geometry);
        Assert.Equal("{\"w\":300,\"h\":180}", json);
        using var document = JsonDocument.Parse(json);
        Assert.True(_subject.TryParseGeometry(document.RootElement, out var parsed));
        Assert.Equal(geometry, parsed);
    }

    [Fact]
    public void BoundsOf_ValidRectangle_UsesTopLeftOrigin() => Assert.Equal(new Bbox(0, 0, 300, 180), _subject.BoundsOf(new RectangleGeometry(300, 180)));

    [Fact]
    public void FromGesture_CornerToCorner_ReproducesV11Placement()
    {
        var placement = _subject.FromGesture(new CanvasPoint(10, 20), new CanvasPoint(310, 200));
        Assert.Equal(10, placement.X);
        Assert.Equal(20, placement.Y);
        Assert.Equal(new RectangleGeometry(300, 180), placement.Geometry);
    }

    [Fact]
    public void FromGesture_ReversedCorners_ProducesSamePlacement()
    {
        var downRight = _subject.FromGesture(new CanvasPoint(10, 20), new CanvasPoint(310, 200));
        var upLeft = _subject.FromGesture(new CanvasPoint(310, 200), new CanvasPoint(10, 20));
        Assert.Equal(downRight, upLeft);
    }

    [Fact]
    public void FromGesture_FarCorner_ClampsToInclusiveCanvasBounds()
    {
        var placement = _subject.FromGesture(new CanvasPoint(-100, -100), new CanvasPoint(2000, 1000));
        Assert.Equal(new ShapePlacement(0, 0, new RectangleGeometry(1472, 828)), placement);
    }
}
