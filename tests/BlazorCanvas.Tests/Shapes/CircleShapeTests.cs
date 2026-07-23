using System.Text.Json;
using BlazorCanvas.Shapes;

namespace BlazorCanvas.Tests.Shapes;

public class CircleShapeTests
{
    private readonly CircleShape _subject = new();

    public static IEnumerable<object[]> InvalidJson()
    {
        yield return new object[] { "{}" };
        yield return new object[] { "{\"r\":\"10\"}" };
        yield return new object[] { "{\"r\":0}" };
        yield return new object[] { "{\"r\":-1}" };
        yield return new object[] { "{\"r\":1e400}" };
        yield return new object[] { "[]" };
    }

    [Theory]
    [MemberData(nameof(InvalidJson))]
    public void TryParseGeometry_InvalidRadius_ReturnsFalseWithNullGeometry(string raw)
    {
        using var document = JsonDocument.Parse(raw);
        Assert.False(_subject.TryParseGeometry(document.RootElement, out var geometry));
        Assert.Null(geometry);
    }

    [Fact]
    public void TryParseGeometry_FinitePositiveRadius_PreservesExactValue()
    {
        using var document = JsonDocument.Parse("{\"r\":12.5}");
        Assert.True(_subject.TryParseGeometry(document.RootElement, out var geometry));
        Assert.Equal(new CircleGeometry(12.5), Assert.IsType<CircleGeometry>(geometry));
    }

    [Fact]
    public void ToJson_ValidGeometry_RoundTripsCanonicalJson()
    {
        var geometry = new CircleGeometry(100);
        var json = _subject.ToJson(geometry);
        Assert.Equal("{\"r\":100}", json);
        using var document = JsonDocument.Parse(json);
        Assert.True(_subject.TryParseGeometry(document.RootElement, out var parsed));
        Assert.Equal(geometry, parsed);
    }

    [Fact]
    public void BoundsOf_Radius100_UsesTopLeftOriginAndCentreAtRadius()
    {
        var bounds = _subject.BoundsOf(new CircleGeometry(100));
        Assert.Equal(new Bbox(0, 0, 200, 200), bounds);
        Assert.Equal(new LocalPoint(100, 100), new LocalPoint(bounds.X + (bounds.W / 2), bounds.Y + (bounds.H / 2)));
    }

    [Fact]
    public void FromGesture_CentreOut_OffsetsPlacementByRadius()
    {
        var placement = _subject.FromGesture(new CanvasPoint(700, 400), new CanvasPoint(800, 400));
        Assert.Equal(new ShapePlacement(600, 300, new CircleGeometry(100)), placement);
    }

    [Fact]
    public void FromGesture_NearLeftEdge_CapsRadiusAtNearestEdge()
    {
        var placement = _subject.FromGesture(new CanvasPoint(10, 300), new CanvasPoint(500, 300));
        Assert.Equal(new ShapePlacement(0, 290, new CircleGeometry(10)), placement);
    }

    [Fact]
    public void FromGesture_MidpointCoordinates_RoundsAwayFromZeroBeforeDistance()
    {
        var placement = _subject.FromGesture(new CanvasPoint(100.5, 100.5), new CanvasPoint(103.5, 100.5));
        Assert.Equal(new ShapePlacement(98, 98, new CircleGeometry(3)), placement);
    }

    [Fact]
    public void FromGesture_ZeroDistance_ProducesUndrawableZeroRadius()
    {
        var placement = _subject.FromGesture(new CanvasPoint(500, 300), new CanvasPoint(500, 300));
        var geometry = Assert.IsType<CircleGeometry>(placement.Geometry);
        Assert.Equal(0, geometry.R);
        Assert.False(_subject.IsDrawable(geometry));
    }
}
