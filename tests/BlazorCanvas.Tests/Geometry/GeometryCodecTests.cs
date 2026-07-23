using BlazorCanvas.Geometry;

namespace BlazorCanvas.Tests.Geometry;

public sealed class GeometryCodecTests
{
    public static TheoryData<FigureType, Box> RoundTripCases => new()
    {
        { FigureType.Rectangle, new Box(10, 20, 80, 90) },
        { FigureType.Triangle, new Box(5, 6, 45, 66) },
        { FigureType.Circle, new Box(20, 30, 60, 70) },
        { FigureType.Line, new Box(0, 50, 100, 50) },
        { FigureType.Line, new Box(40, 0, 40, 80) },
        { FigureType.Line, new Box(0, 0, 100, 50) },
        { FigureType.Line, new Box(0, 100, 100, 0) },
    };

    [Theory]
    [MemberData(nameof(RoundTripCases))]
    public void EncodeThenDecode_ReturnsOriginalBox(FigureType type, Box box)
    {
        var encoded = GeometryCodec.Encode(type, box);

        var decoded = GeometryCodec.DecodeToBox(type, encoded.X, encoded.Y, encoded.Geometry);

        Assert.Equal(box, decoded);
    }

    [Fact]
    public void Rectangle_UsesTopLeftAnchorAndWidthHeight()
    {
        var encoded = GeometryCodec.Encode(FigureType.Rectangle, new Box(10, 20, 80, 90));

        Assert.Equal(10, encoded.X);
        Assert.Equal(20, encoded.Y);
        Assert.Equal("""{"w":70,"h":70}""", encoded.Geometry);
    }

    [Fact]
    public void Triangle_UsesTopLeftAnchorAndWidthHeight()
    {
        var encoded = GeometryCodec.Encode(FigureType.Triangle, new Box(5, 6, 45, 66));

        Assert.Equal(5, encoded.X);
        Assert.Equal(6, encoded.Y);
        Assert.Equal("""{"w":40,"h":60}""", encoded.Geometry);
    }

    [Fact]
    public void Circle_UsesCentreAnchorAndRadius()
    {
        var encoded = GeometryCodec.Encode(FigureType.Circle, new Box(20, 30, 60, 70));

        Assert.Equal(40, encoded.X);
        Assert.Equal(50, encoded.Y);
        Assert.Equal("""{"r":20}""", encoded.Geometry);
    }

    [Fact]
    public void Line_PreservesSignedEndpointDelta()
    {
        var encoded = GeometryCodec.Encode(FigureType.Line, new Box(0, 0, 100, 50));

        Assert.Equal(0, encoded.X);
        Assert.Equal(0, encoded.Y);
        Assert.Equal("""{"dx":100,"dy":50}""", encoded.Geometry);
        Assert.Equal(new Box(0, 0, 100, 50), GeometryCodec.DecodeToBox(FigureType.Line, encoded.X, encoded.Y, encoded.Geometry));
    }
}
